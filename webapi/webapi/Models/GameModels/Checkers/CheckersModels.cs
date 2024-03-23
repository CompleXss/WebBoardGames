using System.Drawing;

namespace webapi.Models.GameModels.Checkers;

public readonly struct CheckersCell
{
	public CheckersCellStates DraughtColor { get; init; }
	public bool IsQueen { get; init; }

	public CheckersCell(CheckersCellStates draughtColor, bool isQueen)
	{
		this.DraughtColor = draughtColor;
		this.IsQueen = isQueen;
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
		this.From = from;
		this.To = to;
	}
}

public readonly struct Draught
{
	public int X { get; init; }
	public int Y { get; init; }
	public bool IsQueen { get; init; }

	public Draught(int x, int y, bool isQueen)
	{
		this.X = x;
		this.Y = y;
		this.IsQueen = isQueen;
	}
}
