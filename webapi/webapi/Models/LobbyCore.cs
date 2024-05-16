namespace webapi.Models;

public class LobbyCore
{
	public GameNames GameName { get; }

	/// <summary>
	/// Max number of lobbies that can use this core.
	/// </summary>
	public int MaxLobbiesCount { get; }

	/// <summary>
	/// Random lobby key pool limited by <see cref="MaxLobbiesCount"/>.
	/// </summary>
	public RandomItemPool<int> KeyPool { get; }

	/// <summary>
	/// Min number of players to start the game. Lobby host included.
	/// </summary>
	public int MinPlayersToStartGame { get; }

	/// <summary>
	/// Max number of players. Lobby host included.
	/// </summary>
	public int MaxPlayers { get; }

	public LobbyCore(GameNames gameName, int maxLobbiesCount, int minPlayersToStartGame, int maxPlayersIncludingHost)
	{
		GameName = gameName;
		MaxLobbiesCount = maxLobbiesCount;
		KeyPool = new(Enumerable.Range(0, maxLobbiesCount));

		MinPlayersToStartGame = minPlayersToStartGame;
		MaxPlayers = maxPlayersIncludingHost;
	}
}
