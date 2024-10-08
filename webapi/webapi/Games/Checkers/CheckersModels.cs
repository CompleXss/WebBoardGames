﻿using System.Drawing;

namespace webapi.Games.Checkers;

public readonly struct CheckersCell
{
	public CheckersCellStates DraughtColor { get; init; }
	public bool IsQueen { get; init; }

	public CheckersCell(CheckersCellStates draughtColor, bool isQueen)
	{
		DraughtColor = draughtColor;
		IsQueen = isQueen;
	}
}

public enum CheckersCellStates
{
	None,
	Black,
	White,
}

public readonly struct CheckersMove
{
	public Point From { get; init; }
	public Point To { get; init; }

	public CheckersMove(Point from, Point to)
	{
		From = from;
		To = to;
	}
}

internal readonly struct Draught
{
	public int X { get; init; }
	public int Y { get; init; }
	public bool IsQueen { get; init; }

	public Draught(int x, int y, bool isQueen)
	{
		X = x;
		Y = y;
		IsQueen = isQueen;
	}
}
