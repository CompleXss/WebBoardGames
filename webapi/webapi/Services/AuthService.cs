using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using webapi.Models;
using webapi.Repositories;
using webapi.Configuration;
using webapi.Endpoints;
using webapi.Extensions;

namespace webapi.Services;

public class AuthService
{
	public const string ACCESS_TOKEN_COOKIE_NAME = "access_token";
	public const string REFRESH_TOKEN_COOKIE_NAME = "refresh_token";
	public const string DEVICE_ID_COOKIE_NAME = "device_guid";

	public static readonly TimeSpan accessTokenLifetime = TimeSpan.FromMinutes(5);
	public static readonly TimeSpan refreshTokenLifetime = TimeSpan.FromDays(60);
	public static readonly TimeSpan deviceID_CookieLifetime = TimeSpan.FromDays(400);

	private readonly IConfiguration config;
	private readonly UserRefreshTokenRepository refreshTokenRepo;

	public AuthService(IConfiguration config, UserRefreshTokenRepository refreshTokenRepo)
	{
		this.config = config;
		this.refreshTokenRepo = refreshTokenRepo;
	}



	public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
	{
		using var hmac = new HMACSHA512();

		passwordSalt = hmac.Key;
		passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
	}

	public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
	{
		using var hmac = new HMACSHA512(passwordSalt);

		byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
		return passwordHash.SequenceEqual(computedHash);
	}



	public string CreateAccessToken(User user) => CreateAccessToken(user.PublicID);

	public string CreateAccessToken(string userPublicID)
	{
		var claims = new Claim[]
		{
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new(JwtRegisteredClaimNames.Sub, userPublicID.ToString()),
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

		var token = new JwtSecurityToken
		(
			claims: claims,
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			expires: DateTime.Now.Add(accessTokenLifetime),
			notBefore: DateTime.Now,
			signingCredentials: creds
		);

		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	public static async Task<UserTokenInfo?> TryGetUserInfoFromHttpContextAsync(HttpContext context)
	{
		var accessToken = await context.GetTokenAsync(ACCESS_TOKEN_COOKIE_NAME);
		if (accessToken is null) return null;

		return GetUserInfoFromAccessToken(accessToken);
	}

	public static UserTokenInfo GetUserInfoFromAccessToken(string accessToken)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		var decodedToken = tokenHandler.ReadJwtToken(accessToken);

		return GetUserInfoFromAccessToken(decodedToken);
	}

	public static UserTokenInfo GetUserInfoFromAccessToken(JwtSecurityToken accessToken)
	{
		string publicID = accessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value;

		return new UserTokenInfo()
		{
			PublicID = publicID,
		};
	}

	/// <returns> <see cref="JwtSecurityToken"/> if token is valid. Null if it's not. </returns>
	public async Task<JwtSecurityToken?> ValidateAccessTokenAsync(string? accessToken)
	{
		if (accessToken is null)
			return null;

		var tokenHandler = new JwtSecurityTokenHandler();
		var validationResult = await tokenHandler.ValidateTokenAsync(accessToken, ServicesConfigurator.GetJwtTokenValidationParameters(config));

		return validationResult.IsValid
			? validationResult.SecurityToken as JwtSecurityToken
			: null;
	}

	/// <returns> <see cref="JwtSecurityToken"/> if token is valid. Null if it's not. </returns>
	public async Task<JwtSecurityToken?> ValidateAccessTokenAsync_DontCheckExpireDate(string? accessToken)
	{
		if (accessToken is null)
			return null;

		var tokenHandler = new JwtSecurityTokenHandler();
		var validationResult = await tokenHandler.ValidateTokenAsync(accessToken, new TokenValidationParameters
		{
			ValidIssuer = config["Jwt:Issuer"],
			ValidAudience = config["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = false,
			ValidateIssuerSigningKey = true,
		});

		return validationResult.IsValid
			? validationResult.SecurityToken as JwtSecurityToken
			: null;
	}

	public Task<UserRefreshToken?> GetUserRefreshTokenAsync(string userPublicID, string deviceID)
	{
		return refreshTokenRepo.GetAsync(userPublicID, deviceID);
	}

	public Task<RefreshToken?> CreateRefreshTokenAsync(long userID, string deviceID)
	{
		var token = CreateRefreshToken();
		return refreshTokenRepo.AddRefreshTokenAsync(userID, deviceID, token);
	}

	public Task<RefreshToken?> UpdateUserRefreshTokenAsync(UserRefreshToken userRefreshToken)
	{
		var token = CreateRefreshToken();
		return refreshTokenRepo.UpdateRefreshTokenAsync(userRefreshToken, token);
	}

	public Task<bool> LogoutFromDevice(string userPublicID, string deviceID)
	{
		return refreshTokenRepo.RemoveUserTokenDeviceAsync(userPublicID, deviceID);
	}

	public Task<bool> LogoutFromAllDevices(string userPublicID)
	{
		return refreshTokenRepo.RemoveAllUserTokensAsync(userPublicID);
	}

	public Task<bool> LogoutFromAllDevices_ExceptOne(string userPublicID, string deviceID)
	{
		return refreshTokenRepo.RemoveUserTokensExceptOneDeviceAsync(userPublicID, deviceID);
	}



	private static RefreshToken CreateRefreshToken()
	{
		return new RefreshToken
		{
			Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
			TokenCreated = DateTime.Now,
			TokenExpires = DateTime.Now.Add(refreshTokenLifetime)
		};
	}



	public async Task<bool> AddNewTokenPairToResponseCookies(HttpContext context, User user)
	{
		string? deviceID = context.Request.GetDeviceIdCookie();
		deviceID ??= Guid.NewGuid().ToString();

		var refreshToken = await CreateRefreshTokenAsync(user.ID, deviceID);
		if (refreshToken is null)
			return false;

		var accessToken = CreateAccessToken(user);
		AddTokenCookiesToResponse(context.Response, deviceID, accessToken, refreshToken);

		return true;
	}

	public void AddTokenCookiesToResponse(HttpResponse response, string deviceID, string accessToken, RefreshToken refreshToken)
	{
		response.Cookies.Append(DEVICE_ID_COOKIE_NAME, deviceID, new CookieOptions()
		{
			Path = AuthEndpoint.AUTH_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = false,
			HttpOnly = true,
			Expires = DateTime.Now.Add(deviceID_CookieLifetime),
		});

		response.Cookies.Append(ACCESS_TOKEN_COOKIE_NAME, accessToken, new CookieOptions()
		{
			Path = "/",
			SameSite = SameSiteMode.Strict,
			Secure = false,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});

		response.Cookies.Append(REFRESH_TOKEN_COOKIE_NAME, refreshToken.Token, new CookieOptions()
		{
			Path = AuthEndpoint.REFRESH_TOKEN_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = false,
			HttpOnly = true,
			Expires = refreshToken.TokenExpires,
		});
	}

	public void DeleteTokenCookies(HttpResponse response)
	{
		response.Cookies.Append(ACCESS_TOKEN_COOKIE_NAME, "", new CookieOptions()
		{
			Path = "/",
			SameSite = SameSiteMode.Strict,
			Secure = false,
			HttpOnly = true,
			Expires = DateTime.UnixEpoch,
		});

		response.Cookies.Append(REFRESH_TOKEN_COOKIE_NAME, "", new CookieOptions()
		{
			Path = AuthEndpoint.REFRESH_TOKEN_PATH,
			SameSite = SameSiteMode.Strict,
			Secure = false,
			HttpOnly = true,
			Expires = DateTime.UnixEpoch,
		});
	}
}
