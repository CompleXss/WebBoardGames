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
		=> await context.CheckersHistories.Include(x => x.Winner).Include(x => x.Looser).Where(x => x.WinnerId == userId || x.LooserId == userId).ToListAsync();

	public async Task<bool> AddAsync(CheckersHistory history)
	{
		try
		{
			await context.CheckersHistories.AddAsync(history);
			return await context.SaveChangesAsync() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
