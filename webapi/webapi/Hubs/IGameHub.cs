namespace webapi.Hubs;

public interface IGameHub
{
	Task NotAllowed();
	Task GameStateChanged();
	Task UserReconnected(string userPublicID);
	Task UserDisconnected(string userPublicID);
}
