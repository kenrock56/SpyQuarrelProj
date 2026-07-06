using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class Interactor : MonoBehaviour
    {
        [field:SerializeField]public PlayerRole PlayerRole { get; private set; }
        
        public IInteractable CurrentInteractable => _currentInteractable;
        private IInteractable _currentInteractable;
        
        [SerializeField]private bool _canInteract;
        [SerializeField] private float _interactRange;

        [SerializeField]private PlayerInputController _inputController;

        private bool interactHeld => _inputController && _inputController.InteractHeld;

        private Transform _cameraTransform;
        
        void Awake()
        {
            if (transform.root.TryGetComponent(out Player player))
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
            
            var playerCamera = transform.root.GetComponentInChildren<PlayerCamera>();

            if (!playerCamera)
                _cameraTransform = playerCamera.transform;

            if (_cameraTransform == null)
            {
                if (Camera.main != null)
                    _cameraTransform = Camera.main.transform;
            }
                
        }
        

        private void Update()
        {
            if (!_cameraTransform || !_inputController)
            {
                ClearInteractable();
                return;
            }

            IInteractable hitInteractable = null;

            if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit interactHit, _interactRange))
            {
                interactHit.collider.TryGetComponent(out hitInteractable);
            }

            if (hitInteractable != _currentInteractable)
            {
                _currentInteractable?.OnInteractExit(this);

                _currentInteractable = hitInteractable;

                _currentInteractable?.OnInteractEnter(this);
            }

            if (_currentInteractable == null)
            {
                return;
            }

            _currentInteractable.OnInteractHover(this);

            if (interactHeld)
            {
                _currentInteractable.Interact(this);
            }
        }

        private void ClearInteractable()
        {
            if (_currentInteractable == null)
            {
                return;
            }

            _currentInteractable.OnInteractExit(this);
            _currentInteractable = null;
        }
        

    }
}
