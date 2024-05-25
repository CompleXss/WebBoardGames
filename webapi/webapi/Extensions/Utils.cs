using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace webapi.Extensions;

public partial class Utils
{
	public static T? ReadAsJson<T>(string filePath)
	{
		string json = File.ReadAllText(filePath);
		return JsonConvert.DeserializeObject<T>(json);
	}

	public static string FormatNumberWithCommas(string number)
	{
		return NumberWithCommasRegex().Replace(number, ",");
	}

	[GeneratedRegex(@"\B(?=(\d{3})+(?!\d))")]
	private static partial Regex NumberWithCommasRegex();
}
