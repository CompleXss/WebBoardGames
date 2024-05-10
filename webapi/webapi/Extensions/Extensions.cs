﻿namespace webapi.Extensions;

public static class Extensions
{
	public static bool NextBoolean(this Random random)
	{
		// Next() returns an int in the range [0 .. int.MaxValue]
		return random.Next() > int.MaxValue / 2;
	}

	#region List
	// O(1)
	public static void RemoveBySwapAt<T>(this List<T> list, int index)
	{
		int lastIndex = list.Count - 1;

		list[index] = list[lastIndex];
		list.RemoveAt(lastIndex);
	}

	// O(n) because of IndexOf
	public static bool RemoveBySwap<T>(this List<T> list, T item)
	{
		int index = list.IndexOf(item);
		if (index >= 0)
		{
			list.RemoveBySwapAt(index);
			return true;
		}

		return false;
	}
	#endregion
}
