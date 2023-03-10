using FishNet;
using System.Collections.Generic;

namespace CMDev.Networking.Lobby.Spawning
{
    /// <summary>
    /// Spawer class for spawning players at the start of the scene or when clients connect to the lobby.
    /// </summary>
    public class LobbySpawnManager : SpawnManagerBase
    {
        private NetworkLobbyManager _lobbyManager;

        #region MonoBehaviour
        private void Awake()
        {
            _lobbyManager = NetworkLobbyManager.Instance;

            NetworkLobbyClient.ClientLoadedSceneEvent += NetworkLobbyClient_ClientLoadedScene;
            _lobbyManager.PlayerAddedEvent += LobbyManager_PlayerAdded;
        }

        private void Start()
        {
            if (InstanceFinder.IsServer)
            {
                foreach (NetworkLobbyPlayer player in NetworkLobbyPlayer.AllPlayers)
                {
                    SpawnPlayer(player);
                }
            }
        }

        private void OnDestroy()
        {
            NetworkLobbyClient.ClientLoadedSceneEvent -= NetworkLobbyClient_ClientLoadedScene;

            if (_lobbyManager != null)
            {
                _lobbyManager.PlayerAddedEvent -= LobbyManager_PlayerAdded;
            }
        }
        #endregion

        /// <summary>
        /// Called when a client loads the current global scene. If server, spawns all the players for that connection.
        /// </summary>
        /// <param name="lobbyClient">The lobby client to load the current global scene.</param>
        private void NetworkLobbyClient_ClientLoadedScene(NetworkLobbyClient lobbyClient)
        {
            if (!InstanceFinder.IsServer)
                return;
            List<NetworkLobbyPlayer> clientPlayers = NetworkLobbyPlayer.GetPlayersForConnection(lobbyClient.Owner);

            for (int i = 0; i < clientPlayers.Count; i++)
            {
                SpawnPlayer(clientPlayers[i]);
            }
        }

        /// <summary>
        /// Called when a lobby player is added. If server, spawns a game player for that lobby player.
        /// </summary>
        /// <param name="player">The lobby player that was added.</param>
        private void LobbyManager_PlayerAdded(NetworkLobbyPlayer player)
        {
            if (!InstanceFinder.IsServer)
                return;

            SpawnPlayer(player);
        }
    }
}