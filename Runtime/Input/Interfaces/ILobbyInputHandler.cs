namespace CMDev.Networking.Lobby.Input
{
    public interface ILobbyInputHandler
    {
        public void BindToLobbyPlayer(NetworkLobbyPlayer player);
    }
}