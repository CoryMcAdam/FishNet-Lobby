using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CMDev.Networking.Lobby
{
    /// <summary>
    /// A networked component that represents a client connection to the server.
    /// </summary>
    public class NetworkLobbyClient : NetworkBehaviour
    {
        //PRIVATE FIELDS.
        private static NetworkLobbyClient _localClient;
        private static List<NetworkLobbyClient> _all = new List<NetworkLobbyClient>();
        private static bool _allLoaded = false;

        //SYNCED FIELDS.
        /// <summary>
        /// [SyncVar] The current load state of the client.
        /// </summary>
        [SyncVar(OnChange = "OnLoadStateChanged")]
        private EClientLoadState _loadState = EClientLoadState.Idle;

        #region EVENTS

        /// <summary>
        /// Called when a client loads into the current global scene.
        /// <para>Passes the client that loaded.</para>
        /// </summary>
        public static event Action<NetworkLobbyClient> ClientLoadedSceneEvent;

        /// <summary>
        /// Called when all clients have loaded into the current global scene.
        /// </summary>
        public static event Action AllClientsLoadedSceneEvent;
        #endregion

        #region PROPERTIES
        /// <summary>
        /// Is the client loaded into the current global scene.
        /// </summary>
        public bool IsLoaded { get { return _loadState == EClientLoadState.Loaded; } }

        /// <summary>
        /// A list of all clients on the network as read only.
        /// </summary>
        public static ReadOnlyCollection<NetworkLobbyClient> All { get { return _all.AsReadOnly(); } }

        /// <summary>
        /// Are all network clients loaded into the current global scene.
        /// </summary>
        public static bool AllLoaded { get { return _allLoaded; } }

        /// <summary>
        /// The network client for the local connection.
        /// </summary>
        public static NetworkLobbyClient LocalClient
        {
            get { return _localClient; }
            private set
            {
                if (value == _localClient)
                {
                    Debug.LogError("Is already local client, shouldn't be trying to set again.");
                    return;
                }

                if (value != null && _localClient != null)
                {
                    Debug.LogError("Multiple local clients attempting to register.");
                    return;
                }

                _localClient = value;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (!_all.Contains(this))
                _all.Add(this);
        }

        private void OnDestroy()
        {
            if (_all.Contains(this))
                _all.Remove(this);
        }

        #endregion

        #region NetworkBehaviour

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            name = $"Client - {OwnerId} ({(base.Owner.IsLocalClient ? "Local" : "Remote")})";

            if (base.Owner.IsLocalClient)
            {
                LocalClient = this;
                _loadState = EClientLoadState.Idle;

                SceneManager.OnQueueStart += SceneManager_OnQueueStart;
                SceneManager.OnQueueEnd += SceneManager_OnQueueEnd;

                UpdateAllLoading();
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            if (base.Owner.IsLocalClient)
            {
                SceneManager.OnQueueStart -= SceneManager_OnQueueStart;
                SceneManager.OnQueueEnd -= SceneManager_OnQueueEnd;
            }
        }

        #endregion

        /// <summary>
        /// Called when the scene manager starts a scene queue.
        /// </summary>
        private void SceneManager_OnQueueStart()
        {
            if (base.Owner.IsLocalClient)
                SetLoadState(EClientLoadState.Loading);
        }


        /// <summary>
        /// Called when the scene manager has finished a scene queue.
        /// </summary>
        private void SceneManager_OnQueueEnd()
        {
            if (base.Owner.IsLocalClient)
                SetLoadState(EClientLoadState.Loaded);
        }

        /// <summary>
        /// Sets the load state of the client. 
        /// </summary>
        /// <param name="value">The load state to set the client to.</param>
        [ServerRpc(RunLocally = true)]
        public void SetLoadState(EClientLoadState value)
        {
            _loadState = value;
        }

        /// <summary>
        /// Sets the load state of all clients.
        /// </summary>
        /// <param name="value">The load state to set all clients to.</param>
        [Server]
        public static void SetAllClientsLoadState(EClientLoadState value)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                _all[i]._loadState = value;
            }
        }

        /// <summary>
        /// [SyncVar Hook] Called when the clients load state changes.
        /// </summary>
        /// <param name="oldValue">The previous load state.</param>
        /// <param name="newValue">The new load state.</param>
        /// <param name="asServer">Is the method being run as the server.</param>
        private void OnLoadStateChanged(EClientLoadState oldValue, EClientLoadState newValue, bool asServer)
        {
            if (newValue == EClientLoadState.Loaded && oldValue == EClientLoadState.Loading)
                ClientLoadedSceneEvent?.Invoke(this);

            UpdateAllLoading();
        }

        /// <summary>
        /// Checks all clients and triggers event if all clients have loaded.
        /// </summary>
        private static void UpdateAllLoading()
        {
            //Check all clients
            for (int i = 0; i < _all.Count; i++)
            {
                if (!_all[i].IsLoaded)
                {
                    //If client isn't loaded, set all loaded to false and stop checking further.
                    _allLoaded = false;
                    return;
                }
            }

            //If all clients aren't already loaded.
            if (_allLoaded != true)
            {
                _allLoaded = true;
                AllClientsLoadedSceneEvent?.Invoke();
                SetAllClientsLoadState(EClientLoadState.Idle);
            }
        }
    }
}