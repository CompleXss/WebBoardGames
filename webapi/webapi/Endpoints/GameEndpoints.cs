using webapi.Hubs;
using webapi.Services;
using webapi.Services.Checkers;

namespace webapi.Endpoints;

public static class GameEndpoints
{
	public static void MapGameEndpoints(this WebApplication app)
	{
		// lobbies
		app.MapHub<CheckersLobbyHub>("/lobby/checkers");

		// is in game checks
		app.MapGet("/isInGame/checkers", IsInCheckersGameAsync);

		// games
		app.MapHub<CheckersGameHub>("/play/checkers");
	}

	internal static async Task<IResult> IsInCheckersGameAsync(HttpContext context, CheckersGameService gameService)
	{
		var userTokenInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userTokenInfo is null) return Results.Unauthorized();

		var activeGame = gameService.GetUserGame(userTokenInfo.PublicID);
		return Results.Ok(activeGame is not null);
	}
}
