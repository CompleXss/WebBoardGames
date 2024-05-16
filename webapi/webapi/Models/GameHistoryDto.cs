using System.Text.Json.Serialization;
using webapi.Models.Data;

namespace webapi.Models;

public record GameHistoryDto
{
	[JsonIgnore]
	public GameNames Game { get; init; }

	public required User[] Winners { get; init; }
	public required User[] Loosers { get; init; }
	public required DateTime DateTimeStart { get; init; }
	public required DateTime DateTimeEnd { get; init; }
}
