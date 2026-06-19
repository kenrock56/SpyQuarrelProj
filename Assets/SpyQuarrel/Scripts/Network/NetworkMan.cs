using System;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    public class NetworkMan : MonoBehaviour
    {
        private UnityTransport _transport;
        
        void Awake()
        {
            _transport = GetComponent<UnityTransport>();
            
        }

        private void Start()
        {
            if (_transport == null)return;
            
        }
    }
}
