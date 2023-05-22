using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class PlayHistoryEndpoint
{
	public static void MapPlayHistoryEndpoints(this WebApplication app)
	{
		app.MapGet("/history", GetUserHistoryAsync);
	}

	internal static async Task<IResult> GetUserHistoryAsync(HttpContext context, CheckersHistoryRepository checkersHistoryRepo)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		var checkersHistory = await checkersHistoryRepo.GetAsync(user.ID);
		// other histories...

		return Results.Ok(new
		{
			userID = user.ID,
			history = new
			{
				Checkers = checkersHistory,
			}
		});
	}
}
