using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models.Data;

namespace webapi.Repositories;

public class UsersRepository(AppDbContext db)
{
	private readonly AppDbContext db = db;

	public Task<List<User>> GetAllAsync() => db.Users.ToListAsync();
	public ValueTask<User?> GetByIdAsync(long userID) => db.Users.FindAsync(userID);
	public Task<User?> GetByPublicIdAsync(string publicID) => db.Users.FirstOrDefaultAsync(x => x.PublicID == publicID);
	public Task<User?> GetByLoginAsync(string login) => db.Users.FirstOrDefaultAsync(x => x.Login == login);

	public Task<long> GetDatabaseIdByPublicID(string userPublicID)
	{
		return db.Users
			.Where(x => x.PublicID == userPublicID)
			.Select(x => x.ID)
			.SingleAsync();
	}



	public async Task<bool> AddAsync(User user)
	{
		user.PublicID = Guid.NewGuid().ToString("N");

		try
		{
			await db.AddAsync(user);
			return await db.SaveChangesAsync() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}



	public async Task<bool> DeleteByPublicIdAsync(string userPublicID)
	{
		var userToRemove = await GetByPublicIdAsync(userPublicID);
		return await DeleteAsync(userToRemove);
	}

	public async Task<bool> DeleteByIdAsync(long userID)
	{
		var userToRemove = await GetByIdAsync(userID);
		return await DeleteAsync(userToRemove);
	}

	public async Task<bool> DeleteAsync(User? userToRemove)
	{
		if (userToRemove is null)
			return false;

		try
		{
			db.Users.Remove(userToRemove);
			return await db.SaveChangesAsync() > 0;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
