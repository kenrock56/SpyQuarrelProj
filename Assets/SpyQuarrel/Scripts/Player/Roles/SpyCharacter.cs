using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private readonly NetworkVariable<NpcType> _networkAppearance = new
        (
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            FindMissingReferences();

            _networkRotationY.OnValueChanged +=
                OnNetworkRotationChanged;

            _networkAppearance.OnValueChanged +=
                OnNetworkAppearanceChanged;

            if (_provider == null)
            {
                Debug.LogError(
                    "[SpyCharacter] NPCIdentityProvider is missing.",
                    this
                );
            }

            if (_playerDisguise == null)
            {
                Debug.LogError(
                    "[SpyCharacter] PlayerDisguise reference is missing.",
                    this
                );

                return;
            }

            if (IsServer && _provider != null)
            {
                _networkAppearance.Value =
                    _provider.NpcIdentityType;
            }

            if (_provider != null)
            {
                ApplyAppearance(_networkAppearance.Value);
            }

            if (IsOwner)
            {
                float currentRotation =
                    _playerDisguise.RotationYAxis;

                _lastSubmittedRotation = currentRotation;

                if (IsServer)
                {
                    _networkRotationY.Value =
                        currentRotation;
                }
                else
                {
                    SendRotationRpc(currentRotation);
                }
            }
            else
            {
                _playerDisguise.SetRotationImmediate(
                    _networkRotationY.Value
                );
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkRotationY.OnValueChanged -=
                OnNetworkRotationChanged;

            _networkAppearance.OnValueChanged -=
                OnNetworkAppearanceChanged;

            base.OnNetworkDespawn();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!IsSpawned)
                return;

            if (IsOwner)
            {
                _provider?.UpdateLocal();
                UpdateSpyInput();
            }
            else
            {
                _provider?.UpdateRemote();
            }

            if (_playerDisguise == null)
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

        private void FindMissingReferences()
        {
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
        }

        private void UpdateSpyInput()
        {
            if (Keyboard.current == null)
                return;

            if (!Keyboard.current.qKey.wasPressedThisFrame)
                return;

            NpcType newType = GetRandomNpcType();
            SetAppearance(newType);
        }

        private static NpcType GetRandomNpcType()
        {
            int typeCount =
                Enum.GetValues(typeof(NpcType)).Length;

            int index =
                UnityEngine.Random.Range(0, typeCount);

            return (NpcType)index;
        }

        public void SetAppearance(NpcType identity)
        {
            if (_provider == null)
                return;

            if (!IsSpawned)
            {
                ApplyAppearance(identity);
                return;
            }

            if (IsServer)
            {
                SetAppearanceServer(identity);
                return;
            }

            if (IsOwner)
            {
                RequestSetAppearanceRpc(identity);
            }
        }

        private void SetAppearanceServer(NpcType identity)
        {
            if (!IsServer)
                return;

            bool valueChanged =
                !_networkAppearance.Value.Equals(identity);

            _networkAppearance.Value = identity;

            /*
             * Apply directly when the value is unchanged because
             * NetworkVariable callbacks only run when the value changes.
             */
            if (!valueChanged)
            {
                ApplyAppearance(identity);
                RebuildAppearanceRpc(identity);
            }
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Owner
        )]
        private void RequestSetAppearanceRpc(NpcType identity)
        {
            SetAppearanceServer(identity);
        }

        [Rpc(SendTo.NotServer)]
        private void RebuildAppearanceRpc(NpcType identity)
        {
            ApplyAppearance(identity);
        }

        private void OnNetworkAppearanceChanged(
            NpcType previousIdentity,
            NpcType newIdentity)
        {
            ApplyAppearance(newIdentity);
        }

        private void ApplyAppearance(NpcType identity)
        {
            if (_provider == null)
                return;

            _provider.SetAppearance(identity);
        }

        private void UpdateOwnerOrientation()
        {
            if (Time.unscaledTime < _nextRotationUpdateTime)
                return;

            float currentRotation =
                _playerDisguise.RotationYAxis;

            float difference = Mathf.Abs(
                Mathf.DeltaAngle(
                    _lastSubmittedRotation,
                    currentRotation
                )
            );

            if (difference < _minimumRotationDifference)
                return;

            _lastSubmittedRotation = currentRotation;

            float updateInterval =
                1f / Mathf.Max(
                    1f,
                    _maximumUpdatesPerSecond
                );

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
            {
                SendRotationRpc(yRot);
            }
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Owner
        )]
        private void SendRotationRpc(float yRot)
        {
            if (!IsServer)
                return;

            _networkRotationY.Value =
                Mathf.Repeat(yRot, 360f);
        }

        private void OnNetworkRotationChanged(
            float previousValue,
            float newValue)
        {
            if (IsOwner)
                return;

            UpdateProviderRot(newValue);
        }

        public void OverrideOrientation(float yRot)
        {
            yRot = Mathf.Repeat(yRot, 360f);

            if (_playerDisguise != null)
            {
                _playerDisguise.SetRotationImmediate(yRot);
            }

            _lastSubmittedRotation = yRot;

            UpdateRotEuler(yRot);
        }
    }
}