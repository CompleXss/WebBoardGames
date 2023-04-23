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

	public async Task<CheckersUser?> GetUserInfo(long userID) => await context.CheckersUsers.FirstOrDefaultAsync(x => x.UserId == userID);
	public async Task<CheckersUser?> GetUserInfo(string username) => await context.CheckersUsers.FirstOrDefaultAsync(x => x.User.Name == username);



	public async Task<bool> DeleteUserInfo(long userID)
	{
		var userInfo = await GetUserInfo(userID);
		return await DeleteAsync(userInfo);
	}

	public async Task<bool> DeleteUserInfo(string username)
	{
		var userInfo = await GetUserInfo(username);
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
