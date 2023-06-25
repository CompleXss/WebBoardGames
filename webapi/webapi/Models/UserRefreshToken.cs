namespace webapi.Models;

public partial class UserRefreshToken
{
	public long UserId { get; set; }

	public string DeviceId { get; set; } = null!;

	public byte[] RefreshTokenHash { get; set; } = null!;

	public string TokenCreated { get; set; } = null!;

	public string TokenExpires { get; set; } = null!;

	public virtual User User { get; set; } = null!;
}
