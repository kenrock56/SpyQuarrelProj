using UnityEngine;

namespace SpyQuarrelRuntime
{
    public interface IAnimatorContext
    {
        public Vector3 Velocity { get; }
        public float Speed { get; }
        public Vector3 ForwardDirection { get; }
    }
}
