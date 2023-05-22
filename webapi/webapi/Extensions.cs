namespace webapi;

public static class Extensions
{
	public static bool NextBoolean(this Random random)
	{
		// Next() returns an int in the range [0..int.MaxValue]
		return random.Next() > Int32.MaxValue / 2;
	}
}
