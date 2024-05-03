using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using webapi.Data;
using webapi.Extensions;

namespace webapi.Configuration;

public static class DatabaseConfigurator
{
	/// <summary>
	/// Ensures that database exists and contains all necessary default values.<br/>
	/// DOES NOT check if database model is correct.
	/// </summary>
	/// <exception cref="Exception"></exception>
	public static async Task<bool> CheckDatabaseIsReadyAsync(this IServiceProvider services, ILogger logger)
	{
		using var scope = services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		bool canConnect = await context.Database.CanConnectAsync();
		if (!canConnect)
		{
			logger.CouldNotConnectToDatabase();
			return false;
		}

		bool hasTables = await context.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync();
		if (!hasTables)
		{
			logger.DatabaseDoesNotHaveAnyTables();
			return false;
		}

		var badEntriesCount = await context.CreateMissingDefaultData(logger);
		if (badEntriesCount > 0)
		{
			logger.DatabaseHasBadDefaultValues(badEntriesCount);
			return false;
		}

		logger.AppDoesNotKnowIfDatabaseSchemaIsCorrect();
		return true;
	}
}
