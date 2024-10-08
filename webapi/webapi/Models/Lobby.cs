﻿namespace webapi.Models;

public sealed class Lobby : IDisposable
{
	private readonly LobbyCore lobbyCore;

	public object? Settings { get; set; } = null;

	public string Key { get; }
	private readonly int intKey;

	public string? HostID { get; private set; }

	public IReadOnlyList<string> PlayerIDs => playerIDs;
	private readonly List<string> playerIDs;

	public IReadOnlyList<string> ConnectionIDs => connectionIDs;
	private readonly List<string> connectionIDs;

	public bool IsPublic
	{
		get
		{
			lock (this) return isPublic;
		}

		set
		{
			lock (this) isPublic = value;
		}
	}
	private bool isPublic = false;

	public bool IsEmpty => PlayerIDs.Count == 0;
	public bool IsFull => PlayerIDs.Count >= lobbyCore.MaxPlayers;
	public bool IsEnoughPlayersToStart => PlayerIDs.Count >= lobbyCore.MinPlayersToStartGame;

	public event Action<string>? HostChanged;
	private bool isClosing;
	private bool disposed;

	private Lobby(LobbyCore lobbyCore, string hostID, string hostConnectionID, int intKey)
	{
		this.lobbyCore = lobbyCore;
		this.HostID = hostID;
		this.intKey = intKey;
		this.Key = intKey.ToString("D4");

		playerIDs = new(lobbyCore.MaxPlayers)
		{
			hostID
		};
		connectionIDs = new(lobbyCore.MaxPlayers)
		{
			hostConnectionID
		};
	}

	public static Lobby? TryCreateNew(LobbyCore lobbyCore, string hostID, string hostConnectionID)
	{
		bool keyCaptured = lobbyCore.KeyPool.TryGetRandom(out var key);
		if (!keyCaptured)
			return null;

		return new Lobby(lobbyCore, hostID, hostConnectionID, key);
	}

	public void MarkAsClosing()
	{
		isClosing = true;
	}

	/// <returns>
	/// <see langword="true"/> if player was added successfully.<br/>
	/// <see langword="false"/> if lobby is full or should be closed.
	/// </returns>
	public bool TryAddPlayer(string playerID, string connectionID)
	{
		if (isClosing)
			return false;

		lock (this.playerIDs)
		{
			if (IsFull)
				return false;

			playerIDs.Add(playerID);
			connectionIDs.Add(connectionID);

			return true;
		}
	}

	public void RemovePlayer(string playerID, string connectionID)
	{
		if (isClosing)
			return;

		lock (this.playerIDs)
		{
			playerIDs.Remove(playerID);
			connectionIDs.Remove(connectionID);

			// if host left the lobby, pick new host
			if (HostID == playerID)
			{
				HostID = playerIDs.FirstOrDefault();

				if (HostID is not null)
					HostChanged?.Invoke(HostID);
			}
		}
	}

	public bool TrySetHostID(string newHostID)
	{
		lock (playerIDs)
		{
			if (!PlayerIDs.Contains(newHostID))
				return false;

			HostID = newHostID;
		}

		return true;
	}

	public LobbyInfo GetInfo()
	{
		string[] playerIDsCopy;

		lock (this.playerIDs)
			playerIDsCopy = this.PlayerIDs.ToArray();

		return new()
		{
			HostID = this.HostID,
			Key = this.Key,
			PlayerIDs = playerIDsCopy,
			MaxPlayersCount = lobbyCore.MaxPlayers,
			Settings = this.Settings,
			IsFull = this.IsFull,
			IsEnoughPlayersToStart = this.IsEnoughPlayersToStart,
			IsPublic = this.IsPublic,
		};
	}



	public void Dispose()
	{
		lock (this)
		{
			if (disposed)
				return;

			lobbyCore.KeyPool.Return(intKey);
			HostChanged = null;

			disposed = true;
		}
	}
}
