using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PlayerDisguise : MonoBehaviour
    {
        [SerializeField] private Transform _directionReference;

        private PlayerInputController _playerInputController;
        private Vector2 _inputVector;

        private void Awake()
        {
            _playerInputController = transform.root.GetComponentInChildren<PlayerInputController>();

            if (_directionReference == null)
            {
                _directionReference = transform.root;
            }
        }

        private void Update()
        {
            if (_playerInputController == null) return;

            _inputVector = _playerInputController.MoveInput;

            UpdateRotation();
        }

        private void UpdateRotation()
        {
            transform.rotation = GetInputBasedDirection(transform.rotation);
        }

        private Quaternion GetInputBasedDirection(Quaternion currentRotation)
        {
            Vector2 move = _inputVector;

            if (move.sqrMagnitude <= 0.001f)
            {
                return currentRotation;
            }

            Vector3 up = Vector3.up;

            Vector3 forward = Vector3.ProjectOnPlane(_directionReference.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 moveDirection = forward * move.y + right * move.x;

            if (moveDirection.sqrMagnitude <= 0.001f)
            {
                return currentRotation;
            }

            moveDirection = Vector3.ProjectOnPlane(moveDirection, up).normalized;

            return Quaternion.LookRotation(moveDirection, up);
        }
    }
}