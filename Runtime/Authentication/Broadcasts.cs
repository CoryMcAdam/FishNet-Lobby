using FishNet.Broadcast;

namespace CMDev.Networking.Lobby.Authentication
{
    /// <summary>
    /// Broadcast used for host authentication.
    /// </summary>
    public struct HostPasswordBroadcast : IBroadcast
    {
        public string Password;
    }

    /// <summary>
    /// Empty broadcast used for lobby authentication.
    /// </summary>
    public struct LobbyBroadcast : IBroadcast { }
}