namespace webapi.Models;

public record PlayHistoryDto
{
    public required Games Game { get; init; }

    public required long UserId{ get; init; }
    public required bool IsWin { get; init; }
    public required DateTime DateTimeStart { get; init; }
    public required DateTime DateTimeEnd { get; init; }
}
