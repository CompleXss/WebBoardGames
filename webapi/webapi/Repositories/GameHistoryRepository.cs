using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models.Data;

namespace webapi.Repositories;

public class GameHistoryRepository(AppDbContext db, ILogger<GameHistoryRepository> logger)
{
	private readonly AppDbContext db = db;
	private readonly ILogger<GameHistoryRepository> logger = logger;

	public Task<List<GameHistory>> GetByUserIdAsync(long userID)
	{
		return db.GamePlayers
			.Where(x => x.UserID == userID)
			.Include(x => x.GameHistory).ThenInclude(x => x.Game)
			.Select(x => x.GameHistory)
			.ToListAsync();
	}

	public async Task<Dictionary<string, List<GameHistory>>> GetByUserPublicIdAsync(string userPublicID)
	{
		var games = await db.Games.ToListAsync();
		var dict = new Dictionary<string, List<GameHistory>>(games.Count);

		foreach (var game in games)
		{
			var history = await db.GamePlayers
				.Include(x => x.User).Where(x => x.User.PublicID == userPublicID)
				.Include(x => x.GameHistory)
				.Include(x => x.GameHistory.Game).Where(x => x.GameHistory.Game.ID == game.ID)
				.Include(x => x.GameHistory.GamePlayers).ThenInclude(x => x.User)
				.Select(x => x.GameHistory)
				.OrderByDescending(x => x.DateTimeStart)
				.ToListAsync();

			dict[game.Name] = history;
		}

		return dict;
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
