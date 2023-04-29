using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Repositories;

public class CheckersHistoryRepository
{
	private readonly AppDbContext context;

	public CheckersHistoryRepository(AppDbContext context)
	{
		this.context = context;
	}

	public async Task<List<CheckersHistory>> GetAsync(long userId)
		=> await context.CheckersHistories.Where(x => x.UserId == userId).ToListAsync();

	public async Task<bool> AddAsync(PlayHistoryDto historyDto)
	{
		await context.CheckersHistories.AddAsync(new CheckersHistory
		{
			UserId = historyDto.UserId,
			IsWin = Convert.ToInt64(historyDto.IsWin),
			DateTimeStart = historyDto.DateTimeStart.ToString(AppDbContext.DATETIME_STRING_FORMAT),
			DateTimeEnd = historyDto.DateTimeEnd.ToString(AppDbContext.DATETIME_STRING_FORMAT)
		});

		return await context.SaveChangesAsync() > 0;
	}
}
