using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace CommonNetFuncs.Web.Common.CachingSupportClasses;

public sealed class CacheMetrics(ConcurrentDictionary<string, HashSet<string>>? cacheTags = null)
{
  private readonly Lock cacheHitsLock = new();
  private readonly Lock cacheMissesLock = new();
  private readonly Lock currentCacheSizeLock = new();
  private readonly Lock evictedDueToCapacityLock = new();
  private readonly Lock evictedDueToRemovedLock = new();
  private readonly Lock skippedDueToSizeLock = new();
  private readonly Lock currentCacheEntryCountLock = new();

  private long cacheHits;
  private long cacheMisses;
  private long currentCacheSize;
  //private long evictedDueToExpired;
  private long evictedDueToCapacity;
  //private long evictedDueToNone;
  private long evictedDueToRemoved;
  //private long evictedDueToReplaced;
  //private long evictedDueToTokenExpired;
  private long skippedDueToSize;
  private long currentCacheEntryCount;

  public ConcurrentDictionary<string, HashSet<string>> CacheTags { get; } = cacheTags ??= new(); //Key = tag, Value = All cache keys that have that tag

  //public long EvictedDueToExpiration => Interlocked.Read(ref evictedDueToExpired);

  public long EvictedDueToCapacity()
  {
    lock (evictedDueToCapacityLock)
    {
      return evictedDueToCapacity;
    }
  }

  //public long EvictedDueToNone => Interlocked.Read(ref evictedDueToNone);

  public long EvictedDueToRemoved()
  {
    lock (evictedDueToRemovedLock)
    {
      return evictedDueToRemoved;
    }
  }

  //public long EvictedDueToReplaced => Interlocked.Read(ref evictedDueToReplaced);

  //public long EvictedDueToExpired => Interlocked.Read(ref evictedDueToTokenExpired);

  public long SkippedDueToSize()
  {
    lock (skippedDueToSizeLock)
    {
      return skippedDueToSize;
    }
  }

  public void IncrementEviction(EvictionReason reason)
  {
    switch (reason)
    {
      case EvictionReason.Removed:
        lock (evictedDueToRemovedLock)
        {
          evictedDueToRemoved++;
          //Interlocked.Increment(ref evictedDueToRemoved);
        }
        break;
      case EvictionReason.Capacity:
        lock (evictedDueToCapacityLock)
        {
          evictedDueToCapacity++;
          //Interlocked.Increment(ref evictedDueToCapacity);
        }
        break;
      //case EvictionReason.None:
      //    //Interlocked.Increment(ref evictedDueToNone);
      //    break;
      //case EvictionReason.Replaced:
      //    //Interlocked.Increment(ref evictedDueToReplaced);
      //    break;
      //case EvictionReason.Expired:
      //    //Interlocked.Increment(ref evictedDueToExpired);
      //    break;
      //case EvictionReason.TokenExpired:
      //    //Interlocked.Increment(ref evictedDueToTokenExpired);
      //    break;
    }
  }

  public void IncrementSkippedDueToSize()
  {
    lock (skippedDueToSizeLock)
    {
      skippedDueToSize++;
    }
  }

  public long CacheHits()
  {
    lock (cacheHitsLock)
    {
      return cacheHits;
    }
  }

  public long CacheMisses()
  {
    lock (cacheMissesLock)
    {
      return cacheMisses;
    }
  }

  public long CurrentCacheSize()
  {
    lock (currentCacheSizeLock)
    {
      return currentCacheSize;
    }
  }

  public long CurrentCacheEntryCount()
  {
    lock (currentCacheEntryCountLock)
    {
      return currentCacheEntryCount;
    }
  }

  public void IncrementHits()
  {
    lock (cacheHitsLock)
    {
      cacheHits++;
    }
  }

  public void IncrementMisses()
  {
    lock (cacheMissesLock)
    {
      cacheMisses++;
    }
  }

  public void IncrementCacheEntryCount()
  {
    lock (currentCacheEntryCountLock)
    {
      currentCacheEntryCount++;
    }
  }

  public void DecrementCacheEntryCount()
  {
    lock (currentCacheEntryCountLock)
    {
      if (currentCacheEntryCount > 0)
      {
        currentCacheEntryCount--;
      }
    }
  }

  public void AddToSize(long bytes)
  {
    lock (currentCacheSizeLock)
    {
      currentCacheSize += bytes < 0 ? bytes * -1 : bytes;
    }
  }

  public void SubtractFromSize(long bytes)
  {
    lock (currentCacheSizeLock)
    {
      if (currentCacheSize > 0)
      {
        currentCacheSize += bytes > 0 ? bytes * -1 : bytes;
      }
    }
  }

  /// <summary>
  /// Resets all values in the cache
  /// </summary>

  public void Clear()
  {
    CacheTags.Clear();

    lock (cacheHitsLock)
    {
      lock (cacheMissesLock)
      {
        lock (currentCacheSizeLock)
        {
          lock (evictedDueToCapacityLock)
          {
            lock (evictedDueToRemovedLock)
            {
              lock (skippedDueToSizeLock)
              {
                lock (currentCacheEntryCountLock)
                {
                  cacheHits = 0;
                  cacheMisses = 0;
                  currentCacheSize = 0;
                  evictedDueToCapacity = 0;
                  evictedDueToRemoved = 0;
                  skippedDueToSize = 0;
                  currentCacheEntryCount = 0;
                }
              }
            }
          }
        }
      }
    }
  }
}
