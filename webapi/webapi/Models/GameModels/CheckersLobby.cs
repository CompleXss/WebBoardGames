﻿namespace webapi.Models.GameModels;

public sealed class CheckersLobby : IDisposable
{
	private static readonly HashSet<string> activeKeys = new();

	public List<string> ConnectionIDs { get; } = new();
	public long HostID { get; }
	public long? SecondPlayerID { get; set; }
	public string Key { get; }

	public CheckersLobby(long hostID)
	{
		do
		{
			Key = GetKey(10_000);
		}
		while (!activeKeys.Add(Key));

		HostID = hostID;
	}



	/// <summary> <paramref name="maxValue"/> is exclusive. </summary>
	public string GetKey(int maxValue)
	{
		int key = Random.Shared.Next(0, maxValue);
		string k = key.ToString("D4");
		return k;
	}

	public void Dispose()
	{
		activeKeys.Remove(Key);
	}
}
