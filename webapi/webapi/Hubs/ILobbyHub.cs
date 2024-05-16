namespace webapi.Hubs;

public interface ILobbyHub
{
	Task GameStarted();
	Task LobbyClosed();
	Task HostChanged(string newHostID);
	Task UserConnected(string userPublicID);
	Task UserDisconnected(string userPublicID);
}
