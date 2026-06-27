using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        public static SessionManager Instance => Singleton<SessionManager>.Instance;
        public static bool HasInstance => Singleton<SessionManager>.HasInstance;
        
        [SerializeReference]private ISession _currentSession;

        
        const string PlayerNamePropertyKey = "PlayerName";
        const string PlayerIdPropertyKey = "PlayerId";
        
        public ISession CurrentSession
        {
            get => _currentSession;
            set
            {
                _currentSession = value;
                Debug.Log($"Active Session: {_currentSession}");
            }
        }


        async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
        {
            var playerInfo = await AuthenticationService.Instance.GetPlayerInfoAsync();
            
            var playerNameProperty = new PlayerProperty(playerInfo.Username, VisibilityPropertyOptions.Member);
            var playerIDProperty = new PlayerProperty(playerInfo.Id, VisibilityPropertyOptions.Member);

            var dic = new Dictionary<string, PlayerProperty>()
            {
                {PlayerNamePropertyKey, playerNameProperty},
                {PlayerIdPropertyKey, playerIDProperty}
            };
            
            return dic;
        }
        
        async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                var player = await AuthenticationService.Instance.GetPlayerInfoAsync();
                var id = AuthenticationService.Instance.PlayerId;

                Debug.Log($"PlayerId: {id} : {player.Id}");
                
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            
        }

        private void RegisterCallbacks()
        {
            CurrentSession.Changed += OnSessionChanged;
        }

        private void UnregisterCallbacks()
        {
            CurrentSession.Changed -= OnSessionChanged;
        }

        public async void StartSessionAsHost()
        {
            var playerProperties = await GetPlayerProperties();
            SessionOptions options = new SessionOptions()
            {
                IsLocked = false,
                IsPrivate = false,
                MaxPlayers = 2,
                Name = $"My Session {System.DateTime.UtcNow.TimeOfDay}",
                PlayerProperties = playerProperties,
            }.WithRelayNetwork();
            
            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            
            RegisterCallbacks();
            
            Debug.Log($"Session created: {CurrentSession.Id}. {CurrentSession.Code}");
        }

        public async void JoinSessionById(string sessionId)
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            RegisterCallbacks();
            Debug.Log($"Joined session: {CurrentSession.Id}. {CurrentSession.Code}");
        }

        public async void JoinSessionByCode(string code)
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            RegisterCallbacks();
            Debug.Log($"Joined session: {CurrentSession.Id}. {CurrentSession.Code}");
        }

        public async void QuickJoinSession()
        {

            QuerySessionsOptions sessionOptions = new QuerySessionsOptions()
            {

            };

            var sessions = await MultiplayerService.Instance.QuerySessionsAsync(sessionOptions);

            if (sessions.Sessions.Count > 0)
            {
                CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessions.Sessions[0].Id);
            }
            else
            {
                Debug.LogWarning("No active sessions");
            }

        }

        public async Task KickPlayer(string playerId)
        {
            if(!CurrentSession.IsHost)return;
            
            await CurrentSession.AsHost().RemovePlayerAsync(playerId);
        }

        public async Task<IList<ISessionInfo>> GetSessions()
        {
            var sessionQueryOptions = new QuerySessionsOptions();

            var results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);

            return results.Sessions;
        }

        public async Task LeaveSession()
        {
            if(CurrentSession == null)return;

            UnregisterCallbacks();
            try
            {
                await CurrentSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                CurrentSession = null;
            }
        }

        private void OnSessionChanged()
        {
            
        }
        

    }
}
