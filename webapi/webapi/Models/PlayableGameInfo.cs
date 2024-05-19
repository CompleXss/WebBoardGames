namespace webapi.Models;

public record PlayableGameInfo
{
	public required string Key { get; init; }
	public required IReadOnlyList<string> PlayerIDs { get; init; }
	public required IReadOnlyList<bool> PlayersConnected { get; init; }
	public required string? WinnerID { get; init; }
	public required DateTime GameStarted { get; init; }
	public required bool ErrorWhileCreating { get; init; }
}
