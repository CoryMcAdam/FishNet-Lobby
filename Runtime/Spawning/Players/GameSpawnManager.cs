using FishNet;
using FishNet.Object;

namespace CMDev.Networking.Lobby.Spawning
{
    /// <summary>
    /// Spawer class for spawning all players at the same time after all clients are loaded.
    /// </summary>
    public class GameSpawnManager : SpawnManagerBase
    {
        private bool _spawned;

        #region MonoBehaviour
        private void Awake()
        {
            _spawned = false;
            NetworkLobbyClient.AllClientsLoadedSceneEvent += LobbyClient_AllClientsLoadedScene;

            if (InstanceFinder.IsServer)
            {
                if (NetworkLobbyClient.AllLoaded)
                    SpawnAllPlayers();
            }
        }

        private void OnDestroy()
        {
            NetworkLobbyClient.AllClientsLoadedSceneEvent -= LobbyClient_AllClientsLoadedScene;
        }
        #endregion

        /// <summary>
        /// Called when all clients have loaded the global scene. If server, spawns all players.
        /// </summary>
        private void LobbyClient_AllClientsLoadedScene()
        {
            if (InstanceFinder.IsServer)
                SpawnAllPlayers();
        }

        /// <summary>
        /// [Server] Spawns a game player for each lobby player. Runs once.
        /// </summary>
        [Server]
        private void SpawnAllPlayers()
        {
            if (_spawned)
                return;

            _spawned = true;

            foreach (NetworkLobbyPlayer player in NetworkLobbyPlayer.AllPlayers)
            {
                SpawnPlayer(player);
            }
        }
    }
}