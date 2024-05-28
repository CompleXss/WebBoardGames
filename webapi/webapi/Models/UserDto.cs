namespace webapi.Models;

public record UserPasswordDto
{
	public required string Password { get; init; }
}

public record UserLoginDto : UserPasswordDto
{
	public required string Login { get; init; }
}

public record UserRegisterDto : UserLoginDto
{
	public required string Name { get; init; }
}

public record UserPublicInfo
{
	public required string PublicID { get; init; }
	public required string Name { get; init; }
}
