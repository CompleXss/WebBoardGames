using webapi.Models;
using webapi.Repositories;
using webapi.Data;

namespace webapi.Services;

public class GameHistoryService
{
	private readonly AppDbContext db;
	private readonly GameHistoryRepository gameHistoryRepo;
	private readonly GamesRepository gamesRepository;

	public GameHistoryService(AppDbContext db, GameHistoryRepository gameHistoryRepo, GamesRepository gamesRepository)
	{
		this.db = db;
		this.gameHistoryRepo = gameHistoryRepo;
		this.gamesRepository = gamesRepository;
	}



	public async Task<bool> AddAsync(GameHistoryDto history)
	{
		using var transaction = await db.Database.BeginTransactionAsync();
		string gameName = history.Game.ToString();
		var tasks = new List<Task<bool>>(2 * (history.Winners.Length + history.Loosers.Length));

		try
		{
			// create history entry
			var gameID = await gamesRepository.GetIdByNameAsync(gameName);
			var historyEntry = new GameHistory()
			{
				GameID = gameID,
				DateTimeStart = history.DateTimeStart,
				DateTimeEnd = history.DateTimeEnd
			};
			await gameHistoryRepo.AddAsync(historyEntry);
			await db.SaveChangesAsync();



			// add players to history
			tasks.AddRange(history.Winners.Select(winner => gameHistoryRepo.AddHistoryPlayerAsync(new GamePlayer
			{
				UserID = winner.ID,
				GameHistory = historyEntry,
				IsWinner = true,
			})));

			tasks.AddRange(history.Loosers.Select(looser => gameHistoryRepo.AddHistoryPlayerAsync(new GamePlayer
			{
				UserID = looser.ID,
				GameHistory = historyEntry,
				IsWinner = false,
			})));



			var results = await Task.WhenAll(tasks);
			bool succeeded = results.All(x => x);

			if (succeeded)
				await transaction.CommitAsync();

			return succeeded;
		}
		catch (Exception)
		{
			await transaction.RollbackAsync();
			return false;
		}
	}
}
