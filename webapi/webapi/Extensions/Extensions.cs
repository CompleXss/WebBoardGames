namespace webapi.Extensions;

public static class Extensions
{
	public static bool NextBoolean(this Random random)
	{
		// Next() returns an int in the range [0 .. int.MaxValue]
		return random.Next() > int.MaxValue / 2;
	}



	#region IReadOnlyList
	public static IEnumerable<T> Shuffle<T>(this IReadOnlyList<T> arr)
	{
		return arr.Shuffle(Random.Shared);
	}

	public static IEnumerable<T> Shuffle<T>(this IReadOnlyList<T> arr, Random random)
	{
		return arr
			.Select(x => (x, random.Next()))
			.OrderBy(tuple => tuple.Item2)
			.Select(tuple => tuple.x);
	}

	public static int IndexOf<T>(this IReadOnlyList<T> arr, Predicate<T> match)
	{
		for (int i = 0; i < arr.Count; i++)
			if (match(arr[i]))
			{
				return i;
			}

		return -1;
	}

	public static int IndexOf<T>(this IReadOnlyList<T> arr, T value)
	{
		for (int i = 0; i < arr.Count; i++)
			if (value is not null && value.Equals(arr[i]))
			{
				return i;
			}

		return -1;
	}
	#endregion



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
