using webapi.Extensions;

namespace webapi.Types;

/// <summary>
/// Ensures all non-extension methods are thread-safe by utilizing read-write locks.<br/>
/// Extension methods are NOT guaranteed to be thread-safe.<br/>
/// <see cref="GetEnumerator"/> is NOT thread-safe, it just returns List's enumerator.
/// </summary>
/// <typeparam name="T"> The type of elements in the list. </typeparam>
public class ConcurrentList<T> : IList<T>, IReadOnlyList<T>, IDisposable
{
	private readonly List<T> list;
	private readonly ReaderWriterLockSlim rwLock = new();
	protected bool disposed;

	public int Count
	{
		get
		{
			rwLock.EnterReadLock();
			try
			{
				return list.Count;
			}
			finally
			{
				rwLock.ExitReadLock();
			}
		}
	}

	public int Capacity
	{
		get
		{
			rwLock.EnterReadLock();
			try
			{
				return list.Capacity;
			}
			finally
			{
				rwLock.ExitReadLock();
			}
		}
	}

	bool ICollection<T>.IsReadOnly => false;

	public ConcurrentList()
	{
		list = new();
	}
	public ConcurrentList(int capacity)
	{
		list = new(capacity);
	}
	public ConcurrentList(IEnumerable<T> collection)
	{
		list = new(collection);
	}



	public T this[int index]
	{
		get
		{
			rwLock.EnterReadLock();
			try
			{
				return list[index];
			}
			finally
			{
				rwLock.ExitReadLock();
			}
		}
		set
		{
			rwLock.EnterWriteLock();
			try
			{
				list[index] = value;
			}
			finally
			{
				rwLock.ExitWriteLock();
			}
		}
	}

	public void EnsureCapacity(int capacity)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.EnsureCapacity(capacity);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}



	public void Add(T item)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.Add(item);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void AddRange(IEnumerable<T> collection)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.AddRange(collection);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public bool Remove(T item)
	{
		rwLock.EnterWriteLock();
		try
		{
			return list.Remove(item);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void RemoveAt(int index)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.RemoveAt(index);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void RemoveRange(int index, int count)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.RemoveRange(index, count);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public int RemoveAll(Predicate<T> match)
	{
		rwLock.EnterWriteLock();
		try
		{
			return list.RemoveAll(match);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void RemoveBySwap(T item)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.RemoveBySwap(item);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void RemoveBySwapAt(int index)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.RemoveBySwapAt(index);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public T? Find(Predicate<T> match)
	{
		rwLock.EnterReadLock();
		try
		{
			return list.Find(match);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public List<T> FindAll(Predicate<T> match)
	{
		rwLock.EnterReadLock();
		try
		{
			return list.FindAll(match);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public bool Exists(Predicate<T> match)
	{
		rwLock.EnterReadLock();
		try
		{
			return list.Exists(match);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public int IndexOf(T item)
	{
		rwLock.EnterReadLock();
		try
		{
			return list.IndexOf(item);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public void Insert(int index, T item)
	{
		rwLock.EnterWriteLock();
		try
		{
			list.Insert(index, item);
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public void Clear()
	{
		rwLock.EnterWriteLock();
		try
		{
			list.Clear();
		}
		finally
		{
			rwLock.ExitWriteLock();
		}
	}

	public bool Contains(T item)
	{
		rwLock.EnterReadLock();
		try
		{
			return list.Contains(item);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		rwLock.EnterReadLock();
		try
		{
			list.CopyTo(array, arrayIndex);
		}
		finally
		{
			rwLock.ExitReadLock();
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return list.GetEnumerator();
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}



	#region Dispose
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposed)
			return;

		if (disposing)
		{
			try
			{
				rwLock.Dispose();
			}
			catch (Exception) { }
		}

		disposed = true;
	}
	#endregion
}
