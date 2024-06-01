using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace webapi.Configuration;

public static class RateLimitingConfigurator
{
	public static void ConfigureRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			AddLoginPolicy(options);
			AddRegisterPolicy(options);
		});
	}

	private static void AddLoginPolicy(RateLimiterOptions options)
	{
		options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromSeconds(20),
			}));
	}

	private static void AddRegisterPolicy(RateLimiterOptions options)
	{
		options.AddPolicy("register", httpContext => RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromSeconds(30),
			}));
	}
}
