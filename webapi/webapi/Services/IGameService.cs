using webapi.Models;

namespace webapi.Services;

public interface IGameService
{
	GameNames GameName { get; }
	bool TryStartNewGame(IReadOnlyList<string> playerIDs, object? settings);
	bool IsUserInGame(string userID);
	PlayableGameInfo? GetUserGameInfo(string userID);
	object? GetRelativeGameState(string userID);
	PlayableGameInfo? ConnectPlayer(string playerID, string connectionID);
	PlayableGameInfo? DisconnectPlayer(string playerID);
	bool Surrender(string playerID);
	bool Request(string playerID, string request, object? data);
	bool TryUpdateGameState(string gameKey, string playerID, object data, out string error);
	void CloseGame(string gameKey);
}
