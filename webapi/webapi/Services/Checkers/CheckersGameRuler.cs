using System.Drawing;
using webapi.Models;

namespace webapi.Services.Checkers;

// TODO: Eating is mandatory ??? (шашки)

public static class CheckersGameRuler
{
	public static bool Validate(CheckersCell[,] board, CheckersMove[] moves, CheckersCellStates playerColor, out string validationError)
	{
		if (playerColor == CheckersCellStates.None)
		{
			validationError = "Цвет игрока не определен";
			return false;
		}

		// simple checks
		if (!FirstStage(board, moves, playerColor, out validationError))
			return false;

		// advanced checks
		if (!SecondStage(board, moves, playerColor, out validationError))
			return false;

		validationError = string.Empty;
		return true;
	}

	private static bool FirstStage(CheckersCell[,] board, CheckersMove[] moves, CheckersCellStates playerColor, out string validationError)
	{
		if (moves.Length == 0)
		{
			validationError = "Ход пустой";
			return false;
		}

		foreach (var move in moves)
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
		}

		validationError = string.Empty;
		return true;
	}

	private static bool SecondStage(CheckersCell[,] board, CheckersMove[] moves, CheckersCellStates playerColor, out string validationError)
	{
		board = CloneBoard(board);

		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;
		bool shouldEat = false;

		for (int i = 0; i < moves.Length; i++)
		{
			var from = moves[i].From;
			var to = moves[i].To;
			int distance = Math.Abs(to.X - from.X);
			var moveVector = new Point(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));



			bool isNotQueen = !board[from.X, from.Y].IsQueen;
			if (isNotQueen && distance > 2)
			{
				validationError = "Ход обычной шашкой больше, чем на 2 клетки";
				return false;
			}

			bool ate = false;
			for (int j = 1; j < distance; j++)
			{
				var passedDraughtColor = board[from.X + moveVector.X * j, from.Y + moveVector.Y * j].DraughtColor;
				if (passedDraughtColor == playerColor)
				{
					validationError = "Игрок съел свою шашку";
					return false;
				}

				ate = ate || passedDraughtColor == enemyColor;
			}



			if (ate)
			{
				shouldEat = true; // Съел хотя бы 1 шашку --> на следующих мувах тоже обязательно надо кушать
				CheckersGame.ApplyMove(board, moves[i]);
				continue;
			}

			// else
			if (isNotQueen && distance == 2)
			{
				validationError = "Ход обычной шашкой на 2 клетки, но не съел ни одного врага";
				return false;
			}

			if (shouldEat)
			{
				validationError = "Начал есть, а потом просто передвинул шашку, никого не съев (в пределах одного хода)";
				return false;
			}

			if (isNotQueen && !IsGoingUpwards(moves[i], playerColor))
			{
				validationError = "Ход обычной шашкой не вверх";
				return false;
			}

			validationError = string.Empty;
			return true;
		}

		validationError = string.Empty;
		return true;
	}



	public static bool IsToiletForPlayer(CheckersCell[,] board, CheckersCellStates playerColor)
	{
		if (playerColor == CheckersCellStates.Black)
			board = CloneAndReverse(board);

		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;

		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
			{
				if (board[x, y].DraughtColor != playerColor)
					continue;

				bool isQueen = board[x, y].IsQueen;

				// up left
				bool canMoveUpLeft =
					(x > 0 && y < 7 && board[x - 1, y + 1].DraughtColor == CheckersCellStates.None) ||
					(x > 1 && y < 6 && board[x - 1, y + 1].DraughtColor == enemyColor && board[x - 2, y + 2].DraughtColor == CheckersCellStates.None);

				if (canMoveUpLeft) return false;

				// up right
				bool canMoveUpRight =
					(x < 7 && y < 7 && board[x + 1, y + 1].DraughtColor == CheckersCellStates.None) ||
					(x < 6 && y < 6 && board[x + 1, y + 1].DraughtColor == enemyColor && board[x + 2, y + 2].DraughtColor == CheckersCellStates.None);

				if (canMoveUpRight) return false;

				// down left
				bool canMoveDownLeft =
					(isQueen && x > 0 && y > 0 && board[x - 1, y - 1].DraughtColor == CheckersCellStates.None) ||
					(x > 1 && y > 1 && board[x - 1, y - 1].DraughtColor == enemyColor && board[x - 2, y - 2].DraughtColor == CheckersCellStates.None);

				if (canMoveDownLeft) return false;

				// down right
				bool canMoveDownRight =
					(isQueen && x < 7 && y > 0 && board[x + 1, y - 1].DraughtColor == CheckersCellStates.None) ||
					(x < 6 && y > 1 && board[x + 1, y - 1].DraughtColor == enemyColor && board[x + 2, y - 2].DraughtColor == CheckersCellStates.None);

				if (canMoveDownRight) return false;
			}

		return true;
	}

	public static CheckersCell[,] CloneAndReverse(CheckersCell[,] board)
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



	//private static bool CheckCanDraughtEat(List<Draught> draughts, Draught draught, Player pl)
	//{
	//	CheckQueen(draught, pl);

	//	if (!draught.IsQueen)
	//	{
	//		Vector2 draughtPos = GetPositionPoint(draught.Position);
	//		Vector2 draughtMove1 = new Vector2(1, 1);
	//		Vector2 draughtMove2 = new Vector2(1, -1);
	//		Vector2 draughtMove3 = new Vector2(-1, 1);
	//		Vector2 draughtMove4 = new Vector2(-1, -1);

	//		Draught tmpDraught1 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove1));
	//		Draught tmpDraught2 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove2));
	//		Draught tmpDraught3 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove3));
	//		Draught tmpDraught4 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove4));

	//		if (tmpDraught1 != null && tmpDraught1.DColor != draught.DColor)
	//		{
	//			Draught tmpDraught11 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove1 + draughtMove1));
	//			if (tmpDraught11 == null && GetPosition(draughtPos + draughtMove1 + draughtMove1) != "")
	//			{
	//				return true;
	//			}
	//		}

	//		if (tmpDraught2 != null && tmpDraught2.DColor != draught.DColor)
	//		{
	//			Draught tmpDraught22 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove2 + draughtMove2));
	//			if (tmpDraught22 == null && GetPosition(draughtPos + draughtMove2 + draughtMove2) != "")
	//			{
	//				return true;
	//			}
	//		}

	//		if (tmpDraught3 != null && tmpDraught3.DColor != draught.DColor)
	//		{
	//			Draught tmpDraught33 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove3 + draughtMove3));
	//			if (tmpDraught33 == null && GetPosition(draughtPos + draughtMove3 + draughtMove3) != "")
	//			{
	//				return true;
	//			}
	//		}

	//		if (tmpDraught4 != null && tmpDraught4.DColor != draught.DColor)
	//		{
	//			Draught tmpDraught44 = draughts.Find(X => X.Position == GetPosition(draughtPos + draughtMove4 + draughtMove4));
	//			if (tmpDraught44 == null && GetPosition(draughtPos + draughtMove4 + draughtMove4) != "")
	//			{
	//				return true;
	//			}
	//		}
	//	}
	//	else
	//	{
	//		Vector2 draughtPos = GetPositionPoint(draught.Position);
	//		Vector2 draughtPos1 = draughtPos;
	//		Vector2 draughtPos2 = draughtPos;
	//		Vector2 draughtPos3 = draughtPos;
	//		Vector2 draughtPos4 = draughtPos;


	//		Vector2 draughtMove1 = new Vector2(1, 1);
	//		Vector2 draughtMove2 = new Vector2(1, -1);
	//		Vector2 draughtMove3 = new Vector2(-1, 1);
	//		Vector2 draughtMove4 = new Vector2(-1, -1);

	//		bool isSearch1 = true;
	//		bool isSearch2 = true;
	//		bool isSearch3 = true;
	//		bool isSearch4 = true;

	//		bool isPossible1 = false;
	//		bool isPossible2 = false;
	//		bool isPossible3 = false;
	//		bool isPossible4 = false;

	//		while (isSearch1 || isSearch2 || isSearch3 || isSearch4)
	//		{
	//			if (isSearch1)
	//			{
	//				draughtPos1 += draughtMove1;
	//				Draught tmpDraught = draughts.Find(X => X.Position == GetPosition(draughtPos1));
	//				if (tmpDraught != null)
	//				{
	//					if (tmpDraught.DColor != draught.DColor)
	//					{
	//						if (isPossible1)
	//						{
	//							isSearch1 = false;
	//							isPossible1 = false;
	//						}
	//						isPossible1 = true;
	//					}
	//					else
	//					{
	//						isSearch1 = false;
	//						isPossible1 = false;
	//					}
	//				}
	//				else if (!(GetPosition(draughtPos1) == "") && isPossible1)
	//				{
	//					return true;
	//				}

	//				if (GetPosition(draughtPos1) == "") isSearch1 = false;
	//			}
	//			if (isSearch2)
	//			{
	//				draughtPos2 += draughtMove2;
	//				Draught tmpDraught = draughts.Find(X => X.Position == GetPosition(draughtPos2));
	//				if (tmpDraught != null)
	//				{
	//					if (tmpDraught.DColor != draught.DColor)
	//					{
	//						if (isPossible2)
	//						{
	//							isSearch2 = false;
	//							isPossible2 = false;
	//						}
	//						isPossible2 = true;
	//					}
	//					else
	//					{
	//						isSearch2 = false;
	//						isPossible2 = false;
	//					}
	//				}
	//				else if (!(GetPosition(draughtPos2) == "") && isPossible2)
	//				{
	//					return true;
	//				}

	//				if (GetPosition(draughtPos2) == "") isSearch2 = false;
	//			}
	//			if (isSearch3)
	//			{
	//				draughtPos3 += draughtMove3;
	//				Draught tmpDraught = draughts.Find(X => X.Position == GetPosition(draughtPos3));
	//				if (tmpDraught != null)
	//				{
	//					if (tmpDraught.DColor != draught.DColor)
	//					{
	//						if (isPossible3)
	//						{
	//							isSearch3 = false;
	//							isPossible3 = false;
	//						}
	//						isPossible3 = true;
	//					}
	//					else
	//					{
	//						isSearch3 = false;
	//						isPossible3 = false;
	//					}
	//				}
	//				else if (!(GetPosition(draughtPos3) == "") && isPossible3)
	//				{
	//					return true;
	//				}

	//				if (GetPosition(draughtPos3) == "") isSearch3 = false;
	//			}
	//			if (isSearch4)
	//			{
	//				draughtPos4 += draughtMove4;
	//				Draught tmpDraught = draughts.Find(X => X.Position == GetPosition(draughtPos4));
	//				if (tmpDraught != null)
	//				{
	//					if (tmpDraught.DColor != draught.DColor)
	//					{
	//						if (isPossible4)
	//						{
	//							isSearch4 = false;
	//							isPossible4 = false;
	//						}
	//						isPossible4 = true;
	//					}
	//					else
	//					{
	//						isSearch4 = false;
	//						isPossible4 = false;
	//					}
	//				}
	//				else if (!(GetPosition(draughtPos4) == "") && isPossible4)
	//				{
	//					return true;
	//				}

	//				if (GetPosition(draughtPos4) == "") isSearch4 = false;
	//			}
	//		}
	//	}
	//	return false;
	//}



	private static bool IsGoingUpwards(CheckersMove move, CheckersCellStates playerColor)
	{
		int yDelta = move.To.Y - move.From.Y;

		return playerColor == CheckersCellStates.White && yDelta > 0
			|| playerColor == CheckersCellStates.Black && yDelta < 0;
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
}