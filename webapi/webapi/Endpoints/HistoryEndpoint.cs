using webapi.Models;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class PlayHistoryEndpoint
{
	public static void MapPlayHistoryEndpoints(this WebApplication app)
	{
		app.MapGet("/history", GetUserHistoryAsync);
	}

	internal static async Task<IResult> GetUserHistoryAsync(HttpContext context, GameHistoryRepository gameHistoryRepo)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var history = await gameHistoryRepo.GetByUserPublicIdAsync(userInfo.PublicID);
		var result = new Dictionary<string, IEnumerable<GameHistoryDto>>(history.Count);

		foreach (var item in history)
			result[item.Key] = item.Value.Select(x => new GameHistoryDto
			{
				Winners = x.GamePlayers.Where(x => x.IsWinner).Select(x => x.User).ToArray(),
				Loosers = x.GamePlayers.Where(x => !x.IsWinner).Select(x => x.User).ToArray(),
				DateTimeStart = x.DateTimeStart,
				DateTimeEnd = x.DateTimeEnd,
			});

		return Results.Ok(new
		{
			userID = userInfo.PublicID,
			history = result
		});
	}
}
