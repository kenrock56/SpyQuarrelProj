using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
        [field: SerializeField] public NpcType NpcIdentityType { get; private set; }

        private GameObject _root;
        private GameObject _currentIdentity;

        
        [SerializeField] private Transform _followPositionTransform;

        [SerializeField] private Transform _followRotationTransform;

        [SerializeField] private Animator _animator;

        [Header("Rotation Smoothing")]
        [SerializeField] private float _rotationLerpSpeed = 15f;
        
        
        private void Awake()
        {
            InitialiseIdentity();
        }

        private void Start()
        {
            BuildNpc();
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.NetworkTickSystem.Tick += UpdatePosition;
                NetworkManager.Singleton.NetworkTickSystem.Tick += UpdateRotation;
            }
        }

        private void InitialiseIdentity()
        {
            if (_root != null) return;

            _root = transform.gameObject;
        }

        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
        }

        private void UpdatePosition()
        {
            if (_followPositionTransform == null) return;
            if (_currentIdentity == null) return;

            transform.position = _followPositionTransform.position;
        }

        private void UpdateRotation()
        {
            if (_followRotationTransform == null) return;
            if (_currentIdentity == null) return;
            
            Quaternion targetRotation = _followRotationTransform.rotation;

            if (_rotationLerpSpeed <= 0f)
            {
                transform.rotation = targetRotation;
                return;
            }
            
            float deltaTime = Time.deltaTime;

            float lerpAmount = 1f - Mathf.Exp(-_rotationLerpSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpAmount);
        }

        private void BuildNpc()
        {
            if (!NpcDictionary.HasInstance) return;

            if (NpcDictionary.Entries[NpcIdentityType] is { } identity)
            {
                if (_currentIdentity != null)
                {
                    Destroy(_currentIdentity);
                }

                GameObject disguise = Instantiate(identity, _root.transform);
                disguise.transform.localPosition = Vector3.zero;
                disguise.transform.localRotation = Quaternion.identity;

                _currentIdentity = disguise;
                _animator = _currentIdentity.GetComponent<Animator>();
            }
        }

        public void SetAppearance(NpcType npcIdentityType)
        {
            NpcIdentityType = npcIdentityType;
            BuildNpc();
        }

        private void OnDestroy()
        {
            NetworkManager.Singleton.NetworkTickSystem.Tick -= UpdatePosition;
            NetworkManager.Singleton.NetworkTickSystem.Tick -= UpdateRotation;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                // BuildNpc();
            }
        }
    }
}