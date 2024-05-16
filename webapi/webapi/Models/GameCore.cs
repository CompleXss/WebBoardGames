namespace webapi.Models;

public class GameCore
{
	public Games GameName { get; }

	/// <summary>
	/// Max number of games that can use this core.
	/// </summary>
	public int MaxGamesCount { get; }

	/// <summary>
	/// Random game key pool limited by <see cref="MaxGamesCount"/>.
	/// </summary>
	public RandomItemPool<int> KeyPool { get; }

	public int MinPlayersCount { get; }

	public int MaxPlayersCount { get; }

	public GameCore(Games gameName, int maxGamesCount, int minPlayersCount, int maxPlayersCount)
	{
		GameName = gameName;
		MaxGamesCount = maxGamesCount;
		KeyPool = new(Enumerable.Range(0, maxGamesCount));

		MinPlayersCount = minPlayersCount;
		MaxPlayersCount = maxPlayersCount;
	}
}
