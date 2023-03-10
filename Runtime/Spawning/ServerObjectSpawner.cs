using FishNet;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace CMDev.Networking.Spawning
{
    /// <summary>
    /// Spawns an object when the server starts.
    /// </summary>
    public class ServerObjectSpawner : MonoBehaviour
    {
        //EDITOR FIELDS.
        [SerializeField] private string objectName;
        [SerializeField] private NetworkObject networkObjectPrefab;

        //PRIVATE FIELDS.
        private NetworkManager _networkManager;
        private bool _spawnedOnce = false;

        #region MonoBehaviour
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
        #endregion

        /// <summary>
        /// Called when the server connection state changes. Spawns an object once if the server started, and resets if the server stopped.
        /// </summary>
        /// <param name="state">The connection state args.</param>
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

        /// <summary>
        /// If being called by the server, spawns an object on the network.
        /// </summary>
        private void SpawnObjectIfServer()
        {
            if (!InstanceFinder.IsServer)
                return;

            //Make sure to only spawn the object once each time the server starts.
            if (_spawnedOnce)
                return;

            _spawnedOnce = true;

            NetworkObject newObject = Instantiate(networkObjectPrefab);

            newObject.name = objectName;

            _networkManager.ServerManager.Spawn(newObject.gameObject);
        }
    }
}