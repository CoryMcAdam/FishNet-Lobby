using FishNet.Broadcast;

namespace CMDev.Networking.Lobby.Authentication
{
    public struct HostPasswordBroadcast : IBroadcast
    {
        public string Password;
    }

    public struct LobbyBroadcast : IBroadcast { }
}