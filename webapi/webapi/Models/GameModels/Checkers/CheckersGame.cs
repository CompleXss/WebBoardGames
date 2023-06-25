namespace webapi.Models.GameModels.Checkers;

public sealed class CheckersGame : IDisposable
{
	private static readonly HashSet<string> activeKeys = new();

	public string Key { get; }

	public long WhitePlayerID { get; init; }
	public long BlackPlayerID { get; init; }

	public int PlayersAlive { get; set; }
	public List<string> ConnectionIDs { get; } = new();

	public bool IsWhiteTurn { get; set; } = true;
	public long? WinnerID { get; private set; }

	public DateTime GameStarted { get; }

	public CheckersCell[,] Board { get; } = new CheckersCell[8, 8];

	private CheckersGame(long whitePlayerID, long blackPlayerID)
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

	public static CheckersGame CreateNew(long whitePlayerID, long blackPlayerID)
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

	public CheckersCellStates GetUserColor(long userID)
	{
		return BlackPlayerID == userID
			? CheckersCellStates.Black
			: CheckersCellStates.White;
	}



	public (Draught[] myDraughts, Draught[] enemyDraughts) GetDraughtsRelativeTo(CheckersCellStates playerColor)
	{
		if (playerColor == CheckersCellStates.None)
			return (Array.Empty<Draught>(), Array.Empty<Draught>());

		var myList = new List<Draught>();
		var enemyList = new List<Draught>();

		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;
		bool reverse = ShouldMirrorMove(playerColor);

		int len1 = Board.GetLength(0);
		int len2 = Board.GetLength(1);

		for (int x = 0; x < len1; x++)
			for (int y = 0; y < len2; y++)
				if (Board[x, y].cellState == playerColor)
				{
					if (reverse)
						myList.Add(new Draught(7 - x, 7 - y, Board[x, y].isQueen));
					else
						myList.Add(new Draught(x, y, Board[x, y].isQueen));
				}
				else if (Board[x, y].cellState == enemyColor)
				{
					if (reverse)
						enemyList.Add(new Draught(7 - x, 7 - y, Board[x, y].isQueen));
					else
						enemyList.Add(new Draught(x, y, Board[x, y].isQueen));
				}

		return (myList.ToArray(), enemyList.ToArray());
	}



	public void ApplyMove(CheckersMove[] moves)
	{
		var userColor = Board[moves[0].From.X, moves[0].From.Y].cellState;

		foreach (var move in moves)
		{
			bool isQueen =
				Board[move.From.X, move.From.Y].isQueen
				|| (userColor == CheckersCellStates.White && move.To.Y == 7)
				|| (userColor == CheckersCellStates.Black && move.To.Y == 0);

			Board[move.From.X, move.From.Y] = new CheckersCell(CheckersCellStates.None, false);
			Board[move.To.X, move.To.Y] = new CheckersCell(userColor, isQueen);

			int cellsToClearCount = Math.Abs(move.To.X - move.From.X);
			int moveVectorX = Math.Sign(move.To.X - move.From.X);
			int moveVectorY = Math.Sign(move.To.Y - move.From.Y);

			for (int i = 1; i < cellsToClearCount; i++)
			{
				Board[move.From.X + moveVectorX * i, move.From.Y + moveVectorY * i]
					= new CheckersCell(CheckersCellStates.None, false);
			}
		}

		IsWhiteTurn = !IsWhiteTurn;

		int whiteCount = CountCellWithState(CheckersCellStates.White);
		if (whiteCount == 0)
		{
			WinnerID = BlackPlayerID;
			return;
		}

		int blackCount = CountCellWithState(CheckersCellStates.Black);
		if (blackCount == 0)
		{
			WinnerID = WhitePlayerID;
			return;
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
			if (cell.cellState == state)
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