namespace webapi.Models;

public record UserTokenInfo
{
	public required long ID { get; set; }
	public required string Name { get; set; }
}
