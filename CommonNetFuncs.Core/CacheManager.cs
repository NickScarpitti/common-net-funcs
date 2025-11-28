using System.Collections.Concurrent;
using CommonNetFuncs.Core.CollectionClasses;

namespace CommonNetFuncs.Core;

public interface ICacheManagerApi<TKey, TValue>
{
	void ClearAllCaches();

	void ClearCache();

	void ClearLimitedCache();

	void SetLimitedCacheSize(int size);

	void SetUseLimitedCache(bool useLimitedCache);

	int GetLimitedCacheSize();

	bool IsUsingLimitedCache();

	IReadOnlyDictionary<TKey, TValue> GetCache();

	IReadOnlyDictionary<TKey, TValue?> GetLimitedCache();

	TValue GetOrAddLimitedCache(TKey key, Func<TKey, TValue> func);

	TValue GetOrAddCache(TKey key, Func<TKey, TValue> func);

	bool TryAddLimitedCache(TKey key, TValue value);

	bool TryAddCache(TKey key, TValue value);
}

/// <summary>
/// Class to manage thread-safe dictionary caches. Offers both a fixed size LRU (Least Recently Used) dictionary called "LimitedCache" and a unlimited sized cache called "Cache".
/// </summary>
/// <param name="limitedCacheSize">Optional: How large to make the limited cache. Default value is 100.</param>
/// <param name="useLimitedCache">Optional: Whether to use limited sized cache or not when first initialized. Default is true.</param>
public sealed class CacheManager<TKey, TValue>(int limitedCacheSize = 100, bool useLimitedCache = true) : ICacheManagerApi<TKey, TValue> where TKey : notnull
{
	private readonly ReaderWriterLockSlim readWriteLock = new();

	private int limitedCacheSize = limitedCacheSize;

	private bool UseLimitedCache { get; set; } = useLimitedCache;

	private ConcurrentDictionary<TKey, TValue> Cache { get; } = new();

	private FixedLruDictionary<TKey, TValue> LimitedCache { get; set; } = new(limitedCacheSize);

	/// <summary>
	/// Clear both <see cref="Cache"/> and <see cref="LimitedCache"/>.
	/// </summary>
	public void ClearAllCaches()
	{
		ClearCache();
		ClearLimitedCache();
	}

	/// <summary>
	/// Clears <see cref="Cache"/>.
	/// </summary>
	public void ClearCache()
	{
		readWriteLock.EnterWriteLock();
		try
		{
			Cache.Clear();
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Clears <see cref="LimitedCache"/>.
	/// </summary>
	public void ClearLimitedCache()
	{
		readWriteLock.EnterWriteLock();
		try
		{
			LimitedCache.Clear();
			LimitedCache.TrimExcess();
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Clears <see cref="LimitedCache"/> and sets the size to the specified value.
	/// </summary>
	/// <param name="size">Maximum number of entries to allow in <see cref="LimitedCache"/> before removing least recently used entry.</param>
	public void SetLimitedCacheSize(int size)
	{
		if (size < 1)
		{
			SetUseLimitedCache(false);
		}

		readWriteLock.EnterWriteLock();
		try
		{
			limitedCacheSize = size;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}

		if (UseLimitedCache)
		{
			ClearLimitedCache();
			readWriteLock.EnterWriteLock();
			try
			{
				LimitedCache = new(limitedCacheSize);
			}
			finally
			{
				readWriteLock.ExitWriteLock();
			}
		}
	}

	/// <summary>
	/// Clears caches and initializes <see cref="LimitedCache"/> to use the size specified by <see cref="limitedCacheSize"/> or 1 if <see cref="UseLimitedCache"/> is false.
	/// </summary>
	/// <param name="useLimitedCache">When true, uses cache with limited number of total records.</param>
	public void SetUseLimitedCache(bool useLimitedCache)
	{
		ClearAllCaches();
		SetLimitedCacheSize(useLimitedCache ? limitedCacheSize : 1);
		readWriteLock.EnterWriteLock();
		try
		{
			UseLimitedCache = useLimitedCache;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Gets the <see cref="limitedCacheSize"/> value.
	/// </summary>
	/// <returns>The value of <see cref="limitedCacheSize"/>.</returns>
	public int GetLimitedCacheSize()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return limitedCacheSize;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Returns whether the <see cref="LimitedCache"/> is being used or not.
	/// </summary>
	/// <returns><see langword="true"/> if <see cref="LimitedCache"/> is being used.</returns>
	public bool IsUsingLimitedCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return UseLimitedCache;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Get a readonly copy of <see cref="Cache"/>.
	/// </summary>
	/// <returns>A readonly copy of <see cref="Cache"/>.</returns>
	public IReadOnlyDictionary<TKey, TValue> GetCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return Cache.AsReadOnly();
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Get a readonly copy of <see cref="LimitedCache"/>.
	/// </summary>
	/// <returns>A readonly copy of <see cref="LimitedCache"/>.</returns>
	public IReadOnlyDictionary<TKey, TValue?> GetLimitedCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return LimitedCache.AsReadOnly();
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Gets or adds a record to <see cref="LimitedCache"/> and returns the value.
	/// </summary>
	/// <param name="key">Key of the record to get or add to <see cref="LimitedCache"/>.</param>
	/// <param name="func">Function to generate the value for the new record to get or add to <see cref="LimitedCache"/>.</param>
	/// <returns>The value of the record that was retrieved or added to <see cref="LimitedCache"/>.</returns>
	public TValue GetOrAddLimitedCache(TKey key, Func<TKey, TValue> func)
	{
		return LimitedCache.GetOrAdd(key, func);
	}

	/// <summary>
	/// Gets or adds a record to <see cref="Cache"/> and returns the value.
	/// </summary>
	/// <param name="key">Key of the record to get or add to <see cref="Cache"/>.</param>
	/// <param name="func">Function to generate the value for the new record to get or add to <see cref="Cache"/>.</param>
	/// <returns>The value of the record that was retrieved or added to <see cref="Cache"/>.</returns>
	public TValue GetOrAddCache(TKey key, Func<TKey, TValue> func)
	{
		return Cache.GetOrAdd(key, func);
	}

	/// <summary>
	/// Tries to add a record to <see cref="LimitedCache"/>.
	/// </summary>
	/// <param name="key">Key of record to add to <see cref="LimitedCache"/>.</param>
	/// <param name="value">Value of record to add to <see cref="LimitedCache"/>.</param>
	/// <returns><see langword="true"/> if the record was added successfully, otherwise <see langword="false"/>.</returns>
	public bool TryAddLimitedCache(TKey key, TValue value)
	{
		return LimitedCache.TryAdd(key, value);
	}

	/// <summary>
	/// Tries to add a record to <see cref="Cache"/>
	/// </summary>
	/// <param name="key">Key of record to add to <see cref="Cache"/>.</param>
	/// <param name="value">Value of record to add to <see cref="Cache"/>.</param>
	/// <returns><see langword="true"/> if the record was added successfully, otherwise <see langword="false"/>.</returns>
	public bool TryAddCache(TKey key, TValue value)
	{
		return Cache.TryAdd(key, value);
	}
}

/// <summary>
/// Class to manage thread-safe dictionary caches. Offers both a fixed size LRU (Least Recently Used) dictionary called "<see cref="LimitedCache"/>" and a unlimited sized cache called "<see cref="Cache"/>".
/// </summary>
/// <param name="limitedCacheSize">Optional: How large to make the limited cache. Default value is 100.</param>
/// <param name="useLimitedCache">Optional: Whether to use limited sized cache or not when first initialized. Default is true.</param>
public sealed class CacheManagerFifo<TKey, TValue>(int limitedCacheSize = 100, bool useLimitedCache = true) : ICacheManagerApi<TKey, TValue> where TKey : notnull
{
	private readonly ReaderWriterLockSlim readWriteLock = new();

	private int limitedCacheSize = limitedCacheSize;

	private bool UseLimitedCache { get; set; } = useLimitedCache;

	private ConcurrentDictionary<TKey, TValue> Cache { get; } = new();

	private FixedFifoDictionary<TKey, TValue> LimitedCache { get; set; } = new(limitedCacheSize);

	/// <summary>
	/// Clears both <see cref="Cache"/> and <see cref="LimitedCache"/>.
	/// </summary>
	public void ClearAllCaches()
	{
		ClearCache();
		ClearLimitedCache();
	}

	/// <summary>
	/// Clears <see cref="Cache"/>.
	/// </summary>
	public void ClearCache()
	{
		readWriteLock.EnterWriteLock();
		try
		{
			Cache.Clear();
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Clears <see cref="LimitedCache"/>.
	/// </summary>
	public void ClearLimitedCache()
	{
		readWriteLock.EnterWriteLock();
		try
		{
			LimitedCache.Clear();
			LimitedCache.TrimExcess();
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Clears <see cref="LimitedCache"/> and sets the size to the specified value.
	/// </summary>
	/// <param name="size">Maximum number of entries to allow in <see cref="LimitedCache"/> before removing least recently used entry.</param>
	public void SetLimitedCacheSize(int size)
	{
		if (size < 1)
		{
			SetUseLimitedCache(false);
		}

		readWriteLock.EnterWriteLock();
		try
		{
			limitedCacheSize = size;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}

		if (UseLimitedCache)
		{
			ClearLimitedCache();
			readWriteLock.EnterWriteLock();
			try
			{
				LimitedCache = new(limitedCacheSize);
			}
			finally
			{
				readWriteLock.ExitWriteLock();
			}
		}
	}

	/// <summary>
	/// Clears <see cref="LimitedCache"/> and <see cref="Cache"/> and initializes <see cref="LimitedCache"/> to use the size specified by <see cref="limitedCacheSize"/> or 1 if <see cref="UseLimitedCache"/> is false.
	/// </summary>
	/// <param name="useLimitedCache">When <see langword="true"/>, uses cache with limited number of total records.</param>
	public void SetUseLimitedCache(bool useLimitedCache)
	{
		ClearAllCaches();
		SetLimitedCacheSize(useLimitedCache ? limitedCacheSize : 1);
		readWriteLock.EnterWriteLock();
		try
		{
			UseLimitedCache = useLimitedCache;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Gets the <see cref="limitedCacheSize"/> value.
	/// </summary>
	/// <returns>The value of <see cref="limitedCacheSize"/>.</returns>
	public int GetLimitedCacheSize()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return limitedCacheSize;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Returns whether the <see cref="LimitedCache"/> is being used or not.
	/// </summary>
	/// <returns><see langword="true"/> if <see cref="LimitedCache"/> is being used, otherwise <see langword="false"/></returns>
	public bool IsUsingLimitedCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return UseLimitedCache;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Get a readonly copy of <see cref="Cache"/>.
	/// </summary>
	/// <returns>A readonly copy of <see cref="Cache"/>.</returns>
	public IReadOnlyDictionary<TKey, TValue> GetCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return Cache.AsReadOnly();
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Get a readonly copy of <see cref="LimitedCache"/>.
	/// </summary>
	/// <returns>A readonly copy of <see cref="LimitedCache"/>.</returns>
	public IReadOnlyDictionary<TKey, TValue?> GetLimitedCache()
	{
		readWriteLock.EnterReadLock();
		try
		{
			return LimitedCache.AsReadOnly();
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Gets or adds a record to <see cref="LimitedCache"/> and returns the value.
	/// </summary>
	/// <param name="key">Key of the record to get or add to <see cref="LimitedCache"/>.</param>
	/// <param name="func">Function to generate the value for the new record to get or add to <see cref="LimitedCache"/>.</param>
	/// <returns>The value of the record that was retrieved or added to <see cref="LimitedCache"/>.</returns>
	public TValue GetOrAddLimitedCache(TKey key, Func<TKey, TValue> func)
	{
		return LimitedCache.GetOrAdd(key, func);
	}

	/// <summary>
	/// Gets or adds a record to <see cref="Cache"/> and returns the value.
	/// </summary>
	/// <param name="key">Key of the record to get or add to <see cref="Cache"/>.</param>
	/// <param name="func">Function to generate the value for the new record to get or add to <see cref="Cache"/>.</param>
	/// <returns>The value of the record that was retrieved or added to <see cref="Cache"/>.</returns>
	public TValue GetOrAddCache(TKey key, Func<TKey, TValue> func)
	{
		return Cache.GetOrAdd(key, func);
	}

	/// <summary>
	/// Tries to add a record to <see cref="LimitedCache"/>.
	/// </summary>
	/// <param name="key">Key of the record to add to <see cref="LimitedCache"/>.</param>
	/// <param name="value">Value of the record to add to <see cref="LimitedCache"/>.</param>
	/// <returns><see langword="true"/> if the record was added successfully, otherwise <see langword="false"/>.</returns>
	public bool TryAddLimitedCache(TKey key, TValue value)
	{
		return LimitedCache.TryAdd(key, value);
	}

	/// <summary>
	/// Tries to add a record to <see cref="Cache"/>.
	/// </summary>
	/// <param name="key">Key of the record to add to <see cref="Cache"/>.</param>
	/// <param name="value">Value of the record to add to <see cref="Cache"/>.</param>
	/// <returns><see langword="true"/> if the record was added successfully, otherwise <see langword="false"/>.</returns>
	public bool TryAddCache(TKey key, TValue value)
	{
		return Cache.TryAdd(key, value);
	}
}
