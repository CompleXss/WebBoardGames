using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class CheckersHistory
{
	public long Id { get; set; }

	public long UserId { get; set; }

	public long IsWin { get; set; }

	public string DateTimeStart { get; set; } = null!;

	public string DateTimeEnd { get; set; } = null!;

	[JsonIgnore]
	public virtual User User { get; set; } = null!;
}
