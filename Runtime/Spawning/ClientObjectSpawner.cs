using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

namespace CMDev.Networking.Spawning
{
    /// <summary>
    /// Spawns an object for each connecting client.
    /// </summary>
    public class ClientObjectSpawner : MonoBehaviour
    {
        //EDITOR FIELDS.
        [SerializeField] private NetworkObject clientPrefab;

        //PRIVATE FIELDS.
        private NetworkManager _networkManager;

        #region MonoBehaviour
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
        #endregion

        /// <summary>
        /// Called when a client loads the starting scene. Spawns an object on the server with the client as owner.
        /// </summary>
        /// <param name="conn">The connection that loaded the start scene.</param>
        /// <param name="asServer">Is this being called as the server.</param>
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