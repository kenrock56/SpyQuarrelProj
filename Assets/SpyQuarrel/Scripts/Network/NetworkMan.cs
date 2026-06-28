using System;
using AutoSingleton;
using SpyQuarrelProject;
using Unity.Netcode;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Singleton]
    public class GameNetworkManager : NetworkBehaviour
    {
        public static GameNetworkManager Instance => Singleton<GameNetworkManager>.Instance;
        public static bool HasInstance => Singleton<GameNetworkManager>.HasInstance;

        public Action OnSuccessfulSpawn
        {
            get => _onSuccessfulSpawn;
            set => _onSuccessfulSpawn = value;
        }

        private Action _onSuccessfulSpawn;

        public void SpawnAsRole(PlayerRole role)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("Cannot spawn player. NetworkManager.Singleton is null.");
                return;
            }

            if (!NetworkManager.Singleton.IsListening)
            {
                Debug.LogError("Cannot spawn player. NetworkManager is not running.");
                return;
            }

            if (!IsSpawned)
            {
                Debug.LogError("Cannot spawn player. GameNetworkManager NetworkObject is not spawned.");
                return;
            }

            if (IsServer)
            {
                SpawnPlayer(NetworkManager.Singleton.LocalClientId, role);
                return;
            }

            RequestSpawnAsRoleRpc(role);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestSpawnAsRoleRpc(PlayerRole role, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            SpawnPlayer(senderClientId, role);
        }

        private void SpawnPlayer(ulong clientId, PlayerRole role)
        {
            if (!IsServer)
            {
                Debug.LogError("Cannot spawn player. Only the server/host can spawn NetworkObjects.");
                return;
            }

            var prefab = PlayerPrefabDictionary.Instance.GetValue(role);

            if (prefab == null)
            {
                Debug.LogError($"No player prefab found for role: {role}");
                return;
            }

            NetworkObject networkObjectPrefab = prefab.GetComponent<NetworkObject>();

            if (networkObjectPrefab == null)
            {
                Debug.LogError($"Player prefab '{prefab.name}' does not have a NetworkObject on the same GameObject.");
                return;
            }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            {
                Debug.LogError($"Cannot spawn player. Client {clientId} is not connected.");
                return;
            }

            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                client.PlayerObject.Despawn(true);
            }

            Player playerInstance = Instantiate(prefab, new Vector3(0, 50, 0), Quaternion.identity);
            NetworkObject spawnedNetworkObject = playerInstance.GetComponent<NetworkObject>();

            if (spawnedNetworkObject == null)
            {
                Debug.LogError($"Spawned player '{playerInstance.name}' does not have a NetworkObject.");
                Destroy(playerInstance.gameObject);
                return;
            }

            spawnedNetworkObject.SpawnAsPlayerObject(clientId, true);

            var playerTarget = RpcTarget.Single(clientId, RpcTargetUse.Temp);
            
            InvokeSuccessfulSpawnRpc(playerTarget);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void InvokeSuccessfulSpawnRpc(RpcParams rpcParams = default)
        {
            _onSuccessfulSpawn?.Invoke();
        }
    }
}