using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SpyQuarrelRuntime
{
    public class NPCharacter :
        NetworkBehaviour,
        IAnimatorContext,
        IInteractable
    {
        public string InteractName => "Jeff";

        public string InteractDescription =>
            "press blah to blah";

        public bool IsInteractable => true;

        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField]
        private NPCIdenitityProvider _identityProvider;

        [Header("Movement")]
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _randomRange = 24f;
        [SerializeField] private float _navMeshSampleRadius = 10f;

        [Header("Network Snapshots")]
        [SerializeField] private float _snapshotRate = 15f;
        [SerializeField]
        private float _remotePositionLerpSpeed = 15f;
        [SerializeField]
        private float _remoteRotationLerpSpeed = 15f;

        private Vector3 _networkTargetPosition;
        private float _networkTargetYaw;
        private Vector3 _networkVelocity;

        private float _nextStateTime;

        private readonly NetworkVariable<NpcType> _networkAppearance = new
        (
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public Vector3 Velocity
        {
            get
            {
                if (IsServer && _agent != null && _agent.enabled)
                {
                    return _agent.velocity;
                }

                return _networkVelocity;
            }
        }

        public float Speed
        {
            get
            {
                var velocity = Velocity;
                return new Vector2(velocity.x, velocity.z).magnitude;
            }
        }

        public Vector3 ForwardDirection => transform.forward;

        private void Awake()
        {
            InitComponents();
        }

        private void InitComponents()
        {
            
            if (_agent == null)
            {
                _agent = transform.root
                    .GetComponentInChildren<NavMeshAgent>(true);
            }

            if (_identityProvider == null)
            {
                _identityProvider = transform.root
                    .GetComponentInChildren<NPCIdenitityProvider>(true);
            }

            if (_agent != null)
            { 
                _agent.stoppingDistance = _stoppingDistance;
            }
            
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            InitComponents();

            _networkAppearance.OnValueChanged +=
                OnNetworkAppearanceChanged;

            if (_agent == null)
            {
                Debug.LogError(
                    "[NPCharacter] NavMeshAgent reference is missing.",
                    this
                );

                return;
            }

            if (IsServer)
            {
                _agent.enabled = true;

                _networkTargetPosition =
                    transform.position;

                _networkTargetYaw =
                    transform.eulerAngles.y;

                _networkVelocity =
                    Vector3.zero;

                if (_identityProvider != null)
                {
                    _networkAppearance.Value =
                        _identityProvider.NpcIdentityType;
                }

                ChooseNewDestination();
                SendSnapshotImmediately();
            }
            else
            {
                _agent.enabled = false;

                _networkTargetPosition =
                    transform.position;

                _networkTargetYaw =
                    transform.eulerAngles.y;

                _networkVelocity =
                    Vector3.zero;
            }

            if (_identityProvider != null)
            {
                ApplyAppearance(_networkAppearance.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkAppearance.OnValueChanged -=
                OnNetworkAppearanceChanged;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned || _agent == null)
                return;

            if (IsServer)
            {
                UpdateServerMovement();
                _identityProvider?.UpdateLocal();
            }
            else
            {
                UpdateRemoteTransform();
                _identityProvider?.UpdateRemote();
            }
        }

        public void SetAppearance(NpcType identity)
        {
            if (_identityProvider == null)
                return;

            if (!IsSpawned)
            {
                ApplyAppearance(identity);
                return;
            }

            if (IsServer)
            {
                SetAppearanceServer(identity);
            }
            else
            {
                RequestSetAppearanceRpc(identity);
            }
        }

        
        private void SetAppearanceServer(NpcType identity)
        {
            if (!IsServer) return;

            bool valueChanged = !_networkAppearance.Value.Equals(identity);

            _networkAppearance.Value = identity;

            if (valueChanged) return;
            
            ApplyAppearance(identity);
            RebuildAppearanceRpc(identity);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestSetAppearanceRpc(NpcType identity)
        {
            SetAppearanceServer(identity);
        }

        [Rpc(SendTo.NotServer)]
        private void RebuildAppearanceRpc(NpcType identity)
        {
            ApplyAppearance(identity);
        }

        private void OnNetworkAppearanceChanged(NpcType previousIdentity, NpcType newIdentity)
        {
            ApplyAppearance(newIdentity);
        }

        private void ApplyAppearance(NpcType identity)
        {
            if (_identityProvider == null) return;

            _identityProvider.SetAppearance(identity);
        }

        private void UpdateServerMovement()
        {
            bool reachedDestination = !_agent.pathPending && _agent.hasPath && 
                                      _agent.remainingDistance <= _agent.stoppingDistance;

            bool hasNoPath = !_agent.pathPending && !_agent.hasPath;

            if (reachedDestination || hasNoPath)
            {
                ChooseNewDestination();
            }

            if (Time.time < _nextStateTime)
                return;

            float timeDelta = 1f / _snapshotRate;

            _nextStateTime = Time.time + timeDelta;

            SendStateRpc(transform.position, transform.eulerAngles.y, _agent.velocity);
        }

        private void UpdateRemoteTransform()
        {
            float positionT = Mathf.Clamp01(_remotePositionLerpSpeed * Time.deltaTime);

            float rotationT = Mathf.Clamp01(_remoteRotationLerpSpeed * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, _networkTargetPosition, positionT);

            Quaternion targetRotation = Quaternion.Euler(0f, _networkTargetYaw, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
        }

        public void RequestRandomDestination()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                ChooseNewDestination();
            }
            else
            {
                RequestRandomDestinationRpc();
            }
        }

        public void RequestDestination(Vector3 destination)
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                TrySetDestination(destination);
            }
            else
            {
                RequestDestinationRpc(destination);
            }
        }

        public void Interact(Interactor interactor)
        {
            if (interactor == null)
                return;

            if (interactor.transform.root.TryGetComponent(
                    out SpyCharacter spyCharacter))
            {
                // Add interaction behaviour here.
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestRandomDestinationRpc()
        {
            ChooseNewDestination();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestDestinationRpc(Vector3 destination)
        {
            TrySetDestination(destination);
        }

        private void ChooseNewDestination()
        {
            if (!IsServer || _agent == null || !_agent.enabled)
            {
                return;
            }

            float x = Random.Range(-_randomRange, _randomRange);

            float z = Random.Range(-_randomRange, _randomRange);

            Vector3 randomPosition = new Vector3(x, transform.position.y, z);

            TrySetDestination(randomPosition);
        }

        private bool TrySetDestination(Vector3 requestedPosition)
        {
            if (!IsServer || _agent == null || !_agent.enabled)
            {
                return false;
            }

            //valid path
            if (!NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            return _agent.SetDestination(hit.position);
        }

        private void SendSnapshotImmediately()
        {
            if (!IsServer)
                return;

            Vector3 velocity = _agent != null && _agent.enabled ? _agent.velocity : Vector3.zero;

            SendStateRpc(transform.position, transform.eulerAngles.y, velocity);
        }

        [Rpc(SendTo.NotServer)]
        private void SendStateRpc(Vector3 position, float yaw, Vector3 velocity)
        {
            _networkTargetPosition = position;
            _networkTargetYaw = yaw;
            _networkVelocity = velocity;
        }
    }
}