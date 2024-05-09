using System.Collections;

namespace webapi.Models;

/// <summary>
/// Represents a strongly typed list of objects that are fast to get/add/delete but the order of items is chaotic.
/// Items can be accessed by index.
/// </summary>
/// 
/// <typeparam name="T">
/// The type of elements in the list.
/// </typeparam>
public class HeapList<T> : ICollection<T>, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
{
	private readonly List<T> list;

	public int Count => list.Count;
	public int Capacity => list.Capacity;
	public bool IsReadOnly => false;

	public HeapList()
	{
		list = [];
	}

	public HeapList(int capacity)
	{
		list = new(capacity);
	}

	public HeapList(IEnumerable<T> collection)
	{
		list = new(collection);
	}



	public void Add(T item)
	{
		list.Add(item);
	}

	public void AddRange(IEnumerable<T> collection)
	{
		list.AddRange(collection);
	}

	// O(1) remove by swap
	public void RemoveAt(int index)
	{
		int lastIndex = list.Count - 1;

		list[index] = list[lastIndex];
		list.RemoveAt(lastIndex);
	}

	// O(n)
	public bool Remove(T item)
	{
		int index = list.IndexOf(item);
		if (index >= 0)
		{
			RemoveAt(index);
			return true;
		}

		return false;
	}

	public bool Contains(T item) => list.Contains(item);
	public int IndexOf(T item) => list.IndexOf(item);
	public void Clear() => list.Clear();
	public void TrimExcess() => list.TrimExcess();

	public T this[int index]
	{
		get => list[index];
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		list.CopyTo(array, arrayIndex);
	}

	public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
