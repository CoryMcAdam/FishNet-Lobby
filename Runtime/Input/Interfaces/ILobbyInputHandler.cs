namespace CMDev.Networking.Lobby.Input
{
    /// <summary>
    /// An interface to help with binding a local input to a networked lobby player.
    /// </summary>
    public interface ILobbyInputHandler
    {
        /// <summary>
        /// Binds the local input handler to a networked lobby player.
        /// </summary>
        /// <param name="player">The lobby player to bind to.</param>
        public void BindToLobbyPlayer(NetworkLobbyPlayer player);
    }
}