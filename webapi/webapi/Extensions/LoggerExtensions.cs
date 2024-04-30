namespace webapi.Extensions;

public static partial class LoggerExtensions
{
	[LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "Could not get user info from Access Token")]
	public static partial void CouldNotGetUserInfoFromAccessToken(this ILogger logger);

	[LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "User `{userName}` (ID: {userID}) CONNECTED to {gameName} lobby hub")]
	public static partial void UserConnectedToGameLobbyHub(this ILogger logger, string userName, long userID, string gameName);

	[LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "User `{userName}` (ID: {userID}) DISCONNECTED from {gameName} lobby hub")]
	public static partial void UserDisconnectedFromGameLobbyHub(this ILogger logger, string userName, long userID, string gameName);



	[LoggerMessage(EventId = 100, Level = LogLevel.Error, Message = "Database and code-defined default values in DbSet '{dbSetName}' don't match: '{databaseValue}' and '{codeValue}'")]
	public static partial void DatabaseHasDefaultEntryInDifferentCase(this ILogger logger, string dbSetName, string databaseValue, string codeValue);

	[LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Created new default value in DbSet '{dbSetName}': '{value}'")]
	public static partial void CreatedNewDefaultEntry(this ILogger logger, string dbSetName, string value);
}
