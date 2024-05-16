using webapi.Games.Checkers;
using webapi.Hubs;
using webapi.Models;
using webapi.Services;

namespace webapi.Endpoints;

public static class GameEndpoints
{
	public static void MapGameEndpoints(this WebApplication app)
	{
		// lobbies
		app.MapHub<LobbyHub<CheckersGame>>("/lobby/checkers");

		// games
		app.MapHub<GameHub<CheckersGame>>("/play/checkers");

		// is in game checks
		app.MapGet("/isInGame/checkers", IsInGameAsync<CheckersGame>);
	}

	internal static async Task<IResult> IsInGameAsync<TGame>(HttpContext context, GameService<TGame> gameService)
		where TGame : PlayableGame
	{
		var userTokenInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userTokenInfo is null) return Results.Unauthorized();

		var activeGame = gameService.GetUserGame(userTokenInfo.PublicID);
		return Results.Ok(activeGame is not null);
	}
}
