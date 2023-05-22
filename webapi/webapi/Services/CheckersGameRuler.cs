using System;
using System.Drawing;
using System.Numerics;
using webapi.Models.GameModels.Checkers;

namespace webapi.Services;

// TODO: Валидация хода (шашки)

public static class CheckersGameRuler
{
	public static bool Validate(CheckersCell[,] board, CheckersMove[] moves, CheckersCellStates playerColor, out string validationError)
	{
		if (playerColor == CheckersCellStates.None)
		{
			validationError = "Цвет игрока не определен!";
			return false;
		}

		//CheckSurrender(moves, pl);

		if (!FirstStage(moves))
		{
			validationError = "Ход не прошел 1 ступень валидации.";
			return false;
		}

		if (!SecondStage(Clone(board), moves, playerColor))
		{
			validationError = "Ход не прошел 2 ступень валидации.";
			return false;
		}

		validationError = "";
		return true;
	}

	private static bool FirstStage(CheckersMove[] moves)
	{
		if (moves.Length == 0)
			return false;

		foreach (var move in moves)
		{
			if (move.From.X < 0 || move.From.X > 7 &&
				move.From.Y < 0 || move.From.Y > 7)
				return false; // Ход вышел за пределы доски

			if (move.From.X == move.To.X && move.From.Y == move.To.Y)
				return false; // Ход на ту же клетку
		}

		return true;
	}

	private static bool SecondStage(CheckersCell[,] Board, CheckersMove[] moves, CheckersCellStates playerColor)
	{
		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;

		for (int i = 0; i < moves.Length; i++)
		{
			var from = moves[i].From;
			var to = moves[i].To;

			if (Board[from.X, from.Y].cellState != playerColor)
				return false; // Ход не своей шашкой

			if (Board[to.X, to.Y].cellState != CheckersCellStates.None)
				return false; // Ход не на пустую клетку

			if (!IsMoveDiagonal(from, to))
				return false; // Ход не по диагонали


			int distance = Math.Abs(to.X - from.X);
			var moveVector = new Point(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

			if (!Board[from.X, from.Y].isQueen
				&& !IsGoingUpwards(moves[i], playerColor)
				&& Board[from.X + moveVector.X, from.Y + moveVector.Y].cellState != enemyColor)
				return false; // Ход не в ту сторону



			if (!Board[from.X, from.Y].isQueen && distance > 1)
			{
				if (distance > 2) return false; // Ход обычной шашкой больше, чем на 2 клетки

				// distance == 2
				if (Board[from.X + moveVector.X, from.Y + moveVector.Y].cellState != enemyColor)
					return false; // Ход обычной шашкой на 2 клетки, но никого не съел (или съел своего)
			}








		}




		//bool isAte = false;

		//for (int i = 1; i < moves.Length; i++)
		//{
		//	while (curPos != futPos)
		//	{
		//		curPos += deltaVector;
		//		Draught tmpDraught = Board.Find(X => X.Position == GetPosition(curPos));

		//		if (!activeDraught.IsQueen)
		//			lenght++;

		//		if (tmpDraught != null) //если на пути есть шашка
		//			if (tmpDraught.DColor != activeDraught.DColor)
		//			{
		//				Board.Remove(tmpDraught);
		//				isAte = true;
		//				lenght--;
		//			}
		//			else
		//			{
		//				//Debug.Log($"Сторона {pl.MySide} На пути встречена своя шашка Ход: {GetTurn(moves)}");
		//				return false;
		//			}

		//		if (lenght == 2)
		//			return false;//ход дальше, чем возможно
		//	}

		//	activeDraught.Position = GetPosition(curPos);

		//	if (!activeDraught.IsQueen && !isAte && ((pl.IsUpwards && (futPos - curPos1).Y > 0) || (!pl.IsUpwards && (futPos - curPos1).Y < 0)))
		//	{
		//		//Debug.Log($"Сторона {pl.MySide} Ход не в ту сторону Ход: {GetTurn(moves)}");
		//		return false; //проверка на вверх и вниз не для дамки
		//	}

		//	if (!isAte && moves.Length - i - 1 > 0)
		//	{
		//		//Debug.Log($"Сторона {pl.MySide} Избыток ходов Ход: {GetTurn(moves)}");
		//		return false;//я не съел, но есть ещё ходы
		//	}

		//	if (!isAte && moves.Length != 2)
		//	{
		//		//Debug.Log($"Сторона {pl.MySide} Избыток ходов Ход: {GetTurn(moves)}");
		//		return false;//я не съел, но есть ещё ходы
		//	}

		//	if (CheckShouldEat(copyList, pl) != isAte)
		//	{
		//		//Debug.Log($"Сторона {pl.MySide} Не съел, но должен Ход: {GetTurn(moves)}");
		//		return false; //не съел, когда должен был
		//	}

		//	//activeDraught.Position = GetPosition(from);

		//	CheckQueen(activeDraught, pl);
		//}

		//if (CheckShouldEat(activeDraught, Board, pl) && isAte)
		//{
		//	//Debug.Log($"Сторона {pl.MySide} Не всех доел Ход: {GetTurn(moves)}");
		//	return false;//доел ли всех
		//}

		return true;
	}

	private static bool IsGoingUpwards(CheckersMove move, CheckersCellStates playerColor)
	{
		int yDelta = move.To.Y - move.From.Y;

		return playerColor == CheckersCellStates.White && yDelta > 0
			|| playerColor == CheckersCellStates.Black && yDelta < 0;
	}



	//public static void CheckSurrender(string[] moves, Player pl)
	//{
	//	if (moves[0] == "Surrender")
	//	{
	//		Debug.Log($"{pl.MySide} сдался!");
	//	}
	//}

	//private static bool CheckShouldEat(Draught draught, List<Draught> draughts, Player pl)
	//{
	//	if (CheckCanDraughtEat(draughts, draught, pl)) return true;
	//	return false;
	//}

	//private static bool CheckShouldEat(List<Draught> draughts, Player pl)
	//{
	//	List<Draught> playerDraughts = draughts.FindAll(X => X.DColor == pl.MySide);

	//	foreach (var dr in playerDraughts)
	//	{
	//		if (CheckCanDraughtEat(draughts, dr, pl)) return true;
	//	}
	//	return false;
	//}

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

	//private static void CheckQueen(Draught draught, Player pl)
	//{
	//	if (pl.IsUpwards && draught.Position[1] == '1')
	//	{
	//		draught.IsQueen = true;
	//	}

	//	if (!pl.IsUpwards && draught.Position[1] == '8')
	//	{
	//		draught.IsQueen = true;
	//	}
	//}



	private static bool IsMoveDiagonal(Point from, Point to)
	{
		return from.X != to.X
			&& from.Y != to.Y
			&& Math.Abs(to.X - from.X) == Math.Abs(to.Y - from.Y);
	}



	private static CheckersCell[,] Clone(CheckersCell[,] oldArr)
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