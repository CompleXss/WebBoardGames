using Microsoft.AspNetCore.SignalR;
using webapi.Extensions;
using webapi.Models;

namespace webapi.Games;

public abstract class PlayableGame : IDisposable
{
	public delegate PlayableGame Factory(GameCore gameCore, IHubContext hub, IReadOnlyList<string> playerIDs, object? settings);

	private readonly GameCore gameCore;
	private readonly IHubContext hub;

	public string Key { get; }
	private readonly int key;

	public IReadOnlyList<string> PlayerIDs => playerIDs;
	private readonly string[] playerIDs;

	public IReadOnlyList<bool> PlayersConnected => playersConnected;
	private readonly bool[] playersConnected;

	public string? WinnerID { get; protected set; }
	public DateTime GameStarted { get; }
	public bool ErrorWhileCreating { get; protected set; }
	public bool NoPlayersConnected => !PlayersConnected.Contains(true);

	private bool keyCaptured;
	private bool _disposed;

	public PlayableGame(GameCore gameCore, IHubContext hub, IReadOnlyList<string> playerIDs)
	{
		this.gameCore = gameCore;
		this.hub = hub;
		this.keyCaptured = gameCore.KeyPool.TryGetRandom(out key);
		this.ErrorWhileCreating = !keyCaptured;

		int playersCount = playerIDs.Count;

		if (playersCount < gameCore.MinPlayersCount || playersCount > gameCore.MaxPlayersCount)
		{
			this.ErrorWhileCreating = true;
			this.Key = null!;
			this.playerIDs = null!;
			this.playersConnected = null!;
			return;
		}

		this.Key = key.ToString();
		this.GameStarted = DateTime.Now;

		this.playerIDs = playerIDs.ToArray();
		Random.Shared.Shuffle(this.playerIDs);

		this.playersConnected = Enumerable.Repeat(false, playersCount).ToArray();
	}

	protected void SendHubMessage(string method, object? arg = null)
	{
		hub.Clients.Groups(Key).SendAsync(method, arg); // todo: execute sync ?
	}

	public void SendChatMessage(string message)
	{
		lock (this)
		{
			SendChatMessage_Internal(message);
		}
	}

	protected virtual void SendChatMessage_Internal(string message) { }

	public bool IsPlayerTurn(string playerID)
	{
		lock (this)
		{
			return IsPlayerTurn_Internal(playerID);
		}
	}
	protected abstract bool IsPlayerTurn_Internal(string playerID);

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



	public bool ConnectPlayer(string playerID)
	{
		int index = PlayerIDs.IndexOf(playerID);
		if (index == -1)
			return false;

		if (PlayersConnected[index])
			return true;

		lock (playersConnected)
		{
			playersConnected[index] = true;
		}
		return true;
	}

	public bool DisconnectPlayer(string playerID)
	{
		int index = PlayerIDs.IndexOf(playerID);
		if (index == -1)
			return false;

		if (!PlayersConnected[index])
			return true;

		lock (playersConnected)
		{
			playersConnected[index] = false;
		}
		return true;
	}

	public PlayableGameInfo GetInfo() => new()
	{
		Key = this.Key,
		PlayerIDs = this.PlayerIDs.ToArray(),
		PlayersConnected = this.PlayersConnected.ToArray(),
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
}
