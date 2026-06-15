using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public struct NetworkVector3 : INetworkSerializable
    {
        public short X;
        public short Y;
        public short Z;

        public const float Precision = 100f;


        public NetworkVector3(Vector3 vector)
        {
            var vexX = vector.x;
            var vexY = vector.y;
            var vexZ = vector.z;
            
            X = (short)(vexX * Precision);
            Y = (short)(vexY * Precision);
            Z = (short)(vexZ * Precision);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X / Precision, Y / Precision, Z / Precision);
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            throw new System.NotImplementedException();
        }
    }
}
