using webapi.Models;

namespace webapi.Services;

public interface ILobbyService
{
	LobbyInfo? GetUserLobbyInfo(string userID);
	IEnumerable<LobbyInfo> GetAllPublicLobbies();
	Task<LobbyInfo?> TryCreateLobbyAsync(string hostID, string connectionID);
	Task<(LobbyInfo? lobby, IResult errorResult)> TryEnterLobbyAsync(string userID, string connectionID, string lobbyKey);
	Task<string?> LeaveLobby(string userID, string connectionID);
	bool MakeLobbyPublic(string lobbyKey, bool value);
	bool SetLobbySettings(string lobbyKey, object? settings);
	Task CloseLobby(string lobbyKey, bool notifyUsers);
}
