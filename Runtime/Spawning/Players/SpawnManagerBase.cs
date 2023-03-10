using FishNet;
using FishNet.Object;
using UnityEngine;

namespace CMDev.Networking.Lobby.Spawning
{
    /// <summary>
    /// Base class for spawning game players into a scene.
    /// </summary>
    public abstract class SpawnManagerBase : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private NetworkGamePlayer gamePlayerPrefab;
        [SerializeField] private Transform[] spawnpoints;

        /// <summary>
        /// [Server] Spawns a game player and binds it to the lobby player.
        /// </summary>
        /// <param name="lobbyPlayer">The lobby player to spawn a game player for.</param>
        [Server]
        protected void SpawnPlayer(NetworkLobbyPlayer lobbyPlayer)
        {
            Debug.Log($"Attempting to spawn player for index {lobbyPlayer.Index}", this);
            if (lobbyPlayer.HasGamePlayer)
                return;

            Transform spawnTransform = GetSpawnTransform(lobbyPlayer.Index);

            Debug.Log($"Spawning player {lobbyPlayer.Index} at {spawnTransform.position}", this);

            NetworkGamePlayer gamePlayer = Instantiate(gamePlayerPrefab, spawnTransform.position, spawnTransform.rotation);

            gamePlayer.SetIndex(lobbyPlayer.Index);

            //Spawn on server.
            InstanceFinder.ServerManager.Spawn(gamePlayer.NetworkObject, lobbyPlayer.Owner);

            //Bind to the lobby player.
            lobbyPlayer.BindToGamePlayer(gamePlayer);
        }

        /// <summary>
        /// Gets a spawn transform for the passed index.
        /// </summary>
        /// <param name="index">The index to get a spawnpoint for.</param>
        /// <returns>Transform from the list of spawnpoints. Cycles through if index is greater than spawnpoints length.</returns>
        private Transform GetSpawnTransform(int index)
        {
            return spawnpoints[index % (spawnpoints.Length - 1)];
        }
    }
}