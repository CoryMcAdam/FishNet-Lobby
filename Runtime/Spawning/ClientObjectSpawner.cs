using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace CMDev.Lobby.Spawning
{
    public class ClientObjectSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject clientPrefab;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;

            _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
            }
        }

        private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (!asServer)
                return;

            if (clientPrefab == null)
            {
                Debug.LogWarning($"Client prefab is empty and cannot be spawned for connection {conn.ClientId}.", this);
                return;
            }

            NetworkObject clientObject = Instantiate(clientPrefab);
            _networkManager.ServerManager.Spawn(clientObject, conn);
        }
    }
}
