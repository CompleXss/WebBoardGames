namespace webapi.Models;

public interface ILobbyInfo
{
	object? Settings { get; set; }
	string Key { get; }
	string? HostID { get; }
	IReadOnlyList<string> PlayerIDs { get; }
	IReadOnlyList<string> ConnectionIDs { get; }
	bool IsEmpty { get; }
	bool IsFull { get; }
	bool IsEnoughPlayersToStart { get; }
}
