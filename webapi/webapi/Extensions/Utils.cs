using Newtonsoft.Json;

namespace webapi.Extensions;

public class Utils
{
	public static T? ReadAsJson<T>(string filePath)
	{
		string json = File.ReadAllText(filePath);
		return JsonConvert.DeserializeObject<T>(json);
	}
}
