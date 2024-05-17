using webapi.Extensions;
using webapi.Models;

namespace webapi.Games;

public abstract class PlayableGame : IDisposable
{
	public delegate PlayableGame Factory(GameCore gameCore, IReadOnlyList<string> playerIDs, object? settings);

	private readonly GameCore gameCore;

	public string Key { get; }
	private readonly int key;

	public IReadOnlyList<PlayerInfo> Players => players;
	private readonly PlayerInfo[] players;

	public string? WinnerID { get; protected set; }
	public DateTime GameStarted { get; }
	public bool ErrorWhileCreating { get; protected set; }
	public bool NoPlayersConnected => !Players.Any(x => x.isConnected);

	private bool keyCaptured;
	private bool _disposed;

	public PlayableGame(GameCore gameCore, IReadOnlyList<string> playerIDs)
	{
		this.gameCore = gameCore;

		this.keyCaptured = gameCore.KeyPool.TryGetRandom(out key);
		this.ErrorWhileCreating = !keyCaptured;

		if (playerIDs.Count < gameCore.MinPlayersCount || playerIDs.Count > gameCore.MaxPlayersCount)
		{
			this.ErrorWhileCreating = true;
			this.Key = string.Empty;
			this.players = [];
			return;
		}

		this.Key = key.ToString();
		this.GameStarted = DateTime.Now;
		this.players = playerIDs.Select(x => new PlayerInfo(x, false)).ToArray();
	}

	public abstract bool IsPlayerTurn(string playerID);
	public abstract object? GetRelativeState(string playerID);
	public abstract bool TryUpdateState(string playerID, object data, out string error);

	public bool ConnectPlayer(string playerID)
	{
		int index = players.IndexOf(x => x.playerID == playerID);
		if (index == -1)
			return false;

		var item = players[index];
		if (item.isConnected)
			return true;

		lock (players)
		{
			players[index] = new PlayerInfo(item.playerID, true);
		}
		return true;
	}

	public bool DisconnectPlayer(string playerID)
	{
		int index = players.IndexOf(x => x.playerID == playerID);
		if (index == -1)
			return false;

		var item = players[index];
		if (!item.isConnected)
			return true;

		lock (players)
		{
			players[index] = new PlayerInfo(item.playerID, false);
		}
		return true;
	}

	public PlayableGameInfo GetInfo() => new()
	{
		Key = this.Key,
		Players = this.Players.ToArray(),
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
		}

		_disposed = true;
	}
	#endregion

	public readonly struct PlayerInfo
	{
		public readonly string playerID;
		public readonly bool isConnected;

		public PlayerInfo(string playerID, bool isConnected)
		{
			this.playerID = playerID;
			this.isConnected = isConnected;
		}
	}
}
