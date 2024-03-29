namespace webapi.Models;

public record UserLoginDto
{
	public required string Login { get; init; }
	public required string Password { get; init; }
}

public record UserRegisterDto : UserLoginDto
{
	public required string Name { get; init; }
}
