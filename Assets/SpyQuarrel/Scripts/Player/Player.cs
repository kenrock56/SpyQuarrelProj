using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

#if UNITY_EDITOR
        //[SerializeField] private GameObject _clientCapsule;
        //[SerializeField] private GameObject _serverCapsule;
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            // _serverCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            // if (_serverCapsule.TryGetComponent(out CapsuleCollider col))
            //     col.enabled = false;
            //
            // _clientCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            // if (_clientCapsule.TryGetComponent(out CapsuleCollider col2))
            //     col2.enabled = false;
#endif

            if (_inputController == null)
                TryGetComponent(out _inputController);

            _simulationBridge = KinematicSimulationBridge.Instance;

            if (_character != null)
                _character.Initialize();

            _networkTimer = new NetworkTimer(NetworkManager.Singleton);

            _clientStateBuffer = new(_bufferSize);
            _clientInputBuffer = new(_bufferSize);

            _serverStateBuffer = new(_bufferSize);
            _serverInputQueue = new();

            _networkTimer.OnTick += HandleServerTick;
            _networkTimer.OnTick += HandleClientTick;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkSuccess = true;

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
        }

        private void Start()
        {
            if (_networkSuccess && !IsOwner)
            {
                DisableLocalItems();
                return;
            }

            CursorManager.SetCursor(false);
            EnableLocalItems();
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
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1) return;

            foreach (Transform child in _playerRoot.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _character.UpdateBody(deltaTime);

            if (_networkSuccess && !IsOwner)
                return;

            CameraInput cameraInput = new CameraInput(_inputController.LookInput);
            _camera.UpdateRotation(cameraInput);

            if (_inputController.FirePressed)
            {
                var forward = _camera.transform.forward;
                var ray = new Ray(_camera.transform.position, forward);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.point != Vector3.zero)
                        _character.Teleport(hit.point);
                }
            }
        }

       
        private void HandleServerTick()
        {
            if (!IsServer) return;

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

#if UNITY_EDITOR
                //_serverCapsule.transform.position = statePayload.Position;
#endif
                _serverStateBuffer[bufferIndex] = statePayload;

                if (!hadInput || input.Tick > lastState.Tick)
                {
                    lastState = statePayload;
                    hadInput = true;
                }
            }

            if (!hadInput) return;

            
            if (IsHost && IsOwner) return;

           
            SendToClientRpc(lastState);

          
            SendStateToObserversRpc(lastState);
        }

        
        private void HandleClientTick()
        {
            if (!IsClient || !IsOwner) return;

            var currentTick = _networkTimer.CurrentTick;
            var bufferIndex = currentTick % _bufferSize;

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

                //_serverStateBuffer[bufferIndex] = statePayload;

#if UNITY_EDITOR
                //_clientCapsule.transform.position = statePayload.Position;
#endif
                _clientStateBuffer[bufferIndex] = statePayload;

                // Update own _lastServerState
                SendToClientRpc(statePayload);

                // Sync host player position to all observers
                SendStateToObserversRpc(statePayload);
            }
            else
            {
                // Send input to server for authoritative simulation
                SendToServerRpc(input);

                // Predict locally
                PlayerStatePayload statePayload = ProcessMovement(input);

#if UNITY_EDITOR
                //_clientCapsule.transform.position = statePayload.Position;
#endif
                _clientStateBuffer[bufferIndex] = statePayload;

                HandleServerReconciliation();
            }
        }

 
        //client player only
        private void HandleServerReconciliation()
        {
            if (!ShouldReconcile()) return;

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

        private bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastUndefinedOrDifferent = _lastProcessedState.Equals(default)
                                           || !_lastProcessedState.Equals(_lastServerState);
            return isNewServerState && isLastUndefinedOrDifferent;
        }

    
        [Rpc(SendTo.Server)]
        private void SendToServerRpc(PlayerInputPayload input)
        {
            _serverInputQueue.Enqueue(input);
        }

       
        [Rpc(SendTo.Owner)]
        private void SendToClientRpc(PlayerStatePayload state)
        {
            if (!IsOwner) return;
            _lastServerState = state;
        }

       
        [Rpc(SendTo.NotOwner)]
        private void SendStateToObserversRpc(PlayerStatePayload state)
        {
            // Server already has state — skip
            if (IsServer) return;
            _character.SetPredictedState(state);
        }

       
        private PlayerStatePayload ProcessMovement(PlayerInputPayload input)
        {
            var fixeddt = _networkTimer.FixedTickInterval;
            _character.UpdateInput(input.Command);
            _simulationBridge.SimulateMotor(_character.Motor, fixeddt);
            return _character.GetPredictedState(input.Tick);
        }

        private PlayerInputCommand GetRequestedMovement()
        {
            return new PlayerInputCommand()
            {
                Movement = _inputController.MoveInput,
                Rotation = _camera.transform.rotation,
                Jump = _inputController.TryToJump,
                Crouch = _inputController.ConsumeCrouchInput(),
            };
        }
    }
}