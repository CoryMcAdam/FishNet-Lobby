using FishNet;
using FishNet.Object;

namespace CMDev.Networking.Lobby.Spawning
{
    public class GameSpawnManager : SpawnManagerBase
    {
        private bool _spawned;

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

        private void LobbyClient_AllClientsLoadedScene()
        {
            if (InstanceFinder.IsServer)
                SpawnAllPlayers();
        }

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