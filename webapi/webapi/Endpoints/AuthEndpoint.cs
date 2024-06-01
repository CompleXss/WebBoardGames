using webapi.Errors;
using webapi.Extensions;
using webapi.Filters;
using webapi.Models;
using webapi.Models.Data;
using webapi.Repositories;
using webapi.Services;

namespace webapi.Endpoints;

public static class AuthEndpoint
{
	public const string AUTH_PATH = "/auth";
	public const string REFRESH_TOKEN_PATH = AUTH_PATH + "/refresh";

	public static void MapAuthEndpoints(this WebApplication app)
	{
		// register & login & refresh
		app.MapPost(AUTH_PATH + "/register", RegisterAsync)
			.AddEndpointFilter<ValidationFilter<UserRegisterDto>>()
			.AllowAnonymous()
			.RequireRateLimiting("register");

		app.MapPost(AUTH_PATH + "/login", LoginAsync)
			.AddEndpointFilter<ValidationFilter<UserLoginDto>>()
			.AllowAnonymous()
			.RequireRateLimiting("login");

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

	internal static IResult IsAuthorized() => Results.Ok();

	internal static async Task<IResult> RegisterAsync(HttpContext context, UsersRepository usersRepository, AuthService auth, UserRegisterDto userDto)
	{
		if (await usersRepository.GetByLoginAsync(userDto.Login) is not null)
			return AuthErrors.UserAlreadyExists(userDto.Login);

		auth.CreatePasswordHash(userDto.Password, out var passwordHash, out var passwordSalt);
		var user = new User
		{
			Login = userDto.Login,
			Name = userDto.Name,
			PasswordHash = passwordHash,
			PasswordSalt = passwordSalt
		};

		if (!await usersRepository.AddAsync(user))
			return UserErrors.CouldNotCreate(userDto.Login);

		if (!await auth.AddNewTokenPairToResponseCookies(context, user))
			return AuthErrors.CouldNotCreateTokenPair();

		return Results.Created($"/users/{user.Login}", user);
	}

	internal static async Task<IResult> LoginAsync(HttpContext context, UsersRepository usersRepository, AuthService auth, UserLoginDto userDto)
	{
		var user = await usersRepository.GetByLoginAsync(userDto.Login);

		if (user is null || !auth.VerifyPasswordHash(userDto.Password, user.PasswordHash, user.PasswordSalt))
			return Results.NotFound("Неверный логин или пароль");

		if (!await auth.AddNewTokenPairToResponseCookies(context, user))
			return Results.Problem("Could not add new token pair to cookies");

		return Results.Ok();
	}

	internal static async Task<IResult> RefreshTokenAsync(HttpContext context, AuthService auth)
	{
		var providedAccessToken_str = context.Request.GetAccessTokenCookie();
		var providedAccessToken = await auth.ValidateAccessTokenAsync_DontCheckExpireDate(providedAccessToken_str);
		if (providedAccessToken is null) return Results.Unauthorized();

		var userTokenInfo = AuthService.GetUserInfoFromAccessToken(providedAccessToken);

		var providedRefreshToken = context.Request.GetRefreshTokenCookie();
		if (providedRefreshToken is null)
			return Results.BadRequest("Provided Refresh Token is null");

		var deviceID = context.Request.GetDeviceIdCookie();
		if (deviceID is null)
			return Results.BadRequest("Provided Device Guid is null");



		// Get user's active refresh token
		var activeUserRefreshToken = await auth.GetUserRefreshTokenAsync(userTokenInfo.PublicID, deviceID);
		if (activeUserRefreshToken is null)
			return Results.BadRequest("This user does not have a Refresh Token for provided Device Guid");

		if (!RefreshToken.VerifyTokenHash(providedRefreshToken, activeUserRefreshToken.RefreshTokenHash))
		{
			// Something fishy is going on here
			// Database contains another refreshToken for provided user-deviceID pair, so someone is probably trying to fool me

			await auth.LogoutFromAllDevices(userTokenInfo.PublicID);
			return Results.BadRequest("Invalid refresh token. Suspicious activity detected");
		}

		if (activeUserRefreshToken.TokenExpires < DateTime.UtcNow)
			return Results.BadRequest("Refresh Token expired");

		var refreshToken = await auth.UpdateUserRefreshTokenAsync(activeUserRefreshToken);
		if (refreshToken is null)
			return Results.Problem("Can not update refresh token");

		var accessToken = auth.CreateAccessToken(userTokenInfo.PublicID);
		auth.AddTokenCookiesToResponse(context.Response, deviceID, accessToken, refreshToken);

		return Results.Ok();
	}

	internal static async Task<IResult> GetLoginDeviceCountAsync(HttpContext context, UserRefreshTokenRepository repo)
	{
		var userTokenInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userTokenInfo is null) return Results.Unauthorized();

		var deviceCount = await repo.GetUserDeviceCount(userTokenInfo.PublicID);
		return Results.Ok(deviceCount);
	}



	internal static async Task<IResult> LogoutAsync(HttpContext context, AuthService auth)
	{
		var userTokenInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userTokenInfo is null) return Results.Unauthorized();

		var deviceID = context.Request.GetDeviceIdCookie();
		if (deviceID is null) return Results.BadRequest("Provided Device Guid is null");

		if (!await auth.LogoutFromDevice(userTokenInfo.PublicID, deviceID))
			return Results.Problem("Could not logout from this device");

		auth.DeleteTokenCookies(context.Response);
		return Results.Ok();
	}

	internal static async Task<IResult> LogoutFromAllDevicesAsync(HttpContext context, AuthService auth)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		if (!await auth.LogoutFromAllDevices(userInfo.PublicID))
			return Results.Problem("Could not logout from all devices");

		auth.DeleteTokenCookies(context.Response);
		return Results.Ok();
	}

	internal static async Task<IResult> LogoutFromAnotherDevicesAsync(HttpContext context, AuthService auth)
	{
		var userInfo = await AuthService.TryGetUserInfoFromHttpContextAsync(context);
		if (userInfo is null) return Results.Unauthorized();

		var deviceID = context.Request.GetDeviceIdCookie();
		if (deviceID is null) return Results.BadRequest("Provided Device Guid is null");

		if (!await auth.LogoutFromAllDevices_ExceptOne(userInfo.PublicID, deviceID))
			return Results.Problem("Could not logout from all devices except this one");

		return Results.Ok();
	}
}
