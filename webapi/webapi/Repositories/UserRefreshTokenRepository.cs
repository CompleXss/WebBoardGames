using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.Services;

namespace webapi.Repositories;

public class UserRefreshTokenRepository
{
	private readonly AppDbContext context;
	private readonly AuthService auth;

	public UserRefreshTokenRepository(AppDbContext context, AuthService auth)
	{
		this.context = context;
		this.auth = auth;
	}

	public async Task<UserRefreshToken?> GetAsync(long userID, string refreshToken)
		=> await context.UserRefreshTokens.FirstOrDefaultAsync(x => x.UserId == userID && x.RefreshToken == refreshToken);

	public async Task<RefreshToken?> AddRefreshTokenAsync(long userID)
	{
		try
		{
			var token = auth.CreateRefreshToken();

			await context.UserRefreshTokens.AddAsync(new UserRefreshToken
			{
				UserId = userID,
				RefreshToken = token.Token,
				TokenCreated = token.TokenCreated.ToString("yyyy-MM-dd HH:mm:ss"),
				TokenExpires = token.TokenExpires.ToString("yyyy-MM-dd HH:mm:ss"),
			});

			return await context.SaveChangesAsync() > 0 ? token : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task<RefreshToken?> UpdateRefreshTokenAsync(long userID, string oldToken)
	{
		var userToken = await GetAsync(userID, oldToken);
		if (userToken is null)
			return null;

		return await UpdateRefreshTokenAsync(userToken);
	}

	public async Task<RefreshToken?> UpdateRefreshTokenAsync(UserRefreshToken userToken)
	{
		try
		{
			var newToken = auth.CreateRefreshToken();
			context.UserRefreshTokens.Remove(userToken);
			await context.SaveChangesAsync();

			// update entry
			userToken.RefreshToken = newToken.Token;
			userToken.TokenCreated = newToken.TokenCreated.ToString("yyyy-MM-dd HH:mm:ss");
			userToken.TokenExpires = newToken.TokenExpires.ToString("yyyy-MM-dd HH:mm:ss");

			await context.UserRefreshTokens.AddAsync(userToken);
			return await context.SaveChangesAsync() > 0 ? newToken : null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
