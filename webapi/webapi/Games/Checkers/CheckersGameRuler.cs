using System.Drawing;

namespace webapi.Games.Checkers;

internal static class CheckersGameRuler
{
	private const bool EATING_IS_MANDATORY = true;

	public static bool Validate(CheckersCell[,] board, in CheckersMove move, CheckersCellStates playerColor, out bool shouldMoveOneMoreTime, out string validationError)
	{
		shouldMoveOneMoreTime = false;

		if (playerColor == CheckersCellStates.None)
		{
			validationError = "Цвет игрока не определен";
			return false;
		}

		// simple checks
		if (!FirstStage(board, move, playerColor, out validationError))
			return false;

		// advanced checks
		if (!SecondStage(board, move, playerColor, out shouldMoveOneMoreTime, out validationError))
			return false;

		validationError = string.Empty;
		return true;
	}

	private static bool FirstStage(CheckersCell[,] board, in CheckersMove move, CheckersCellStates playerColor, out string validationError)
	{
		if (board[move.From.X, move.From.Y].DraughtColor != playerColor)
		{
			validationError = "Ход не своей шашкой";
			return false;
		}

		if (board[move.To.X, move.To.Y].DraughtColor != CheckersCellStates.None)
		{
			validationError = "Ход не на пустую клетку";
			return false;
		}

		if (move.From.X < 0 || move.From.X > 7 ||
			move.From.Y < 0 || move.From.Y > 7)
		{
			validationError = " Ход вышел за пределы доски";
			return false;
		}

		if (!IsMoveDiagonal(move.From, move.To))
		{
			validationError = "Ход не по диагонали (или на ту же клетку)";
			return false;
		}

		validationError = string.Empty;
		return true;
	}

	private static bool SecondStage(CheckersCell[,] board, in CheckersMove move, CheckersCellStates playerColor, out bool shouldMoveOneMoreTime, out string validationError)
	{
		board = CloneBoard(board);

		var enemyColor = GetEnemyColor(playerColor);

		var from = move.From;
		var to = move.To;
		int distance = Math.Abs(to.X - from.X);
		var moveVector = new Point(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

		bool isNotQueen = !board[from.X, from.Y].IsQueen;
		if (isNotQueen && distance > 2)
		{
			validationError = "Ход обычной шашкой больше, чем на 2 клетки";
			shouldMoveOneMoreTime = false;
			return false;
		}

		bool ate = false;
		for (int j = 1; j < distance; j++)
		{
			var passedDraughtColor = board[from.X + moveVector.X * j, from.Y + moveVector.Y * j].DraughtColor;
			if (passedDraughtColor == playerColor)
			{
				validationError = "Игрок съел свою шашку";
				shouldMoveOneMoreTime = false;
				return false;
			}

			ate = ate || passedDraughtColor == enemyColor;
		}



		if (ate)
		{
			validationError = string.Empty;
			CheckersGame.ApplyMove(board, move);
			shouldMoveOneMoreTime = CanEatFromPosition(board, to.X, to.Y);
			return true;
		}

		// else (if move with no eating)
		shouldMoveOneMoreTime = false;

		if (EATING_IS_MANDATORY && CanEat(board, playerColor))
		{
			validationError = "Кушать обязательно";
			return false;
		}

		if (isNotQueen && distance == 2)
		{
			validationError = "Ход обычной шашкой на 2 клетки, но не съел ни одного врага";
			return false;
		}

		if (isNotQueen && !IsGoingUpwards(move, playerColor))
		{
			validationError = "Ход обычной шашкой не вверх";
			return false;
		}

		validationError = string.Empty;
		return true;
	}



	public static bool IsToiletForPlayer(CheckersCell[,] board, CheckersCellStates playerColor)
	{
		if (playerColor == CheckersCellStates.Black)
			board = CloneAndReverse(board);

		var enemyColor = GetEnemyColor(playerColor);

		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
			{
				if (board[x, y].DraughtColor != playerColor)
					continue;

				bool isQueen = board[x, y].IsQueen;

				// up left
				bool canMoveUpLeft =
					x > 0 && y < 7 && board[x - 1, y + 1].DraughtColor == CheckersCellStates.None ||
					x > 1 && y < 6 && board[x - 1, y + 1].DraughtColor == enemyColor && board[x - 2, y + 2].DraughtColor == CheckersCellStates.None;

				if (canMoveUpLeft) return false;

				// up right
				bool canMoveUpRight =
					x < 7 && y < 7 && board[x + 1, y + 1].DraughtColor == CheckersCellStates.None ||
					x < 6 && y < 6 && board[x + 1, y + 1].DraughtColor == enemyColor && board[x + 2, y + 2].DraughtColor == CheckersCellStates.None;

				if (canMoveUpRight) return false;

				// down left
				bool canMoveDownLeft =
					isQueen && x > 0 && y > 0 && board[x - 1, y - 1].DraughtColor == CheckersCellStates.None ||
					x > 1 && y > 1 && board[x - 1, y - 1].DraughtColor == enemyColor && board[x - 2, y - 2].DraughtColor == CheckersCellStates.None;

				if (canMoveDownLeft) return false;

				// down right
				bool canMoveDownRight =
					isQueen && x < 7 && y > 0 && board[x + 1, y - 1].DraughtColor == CheckersCellStates.None ||
					x < 6 && y > 1 && board[x + 1, y - 1].DraughtColor == enemyColor && board[x + 2, y - 2].DraughtColor == CheckersCellStates.None;

				if (canMoveDownRight) return false;
			}

		return true;
	}



	private static bool CanEat(CheckersCell[,] board, CheckersCellStates playerColor)
	{
		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
				if (board[x, y].DraughtColor == playerColor && CanEatFromPosition(board, x, y))
				{
					return true;
				}

		return false;
	}

	private static bool CanEatFromPosition(CheckersCell[,] board, int x, int y)
	{
		bool isNotQueen = !board[x, y].IsQueen;
		var playerColor = board[x, y].DraughtColor;
		var enemyColor = GetEnemyColor(playerColor);

		// top-left
		if (CanEatInDirection(x, y, -1, 1))
			return true;

		// top-right
		if (CanEatInDirection(x, y, 1, 1))
			return true;

		// bottom-left
		if (CanEatInDirection(x, y, -1, -1))
			return true;

		// bottom-right
		if (CanEatInDirection(x, y, 1, -1))
			return true;

		return false;



		bool CanEatInDirection(int x, int y, int xIncrement, int yIncrement)
		{
			while (
				(xIncrement == -1 && x > 1 || xIncrement == 1 && x < 6) &&
				(yIncrement == -1 && y > 1 || yIncrement == 1 && y < 6))
			{
				x += xIncrement;
				y += yIncrement;

				if (board[x, y].DraughtColor == enemyColor &&
					board[x + xIncrement, y + yIncrement].DraughtColor == CheckersCellStates.None)
				{
					return true;
				}

				if (isNotQueen)
					break;
			}

			return false;
		}
	}



	private static bool IsGoingUpwards(CheckersMove move, CheckersCellStates playerColor)
	{
		int yDelta = move.To.Y - move.From.Y;

		return playerColor == CheckersCellStates.White && yDelta > 0
			|| playerColor == CheckersCellStates.Black && yDelta < 0;
	}

	private static CheckersCellStates GetEnemyColor(CheckersCellStates playerColor)
	{
		return playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;
	}

	/// <returns> True if move is diagonal and <paramref name="from"/> != <paramref name="to"/>. </returns>
	private static bool IsMoveDiagonal(Point from, Point to)
	{
		return from.X != to.X
			&& from.Y != to.Y
			&& Math.Abs(to.X - from.X) == Math.Abs(to.Y - from.Y);
	}

	private static CheckersCell[,] CloneBoard(CheckersCell[,] oldArr)
	{
		var newArr = new CheckersCell[8, 8];

		for (int i = 0; i < 8; i++)
			for (int j = 0; j < 8; j++)
			{
				newArr[i, j] = oldArr[i, j];
			}

		return newArr;
	}

	private static CheckersCell[,] CloneAndReverse(CheckersCell[,] board)
	{
		var boardCopy = new CheckersCell[8, 8];

		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
			{
				if (board[x, y].DraughtColor == CheckersCellStates.None)
					continue;

				boardCopy[7 - x, 7 - y] = board[x, y];
			}

		return boardCopy;
	}
}