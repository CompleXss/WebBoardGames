using System.Text.Json.Serialization;

namespace webapi.Models.Data;

public partial class UserGameStatistic
{
	[JsonIgnore]
	public long UserID { get; set; }

	[JsonIgnore]
	public int GameID { get; set; }

	public int PlayCount { get; set; }

	public int WinCount { get; set; }

	[JsonIgnore]
	public virtual Game Game { get; set; } = null!;

	public virtual User User { get; set; } = null!;
}
