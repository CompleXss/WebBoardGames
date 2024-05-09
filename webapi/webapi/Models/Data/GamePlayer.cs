using System.Text.Json.Serialization;

namespace webapi.Models.Data;

public partial class GamePlayer
{
	[JsonIgnore]
	public long GameHistoryID { get; set; }

	[JsonIgnore]
	public long UserID { get; set; }

	public bool IsWinner { get; set; }

	[JsonIgnore]
	public virtual GameHistory GameHistory { get; set; } = null!;

	public virtual User User { get; set; } = null!;
}
