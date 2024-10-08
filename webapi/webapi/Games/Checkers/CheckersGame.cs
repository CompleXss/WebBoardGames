﻿using Microsoft.AspNetCore.SignalR;
using System.Drawing;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games.Checkers;

public sealed class CheckersGame : PlayableGame
{
	protected override int TurnTimer_LIMIT_Seconds => 60;

	public string WhitePlayerID { get; init; }
	public string BlackPlayerID { get; init; }
	public bool IsWhiteTurn { get; set; } = true;
	public CheckersCell[,] Board { get; } = new CheckersCell[8, 8];

	private Point? ongoingMoveFrom;
	private CheckersMove lastMove;

	public CheckersGame(GameCore gameCore, IHubContext hub, ILogger logger, IReadOnlyList<string> playerIDs)
		: base(gameCore, hub, logger, playerIDs, disableTurnTimer: false)
	{
		if (ErrorWhileCreating)
		{
			WhitePlayerID = string.Empty;
			BlackPlayerID = string.Empty;
			return;
		}

		bool firstPlayerIsWhite = Random.Shared.NextBoolean();
		if (firstPlayerIsWhite)
		{
			WhitePlayerID = playerIDs[0];
			BlackPlayerID = playerIDs[1];
		}
		else
		{
			WhitePlayerID = playerIDs[1];
			BlackPlayerID = playerIDs[0];
		}

		// Do not change 'whites at the bottom, blacks at the top' start state!

		// white
		for (int i = 0; i < 3; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				Board[j, i] = new CheckersCell(CheckersCellStates.White, false);
			}

		// black
		for (int i = 5; i < 8; i++)
			for (int j = i & 1; j < 8; j += 2)
			{
				Board[j, i] = new CheckersCell(CheckersCellStates.Black, false);
			}

		TurnTimerFired += OnTurnTimerFired;
	}

	private void OnTurnTimerFired()
	{
		var playerID = IsWhiteTurn ? WhitePlayerID : BlackPlayerID;

		logger.PlayerForfeitsDueToTimer(playerID, this.Key, TurnTimer_LIMIT_Seconds);
		this.Surrender(playerID);
	}



	public CheckersCellStates GetUserColor(string userID)
	{
		return BlackPlayerID == userID
			? CheckersCellStates.Black
			: CheckersCellStates.White;
	}

	protected override bool IsPlayerTurn_Internal(string playerID)
	{
		var playerColor = GetUserColor(playerID);

		return IsWhiteTurn && playerColor == CheckersCellStates.White
			|| !IsWhiteTurn && playerColor == CheckersCellStates.Black;
	}

	protected override bool Surrender_Internal(string playerID)
	{
		WinnerID = playerID == WhitePlayerID
			? BlackPlayerID
			: WhitePlayerID;

		return true;
	}

	protected override object? GetRelativeState_Internal(string playerID)
	{
		var userColor = GetUserColor(playerID);
		var (allyPositions, enemyPositions) = GetDraughtsRelativeTo(userColor);
		bool isMyTurn = IsWhiteTurn && userColor == CheckersCellStates.White ||
						!IsWhiteTurn && userColor == CheckersCellStates.Black;

		string enemyID = userColor == CheckersCellStates.Black ? WhitePlayerID : BlackPlayerID;
		var ongoingMoveFrom = !ShouldMirrorMove(userColor)
			? this.ongoingMoveFrom
			: this.ongoingMoveFrom.HasValue ? new Point(7 - this.ongoingMoveFrom.Value.X, 7 - this.ongoingMoveFrom.Value.Y) : null;

		var lastMoveFrom = this.lastMove.From;
		var lastMoveTo = this.lastMove.To;
		var lastMove = !ShouldMirrorMove(userColor)
			? this.lastMove
			: new CheckersMove(
				new Point(7 - lastMoveFrom.X, 7 - lastMoveFrom.Y),
				new Point(7 - lastMoveTo.X, 7 - lastMoveTo.Y)
			);

		return new
		{
			myColor = userColor.ToString().ToLower(),
			allyPositions,
			enemyPositions,
			isMyTurn,
			isEnemyConnected = IsPlayerConnected(enemyID),
			ongoingMoveFrom,
			lastMove
		};
	}

	protected override bool TryUpdateState_Internal(string playerID, object data, out string error)
	{
		if (!TryDeserializeData(data, out CheckersMove move))
		{
			error = "Неправильные данные хода.";
			return false;
		}

		var playerColor = GetUserColor(playerID);
		move = DerelatifyMove(move, playerColor);

		if (ongoingMoveFrom.HasValue && move.From != ongoingMoveFrom)
		{
			error = "Нельзя продолжать ход другой шашкой.";
			return false;
		}

		bool moveIsValid = CheckersGameRuler.Validate(Board, move, playerColor, out bool shouldMoveOneMoreTime, out error);
		if (!moveIsValid)
			return false;

		ApplyMove(Board, move);
		this.lastMove = move;

		if (shouldMoveOneMoreTime)
		{
			ongoingMoveFrom = move.To;
		}
		else
		{
			ongoingMoveFrom = null;
			IsWhiteTurn = !IsWhiteTurn;
		}

		ResetTurnTimer();
		CheckForWinner();
		return true;
	}



	public static void ApplyMove(CheckersCell[,] board, CheckersMove move)
	{
		var userColor = board[move.From.X, move.From.Y].DraughtColor;

		bool isQueen =
				board[move.From.X, move.From.Y].IsQueen
				|| userColor == CheckersCellStates.White && move.To.Y == 7
				|| userColor == CheckersCellStates.Black && move.To.Y == 0;

		board[move.To.X, move.To.Y] = new CheckersCell(userColor, isQueen);

		// Clear cells
		int cellsToClearCount = Math.Abs(move.To.X - move.From.X);
		int moveVectorX = Math.Sign(move.To.X - move.From.X);
		int moveVectorY = Math.Sign(move.To.Y - move.From.Y);

		for (int i = 0; i < cellsToClearCount; i++)
		{
			board[move.From.X + moveVectorX * i, move.From.Y + moveVectorY * i]
				= new CheckersCell(CheckersCellStates.None, false);
		}
	}

	private static CheckersMove DerelatifyMove(in CheckersMove move, CheckersCellStates playerColor)
	{
		if (!ShouldMirrorMove(playerColor))
			return move;

		var from = move.From;
		from.X = 7 - from.X;
		from.Y = 7 - from.Y;

		var to = move.To;
		to.X = 7 - to.X;
		to.Y = 7 - to.Y;

		return new CheckersMove(from, to);
	}

	private (Draught[] myDraughts, Draught[] enemyDraughts) GetDraughtsRelativeTo(CheckersCellStates playerColor)
	{
		if (playerColor == CheckersCellStates.None)
			return (Array.Empty<Draught>(), Array.Empty<Draught>());

		var myList = new List<Draught>(12);
		var enemyList = new List<Draught>(12);

		var enemyColor = playerColor == CheckersCellStates.Black ? CheckersCellStates.White : CheckersCellStates.Black;
		bool reverse = ShouldMirrorMove(playerColor);

		for (int x = 0; x < 8; x++)
			for (int y = 0; y < 8; y++)
				if (Board[x, y].DraughtColor == playerColor)
				{
					if (reverse)
						myList.Add(new Draught(7 - x, 7 - y, Board[x, y].IsQueen));
					else
						myList.Add(new Draught(x, y, Board[x, y].IsQueen));
				}
				else if (Board[x, y].DraughtColor == enemyColor)
				{
					if (reverse)
						enemyList.Add(new Draught(7 - x, 7 - y, Board[x, y].IsQueen));
					else
						enemyList.Add(new Draught(x, y, Board[x, y].IsQueen));
				}

		return (myList.ToArray(), enemyList.ToArray());
	}



	private void CheckForWinner()
	{
		int whiteCount = CountCellWithState(CheckersCellStates.White);
		if (whiteCount == 0 || CheckersGameRuler.IsToiletForPlayer(Board, CheckersCellStates.White))
		{
			WinnerID = BlackPlayerID;
			return;
		}

		int blackCount = CountCellWithState(CheckersCellStates.Black);
		if (blackCount == 0 || CheckersGameRuler.IsToiletForPlayer(Board, CheckersCellStates.Black))
		{
			WinnerID = WhitePlayerID;
			return;
		}
	}

	private int CountCellWithState(CheckersCellStates state)
	{
		int count = 0;

		foreach (var cell in Board)
		{
			if (cell.DraughtColor == state)
				count++;
		}

		return count;
	}

	private static bool ShouldMirrorMove(CheckersCellStates playerColor)
	{
		return playerColor == CheckersCellStates.Black;
	}
}