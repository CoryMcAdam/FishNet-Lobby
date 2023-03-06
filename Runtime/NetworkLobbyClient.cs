using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace CMDev.Lobby
{
    public class NetworkLobbyClient : NetworkBehaviour
    {
        //EDITOR FIELDS.


        //PRIVATE FIELDS.
        private List<NetworkLobbyPlayer> _players = new List<NetworkLobbyPlayer>();

        private static NetworkLobbyClient _localClient;
        private static List<NetworkLobbyClient> _all = new List<NetworkLobbyClient>();
        private static bool _allLoaded = false;

        //SYNCED FIELDS.
        [SyncVar(OnChange = "OnLoadStateChanged")]
        private EClientLoadState _loadState = EClientLoadState.Idle;

        //EVENTS.
        public static event Action<NetworkLobbyClient> ClientLoadedSceneEvent;
        public static event Action AllClientsLoadedSceneEvent;

        //PROPERTIES.
        public bool IsLoaded { get { return _loadState == EClientLoadState.Loaded; } }
        public static ReadOnlyCollection<NetworkLobbyClient> All { get { return _all.AsReadOnly(); } }
        public static bool AllLoaded { get { return _allLoaded; } }

        public static NetworkLobbyClient LocalClient
        {
            get { return _localClient; }
            private set
            {
                if (value == _localClient)
                    return;

                if (value != null && _localClient != null)
                    Debug.LogError("Multiple local clients attempting to register.");

                _localClient = value;
            }
        }

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

            if(base.Owner.IsLocalClient)
            {
                SceneManager.OnQueueStart -= SceneManager_OnQueueStart;
                SceneManager.OnQueueEnd -= SceneManager_OnQueueEnd;
            }
        }

        private void SceneManager_OnQueueStart()
        {
            if (base.Owner.IsLocalClient)
                SetLoadState(EClientLoadState.Loading);
        }

        private void SceneManager_OnQueueEnd()
        {
            if (base.Owner.IsLocalClient)
                SetLoadState(EClientLoadState.Loaded);
        }

        [ServerRpc(RunLocally = true)]
        public void SetLoadState(EClientLoadState value)
        {
            _loadState = value;
        }

        [Server]
        public static void SetAllClientsLoadState(EClientLoadState value)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                _all[i]._loadState = value;
            }
        }

        private void OnLoadStateChanged(EClientLoadState oldValue, EClientLoadState newValue, bool asServer)
        {
            if (newValue == EClientLoadState.Loaded)
                ClientLoadedSceneEvent?.Invoke(this);

            UpdateAllLoading();
        }

        //TODO: Can probably clean this up to avoid calls when setting all to false.
        //TODO: Make sure event can only be called once per all loaded.
        private static void UpdateAllLoading()
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (!_all[i].IsLoaded)
                {
                    _allLoaded = false;
                    return;
                }
            }

            if (_allLoaded != true)
            {
                _allLoaded = true;
                AllClientsLoadedSceneEvent?.Invoke();
                SetAllClientsLoadState(EClientLoadState.Idle);
            }
        }
    }
}