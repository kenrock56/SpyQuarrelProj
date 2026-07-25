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

        [Header("Patrol Settings")]
        [SerializeField] private PatrolRoute _patrolRoute;
        [SerializeField] private bool _isUsingPatrol;
        [SerializeField] private Vector3 _agentDestination;
        [SerializeField, Min(1)] private int _randomDestinationAttempts = 5;

        private int _currentPatrolIndex;
        private bool _hasDestinationOverride;

        [Header("References")]
        [SerializeField] private NavMeshAgent _agent;

        public NPCIdenitityProvider IdentityProvider =>
            _identityProvider;

        [SerializeField]
        private NPCIdenitityProvider _identityProvider;

        [Header("Movement")]
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _randomRange = 24f;
        [SerializeField] private float _navMeshSampleRadius = 10f;

        [Header("Network Snapshots")]
        [SerializeField] private float _snapshotRate = 15f;

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
                if (IsServer &&
                    _agent != null &&
                    _agent.enabled)
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
                Vector3 velocity = Velocity;

                return new Vector2(
                    velocity.x,
                    velocity.z
                ).magnitude;
            }
        }

        public Vector3 ForwardDirection =>
            transform.forward;

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
                _agent.stoppingDistance =
                    _stoppingDistance;
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

            _networkVelocity = Vector3.zero;

            if (IsServer)
            {
                _agent.enabled = true;

                if (_identityProvider != null)
                {
                    _networkAppearance.Value =
                        _identityProvider.NpcIdentityType;
                }

                if (_isUsingPatrol)
                {
                    SetInitialPatrolIndex();
                    ChooseNewDestination();
                }

                SendSnapshotImmediately();
            }
            else
            {
                _agent.enabled = false;
            }

            if (_identityProvider != null)
            {
                ApplyAppearance(
                    _networkAppearance.Value
                );
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
            if (!IsSpawned ||
                _agent == null)
            {
                return;
            }

            if (IsServer)
            {
                UpdateServerMovement();
                _identityProvider?.UpdateLocal();
            }
            else
            {
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

        private void SetAppearanceServer(
            NpcType identity)
        {
            if (!IsServer)
                return;

            bool valueChanged =
                !_networkAppearance.Value.Equals(identity);

            _networkAppearance.Value = identity;

            if (valueChanged)
                return;

            ApplyAppearance(identity);
            RebuildAppearanceRpc(identity);
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void RequestSetAppearanceRpc(
            NpcType identity)
        {
            SetAppearanceServer(identity);
        }

        [Rpc(SendTo.NotServer)]
        private void RebuildAppearanceRpc(
            NpcType identity)
        {
            ApplyAppearance(identity);
        }

        private void OnNetworkAppearanceChanged(
            NpcType previousIdentity,
            NpcType newIdentity)
        {
            ApplyAppearance(newIdentity);
        }

        private void ApplyAppearance(
            NpcType identity)
        {
            if (_identityProvider == null)
                return;

            _identityProvider.SetAppearance(identity);
        }

        private void UpdateServerMovement()
        {
            bool reachedDestination =
                !_agent.pathPending &&
                _agent.hasPath &&
                _agent.remainingDistance <=
                _agent.stoppingDistance;

            bool hasNoPath =
                !_agent.pathPending &&
                !_agent.hasPath;

            if (_hasDestinationOverride)
            {
                if (reachedDestination || hasNoPath)
                {
                    _hasDestinationOverride = false;
                    _agent.ResetPath();
                }
            }
            else if (_isUsingPatrol &&
                     (reachedDestination || hasNoPath))
            {
                ChooseNewDestination();
            }

            if (Time.time < _nextStateTime)
                return;

            float timeDelta =
                1f / Mathf.Max(1f, _snapshotRate);

            _nextStateTime =
                Time.time + timeDelta;

            SendVelocityRpc(_agent.velocity);
        }

        public void StartPatrol()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                StartPatrolServer();
            }
            else
            {
                StartPatrolRpc();
            }
        }

        public void StopPatrol()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                StopPatrolServer();
            }
            else
            {
                StopPatrolRpc();
            }
        }

        public void RequestDestination(
            Vector3 destination)
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                SetDestinationOverride(destination);
            }
            else
            {
                RequestDestinationRpc(destination);
            }
        }

        public void RequestRandomDestination()
        {
            if (!IsSpawned)
                return;

            if (IsServer)
            {
                SetRandomDestinationOverride();
            }
            else
            {
                RequestRandomDestinationRpc();
            }
        }

        public void Interact(
            Interactor interactor)
        {
            if (interactor == null)
                return;

            if (interactor.transform.root.TryGetComponent(
                    out SpyCharacter spyCharacter))
            {
                // Add interaction behaviour here.
            }
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void StartPatrolRpc()
        {
            StartPatrolServer();
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void StopPatrolRpc()
        {
            StopPatrolServer();
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void RequestDestinationRpc(
            Vector3 destination)
        {
            SetDestinationOverride(destination);
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Everyone
        )]
        private void RequestRandomDestinationRpc()
        {
            SetRandomDestinationOverride();
        }

        private void StartPatrolServer()
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return;
            }

            PatrolPoint[] patrolPoints =
                _patrolRoute != null
                    ? _patrolRoute.PatrolPoints
                    : null;

            if (patrolPoints == null ||
                patrolPoints.Length == 0)
            {
                Debug.LogWarning(
                    "[NPCharacter] Cannot start patrol because no patrol route or patrol points are assigned.",
                    this
                );

                return;
            }

            _agent.ResetPath();

            _hasDestinationOverride = false;
            _isUsingPatrol = true;

            SetInitialPatrolIndex();
            ChooseNewDestination();
        }

        private void StopPatrolServer()
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return;
            }

            _isUsingPatrol = false;
            _hasDestinationOverride = false;

            _agent.ResetPath();
        }

        private void SetDestinationOverride(
            Vector3 destination)
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return;
            }

            StopPatrolServer();

            if (TrySetDestination(destination))
            {
                _hasDestinationOverride = true;
            }
        }

        private void SetRandomDestinationOverride()
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return;
            }

            StopPatrolServer();

            if (TrySetRandomDestination())
            {
                _hasDestinationOverride = true;
            }
        }

        private void ChooseNewDestination()
        {
            if (!IsServer ||
                !_isUsingPatrol ||
                _agent == null ||
                !_agent.enabled)
            {
                return;
            }

            if (TrySetNextPatrolDestination())
                return;

            StopPatrolServer();

            Debug.LogWarning(
                "[NPCharacter] Patrol stopped because no valid patrol destination could be found.",
                this
            );
        }

        private bool TrySetNextPatrolDestination()
        {
            PatrolPoint[] patrolPoints =
                _patrolRoute != null
                    ? _patrolRoute.PatrolPoints
                    : null;

            if (patrolPoints == null ||
                patrolPoints.Length == 0)
            {
                return false;
            }

            _currentPatrolIndex =
                Mathf.Abs(_currentPatrolIndex) %
                patrolPoints.Length;

            for (int i = 0;
                 i < patrolPoints.Length;
                 i++)
            {
                int pointIndex =
                    (_currentPatrolIndex + i) %
                    patrolPoints.Length;

                PatrolPoint patrolPoint =
                    patrolPoints[pointIndex];

                if (patrolPoint == null)
                    continue;

                if (!TrySetDestination(
                        patrolPoint.transform.position))
                {
                    continue;
                }

                _currentPatrolIndex =
                    (pointIndex + 1) %
                    patrolPoints.Length;

                return true;
            }

            return false;
        }

        private bool TrySetRandomDestination()
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return false;
            }

            int attempts =
                Mathf.Max(
                    1,
                    _randomDestinationAttempts
                );

            for (int i = 0; i < attempts; i++)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(
                        -_randomRange,
                        _randomRange
                    ),
                    0f,
                    Random.Range(
                        -_randomRange,
                        _randomRange
                    )
                );

                Vector3 randomPosition =
                    transform.position + randomOffset;

                if (TrySetDestination(randomPosition))
                {
                    return true;
                }
            }

            Debug.LogWarning(
                $"[NPCharacter] Could not find a valid random destination after {attempts} attempts.",
                this
            );

            return false;
        }

        private bool TrySetDestination(
            Vector3 requestedPosition)
        {
            if (!IsServer ||
                _agent == null ||
                !_agent.enabled)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    requestedPosition,
                    out NavMeshHit hit,
                    _navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshPath path =
                new NavMeshPath();

            if (!_agent.CalculatePath(
                    hit.position,
                    path))
            {
                return false;
            }

            if (path.status !=
                NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            if (!_agent.SetPath(path))
            {
                return false;
            }

            _agentDestination = hit.position;

            return true;
        }

        private void SetInitialPatrolIndex()
        {
            _currentPatrolIndex = 0;

            if (_patrolRoute == null)
                return;

            PatrolPoint[] patrolPoints =
                _patrolRoute.PatrolPoints;

            if (patrolPoints == null ||
                patrolPoints.Length == 0)
            {
                return;
            }

            float closestDistance =
                float.PositiveInfinity;

            for (int i = 0;
                 i < patrolPoints.Length;
                 i++)
            {
                PatrolPoint patrolPoint =
                    patrolPoints[i];

                if (patrolPoint == null)
                    continue;

                float distance = (
                    patrolPoint.transform.position -
                    transform.position
                ).sqrMagnitude;

                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                _currentPatrolIndex = i;
            }
        }

        private void SendSnapshotImmediately()
        {
            if (!IsServer)
                return;

            Vector3 velocity =
                _agent != null && _agent.enabled
                    ? _agent.velocity
                    : Vector3.zero;

            SendVelocityRpc(velocity);
        }

        [Rpc(SendTo.NotServer)]
        private void SendVelocityRpc(
            Vector3 velocity)
        {
            _networkVelocity = velocity;
        }
    }
}