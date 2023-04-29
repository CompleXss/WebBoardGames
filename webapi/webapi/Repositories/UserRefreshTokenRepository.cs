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

	public async Task<UserRefreshToken?> GetAsync(long userID, string refreshToken)
		=> await context.UserRefreshTokens.FirstOrDefaultAsync(x => x.UserId == userID && x.RefreshToken == refreshToken);

	public async Task<RefreshToken?> AddRefreshTokenAsync(long userID, RefreshToken token)
	{
		try
		{
			await context.UserRefreshTokens.AddAsync(new UserRefreshToken
			{
				UserId = userID,
				RefreshToken = token.Token,
				TokenCreated = token.TokenCreated.ToString(AppDbContext.DATETIME_STRING_FORMAT),
				TokenExpires = token.TokenExpires.ToString(AppDbContext.DATETIME_STRING_FORMAT),
			});

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
