using System.Text.Json.Serialization;

namespace webapi.Models;

public record GameHistoryDto
{
	[JsonIgnore]
	public Games Game { get; init; }

	public required User[] Winners { get; init; }
	public required User[] Loosers { get; init; }
	public required DateTime DateTimeStart { get; init; }
	public required DateTime DateTimeEnd { get; init; }
}
