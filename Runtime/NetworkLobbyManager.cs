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

        //EVENTS.
        public static event Action<NetworkLobbyManager> SpawnedEvent; //Called when any lobby is spawned for the first time, passes the started lobby script.

        public event Action<bool> StartedEvent;
        public event Action StoppedEvent;

        public event Action<NetworkLobbyPlayer> PlayerAddedEvent;
        public event Action<NetworkLobbyPlayer> PlayerRemovedEvent;

        public event Action CreatePlayerFailedEvent;

        //PROPERTIES.
        public int PlayerCount { get { return _lobbySlots.Count(s => s != null); } } //How many lobby slots contain a lobby player.
        public bool IsLobbyFull { get { return (PlayerCount + _reseveredPlayers.Count) >= LobbySettings.MAX_PLAYERS; } }
        public bool CanJoin { get { return !IsLobbyFull; } }

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

        [Server]
        public void ReservePlayerSlot(NetworkConnection conn)
        {
            if (!_reseveredPlayers.Contains(conn))
                _reseveredPlayers.Add(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateLobbyPlayer(NetworkConnection conn = null)
        {
            CreateLobbyPlayerInternal(conn);
        }

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

        [TargetRpc]
        private void CreatePlayerFailed(NetworkConnection conn)
        {
            CreatePlayerFailedEvent?.Invoke();
        }

        public void AddPlayer(NetworkLobbyPlayer player)
        {
            if (_lobbySlots[player.Index] == null)
                _lobbySlots[player.Index] = player;

            PlayerAddedEvent?.Invoke(player);
        }

        public void RemovePlayer(NetworkLobbyPlayer player)
        {
            if (_lobbySlots[player.Index] == player)
                _lobbySlots[player.Index] = null;

            PlayerRemovedEvent?.Invoke(player);
        }

        public void UpdatePlayerSlot(NetworkLobbyPlayer player, int oldIndex)
        {
            if (oldIndex > 0 && oldIndex < _lobbySlots.Length)
            {
                if (_lobbySlots[oldIndex] == player)
                    _lobbySlots[oldIndex] = null;
            }

            _lobbySlots[player.Index] = player;
        }

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