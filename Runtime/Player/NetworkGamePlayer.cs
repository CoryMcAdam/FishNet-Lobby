using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CMDev.Lobby
{
    public class NetworkGamePlayer : NetworkBehaviour
    {
        [SyncVar]
        public int Index = -1;

        public static event Action<NetworkGamePlayer> PlayerAddedEvent;
        public static event Action<NetworkGamePlayer> PlayerRemovedEvent;

        public static List<NetworkGamePlayer> AllPlayers;


        private void Awake()
        {
            Debug.Log($"Player ? spawned at {transform.position}", this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            Debug.Log($"Player {Index} spawned on server at {transform.position}", this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Debug.Log($"Player {Index} started on client", this);

            AddPlayer();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Debug.Log($"Player {Index} started on server", this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            RemovePlayer();
        }

        private void AddPlayer()
        {
            if (AllPlayers == null)
                AllPlayers = new List<NetworkGamePlayer>();

            if (!AllPlayers.Contains(this))
            {
                AllPlayers.Add(this);
                PlayerAddedEvent?.Invoke(this);
            }
        }

        private void RemovePlayer()
        {
            if (AllPlayers == null)
                return;

            if (AllPlayers.Contains(this))
            {
                AllPlayers.Remove(this);
                PlayerRemovedEvent?.Invoke(this);
            }
        }
    }
}