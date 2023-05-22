using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class CheckersHistory
{
	[JsonIgnore]
	public long Id { get; set; }

	[JsonIgnore]
	public long WinnerId { get; set; }

	[JsonIgnore]
	public long LooserId { get; set; }

	public virtual User Winner { get; set; } = null!;

	public virtual User Looser { get; set; } = null!;

	public string DateTimeStart { get; set; } = null!;

	public string DateTimeEnd { get; set; } = null!;
}
