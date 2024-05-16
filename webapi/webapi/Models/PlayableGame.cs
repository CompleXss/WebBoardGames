namespace webapi.Models;

public abstract class PlayableGame : IDisposable
{
	public delegate PlayableGame Factory(GameCore gameCore, IReadOnlyList<string> playerIDs, object? settings);

	private readonly GameCore gameCore;

	public string Key { get; }
	private readonly int key;

	public List<string> PlayerIDs { get; } = [];
	public int PlayersAlive { get; set; } // todo: удалить?
	public string? WinnerID { get; protected set; }
	public DateTime GameStarted { get; }
	public bool ErrorWhileCreating { get; protected set; }

	private bool keyCaptured;
	private bool _disposed;

	public PlayableGame(GameCore gameCore, IReadOnlyList<string> playerIDs)
	{
		this.gameCore = gameCore;

		keyCaptured = gameCore.KeyPool.TryGetRandom(out key);
		ErrorWhileCreating = !keyCaptured;

		if (playerIDs.Count < gameCore.MinPlayersCount || playerIDs.Count > gameCore.MaxPlayersCount)
		{
			ErrorWhileCreating = true;
			Key = string.Empty;
			return;
		}

		Key = key.ToString();
		GameStarted = DateTime.Now;
		PlayerIDs.AddRange(playerIDs);
	}

	public abstract bool IsPlayerTurn(string playerID);
	public abstract object? GetRelativeState(string playerID);
	public abstract bool TryUpdateState(string playerID, object data, out string error);



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
