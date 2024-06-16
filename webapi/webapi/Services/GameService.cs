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
	private readonly IHubContext<GameHub<TGame>, IGameHub> typedHub;
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<GameService<TGame>> logger;

	public GameService(GameCore gameCore, PlayableGame.Factory gameFactory, IHubContext<GameHub<TGame>> hub, IHubContext<GameHub<TGame>, IGameHub> typedHub, IServiceProvider serviceProvider, ILogger<GameService<TGame>> logger)
	{
		this.gameCore = gameCore;
		this.gameFactory = gameFactory;
		this.hub = hub;
		this.typedHub = typedHub;
		this.serviceProvider = serviceProvider;
		this.logger = logger;
	}

	public bool TryStartNewGame(IReadOnlyList<string> playerIDs, object? settings)
	{
		var game = gameFactory(gameCore, (IHubContext)hub, logger, playerIDs, settings);
		if (game.ErrorWhileCreating)
			return false;

		activeGames.Add(game);

		game.WinnerDefined += async () =>
		{
			CloseGame(game);

			// add game to history
			using var scope = serviceProvider.CreateScope();
			var gameHistoryService = scope.ServiceProvider.GetRequiredService<GameHistoryService>();
			await gameHistoryService.AddGameToHistoryAsync(game.GetInfo());
		};

		game.ClosedWithNoResult += () =>
		{
			CloseGame(game);
		};

		game.TurnTimerTick += secondsLeft =>
		{
			typedHub.Clients.Group(game.Key).TurnTimerTicked(secondsLeft);
		};

		logger.LogInformation("{gameName} game with key {gameKey} was CREATED.", GameName, game.Key);
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
		return disconnected ? game.GetInfo() : null;
	}

	public bool Surrender(string playerID)
	{
		var game = GetUserGame(playerID);
		if (game is null)
			return false;

		return game.Surrender(playerID);
	}

	public bool Request(string playerID, string request, object? data)
	{
		var game = GetUserGame(playerID);
		if (game is null)
			return false;

		bool success = game.Request(playerID, request, data);
		return success;
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

	public void CloseGame(string gameKey)
	{
		var game = GetGameByKey(gameKey);
		if (game is null) return;

		CloseGame(game);
	}

	private void CloseGame(PlayableGame game)
	{
		activeGames.RemoveBySwap(game);

		typedHub.Clients.Group(game.Key).GameClosed(game.WinnerID);
		RemoveAllUsersFromGameGroup(game);

		logger.LogInformation("{gameName} game with key {gameKey} was CLOSED.", GameName, game.Key);
		game.Dispose();
	}

	private void RemoveAllUsersFromGameGroup(PlayableGame game)
	{
		var connectionIDs = game.ConnectionIDs.Where(x => x is not null).ToArray();
		if (connectionIDs.Length == 0)
			return;

		var tasks = new Task[connectionIDs.Length];

		for (int i = 0; i < connectionIDs.Length; i++)
			tasks[i] = hub.Groups.RemoveFromGroupAsync(connectionIDs[i]!, game.Key);

		try
		{
			Task.WhenAll(tasks);
		}
		catch (Exception) { }
	}
}
