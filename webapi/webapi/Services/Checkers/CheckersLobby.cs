namespace webapi.Services.Checkers;

public sealed class CheckersLobby : IDisposable
{
	private const int MAX_LOBBIES_COUNT = 10_000;
	private static readonly HashSet<string> activeKeys = [];

	public string Key { get; }
	public string HostID { get; }
	public string? SecondPlayerID { get; set; }
	public List<string> ConnectionIDs { get; } = [];
	public bool ErrorWhileCreating { get; }

	public CheckersLobby(string hostID)
	{
		HostID = hostID;

		if (activeKeys.Count >= MAX_LOBBIES_COUNT)
		{
			ErrorWhileCreating = true;
			Key = "";
			return;
		}

		int retries = 0;
		do
		{
			Key = GetRandomKey(MAX_LOBBIES_COUNT);

			if (++retries == 1000)
			{
				ErrorWhileCreating = true;
				break;
			}
		}
		while (!activeKeys.Add(Key));
	}



	/// <summary> <paramref name="maxValue"/> is exclusive. </summary>
	private static string GetRandomKey(int maxValue)
	{
		int key = Random.Shared.Next(0, maxValue);
		return key.ToString("D4");
	}

	public void Dispose()
	{
		activeKeys.Remove(Key);
	}
}
