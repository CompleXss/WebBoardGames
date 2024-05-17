using webapi.Games;

namespace webapi.Models;

public record PlayableGameInfo
{
	public required string Key { get; init; }
	public required IReadOnlyList<PlayableGame.PlayerInfo> Players { get; init; }
	public required string? WinnerID { get; init; }
	public required DateTime GameStarted { get; init; }
	public required bool ErrorWhileCreating { get; init; }
}
