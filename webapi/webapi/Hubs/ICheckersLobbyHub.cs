namespace webapi.Hubs;

public interface ICheckersLobbyHub
{
	Task GameStarted();
	Task LobbyClosed();
	Task UserConnected(long userID);
	Task UserDisconnected(long userID);
}
