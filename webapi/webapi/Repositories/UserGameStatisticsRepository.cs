using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Repositories;

public class UserGameStatisticsRepository(AppDbContext db, GamesRepository gamesRepository, ILogger<UserGameStatisticsRepository> logger)
{
	private readonly AppDbContext db = db;
	private readonly GamesRepository gamesRepository = gamesRepository;
	private readonly ILogger<UserGameStatisticsRepository> logger = logger;

	public async Task<UserGameStatistic?> GetUserInfoByIdAsync(int gameID, long userID)
	{
		return await db.UserGameStatistics
			.Where(x => x.GameID == gameID)
			.Include(x => x.Game)
			.Include(x => x.User)
			.FirstOrDefaultAsync(x => x.UserID == userID);
	}

	public async Task<UserGameStatistic?> GetUserInfoByIdAsync(string gameName, long userID)
	{
		return await db.UserGameStatistics
			.Include(x => x.Game).Where(x => x.Game.Name == gameName)
			.Include(x => x.User)
			.FirstOrDefaultAsync(x => x.UserID == userID);
	}

	public Task<List<UserGameStatistic>> GetTopUsersInfoByGameID(int gameID, int count)
	{
		return db.UserGameStatistics
			.Where(x => x.GameID == gameID)
			.Include(x => x.Game)
			.Include(x => x.User)
			.OrderByDescending(x => x.WinCount)
			.OrderBy(x => x.PlayCount)
			.Take(count)
			.ToListAsync();
	}

	public Task<List<UserGameStatistic>> GetTopUsersInfoByGameName(string gameName, int count)
	{
		return db.UserGameStatistics
			.Include(x => x.Game).Where(x => x.Game.Name == gameName)
			.Include(x => x.User)
			.OrderByDescending(x => x.WinCount)
			.OrderBy(x => x.PlayCount)
			.Take(count)
			.ToListAsync();
	}

	public async Task<Dictionary<string, List<UserGameStatistic>>> GetTopUsersInfoForEveryGame(int countForEveryGame)
	{
		var games = await db.Games.ToListAsync();
		var dict = new Dictionary<string, List<UserGameStatistic>>(games.Count * countForEveryGame);

		foreach (var game in games)
		{
			var top10 = await GetTopUsersInfoByGameID(game.ID, countForEveryGame);
			dict[game.Name] = top10;
		}

		return dict;
	}



	public async Task<bool> AddAsync(string gameName, long userID, int playCountToAdd, int winCountToAdd)
	{
		try
		{
			var info = await GetUserInfoByIdAsync(gameName, userID);
			bool needToAdd = info is null;

			if (info is null)
			{
				var gameID = await gamesRepository.GetIdByName(gameName);

				info = new UserGameStatistic
				{
					UserID = userID,
					GameID = gameID
				};
			}

			info.PlayCount += playCountToAdd;
			info.WinCount += winCountToAdd;

			if (needToAdd)
				await db.AddAsync(info);

			return await db.SaveChangesAsync() > 0;
		}
		catch (Exception e)
		{
			logger.LogWarning("Could not add game stats info to DB. Error: {error}", e.Message);
			return false;
		}
	}
}
