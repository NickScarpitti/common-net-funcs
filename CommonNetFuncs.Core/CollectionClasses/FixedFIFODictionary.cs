using System.Collections;

namespace CommonNetFuncs.Core.CollectionClasses;

/// <summary>
/// Fixed size dictionary that maintains insertion order and evicts the oldest item when capacity is exceeded.
/// </summary>
/// <remarks>This dictionary enforces a maximum capacity. When the capacity is exceeded, the oldest item is automatically removed to make room for new entries.
/// This implementation is thread-safe and uses a <see cref="ReaderWriterLockSlim"/> to synchronize access.
/// Uses <see cref="System.Collections.Generic.OrderedDictionary{TKey, TValue}"/> on net9.0+ (the fastest option available there), falling back to a
/// <see cref="Dictionary{TKey, TValue}"/> + <see cref="LinkedList{T}"/> implementation on older target frameworks where OrderedDictionary doesn't exist.</remarks>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-null.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public class FixedFifoDictionary<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
{
	private readonly ReaderWriterLockSlim readWriteLock = new();
	private readonly int capacity;

#if NET9_0_OR_GREATER
	private readonly OrderedDictionary<TKey, TValue?> dictionary;
#else
	private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue?>>> lookup;

	// First = oldest item (next to be evicted), Last = newest item
	private readonly LinkedList<KeyValuePair<TKey, TValue?>> order = new();
#endif

	/// <summary>
	/// Initializes a new instance of the <see cref="FixedFifoDictionary{TKey,TValue}"/> class with the specified capacity and an optional source dictionary.
	/// </summary>
	/// <param name="capacity">The maximum number of items the dictionary can hold.</param>
	/// <param name="sourceDictionary">Optional: A dictionary to initialize the contents of the new dictionary.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the capacity is less than or equal to zero.</exception>
	/// <exception cref="ArgumentException">Thrown when the source dictionary exceeds the specified capacity.</exception>
	public FixedFifoDictionary(int capacity, IDictionary<TKey, TValue?>? sourceDictionary = null)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
		}

		if (sourceDictionary != null && sourceDictionary.Count > capacity)
		{
			throw new ArgumentException("Source dictionary exceeds the specified capacity.", nameof(sourceDictionary));
		}

		this.capacity = capacity;
#if NET9_0_OR_GREATER
		dictionary = new OrderedDictionary<TKey, TValue?>(capacity);
#else
		lookup = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue?>>>(capacity);
#endif

		if (sourceDictionary != null)
		{
			foreach (KeyValuePair<TKey, TValue?> kvp in sourceDictionary)
			{
				AddNewest(kvp.Key, kvp.Value);
			}
		}
	}

	/// <inheritdoc />
	public ICollection<TKey> Keys
	{
		get
		{
			readWriteLock.EnterReadLock();
			try
			{
#if NET9_0_OR_GREATER
				return dictionary.Keys;
#else
				return order.Select(static x => x.Key).ToList();
#endif
			}
			finally
			{
				readWriteLock.ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public ICollection<TValue?> Values
	{
		get
		{
			readWriteLock.EnterReadLock();
			try
			{
#if NET9_0_OR_GREATER
				return dictionary.Values;
#else
				return order.Select(static x => x.Value).ToList();
#endif
			}
			finally
			{
				readWriteLock.ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public int Count
	{
		get
		{
			readWriteLock.EnterReadLock();
			try
			{
#if NET9_0_OR_GREATER
				return dictionary.Count;
#else
				return lookup.Count;
#endif
			}
			finally
			{
				readWriteLock.ExitReadLock();
			}
		}
	}

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public TValue? this[TKey key]
	{
		get
		{
			readWriteLock.EnterReadLock();
			try
			{
#if NET9_0_OR_GREATER
				return dictionary[key];
#else
				return lookup[key].Value.Value;
#endif
			}
			finally
			{
				readWriteLock.ExitReadLock();
			}
		}
		set
		{
			readWriteLock.EnterWriteLock();
			try
			{
#if NET9_0_OR_GREATER
				if (dictionary.ContainsKey(key))
				{
					dictionary[key] = value;
				}
				else
				{
					AddNewest(key, value);
				}
#else
				if (lookup.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
				{
					node.Value = new(key, value);
				}
				else
				{
					AddNewest(key, value);
				}
#endif
			}
			finally
			{
				readWriteLock.ExitWriteLock();
			}
		}
	}

	/// <inheritdoc />
	public bool ContainsKey(TKey key)
	{
		readWriteLock.EnterReadLock();
		try
		{
#if NET9_0_OR_GREATER
			return dictionary.ContainsKey(key);
#else
			return lookup.ContainsKey(key);
#endif
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <inheritdoc />
	public bool TryGetValue(TKey key, out TValue? value)
	{
		readWriteLock.EnterReadLock();
		try
		{
#if NET9_0_OR_GREATER
			return dictionary.TryGetValue(key, out value);
#else
			if (lookup.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
			{
				value = node.Value.Value;
				return true;
			}
			value = default;
			return false;
#endif
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			dictionary.Clear();
#else
			lookup.Clear();
			order.Clear();
#endif
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	public void TrimExcess()
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			dictionary.TrimExcess();
#else
			lookup.TrimExcess();
#endif
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public void Add(TKey key, TValue? value)
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			if (dictionary.ContainsKey(key))
			{
				dictionary[key] = value;
			}
			else
			{
				AddNewest(key, value);
			}
#else
			if (lookup.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
			{
				node.Value = new(key, value);
			}
			else
			{
				AddNewest(key, value);
			}
#endif
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public void Add(KeyValuePair<TKey, TValue?> item)
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			if (dictionary.ContainsKey(item.Key))
			{
				// Update existing item - not changing its position in the queue
				dictionary[item.Key] = item.Value;
			}
			else
			{
				AddNewest(item.Key, item.Value);
			}
#else
			if (lookup.TryGetValue(item.Key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
			{
				// Update existing item - not changing its position in the queue
				node.Value = item;
			}
			else
			{
				AddNewest(item.Key, item.Value);
			}
#endif
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Attempts to add the specified key and value to the dictionary.
	/// </summary>
	/// <param name="key">Key of the value to add.</param>
	/// <param name="value">Value to add.</param>
	/// <returns><see langword="true"/> if the key/value pair was added successfully, <see langword="false"/> otherwise.</returns>
	public bool TryAdd(TKey key, TValue? value)
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			if (dictionary.ContainsKey(key))
			{
				return false;
			}
#else
			if (lookup.ContainsKey(key))
			{
				return false;
			}
#endif
			AddNewest(key, value);
			return true;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public bool Remove(TKey key)
	{
		readWriteLock.EnterWriteLock();
		try
		{
			return RemoveInternal(key);
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Gets the value associated with the specified key, or adds a new key/value pair to the dictionary if the key does not exist.
	/// </summary>
	/// <param name="key">The key to locate in the dictionary.</param>
	/// <param name="valueFactory">A function to generate a value for the key if it does not exist.</param>
	/// <returns>The value associated with the specified key.</returns>
	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		readWriteLock.EnterUpgradeableReadLock();
		try
		{
#if NET9_0_OR_GREATER
			if (dictionary.TryGetValue(key, out TValue? existing))
			{
				return existing!;
			}
#else
			if (lookup.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
			{
				return node.Value.Value!;
			}
#endif

			readWriteLock.EnterWriteLock();
			try
			{
				TValue value = valueFactory(key);
				AddNewest(key, value);
				return value;
			}
			finally
			{
				readWriteLock.ExitWriteLock();
			}
		}
		finally
		{
			readWriteLock.ExitUpgradeableReadLock();
		}
	}

	/// <inheritdoc />
	public bool Contains(KeyValuePair<TKey, TValue?> item)
	{
		readWriteLock.EnterReadLock();
		try
		{
#if NET9_0_OR_GREATER
			return dictionary.TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue?>.Default.Equals(value, item.Value);
#else
			return lookup.TryGetValue(item.Key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node) && EqualityComparer<TValue?>.Default.Equals(node.Value.Value, item.Value);
#endif
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <inheritdoc />
	public void CopyTo(KeyValuePair<TKey, TValue?>[] array, int arrayIndex)
	{
		readWriteLock.EnterReadLock();
		try
		{
#if NET9_0_OR_GREATER
			((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).CopyTo(array, arrayIndex);
#else
			order.ToList().CopyTo(array, arrayIndex);
#endif
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <inheritdoc />
	public bool Remove(KeyValuePair<TKey, TValue?> item)
	{
		readWriteLock.EnterWriteLock();
		try
		{
#if NET9_0_OR_GREATER
			return dictionary.TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue?>.Default.Equals(value, item.Value) && RemoveInternal(item.Key);
#else
			return lookup.TryGetValue(item.Key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node)
				&& EqualityComparer<TValue?>.Default.Equals(node.Value.Value, item.Value)
				&& RemoveInternal(item.Key);
#endif
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator()
	{
		readWriteLock.EnterReadLock();
		try
		{
#if NET9_0_OR_GREATER
			return dictionary.ToList().GetEnumerator();
#else
			return order.ToList().GetEnumerator();
#endif
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	// Callers must already hold the write lock.
	private void AddNewest(TKey key, TValue? value)
	{
#if NET9_0_OR_GREATER
		if (dictionary.Count >= capacity)
		{
			dictionary.RemoveAt(0);
		}
		dictionary.Add(key, value);
#else
		if (lookup.Count >= capacity)
		{
			RemoveInternal(order.First!.Value.Key);
		}

		LinkedListNode<KeyValuePair<TKey, TValue?>> node = order.AddLast(new KeyValuePair<TKey, TValue?>(key, value));
		lookup[key] = node;
#endif
	}

	// Callers must already hold the write lock.
	private bool RemoveInternal(TKey key)
	{
#if NET9_0_OR_GREATER
		return dictionary.Remove(key);
#else
		if (!lookup.TryGetValue(key, out LinkedListNode<KeyValuePair<TKey, TValue?>>? node))
		{
			return false;
		}

		order.Remove(node);
		lookup.Remove(key);
		return true;
#endif
	}
}
