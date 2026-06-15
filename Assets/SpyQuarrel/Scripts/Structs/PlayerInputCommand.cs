using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    /// <summary>
    /// command for local logic on player (optimized input packing, full quaternion preserved)
    /// </summary>
    [Serializable]
    public struct PlayerInputCommand : INetworkSerializable, IEmptyDefault<PlayerInputCommand>
    {
        public Quaternion Rotation;
        public Vector2 Movement;
        public bool Jump;
        public CrouchType Crouch;

        public PlayerInputCommand(Vector2 movement, Quaternion rotation, bool jump = false, CrouchType crouch = CrouchType.None)
        {
            Movement = movement;
            Rotation = rotation;
            Jump = jump;
            Crouch = crouch;
        }

        public static PlayerInputCommand Empty =>
            new PlayerInputCommand(Vector2.zero, Quaternion.identity, false, CrouchType.None);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // full precision rotation (no compression)
            serializer.SerializeValue(ref Rotation);

            // compressed movement (-1..1 range)
            sbyte moveX = (sbyte)(Movement.x * 127f);
            sbyte moveY = (sbyte)(Movement.y * 127f);

            serializer.SerializeValue(ref moveX);
            serializer.SerializeValue(ref moveY);

            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Crouch);

            // decode
            Movement = new Vector2(
                moveX / 127f,
                moveY / 127f);
        }
    }
}