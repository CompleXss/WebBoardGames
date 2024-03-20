using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using webapi.Endpoints;

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
					context.Token = context.Request.Cookies[AuthEndpoint.ACCESS_TOKEN_COOKIE_NAME];
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
}
