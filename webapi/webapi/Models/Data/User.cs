using System.Text.Json.Serialization;

namespace webapi.Models.Data;

public partial class User
{
	[JsonIgnore]
	public long ID { get; set; }

	public string PublicID { get; set; } = null!;

	[JsonIgnore]
	public string Login { get; set; } = null!;

	public string Name { get; set; } = null!;

	[JsonIgnore]
	public byte[] PasswordHash { get; set; } = null!;

	[JsonIgnore]
	public byte[] PasswordSalt { get; set; } = null!;

	[JsonIgnore]
	public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();

	[JsonIgnore]
	public virtual ICollection<UserGameStatistic> UserGameStatistics { get; set; } = new List<UserGameStatistic>();

	[JsonIgnore]
	public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();
}
