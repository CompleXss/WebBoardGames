using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class GameHistory
{
	[JsonIgnore]
	public long ID { get; set; }

	[JsonIgnore]
	public int GameID { get; set; }

	public DateTime DateTimeStart { get; set; }

	public DateTime DateTimeEnd { get; set; }

	[JsonIgnore]
	public virtual Game Game { get; set; } = null!;

	public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();
}
