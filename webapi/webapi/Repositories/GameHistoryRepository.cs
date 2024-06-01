using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.Models.Data;

namespace webapi.Repositories;

public class GameHistoryRepository(AppDbContext db, ILogger<GameHistoryRepository> logger)
{
	private readonly AppDbContext db = db;
	private readonly ILogger<GameHistoryRepository> logger = logger;

	public async Task<Dictionary<string, List<GameHistory>>> GetByUserPublicIdAsync(string userPublicID)
	{
		var games = Enum.GetNames<GameNames>();
		var dict = new Dictionary<string, List<GameHistory>>(games.Length);

		foreach (var gameName in games)
		{
			var history = await GetByUserPublicIdAsync(userPublicID, gameName);
			dict[gameName] = history;
		}

		return dict;
	}

	public async Task<List<GameHistory>> GetByUserPublicIdAsync(string userPublicID, string gameName)
	{
		return await db.GamePlayers
			.Include(x => x.User).Where(x => x.User.PublicID == userPublicID)
			.Include(x => x.GameHistory)
			.Include(x => x.GameHistory.Game).Where(x => x.GameHistory.Game.Name == gameName)
			.Include(x => x.GameHistory.GamePlayers).ThenInclude(x => x.User)
			.Select(x => x.GameHistory)
			.OrderByDescending(x => x.DateTimeStart)
			.ToListAsync();
	}

	public async Task<bool> AddAsync(GameHistory history)
	{
		try
		{
			await db.GameHistories.AddAsync(history);
			return await db.SaveChangesAsync() > 0;
		}
		catch (Exception e)
		{
			logger.LogWarning("Could not add game history to DB. Error: {error}", e.Message);
			return false;
		}
	}

	public async Task<bool> AddHistoryPlayerAsync(GamePlayer player)
	{
		try
		{
			await db.GamePlayers.AddAsync(player);
			return await db.SaveChangesAsync() > 0;
		}
		catch (Exception e)
		{
			logger.LogWarning("Could not add game history player to DB. Error: {error}", e.Message);
			return false;
		}
	}
}
