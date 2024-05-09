using System.Text.Json.Serialization;

namespace webapi.Models.Data;

public partial class Game
{
	[JsonIgnore]
	public int ID { get; set; }

	public string Name { get; set; } = null!;

	[JsonIgnore]
	public virtual ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>();

	[JsonIgnore]
	public virtual ICollection<UserGameStatistic> UserGameStatistics { get; set; } = new List<UserGameStatistic>();
}
