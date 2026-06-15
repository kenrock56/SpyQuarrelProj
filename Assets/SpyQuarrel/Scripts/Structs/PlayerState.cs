using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Serializable]
    public struct PlayerState : INetworkSerializable
    {
        [field: SerializeField] public bool Grounded { get; set; }
        [field: SerializeField] public PlayerStance Stance { get; set; }
        [field: SerializeField] public Vector3 Velocity { get; set; }

        public PlayerState(bool grounded = true, PlayerStance stance = PlayerStance.Standing, Vector3 velocity = default)
        {
            Grounded = grounded;
            Stance = stance;
            Velocity = velocity;
        }

        public static PlayerState Default()
        {
            return new PlayerState(true, PlayerStance.Standing, Vector3.zero);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            bool grounded = Grounded;
            PlayerStance stance = Stance;
            Vector3 velocity = Velocity;

            serializer.SerializeValue(ref grounded);
            serializer.SerializeValue(ref stance);
            serializer.SerializeValue(ref velocity);

            Grounded = grounded;
            Stance = stance;
            Velocity = velocity;
        }
    }
}