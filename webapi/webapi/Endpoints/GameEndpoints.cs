using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using webapi.Hubs;
using webapi.Services;

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
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		var activeGame = gameService.GetUserGame(user.ID);

		return Results.Ok(activeGame is not null);
	}
}
