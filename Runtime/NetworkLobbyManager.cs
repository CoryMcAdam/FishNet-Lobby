using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CMDev.Networking.Lobby
{
    /// <summary>
    /// A networked lobby manager, manages adding and removing players for different connections.
    /// </summary>
    public class NetworkLobbyManager : NetworkBehaviour
    {
        public static NetworkLobbyManager Instance { get { return _instance; } }
        private static NetworkLobbyManager _instance;

        //EDITOR FIELDS.
        [Header("Player")]
        [SerializeField] private NetworkLobbyPlayer lobbyPlayerPrefab;

        //PRIVATE FIELDS.
        [Header("Lobby Slots")]
        private NetworkLobbyPlayer[] _lobbySlots; //Array of slots for players to join. Array should stay the same size.
        private List<NetworkConnection> _reseveredPlayers; //List of connecting players. For avoiding situations where player has connected but not loaded lobby yet.
        private NetworkManager _networkManager;

        #region EVENTS
        /// <summary>
        /// Called when a new network lobby manager is started on the network.
        /// <para>Passes the "NetworkLobbyManager" that spawned.</para>
        /// </summary>
        public static event Action<NetworkLobbyManager> SpawnedEvent;

        /// <summary>
        /// Called when the network lobby manager starts on the server or client.
        /// <para>Passes "true" if the event was called by the server, "false" if called by the client.</para>
        /// </summary>
        public event Action<bool> StartedEvent;

        /// <summary>
        /// Called when the network lobby stops on the network.
        /// </summary>
        public event Action StoppedEvent;

        /// <summary>
        /// Called when a lobby player is added to the lobby.
        /// <para>Passes the "NetworkLobbyPlayer" that was added.</para>
        /// </summary>
        public event Action<NetworkLobbyPlayer> PlayerAddedEvent;

        /// <summary>
        /// Called when a lobby player is removed from the lobby.
        /// <para>Passes the "NetworkLobbyPlayer" that was removed.</para>
        /// </summary>
        public event Action<NetworkLobbyPlayer> PlayerRemovedEvent;

        /// <summary>
        /// Called when a create player request has failed to create a player for the local connection.
        /// </summary>
        public event Action CreatePlayerFailedEvent;
        #endregion

        #region PROPERTIES
        /// <summary>
        /// Returns the amount of filled lobby slots.
        /// </summary>
        public int PlayerCount { get { return _lobbySlots.Count(s => s != null); } } //How many lobby slots contain a lobby player.

        /// <summary>
        /// Checks if the current player count + reserved slots is equal to the max players count.
        /// </summary>
        public bool IsLobbyFull { get { return (PlayerCount + _reseveredPlayers.Count) >= LobbySettings.MAX_PLAYERS; } }

        /// <summary>
        /// Returns if a new player or connection can join the lobby.
        /// </summary>
        public bool CanJoin { get { return !IsLobbyFull; } }
        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (_instance == null)
                _instance = this;

            //Get other components.
            _networkManager = InstanceFinder.NetworkManager;

            //Initialise lists/arrays.
            _lobbySlots = new NetworkLobbyPlayer[LobbySettings.MAX_PLAYERS];
            _reseveredPlayers = new List<NetworkConnection>();
        }

        #endregion

        #region NetworkBehaviour

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            Debug.Log("New lobby spawned.", this);
            SpawnedEvent?.Invoke(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Debug.Log("Lobby started on server.", this);
            StartedEvent?.Invoke(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Debug.Log("Lobby started on client.", this);
            StartedEvent?.Invoke(false);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            Debug.Log("Lobby stopped.", this);
            StoppedEvent?.Invoke();
        }

        #endregion

        /// <summary>
        /// [Server] Reserves a player slot for a network connection.
        /// </summary>
        /// <param name="conn">The network connection to reserve a slot for.</param>
        [Server]
        public void ReservePlayerSlot(NetworkConnection conn)
        {
            if (!_reseveredPlayers.Contains(conn))
                _reseveredPlayers.Add(conn);
        }

        /// <summary>
        /// [ServerRPC] Creates a lobby player for a network connection.
        /// </summary>
        /// <param name="conn">The network connection to create a lobby player for.</param>
        [ServerRpc(RequireOwnership = false)]
        public void CreateLobbyPlayer(NetworkConnection conn = null)
        {
            CreateLobbyPlayerInternal(conn);
        }

        /// <summary>
        /// [Server] Internal function for creating a lobby player.
        /// </summary>
        /// <param name="conn">The network connection to create a lobby player for.</param>
        [Server]
        private void CreateLobbyPlayerInternal(NetworkConnection conn = null)
        {
            //Does the client have a reserved slot? e.g. is it the first player for this connection.
            if (!_reseveredPlayers.Contains(conn))
            {
                Debug.Log("Client doesn't have a reserved player slot in the lobby.", this);

                //If the lobby is full a player can't be created.
                if (IsLobbyFull)
                {
                    Debug.Log("Lobby is full, could not create a player.", this);
                    CreatePlayerFailed(conn);
                    return;
                }
            }

            Debug.Log("Lobby has empty slot, trying to create a player.", this);

            //Get the index of the first empty player slot
            int index = GetEmptySlotIndex();

            if (index == -1)
            {
                CreatePlayerFailed(conn);
                return;
            }

            if (_reseveredPlayers.Contains(conn))
                _reseveredPlayers.Remove(conn);

            NetworkLobbyPlayer player = Instantiate(lobbyPlayerPrefab);

            //Setup player data.
            player.SetIndex(index);

            //Server assigns early to prevent too many players.
            _lobbySlots[index] = player;

            //Spawn on server.
            _networkManager.ServerManager.Spawn(player.NetworkObject, conn);
        }

        /// <summary>
        /// [TargetRPC] Triggers the CreatePlayerFailed event for a specific connection.
        /// </summary>
        /// <param name="conn">The connection to trigger the event on.</param>
        [TargetRpc]
        private void CreatePlayerFailed(NetworkConnection conn)
        {
            CreatePlayerFailedEvent?.Invoke();
        }

        /// <summary>
        /// Adds a player to the lobby slot of their index and invokes event.
        /// </summary>
        /// <param name="player">The player being added to the lobby.</param>
        public void AddPlayer(NetworkLobbyPlayer player)
        {
            if (_lobbySlots[player.Index] == null)
                _lobbySlots[player.Index] = player;

            PlayerAddedEvent?.Invoke(player);
        }

        /// <summary>
        /// Removes a player from the lobby slot of their index and invokes event.
        /// </summary>
        /// <param name="player">The player being removed from the lobby.</param>
        public void RemovePlayer(NetworkLobbyPlayer player)
        {
            if (_lobbySlots[player.Index] == player)
                _lobbySlots[player.Index] = null;

            PlayerRemovedEvent?.Invoke(player);
        }

        /// <summary>
        /// Moves the player from one lobby slot to another.
        /// </summary>
        /// <param name="player">The player changing slots.</param>
        /// <param name="oldIndex">The previous index of the player.</param>
        public void UpdatePlayerSlot(NetworkLobbyPlayer player, int oldIndex)
        {
            if (oldIndex > 0 && oldIndex < _lobbySlots.Length)
            {
                if (_lobbySlots[oldIndex] == player)
                    _lobbySlots[oldIndex] = null;
            }

            _lobbySlots[player.Index] = player;
        }

        /// <summary>
        /// Gets the index of the first empty lobby slot.
        /// </summary>
        /// <returns>The index of the first empty lobby slot, or -1 if no slot is available.</returns>
        private int GetEmptySlotIndex()
        {
            for (int i = 0; i < _lobbySlots.Length; i++)
            {
                if (_lobbySlots[i] == null)
                    return i;
            }

            return -1;
        }
    }
}