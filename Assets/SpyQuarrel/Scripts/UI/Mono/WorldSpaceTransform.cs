using UnityEngine;

public class WorldSpaceTransform : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private void Awake()
    {
        if (!_camera)
        {
            _camera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (!_camera)
        {
            _camera = Camera.main;
            return;
        }

        var rot = Quaternion.LookRotation(
            transform.position - _camera.transform.position,
            _camera.transform.up);

        rot.x = 0;
        rot.z = 0;
        
        transform.rotation = rot;
    }
}