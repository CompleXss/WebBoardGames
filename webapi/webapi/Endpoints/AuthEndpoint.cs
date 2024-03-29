﻿using webapi.Filters;
using webapi.Models;
using webapi.Repositories;
using webapi.Services;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace webapi.Endpoints;

public static class AuthEndpoint
{
	public const string ACCESS_TOKEN_COOKIE_NAME = "access_token";
	public const string REFRESH_TOKEN_COOKIE_NAME = "refresh_token";
	public const string DEVICE_ID_COOKIE_NAME = "device-guid";

	private const string AUTH_PATH = "/auth";
	private const string REFRESH_TOKEN_PATH = AUTH_PATH + "/refresh";

	public static void MapAuthEndpoints(this WebApplication app)
	{
		// register & login & refresh
		app.MapPost(AUTH_PATH + "/register", RegisterAsync)
			.AddEndpointFilter<ValidationFilter<UserDto>>()
			.AllowAnonymous();

		app.MapPost(AUTH_PATH + "/login", LoginAsync)
			.AddEndpointFilter<ValidationFilter<UserDto>>()
			.AllowAnonymous();

		app.MapPost(REFRESH_TOKEN_PATH, RefreshTokenAsync)
			.AllowAnonymous();

		// logout
		app.MapPost(AUTH_PATH + "/logout", LogoutAsync);
		app.MapPost(AUTH_PATH + "/logout-from-all-devices", LogoutFromAllDevicesAsync);
		app.MapPost(AUTH_PATH + "/logout-from-another-devices", LogoutFromAnotherDevicesAsync);

		// other
		app.MapGet(AUTH_PATH + "/isAuthorized", IsAuthorized);
		app.MapGet(AUTH_PATH + "/deviceCount", GetLoginDeviceCountAsync);
	}

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
		var providedAccessToken = await auth.ValidateAccessTokenAsync_DontCheckExpireDate(providedAccessToken_str);
		if (providedAccessToken is null) return Results.Unauthorized();

		var user = AuthService.GetUserInfoFromAccessToken(providedAccessToken);

		var providedRefreshToken = context.Request.Cookies[REFRESH_TOKEN_COOKIE_NAME];
		if (providedRefreshToken is null)
			return Results.BadRequest("Provided Refresh Token is null.");

		var deviceID = context.Request.Cookies[DEVICE_ID_COOKIE_NAME];
		if (deviceID is null)
			return Results.BadRequest("Provided Device Guid is null.");



		// Get user's active refresh token
		var activeUserRefreshToken = await auth.FindUserRefreshTokenAsync(user.ID, deviceID);
		if (activeUserRefreshToken is null)
			return Results.BadRequest("This user does not have a Refresh Token for provided Device Guid.");

		if (!RefreshToken.VerifyTokenHash(providedRefreshToken, activeUserRefreshToken.RefreshTokenHash))
		{
			// Something fishy is going on here
			// Database contains another refreshToken for provided user-deviceID pair, so someone is probably trying to fool me

			await auth.LogoutFromAllDevices(user.ID);
			return Results.BadRequest("Invalid refresh token. Suspicious activity detected.");
		}

		if (DateTime.Parse(activeUserRefreshToken.TokenExpires) < DateTime.UtcNow)
			return Results.BadRequest("Refresh Token expired.");

		var refreshToken = await auth.UpdateUserRefreshTokenAsync(activeUserRefreshToken);
		if (refreshToken is null)
			return Results.Problem("Can not update refresh token.");

		var accessToken = auth.CreateAccessToken(user.ID, user.Name);
		AddCookiesToResponse(context.Response, deviceID, accessToken, refreshToken);

		return Results.Ok();
	}

	internal static IResult IsAuthorized() => Results.Ok();

	internal static async Task<IResult> GetLoginDeviceCountAsync(HttpContext context, UserRefreshTokenRepository repo)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		var deviceCount = await repo.GetUserDeviceCount(user.ID);

		return Results.Ok(deviceCount);
	}



	internal static async Task<IResult> LogoutAsync(HttpContext context, AuthService auth)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		var deviceID = context.Request.Cookies[DEVICE_ID_COOKIE_NAME];
		if (deviceID is null) return Results.BadRequest("Provided Device Guid is null.");

		if (!await auth.LogoutFromDevice(user.ID, deviceID))
			return Results.Problem("Can not logout from this device.");

		DeleteTokenCookies(context.Response);

		return Results.Ok();
	}

	internal static async Task<IResult> LogoutFromAllDevicesAsync(HttpContext context, AuthService auth)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		if (!await auth.LogoutFromAllDevices(user.ID))
			return Results.Problem("Can not logout from all devices.");

		DeleteTokenCookies(context.Response);

		return Results.Ok();
	}

	internal static async Task<IResult> LogoutFromAnotherDevicesAsync(HttpContext context, AuthService auth)
	{
		var user = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (user is null) return Results.Unauthorized();

		var deviceID = context.Request.Cookies[DEVICE_ID_COOKIE_NAME];
		if (deviceID is null) return Results.BadRequest("Provided Device Guid is null.");

		bool succeeded = await auth.LogoutFromAllDevices_ExceptOne(user.ID, deviceID);

		return succeeded
			? Results.Ok()
			: Results.Problem("Can not logout from all devices except this one.");
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
		AddCookiesToResponse(context.Response, deviceID, accessToken, refreshToken);

		return null;
	}

	internal static void AddCookiesToResponse(HttpResponse response, string deviceID, string accessToken, RefreshToken refreshToken)
	{
		response.Cookies.Append(DEVICE_ID_COOKIE_NAME, deviceID, new CookieOptions()
		{
			Path = AUTH_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = DateTime.UtcNow.Add(AuthService.deviceID_CookieLifetime),
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

	internal static void DeleteTokenCookies(HttpResponse response)
	{
		response.Cookies.Append(ACCESS_TOKEN_COOKIE_NAME, "", new CookieOptions()
		{
			Path = "/",
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = DateTime.UnixEpoch,
		});

		response.Cookies.Append(REFRESH_TOKEN_COOKIE_NAME, "", new CookieOptions()
		{
			Path = REFRESH_TOKEN_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = true,
			HttpOnly = true,
			Expires = DateTime.UnixEpoch,
		});
	}
}
