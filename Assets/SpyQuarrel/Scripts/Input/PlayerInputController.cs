using UnityEngine;
using UnityEngine.InputSystem;

namespace SpyQuarrelRuntime 
{
    public class PlayerInputController : MonoBehaviour
    {
        public PlayerInputActions PlayerInputActions => _playerInputActions;
        private PlayerInputActions _playerInputActions;

        public Vector2 LookInput => _lookInput;
        public Vector2 MoveInput => _moveInput;
        public bool TryToJump => _tryToJump;
        public bool TryToCrouch => _tryToCrouch;

        public bool FirePressed => _playerInputActions.Player.Attack.WasPressedThisFrame();
        public bool FireHeld => _playerInputActions.Player.Attack.IsPressed();
        
        
        [SerializeField] private Vector2 _moveInput = Vector2.zero;
        [SerializeField] private Vector2 _lookInput = Vector2.zero;
        [SerializeField] private bool _tryToJump = false;
        [SerializeField] private bool _tryToCrouch = false;

        private void Awake()
        {
            _playerInputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        public CrouchType ConsumeCrouchInput()
        {
            if (!_tryToCrouch)
            {
                return CrouchType.None;
            }

            _tryToCrouch = false;
            return CrouchType.Toggle;
        }

        private void RegisterCallbacks()
        {
            _playerInputActions.Player.Move.performed += OnMoveStart;
            _playerInputActions.Player.Move.canceled += OnMoveEnd;

            _playerInputActions.Player.Look.performed += OnLookStart;
            _playerInputActions.Player.Look.canceled += OnLookEnd;

            _playerInputActions.Player.Jump.started += OnJumpStart;
            _playerInputActions.Player.Jump.canceled += OnJumpEnd;

            _playerInputActions.Player.Crouch.started += OnCrouchStart;

            _playerInputActions.Enable();
        }

        private void UnregisterCallbacks()
        {
            _playerInputActions.Disable();

            _playerInputActions.Player.Move.performed -= OnMoveStart;
            _playerInputActions.Player.Move.canceled -= OnMoveEnd;

            _playerInputActions.Player.Look.performed -= OnLookStart;
            _playerInputActions.Player.Look.canceled -= OnLookEnd;

            _playerInputActions.Player.Jump.started -= OnJumpStart;
            _playerInputActions.Player.Jump.canceled -= OnJumpEnd;

            _playerInputActions.Player.Crouch.started -= OnCrouchStart;
        }

        private void OnMoveStart(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveEnd(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void OnLookStart(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void OnLookEnd(InputAction.CallbackContext context)
        {
            _lookInput = Vector2.zero;
        }

        private void OnJumpStart(InputAction.CallbackContext context)
        {
            _tryToJump = true;
        }

        private void OnJumpEnd(InputAction.CallbackContext context)
        {
            _tryToJump = false;
        }

        private void OnCrouchStart(InputAction.CallbackContext context)
        {
            _tryToCrouch = true;
        }

        private void OnDestroy()
        {
            _playerInputActions.Dispose();
        }
    }
}