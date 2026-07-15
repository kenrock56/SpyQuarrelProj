using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        public float RotationYAxis => _rotationEuler.y;

        private Vector3 _rotationEuler;

        [field:SerializeField] private IAnimatorContext _animatorContext;
        
        [field: SerializeField] public NpcType NpcIdentityType { get; private set; }

        private GameObject _root;
        private GameObject _currentIdentity;

        [SerializeField] private Transform _followPositionTransform;
        [SerializeField] private Transform _followRotationTransform;
        
        
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Avatar _avatar;
        [SerializeField] private Animator _animator;
        [SerializeField]private NetworkAnimator _animatorNetwork;

        [Header("Rotation Smoothing")]
        [SerializeField] private float _rotationLerpSpeed = 15f;

        private bool _networkTickRegistered;

        private void Awake()
        {
            InitialiseIdentity();
            
        }

        private void Start()
        {
            BuildNpc();

            if (NetworkManager.Singleton == null)
                return;

            if (!NetworkManager.Singleton.IsListening)
                return;

            NetworkManager.Singleton.NetworkTickSystem.Tick += UpdatePosition;
            NetworkManager.Singleton.NetworkTickSystem.Tick += UpdateRotation;

            _networkTickRegistered = true;
        }

        private void InitialiseIdentity()
        {
            _animatorContext = transform.root.GetComponentInChildren<IAnimatorContext>();

            if (_animatorContext == null)
            {
                Debug.LogError("No animator context found on the root object");
            }
            else
            {
                Debug.Log("Animator context found on the root object");
            }
            
            if (_root != null)
                return;

            _root = gameObject;
        }

        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
            UpdateAnimator();
        }

        

        private void UpdatePosition()
        {
            if (_followPositionTransform == null)
                return;

            if (_currentIdentity == null)
                return;

            transform.position = _followPositionTransform.position;
        }

        private void UpdateRotation()
        {
            if (_followRotationTransform == null)
                return;

            if (_currentIdentity == null)
                return;

            Quaternion targetRotation =
                _followRotationTransform.rotation;

            if (_rotationLerpSpeed <= 0f)
            {
                transform.rotation = targetRotation;
                _rotationEuler = transform.eulerAngles;
                return;
            }

            float t = 1f - Mathf.Exp
            (
                -_rotationLerpSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp
            (
                transform.rotation,
                targetRotation,
                t
            );

            _rotationEuler = transform.eulerAngles;
        }
        
        private void UpdateAnimator()
        {
            if (_animator == null)return;
            if(_animatorContext == null)return;
            
            _animator.SetFloat(Speed, _animatorContext.Speed);
        }

        private void BuildNpc()
        {
            if (!NpcDictionary.HasInstance)
                return;

            if (!NpcDictionary.Entries.TryGetValue(
                    NpcIdentityType,
                    out GameObject identity))
            {
                return;
            }

            if (identity == null)
                return;

            if (_currentIdentity != null)
                Destroy(_currentIdentity);

            GameObject disguise = Instantiate(identity, _root.transform);

            disguise.transform.localPosition = Vector3.zero;
            disguise.transform.localRotation = Quaternion.identity;

            _currentIdentity = disguise;

            if (_currentIdentity.TryGetComponent(out _animator))
            {
                _animator.runtimeAnimatorController = _animatorController;
                _animator.avatar = _avatar;
            }
            
            if (!_currentIdentity.TryGetComponent(out NetworkAnimator networkAnimator))
            {
                _animatorNetwork = _currentIdentity.AddComponent<NetworkAnimator>();
            }
            else
            {
                _animatorNetwork = networkAnimator;
            }

            if (_animatorNetwork != null && _animator != null)
            {
                _animatorNetwork.Animator = _animator;
            }
        }

        public void SetAppearance(NpcType npcIdentityType)
        {
            NpcIdentityType = npcIdentityType;
            BuildNpc();
        }

        private void OnDisable()
        {
            UnregisterNetworkTick();
        }

        private void OnDestroy()
        {
            UnregisterNetworkTick();
        }

        private void UnregisterNetworkTick()
        {
            if (!_networkTickRegistered)
                return;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.NetworkTickSystem.Tick -=
                    UpdatePosition;

                NetworkManager.Singleton.NetworkTickSystem.Tick -=
                    UpdateRotation;
            }

            _networkTickRegistered = false;
        }
    }
}