namespace webapi.Hubs;

public interface ICheckersLobbyHub
{
	Task GameStarted();
	Task LobbyClosed();
	Task UserConnected(string userPublicID);
	Task UserDisconnected(string userPublicID);
}
