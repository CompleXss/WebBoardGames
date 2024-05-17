namespace webapi.Models;

public record LobbyInfo
{
	public required object? Settings { get; init; }

	public required string? HostID { get; init; }
	public required string Key { get; init; }
	public required IReadOnlyList<string> PlayerIDs { get; init; }

	public bool IsEmpty => PlayerIDs.Count == 0;
	public required bool IsFull { get; init; }
	public required bool IsEnoughPlayersToStart { get; init; }
}
