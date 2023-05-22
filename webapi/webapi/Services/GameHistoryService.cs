using webapi.Models;
using webapi.Repositories;
using webapi.Data;

namespace webapi.Services;

public class GameHistoryService
{
	private readonly AppDbContext db;
	private readonly CheckersHistoryRepository checkersHistoryRepo;
	private readonly CheckersUserRepository userStatsRepo;

	public GameHistoryService(AppDbContext db, CheckersHistoryRepository checkersHistoryRepo, CheckersUserRepository userStatsRepo)
	{
		this.db = db;
		this.checkersHistoryRepo = checkersHistoryRepo;
		this.userStatsRepo = userStatsRepo;
	}



	public async Task<bool> AddAsync(PlayHistoryDto playHistory)
	{
		using var transaction = await db.Database.BeginTransactionAsync();
		bool succeeded;

		var task1 = userStatsRepo.AddStatsToUserInfo(playHistory.WinnerId, 1, 1);
		var task2 = userStatsRepo.AddStatsToUserInfo(playHistory.LooserID, 1, 0);

		switch (playHistory.Game)
		{
			case Games.Checkers:
				var task3 = AddCheckersGameAsync(playHistory);
				succeeded = (await Task.WhenAll(task1, task2, task3)).All(x => x);
				break;

			default:
				succeeded = (await Task.WhenAll(task1, task2)).All(x => x);
				break;
		}

		if (succeeded)
			await transaction.CommitAsync();

		return succeeded;
	}

	private async Task<bool> AddCheckersGameAsync(PlayHistoryDto historyDto)
	{
		var checkersHistory = new CheckersHistory
		{
			WinnerId = historyDto.WinnerId,
			LooserId = historyDto.LooserID,
			DateTimeStart = historyDto.DateTimeStart.ToString(AppDbContext.DATETIME_STRING_FORMAT),
			DateTimeEnd = historyDto.DateTimeEnd.ToString(AppDbContext.DATETIME_STRING_FORMAT)
		};

		return await checkersHistoryRepo.AddAsync(checkersHistory);
	}
}
