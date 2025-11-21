using System.Collections.Immutable;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Common.CachingSupportClasses.CacheOptionDefaults;

namespace CommonNetFuncs.Web.Common.CachingSupportClasses;

public sealed class CacheOptions
{
	public string EvictionQueryParam { get; set; } = DefaultEvictionQueryParam;

	public string UseCacheQueryParam { get; set; } = DefaultCacheQueryParam;

	public string CacheSecondsQueryParam { get; set; } = DefaultCacheSecondsQueryParam;

	public string CacheMinutesQueryParam { get; set; } = DefaultCacheMinutesQueryParam;

	public string CacheHoursQueryParam { get; set; } = DefaultCacheHoursQueryParam;

	public string CacheTagHeader { get; set; } = DefaultCacheTagHeader;

	public string EvictTagQueryParam { get; set; } = DefaultEvictTagQueryParam;

	public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

	public long MaxCacheSizeInBytes { get; set; } = DefaultCacheSize; // 100MB default

	public ImmutableHashSet<string> HeadersToCache { get; set; } = DefaultHeadersToCache;

	public bool UseCompression { get; set; } = true;

	public ECompressionType CompressionType { get; set; } = ECompressionType.Gzip;

	public bool SuppressLogs { get; set; } = true;
}

public static class CacheOptionDefaults
{
	public const string DefaultEvictionQueryParam = "evictCacheEntry";
	public const string DefaultCacheQueryParam = "useCache";
	public const string DefaultCacheSecondsQueryParam = "cacheSeconds";
	public const string DefaultCacheMinutesQueryParam = "cacheMinutes";
	public const string DefaultCacheHoursQueryParam = "cacheHours";
	public const string DefaultCacheTagHeader = "X-Cache-Tags";
	public const string DefaultEvictTagQueryParam = "evictTags";
	public const long DefaultCacheSize = 100 * 1024 * 1024; //100MB

	public static readonly ImmutableHashSet<string> DefaultHeadersToCache = ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase,
		new[]
		{
			"Content-Type",
			"Content-Language",
			"Content-Encoding",
			"Cache-Control",
			"Vary",
		});
}
