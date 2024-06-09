namespace webapi.Hubs;

public interface IGameHub
{
	Task NotAllowed();
	Task GameStateChanged();
	Task GameClosed(string? winnerID);
	Task UserReconnected(string userPublicID);
	Task UserDisconnected(string userPublicID);
	Task TurnTimerTicked(int secondsLeft);
}
