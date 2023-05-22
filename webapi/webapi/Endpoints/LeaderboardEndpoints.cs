using webapi.Repositories;

namespace webapi.Endpoints;

public static class LeaderboardEndpoints
{
	public static void MapLeaderboardEndpoints(this WebApplication app)
	{
		app.MapGet("/leaderboard", GetLeaderboardAsync).AllowAnonymous();
	}

	internal static async Task<IResult> GetLeaderboardAsync(HttpContext context, CheckersUserRepository checkersStatsRepo)
	{
		var checkersLeaderboard = await checkersStatsRepo.GetTopUsersInfo(10);
		// other leaderboards...

		return Results.Ok(new
		{
			Checkers = checkersLeaderboard,
		});
	}
}
