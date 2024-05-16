using webapi.Extensions;
using webapi.Games;
using webapi.Models;

namespace webapi.Services;

public class GameService<TGame> : IGameService where TGame : PlayableGame
{
	private readonly GameCore gameCore;
	private readonly PlayableGame.Factory gameFactory;
	private readonly List<PlayableGame> activeGames = []; // order is not preserved
	private readonly ILogger<GameService<TGame>> logger;

	public GameService(GameCore gameCore, PlayableGame.Factory gameFactory, ILogger<GameService<TGame>> logger)
	{
		this.gameCore = gameCore;
		this.gameFactory = gameFactory;
		this.logger = logger;
	}



	public bool TryStartNewGame(IReadOnlyList<string> playerIDs, object? settings)
	{
		var game = gameFactory(gameCore, playerIDs, settings);
		if (game.ErrorWhileCreating)
			return false;

		activeGames.Add(game);

		logger.LogInformation("New {gameName} game with key {gameKey} was CREATED.", gameCore.GameName, game.Key);
		return true;
	}

	public PlayableGame? GetUserGame(string userID)
	{
		return activeGames.Find(x => x.PlayerIDs.Contains(userID));
	}



	public object? GetRelativeGameState(string userID)
	{
		return GetUserGame(userID)?.GetRelativeState(userID);
	}



	public bool TryUpdateGameState(PlayableGame game, string playerID, object moves, out string error)
	{
		if (!game.IsPlayerTurn(playerID))
		{
			error = "Сейчас не твой ход.";
			return false;
		}

		return game.TryUpdateState(playerID, moves, out error);
	}

	public void CloseGame(PlayableGame game)
	{
		activeGames.RemoveBySwap(game);

		logger.LogInformation("{gameName} game with key {gameKey} was CLOSED.", gameCore.GameName, game.Key);
		game.Dispose();
	}
}
