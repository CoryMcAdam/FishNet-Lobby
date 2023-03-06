using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using System;

namespace CMDev.Lobby.Authentication
{
    public class LobbyAuthenticator : HostAuthenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);

            NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

            NetworkManager.ServerManager.RegisterBroadcast<LobbyBroadcast>(OnLobbyBroadcast, false);
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState != LocalConnectionState.Started)
                return;

            //Authenticated as host, can skip lobby authentication.
            if (AuthenticateAsHost())
                return;

            LobbyBroadcast lobbyBroadcast = new LobbyBroadcast();
            NetworkManager.ClientManager.Broadcast(lobbyBroadcast);
        }

        protected override void OnHostAuthenticationResult(NetworkConnection conn, bool authenticated)
        {
            OnAuthenticationResult?.Invoke(conn, authenticated);
        }

        private void OnLobbyBroadcast(NetworkConnection conn, LobbyBroadcast broadcast)
        {
            if (conn.Authenticated)
            {
                conn.Disconnect(true);
                return;
            }

            bool canJoinLobby = NetworkLobbyManager.Instance == null ? false : NetworkLobbyManager.Instance.CanJoin;

            if (canJoinLobby)
            {
                //Reserve a player slot for the connection if authentication is allowed.
                NetworkLobbyManager.Instance.ReservePlayerSlot(conn);
            }

            OnAuthenticationResult?.Invoke(conn, canJoinLobby);
        }
    }
}