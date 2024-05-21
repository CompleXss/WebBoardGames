using Microsoft.AspNetCore.SignalR;
using webapi.Games;
using webapi.Hubs;
using webapi.Models;
using webapi.Types;

namespace webapi.Services;

public class GameService<TGame> : IGameService where TGame : PlayableGame
{
	public GameNames GameName => gameCore.GameName;
	private readonly GameCore gameCore;
	private readonly PlayableGame.Factory gameFactory;
	private readonly ConcurrentList<PlayableGame> activeGames = []; // order is not preserved
	private readonly IHubContext<GameHub<TGame>> hub;
	private readonly ILogger<GameService<TGame>> logger;

	public GameService(GameCore gameCore, PlayableGame.Factory gameFactory, IHubContext<GameHub<TGame>> hub, ILogger<GameService<TGame>> logger)
	{
		this.gameCore = gameCore;
		this.gameFactory = gameFactory;
		this.hub = hub;
		this.logger = logger;
	}

	public bool TryStartNewGame(IReadOnlyList<string> playerIDs, object? settings)
	{
		var game = gameFactory(gameCore, (IHubContext)hub, playerIDs, settings);
		if (game.ErrorWhileCreating)
			return false;

		activeGames.Add(game);

		logger.LogInformation("New {gameName} game with key {gameKey} was CREATED.", GameName, game.Key);
		return true;
	}

	public bool IsUserInGame(string userID)
	{
		return GetUserGame(userID) is not null;
	}

	public PlayableGameInfo? GetUserGameInfo(string userID)
	{
		return GetUserGame(userID)?.GetInfo();
	}

	private PlayableGame? GetUserGame(string userID)
	{
		return activeGames.Find(x => x.PlayerIDs.Contains(userID));
	}

	private PlayableGame? GetGameByKey(string gameKey)
	{
		return activeGames.Find(x => x.Key == gameKey);
	}

	public object? GetRelativeGameState(string userID)
	{
		return GetUserGame(userID)?.GetRelativeState(userID);
	}

	public PlayableGameInfo? ConnectPlayer(string playerID, string connectionID)
	{
		var game = GetUserGame(playerID);
		if (game is null)
			return null;

		bool connected = game.ConnectPlayer(playerID, connectionID);
		return connected ? game.GetInfo() : null;
	}

	public PlayableGameInfo? DisconnectPlayer(string playerID)
	{
		var game = GetUserGame(playerID);
		if (game is null)
			return null;

		bool disconnected = game.DisconnectPlayer(playerID);

		// todo: rework CloseGame on all players disconnect
		if (game.NoPlayersConnected)
			CloseGame(game);

		return disconnected ? game.GetInfo() : null;
	}



	public bool TryUpdateGameState(string gameKey, string playerID, object data, out string error)
	{
		var game = GetGameByKey(gameKey);
		if (game is null)
		{
			error = "Не найдена игра с указанным ID.";
			return false;
		}

		if (!game.IsPlayerTurn(playerID))
		{
			error = "Сейчас не твой ход.";
			return false;
		}

		return game.TryUpdateState(playerID, data, out error);
	}

	public void SendChatMessage(string gameKey, string message)
	{
		var game = GetGameByKey(gameKey);
		if (game is null)
			return;

		game.SendChatMessage(message);
	}

	public void CloseGame(string gameKey)
	{
		var game = GetGameByKey(gameKey);
		if (game is null) return;

		CloseGame(game);
	}

	private void CloseGame(PlayableGame game)
	{
		activeGames.RemoveBySwap(game);

		logger.LogInformation("{gameName} game with key {gameKey} was CLOSED.", GameName, game.Key);
		game.Dispose();
	}
}
