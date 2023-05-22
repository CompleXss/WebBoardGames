using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Repositories;

public class CheckersUserRepository
{
	private readonly AppDbContext context;

	public CheckersUserRepository(AppDbContext context)
	{
		this.context = context;
	}

	public async Task<CheckersUser?> GetUserInfo(long userID) => await context.CheckersUsers.FindAsync(userID);
	public async Task<List<CheckersUser>> GetAllUsersInfo() => await context.CheckersUsers.Include(x => x.User).ToListAsync();

	public async Task<List<CheckersUser>> GetTopUsersInfo(int count)
	{
		return await context.CheckersUsers
			.Include(x => x.User)
			.OrderByDescending(x => x.PlayCount)
			.Take(count)
			.ToListAsync();
	}


	public async Task<bool> AddStatsToUserInfo(long userID, long playCountToAdd, long winCountToAdd)
	{
		try
		{
			var info = await GetUserInfo(userID);
			bool add = info is null;

			info ??= new CheckersUser()
			{
				UserId = userID,
				PlayCount = 0,
				WinCount = 0,
			};

			info.PlayCount += playCountToAdd;
			info.WinCount += winCountToAdd;

			if (add)
				await context.AddAsync(info);

			return await context.SaveChangesAsync() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task<bool> UpdateUserStatsAsync(long userID, long playCount, long winCount)
	{
		try
		{
			var info = await GetUserInfo(userID);
			bool add = info is null;

			info ??= new CheckersUser()
			{
				UserId = userID,
			};

			info.PlayCount = playCount;
			info.WinCount = winCount;

			if (add)
				await context.CheckersUsers.AddAsync(info);

			return await context.SaveChangesAsync() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task<bool> DeleteUserInfo(long userID)
	{
		var userInfo = await GetUserInfo(userID);
		return await DeleteAsync(userInfo);
	}

	private async Task<bool> DeleteAsync(CheckersUser? infoToRemove)
	{
		if (infoToRemove == null)
			return false;

		context.CheckersUsers.Remove(infoToRemove);
		return await context.SaveChangesAsync() >= 1;
	}
}
