using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public struct SniperLaserData : INetworkSerializable
    {
        public Vector3 EndPos { get;private set; }
        public float StartYPos { get;private set; }


        public SniperLaserData(Vector3 endPos, float startYPos)
        {
            EndPos = endPos;
            StartYPos = startYPos;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var endPos = EndPos;
            var startY = StartYPos;
            
            serializer.SerializeValue(ref endPos);
            serializer.SerializeValue(ref startY);
            
            EndPos = endPos;
            StartYPos = startY;
        }
    }
}
