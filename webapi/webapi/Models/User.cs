using System.Text.Json.Serialization;

namespace webapi.Models;

public partial class User
{
	public long Id { get; set; }

	public string Name { get; set; } = null!;

	[JsonIgnore]
	public byte[] PasswordHash { get; set; } = null!;

	[JsonIgnore]
	public byte[] PasswordSalt { get; set; } = null!;

	[JsonIgnore]
	public virtual ICollection<CheckersHistory> CheckersHistories { get; set; } = new List<CheckersHistory>();

	[JsonIgnore]
	public virtual CheckersUser? CheckersUser { get; set; }

	[JsonIgnore]
	public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();
}
