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

	public async Task<User?> GetAsync(long id) => await context.Users.FindAsync(id);
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

	public async Task<bool> DeleteAsync(long id)
	{
		var userToRemove = await GetAsync(id);
		return await DeleteAsync(userToRemove);
	}

	private async Task<bool> DeleteAsync(User? userToRemove)
	{
		if (userToRemove == null)
			return false;

		context.Users.Remove(userToRemove);
		return await context.SaveChangesAsync() >= 1;
	}
}
