using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CMDev.Lobby
{
    public class NetworkLobbyPlayer : NetworkBehaviour
    {
        //EDITOR FIELDS.


        //PRIVATE FIELDS.
        private NetworkLobbyManager _lobbyManager;

        private static List<NetworkLobbyPlayer> _localPlayers = new List<NetworkLobbyPlayer>();
        private static List<NetworkLobbyPlayer> _allPlayers = new List<NetworkLobbyPlayer>();

        //SYNCED FIELDS.
        [SyncVar(OnChange = "OnIndexChanged")]
        private int _index = -1;

        [SyncVar(OnChange = "OnGamePlayerChanged")]
        private NetworkGamePlayer _gamePlayer;

        //PUBLIC FIELDS.
        public bool IsLocal { get { return base.Owner.IsLocalClient; } } //Is the player owned by the client.
        public int Index { get { return _index; } } //The lobby index of the player.

        public static ReadOnlyCollection<NetworkLobbyPlayer> LobbyPlayers { get { return _localPlayers.AsReadOnly(); } }
        public static ReadOnlyCollection<NetworkLobbyPlayer> AllPlayers { get { return _allPlayers.AsReadOnly(); } }

        //EVENTS.
        public event Action<NetworkGamePlayer, NetworkGamePlayer> GamePlayerChangedEvent;

        //PROPERTIES.
        public bool HasGamePlayer { get { return _gamePlayer != null; } }
        public NetworkGamePlayer GamePlayer { get { return _gamePlayer; } }


        private void Awake()
        {
            this.name = $"Player_{Index}";

            _lobbyManager = NetworkLobbyManager.Instance;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _lobbyManager.AddPlayer(this);

            if (!_allPlayers.Contains(this))
                _allPlayers.Add(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            _lobbyManager.RemovePlayer(this);

            if (_localPlayers.Contains(this))
                _localPlayers.Remove(this);

            if (_allPlayers.Contains(this))
                _allPlayers.Remove(this);
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            if (IsOwner)
            {
                if (!_localPlayers.Contains(this))
                    _localPlayers.Add(this);
            }
            else
            {
                if (_localPlayers.Contains(this))
                    _localPlayers.Remove(this);
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            if (HasGamePlayer)
            {
                _gamePlayer.Despawn();
            }
        }

        public void SetIndex(int value)
        {
            _index = value;
        }

        private void OnIndexChanged(int oldValue, int newValue, bool asServer)
        {
            _lobbyManager.UpdatePlayerSlot(this, oldValue);

            this.name = $"Player_{newValue}";
        }

        [ServerRpc(RequireOwnership = true)]
        public void InputBindingFailed()
        {
            //Couldn't bind to an input, despawns the player to clear the lobby.
            base.Despawn();
        }

        public void BindToGamePlayer(NetworkGamePlayer player)
        {
            _gamePlayer = player;
        }

        private void OnGamePlayerChanged(NetworkGamePlayer oldValue, NetworkGamePlayer newValue, bool asServer)
        {
            if (asServer)
                return;

            GamePlayerChangedEvent?.Invoke(oldValue, newValue);
        }

        public static List<NetworkLobbyPlayer> GetPlayersForConnection(NetworkConnection conn)
        {
            List<NetworkLobbyPlayer> players = new List<NetworkLobbyPlayer>();

            for (int i = 0; i < AllPlayers.Count; i++)
            {
                if (AllPlayers[i].Owner == conn)
                    players.Add(AllPlayers[i]);
            }

            return players;
        }
    }
}