using webapi.Extensions;
using webapi.Models;
using webapi.Repositories;

namespace webapi.Services.Checkers;

public class CheckersGameService(ILogger<CheckersGameService> logger)
{
	private readonly List<CheckersGame> activeGames = [];
	private readonly ILogger<CheckersGameService> logger = logger;



	public string CreateNewGame(string firstPlayerID, string secondPlayerID)
	{
		bool firstPlayerIsWhite = Random.Shared.NextBoolean();

		var game = firstPlayerIsWhite
			? CheckersGame.CreateNew(firstPlayerID, secondPlayerID)
			: CheckersGame.CreateNew(secondPlayerID, firstPlayerID);

		activeGames.Add(game);

		logger.LogInformation("New checkers game with key {gameKey} was CREATED.", game.Key);
		return game.Key;
	}

	public CheckersGame? GetUserGame(string userID)
	{
		return activeGames.Find(x => x.WhitePlayerID == userID || x.BlackPlayerID == userID);
	}



	public object? GetRelativeGameState(string userID)
	{
		var game = GetUserGame(userID);
		if (game is null)
			return null;

		var userColor = game.GetUserColor(userID);
		var (allyPositions, enemyPositions) = game.GetDraughtsRelativeTo(userColor);
		bool isMyTurn = game.IsWhiteTurn && userColor == CheckersCellStates.White ||
						!game.IsWhiteTurn && userColor == CheckersCellStates.Black;

		return new
		{
			myColor = userColor.ToString().ToLower(),
			allyPositions,
			enemyPositions,
			isMyTurn,
			winnerID = game.WinnerID,
		};
	}



	public CheckersGame? TryMakeMove(CheckersGame game, string userID, CheckersMove[] moves, out string error)
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

		game.ApplyMoves(moves);
		return game;
	}

	public void CloseGame(CheckersGame game)
	{
		activeGames.Remove(game);

		logger.LogInformation("Checkers game with key {gameKey} was CLOSED.", game.Key);
		game.Dispose();
	}



	public async Task AddGameToHistory(GameHistoryService gameHistoryService, UsersRepository usersRepository, CheckersGame game)
	{
		if (game.WinnerID is null) return;

		var looserPublicID = game.WhitePlayerID == game.WinnerID ? game.BlackPlayerID : game.WhitePlayerID;

		var winner = await usersRepository.GetByPublicIdAsync(game.WinnerID);
		var looser = await usersRepository.GetByPublicIdAsync(looserPublicID);

		if (winner is null || looser is null)
			return;

		var playHistoryDto = new GameHistoryDto()
		{
			Game = Games.checkers,
			Winners = [winner],
			Loosers = [looser],
			DateTimeStart = game.GameStarted,
			DateTimeEnd = DateTime.Now,
		};

		await gameHistoryService.AddAsync(playHistoryDto);
	}
}
