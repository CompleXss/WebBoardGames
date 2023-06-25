using System.Security.Cryptography;
using System.Text;

namespace webapi.Models;

public record RefreshToken
{
	public required string Token { get; init; }
	public required DateTime TokenCreated { get; init; }
	public required DateTime TokenExpires { get; init; }

	public byte[] CreateHash() => CreateHash(Token);

	public static byte[] CreateHash(string token)
	{
		byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
		return SHA256.HashData(tokenBytes);
	}

	public static bool VerifyTokenHash(string token, byte[] hash)
	{
		byte[] computedHash = CreateHash(token);
		return computedHash.SequenceEqual(hash);
	}
}