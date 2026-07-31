using System.Collections.Generic;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NPCIdenitityProvider : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int _moveHash = Animator.StringToHash("Move");
        private static readonly int _singHash = Animator.StringToHash("StandingSing");
        private static readonly int _standToSit = Animator.StringToHash("StandToSit");
        private static readonly int _sitting = Animator.StringToHash("Sitting");
        private static readonly int _sitToStand = Animator.StringToHash("SitToStand");

        
        private Dictionary<NpcAnimState, int> _animStateDictionary = new Dictionary<NpcAnimState, int>();
        
        [Header("Identity")]
        [field: SerializeField]
        public NpcType NpcIdentityType { get; private set; }

        [Header("Permanent References")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;
        [SerializeField] private LODGroup _lodGroup;

        [Header("Pose Handling")]
        private HumanPoseHandler _poseHandler;
        private HumanPose _pose;
        [SerializeField] private bool _validPose;

        [Header("Follow References")]
        [SerializeField] private Transform _followPositionTransform;
        [SerializeField] private Transform _followRotationTransform;

        [Header("Source Prefab")]
        [Tooltip(
            "Optional child name used by identity prefabs. " +
            "If not found, the prefab root itself is used."
        )]
        [SerializeField] private string _sourceVisualRootName = "VisualRoot";

        [Header("Rotation Smoothing")]
        [SerializeField] private float _rotationLerpSpeed = 15f;

        [Header("Debug")]
        [SerializeField] private bool _logAppearanceChanges;

        private IAnimatorContext _animatorContext;

        private Vector3 _rotationEuler;

        private bool _initialized;
        private bool _hasAppearance;
        private int _rebindVersion;

        public float RotationYAxis => _rotationEuler.y;

        public Animator CurrentAnimator => _animator;

        public Transform VisualRoot => _visualRoot;

        private void Awake()
        {
            BuildDictionary();
            InitialiseIdentity();
            InitialiseAnimator();
            BuildNpc();
        }

        private void Start()
        {
            if (!_hasAppearance)
            {
                BuildNpc();
                SetMoveAnim();
            }
        }

        private void InitialiseIdentity()
        {
            FindMissingReferences();

            _animatorContext =
                transform.root.GetComponentInChildren<IAnimatorContext>(true);

            if (_animatorContext == null)
            {
                Debug.LogError("[NPCIdentityProvider] No animator context found.", this);
            }
        }

        private void BuildDictionary()
        {
            _animStateDictionary = new Dictionary<NpcAnimState, int>();

            _animStateDictionary.TryAdd(NpcAnimState.Move, _moveHash);
            _animStateDictionary.TryAdd(NpcAnimState.Sing, _singHash);
            _animStateDictionary.TryAdd(NpcAnimState.StandToSit, _standToSit);
            _animStateDictionary.TryAdd(NpcAnimState.Sit, _sitting);
            _animStateDictionary.TryAdd(NpcAnimState.SitToStand, _sitToStand);
        }
        private void InitialiseAnimator()
        {
            if (_animator == null)
                return;

            _animator.keepAnimatorStateOnDisable = true;

            CreatePoseHandler(true);
        }

        private void FindMissingReferences()
        {
            if (_visualRoot == null)
            {
                Transform foundVisualRoot = transform.Find("VisualRoot");

                if (foundVisualRoot != null)
                {
                    _visualRoot = foundVisualRoot;
                }
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_lodGroup == null)
            {
                _lodGroup = GetComponent<LODGroup>();
            }
        }

        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
        }

        /// <summary>
        /// Called by the locally authoritative NetworkBehaviour.
        /// This peer writes Animator parameters.
        /// </summary>
        public void UpdateLocal()
        {
            UpdateAnimator();
        }

        /// <summary>
        /// Called by remote NetworkBehaviour instances.
        /// NetworkAnimator controls the Animator on these peers.
        /// </summary>
        public void UpdateRemote()
        {
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

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                t
            );

            _rotationEuler = transform.eulerAngles;
        }

        private void UpdateAnimator()
        {
            if (_animator == null || _animatorContext == null)
                return;

            _animator.SetFloat(Speed, _animatorContext.Speed);

            //SetAnimation(_moveHash);
        }

        public void SetAnimation(NpcAnimState state)
        {
            if(_animStateDictionary == null)return;
            if (_animStateDictionary.TryGetValue(state, out var animHash))
            {
                SetAnimation(animHash);
            }
            
        }
        
        public void SetAnimation(int hash)
        {
            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.fullPathHash == hash)return;
            else
            {
                _animator.CrossFade(hash, 0.02f, 0);
            }
        }

        public void SetMoveAnim()
        {
            SetAnimation(_moveHash);
        }
        
        

        [ContextMenu("Build Npc")]
        public void BuildNpc()
        {
            FindMissingReferences();

            if (_visualRoot == null)
            {
                Debug.LogError(
                    "[NPCIdentityProvider] VisualRoot reference is missing.",
                    this
                );

                return;
            }

            if (_animator == null)
            {
                Debug.LogError(
                    "[NPCIdentityProvider] Permanent Animator is missing.",
                    this
                );

                return;
            }

            if (!NpcDictionary.HasInstance)
            {
                Debug.LogWarning(
                    "[NPCIdentityProvider] NpcDictionary is not available.",
                    this
                );

                return;
            }

            if (!NpcDictionary.Entries.TryGetValue(
                    NpcIdentityType,
                    out GameObject identityPrefab))
            {
                Debug.LogError(
                    $"[NPCIdentityProvider] Missing identity " +
                    $"'{NpcIdentityType}'.",
                    this
                );

                return;
            }

            if (identityPrefab == null)
            {
                Debug.LogError(
                    $"[NPCIdentityProvider] Identity prefab for " +
                    $"'{NpcIdentityType}' is null.",
                    this
                );

                return;
            }

            CacheCurrentPose();

            int refreshVersion = ++_rebindVersion;

            _animator.keepAnimatorStateOnDisable = true;
            _animator.enabled = false;

            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(false);
            }

            GameObject temporaryIdentity =
                Instantiate(identityPrefab);

            temporaryIdentity.name =
                $"{identityPrefab.name}_SwapSource";

            Transform sourceVisualRoot =
                FindSourceVisualRoot(temporaryIdentity.transform);

            if (sourceVisualRoot == null)
            {
                Debug.LogError(
                    $"[NPCIdentityProvider] Could not find visual content " +
                    $"inside '{identityPrefab.name}'.",
                    temporaryIdentity
                );

                Destroy(temporaryIdentity);

                _animator.enabled = true;

                if (_visualRoot != null)
                {
                    _visualRoot.gameObject.SetActive(true);
                }

                return;
            }

            LODGroup sourceLodGroup =
                temporaryIdentity.GetComponent<LODGroup>();

            LOD[] sourceLods = null;

            if (sourceLodGroup != null)
            {
                sourceLods = sourceLodGroup.GetLODs();
            }

            ClearVisualRoot();
            TransferVisualChildren(sourceVisualRoot);

            if (sourceLodGroup != null && _lodGroup != null)
            {
                CopyLodGroup(sourceLodGroup, sourceLods);
            }
            else if (_lodGroup != null)
            {
                _lodGroup.RecalculateBounds();
            }

            Destroy(temporaryIdentity);

            RebindAnimator(refreshVersion);

            _hasAppearance = true;
            _initialized = true;

            if (_logAppearanceChanges)
            {
                Debug.Log(
                    $"[NPCIdentityProvider] Applied appearance " +
                    $"'{NpcIdentityType}'.",
                    this
                );
            }
        }

        private Transform FindSourceVisualRoot(
            Transform temporaryIdentityRoot)
        {
            if (temporaryIdentityRoot == null)
                return null;

            if (!string.IsNullOrWhiteSpace(_sourceVisualRootName))
            {
                Transform namedVisualRoot =
                    temporaryIdentityRoot.Find(_sourceVisualRootName);

                if (namedVisualRoot != null)
                {
                    return namedVisualRoot;
                }
            }

            return temporaryIdentityRoot;
        }

        private void ClearVisualRoot()
        {
            for (int i = _visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _visualRoot.GetChild(i);

                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void TransferVisualChildren(Transform sourceVisualRoot)
        {
            int childCount = sourceVisualRoot.childCount;

            Transform[] children = new Transform[childCount];

            for (int i = 0; i < childCount; i++)
            {
                children[i] = sourceVisualRoot.GetChild(i);
            }

            foreach (Transform child in children)
            {
                if (child == null)
                    continue;

                child.SetParent(_visualRoot, false);
                child.gameObject.SetActive(true);
            }
        }

        private void CopyLodGroup(
            LODGroup source,
            LOD[] sourceLods)
        {
            if (_lodGroup == null || source == null)
                return;

            _lodGroup.enabled = source.enabled;
            _lodGroup.fadeMode = source.fadeMode;
            _lodGroup.animateCrossFading =
                source.animateCrossFading;

            _lodGroup.localReferencePoint =
                source.localReferencePoint;

            _lodGroup.size = source.size;

            if (sourceLods != null && sourceLods.Length > 0)
            {
                _lodGroup.SetLODs(sourceLods);
            }

            _lodGroup.RecalculateBounds();
        }

        private bool CanUseHumanPose()
        {
            if (_animator == null)
                return false;

            if (_animator.avatar == null)
                return false;

            if (!_animator.avatar.isValid)
                return false;

            if (!_animator.avatar.isHuman)
                return false;

            return true;
        }

        private void CreatePoseHandler(bool forceRecreate)
        {
            if (!CanUseHumanPose())
            {
                _poseHandler = null;
                return;
            }

            if (_poseHandler != null && !forceRecreate)
                return;

            _poseHandler =
                new HumanPoseHandler(
                    _animator.avatar,
                    _animator.transform
                );
        }

        private void CacheCurrentPose()
        {
            _validPose = false;

            if (!_hasAppearance)
                return;

            if (!CanUseHumanPose())
                return;

            CreatePoseHandler(false);

            if (_poseHandler == null)
                return;

            _poseHandler.GetHumanPose(ref _pose);
            _validPose = true;
        }

        private async void RebindAnimator(int refreshVersion)
        {
            await Awaitable.NextFrameAsync();

            if (this == null)
                return;

            if (refreshVersion != _rebindVersion)
                return;

            if (_animator == null)
                return;

            _animator.enabled = true;
            _animator.Rebind();
            _animator.Update(0f);

            CreatePoseHandler(true);

            if (_validPose && _poseHandler != null)
            {
                _poseHandler.SetHumanPose(ref _pose);
                _animator.Update(0f);
            }

            if (_lodGroup != null)
            {
                _lodGroup.RecalculateBounds();
            }

            if (_visualRoot != null)
            {
                _visualRoot.gameObject.SetActive(true);
            }

            _animator.Update(0f);
        }

        public void SetAppearance(NpcType npcIdentityType)
        {
            if (NpcIdentityType == npcIdentityType &&
                _hasAppearance)
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