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
		app.MapPost("/auth/refresh", RefreshAsync).AllowAnonymous();
	}

	// TODO: улетает в exception, если в body не было юзера
	internal static async Task<IResult> RegisterAsync(UsersRepository users, AuthService auth, UserDto request)
	{
		if (request is null)
			return Results.BadRequest("Invalid user data.");

		if (await users.GetAsync(request.Name) is not null)
			return Results.BadRequest($"User with this name ({request.Name}) already exists.");

		var user = auth.CreateUser(request);
		bool created = await users.AddAsync(user);

		if (!created)
			return Results.BadRequest($"Can not create user \"{request.Name}\".");

		var accessToken = auth.CreateAccessToken(user);
		return Results.Created($"/users/{user.Name}", new
		{
			user,
			accessToken
		});
	}

	internal static async Task<IResult> LoginAsync(UsersRepository users, UserRefreshTokenRepository userTokens, AuthService auth, UserDto request)
	{
		var user = await users.GetAsync(request.Name);

		if (user is null || !AuthService.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
			return Results.NotFound("Username or password is invalid.");

		var accessToken = auth.CreateAccessToken(user);
		var refreshToken = await userTokens.AddRefreshTokenAsync(user.Id);

		if (refreshToken is null)
			return Results.BadRequest("Can not create refresh token.");

		return Results.Ok(new
		{
			accessToken,
			refreshToken,
		});
	}

	internal static async Task<IResult> RefreshAsync(HttpContext context, UserRefreshTokenRepository userTokens, AuthService auth)
	{
		//var accessToken_str = await context.GetTokenAsync("access_token");
		var accessToken_str = context.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
		var oldAccessToken = await auth.ValidateAccessToken_DontCheckExpireDate(accessToken_str);
		if (oldAccessToken is null)
			return Results.Unauthorized();

		long userID = long.Parse(oldAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
		string userName = oldAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

		if (!context.Request.Cookies.TryGetValue("refresh_token", out var oldRefreshToken) || oldRefreshToken is null)
			return Results.BadRequest("Provided Refresh Token is null.");



		var userToken = await userTokens.GetAsync(userID, oldRefreshToken);
		if (userToken is null)
			return Results.BadRequest("Invalid Refresh Token.");

		if (userToken.RefreshToken != oldRefreshToken)
			return Results.BadRequest("Invalid Refresh Token.");

		if (DateTime.Parse(userToken.TokenExpires) < DateTime.UtcNow)
			return Results.BadRequest("Refresh Token expired.");

		var refreshToken = await userTokens.UpdateRefreshTokenAsync(userToken);
		if (refreshToken is null)
			return Results.BadRequest("Invalid Refresh Token.");

		var accessToken = auth.CreateAccessToken(userID, userName);

		return Results.Ok(new
		{
			accessToken,
			refreshToken,
		});
	}
}
