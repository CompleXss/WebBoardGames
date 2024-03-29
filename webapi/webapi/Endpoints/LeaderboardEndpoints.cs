using webapi.Repositories;

namespace webapi.Endpoints;

public static class LeaderboardEndpoints
{
	public static void MapLeaderboardEndpoints(this WebApplication app)
	{
		app.MapGet("/leaderboard", GetLeaderboardAsync).AllowAnonymous();
	}

	internal static async Task<IResult> GetLeaderboardAsync(HttpContext context, UserGameStatisticsRepository gameStatsRepo)
	{
		var leaderboard = await gameStatsRepo.GetTopUsersInfoForEveryGame(10);
		return Results.Ok(leaderboard);
	}
}
