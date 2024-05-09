namespace webapi.Models;

public class RandomItemPool<T>
{
	protected readonly HeapList<T> heapList;
	protected readonly Random random = new();

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
	public void Add(IEnumerable<T> collection)
	{
		heapList.AddRange(collection);
	}

	/// <summary>
	/// Attempts to get a random item from the pool. Removes item from the pool.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if item is successfully retrieved<br/>
	/// <see langword="false"/> if pool is empty
	/// </returns>
	public virtual bool TryGetRandom(out T? item)
	{
		if (heapList.Count == 0)
		{
			item = default;
			return false;
		}

		int index = random.Next(0, heapList.Count);

		item = heapList[index];
		heapList.RemoveAt(index);

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
