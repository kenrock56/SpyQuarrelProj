using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class WeaponCarrier : MonoBehaviour
    {
        [SerializeField] private Transform _transformToFollow;
        [SerializeField] private Transform _transformToAim;
        [SerializeField] private float _rotationLerpSpeed = 15f;

        private void UpdateTransform()
        {
            if (_transformToFollow == null) return;
            if (_transformToAim == null) return;

            transform.position = _transformToFollow.position;

            Quaternion targetRotation = Quaternion.LookRotation(
                _transformToAim.forward,
                _transformToAim.up);

            Quaternion targetLocalRotation = transform.parent != null
                ? Quaternion.Inverse(transform.parent.rotation) * targetRotation
                : targetRotation;

            float t = 1f - Mathf.Exp(-_rotationLerpSpeed * Time.deltaTime);

            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetLocalRotation,
                t);
        }

        private void LateUpdate()
        {
            UpdateTransform();
        }
    }
}