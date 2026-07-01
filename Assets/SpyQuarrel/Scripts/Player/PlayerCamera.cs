using Unity.Cinemachine;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Vector2 _mouseSensitivity = new Vector2(0.2f, 0.2f);
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;

        private Transform _cameraTarget;

        private float _yaw;
        private float _pitch;

        public Quaternion BodyRotation => Quaternion.Euler(0f, _yaw, 0f);
        public Quaternion CameraRotation => Quaternion.Euler(_pitch, _yaw, 0f);

        public void Initialize(Transform cameraTarget)
        {
            _cameraTarget = cameraTarget;

            Vector3 startAngles = cameraTarget.eulerAngles;

            _yaw = startAngles.y;
            _pitch = NormalizePitch(startAngles.x);

            transform.position = cameraTarget.position;
            transform.rotation = CameraRotation;

            var cinemachineCam = FindFirstObjectByType<CinemachineCamera>();

            if (cinemachineCam != null)
                cinemachineCam.Target.TrackingTarget = _cameraTarget;
        }

        public void UpdateRotation(CameraInput input)
        {
            _yaw += input.Look.x * _mouseSensitivity.x;
            _pitch -= input.Look.y * _mouseSensitivity.y;

            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            transform.rotation = CameraRotation;
        }

        private float NormalizePitch(float pitch)
        {
            if (pitch > 180f)
                pitch -= 360f;

            return Mathf.Clamp(pitch, _minPitch, _maxPitch);
        }
    }
}