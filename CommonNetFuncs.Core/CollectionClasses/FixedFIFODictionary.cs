using System.Collections;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Core.CollectionClasses;

public class FixedFIFODictionary<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
{
    private readonly ReaderWriterLockSlim readWriteLock = new();
    private readonly int capacity;
    private readonly OrderedDictionary<TKey, TValue?> dictionary;

    public FixedFIFODictionary(int capacity, IDictionary<TKey, TValue?>? sourceDictionary = null)
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
        return ((IEnumerable)dictionary).GetEnumerator();
    }
}

//public class FixedFIFODictionaryAlt<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
//{
//    private readonly Lock addRemoveLock = new();
//    private readonly int capacity;
//    private readonly Dictionary<TKey, TValue?> dictionary;

//    private Queue<TKey> insertionOrderQueue;

//    public FixedFIFODictionaryAlt(int capacity, IDictionary<TKey, TValue?>? sourceDictionary = null)
//    {
//        if (capacity <= 0)
//        {
//            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
//        }

//        if (sourceDictionary != null && sourceDictionary.Count > capacity)
//        {
//            throw new ArgumentException("Source dictionary exceeds the specified capacity.", nameof(sourceDictionary));
//        }

//        this.capacity = capacity;
//        dictionary = new Dictionary<TKey, TValue?>(capacity);
//        insertionOrderQueue = new Queue<TKey>(capacity);

//        if (sourceDictionary != null)
//        {
//            foreach (KeyValuePair<TKey, TValue?> kvp in sourceDictionary)
//            {
//                dictionary[kvp.Key] = kvp.Value;
//                insertionOrderQueue.Enqueue(kvp.Key);
//            }
//        }
//    }

//    public ICollection<TKey> Keys => dictionary.Keys;

//    public ICollection<TValue?> Values => dictionary.Values;

//    public int Count => dictionary.Count;

//    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).IsReadOnly;

//    public TValue? this[TKey key]
//    {
//        get
//        {
//            lock (addRemoveLock)
//            {
//                return dictionary[key];
//            }
//        }
//        set
//        {
//            lock (addRemoveLock)
//            {
//                if (dictionary.ContainsKey(key))
//                {
//                    // Update existing item
//                    dictionary[key] = value; // Note: We're not changing its position in the queue
//                }
//                else
//                {
//                    // Add new item
//                    if (dictionary.Count >= capacity)
//                    {
//                        // Remove oldest item
//                        TKey oldestKey = insertionOrderQueue.Dequeue();
//                        dictionary.Remove(oldestKey);
//                    }
//                    dictionary[key] = value;
//                    insertionOrderQueue.Enqueue(key);
//                }
//            }
//        }
//    }

//    public bool ContainsKey(TKey key)
//    {
//        return dictionary.ContainsKey(key);
//    }

//    public bool TryGetValue(TKey key, out TValue? value)
//    {
//        return dictionary.TryGetValue(key, out value);
//    }

//    public void Clear()
//    {
//        dictionary.Clear();
//        insertionOrderQueue.Clear();
//    }

//    public void Add(TKey key, TValue? value)
//    {
//        lock (addRemoveLock)
//        {
//          if (dictionary.ContainsKey(key))
//          {
//              // Update existing item
//              dictionary[key] = value; // Note: We're not changing its position in the queue
//          }
//          else
//          {
//              // Add new item
//              if (dictionary.Count >= capacity)
//              {
//                  // Remove oldest item
//                  TKey oldestKey = insertionOrderQueue.Dequeue();
//                  dictionary.Remove(oldestKey);
//              }
//              dictionary.Add(key, value);
//              insertionOrderQueue.Enqueue(key);
//          }
//        }
//    }

//    public bool Remove(TKey key)
//    {
//        lock (addRemoveLock)
//        {
//          if (dictionary.Remove(key))
//          {
//              insertionOrderQueue = new Queue<TKey>(insertionOrderQueue.Where(k => !EqualityComparer<TKey>.Default.Equals(k, key)));
//              return true;
//          }
//        }
//        return false;
//    }

//    public void Add(KeyValuePair<TKey, TValue?> item)
//    {
//        lock (addRemoveLock)
//        {
//          ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Add(item);
//          insertionOrderQueue.Enqueue(item.Key);
//        }
//    }

//    public bool Contains(KeyValuePair<TKey, TValue?> item)
//    {
//        return ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Contains(item);
//    }

//    public void CopyTo(KeyValuePair<TKey, TValue?>[] array, int arrayIndex)
//    {
//        ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).CopyTo(array, arrayIndex);
//    }

//    public bool Remove(KeyValuePair<TKey, TValue?> item)
//    {
//        if (((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Remove(item))
//        {
//            insertionOrderQueue = new Queue<TKey>(insertionOrderQueue.Where(k => !EqualityComparer<TKey>.Default.Equals(k, item.Key)));
//            return true;
//        }
//        return false;
//    }

//    public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator()
//    {
//        return ((IEnumerable<KeyValuePair<TKey, TValue?>>)dictionary).GetEnumerator();
//    }

//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return ((IEnumerable)dictionary).GetEnumerator();
//    }
//}
