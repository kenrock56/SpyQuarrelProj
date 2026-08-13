using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SpyQuarrelRuntime
{
    public class SniperWeapon : MonoBehaviour
    {
       // [SerializeField] private UIDocument _scopeDocument;
        
        [SerializeField] private List<Vector3> _cachedPositions = new List<Vector3>();

        [SerializeField] private bool _aiming = false;
        [SerializeField] private float _aimLerpDuration = 0.15f;

        [SerializeField] private Vector3 _startPos;
        [SerializeField] private Vector3 _aimPos;
        [SerializeField] private Transform _weaponFollow;
        
        private PlayerInputController _inputController;
        private CancellationTokenSource _aimCancellation;

        [SerializeField]private DualRenderScope _renderScope;
        
        private Progress<float> _startAimProgress;
        private Progress<float> _emdAimProgress;
        
        private void Awake()
        {
            _startAimProgress = new Progress<float>();
            _emdAimProgress = new Progress<float>();

            _startAimProgress.ProgressChanged += HandleStartAim;
            _emdAimProgress.ProgressChanged += HandleEndAim;
            
            var controller = transform.root.GetComponentInChildren<PlayerInputController>();

            if (controller != null)
            {
                _inputController = controller;
                Debug.Log("Found PlayerInputController");
            }
            else
            {
                Debug.LogError("Could not find PlayerInputController");
            }
        }

        private void HandleStartAim(object sender, float value)
        {
            if (value >= 0.1f && _renderScope)
            {
                _renderScope.RenderScope.ShowScope();
            }
        }
        
        private void HandleEndAim(object sender, float value)
        {
            if (value >= 0.5f && _renderScope)
            {
                _renderScope.RenderScope.HideScope();
            }
        }

        private void Start()
        {
            if (_inputController != null)
            {
                SetupCallbacks();
            }
            
            SetAim(false, true);
        }

        private void OnDestroy()
        {
            if (_inputController != null)
            {
                _inputController.PlayerInputActions.Player.ADS.performed -= CheckAim;
            }

            _aimCancellation?.Cancel();
            _aimCancellation?.Dispose();
        }

        private void SetupCallbacks()
        {
            if (_inputController == null) return;

            _inputController.PlayerInputActions.Player.ADS.performed += CheckAim;
        }

        private void CheckAim(InputAction.CallbackContext context)
        {
            ToggleAim();
        }

        private void ToggleAim()
        {
            _aiming = !_aiming;
            SetAim(_aiming);
        }

        private void SetAim(bool aim, bool instant = false)
        {
            _aiming = aim;

            //_scopeDocument.enabled = aim;

            Vector3 targetPosition = aim ? _aimPos : _startPos;
            Progress<float> progress = aim ? _startAimProgress : _emdAimProgress;
            
            _aimCancellation?.Cancel();
            _aimCancellation?.Dispose();

            if (instant)
            {
                _weaponFollow.localPosition = targetPosition;
                return;
            }

            _aimCancellation = new CancellationTokenSource();
            _ = LerpPos(targetPosition, _aimLerpDuration, _aimCancellation.Token, progress);
        }

        [ContextMenu("Save Aim")]
        public void SavePosition()
        {
            if (_cachedPositions == null) return;

            _cachedPositions.Add(_weaponFollow.localPosition);
        }

        private async Task LerpPos(Vector3 end, float duration, CancellationToken token, IProgress<float> progress = default)
        {
            Vector3 startPos = _weaponFollow.localPosition;
            float timeElapsed = 0f;

            while (timeElapsed < duration)
            {
                if (token.IsCancellationRequested)
                    return;

                timeElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(timeElapsed / duration);

                _weaponFollow.localPosition = Vector3.Lerp(startPos, end, t);

                Debug.Log("Progress " + t);
                
                if (progress != null)
                {
                    progress.Report(t);
                }
                
                await Awaitable.EndOfFrameAsync(token);
            }

            if (!token.IsCancellationRequested)
            {
                _weaponFollow.localPosition = end;
            }
        }
    }
}