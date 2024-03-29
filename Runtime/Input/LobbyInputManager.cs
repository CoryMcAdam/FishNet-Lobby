using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CMDev.Networking.Lobby.Input
{
    /// <summary>
    /// Handles local inputs for joining and leaving the lobby. Spawns a local object to represent an input device and helps bind it to a lobby player.
    /// </summary>
    public class LobbyInputManager : MonoBehaviour
    {
        //EDITOR FIELDS.
        [Header("Joining")]
        [SerializeField] private InputActionProperty joinAction;

        [Header("Prefabs")]
        [SerializeField] private GameObject localInputPrefab;

        //PRIVATE FIELDS.
        private NetworkLobbyManager _lobbyManager;
        private List<InputDevice> _pendingDevices = new List<InputDevice>(); //A list of all devices waiting for a network object to bind to.

        #region MonoBehaviour

        private void Awake()
        {
            _lobbyManager = GetComponent<NetworkLobbyManager>();

            joinAction.action.performed += JoinAction_Performed;

            _lobbyManager.StartedEvent += LobbyManager_Started;
            _lobbyManager.CreatePlayerFailedEvent += LobbyManager_CreatePlayerFailed;

            _lobbyManager.PlayerAddedEvent += LobbyManager_OnPlayerAdded;
            _lobbyManager.PlayerRemovedEvent += LobbyManager_OnPlayerRemoved;
        }

        private void OnDestroy()
        {
            joinAction.action.performed -= JoinAction_Performed;

            if (_lobbyManager != null)
            {
                _lobbyManager.StartedEvent -= LobbyManager_Started;
                _lobbyManager.CreatePlayerFailedEvent -= LobbyManager_CreatePlayerFailed;

                _lobbyManager.PlayerAddedEvent -= LobbyManager_OnPlayerAdded;
                _lobbyManager.PlayerRemovedEvent -= LobbyManager_OnPlayerRemoved;
            }
        }

        private void Start()
        {
            joinAction.action.Enable();
        }

        #endregion

        /// <summary>
        /// Called when the lobby manager starts. Joins the primary input to the lobby.
        /// </summary>
        /// <param name="asServer">Was the lobby started as server.</param>
        private void LobbyManager_Started(bool asServer)
        {
            if (asServer)
                return;

            //When the client connects and loads the lobby manager, create the first player.
            JoinLocalPlayer(InputManager.PrimaryInput);
        }

        /// <summary>
        /// Called when a join action is performed by any device.
        /// </summary>
        /// <param name="context">Context for the performed action such as the device that performed it.</param>
        private void JoinAction_Performed(InputAction.CallbackContext context)
        {
            Debug.Log("Join action performed.", this);
            JoinLocalPlayerIfNotAlreadyJoined(context);
        }

        /// <summary>
        /// Joins a new device if the device isn't already paired or queued.
        /// </summary>
        private void JoinLocalPlayerIfNotAlreadyJoined(InputAction.CallbackContext context)
        {
            if (!CanPlayerJoin())
                return;

            InputDevice device = context.control.device;

            if (PlayerInput.FindFirstPairedToDevice(device) != null)
                return;

            JoinLocalPlayer(device);
        }

        /// <summary>
        /// Checks that adding a player is currently allowed.
        /// </summary>
        /// <returns>True if player is allowed to join.</returns>
        private bool CanPlayerJoin()
        {
            //No prefab.
            if (localInputPrefab == null)
                return false;

            //No lobby.
            if (NetworkLobbyManager.Instance == null)
                return false;

            //No space in lobby.
            if (!NetworkLobbyManager.Instance.CanJoin)
                return false;

            return true;
        }

        /// <summary>
        /// Tries to join the lobby for a local input device.
        /// </summary>
        /// <param name="device">The input device to join as.</param>
        private void JoinLocalPlayer(InputDevice device)
        {
            RequestPlayerForDevice(device);
        }

        /// <summary>
        /// Requests player spawn if device is successfully added to queue.
        /// </summary>
        /// <param name="device">The input device to request a player for.</param>
        private void RequestPlayerForDevice(InputDevice device)
        {
            if (AddPendingDeviceToQueue(device))
            {
                RequestPlayerSpawn();
            }
        }

        /// <summary>
        /// Adds a pending device to the queue.
        /// </summary>
        /// <param name="device">The input device to add to the queue.</param>
        /// <returns>True if device was added to the queue.</returns>
        private bool AddPendingDeviceToQueue(InputDevice device)
        {
            if (_pendingDevices.Contains(device))
            {
                Debug.LogWarning("Device already pending.", this);
                return false;
            }

            Debug.Log("Added pending device to queue.", this);
            _pendingDevices.Add(device);

            return true;
        }

        /// <summary>
        /// Asks the lobby manager to spawn a new lobby player.
        /// </summary>
        private void RequestPlayerSpawn()
        {
            _lobbyManager.CreateLobbyPlayer();
        }

        /// <summary>
        /// Removes a pending device from the queue.
        /// </summary>
        /// <param name="device">The input device to remove from the queue.</param>
        /// <returns>True if device was removed successfully.</returns>
        private bool RemovePendingDeviceFromQueue(InputDevice device)
        {
            if (!_pendingDevices.Contains(device))
            {
                Debug.LogWarning("No pending device with id found.", this);
                return false;
            }

            Debug.Log("Removed pending device from queue.", this);
            _pendingDevices.Remove(device);

            return true;
        }

        /// <summary>
        /// Called when the lobby manager fails to create a lobby player for the local client. Removes the first pending device from the list.
        /// </summary>
        private void LobbyManager_CreatePlayerFailed()
        {
            if (_pendingDevices.Count <= 0)
            {
                Debug.LogWarning("Failed to create lobby player but no devices pending anyway.", this);
                return;
            }

            _pendingDevices.RemoveAt(0);
        }

        /// <summary>
        /// Called when a new player is added to the lobby. Checks if owner is local and then creates an input for it.
        /// </summary>
        /// <param name="player">The lobby player added to the lobby.</param>
        private void LobbyManager_OnPlayerAdded(NetworkLobbyPlayer player)
        {
            if (player.Owner.IsLocalClient)
            {
                CreateLocalInputForPlayer(player);
            }
        }

        /// <summary>
        /// Creates a local player input object for lobby player using the first pending device in the list.
        /// </summary>
        /// <param name="player">The lobby player to create an input for.</param>
        private void CreateLocalInputForPlayer(NetworkLobbyPlayer player)
        {
            Debug.Log("Trying to create local input for player", this);

            if (_pendingDevices.Count <= 0)
            {
                Debug.LogWarning("Pending device to bind player to was not found!", this);

                player.InputBindingFailed();

                return;
            }

            //Get next device in list.
            InputDevice device = _pendingDevices[0];

            Debug.Log("Spawning local input handler for player.", this);

            //Spawn local input.
            PlayerInput localInput = PlayerInput.Instantiate(localInputPrefab, pairWithDevice: device);

            //Set the parent to the lobby player gameobject. When the lobby player is removed, the input is cleared with it.
            localInput.transform.SetParent(player.transform);

            ILobbyInputHandler inputHandler = localInput.gameObject.GetComponent<ILobbyInputHandler>();
            inputHandler.BindToLobbyPlayer(player);

            //Remove pending device from queue.
            _pendingDevices.Remove(device);
        }

        /// <summary>
        /// Called when a new player is removed from the lobby.
        /// </summary>
        /// <param name="player">The lobby player removed from the lobby.</param>
        private void LobbyManager_OnPlayerRemoved(NetworkLobbyPlayer player)
        {

        }
    }
}