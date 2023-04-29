using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class AuthEndpoint
{
	public static void MapAuthEndpoints(this WebApplication app)
	{
		app.MapPost("/auth/register", RegisterAsync).AllowAnonymous();
		app.MapPost("/auth/login", LoginAsync).AllowAnonymous();
		app.MapPost("/auth/refresh", RefreshTokenAsync).AllowAnonymous();
	}

	// TODO: улетает в exception, если в body не было юзера
	internal static async Task<IResult> RegisterAsync(UsersRepository users, AuthService auth, UserDto request)
	{
		if (await users.GetAsync(request.Name) is not null)
			return Results.BadRequest($"User with this name ({request.Name}) already exists.");

		var user = auth.CreateUser(request);
		bool created = await users.AddAsync(user);

		if (!created)
			return Results.BadRequest($"Can not create user '{request.Name}'.");

		var accessToken = auth.CreateAccessToken(user);
		var refreshToken = await auth.CreateRefreshTokenAsync(user.Id);

		return Results.Created($"/users/{user.Name}", new
		{
			user,
			accessToken,
			refreshToken
		});
	}

	internal static async Task<IResult> LoginAsync(UsersRepository users, AuthService auth, UserDto request)
	{
		var user = await users.GetAsync(request.Name);

		if (user is null || !AuthService.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
			return Results.NotFound("Invalid username or password.");

		var accessToken = auth.CreateAccessToken(user);
		var refreshToken = await auth.CreateRefreshTokenAsync(user.Id);

		if (refreshToken is null)
			return Results.Problem("Can not create refresh token.");

		return Results.Ok(new
		{
			accessToken,
			refreshToken,
		});
	}

	internal static async Task<IResult> RefreshTokenAsync(HttpContext context, AuthService auth)
	{
		//var providedAccessToken_str = await context.GetTokenAsync("access_token");

		var providedAccessToken_str = context.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
		var providedAccessToken = await auth.ValidateAccessToken_DontCheckExpireDate(providedAccessToken_str);
		if (providedAccessToken is null)
			return Results.Unauthorized();

		long userID = long.Parse(providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
		string userName = providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

		if (!context.Request.Cookies.TryGetValue("refresh_token", out var providedRefreshToken) || providedRefreshToken is null)
			return Results.BadRequest("Provided Refresh Token is null.");



		// Get user's active refresh token
		var activeUserRefreshToken = await auth.FindUserRefreshTokenAsync(userID, providedRefreshToken);
		if (activeUserRefreshToken is null)
		{
			// Something fishy is going on here
			// Database does not contain provided user-refreshToken pair, so someone is probably trying to fool me

			// TODO: invalidate all user's refresh tokens
			return Results.BadRequest("Invalid refresh token. Suspicious activity detected.");
		}

		if (DateTime.Parse(activeUserRefreshToken.TokenExpires) < DateTime.UtcNow)
			return Results.BadRequest("Refresh Token expired.");

		var refreshToken = await auth.UpdateUserRefreshTokenAsync(activeUserRefreshToken);
		if (refreshToken is null)
			return Results.Problem("Can not update refresh token.");

		var accessToken = auth.CreateAccessToken(userID, userName);

		return Results.Ok(new
		{
			accessToken,
			refreshToken,
		});
	}
}
