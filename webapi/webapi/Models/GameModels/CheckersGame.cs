namespace webapi.Models.GameModels;

public class CheckersGame
{
	public required long BlackPlayerID { get; init; }
	public required long WhitePlayerID { get; init; }

	public CheckersCellStates[,] board = new CheckersCellStates[8, 8];

	private CheckersGame()
	{
	}

	public static CheckersGame CreateNew(long blackPlayerID, long whitePlayerID)
	{
		var game = new CheckersGame
		{
			BlackPlayerID = blackPlayerID,
			WhitePlayerID = whitePlayerID,
		};
		var board = game.board;

		// black
		for (int i = 0; i < 3; i++)
			for (int j = 1 - i & 1; j < 8; j += 2)
			{
				board[i, j] = CheckersCellStates.Black;
			}

		// white
		for (int i = 5; i < 8; i++)
			for (int j = 1 - i & 1; j < 8; j += 2)
			{
				board[i, j] = CheckersCellStates.White;
			}

		return game;
	}
}

public enum CheckersCellStates
{
	Empty,
	Black,
	White,
}
