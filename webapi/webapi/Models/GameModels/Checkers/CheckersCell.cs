namespace webapi.Models.GameModels.Checkers;

public struct CheckersCell
{
	public CheckersCellStates cellState;
	public bool isQueen;

	public CheckersCell(CheckersCellStates cell, bool isQueen)
	{
		this.cellState = cell;
		this.isQueen = isQueen;
	}
}

public enum CheckersCellStates
{
	None,
	Black,
	White,
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