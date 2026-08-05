using System;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class Interactor : MonoBehaviour
    {
        [field: SerializeField] public PlayerRole PlayerRole { get; private set; }

        public Component Owner { get; private set; }
        
        public event Action<IInteractable> OnInteractableChanged;
        public event Action<IInteractable, float> OnHoldProgressChanged;

        public IInteractable CurrentInteractable => _currentInteractable;

        public float HoldProgress { get; private set; }

        public bool IsHoldingInteraction =>
            _holdInteractable != null &&
            InteractHeld &&
            !_holdInteractionCompleted;

        [SerializeField] private bool _canInteract = true;
        [SerializeField] private float _interactRange = 10f;
        [SerializeField] private PlayerInputController _inputController;

        public Vector3 InteractStartPos => _startPos;
        public Vector3 InteractEndPos => _endPos;

        private IInteractable _currentInteractable;
        private IInteractable _holdInteractable;

        private Vector3 _startPos;
        private Vector3 _endPos;

        private float _holdTimer;

        private bool _holdInteractionCompleted;

        private bool InteractPress =>
            _inputController &&
            _inputController.InteractPressed;

        private bool InteractHeld =>
            _inputController &&
            _inputController.InteractHeld;

        private Transform _cameraTransform;

        private void Awake()
        {
            Owner = transform.root;
            if (Owner.TryGetComponent(out Player player))
            {
                switch (player)
                {
                    case SpyCharacter:
                        PlayerRole = PlayerRole.Spy;
                        break;

                    case SniperCharacter:
                        PlayerRole = PlayerRole.Sniper;
                        break;

                    default:
                        PlayerRole = PlayerRole.None;
                        break;
                }
            }

            if (!_inputController)
            {
                _inputController = GetComponent<PlayerInputController>();
            }

            PlayerCamera playerCamera =
                Owner.GetComponentInChildren<PlayerCamera>();

            if (playerCamera)
            {
                _cameraTransform = playerCamera.transform;
            }
            else if (Camera.main)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (!_cameraTransform || !_inputController || !_canInteract)
            {
                _startPos = Vector3.zero;
                _endPos = Vector3.zero;

                ClearInteractable();
                return;
            }

            UpdateInteractable();

            if (_currentInteractable == null)
            {
                ResetHoldInteraction();
                return;
            }

            _currentInteractable.OnInteractHover(this);

            HandleInteraction();
        }

        private void UpdateInteractable()
        {
            _startPos = _cameraTransform.position;
            _endPos = _startPos + _cameraTransform.forward * _interactRange;

            IInteractable hitInteractable = null;

            if (Physics.Raycast(_startPos, _cameraTransform.forward, out RaycastHit interactHit, _interactRange))
            {
                _endPos = interactHit.point;

                interactHit.collider.TryGetComponent(
                    out hitInteractable);
            }

            if (hitInteractable == _currentInteractable)
            {
                return;
            }

            ResetHoldInteraction();

            _currentInteractable?.OnInteractExit(this);

            SetInteractable(hitInteractable);

            _currentInteractable?.OnInteractEnter(this);
        }

        private void HandleInteraction()
        {
            if (!_currentInteractable.IsInteractable)
            {
                ResetHoldInteraction();
                return;
            }

            if (!_currentInteractable.HoldInteraction)
            {
                ResetHoldInteraction();

                if (InteractPress)
                {
                    _currentInteractable.Interact(this);
                }

                return;
            }

            HandleHoldInteraction();
        }

        private void HandleHoldInteraction()
        {
            if (InteractPress)
            {
                BeginHoldInteraction(_currentInteractable);
            }

            if (!InteractHeld)
            {
                ResetHoldInteraction();
                return;
            }

            if (_holdInteractable != _currentInteractable || _holdInteractionCompleted)
            {
                return;
            }

            float holdDuration = Mathf.Max(0f, _holdInteractable.HoldInteractionTime);

            if (holdDuration <= 0f)
            {
                CompleteHoldInteraction();
                return;
            }

            _holdTimer += Time.deltaTime;

            HoldProgress = Mathf.Clamp01(_holdTimer / holdDuration);
            
            OnHoldProgressChanged?.Invoke(_holdInteractable, HoldProgress);

            Debug.Log($"Progress: {HoldProgress}%");
            
            if (_holdTimer >= holdDuration)
            {
                CompleteHoldInteraction();
            }
        }

        private void BeginHoldInteraction(IInteractable interactable)
        {
            ResetHoldInteraction();

            _holdInteractable = interactable;
            _holdTimer = 0f;
            HoldProgress = 0f;
            _holdInteractionCompleted = false;

            OnHoldProgressChanged?.Invoke(_holdInteractable, HoldProgress);
        }

        private void CompleteHoldInteraction()
        {
            if (_holdInteractable == null || _holdInteractionCompleted)
            {
                return;
            }

            _holdInteractionCompleted = true;
            HoldProgress = 1f;

            OnHoldProgressChanged?.Invoke(
                _holdInteractable,
                HoldProgress);

            _holdInteractable.Interact(this);
        }

        private void ResetHoldInteraction()
        {
            if (_holdInteractable == null &&
                HoldProgress <= 0f &&
                !_holdInteractionCompleted)
            {
                return;
            }

            IInteractable previousInteractable =
                _holdInteractable;

            _holdInteractable = null;
            _holdTimer = 0f;
            HoldProgress = 0f;
            _holdInteractionCompleted = false;

            if (previousInteractable != null)
            {
                OnHoldProgressChanged?.Invoke(
                    previousInteractable,
                    HoldProgress);
            }
        }

        private void ClearInteractable()
        {
            ResetHoldInteraction();

            if (_currentInteractable == null)
            {
                return;
            }

            _currentInteractable.OnInteractExit(this);
            SetInteractable(null);
        }

        private void SetInteractable(IInteractable interactable)
        {
            if (_currentInteractable == interactable)
            {
                return;
            }

            _currentInteractable = interactable;

            OnInteractableChanged?.Invoke(_currentInteractable);
        }

        private void OnDisable()
        {
            ClearInteractable();

            _startPos = Vector3.zero;
            _endPos = Vector3.zero;
        }
    }
}