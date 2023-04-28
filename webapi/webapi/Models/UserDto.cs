namespace webapi.Models;

public record UserDto
{
    public required string Name { get; init; }
    public required string Password { get; init; }
}