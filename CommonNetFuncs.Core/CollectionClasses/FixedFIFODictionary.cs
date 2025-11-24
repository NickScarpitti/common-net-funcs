using System.Collections;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Core.CollectionClasses;

/// <summary>
/// Fixed size dictionary that maintains insertion order and evicts the oldest item when capacity is exceeded.
/// </summary>
/// <remarks>This dictionary enforces a maximum capacity. When the capacity is exceeded, the oldest item is automatically removed to make room for new entries.
/// This implementation is thread-safe and uses a <see cref="ReaderWriterLockSlim"/> to synchronize access.</remarks>
/// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-null.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public class FixedFifoDictionary<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
{
  private readonly ReaderWriterLockSlim readWriteLock = new();
  private readonly int capacity;
  private readonly OrderedDictionary<TKey, TValue?> dictionary;

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
    dictionary = new OrderedDictionary<TKey, TValue?>(capacity);

    if (sourceDictionary != null)
    {
      foreach (KeyValuePair<TKey, TValue?> kvp in sourceDictionary)
      {
        dictionary[kvp.Key] = kvp.Value;
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
        return dictionary.Keys;
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
        return dictionary.Values;
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
        return dictionary.Count;
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
        return dictionary[key];
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
        if (dictionary.ContainsKey(key))
        {
          dictionary[key] = value;
        }
        else
        {
          if (dictionary.Count >= capacity)
          {
            TKey oldestKey = dictionary.GetAt(0).Key;
            dictionary.Remove(oldestKey);
          }
          dictionary[key] = value;
        }
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
      return dictionary.ContainsKey(key);
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
      return dictionary.TryGetValue(key, out value);
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
      dictionary.Clear();
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
      dictionary.TrimExcess();
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
      if (!dictionary.TryAdd(key, value))
      {
        dictionary[key] = value;
      }
      else
      {
        if (dictionary.Count > capacity)
        {
          TKey oldestKey = dictionary.GetAt(0).Key;
          dictionary.Remove(oldestKey);
        }
      }
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
    readWriteLock.EnterUpgradeableReadLock();
    try
    {
      if (dictionary.ContainsKey(key))
      {
        return false;
      }
      readWriteLock.EnterWriteLock();
      try
      {
        if (dictionary.Count >= capacity)
        {
          TKey oldestKey = dictionary.GetAt(dictionary.Count - 1).Key;
          dictionary.Remove(oldestKey);
        }
        dictionary.Insert(0, key, value);
        return true;
      }
      catch
      {
        return false;
      }
      finally
      {
        readWriteLock.ExitWriteLock();
      }
    }
    catch
    {
      return false;
    }
    finally
    {
      readWriteLock.ExitUpgradeableReadLock();
    }
  }

  /// <inheritdoc />
  public bool Remove(TKey key)
  {
    readWriteLock.EnterWriteLock();
    try
    {
      return dictionary.Remove(key);
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
      if (!dictionary.TryAdd(item.Key, item.Value))
      {
        // Update existing item
        dictionary[item.Key] = item.Value; // Note: We're not changing its position in the queue
      }
      else
      {
        // Add new item
        if (dictionary.Count >= capacity)
        {
          // Remove oldest item
          TKey oldestKey = dictionary.GetAt(0).Key;
          dictionary.Remove(oldestKey);
        }
      }
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
      if (!dictionary.TryGetValue(key, out TValue? value))
      {
        readWriteLock.EnterWriteLock();
        try
        {
          value = valueFactory(key);
          if (dictionary.Count >= capacity)
          {
            TKey oldestKey = dictionary.GetAt(0).Key;
            dictionary.Remove(oldestKey);
          }
          dictionary[key] = value;
        }
        finally
        {
          readWriteLock.ExitWriteLock();
        }
      }
      return value!;
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
      return ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Contains(item);
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
      dictionary.ToList().CopyTo(array, arrayIndex);
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
      return ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Remove(item);
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
      return dictionary.GetEnumerator();
    }
    finally
    {
      readWriteLock.ExitReadLock();
    }
  }

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator()
  {
    return ((IEnumerable)dictionary).GetEnumerator();
  }
}
