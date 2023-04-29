using webapi.Models;
using webapi.Repositories;

namespace webapi.Endpoints;

public static class PlayHistoryEndpoint
{
	public static void MapPlayHistoryEndpoints(this WebApplication app)
	{
		app.MapGet("/history/{userId}", GetUserHistory).AllowAnonymous();
		app.MapPost("/history", AddUserHistory);
	}

	internal static async Task<IResult> GetUserHistory(CheckersHistoryRepository history, long userId)
	{
		var checkersHistory = await history.GetAsync(userId);
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
