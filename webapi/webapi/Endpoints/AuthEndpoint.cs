using System.IdentityModel.Tokens.Jwt;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace webapi.Endpoints;

public static class AuthEndpoint
{
	private const string REFRESH_TOKEN_PATH = "/auth/refresh";
	private const string ACCESS_TOKEN_COOKIE_NAME = "access_token";
	private const string REFRESH_TOKEN_COOKIE_NAME = "refresh_token";
	private const string DEVICE_ID_COOKIE_NAME = "device-guid";

	public static void MapAuthEndpoints(this WebApplication app)
	{
		app.MapPost("/auth/register", RegisterAsync).AllowAnonymous();
		app.MapPost("/auth/login", LoginAsync).AllowAnonymous();
		app.MapGet(REFRESH_TOKEN_PATH, RefreshTokenAsync).AllowAnonymous();
	}

	// TODO: улетает в exception, если в body не было юзера
	internal static async Task<IResult> RegisterAsync(HttpContext context, UsersRepository users, AuthService auth, UserDto userDto)
	{
		if (await users.GetAsync(userDto.Name) is not null)
			return Results.BadRequest($"User with this name ({userDto.Name}) already exists.");

		var user = auth.CreateUser(userDto);
		bool created = await users.AddAsync(user);

		if (!created)
			return Results.BadRequest($"Can not create user '{userDto.Name}'.");

		var errorResult = await AddNewTokenPairToCookies(context, auth, user.Id, user.Name);
		if (errorResult is not null) return errorResult;

		return Results.Created($"/users/{user.Name}", user);
	}

	internal static async Task<IResult> LoginAsync(HttpContext context, UsersRepository users, AuthService auth, UserDto userDto)
	{
		var user = await users.GetAsync(userDto.Name);

		if (user is null || !AuthService.VerifyPasswordHash(userDto.Password, user.PasswordHash, user.PasswordSalt))
			return Results.NotFound("Invalid username or password.");

		var errorResult = await AddNewTokenPairToCookies(context, auth, user.Id, user.Name);
		if (errorResult is not null) return errorResult;

		return Results.Ok();
	}

	internal static async Task<IResult> RefreshTokenAsync(HttpContext context, AuthService auth)
	{
		var providedAccessToken_str = context.Request.Cookies[ACCESS_TOKEN_COOKIE_NAME];
		var providedAccessToken = await auth.ValidateAccessToken_DontCheckExpireDate(providedAccessToken_str);
		if (providedAccessToken is null)
			return Results.Unauthorized();

		long userID = long.Parse(providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
		string userName = providedAccessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

		var providedRefreshToken = context.Request.Cookies[REFRESH_TOKEN_COOKIE_NAME];
		if (providedRefreshToken is null)
			return Results.BadRequest("Provided Refresh Token is null.");

		var deviceID = context.Request.Cookies[DEVICE_ID_COOKIE_NAME];
		if (deviceID is null)
			return Results.BadRequest("Provided Device Guid is null.");



		// Get user's active refresh token
		var activeUserRefreshToken = await auth.FindUserRefreshTokenAsync(userID, deviceID);
		if (activeUserRefreshToken is null)
			return Results.BadRequest("This user does not have a Refresh Token for provided Device Guid.");

		if (activeUserRefreshToken.RefreshToken != providedRefreshToken)
		{
			// Something fishy is going on here
			// Database contains another refreshToken for provided user-deviceID pair, so someone is probably trying to fool me

			// TODO: invalidate all user's refresh tokens
			return Results.BadRequest("Invalid refresh token. Suspicious activity detected.");
		}

		if (DateTime.Parse(activeUserRefreshToken.TokenExpires) < DateTime.UtcNow)
		{
			// TODO: delete token from database
			return Results.BadRequest("Refresh Token expired.");
		}

		var refreshToken = await auth.UpdateUserRefreshTokenAsync(activeUserRefreshToken);
		if (refreshToken is null)
			return Results.Problem("Can not update refresh token.");

		var accessToken = auth.CreateAccessToken(userID, userName);
		WriteTokenPairIntoCookies(context.Response, deviceID, accessToken, refreshToken);

		return Results.Ok();
	}



	/// <returns> Error result or null if no error occured. </returns>
	internal static async Task<IResult?> AddNewTokenPairToCookies(HttpContext context, AuthService auth, long userID, string username)
	{
		var deviceID = context.Request.Cookies[DEVICE_ID_COOKIE_NAME];
		deviceID ??= Guid.NewGuid().ToString();

		var refreshToken = await auth.CreateRefreshTokenAsync(userID, deviceID);
		if (refreshToken is null)
			return Results.Problem("Can not create refresh token.");

		var accessToken = auth.CreateAccessToken(userID, username);
		WriteTokenPairIntoCookies(context.Response, deviceID, accessToken, refreshToken);

		return null;
	}

	internal static void WriteTokenPairIntoCookies(HttpResponse response, string deviceID, string accessToken, RefreshToken refreshToken)
	{
		response.Cookies.Append(DEVICE_ID_COOKIE_NAME, deviceID, new CookieOptions()
		{
			Path = "/auth",
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
		});

		response.Cookies.Append(ACCESS_TOKEN_COOKIE_NAME, accessToken, new CookieOptions()
		{
			Path = "/",
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});

		response.Cookies.Append(REFRESH_TOKEN_COOKIE_NAME, refreshToken.Token, new CookieOptions()
		{
			Path = REFRESH_TOKEN_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});
	}
}
