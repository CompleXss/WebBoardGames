namespace webapi.Models;

public record ChangeUserPasswordDto
{
	public required string OldPassword { get; init; }
	public required string NewPassword { get; init; }
}
