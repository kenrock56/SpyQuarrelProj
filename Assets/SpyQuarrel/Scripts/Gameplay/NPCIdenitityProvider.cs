using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Identity")]
        [field: SerializeField]
        public NpcType NpcIdentityType { get; private set; }

        [Header("Follow References")]
        [SerializeField] private Transform _followPositionTransform;
        [SerializeField] private Transform _followRotationTransform;

        [Header("Animator Setup")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Avatar _avatar;
        
        private IAnimatorContext _animatorContext;

        [Header("Rotation Smoothing")]
        [SerializeField] private float _rotationLerpSpeed = 15f;

        private GameObject _root;
        private GameObject _currentIdentity;
        private Animator _animator;

        private Vector3 _rotationEuler;
        private float _currentAnimatorSpeed;

        private bool _initialized;
        private bool _appearanceQueued;
        private NpcType _queuedAppearance;

        public float RotationYAxis => _rotationEuler.y;
        public Animator CurrentAnimator => _animator;
        public GameObject CurrentIdentity => _currentIdentity;
        public bool IsInitialized => _initialized;

        private void Awake()
        {
            InitializeIdentity();
        }

        private void Start()
        {
            
        }

        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
            ProcessQueuedAppearanceChange();
            ApplyAnimatorSpeed();
        }

        private void InitializeIdentity()
        {
            _animatorContext = transform.root.GetComponentInChildren<IAnimatorContext>();
            
            if (_root != null)
                return;

            _root = gameObject;
        }

        private void UpdatePosition()
        {
            if (_followPositionTransform == null)
                return;

            transform.position = _followPositionTransform.position;
        }

        private void UpdateRotation()
        {
            if (_followRotationTransform == null)
                return;

            Quaternion targetRotation = _followRotationTransform.rotation;

            if (_rotationLerpSpeed <= 0f)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                float t =
                    1f - Mathf.Exp(-_rotationLerpSpeed * Time.deltaTime);

                transform.rotation = Quaternion.Slerp
                (
                    transform.rotation,
                    targetRotation,
                    t
                );
            }

            _rotationEuler = transform.eulerAngles;
        }

        /// <summary>
        /// Applies the networked or locally calculated speed to the
        /// currently active identity Animator.
        /// </summary>
        public void SetAnimatorSpeed(float speed)
        {
            _currentAnimatorSpeed = speed;
        }

        private void ApplyAnimatorSpeed()
        {
            if (_animator == null)
                return;
            
            if(_animatorContext == null)return;

            SetAnimatorSpeed(_animatorContext.Speed);

            _animator.SetFloat(SpeedHash, _currentAnimatorSpeed);
        }

        public void SetAppearance(NpcType npcIdentityType)
        {
            QueueAppearanceChange(npcIdentityType);
        }

        private void QueueAppearanceChange(NpcType npcIdentityType)
        {
            _queuedAppearance = npcIdentityType;
            _appearanceQueued = true;
        }

        private void ProcessQueuedAppearanceChange()
        {
            if (!_appearanceQueued)
                return;

            _appearanceQueued = false;
            NpcIdentityType = _queuedAppearance;

            BuildNpc();
        }

        [ContextMenu("Build Npc")]
        public void BuildNpc()
        {
            if (!NpcDictionary.HasInstance)
            {
                Debug.LogWarning
                (
                    "[NPCIdenitityProvider] NpcDictionary is not available.",
                    this
                );

                return;
            }

            if (!NpcDictionary.Entries.TryGetValue
                (
                    NpcIdentityType,
                    out GameObject identityPrefab
                ))
            {
                Debug.LogError
                (
                    $"[NPCIdenitityProvider] No identity found for {NpcIdentityType}.",
                    this
                );

                return;
            }

            if (identityPrefab == null)
            {
                Debug.LogError
                (
                    $"[NPCIdenitityProvider] Identity prefab for {NpcIdentityType} is null.",
                    this
                );

                return;
            }

            GameObject previousIdentity = _currentIdentity;

            GameObject newIdentity = Instantiate
            (
                identityPrefab,
                _root.transform
            );

            newIdentity.name = $"{identityPrefab.name}_Identity";

            Transform identityTransform = newIdentity.transform;

            identityTransform.localPosition = Vector3.zero;
            identityTransform.localRotation = Quaternion.identity;
            identityTransform.localScale = Vector3.one;

            Animator newAnimator =
                newIdentity.GetComponentInChildren<Animator>(true);

            if (newAnimator == null)
            {
                Debug.LogError
                (
                    $"[NPCIdenitityProvider] Identity '{identityPrefab.name}' has no Animator.",
                    newIdentity
                );

                Destroy(newIdentity);
                return;
            }

            ConfigureAnimator(newAnimator);

            _currentIdentity = newIdentity;
            _animator = newAnimator;
            _initialized = true;

            // Immediately apply the most recent replicated speed so the
            // replacement model starts in the correct animation state.
            _animator.SetFloat(SpeedHash, _currentAnimatorSpeed);

            if (previousIdentity != null)
            {
                Destroy(previousIdentity);
            }
        }

        private void ConfigureAnimator(Animator targetAnimator)
        {
            if (targetAnimator == null)
                return;

            targetAnimator.enabled = true;

            if (_animatorController != null)
            {
                targetAnimator.runtimeAnimatorController =
                    _animatorController;
            }

            if (_avatar != null)
            {
                targetAnimator.avatar = _avatar;
            }

            targetAnimator.Rebind();
            targetAnimator.Update(0f);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (!_initialized)
                return;

            QueueAppearanceChange(NpcIdentityType);
        }
    }
}