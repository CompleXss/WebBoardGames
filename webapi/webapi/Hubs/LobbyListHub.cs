using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Hubs;

public class LobbyListHub(IServiceProvider serviceProvider) : Hub
{
	private readonly IServiceProvider serviceProvider = serviceProvider;

	public IResult GetLobbiesForGame(string gameName)
	{
		if (!Enum.TryParse<GameNames>(gameName, out var game))
			return Results.BadRequest("Invalid game name");

		var lobbyService = serviceProvider.GetLobbyServiceForGame(game);
		if (lobbyService is null)
			return Results.BadRequest("Could not get service for given game");

		var lobbies = lobbyService.GetAllActiveLobbies();
		return Results.Ok(new { lobbies });
	}
}
