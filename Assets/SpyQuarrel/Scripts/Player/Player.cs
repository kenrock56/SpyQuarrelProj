using System;
using System.Collections.Generic;
using KinematicCharacterController;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private PlayerInputController _inputController;
        [SerializeField] private PlayerCamera _camera;
        public PlayerCharacter Character => _character;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private Transform _playerRoot;
        [SerializeField] protected bool _networkSuccess = false;
        
        
        private KinematicSimulationBridge _simulationBridge;

        [SerializeField] private int _bufferSize = 1024;
        private NetworkTimer _networkTimer;

        [SerializeField] private CircularBuffer<PlayerStatePayload> _clientStateBuffer;
        [SerializeField] private CircularBuffer<PlayerInputPayload> _clientInputBuffer;

        private PlayerStatePayload _lastServerState;
        private PlayerStatePayload _lastProcessedState;

        [SerializeField] private CircularBuffer<PlayerStatePayload> _serverStateBuffer;
        private Queue<PlayerInputPayload> _serverInputQueue;

        private bool _offlineMotorRegistered;

        protected virtual void Awake()
        {
            if (_bufferSize <= 1)
                _bufferSize = 1024;

            if (_inputController == null)
                TryGetComponent(out _inputController);

            _simulationBridge = KinematicSimulationBridge.Instance;

            if (_character != null)
                _character.Initialize();

            _clientStateBuffer = new CircularBuffer<PlayerStatePayload>(_bufferSize);
            _clientInputBuffer = new CircularBuffer<PlayerInputPayload>(_bufferSize);
            _serverStateBuffer = new CircularBuffer<PlayerStatePayload>(_bufferSize);
            _serverInputQueue = new Queue<PlayerInputPayload>();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                CreateNetworkTimer();
        }

        public void InitializeSpawnPosition(Vector3 position)
        {
            transform.position = position;

            if (_character != null)
                _character.Teleport(position);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _networkSuccess = true;

            if (_simulationBridge == null)
                _simulationBridge = KinematicSimulationBridge.Instance;

            if (_character == null)
            {
                Debug.LogError("[Player] Character reference is null.", this);
                return;
            }

            if (_character.Motor == null)
            {
                Debug.LogError("[Player] Character Motor reference is null.", this);
                return;
            }

            if (_networkTimer == null)
                CreateNetworkTimer();

            if (IsServer || IsOwner)
                _simulationBridge.RegisterMotor(_character.Motor);

            if (IsOwner)
                EnableLocalItems();
            else
                DisableLocalItems();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (_character != null && (IsServer || IsOwner))
                _simulationBridge.UnregisterMotor(_character.Motor);

            UnregisterTimer();
        }

        private void OnDestroy()
        {
            if (!_networkSuccess && _offlineMotorRegistered && _character != null)
            {
                _simulationBridge.UnregisterMotor(_character.Motor);
                _offlineMotorRegistered = false;
            }

            UnregisterTimer();
        }

        private void Start()
        {
            if (!_networkSuccess)
            {
                if (_character != null && !_offlineMotorRegistered)
                {
                    _simulationBridge.RegisterMotor(_character.Motor);
                    _offlineMotorRegistered = true;
                }

                CursorManager.SetCursor(false);
                EnableLocalItems();
                return;
            }

            if (_networkSuccess && !IsOwner)
            {
                DisableLocalItems();
                return;
            }

            CursorManager.SetCursor(false);
            EnableLocalItems();
        }

        private void Update()
        {
            if (_character != null)
                _character.UpdateBody(Time.deltaTime);
            
            OnUpdate();

            if (_networkSuccess && !IsOwner)
                return;

            if (_inputController == null || _camera == null)
                return;

            CameraInput cameraInput = new CameraInput(_inputController.LookInput);
            _camera.UpdateRotation(cameraInput);

            if (_inputController.FirePressed)
            {
                Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.point == Vector3.zero)
                        return;
                    TeleportCheat(hit.point);
                    //Teleport(hit.point);
                }
            }
        }

        private void FixedUpdate()
        {
            
            OnFixedUpdate();
            
            if (_networkSuccess)
                return;

            if (_character == null || _inputController == null || _camera == null)
                return;

            PlayerInputCommand command = GetRequestedMovement();

            _character.UpdateInput(command);

            if (!_networkSuccess)
                KinematicCharacterSystem.Settings.AutoSimulation = true;
        }
        
        protected virtual void OnFixedUpdate(){}
        protected virtual void OnLateUpdate(){}
        protected virtual void OnUpdate(){}


        private void LateUpdate()
        {
            OnLateUpdate();
        }

        private void HandleServerTick()
        {
            if (!IsServer)
                return;

            if (_serverInputQueue == null)
                _serverInputQueue = new Queue<PlayerInputPayload>();

            PlayerStatePayload lastState = default;
            bool hadInput = false;

            while (_serverInputQueue.Count > 0)
            {
                PlayerInputPayload input = _serverInputQueue.Dequeue();

                if (input.Tick < 0)
                    continue;

                int bufferIndex = input.Tick % _bufferSize;

                PlayerStatePayload statePayload;

                if (IsHost && IsOwner)
                {
                    statePayload = _serverStateBuffer[bufferIndex];
                }
                else
                {
                    statePayload = ProcessMovement(input);

                    if (IsHost && !IsOwner)
                        _character.SetPredictedState(statePayload);
                }

                _serverStateBuffer[bufferIndex] = statePayload;

                if (!hadInput || input.Tick > lastState.Tick)
                {
                    lastState = statePayload;
                    hadInput = true;
                }
            }

            if (!hadInput)
                return;

            if (IsHost && IsOwner)
                return;

            SendToClientRpc(CreateReconciliationStatePayload(lastState));
            SendStateToObserversRpc(CreateImpliedStatePayload(lastState));
        }

        private void HandleClientTick()
        {
            if (!IsClient || !IsOwner)
                return;

            int currentTick = _networkTimer.CurrentTick;

            if (currentTick < 0)
                return;

            int bufferIndex = currentTick % _bufferSize;

            PlayerInputPayload input = new PlayerInputPayload()
            {
                Tick = currentTick,
                Command = GetRequestedMovement()
            };

            _clientInputBuffer[bufferIndex] = input;

            if (IsHost)
            {
                _serverInputQueue.Enqueue(input);

                PlayerStatePayload statePayload = ProcessMovement(input);

                _clientStateBuffer[bufferIndex] = statePayload;
                _serverStateBuffer[bufferIndex] = statePayload;

                SendToClientRpc(CreateReconciliationStatePayload(statePayload));
                SendStateToObserversRpc(CreateImpliedStatePayload(statePayload));
            }
            else
            {
                SendToServerRpc(input);

                PlayerStatePayload statePayload = ProcessMovement(input);

                _clientStateBuffer[bufferIndex] = statePayload;

                HandleServerReconciliation();
            }
        }

        private void HandleServerReconciliation()
        {
            if (!ShouldReconcile())
                return;

            if (_lastServerState.Tick < 0)
                return;

            int bufferIndex = _lastServerState.Tick % _bufferSize;

            PlayerStatePayload rewindState = _lastServerState;
            PlayerStatePayload currentState = _clientStateBuffer[bufferIndex];

            float positionError = Vector3.Distance(rewindState.Position, currentState.Position);

            if (positionError > 0.1f)
                ReconcileState(rewindState);

            _lastProcessedState = _lastServerState;
        }

        private void ReconcileState(PlayerStatePayload rewindState)
        {
            if (rewindState.Tick < 0)
                return;

            _character.SetPredictedState(rewindState);

            _clientStateBuffer[rewindState.Tick % _bufferSize] = rewindState;

            int tickToProcess = rewindState.Tick + 1;

            while (tickToProcess < _networkTimer.CurrentTick)
            {
                if (tickToProcess < 0)
                {
                    tickToProcess++;
                    continue;
                }

                int bufferIndex = tickToProcess % _bufferSize;

                PlayerInputPayload inputPayload = _clientInputBuffer[bufferIndex];
                PlayerStatePayload stateToProcess = ProcessMovement(inputPayload);

                _clientStateBuffer[bufferIndex] = stateToProcess;

                tickToProcess++;
            }
        }

        public void Teleport(Vector3 position)
        {
            if (_character == null)
                return;

            if (!_networkSuccess)
            {
                _character.Teleport(position);
                return;
            }

            if (!IsOwner)
                return;

            int localTick = _networkTimer.CurrentTick;

            if (localTick < 0)
                return;

            ApplyTeleportState(position, localTick, true, IsServer);

            if (IsServer)
                BroadcastTeleportState(localTick);
            else
                RequestTeleportRpc(position);
        }

        [Rpc(SendTo.Server)]
        public void RequestTeleportRpc(Vector3 position)
        {
            if (!IsServer)
                return;

            int serverTick = _networkTimer.CurrentTick;

            if (serverTick < 0)
                return;

            ApplyTeleportState(position, serverTick, false, true);
            BroadcastTeleportState(serverTick);
        }

        private void BroadcastTeleportState(int tick)
        {
            if (tick < 0)
                return;

            int bufferIndex = tick % _bufferSize;
            PlayerStatePayload state = _serverStateBuffer[bufferIndex];

            SendToClientRpc(CreateReconciliationStatePayload(state));
            SendStateToObserversRpc(CreateImpliedStatePayload(state));
        }


        private void TeleportCheat(Vector3 position)
        {
            _character.Teleport(position);
        }
        private void ApplyTeleportState(Vector3 position, int tick, bool writeClientBuffer, bool writeServerBuffer)
        {
            if (tick < 0)
                return;

            _character.Teleport(position);

            PlayerStatePayload state = _character.GetPredictedState(tick);
            int bufferIndex = tick % _bufferSize;

            if (writeClientBuffer)
                _clientStateBuffer[bufferIndex] = state;

            if (writeServerBuffer)
                _serverStateBuffer[bufferIndex] = state;

            _lastServerState = state;
            _lastProcessedState = state;
        }

        private bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);

            bool isLastUndefinedOrDifferent =
                _lastProcessedState.Equals(default) ||
                !_lastProcessedState.Equals(_lastServerState);

            return isNewServerState && isLastUndefinedOrDifferent;
        }

        [Rpc(SendTo.Server)]
        private void SendToServerRpc(PlayerInputPayload input)
        {
            if (_serverInputQueue == null)
                _serverInputQueue = new Queue<PlayerInputPayload>();

            _serverInputQueue.Enqueue(input);
        }

        [Rpc(SendTo.Owner)]
        private void SendToClientRpc(PlayerReconciliationStatePayload state)
        {
            if (!IsOwner)
                return;

            _lastServerState = state.ReconcileToFull();
        }

        [Rpc(SendTo.NotOwner)]
        private void SendStateToObserversRpc(PlayerImpliedStatePayload state)
        {
            if (IsServer)
                return;

            _character.SetImpliedState(state);
        }

        private PlayerStatePayload ProcessMovement(PlayerInputPayload input)
        {
            float fixedDt = _networkTimer.FixedTickInterval;

            _character.UpdateInput(input.Command);
            _simulationBridge.SimulateMotor(_character.Motor, fixedDt);

            return _character.GetPredictedState(input.Tick);
        }

        private PlayerReconciliationStatePayload CreateReconciliationStatePayload(PlayerStatePayload state)
        {
            return PlayerReconciliationStatePayload.FromFullState(state);
        }

        private PlayerImpliedStatePayload CreateImpliedStatePayload(PlayerStatePayload state)
        {
            return PlayerImpliedStatePayload.FullToImplied(state);
        }

        public Transform GetCameraTransform()
        {
            return _camera.transform;
        }

        private PlayerInputCommand GetRequestedMovement()
        {
            return new PlayerInputCommand()
            {
                Movement = _inputController.MoveInput,
                Rotation = _camera.BodyRotation,
                Jump = _inputController.TryToJump,
                Crouch = _inputController.ConsumeCrouchInput(),
            };
        }

        protected virtual void EnableLocalItems()
        {
            if (_inputController != null)
                _inputController.enabled = true;

            if (_camera != null)
            {
                _camera.enabled = true;
                _camera.Initialize(_character.GetCameraTarget());
            }

            SetLayerInChildren("Self");


            if (_networkSuccess && GameNetworkManager.HasInstance && GameNetworkManager.Instance.LocalPlayer == null)
            {
                GameNetworkManager.Instance.RegisterLocalPlayer(this);
            }
        }

        protected virtual void DisableLocalItems()
        {
            if (_inputController != null)
                _inputController.enabled = false;

            if (_camera != null)
                _camera.enabled = false;

            if (TryGetComponent(out Interactor interactor))
            {
                interactor.enabled = false;
            }

            SetLayerInChildren("Default");
        }

        private void SetLayerInChildren(string layerName)
        {
            if (_playerRoot == null)
                return;

            int layer = LayerMask.NameToLayer(layerName);

            if (layer == -1)
                return;

            foreach (Transform child in _playerRoot.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private void CreateNetworkTimer()
        {
            if (_networkTimer != null)
                return;

            if (NetworkManager.Singleton == null)
                return;

            _networkTimer = new NetworkTimer(NetworkManager.Singleton);
            _networkTimer.OnTick += HandleServerTick;
            _networkTimer.OnTick += HandleClientTick;
        }

        private void UnregisterTimer()
        {
            if (_networkTimer == null)
                return;

            _networkTimer.OnTick -= HandleServerTick;
            _networkTimer.OnTick -= HandleClientTick;
            _networkTimer = null;
        }
    }
}