using System.Collections.Concurrent;

namespace CommonNetFuncs.Web.Middleware.CachingMiddleware;

internal class CacheTracker
{
    public ConcurrentDictionary<string, HashSet<string>> cacheTags { get; set; } = new(); //Key = tag, Value = All cache keys that have that tag

    private readonly ConcurrentDictionary<string, CacheEntryMetadata> entries = new();

    public record CacheEntryMetadata(long Size, DateTimeOffset TimeCreated);

    public void TrackEntry(string key, long size)
    {
        entries[key] = new CacheEntryMetadata(size, DateTimeOffset.UtcNow);
    }

    public void RemoveEntry(string key)
    {
        entries.TryRemove(key, out _);
    }

    public IEnumerable<KeyValuePair<string, CacheEntryMetadata>> GetEntries()
    {
        return entries.ToArray(); // Create a snapshot to avoid modification during enumeration
    }

    public void Clear()
    {
        cacheTags.Clear();
        entries.Clear();
    }
}
