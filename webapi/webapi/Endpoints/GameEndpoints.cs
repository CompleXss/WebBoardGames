using webapi.Games;
using webapi.Games.Checkers;
using webapi.Games.Monopoly;
using webapi.Hubs;
using webapi.Models;
using webapi.Services;

namespace webapi.Endpoints;

public static class GameEndpoints
{
	public static void MapGameEndpoints(this WebApplication app)
	{
		app.MapHub<LobbyListHub>("/lobbyList");

		MapGame<CheckersGame>(app, GameNames.checkers);
		MapGame<MonopolyGame>(app, GameNames.monopoly);
	}

	internal static void MapGame<TGame>(WebApplication app, GameNames gameName)
		where TGame : PlayableGame
	{
		app.MapHub<LobbyHub<TGame>>("/lobby/" + gameName);
		app.MapHub<GameHub<TGame>>("/play/" + gameName);
		app.MapGet("/isInGame/" + gameName, IsInGameAsync<TGame>);
	}

	internal static async Task<IResult> IsInGameAsync<TGame>(HttpContext context, GameService<TGame> gameService)
		where TGame : PlayableGame
	{
		var userTokenInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userTokenInfo is null) return Results.Unauthorized();

		bool isInGame = gameService.IsUserInGame(userTokenInfo.PublicID);
		return Results.Ok(isInGame);
	}
}
