using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using webapi.Data;
using webapi.Extensions;

namespace webapi.Configuration;

public static class ServicesConfigurator
{
	private const string CORS_POLICY_NAME = "CORSPolicy";

	public static void AddCORSPolicy(this IServiceCollection services, IConfiguration config)
	{
		services.AddCors(options =>
		{
			string[] origins = config.GetSection("CORSAllowedOrigins").Get<string[]>() ?? [];

			options.AddPolicy(CORS_POLICY_NAME,
				builder =>
				{
					builder
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials()
					.WithOrigins(origins);
				});
		});
	}

	public static void UseCORSPolicy(this WebApplication app)
	{
		app.UseCors(CORS_POLICY_NAME);
	}

	public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration config)
	{
		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
		{
			options.TokenValidationParameters = GetJwtTokenValidationParameters(config);

			options.Events = new JwtBearerEvents()
			{
				OnMessageReceived = context =>
				{
					context.Token = context.Request.GetAccessTokenCookie();
					return Task.CompletedTask;
				}
			};
		});
	}

	public static TokenValidationParameters GetJwtTokenValidationParameters(IConfiguration config) => new()
	{
		ValidIssuer = config["Jwt:Issuer"],
		ValidAudience = config["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ClockSkew = TimeSpan.FromSeconds(5),
		ValidateIssuerSigningKey = true,
	};



	public static void ConfigureAuthorization(this IServiceCollection services)
	{
		services.AddAuthorizationBuilder()
			.SetFallbackPolicy(new AuthorizationPolicyBuilder()
				.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
				.RequireAuthenticatedUser()
				.Build()
			);
	}



	/// <summary> Ensure that database is created and contains all necessary default values. </summary>
	/// <exception cref="Exception"></exception>
	public static async Task EnsureDatabaseIsReadyAsync(this IServiceProvider services, ILogger logger)
	{
		using var scope = services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		bool databaseCreated = await context.Database.EnsureCreatedAsync();
		if (databaseCreated)
			logger.LogInformation("Database created");

		int badEntriesCount = await context.CreateMissingDefaultData(logger);
		if (badEntriesCount > 0)
		{
			throw new Exception($"Database contains {badEntriesCount} bad default values. See above messages for details");
		}
	}
}
