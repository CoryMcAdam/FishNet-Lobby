using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace CMDev.Lobby.Spawning
{
    public class ServerObjectSpawner : MonoBehaviour
    {
        //EDITOR FIELDS.
        [SerializeField] private string objectName;
        [SerializeField] private NetworkObject networkObjectPrefab;

        //PRIVATE FIELDS.
        private NetworkManager _networkManager;
        private bool _spawnedOnce = false;

        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;

            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            }
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            switch (state.ConnectionState)
            {
                case LocalConnectionState.Stopped:
                    _spawnedOnce = false;
                    break;
                case LocalConnectionState.Starting:
                    break;
                case LocalConnectionState.Started:
                    SpawnObjectIfServer();
                    break;
                case LocalConnectionState.Stopping:
                    break;
            }
        }

        private void SpawnObjectIfServer()
        {
            if (!InstanceFinder.IsServer)
                return;

            if (_spawnedOnce)
                return;

            _spawnedOnce = true;

            NetworkObject newObject = Instantiate(networkObjectPrefab);

            newObject.name = objectName;

            _networkManager.ServerManager.Spawn(newObject.gameObject);
        }
    }
}