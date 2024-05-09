using webapi.Models;

namespace webapi.Services.Checkers;

public sealed class CheckersLobby : IDisposable
{
	private const int MAX_LOBBIES_COUNT = 10_000;
	private static readonly RandomItemPool<int> lobbyKeyPool = new(Enumerable.Range(0, MAX_LOBBIES_COUNT));

	public string Key { get; }
	public string HostID { get; }
	public string? SecondPlayerID { get; set; }
	public List<string> ConnectionIDs { get; } = [];
	public bool ErrorWhileCreating { get; }

	private readonly int key;

	public CheckersLobby(string hostID)
	{
		HostID = hostID;

		ErrorWhileCreating = !lobbyKeyPool.TryGetRandom(out key);
		Key = ErrorWhileCreating ? "" : key.ToString("D4");
	}

	public void Dispose()
	{
		lobbyKeyPool.Return(key);
	}
}
