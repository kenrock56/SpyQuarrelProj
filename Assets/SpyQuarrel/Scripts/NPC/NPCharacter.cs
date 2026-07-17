using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SpyQuarrelRuntime
{
    public class NPCharacter : NetworkBehaviour, IAnimatorContext, IInteractable
    {

        public string InteractName => "Jeff";
        public string InteractDescription => "press blah to blah";
        public bool IsInteractable => true;
        
        private readonly NetworkVariable<int> _animatorState = new
        (
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private NPCIdenitityProvider _identityProvider;
        [SerializeField] private Animator _animatorRef;

        [Header("Movement")]
        [SerializeField] private float _arrivalDistance = 0.5f;
        [SerializeField] private float _randomRange = 24f;
        [SerializeField] private float _navMeshSampleRadius = 10f;

        [Header("Network Snapshots")]
        [SerializeField] private float _snapshotRate = 15f;
        [SerializeField] private float _remotePositionLerpSpeed = 15f;
        [SerializeField] private float _remoteRotationLerpSpeed = 15f;

        [Header("Animation Synchronization")]
        [SerializeField] private int _animatorLayer = 0;
        [SerializeField] private float _animationCrossFadeDuration = 0.31f;

        private Vector3 _networkTargetPosition;
        private float _networkTargetYaw;
        private Vector3 _networkVelocity;

        private float _nextSnapshotTime;
        private bool _hasReceivedSnapshot;

        private int _lastWrittenAnimatorState;
        private int _lastAppliedAnimatorState;

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

        private void Awake()
        {
            FindMissingReferences();
        }

        private void FindMissingReferences()
        {
            if (_agent == null)
            {
                _agent = transform.root.GetComponentInChildren<NavMeshAgent>(true);
            }

            if (_identityProvider == null)
            {
                _identityProvider =
                    transform.root.GetComponentInChildren<NPCIdenitityProvider>(true);
            }

            CacheAnimator();
        }

        private void CacheAnimator()
        {
            if (_identityProvider != null &&
                _identityProvider.CurrentAnimator != null)
            {
                _animatorRef = _identityProvider.CurrentAnimator;
                return;
            }

            _animatorRef =
                transform.root.GetComponentInChildren<Animator>(true);
        }
        

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            FindMissingReferences();

            _animatorState.OnValueChanged += OnAnimatorStateChanged;

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

            if (_identityProvider != null)
            {
                SetAppearence(_identityProvider.NpcIdentityType);
            }
        }

        public override void OnNetworkDespawn()
        {
            _animatorState.OnValueChanged -= OnAnimatorStateChanged;

            base.OnNetworkDespawn();
        }

        public void SetAppearence(NpcType identity)
        {
            if (_identityProvider == null)
                return;

            _identityProvider.SetAppearance(identity);

            /*
             * The provider processes its queued appearance change during
             * LateUpdate, so the new Animator may not exist immediately.
             * UpdateAnimatorState will keep trying to cache it.
             */
            _animatorRef = null;
            _lastWrittenAnimatorState = 0;
            _lastAppliedAnimatorState = 0;
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

            UpdateAnimatorState();
        }

        private void UpdateAnimatorState()
        {
            if (_animatorRef == null)
            {
                CacheAnimator();

                if (_animatorRef == null)
                    return;
            }

            if (IsOwner)
            {
                WriteAnimatorState();
            }
            else
            {
                ApplyAnimatorState();
            }
        }

        private void WriteAnimatorState()
        {
            int currentState = GetAnimState();

            if (currentState == 0)
                return;

            if (currentState == _lastWrittenAnimatorState)
                return;

            _lastWrittenAnimatorState = currentState;
            _animatorState.Value = currentState;
        }

        private void ApplyAnimatorState()
        {
            int targetState = _animatorState.Value;

            if (targetState == 0)
                return;

            if (targetState == _lastAppliedAnimatorState)
                return;

            AnimatorStateInfo currentState =
                _animatorRef.GetCurrentAnimatorStateInfo(_animatorLayer);

            if (currentState.fullPathHash == targetState)
            {
                _lastAppliedAnimatorState = targetState;
                return;
            }

            _lastAppliedAnimatorState = targetState;

            _animatorRef.CrossFade(targetState, _animationCrossFadeDuration, _animatorLayer);
        }

        private void OnAnimatorStateChanged(int previousState, int newState) {
            if (IsOwner)
                return;

            /*
             * Reset this so UpdateAnimatorState applies the newly received
             * state as soon as the local Animator is available.
             */
            _lastAppliedAnimatorState = 0;
        }

        private int GetAnimState()
        {
            if (_animatorRef == null)
                return 0;

            AnimatorStateInfo state =
                _animatorRef.GetCurrentAnimatorStateInfo(_animatorLayer);

            /*
             * fullPathHash matches Animator.CrossFade(int) reliably when
             * using the same Animator Controller on every peer.
             */
            return state.fullPathHash;
        }

        private void UpdateServerMovement()
        {
            if (!_agent.pathPending && _agent.hasPath && _agent.remainingDistance <= 
                Mathf.Max(_agent.stoppingDistance, _arrivalDistance)) { ChooseNewDestination(); }

            if (!_agent.pathPending && !_agent.hasPath)
            {
                ChooseNewDestination();
            }

            if (Time.time < _nextSnapshotTime)
                return;

            float interval = 1f / Mathf.Max(1f, _snapshotRate);
            _nextSnapshotTime = Time.time + interval;

            SendSnapshotRpc(transform.position, transform.eulerAngles.y, _agent.velocity);
        }

        private void UpdateRemoteTransform()
        {
            if (!_hasReceivedSnapshot)
                return;

            float positionT = 1f - Mathf.Exp(-_remotePositionLerpSpeed * Time.deltaTime);

            float rotationT = 1f - Mathf.Exp(-_remoteRotationLerpSpeed * Time.deltaTime);

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
            if (interactor.transform.root.TryGetComponent(out SpyCharacter character))
            {
                
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
                return;

            var x = Random.Range(-_randomRange, _randomRange);
            var z = Random.Range(-_randomRange, _randomRange);
            
            Vector3 randomPosition = new Vector3(x, transform.position.y, z);

            TrySetDestination(randomPosition);
        }

        private bool TrySetDestination(Vector3 requestedPosition)
        {
            if (!IsServer || _agent == null || !_agent.enabled)
                return false;

            if (!NavMesh.SamplePosition
                (requestedPosition, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas
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
            
            Vector3 velocity = _agent != null && _agent.enabled ? _agent.velocity : Vector3.zero;

            SendSnapshotRpc(transform.position, transform.eulerAngles.y, velocity);
        }

        [Rpc(SendTo.NotServer)]
        private void SendSnapshotRpc(Vector3 position, float yaw, Vector3 velocity)
        {
            _networkTargetPosition = position;
            _networkTargetYaw = yaw;
            _networkVelocity = velocity;
            _hasReceivedSnapshot = true;
        }

       
    }
}