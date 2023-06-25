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

namespace webapi.Services;

public class AuthService
{
	public static readonly TimeSpan accessTokenLifetime = TimeSpan.FromMinutes(5);
	public static readonly TimeSpan refreshTokenLifetime = TimeSpan.FromDays(60);
	public static readonly TimeSpan deviceID_CookieLifetime = TimeSpan.FromDays(400);

	private readonly UserRefreshTokenRepository repo;
	private readonly IConfiguration config;

	public AuthService(IConfiguration config, UserRefreshTokenRepository userRefreshTokenRepo)
	{
		this.config = config;
		this.repo = userRefreshTokenRepo;
	}



	public User CreateUser(UserDto request)
	{
		CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

		return new User()
		{
			Name = request.Name,
			PasswordHash = passwordHash,
			PasswordSalt = passwordSalt,
		};
	}

	private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
	{
		using var hmac = new HMACSHA512();

		passwordSalt = hmac.Key;
		passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
	}

	public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
	{
		using var hmac = new HMACSHA512(passwordSalt);

		byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
		return passwordHash.SequenceEqual(computedHash);
	}



	public string CreateAccessToken(User user) => CreateAccessToken(user.Id, user.Name);

	public string CreateAccessToken(long userID, string userName)
	{
		var claims = new Claim[]
		{
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new Claim(JwtRegisteredClaimNames.Sub, userID.ToString()),
			new Claim(JwtRegisteredClaimNames.Name, userName),
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

		var token = new JwtSecurityToken
		(
			claims: claims,
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			expires: DateTime.UtcNow.Add(accessTokenLifetime),
			notBefore: DateTime.UtcNow,
			signingCredentials: creds
		);

		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	public static UserTokenInfo GetUserInfoFromAccessToken(string accessToken)
	{
		var decodedToken = DecodeJwtToken(accessToken);
		return GetUserInfoFromAccessToken(decodedToken);
	}

	public static async Task<UserTokenInfo?> TryGetUserInfoFromHttpContextAsync(HttpContext context)
	{
		var accessToken = await context.GetTokenAsync(AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME);
		if (accessToken is null) return null;

		return GetUserInfoFromAccessToken(accessToken);
	}

	public static UserTokenInfo GetUserInfoFromAccessToken(JwtSecurityToken accessToken)
	{
		long userID = long.Parse(accessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)!.Value);
		string userName = accessToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)!.Value;

		return new UserTokenInfo()
		{
			ID = userID,
			Name = userName,
		};
	}

	public static JwtSecurityToken DecodeJwtToken(string token)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.ReadJwtToken(token);
	}

	/// <returns><see cref="JwtSecurityToken"/> if token is valid. Null if it's not.</returns>
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

	/// <returns><see cref="JwtSecurityToken"/> if token is valid. Null if it's not.</returns>
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

	public async Task<UserRefreshToken?> FindUserRefreshTokenAsync(long userId, string deviceID)
		=> await repo.GetAsync(userId, deviceID);

	public async Task<RefreshToken?> CreateRefreshTokenAsync(long userID, string deviceID)
	{
		var token = CreateRefreshToken();
		return await repo.AddRefreshTokenAsync(userID, deviceID, token);
	}

	public async Task<RefreshToken?> UpdateUserRefreshTokenAsync(UserRefreshToken userRefreshToken)
	{
		var token = CreateRefreshToken();
		return await repo.UpdateRefreshTokenAsync(userRefreshToken, token);
	}

	public async Task<bool> LogoutFromDevice(long userID, string deviceID)
	{
		return await repo.RemoveUserTokenDevice(userID, deviceID);
	}

	public async Task<bool> LogoutFromAllDevices(long userID)
	{
		return await repo.RemoveAllUserTokens(userID);
	}

	public async Task<bool> LogoutFromAllDevices_ExceptOne(long userID, string deviceID)
	{
		return await repo.RemoveUserTokensExceptOneDevice(userID, deviceID);
	}



	private static RefreshToken CreateRefreshToken()
	{
		return new RefreshToken
		{
			Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
			TokenCreated = DateTime.UtcNow,
			TokenExpires = DateTime.UtcNow.Add(refreshTokenLifetime)
		};
	}
}
