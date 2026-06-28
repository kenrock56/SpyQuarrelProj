using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Serializable]
    public struct PlayerImpliedStatePayload : INetworkSerializable
    {
        public int Tick;

        public Vector3 Position;
        public float Yaw;
        public Vector3 Velocity;

        public PlayerStance Stance;
        public bool Grounded;

        public static PlayerImpliedStatePayload FullToImplied(PlayerStatePayload state)
        {
            return new PlayerImpliedStatePayload
            {
                Tick = state.Tick,

                Position = state.Position,
                Yaw = state.Rotation.eulerAngles.y,
                Velocity = state.Velocity,

                Stance = state.CurrentState.Stance,
                Grounded = state.CurrentState.Grounded
            };
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);

            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Yaw);
            serializer.SerializeValue(ref Velocity);

            serializer.SerializeValue(ref Stance);
            serializer.SerializeValue(ref Grounded);
        }
    }
}