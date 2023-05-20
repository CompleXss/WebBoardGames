using Microsoft.AspNetCore.Authentication;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class PlayHistoryEndpoint
{
	public static void MapPlayHistoryEndpoints(this WebApplication app)
	{
		app.MapGet("/history", GetUserHistory);
		app.MapPost("/history", AddUserHistory);
	}

	internal static async Task<IResult> GetUserHistory(HttpContext context, CheckersHistoryRepository history)
	{
		var accessToken = await context.GetTokenAsync(AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME);
		if (accessToken is null) return Results.Unauthorized();

		var userID = AuthService.GetUserInfoFromAccessToken(accessToken).ID;

		var checkersHistory = await history.GetAsync(userID);
		// other histories...

		return Results.Ok(new
		{
			Checkers = checkersHistory
		});
	}

	internal static async Task<IResult> AddUserHistory(CheckersHistoryRepository history, PlayHistoryDto historyDto)
	{
		return historyDto.Game switch
		{
			Games.Checkers => await history.AddAsync(historyDto)
								? Results.Ok() // TODO: Results.Created ??
								: Results.Problem("Can not add user history"),

			_ => Results.Problem($"Can not add '{historyDto.Game}' history."),
		};
	}
}
