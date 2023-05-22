namespace webapi.Models;

public record PlayHistoryDto
{
	public required Games Game { get; init; }

	public required long WinnerId { get; init; }
	public required long LooserID { get; init; }
	public required DateTime DateTimeStart { get; init; }
	public required DateTime DateTimeEnd { get; init; }
}
