using System;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public enum PlayerCameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    public class PlayerCamera : MonoBehaviour
    {
        private const string FIRST_PERSON = "FirstPersonCamera";
        private const string THIRD_PERSON = "ThirdPersonCamera";
        
        
        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineCamera _firstPersonCamera;
        [SerializeField] private CinemachineCamera _thirdPersonCamera;
        [SerializeField] private PlayerCameraMode _startingCameraMode = PlayerCameraMode.FirstPerson;

        [Header("Rotation")]
        [SerializeField] private Vector2 _mouseSensitivity = new Vector2(0.2f, 0.2f);
        [SerializeField] private float _minPitch = -85f;
        [SerializeField] private float _maxPitch = 85f;

        public event Action<PlayerCameraMode> OnCameraModeChanged;

        public PlayerCameraMode CameraMode { get; private set; }

        public bool IsFirstPerson => CameraMode == PlayerCameraMode.FirstPerson;
        public bool IsThirdPerson => CameraMode == PlayerCameraMode.ThirdPerson;

        public Quaternion BodyRotation => Quaternion.Euler(0f, _yaw, 0f);
        public Quaternion CameraRotation => Quaternion.Euler(_pitch, _yaw, 0f);

        private Transform _cameraTarget;

        private float _yaw;
        private float _pitch;

        private bool _initialized;

        void Awake()
        {
            if (_firstPersonCamera == null)
            {
                _firstPersonCamera = FindObjectsByType<CinemachineCamera>(sortMode: FindObjectsSortMode.None).First(cam => cam.gameObject.CompareTag(FIRST_PERSON));
            }

            if (_thirdPersonCamera == null)
            {
               _thirdPersonCamera = FindObjectsByType<CinemachineCamera>(sortMode: FindObjectsSortMode.None).First(cam => cam.gameObject.CompareTag(THIRD_PERSON));
            }
        }
        
        public void Initialize(Transform cameraTarget)
        {
            if (!cameraTarget)
            {
                Debug.LogError("[PlayerCamera] Cannot initialize without a camera target.");
                return;
            }

            _cameraTarget = cameraTarget;

            Vector3 startAngles = cameraTarget.eulerAngles;

            _yaw = startAngles.y;
            _pitch = NormalizePitch(startAngles.x);

            UpdateCameraTargetTransform();

            ConfigureCinemachineCamera(_firstPersonCamera);
            ConfigureCinemachineCamera(_thirdPersonCamera);

            _initialized = true;

            SetCameraMode(_startingCameraMode, true);
        }

        private void LateUpdate()
        {
            if (!_initialized || !_cameraTarget)
                return;

            UpdateCameraTargetTransform();
        }

        public void UpdateRotation(CameraInput input)
        {
            _yaw += input.Look.x * _mouseSensitivity.x;
            _pitch -= input.Look.y * _mouseSensitivity.y;

            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        public void ToggleCameraMode()
        {
            PlayerCameraMode newMode = CameraMode == PlayerCameraMode.FirstPerson
                ? PlayerCameraMode.ThirdPerson
                : PlayerCameraMode.FirstPerson;

            SetCameraMode(newMode);
        }

        public void SetCameraMode(PlayerCameraMode cameraMode)
        {
            SetCameraMode(cameraMode, false);
        }

        private void SetCameraMode(PlayerCameraMode cameraMode, bool force)
        {
            if (!force && CameraMode == cameraMode)
                return;

            CameraMode = cameraMode;

            switch (CameraMode)
            {
                case PlayerCameraMode.FirstPerson:
                    SetCameraActive(_firstPersonCamera, true);
                    SetCameraActive(_thirdPersonCamera, false);
                    break;

                case PlayerCameraMode.ThirdPerson:
                    SetCameraActive(_thirdPersonCamera, true);
                    SetCameraActive(_firstPersonCamera, false);
                    break;
            }

            OnCameraModeChanged?.Invoke(CameraMode);
        }

        private void ConfigureCinemachineCamera(CinemachineCamera cinemachineCamera)
        {
            if (!cinemachineCamera)
                return;

            cinemachineCamera.Target.TrackingTarget = transform;
        }

        private void SetCameraActive(CinemachineCamera cinemachineCamera, bool active)
        {
            if (!cinemachineCamera)
                return;

            cinemachineCamera.gameObject.SetActive(active);
        }

        private void UpdateCameraTargetTransform()
        {
            transform.SetPositionAndRotation(
                _cameraTarget.position,
                CameraRotation);
        }

        private float NormalizePitch(float pitch)
        {
            if (pitch > 180f)
                pitch -= 360f;

            return Mathf.Clamp(pitch, _minPitch, _maxPitch);
        }
    }
}