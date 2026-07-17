using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
        private bool _initialized;

        private static readonly int Speed = Animator.StringToHash("Speed");

        public float RotationYAxis => _rotationEuler.y;

        private Vector3 _rotationEuler;

        [field: SerializeField]
        private IAnimatorContext _animatorContext;

        [field: SerializeField]
        public NpcType NpcIdentityType { get; private set; }

        public Animator CurrentAnimator => _animator;

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
            BuildNpc();
        }

        private void InitialiseIdentity()
        {
            _animatorContext =
                transform.root.GetComponentInChildren<IAnimatorContext>();

            if (_animatorContext == null)
            {
                Debug.LogError(
                    "[NPCIdentityProvider] No animator context found.",
                    this
                );
            }

            _root = gameObject;
        }

        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
        }

        public void UpdateLocal()
        {
            UpdateAnimator();
        }

        public void UpdateRemote()
        {
            
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

            float t =
                1f - Mathf.Exp(-_rotationLerpSpeed * Time.deltaTime);

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
            if (_animator == null)
                return;

            if (_animatorContext == null)
                return;

            _animator.SetFloat
            (
                Speed,
                _animatorContext.Speed
            );
        }

        [ContextMenu("Build Npc")]
        private void BuildNpc()
        {
            if (!NpcDictionary.HasInstance)
                return;

            if (!NpcDictionary.Entries.TryGetValue
            (
                NpcIdentityType,
                out GameObject identityPrefab
            ))
            {
                Debug.LogError
                (
                    $"[NPCIdentityProvider] Missing identity '{NpcIdentityType}'.",
                    this
                );

                return;
            }

            if (identityPrefab == null)
                return;

            if (_currentIdentity != null)
            {
                Destroy(_currentIdentity);
            }

            _currentIdentity =
                Instantiate(identityPrefab, _root.transform);

            _currentIdentity.transform.localPosition = Vector3.zero;
            _currentIdentity.transform.localRotation = Quaternion.identity;
            _currentIdentity.transform.localScale = Vector3.one;

            _animator =
                _currentIdentity.GetComponentInChildren<Animator>(true);

            if (_animator == null)
            {
                Debug.LogError
                (
                    $"[NPCIdentityProvider] '{identityPrefab.name}' has no Animator.",
                    _currentIdentity
                );

                return;
            }

            _initialized = true;
        }

        public void SetAppearance(NpcType npcIdentityType)
        {
            if (NpcIdentityType == npcIdentityType &&
                _currentIdentity != null)
            {
                return;
            }

            NpcIdentityType = npcIdentityType;
            BuildNpc();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (!_initialized)
                return;

            SetAppearance(NpcIdentityType);
        }
    }
}