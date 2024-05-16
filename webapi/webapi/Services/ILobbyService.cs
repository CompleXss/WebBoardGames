using webapi.Models;

namespace webapi.Services;

public interface ILobbyService
{
	Lobby? GetUserLobby(string userID);
	Task<Lobby?> TryCreateLobbyAsync(string hostID, string connectionID);
	Task<(Lobby? lobby, IResult errorResult)> TryEnterLobbyAsync(string userID, string connectionID, string lobbyKey);
	Task<string?> LeaveLobby(string userID, string connectionID);
	Task CloseLobby(Lobby lobby, bool notifyUsers);
}
