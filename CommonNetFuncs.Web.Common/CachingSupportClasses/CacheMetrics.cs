using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace CommonNetFuncs.Web.Common.CachingSupportClasses;

public sealed class CacheMetrics(ConcurrentDictionary<string, HashSet<string>>? cacheTags = null)
{
    private long _cacheHits;
    private long _cacheMisses;
    private long _currentCacheSize;
    private long _evictedDueToExpired;
    private long _evictedDueToCapacity;
    private long _evictedDueToNone;
    private long _evictedDueToRemoved;
    private long _evictedDueToReplaced;
    private long _evictedDueToTokenExpired;
    private long _evictionsForSpace;
    private long _skippedDueToSize;

    public long EvictedDueToExpiration => Interlocked.Read(ref _evictedDueToExpired);
    public long EvictedDueToCapacity => Interlocked.Read(ref _evictedDueToCapacity);
    public long EvictedDueToNone => Interlocked.Read(ref _evictedDueToNone);
    public long EvictedDueToRemoved => Interlocked.Read(ref _evictedDueToRemoved);
    public long EvictedDueToReplaced => Interlocked.Read(ref _evictedDueToReplaced);
    public long EvictedDueToExpired => Interlocked.Read(ref _evictedDueToTokenExpired);

    public long EvictionsForSpace => Interlocked.Read(ref _evictionsForSpace);
    public long SkippedDueToSize => Interlocked.Read(ref _skippedDueToSize);

    public void IncrementEviction(EvictionReason reason)
    {
        switch (reason)
        {
            case EvictionReason.None:
                Interlocked.Increment(ref _evictedDueToNone);
                break;
            case EvictionReason.Removed:
                Interlocked.Increment(ref _evictedDueToRemoved);
                break;
            case EvictionReason.Replaced:
                Interlocked.Increment(ref _evictedDueToReplaced);
                break;
            case EvictionReason.Expired:
                Interlocked.Increment(ref _evictedDueToExpired);
                break;
            case EvictionReason.TokenExpired:
                Interlocked.Increment(ref _evictedDueToTokenExpired);
                break;
            case EvictionReason.Capacity:
                Interlocked.Increment(ref _evictedDueToCapacity);
                break;
        }
    }

    public void IncrementEvictionsForSpace() => Interlocked.Increment(ref _evictionsForSpace);
    public void IncrementSkippedDueToSize() => Interlocked.Increment(ref _skippedDueToSize);

    public ConcurrentDictionary<string, HashSet<string>> CacheTags { get; } = cacheTags ??= new(); //Key = tag, Value = All cache keys that have that tag
    public long CacheHits => Interlocked.Read(ref _cacheHits);
    public long CacheMisses => Interlocked.Read(ref _cacheMisses);
    public long CurrentCacheSize => Interlocked.Read(ref _currentCacheSize);

    public void IncrementHits() => Interlocked.Increment(ref _cacheHits);
    public void IncrementMisses() => Interlocked.Increment(ref _cacheMisses);
    public void AddToSize(long bytes) => Interlocked.Add(ref _currentCacheSize, bytes);
    public void SubtractFromSize(long bytes) => Interlocked.Add(ref _currentCacheSize, bytes);
}
