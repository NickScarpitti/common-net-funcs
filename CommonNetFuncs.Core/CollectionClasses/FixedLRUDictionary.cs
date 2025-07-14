using System.Collections;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Core.CollectionClasses;

public class FixedLRUDictionary<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
{
    private readonly ReaderWriterLockSlim readWriteLock = new();
    private readonly int capacity;
    private readonly OrderedDictionary<TKey, TValue?> dictionary;

    public FixedLRUDictionary(int capacity, IDictionary<TKey, TValue?>? sourceDictionary = null)
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

    public ICollection<TKey> Keys
    {
        get
        {
            readWriteLock.EnterReadLock();
            try
            {
                return dictionary.Keys.ToList();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }
    }

    public ICollection<TValue?> Values
    {
        get
        {
            readWriteLock.EnterReadLock();
            try
            {
                return dictionary.Values.ToList();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }
    }

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

    public bool IsReadOnly => false;

    public TValue? this[TKey key]
    {
        get
        {
            readWriteLock.EnterUpgradeableReadLock();
            try
            {
                if (!dictionary.TryGetValue(key, out TValue? value))
                {
                    throw new KeyNotFoundException();
                }

                // Move to front if not already there
                int idx = dictionary.IndexOf(key);
                if (idx > 0)
                {
                    readWriteLock.EnterWriteLock();
                    try
                    {
                        dictionary.RemoveAt(idx);
                        dictionary.Insert(0, key, value);
                    }
                    finally
                    {
                        readWriteLock.ExitWriteLock();
                    }
                }
                return value;
            }
            finally
            {
                readWriteLock.ExitUpgradeableReadLock();
            }
        }
        set
        {
            readWriteLock.EnterWriteLock();
            try
            {
                if (!dictionary.Remove(key) && dictionary.Count >= capacity)
                {
                    TKey oldestKey = dictionary.GetAt(dictionary.Count - 1).Key;
                    dictionary.Remove(oldestKey);
                }

                dictionary.Insert(0, key, value);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }
    }

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

    public bool TryGetValue(TKey key, out TValue? value)
    {
        readWriteLock.EnterUpgradeableReadLock();
        try
        {
            if (!dictionary.TryGetValue(key, out value))
            {
                return false;
            }

            int idx = dictionary.IndexOf(key);
            if (idx > 0)
            {
                readWriteLock.EnterWriteLock();
                try
                {
                    dictionary.RemoveAt(idx);
                    dictionary.Insert(0, key, value);
                }
                finally
                {
                    readWriteLock.ExitWriteLock();
                }
            }
            return true;
        }
        finally
        {
            readWriteLock.ExitUpgradeableReadLock();
        }
    }

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

    public void Add(TKey key, TValue? value)
    {
        readWriteLock.EnterWriteLock();
        try
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));
            }

            if (dictionary.Count >= capacity)
            {
                TKey oldestKey = dictionary.GetAt(dictionary.Count - 1).Key;
                dictionary.Remove(oldestKey);
            }
            dictionary.Insert(0, key, value);
        }
        finally
        {
            readWriteLock.ExitWriteLock();
        }
    }

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

    public void Add(KeyValuePair<TKey, TValue?> item)
    {
        Add(item.Key, item.Value);
    }

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

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        readWriteLock.EnterUpgradeableReadLock();
        try
        {
            if (dictionary.TryGetValue(key, out TValue? value))
            {
                dictionary.IndexOf(key);
                int idx = dictionary.IndexOf(key);
                if (idx > 0)
                {
                    readWriteLock.EnterWriteLock();
                    try
                    {
                        dictionary.RemoveAt(idx);
                        dictionary.Insert(0, key, value);
                    }
                    finally
                    {
                        readWriteLock.ExitWriteLock();
                    }
                }
                return value!;
            }
            else
            {
                readWriteLock.EnterWriteLock();
                try
                {
                    value = valueFactory(key);
                    if (dictionary.Count >= capacity)
                    {
                        TKey oldestKey = dictionary.GetAt(dictionary.Count - 1).Key;
                        dictionary.Remove(oldestKey);
                    }
                    dictionary.Insert(0, key, value);
                    return value!;
                }
                finally
                {
                    readWriteLock.ExitWriteLock();
                }
            }
        }
        finally
        {
            readWriteLock.ExitUpgradeableReadLock();
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator()
    {
        readWriteLock.EnterReadLock();
        try
        {
            return dictionary.ToList().GetEnumerator();
        }
        finally
        {
            readWriteLock.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
