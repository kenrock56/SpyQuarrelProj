using System;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Serializable]
    public struct PlayerInputPayload : INetworkSerializable, IEmptyDefault<PlayerInputPayload>
    {
        public int Tick { get; set; }
        
        public PlayerInputCommand Command { get; set; }
        

        public PlayerInputPayload(int tick, PlayerInputCommand command)
        {
            Tick = tick;
            Command = command;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            int tick = Tick;
            PlayerInputCommand command = Command;

            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref command);

            Tick = tick;
            Command = command;
        }


        public static PlayerInputPayload Empty => GetEmpty();
        private static PlayerInputPayload GetEmpty()
        {
            return new PlayerInputPayload()
            {
                Tick = -1,
                Command = PlayerInputCommand.Empty
            };
        }
    }
}