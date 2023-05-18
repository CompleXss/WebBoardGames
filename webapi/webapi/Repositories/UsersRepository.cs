using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Repositories;

public class UsersRepository
{
	private readonly AppDbContext context;

	public UsersRepository(AppDbContext context)
	{
		this.context = context;
	}

	public List<User> GetAll()
	{
		return context.Users.ToList();
	}

	public async Task<List<User>> GetAllAsync()
	{
		return await context.Users.ToListAsync();
	}

	public async Task<User?> GetAsync(long userID) => await context.Users.FindAsync(userID);
	public async Task<User?> GetAsync(string username) => await context.Users.FirstOrDefaultAsync(x => x.Name == username);



	public async Task<bool> AddAsync(User user)
	{
		try
		{
			await context.AddAsync(user);
			return await context.SaveChangesAsync() >= 1;
		}
		catch (Exception)
		{
			return false;
		}
	}



	public async Task<bool> DeleteAsync(string username)
	{
		var userToRemove = await GetAsync(username);
		return await DeleteAsync(userToRemove);
	}

	public async Task<bool> DeleteAsync(long userID)
	{
		var userToRemove = await GetAsync(userID);
		return await DeleteAsync(userToRemove);
	}

	public async Task<bool> DeleteAsync(User? userToRemove)
	{
		if (userToRemove is null)
			return false;

		try
		{
			context.Users.Remove(userToRemove);
			await context.SaveChangesAsync();

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
