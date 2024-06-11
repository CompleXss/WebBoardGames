using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games;

public abstract class PlayableGame : IDisposable
{
	public delegate PlayableGame Factory(GameCore gameCore, IHubContext hub, ILogger logger, IReadOnlyList<string> playerIDs, object? settings);

	private readonly GameCore gameCore;
	private readonly IHubContext hub;
	protected readonly ILogger logger;

	public string Key { get; }
	private readonly int key;

	public IReadOnlyList<string> PlayerIDs => playerIDs;
	private readonly string[] playerIDs;

	public IReadOnlyList<string?> ConnectionIDs => connectionIDs;
	private readonly string?[] connectionIDs;

	public DateTime GameStarted { get; }
	public bool ErrorWhileCreating { get; protected set; }
	public bool NoPlayersConnected => ConnectionIDs.All(x => x is null);

	public string? WinnerID
	{
		get => winnerID;
		protected set
		{
			winnerID = value;

			if (value is not null)
				WinnerDefined?.Invoke();
		}
	}
	private string? winnerID;

	public event Action? WinnerDefined;
	public event Action? ClosedWithNoResult;
	protected event Action<int>? PlayerConnected;
	protected event Action<int>? PlayerDisconnected;

	protected abstract int TurnTimer_LIMIT_Seconds { get; }
	protected int TurnTimer_LEFT_Seconds
	{
		get => turnTimerLeftSeconds;
		private set
		{
			turnTimerLeftSeconds = value;
			TurnTimerTick?.Invoke(value);
		}
	}
	private int turnTimerLeftSeconds;

	protected event Action? TurnTimerFired = null;
	public event Action<int>? TurnTimerTick = null;

	private readonly CancellationTokenSource turnTimerCTS = new();
	private bool keyCaptured;
	private bool _disposed;

	public PlayableGame(GameCore gameCore, IHubContext hub, ILogger logger, IReadOnlyList<string> playerIDs, bool disableTurnTimer)
	{
		this.gameCore = gameCore;
		this.hub = hub;
		this.logger = logger;
		this.keyCaptured = gameCore.KeyPool.TryGetRandom(out key);
		this.ErrorWhileCreating = !keyCaptured;

		int playersCount = playerIDs.Count;

		if (playersCount < gameCore.MinPlayersCount || playersCount > gameCore.MaxPlayersCount)
		{
			this.ErrorWhileCreating = true;
			this.Key = null!;
			this.playerIDs = null!;
			this.connectionIDs = null!;
			return;
		}

		this.Key = key.ToString("D4");
		this.GameStarted = DateTime.Now;

		this.playerIDs = playerIDs.ToArray();
		Random.Shared.Shuffle(this.playerIDs);

		this.connectionIDs = Enumerable.Repeat<string?>(null, playersCount).ToArray();

		SetupGameCloseTimeout();

		if (!disableTurnTimer)
			SetupTurnTimer();
	}



	public bool IsPlayerConnected(string playerID)
	{
		int playerIndex = PlayerIDs.IndexOf(playerID);
		if (playerIndex == -1)
			return false;

		return IsPlayerConnected(playerIndex);
	}

	public bool IsPlayerConnected(int playerIndex) => ConnectionIDs[playerIndex] is not null;



	/// <summary>
	/// To broadcast to all players in this game, leave <paramref name="targetPlayerIndex"/> = <see langword="null"/>.
	/// </summary>
	protected void SendHubMessage(string method, int? targetPlayerIndex, object? arg = null)
	{
		if (targetPlayerIndex.HasValue)
		{
			var connectionID = ConnectionIDs[targetPlayerIndex.Value];
			if (connectionID is null)
				return;

			hub.Clients.Client(connectionID).SendAsync(method, arg);
			return;
		}

		hub.Clients.Groups(Key).SendAsync(method, arg);
	}

	protected void CloseGameWithNoWinner()
	{
		ClosedWithNoResult?.Invoke();
	}



	protected void ResetTurnTimer()
	{
		TurnTimer_LEFT_Seconds = TurnTimer_LIMIT_Seconds;
	}

	protected void ResetTurnTimer(int secondsToAdd)
	{
		TurnTimer_LEFT_Seconds += secondsToAdd;
	}

	protected void SetTurnTimer(int seconds)
	{
		TurnTimer_LEFT_Seconds = seconds;
	}

	private void SetupTurnTimer()
	{
		ResetTurnTimer();
		var token = turnTimerCTS.Token;

		Task.Run(async () =>
		{
			while (true)
			{
				await Task.Delay(1000);
				if (token.IsCancellationRequested)
					break;

				TurnTimer_LEFT_Seconds--;

				lock (this)
					if (TurnTimer_LEFT_Seconds <= 0)
					{
						if (NoPlayersConnected)
						{
							CloseGameWithNoWinner();
							break;
						}

						TurnTimerFired?.Invoke();
						ResetTurnTimer();
					}
			}
		});
	}



	public bool IsPlayerTurn(string playerID)
	{
		lock (this)
		{
			return IsPlayerTurn_Internal(playerID);
		}
	}
	protected abstract bool IsPlayerTurn_Internal(string playerID);

	public bool Surrender(string playerID)
	{
		lock (this)
		{
			bool success = Surrender_Internal(playerID);

			if (success)
				ResetTurnTimer();

			return success;
		}
	}
	protected abstract bool Surrender_Internal(string playerID);

	public bool Request(string playerID, string request, object? data)
	{
		lock (this)
		{
			return Request_Internal(playerID, request, data);
		}
	}
	protected virtual bool Request_Internal(string playerID, string request, object? data) => false;



	public object? GetRelativeState(string playerID)
	{
		lock (this)
		{
			return GetRelativeState_Internal(playerID);
		}
	}
	protected abstract object? GetRelativeState_Internal(string playerID);

	public bool TryUpdateState(string playerID, object data, out string error)
	{
		lock (this)
		{
			return TryUpdateState_Internal(playerID, data, out error);
		}
	}
	protected abstract bool TryUpdateState_Internal(string playerID, object data, out string error);

	protected bool TryDeserializeData<T>(object data, [NotNullWhen(true)] out T? result)
	{
		try
		{
			string str = data?.ToString()!;
			result = Json.Deserialize<T>(str);
			return result is not null;
		}
		catch (Exception)
		{
			result = default;
			return false;
		}
	}


	public bool ConnectPlayer(string playerID, string connectionID)
	{
		int index = PlayerIDs.IndexOf(playerID);
		if (index == -1)
			return false;

		if (IsPlayerConnected(index))
			return true;

		lock (connectionIDs)
		{
			connectionIDs[index] = connectionID;
		}

		PlayerConnected?.Invoke(index);
		return true;
	}

	public bool DisconnectPlayer(string playerID)
	{
		int index = PlayerIDs.IndexOf(playerID);
		if (index == -1)
			return false;

		if (!IsPlayerConnected(index))
			return true;

		lock (connectionIDs)
		{
			connectionIDs[index] = null;
		}

		PlayerDisconnected?.Invoke(index);
		return true;
	}

	private void SetupGameCloseTimeout()
	{
		CancellationTokenSource? cts = null;

		PlayerDisconnected += async _ =>
		{
			lock (connectionIDs)
			{
				if (!NoPlayersConnected || cts is not null)
					return;

				cts = new CancellationTokenSource();
			}

			try
			{
				logger.LogInformation("All players disconnected from {gameName} game with key {key}. Closing this game in 1 minute.", this.gameCore.GameName, this.Key);
				await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);

				CloseGameWithNoWinner();
				DisposeCTS();
			}
			catch (Exception) { }
		};

		PlayerConnected += _ =>
		{
			lock (connectionIDs)
			{
				if (cts is null)
					return;

				logger.LogInformation("Player connected to {gameName} game with key {key}. Stopped closing this game.", this.gameCore.GameName, this.Key);

				try
				{
					DisposeCTS();
				}
				catch (Exception) { }
			}
		};

		void DisposeCTS()
		{
			cts.Cancel();
			cts.Dispose();
			cts = null;
		}
	}



	public PlayableGameInfo GetInfo() => new()
	{
		Key = this.Key,
		GameName = gameCore.GameName,
		PlayerIDs = this.PlayerIDs.ToArray(),
		PlayersConnected = this.ConnectionIDs.Select(x => x is not null).ToArray(),
		WinnerID = this.WinnerID,
		GameStarted = this.GameStarted,
		ErrorWhileCreating = this.ErrorWhileCreating,
	};



	#region Dispose
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			if (keyCaptured)
			{
				gameCore.KeyPool.Return(key);
				keyCaptured = false;
			}

			try
			{
				if (!turnTimerCTS.IsCancellationRequested)
				{
					turnTimerCTS.Cancel();
					turnTimerCTS.Dispose();
				}
			}
			catch (Exception) { }

			WinnerDefined = null;
			ClosedWithNoResult = null;
			PlayerConnected = null;
			PlayerDisconnected = null;
		}

		_disposed = true;
	}
	#endregion
}
