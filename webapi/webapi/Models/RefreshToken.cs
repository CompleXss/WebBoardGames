namespace webapi.Models;

public record RefreshToken
{
	public required string Token { get; init; }
	public required DateTime TokenCreated { get; init; }
	public required DateTime TokenExpires { get; init; }
}