using System.Text.Json;
using System.Text.Json.Serialization;

namespace webapi.Extensions;

public static class Json
{
	private static readonly JsonSerializerOptions options = new()
	{
		PropertyNameCaseInsensitive = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	public static T? Deserialize<T>(string json)
	{
		return JsonSerializer.Deserialize<T>(json, options);
	}
}
