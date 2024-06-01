using webapi.Extensions;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class PlayHistoryEndpoint
{
	public static void MapPlayHistoryEndpoints(this WebApplication app)
	{
		app.MapGet("/history", GetUserHistoryAsync);
		app.MapGet("/history/{gameName}", GetUserHistoryForGameAsync);
	}

	internal static async Task<IResult> GetUserHistoryForGameAsync(HttpContext context, GameHistoryRepository gameHistoryRepo, string gameName)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var result = new Dictionary<string, IEnumerable<GameHistoryDto>>(1);
		var history = await gameHistoryRepo.GetByUserPublicIdAsync(userInfo.PublicID, gameName);
		result[gameName] = history.ToDto();

		return Results.Ok(new
		{
			userID = userInfo.PublicID,
			history = result
		});
	}

	internal static async Task<IResult> GetUserHistoryAsync(HttpContext context, GameHistoryRepository gameHistoryRepo)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var history = await gameHistoryRepo.GetByUserPublicIdAsync(userInfo.PublicID);
		var result = new Dictionary<string, IEnumerable<GameHistoryDto>>(history.Count);

		foreach (var item in history)
			result[item.Key] = item.Value.ToDto();

		return Results.Ok(new
		{
			userID = userInfo.PublicID,
			history = result
		});
	}
}
