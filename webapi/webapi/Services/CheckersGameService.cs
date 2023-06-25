using webapi.Models;
using webapi.Models.GameModels.Checkers;

namespace webapi.Services;

public class CheckersGameService
{
	private readonly List<CheckersGame> activeGames = new();
	private readonly ILogger<CheckersGameService> logger;

	public CheckersGameService(ILogger<CheckersGameService> logger)
	{
		this.logger = logger;
	}



	public string CreateNewGame(long firstPlayerID, long secondPlayerID)
	{
		bool firstPlayerIsWhite = Random.Shared.NextBoolean();

		var game = firstPlayerIsWhite
			? CheckersGame.CreateNew(firstPlayerID, secondPlayerID)
			: CheckersGame.CreateNew(secondPlayerID, firstPlayerID);

		activeGames.Add(game);

		logger.LogInformation("New checkers game with key {gameKey} was CREATED.", game.Key);
		return game.Key;
	}

	public CheckersGame? GetUserGame(long userID)
	{
		return activeGames.Find(x => x.WhitePlayerID == userID || x.BlackPlayerID == userID);
	}



	public object? GetRelativeGameState(long userID)
	{
		var game = GetUserGame(userID);
		if (game is null)
			return null;

		var userColor = game.GetUserColor(userID);
		var (allyPositions, enemyPositions) = game.GetDraughtsRelativeTo(userColor);
		bool isMyTurn = (game.IsWhiteTurn && userColor == CheckersCellStates.White) ||
						(!game.IsWhiteTurn && userColor == CheckersCellStates.Black);

		return new
		{
			myColor = userColor.ToString().ToLower(),
			allyPositions,
			enemyPositions,
			isMyTurn,
			winnerID = game.WinnerID,
		};
	}



	public CheckersGame? TryMakeMove(CheckersGame game, long userID, CheckersMove[] moves, out string error)
	{
		var userColor = game.GetUserColor(userID);
		if (game.IsWhiteTurn && userColor != CheckersCellStates.White
			|| !game.IsWhiteTurn && userColor != CheckersCellStates.Black)
		{
			error = "Сейчас не твой ход.";
			return null;
		}

		game.DerelatifyMoves(moves, userColor);
		bool moveIsValid = CheckersGameRuler.Validate(game.Board, moves, userColor, out error);

		if (!moveIsValid)
			return null;

		game.ApplyMove(moves);
		return game;
	}

	public void CloseGame(CheckersGame game)
	{
		activeGames.Remove(game);

		logger.LogInformation("Checkers game with key {gameKey} was CLOSED.", game.Key);
	}



	public async Task AddGameToHistory(GameHistoryService gameHistoryService, CheckersGame game)
	{
		if (!game.WinnerID.HasValue) return;

		var looserID = game.WhitePlayerID == game.WinnerID ? game.BlackPlayerID : game.WhitePlayerID;

		var playHistoryDto = new PlayHistoryDto()
		{
			Game = Games.Checkers,
			WinnerId = game.WinnerID!.Value,
			LooserID = looserID,
			DateTimeStart = game.GameStarted,
			DateTimeEnd = DateTime.UtcNow,
		};

		await gameHistoryService.AddAsync(playHistoryDto);
	}
}
