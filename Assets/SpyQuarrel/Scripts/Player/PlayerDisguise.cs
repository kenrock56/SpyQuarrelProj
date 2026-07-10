using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PlayerDisguise : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _directionReference;

        [Header("Rotation")]
        [SerializeField] private float _remoteRotationLerpSpeed = 15f;

        private PlayerInputController _playerInputController;
        private Vector2 _inputVector;

        private bool _hasNetworkTarget;
        private float _networkTargetYaw;

        public float RotationYAxis => transform.eulerAngles.y;

        private void Awake()
        {
            _playerInputController =
                transform.root.GetComponentInChildren<PlayerInputController>(true);

            if (_directionReference == null)
                _directionReference = transform.root;
        }

        private void Update()
        {
            if (_playerInputController != null &&
                _playerInputController.isActiveAndEnabled)
            {
                _hasNetworkTarget = false;

                _inputVector = _playerInputController.MoveInput;
                UpdateLocalRotation();

                return;
            }

            UpdateNetworkRotation();
        }
        

        private void UpdateLocalRotation()
        {
            if (_inputVector.magnitude > 0.001f)
            {
                transform.rotation = GetInputBasedDirection(transform.rotation);
            }
            
           
        }

        private void UpdateNetworkRotation()
        {
            if (!_hasNetworkTarget)
                return;

            Quaternion targetRotation = Quaternion.Euler
            (
                0f,
                _networkTargetYaw,
                0f
            );

            if (_remoteRotationLerpSpeed <= 0f)
            {
                transform.rotation = targetRotation;
                return;
            }

            float t = 1f - Mathf.Exp
            (
                -_remoteRotationLerpSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp
            (
                transform.rotation,
                targetRotation,
                t
            );
        }

        private Quaternion GetInputBasedDirection(Quaternion currentRotation)
        {
            Vector2 move = _inputVector;

            if (move.sqrMagnitude <= 0.001f)
                return currentRotation;

            Vector3 up = Vector3.up;

            Vector3 forward = Vector3.ProjectOnPlane
            (
                _directionReference.forward,
                up
            );

            if (forward.sqrMagnitude <= 0.001f)
                return currentRotation;

            forward.Normalize();

            Vector3 right = Vector3.Cross(up, forward).normalized;

            Vector3 moveDirection =
                forward * move.y +
                right * move.x;

            moveDirection = Vector3.ProjectOnPlane
            (
                moveDirection,
                up
            );

            if (moveDirection.sqrMagnitude <= 0.001f)
                return currentRotation;

            return Quaternion.LookRotation
            (
                moveDirection.normalized,
                up
            );
        }

        /// <summary>
        /// Called by SpyCharacter on remote player instances.
        /// </summary>
        public void SetNetworkRotation(float yRotation)
        {
            _networkTargetYaw = Mathf.Repeat(yRotation, 360f);
            _hasNetworkTarget = true;
        }

        /// <summary>
        /// Used when an immediate orientation override is required.
        /// </summary>
        public void SetRotationImmediate(float yRotation)
        {
            yRotation = Mathf.Repeat(yRotation, 360f);

            _networkTargetYaw = yRotation;
            _hasNetworkTarget = true;

            transform.rotation = Quaternion.Euler
            (
                0f,
                yRotation,
                0f
            );
        }

        /// <summary>
        /// Clears a remote orientation target and returns control to
        /// the local input-driven rotation when local input is active.
        /// </summary>
        public void ClearNetworkRotation()
        {
            _hasNetworkTarget = false;
        }
    }
}