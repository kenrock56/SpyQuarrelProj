using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class SpyCharacter : Player
    {
        [Header("Disguise References")]
        [SerializeField] private NPCIdenitityProvider _provider;
        [SerializeField] private PlayerDisguise _playerDisguise;
        
        
        [Header("Network Orientation")]
        [SerializeField] private float _minimumRotationDifference = 0.25f;
        [SerializeField] private float _maximumUpdatesPerSecond = 20f;

        private float _lastSubmittedRotation;
        private float _nextRotationUpdateTime;

        private readonly NetworkVariable<float> _networkRotationY = new
        (
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (_playerDisguise == null)
            {
                _playerDisguise =
                    GetComponentInChildren<PlayerDisguise>(true);
            }

            if (_provider == null)
            {
                _provider =
                    GetComponentInChildren<NPCIdenitityProvider>(true);
            }

            _networkRotationY.OnValueChanged += OnNetworkRotationChanged;

            if (_playerDisguise == null)
            {
                Debug.LogError
                (
                    "[SpyCharacter] PlayerDisguise reference is missing.",
                    this
                );

                return;
            }

            if (IsOwner)
            {
                float currentRotation = _playerDisguise.RotationYAxis;

                _lastSubmittedRotation = currentRotation;

                if (IsServer)
                {
                    _networkRotationY.Value = currentRotation;
                }
                else
                {
                    SendRotationRpc(currentRotation);
                }
            }
            else
            {
                _playerDisguise.SetRotationImmediate
                (
                    _networkRotationY.Value
                );
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkRotationY.OnValueChanged -= OnNetworkRotationChanged;

            base.OnNetworkDespawn();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!IsSpawned || _playerDisguise == null)
                return;

            if (IsOwner)
            {
                UpdateOwnerOrientation();
            }
            else
            {
                UpdateRemoteOrientation();
            }
        }

        protected override void OnLateUpdate()
        {
            base.OnLateUpdate();
        }

        protected override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
        }

        private void UpdateOwnerOrientation()
        {
            if (Time.unscaledTime < _nextRotationUpdateTime)
                return;

            float currentRotation = _playerDisguise.RotationYAxis;

            float difference = Mathf.Abs
            (
                Mathf.DeltaAngle
                (
                    _lastSubmittedRotation,
                    currentRotation
                )
            );

            if (difference < _minimumRotationDifference)
                return;

            _lastSubmittedRotation = currentRotation;

            float updateInterval =
                1f / Mathf.Max(1f, _maximumUpdatesPerSecond);

            _nextRotationUpdateTime =
                Time.unscaledTime + updateInterval;

            UpdateRotEuler(currentRotation);
        }

        private void UpdateRemoteOrientation()
        {
            UpdateProviderRot(_networkRotationY.Value);
        }

        private void UpdateProviderRot(float yRot)
        {
            if (_playerDisguise == null)
                return;

            _playerDisguise.SetNetworkRotation(yRot);
        }

        private void UpdateRotEuler(float yRot)
        {
            yRot = Mathf.Repeat(yRot, 360f);

            if (!IsSpawned)
            {
                UpdateProviderRot(yRot);
                return;
            }

            if (IsServer)
            {
                _networkRotationY.Value = yRot;
                return;
            }

            if (IsOwner)
                SendRotationRpc(yRot);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SendRotationRpc(float yRot)
        {
            if (!IsServer)
                return;

            _networkRotationY.Value = Mathf.Repeat(yRot, 360f);
        }

        private void OnNetworkRotationChanged(float previousValue, float newValue)
        {
            if (IsOwner)
                return;

            UpdateProviderRot(newValue);
        }

        /// <summary>
        /// Allows another owner-side system to force a new disguise
        /// orientation without sending movement input.
        /// </summary>
        public void OverrideOrientation(float yRot)
        {
            yRot = Mathf.Repeat(yRot, 360f);

            if (_playerDisguise != null)
                _playerDisguise.SetRotationImmediate(yRot);

            _lastSubmittedRotation = yRot;

            UpdateRotEuler(yRot);
        }

        
    }
}