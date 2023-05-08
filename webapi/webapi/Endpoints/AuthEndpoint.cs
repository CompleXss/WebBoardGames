using System.IdentityModel.Tokens.Jwt;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace webapi.Endpoints;

public static class AuthEndpoint
{
	private const string REFRESH_TOKEN_PATH = "/auth/refresh";

	public static void MapAuthEndpoints(this WebApplication app)
	{
		app.MapPost("/auth/register", RegisterAsync).AllowAnonymous();
		app.MapPost("/auth/login", LoginAsync).AllowAnonymous();
		app.MapGet(REFRESH_TOKEN_PATH, RefreshTokenAsync).AllowAnonymous();
	}

	// TODO: улетает в exception, если в body не было юзера
	internal static async Task<IResult> RegisterAsync(HttpResponse response, UsersRepository users, AuthService auth, UserDto request)
	{
		if (await users.GetAsync(request.Name) is not null)
			return Results.BadRequest($"User with this name ({request.Name}) already exists.");

		var user = auth.CreateUser(request);
		bool created = await users.AddAsync(user);

		if (!created)
			return Results.BadRequest($"Can not create user '{request.Name}'.");

		var errorResult = await AddNewTokenPairToCookies(response, auth, user.Id, user.Name);
		if (errorResult is not null) return errorResult;

		return Results.Created($"/users/{user.Name}", user);
	}

	internal static async Task<IResult> LoginAsync(HttpResponse response, UsersRepository users, AuthService auth, UserDto request)
	{
		var user = await users.GetAsync(request.Name);

		if (user is null || !AuthService.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
			return Results.NotFound("Invalid username or password.");

		var errorResult = await AddNewTokenPairToCookies(response, auth, user.Id, user.Name);
		if (errorResult is not null) return errorResult;

		return Results.Ok();
	}

	internal static async Task<IResult> RefreshTokenAsync(HttpContext context, AuthService auth)
	{
		var providedAccessToken_str = context.Request.Cookies["access_token"];
		var providedAccessToken = await auth.ValidateAccessToken_DontCheckExpireDate(providedAccessToken_str);
		if (providedAccessToken is null)
			return Results.Unauthorized();

		long userID = long.Parse(providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
		string userName = providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

		var providedRefreshToken = context.Request.Cookies["refresh_token"];
		if (providedRefreshToken is null)
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
		WriteTokenPairIntoCookies(context.Response, accessToken, refreshToken);

		return Results.Ok();
	}



	/// <returns> Error result or null if no error occured. </returns>
	internal static async Task<IResult?> AddNewTokenPairToCookies(HttpResponse response, AuthService auth, long userID, string username)
	{
		var refreshToken = await auth.CreateRefreshTokenAsync(userID);
		if (refreshToken is null)
			return Results.Problem("Can not create refresh token.");

		var accessToken = auth.CreateAccessToken(userID, username);
		WriteTokenPairIntoCookies(response, accessToken, refreshToken);

		return null;
	}

	internal static void WriteTokenPairIntoCookies(HttpResponse response, string accessToken, RefreshToken refreshToken)
	{
		response.Cookies.Append("access_token", accessToken, new CookieOptions()
		{
			Path = "/",
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});

		response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions()
		{
			Path = REFRESH_TOKEN_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});
	}
}
