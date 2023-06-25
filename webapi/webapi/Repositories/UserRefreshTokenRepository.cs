using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;

namespace webapi.Repositories;

public class UserRefreshTokenRepository
{
	private readonly AppDbContext context;

	public UserRefreshTokenRepository(AppDbContext context)
	{
		this.context = context;
	}

	public async Task<UserRefreshToken?> GetAsync(long userID, string deviceID)
		=> await context.UserRefreshTokens.FindAsync(userID, deviceID);

	public async Task<int> GetUserDeviceCount(long userID)
		=> await context.UserRefreshTokens.CountAsync(x => x.UserId == userID);

	public async Task<RefreshToken?> AddRefreshTokenAsync(long userID, string deviceID, RefreshToken token)
	{
		try
		{
			var existingEntry = await context.UserRefreshTokens.FindAsync(userID, deviceID);
			var entry = existingEntry ?? new UserRefreshToken()
			{
				UserId = userID,
				DeviceId = deviceID,
			};

			entry.RefreshTokenHash = token.CreateHash();
			entry.TokenCreated = token.TokenCreated.ToString(AppDbContext.DATETIME_STRING_FORMAT);
			entry.TokenExpires = token.TokenExpires.ToString(AppDbContext.DATETIME_STRING_FORMAT);

			// if there was no entry, create new, otherwise update
			if (existingEntry is null)
				await context.UserRefreshTokens.AddAsync(entry);

			return await context.SaveChangesAsync() > 0 ? token : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task<RefreshToken?> UpdateRefreshTokenAsync(UserRefreshToken userToken, RefreshToken newToken)
	{
		try
		{
			context.UserRefreshTokens.Remove(userToken);
			await context.SaveChangesAsync();

			// update entry
			userToken.RefreshTokenHash = newToken.CreateHash();
			userToken.TokenCreated = newToken.TokenCreated.ToString(AppDbContext.DATETIME_STRING_FORMAT);
			userToken.TokenExpires = newToken.TokenExpires.ToString(AppDbContext.DATETIME_STRING_FORMAT);

			await context.UserRefreshTokens.AddAsync(userToken);
			return await context.SaveChangesAsync() > 0 ? newToken : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task<bool> RemoveUserTokenDevice(long userID, string deviceID)
	{
		try
		{
			var entryToDelete = await context.UserRefreshTokens.FindAsync(userID, deviceID);
			if (entryToDelete is null)
				return false;

			context.UserRefreshTokens.Remove(entryToDelete);
			await context.SaveChangesAsync();

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task<bool> RemoveUserTokensExceptOneDevice(long userID, string deviceID)
	{
		try
		{
			var entriesToDelete = context.UserRefreshTokens.Where(x => x.UserId == userID && x.DeviceId != deviceID);
			context.UserRefreshTokens.RemoveRange(entriesToDelete);
			await context.SaveChangesAsync();

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public async Task<bool> RemoveAllUserTokens(long userID)
	{
		try
		{
			var entriesToDelete = context.UserRefreshTokens.Where(x => x.UserId == userID);
			context.UserRefreshTokens.RemoveRange(entriesToDelete);
			await context.SaveChangesAsync();

			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
