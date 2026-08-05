using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpyQuarrelRuntime
{
    public class SpyCharacter : Player
    {
        public NPCIdenitityProvider NpcIdentityProvider => _provider;
        
        [Header("Disguise Refs")] 
        [SerializeField] private NPCIdenitityProvider _provider;
        [SerializeField] private PlayerDisguise _playerDisguise;

        [Header("Network Orientation")]
        [SerializeField] private float _minimumRotationDifference = 0.25f;
        [SerializeField] private float _maximumUpdatesPerSecond = 20f;

        [SerializeField]private InteractUI _interactUI;
        
        private float _lastSubmittedRotation;
        private float _nextRotationUpdateTime;

        private readonly NetworkVariable<float> _networkRotationY = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<NpcType> _networkAppearance = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        
        public override void OnNetworkSpawn()
        {
            InitComponents();
            
            base.OnNetworkSpawn();
            
            _networkRotationY.OnValueChanged += OnRotationUpdate;

            _networkAppearance.OnValueChanged += OnAppearanceChange;

            if (_provider == null)
            {
                Debug.LogError("[SpyCharacter] NPCIdentityProvider is missing.", this);
            }

            if (_playerDisguise == null)
            {
                Debug.LogError("[SpyCharacter] PlayerDisguise reference is missing.", this);
                return;
            }

            if (IsOwner)
            {
                if (_provider != null)
                {
                    _networkAppearance.Value = _provider.NpcIdentityType;

                    ApplyAppearance(_networkAppearance.Value);
                    _provider.UpdateLocal();
                }

                float currentRotation = _playerDisguise.RotationYAxis;

                _lastSubmittedRotation = currentRotation;

                _networkRotationY.Value = Mathf.Repeat(currentRotation, 360f);
            }
            else
            {
                if (_provider != null)
                {
                    ApplyAppearance(_networkAppearance.Value);
                    _provider.UpdateRemote();
                }

                _playerDisguise.SetRotationImmediate(_networkRotationY.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkRotationY.OnValueChanged -= OnRotationUpdate;

            _networkAppearance.OnValueChanged -= OnAppearanceChange;

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
                //_provider?.UpdateRemote();
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

        private void InitComponents()
        {
            if (_playerDisguise == null)
            {
                _playerDisguise = GetComponentInChildren<PlayerDisguise>(true);
            }

            if (_provider == null)
            {
                _provider = GetComponentInChildren<NPCIdenitityProvider>(true);
            }

            if (_interactUI == null)
            {
                _interactUI = GetComponentInChildren<InteractUI>(true);
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

        protected override void EnableLocalItems()
        {
            base.EnableLocalItems();
        }

        protected override void DisableLocalItems()
        {
            base.DisableLocalItems();
            
            _interactUI.gameObject.SetActive(false);
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

            if (!IsOwner)
                return;

            bool valueChanged = !_networkAppearance.Value.Equals(identity);

            _networkAppearance.Value = identity;

            if (!valueChanged)
            {
                ApplyAppearance(identity);
            }
        }

        private void OnAppearanceChange(NpcType previousIdentity, NpcType newIdentity)
        {
            ApplyAppearance(newIdentity);
        }

        private void ApplyAppearance(NpcType identity)
        {
            if (_provider == null) return;

            _provider.SetAppearance(identity);
        }

        private void UpdateOwnerOrientation()
        {
            if (Time.unscaledTime < _nextRotationUpdateTime) return;

            float currentRotation = _playerDisguise.RotationYAxis;


            //wrap around 360 angle
            var delta = Mathf.DeltaAngle(_lastSubmittedRotation, currentRotation);
            
            float difference = Mathf.Abs(delta);

            if (difference < _minimumRotationDifference) return;

            _lastSubmittedRotation = currentRotation;

            float updateInterval = 1f / Mathf.Max(1f, _maximumUpdatesPerSecond);

            _nextRotationUpdateTime = Time.unscaledTime + updateInterval;

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
            yRot = yRot % 360;

            if (!IsSpawned)
            {
                UpdateProviderRot(yRot);
                return;
            }

            if (!IsOwner)
                return;

            _networkRotationY.Value = yRot;
        }

        private void OnRotationUpdate(float previousValue, float newValue)
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