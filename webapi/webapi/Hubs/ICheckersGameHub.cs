namespace webapi.Hubs;

public interface ICheckersGameHub
{
	Task NotAllowed();
	Task GameStateChanged();
	Task UserReconnected(string userPublicID);
	Task UserDisconnected(string userPublicID);
}
