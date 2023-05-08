﻿using Microsoft.EntityFrameworkCore;
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

			entry.RefreshToken = token.Token;
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
			userToken.RefreshToken = newToken.Token;
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
}
