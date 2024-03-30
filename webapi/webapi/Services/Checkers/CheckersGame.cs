using webapi.Models;

namespace webapi.Services.Checkers;

public sealed class CheckersGame : IDisposable
{
	private static readonly HashSet<string> activeKeys = [];

	public string Key { get; }

	public string WhitePlayerID { get; init; }
	public string BlackPlayerID { get; init; }

	public int PlayersAlive { get; set; }

	public bool IsWhiteTurn { get; set; } = true;
	public string? WinnerID { get; private set; }

	public DateTime GameStarted { get; }

	public CheckersCell[,] Board { get; } = new CheckersCell[8, 8];

	private CheckersGame(string whitePlayerID, string blackPlayerID)
	{
		do
		{
			Key = Guid.NewGuid().ToString();
		}
		while (!activeKeys.Add(Key));

		WhitePlayerID = whitePlayerID;
		BlackPlayerID = blackPlayerID;

		GameStarted = DateTime.UtcNow;
	}

	public static CheckersGame CreateNew(string whitePlayerID, string blackPlayerID)
	{
		var game = new CheckersGame(whitePlayerID, blackPlayerID);
		var board = game.Board;

		// Do not change 'whites at the bottom, blacks at the top' start state!

		// white
		for (int i = 0; i < 3; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				board[j, i] = new CheckersCell(CheckersCellStates.White, false);
			}

		// black
		for (int i = 5; i < 8; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				board[j, i] = new CheckersCell(CheckersCellStates.Black, false);
			}

		return game;
	}

	public CheckersCellStates GetUserColor(string userID)
	{
		return BlackPlayerID == userID
			? CheckersCellStates.Black
			: CheckersCellStates.White;
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

	public void Dispose()
	{
		activeKeys.Remove(Key);
	}
}