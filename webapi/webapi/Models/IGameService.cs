namespace webapi.Models;

public interface IGameService
{
	bool TryStartNewGame(IReadOnlyList<string> playerIDs, object? settings);
	PlayableGame? GetUserGame(string userID);
	object? GetRelativeGameState(string userID);
	bool TryUpdateGameState(PlayableGame game, string playerID, object data, out string error);
	void CloseGame(PlayableGame game);
}
