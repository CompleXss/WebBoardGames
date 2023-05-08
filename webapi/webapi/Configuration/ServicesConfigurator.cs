using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace webapi.Configuration;

public static class ServicesConfigurator
{
	public static void AddCORSPolicy(this IServiceCollection services)
	{
		services.AddCors(options =>
		{
			options.AddPolicy("CORSPolicy",
				builder =>
				{
					builder
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials()
					.WithOrigins("http://localhost:3000");
				});
		});
	}

	public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration config)
	{
		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidIssuer = config["Jwt:Issuer"],
				ValidAudience = config["Jwt:Audience"],
				IssuerSigningKey = new SymmetricSecurityKey
					(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero,
				ValidateIssuerSigningKey = true,
			};

			options.Events = new JwtBearerEvents()
			{
				OnMessageReceived = context =>
				{
					context.Token = context.Request.Cookies["access_token"];
					return Task.CompletedTask;
				}
			};
		});
	}

	public static void ConfigureAuthorization(this IServiceCollection services)
	{
		services.AddAuthorization(options =>
		{
			options.FallbackPolicy = new AuthorizationPolicyBuilder()
				.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
				.RequireAuthenticatedUser()
				.Build();
		});
	}
}
