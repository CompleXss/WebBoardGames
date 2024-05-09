using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.Models.Data;

namespace webapi.Repositories;

public class UserRefreshTokenRepository(AppDbContext db)
{
	private readonly AppDbContext db = db;



	public Task<UserRefreshToken?> GetAsync(string userPublicID, string deviceID)
	{
		return db.UserRefreshTokens
			.Include(x => x.User)
			.Where(x => x.User.PublicID == userPublicID && x.DeviceID == deviceID)
			.FirstOrDefaultAsync();
	}

	public Task<int> GetUserDeviceCount(string userPublicID)
	{
		return db.UserRefreshTokens
			.Include(x => x.User)
			.CountAsync(x => x.User.PublicID == userPublicID);
	}

	public async Task<RefreshToken?> AddRefreshTokenAsync(long userID, string deviceID, RefreshToken token)
	{
		try
		{
			var existingEntry = await db.UserRefreshTokens.FindAsync(userID, deviceID);
			var entry = existingEntry ?? new UserRefreshToken()
			{
				UserID = userID,
				DeviceID = deviceID,
			};

			entry.RefreshTokenHash = token.CreateHash();
			entry.TokenCreated = token.TokenCreated;
			entry.TokenExpires = token.TokenExpires;

			// if there was no entry, create new, otherwise update
			if (existingEntry is null)
				await db.UserRefreshTokens.AddAsync(entry);

			return await db.SaveChangesAsync() > 0 ? token : null;
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
			bool needToAdd = !await db.UserRefreshTokens.ContainsAsync(userToken);

			// update entry
			userToken.RefreshTokenHash = newToken.CreateHash();
			userToken.TokenCreated = newToken.TokenCreated;
			userToken.TokenExpires = newToken.TokenExpires;

			if (needToAdd)
				await db.UserRefreshTokens.AddAsync(userToken);

			return await db.SaveChangesAsync() > 0 ? newToken : null;
		}
		catch (Exception)
		{
			return null;
		}
	}



	public Task<bool> RemoveUserTokenDeviceAsync(string userPublicID, string deviceID)
	{
		var entriesToDelete = db.UserRefreshTokens
			.Include(x => x.User)
			.Where(x => x.User.PublicID == userPublicID && x.DeviceID == deviceID);

		return RemoveEntriesAsync(entriesToDelete);
	}

	public Task<bool> RemoveUserTokensExceptOneDeviceAsync(string userPublicID, string deviceID)
	{
		var entriesToDelete = db.UserRefreshTokens
				.Include(x => x.User)
				.Where(x => x.User.PublicID == userPublicID && x.DeviceID != deviceID);

		return RemoveEntriesAsync(entriesToDelete);
	}

	public Task<bool> RemoveAllUserTokensAsync(string userPublicID)
	{
		var entriesToDelete = db.UserRefreshTokens
				.Include(x => x.User)
				.Where(x => x.User.PublicID == userPublicID);

		return RemoveEntriesAsync(entriesToDelete);
	}

	private async Task<bool> RemoveEntriesAsync(IQueryable<UserRefreshToken> entriesToDelete)
	{
		try
		{
			db.UserRefreshTokens.RemoveRange(entriesToDelete);
			await db.SaveChangesAsync();

			// return true even if deleted 0 entries
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
