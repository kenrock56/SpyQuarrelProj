using AutoSingleton;
using Unity.Services.Core;
using Unity.Services.Authentication;

using Unity.Services.Multiplayer;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Singleton]
    public class SessionManager : MonoBehaviour
    {
        [SerializeReference]private ISession _currentSession;

        public ISession CurrentSession
        {
            get => _currentSession;
            set
            {
                _currentSession = value;
                Debug.Log($"Active Session: {_currentSession}");
            }
        }

        async void Start()
        {
        }
        
    }
}
