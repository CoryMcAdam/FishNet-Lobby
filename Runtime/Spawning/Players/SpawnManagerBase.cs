using FishNet;
using FishNet.Object;
using UnityEngine;

namespace CMDev.Networking.Lobby.Spawning
{
    public abstract class SpawnManagerBase : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private NetworkGamePlayer gamePlayerPrefab;
        [SerializeField] private Transform[] spawnpoints;

        [Server]
        protected void SpawnPlayer(NetworkLobbyPlayer lobbyPlayer)
        {
            Debug.Log($"Attempting to spawn player for index {lobbyPlayer.Index}", this);
            if (lobbyPlayer.HasGamePlayer)
                return;

            Transform spawnTransform = GetSpawnTransform(lobbyPlayer.Index);

            Debug.Log($"Spawning player {lobbyPlayer.Index} at {spawnTransform.position}", this);

            NetworkGamePlayer gamePlayer = Instantiate(gamePlayerPrefab, spawnTransform.position, spawnTransform.rotation);

            gamePlayer.Index = lobbyPlayer.Index;

            //Spawn on server.
            InstanceFinder.ServerManager.Spawn(gamePlayer.NetworkObject, lobbyPlayer.Owner);

            //Bind to the lobby player.
            lobbyPlayer.BindToGamePlayer(gamePlayer);
        }

        private Transform GetSpawnTransform(int index)
        {
            return spawnpoints[index % (spawnpoints.Length - 1)];
        }
    }
}
