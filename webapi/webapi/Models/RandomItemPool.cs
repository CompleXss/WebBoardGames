using webapi.Types;

namespace webapi.Models;

public class RandomItemPool<T>
{
	protected readonly ConcurrentList<T> heapList; // don't care about items order
	protected readonly Random random = new();

	public int Count => heapList.Count;

	public RandomItemPool(int capacity)
	{
		heapList = new(capacity);
	}

	public RandomItemPool(IEnumerable<T> collection)
	{
		heapList = new(collection);
	}



	/// <summary>
	/// Adds <paramref name="collection"/> to the pool.
	/// </summary>
	public void AddRange(IEnumerable<T> collection)
	{
		heapList.AddRange(collection);
	}

	/// <summary>
	/// Attempts to get a random item from the pool. Removes item from the pool on success.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if item is successfully retrieved<br/>
	/// <see langword="false"/> if pool is empty
	/// </returns>
	public bool TryGetRandom(out T? item)
	{
		if (heapList.Count == 0)
		{
			item = default;
			return false;
		}

		int index = random.Next(0, heapList.Count);

		item = heapList[index];
		heapList.RemoveBySwapAt(index);

		return true;
	}

	/// <summary>
	/// Returns item to the pool.
	/// </summary>
	public void Return(T item)
	{
		heapList.Add(item);
	}
}
