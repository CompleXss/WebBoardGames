namespace webapi.Models.Data;

public partial class UserRefreshToken
{
	public long UserID { get; set; }

	public string DeviceID { get; set; } = null!;

	public byte[] RefreshTokenHash { get; set; } = null!;

	public DateTime TokenCreated { get; set; }

	public DateTime TokenExpires { get; set; }

	public virtual User User { get; set; } = null!;
}
