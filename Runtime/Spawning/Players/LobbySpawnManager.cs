using FishNet;
using System.Collections.Generic;

namespace CMDev.Lobby.Spawning
{
    public class LobbySpawnManager : SpawnManagerBase
    {
        private NetworkLobbyManager _lobbyManager;

        private void Awake()
        {
            _lobbyManager = NetworkLobbyManager.Instance;

            NetworkLobbyClient.ClientLoadedSceneEvent += NetworkLobbyClient_ClientLoadedScene;
            _lobbyManager.PlayerAddedEvent += LobbyManager_PlayerAdded;

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
            _lobbyManager.PlayerAddedEvent -= LobbyManager_PlayerAdded;
        }

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

        private void LobbyManager_PlayerAdded(NetworkLobbyPlayer player)
        {
            if (!InstanceFinder.IsServer)
                return;

            SpawnPlayer(player);
        }
    }
}