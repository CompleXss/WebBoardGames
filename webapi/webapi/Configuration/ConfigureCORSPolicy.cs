namespace webapi.Configuration;

public static class ConfigureCORSPolicy
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
					.WithOrigins("http://localhost:3000");
				});
		});
	}
}
