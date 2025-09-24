using System.Collections.Concurrent;

namespace CommonNetFuncs.Web.Middleware.CachingMiddleware;

internal class CacheTracker
{
  public ConcurrentDictionary<string, HashSet<string>> CacheTags { get; set; } = new(); //Key = tag, Value = All cache keys that have that tag

  private readonly ConcurrentDictionary<string, CacheEntryMetadata> Entries = new();

  public record CacheEntryMetadata(long Size, DateTimeOffset TimeCreated);

  public void TrackEntry(string key, long size)
  {
    Entries[key] = new CacheEntryMetadata(size, DateTimeOffset.UtcNow);
  }

  public void RemoveEntry(string key)
  {
    Entries.TryRemove(key, out _);
  }

  public IEnumerable<KeyValuePair<string, CacheEntryMetadata>> GetEntries()
  {
    return Entries.ToArray(); // Create a snapshot to avoid modification during enumeration
  }

  public void Clear()
  {
    CacheTags.Clear();
    Entries.Clear();
  }
}
