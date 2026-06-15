using System;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class CameraSpring : MonoBehaviour
    {

        [Min(0.01f), SerializeField] private float _halfLife = 0.075f;
        [SerializeField] private float _frequency = 18f;
        [SerializeField] private float _angularDisplacement = 2f;
        [SerializeField] private float _linearDisplacement = 0.05f;
        
        private Vector3 _springPosition;
        private Vector3 _springVelocity;
        
        public void Initialize()
        {
            _springPosition = transform.position;
            _springVelocity = Vector3.zero;
        }

        public void UpdateSpring(float deltaTime)
        {
            Spring(ref _springPosition, ref _springVelocity, transform.position, _halfLife, _frequency, deltaTime);
            
            var localSprintPosition = _springPosition - transform.position;
            localSprintPosition.y = transform.position.y;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _springPosition);
            Gizmos.DrawSphere(_springPosition, 0.1f);
        }

        // function ffrom https://allenchou.net/2015/04/game-math-more-on-numeric-springing/
        private static void Spring(ref Vector3 current, ref Vector3 velocity, Vector3 target, float halfLife, float frequency, float timeStep)
        {
            var dampingRatio = -Mathf.Log(0.5f) / (frequency * halfLife);
            var f = 1.0f + 2.0f * timeStep * dampingRatio * frequency;
            var oo = frequency * frequency;
            var hoo = timeStep * oo;
            var hhoo = timeStep * hoo;
            var detInv = 1.0f / (f + hhoo);
            var detX = f * current + timeStep * velocity + hhoo * target;
            var detV = velocity + hoo * (target - current);
            current = detX * detInv;
            velocity = detV * detInv;
        }
    }
}
