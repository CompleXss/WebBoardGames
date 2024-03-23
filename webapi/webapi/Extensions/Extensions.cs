namespace webapi.Extensions;

public static class Extensions
{
    public static bool NextBoolean(this Random random)
    {
        // Next() returns an int in the range [0 .. int.MaxValue]
        return random.Next() > int.MaxValue / 2;
    }
}
