using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace SpyQuarrelRuntime
{
    public class NPCharacter : NetworkBehaviour, IAnimatorContext
    {
        
        public Vector3 Velocity
        {
            get
            {
                if (IsServer && _agent != null && _agent.enabled)
                    return _agent.velocity;

                return _networkVelocity;
            }
        }

        public float Speed
        {
            get
            {
                Vector3 velocity = Velocity;
                return new Vector2(velocity.x, velocity.z).magnitude;
            }
        }

        public Vector3 ForwardDirection => transform.forward;
        
        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;

        [SerializeField]private NPCIdenitityProvider _identityProvider;
        
        [Header("Movement")]
        [SerializeField] private float _arrivalDistance = 0.5f;
        [SerializeField] private float _randomRange = 24f;
        [SerializeField] private float _navMeshSampleRadius = 10f;

        [Header("Network Snapshots")]
        [SerializeField] private float _snapshotRate = 15f;
        [SerializeField] private float _remotePositionLerpSpeed = 15f;
        [SerializeField] private float _remoteRotationLerpSpeed = 15f;

        private Vector3 _networkTargetPosition;
        private float _networkTargetYaw;
        private Vector3 _networkVelocity;

        private float _nextSnapshotTime;
        private bool _hasReceivedSnapshot;

        private void Awake()
        {
            if (_agent == null)
            {
                _agent = transform.root.GetComponentInChildren<NavMeshAgent>(true);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

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

                _networkTargetPosition = transform.position;
                _networkTargetYaw = transform.eulerAngles.y;
                _networkVelocity = Vector3.zero;

                ChooseNewDestination();
                SendSnapshotImmediately();
            }
            else
            {
                _agent.enabled = false;

                _networkTargetPosition = transform.position;
                _networkTargetYaw = transform.eulerAngles.y;
                _networkVelocity = Vector3.zero;
            }
            
            SetAppearence(_identityProvider.NpcIdentityType);
        }

        public void SetAppearence(NpcType identity)
        {
            if (_identityProvider == null)return;
            
            _identityProvider.SetAppearance(identity);

            if (transform.root.TryGetComponent(out NetworkAnimator animator))
            {
                Destroy(animator);
            }

            var newAnimator = transform.root.gameObject.AddComponent<NetworkAnimator>();
            
            var anim = transform.root.GetComponentInChildren<Animator>(true);

            if (anim != null)
            {
                newAnimator.Animator = anim;
            }
            
            
        }
        
        private void Update()
        {
            if (!IsSpawned || _agent == null)
                return;

            if (IsServer)
            {
                UpdateServerMovement();
            }
            else
            {
                UpdateRemoteTransform();
            }
        }

        private void UpdateServerMovement()
        {
            if (!_agent.pathPending &&
                _agent.hasPath &&
                _agent.remainingDistance <=
                Mathf.Max(_agent.stoppingDistance, _arrivalDistance))
            {
                ChooseNewDestination();
            }

            if (!_agent.pathPending && !_agent.hasPath)
            {
                ChooseNewDestination();
            }

            if (Time.time >= _nextSnapshotTime)
            {
                float interval = 1f / Mathf.Max(1f, _snapshotRate);
                _nextSnapshotTime = Time.time + interval;

                SendSnapshotRpc
                (
                    transform.position,
                    transform.eulerAngles.y,
                    _agent.velocity
                );
            }
        }

        private void UpdateRemoteTransform()
        {
            if (!_hasReceivedSnapshot)
                return;

            float positionT = 1f - Mathf.Exp
            (
                -_remotePositionLerpSpeed * Time.deltaTime
            );

            float rotationT = 1f - Mathf.Exp
            (
                -_remoteRotationLerpSpeed * Time.deltaTime
            );

            transform.position = Vector3.Lerp
            (
                transform.position,
                _networkTargetPosition,
                positionT
            );

            Quaternion targetRotation = Quaternion.Euler
            (
                0f,
                _networkTargetYaw,
                0f
            );

            transform.rotation = Quaternion.Slerp
            (
                transform.rotation,
                targetRotation,
                rotationT
            );
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

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void RequestRandomDestinationRpc()
        {
            ChooseNewDestination();
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void RequestDestinationRpc(Vector3 destination)
        {
            TrySetDestination(destination);
        }

        private void ChooseNewDestination()
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return;

            Vector3 randomPosition = new Vector3
            (
                UnityEngine.Random.Range(-_randomRange, _randomRange),
                transform.position.y,
                UnityEngine.Random.Range(-_randomRange, _randomRange)
            );

            TrySetDestination(randomPosition);
        }

        private bool TrySetDestination(Vector3 requestedPosition)
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return false;

            if (!NavMesh.SamplePosition
                (
                    requestedPosition,
                    out NavMeshHit hit,
                    _navMeshSampleRadius,
                    NavMesh.AllAreas
                ))
            {
                return false;
            }

            return _agent.SetDestination(hit.position);
        }

        private void SendSnapshotImmediately()
        {
            if (!IsServer)
                return;

            SendSnapshotRpc
            (
                transform.position,
                transform.eulerAngles.y,
                _agent != null ? _agent.velocity : Vector3.zero
            );
        }

        [Rpc(SendTo.NotServer)]
        private void SendSnapshotRpc
        (
            Vector3 position,
            float yaw,
            Vector3 velocity
        )
        {
            _networkTargetPosition = position;
            _networkTargetYaw = yaw;
            _networkVelocity = velocity;
            _hasReceivedSnapshot = true;
        }
        
    }
}