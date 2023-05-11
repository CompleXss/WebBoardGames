namespace webapi.Models.GameModels;

public sealed class CheckersLobbyRoom : IDisposable
{
	private static readonly TimeSpan hostLiveTime = TimeSpan.FromMinutes(5);
	private static readonly HashSet<int> activeKeys = new();

	public long HostID { get; }
	public DateTime HostLastActiveTime { get; set; }

	public long? secondPlayerID;
	public long? SecondPlayerID
	{
		get => secondPlayerID;
		set
		{
			secondPlayerID = value;
			SecondPlayerLastActiveTime = DateTime.UtcNow;
		}
	}
	public DateTime SecondPlayerLastActiveTime { get; set; }
	public string RoomKey { get; }

	public int roomKey;

	public CheckersLobbyRoom(long hostID)
	{
		int key;
		do
		{
			key = Random.Shared.Next(0, 100_000);
		}
		while (!activeKeys.Add(key));

		roomKey = key;
		RoomKey = key.ToString();
		HostID = hostID;
		HostLastActiveTime = DateTime.UtcNow;
	}

	public bool HostIsDead()
	{
		return HostLastActiveTime.Add(hostLiveTime) <= DateTime.UtcNow;
	}

	public void Dispose()
	{
		activeKeys.Remove(roomKey);
	}
}
