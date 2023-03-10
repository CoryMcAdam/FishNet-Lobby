using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CMDev.Networking.Lobby
{
    /// <summary>
    /// Represents a lobby player object in the current lobby.
    /// </summary>
    public class NetworkLobbyPlayer : NetworkBehaviour
    {
        //PRIVATE FIELDS.
        private NetworkLobbyManager _lobbyManager;
        private static List<NetworkLobbyPlayer> _localPlayers = new List<NetworkLobbyPlayer>();
        private static List<NetworkLobbyPlayer> _allPlayers = new List<NetworkLobbyPlayer>();

        //SYNCED FIELDS.
        [SyncVar(OnChange = "OnIndexChanged")]
        private int _index = -1;

        [SyncVar(OnChange = "OnGamePlayerChanged")]
        private NetworkGamePlayer _gamePlayer;

        #region PROPERTIES
        /// <summary>
        /// Does the lobby player have a linked game player object.
        /// </summary>
        public bool HasGamePlayer { get { return _gamePlayer != null; } }

        /// <summary>
        /// The current game player the lobby player is linked to.
        /// </summary>
        public NetworkGamePlayer GamePlayer { get { return _gamePlayer; } }

        /// <summary>
        /// Is the lobby player owned by the local client.
        /// </summary>
        public bool IsLocal { get { return base.Owner.IsLocalClient; } }

        /// <summary>
        /// The current lobby slot index of the lobby player.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// A read only list of all lobby players.
        /// </summary>
        public static ReadOnlyCollection<NetworkLobbyPlayer> AllPlayers { get { return _allPlayers.AsReadOnly(); } }

        /// <summary>
        /// A read only list of all local players.
        /// </summary>
        public static ReadOnlyCollection<NetworkLobbyPlayer> LocalPlayers { get { return _localPlayers.AsReadOnly(); } }
        #endregion

        #region EVENTS
        /// <summary>
        /// Called when the linked game player is changed.
        /// <para>Passes the old game player and the new game player.</para>
        /// </summary>
        public event Action<NetworkGamePlayer, NetworkGamePlayer> GamePlayerChangedEvent;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            this.name = $"Player_{Index}";

            _lobbyManager = NetworkLobbyManager.Instance;
        }
        #endregion

        #region NetworkBehaviour

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

        #endregion

        /// <summary>
        /// Sets the index of the lobby player.
        /// </summary>
        /// <param name="value">The new index.</param>
        public void SetIndex(int value)
        {
            _index = value;
        }

        /// <summary>
        /// [SyncVar Hook] Called when the lobby players index changes.
        /// </summary>
        /// <param name="oldValue">The previous index value.</param>
        /// <param name="newValue">The new index value.</param>
        /// <param name="asServer">Is this being called as the server.</param>
        private void OnIndexChanged(int oldValue, int newValue, bool asServer)
        {
            _lobbyManager.UpdatePlayerSlot(this, oldValue);

            this.name = $"Player_{newValue}";
        }

        /// <summary>
        /// [ServerRPC] Called when a client has a local lobby player spawn but doesn't have an input device to bind to it.
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void InputBindingFailed()
        {
            //Couldn't bind to an input, despawns the player to clear the lobby.
            base.Despawn();
        }

        /// <summary>
        /// Links the lobby player to a new game player object.
        /// </summary>
        /// <param name="player">The game player to link to.</param>
        public void BindToGamePlayer(NetworkGamePlayer player)
        {
            _gamePlayer = player;
        }

        /// <summary>
        /// [SyncVar Hook] Called when the linked game player changes.
        /// </summary>
        /// <param name="oldValue">The previous game player linked to.</param>
        /// <param name="newValue">The new game player linked to.</param>
        /// <param name="asServer">Is this being called as the server.</param>
        private void OnGamePlayerChanged(NetworkGamePlayer oldValue, NetworkGamePlayer newValue, bool asServer)
        {
            if (asServer)
                return;

            GamePlayerChangedEvent?.Invoke(oldValue, newValue);
        }

        /// <summary>
        /// Gets all the lobby players for a specifc connection.
        /// </summary>
        /// <param name="conn">The connection to get the players for.</param>
        /// <returns>A list of lobby players that belong to the connection specified.</returns>
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