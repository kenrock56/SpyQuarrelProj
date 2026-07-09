using System.Collections.Generic;
using KinematicCharacterController;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpyQuarrelRuntime
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private PlayerInputController _inputController;
        [SerializeField] private PlayerCamera _camera;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private Transform _playerRoot;
        [SerializeField] private bool _networkSuccess = false;

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

        private void Awake()
        {
            if (_inputController == null)
                TryGetComponent(out _inputController);

            _simulationBridge = KinematicSimulationBridge.Instance;

            if (_character != null)
                _character.Initialize();

            _clientStateBuffer = new(_bufferSize);
            _clientInputBuffer = new(_bufferSize);
            _serverStateBuffer = new(_bufferSize);
            _serverInputQueue = new();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                _networkTimer = new NetworkTimer(NetworkManager.Singleton);
                _networkTimer.OnTick += HandleServerTick;
                _networkTimer.OnTick += HandleClientTick;
            }
            
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

            if (_networkTimer == null)
            {
                _networkTimer = new NetworkTimer(NetworkManager.Singleton);
                _networkTimer.OnTick += HandleServerTick;
                _networkTimer.OnTick += HandleClientTick;
            }

            if (_character != null && (IsServer || IsOwner))
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

                    CheatTeleport(hit.point);
                }
            }
        }

        private void FixedUpdate()
        {
            if (_networkSuccess)
                return;

            if (_character == null || _inputController == null || _camera == null)
                return;

            PlayerInputCommand command = GetRequestedMovement();

            _character.UpdateInput(command);

            if (!_networkSuccess)
                KinematicCharacterSystem.Settings.AutoSimulation = true;
        }

        private void HandleServerTick()
        {
            if (!IsServer)
                return;

            PlayerStatePayload lastState = default;
            bool hadInput = false;

            while (_serverInputQueue.Count > 0)
            {
                PlayerInputPayload input = _serverInputQueue.Dequeue();
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
            _character.SetPredictedState(rewindState);

            _clientStateBuffer[rewindState.Tick % _bufferSize] = rewindState;

            int tickToProcess = rewindState.Tick + 1;

            while (tickToProcess < _networkTimer.CurrentTick)
            {
                int bufferIndex = tickToProcess % _bufferSize;

                PlayerInputPayload inputPayload = _clientInputBuffer[bufferIndex];
                PlayerStatePayload stateToProcess = ProcessMovement(inputPayload);

                _clientStateBuffer[bufferIndex] = stateToProcess;

                tickToProcess++;
            }
        }

        private void CheatTeleport(Vector3 position)
        {
            _character.Teleport(position);
        }
        
        public void Teleport(Vector3 position)
        {
            if (_character == null)
                return;

            if (!_networkSuccess)
            {
                ApplyTeleportState(position, 0, true, true);
                return;
            }

            if (!IsOwner)
                return;

            int tick = _networkTimer.CurrentTick;

            ApplyTeleportState(position, tick, true, IsServer);

            if (IsServer)
                BroadcastTeleportState(tick);
            else
                RequestTeleportRpc(position, tick);
        }

        [Rpc(SendTo.Server)]
        public void RequestTeleportRpc(Vector3 position, int clientTick)
        {
            if (!IsServer)
                return;

            ApplyTeleportState(position, clientTick, false, true);
            BroadcastTeleportState(clientTick);
        }

        private void BroadcastTeleportState(int tick)
        {
            int bufferIndex = tick % _bufferSize;
            PlayerStatePayload state = _serverStateBuffer[bufferIndex];

            SendToClientRpc(CreateReconciliationStatePayload(state));
            SendStateToObserversRpc(CreateImpliedStatePayload(state));
        }

        private void ApplyTeleportState(Vector3 position, int tick, bool writeClientBuffer, bool writeServerBuffer)
        {
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
            _serverInputQueue.Enqueue(input);
        }

        [Rpc(SendTo.Owner)]
        private void SendToClientRpc(PlayerReconciliationStatePayload state)
        {
            if (!IsOwner)
                return;

            PlayerStatePayload fullState = state.ReconcileToFull();

            _lastServerState = fullState;

            int bufferIndex = fullState.Tick % _bufferSize;
            _clientStateBuffer[bufferIndex] = fullState;
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

        private void EnableLocalItems()
        {
            if (_inputController != null)
                _inputController.enabled = true;

            if (_camera != null)
            {
                _camera.enabled = true;
                _camera.Initialize(_character.GetCameraTarget());
            }

            SetLayerInChildren("Self");
        }

        private void DisableLocalItems()
        {
            if (_inputController != null)
                _inputController.enabled = false;

            if (_camera != null)
                _camera.enabled = false;

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

        private void UnregisterTimer()
        {
            if (_networkTimer == null)
                return;

            _networkTimer.OnTick -= HandleServerTick;
            _networkTimer.OnTick -= HandleClientTick;
        }
    }
}