using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CMDev.Networking.Lobby
{
    /// <summary>
    /// Represents a game player object for gameplay scenes.
    /// </summary>
    public class NetworkGamePlayer : NetworkBehaviour
    {
        //PRIVATE FIELDS.
        private static List<NetworkGamePlayer> _all = new List<NetworkGamePlayer>();

        //SYNCED FIELDS.
        [SyncVar]
        private int _index = -1;

        #region PROPERTIES
        /// <summary>
        /// The index of the game player, should match the index of the linked lobby player.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// A ready only list of all current game players.
        /// </summary>
        public static ReadOnlyCollection<NetworkGamePlayer> All { get { return _all.AsReadOnly(); } }
        #endregion

        #region EVENTS
        /// <summary>
        /// Called when a game player is created.
        /// <para>Passes the game player that was added.</para>
        /// </summary>
        public static event Action<NetworkGamePlayer> PlayerAddedEvent;

        /// <summary>
        /// Called when a game player is destroyed.
        /// <para>Passes the game player that was removed.</para>
        /// </summary>
        public static event Action<NetworkGamePlayer> PlayerRemovedEvent;
        #endregion

        #region MonoBehaviour
        private void Awake()
        {
            AddPlayer();
        }

        private void OnDestroy()
        {
            RemovePlayer();
        }
        #endregion

        /// <summary>
        /// Adds the player to the all players list and invokes an event.
        /// </summary>
        private void AddPlayer()
        {
            if (!_all.Contains(this))
            {
                _all.Add(this);
                PlayerAddedEvent?.Invoke(this);
            }
        }

        /// <summary>
        /// Removes the player from the all players list and invokes an event.
        /// </summary>
        private void RemovePlayer()
        {
            if (_all.Contains(this))
            {
                _all.Remove(this);
                PlayerRemovedEvent?.Invoke(this);
            }
        }

        /// <summary>
        /// [Server] Sets the index of the game player.
        /// </summary>
        /// <param name="value">The new index.</param>
        [Server]
        public void SetIndex(int value)
        {
            _index = value;
        }
    }
}