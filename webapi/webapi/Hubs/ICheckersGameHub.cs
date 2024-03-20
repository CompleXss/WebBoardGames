namespace webapi.Hubs;

public interface ICheckersGameHub
{
	Task NotAllowed();
	Task GameStateChanged();
	Task UserReconnected(long userID);
	Task UserDisconnected(long userID);
}
