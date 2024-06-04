using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games.Checkers;

public sealed class CheckersGame : PlayableGame
{
	public string WhitePlayerID { get; init; }
	public string BlackPlayerID { get; init; }

	public bool IsWhiteTurn { get; set; } = true;

	public CheckersCell[,] Board { get; } = new CheckersCell[8, 8];

	public CheckersGame(GameCore gameCore, IHubContext hub, ILogger logger, IReadOnlyList<string> playerIDs)
		: base(gameCore, hub, logger, playerIDs)
	{
		if (ErrorWhileCreating)
		{
			WhitePlayerID = string.Empty;
			BlackPlayerID = string.Empty;
			return;
		}

		bool firstPlayerIsWhite = Random.Shared.NextBoolean();
		if (firstPlayerIsWhite)
		{
			WhitePlayerID = playerIDs[0];
			BlackPlayerID = playerIDs[1];
		}
		else
		{
			WhitePlayerID = playerIDs[1];
			BlackPlayerID = playerIDs[0];
		}

		// Do not change 'whites at the bottom, blacks at the top' start state!

		// white
		for (int i = 0; i < 3; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				Board[j, i] = new CheckersCell(CheckersCellStates.White, false);
			}

		// black
		for (int i = 5; i < 8; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				Board[j, i] = new CheckersCell(CheckersCellStates.Black, false);
			}
	}

	public CheckersCellStates GetUserColor(string userID)
	{
		return BlackPlayerID == userID
			? CheckersCellStates.Black
			: CheckersCellStates.White;
	}

	protected override bool IsPlayerTurn_Internal(string playerID)
	{
		var playerColor = GetUserColor(playerID);

		return IsWhiteTurn && playerColor == CheckersCellStates.White
			|| !IsWhiteTurn && playerColor == CheckersCellStates.Black;
	}

	protected override object? GetRelativeState_Internal(string playerID)
	{
		var userColor = GetUserColor(playerID);
		var (allyPositions, enemyPositions) = GetDraughtsRelativeTo(userColor);
		bool isMyTurn = IsWhiteTurn && userColor == CheckersCellStates.White ||
						!IsWhiteTurn && userColor == CheckersCellStates.Black;

		return new
		{
			myColor = userColor.ToString().ToLower(),
			allyPositions,
			enemyPositions,
			isMyTurn,
		};
	}

	protected override bool Surrender_Internal(string playerID)
	{
		WinnerID = playerID == WhitePlayerID
			? BlackPlayerID
			: WhitePlayerID;

		return true;
	}



	public (Draught[] myDraughts, Draught[] enemyDraughts) GetDraughtsRelativeTo(CheckersCellStates playerColor)
	{
		if (playerColor == CheckersCellStates.None)
			return (Array.Empty<Draught>(), Array.Empty<Draught>());

		var myList = new List<Draught>(12);
		var enemyList = new List<Draught>(12);

		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;
		bool reverse = ShouldMirrorMove(playerColor);

		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
				if (Board[x, y].DraughtColor == playerColor)
				{
					if (reverse)
						myList.Add(new Draught(7 - x, 7 - y, Board[x, y].IsQueen));
					else
						myList.Add(new Draught(x, y, Board[x, y].IsQueen));
				}
				else if (Board[x, y].DraughtColor == enemyColor)
				{
					if (reverse)
						enemyList.Add(new Draught(7 - x, 7 - y, Board[x, y].IsQueen));
					else
						enemyList.Add(new Draught(x, y, Board[x, y].IsQueen));
				}

		return (myList.ToArray(), enemyList.ToArray());
	}



	protected override bool TryUpdateState_Internal(string playerID, object data, out string error)
	{
		if (!TryDeserializeData(data, out CheckersMove[]? moves))
		{
			error = "Неправильные данные хода.";
			return false;
		}

		var playerColor = GetUserColor(playerID);

		DerelatifyMoves(moves, playerColor);
		bool moveIsValid = CheckersGameRuler.Validate(Board, moves, playerColor, out error);

		if (!moveIsValid)
			return false;

		ApplyMoves(moves);
		return true;
	}

	public void ApplyMoves(CheckersMove[] moves)
	{
		foreach (var move in moves)
		{
			ApplyMove(Board, move);
		}

		IsWhiteTurn = !IsWhiteTurn;

		int whiteCount = CountCellWithState(CheckersCellStates.White);
		if (whiteCount == 0 || CheckersGameRuler.IsToiletForPlayer(Board, CheckersCellStates.White))
		{
			WinnerID = BlackPlayerID;
			return;
		}

		int blackCount = CountCellWithState(CheckersCellStates.Black);
		if (blackCount == 0 || CheckersGameRuler.IsToiletForPlayer(Board, CheckersCellStates.Black))
		{
			WinnerID = WhitePlayerID;
			return;
		}
	}

	public static void ApplyMove(CheckersCell[,] board, CheckersMove move)
	{
		var userColor = board[move.From.X, move.From.Y].DraughtColor;

		bool isQueen =
				board[move.From.X, move.From.Y].IsQueen
				|| userColor == CheckersCellStates.White && move.To.Y == 7
				|| userColor == CheckersCellStates.Black && move.To.Y == 0;

		board[move.To.X, move.To.Y] = new CheckersCell(userColor, isQueen);

		// Clear cells
		int cellsToClearCount = Math.Abs(move.To.X - move.From.X);
		int moveVectorX = Math.Sign(move.To.X - move.From.X);
		int moveVectorY = Math.Sign(move.To.Y - move.From.Y);

		for (int i = 0; i < cellsToClearCount; i++)
		{
			board[move.From.X + moveVectorX * i, move.From.Y + moveVectorY * i]
				= new CheckersCell(CheckersCellStates.None, false);
		}
	}



	public void DerelatifyMoves(CheckersMove[] moves, CheckersCellStates playerColor)
	{
		if (!ShouldMirrorMove(playerColor))
			return;

		for (int i = 0; i < moves.Length; i++)
		{
			var from = moves[i].From;
			from.X = 7 - from.X;
			from.Y = 7 - from.Y;

			var to = moves[i].To;
			to.X = 7 - to.X;
			to.Y = 7 - to.Y;

			moves[i] = new CheckersMove(from, to);
		}
	}



	private int CountCellWithState(CheckersCellStates state)
	{
		int count = 0;

		foreach (var cell in Board)
		{
			if (cell.DraughtColor == state)
				count++;
		}

		return count;
	}

	private static bool ShouldMirrorMove(CheckersCellStates playerColor)
	{
		return playerColor == CheckersCellStates.Black;
	}
}