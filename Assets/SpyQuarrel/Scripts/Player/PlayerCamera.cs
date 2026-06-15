using Unity.Cinemachine;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField]private Vector2 _mouseSensitivity = new Vector2(0.2f, 0.2f);

        
        
        private Transform _cameraTarget;

        private Vector3 _eulerAngles;
        
        public void Initialize(Transform cameraTarget)
        {
            _cameraTarget = cameraTarget;
            transform.position = cameraTarget.position;
            transform.rotation = cameraTarget.rotation;
            
            transform.eulerAngles = _eulerAngles = cameraTarget.eulerAngles;

            var cinemachineCam = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCam == null)return;
            
            cinemachineCam.Target.TrackingTarget = _cameraTarget;
        }
        
        public void UpdateRotation(CameraInput input)
        {
            _eulerAngles += new Vector3(-input.Look.y * _mouseSensitivity.y, input.Look.x * _mouseSensitivity.x);
            transform.eulerAngles = _eulerAngles;
        }

        public void UpdatePosition(Transform target)
        {
            //transform.position = target.position;
        }
    }
}
