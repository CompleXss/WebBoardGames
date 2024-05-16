namespace webapi.Models;

public sealed class Lobby : IDisposable
{
	private readonly LobbyCore lobbyCore;

	public object? Settings { get; set; } = null;

	public string Key { get; }
	private readonly int intKey;

	public string? HostID { get; set; }

	public IReadOnlyList<string> PlayerIDs => playerIDs;
	private readonly List<string> playerIDs;

	public IReadOnlyList<string> ConnectionIDs => connectionIDs;
	private readonly List<string> connectionIDs;

	public bool IsEmpty => PlayerIDs.Count == 0;
	public bool IsFull => PlayerIDs.Count >= lobbyCore.MaxPlayers;
	public bool IsEnoughPlayersToStart => PlayerIDs.Count >= lobbyCore.MinPlayersToStartGame;

	public event Action<string>? HostChanged;
	private bool _disposed;

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

	/// <returns>
	/// <see langword="true"/> if player was added successfully.<br/>
	/// <see langword="false"/> if lobby is full.
	/// </returns>
	public bool TryAddPlayer(string playerID, string connectionID)
	{
		if (IsFull)
			return false;

		playerIDs.Add(playerID);
		connectionIDs.Add(connectionID);

		return true;
	}

	public void RemovePlayer(string playerID, string connectionID)
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



	public void Dispose()
	{
		if (_disposed)
			return;

		lobbyCore.KeyPool.Return(intKey);
		HostChanged = null;

		_disposed = true;
	}
}
