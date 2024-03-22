using System.Drawing;

namespace webapi.Models.GameModels.Checkers;

public struct CheckersCell
{
	public CheckersCellStates DraughtColor { get; set; }
	public bool IsQueen { get; set; }

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
	public Point From { get; }
	public Point To { get; }

	public CheckersMove(Point from, Point to)
	{
		From = from;
		To = to;
	}
}

public readonly struct Draught
{
	public int X { get; }
	public int Y { get; }
	public bool IsQueen { get; }

	public Draught(int x, int y, bool isQueen)
	{
		this.X = x;
		this.Y = y;
		this.IsQueen = isQueen;
	}
}