using System.Drawing;

namespace webapi.Models.GameModels.Checkers;

public struct CheckersMove
{
	public Point From { get; set; }
	public Point To { get; set; }

	public CheckersMove(Point from, Point to)
	{
		From = from;
		To = to;
	}
}