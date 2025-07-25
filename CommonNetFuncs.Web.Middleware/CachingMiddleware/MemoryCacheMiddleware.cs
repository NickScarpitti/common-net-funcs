using System.Security.Cryptography;
using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Core.UnitConversion;

namespace CommonNetFuncs.Web.Middleware.CachingMiddleware;

internal class MemoryCacheMiddleware(RequestDelegate next, IMemoryCache cache, CacheOptions cacheOptions, CacheMetrics? cacheMetrics, CacheTracker cacheTracker) : IDisposable
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly RequestDelegate next = next;
    private readonly IMemoryCache cache = cache;
    private readonly CacheOptions cacheOptions = cacheOptions;
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private readonly Lock tagLock = new();
    private readonly CacheMetrics? cacheMetrics = cacheMetrics;
    private readonly CacheTracker cacheTracker = cacheTracker;

    public async Task InvokeAsync(HttpContext context)
    {
        bool evictCachedValue = context.Request.Query.TryGetValue(cacheOptions.EvictionQueryParam, out StringValues evictValue) && bool.TryParse(evictValue, out bool shouldEvict) && shouldEvict;
        bool useCache = context.Request.Query.TryGetValue(cacheOptions.UseCacheQueryParam, out StringValues useCacheValue) && bool.TryParse(useCacheValue, out bool shouldUseCache) && shouldUseCache;

        string? cacheKey = null;
        if (evictCachedValue || useCache)
        {
            // Generate cache key based on request
            cacheKey = await GenerateCacheKey(context).ConfigureAwait(false);

            if (evictCachedValue)
            {
                // Evict the cache entry
                await EvictCacheAsync(context, cacheKey).ConfigureAwait(false);
            }
        }

        if (useCache && cacheKey != null)
        {
            if (cache.TryGetValue(cacheKey, out CacheEntry? cachedValue))
            {
                cacheMetrics?.IncrementHits();
                if (cachedValue?.Data.Length > 0)
                {
                    foreach (KeyValuePair<string, string> header in cachedValue.Headers)
                    {
                        context.Response.Headers.TryAdd(header.Key, header.Value);
                    }

                    if (cachedValue.CompressionType > 0)
                    {
                        byte[] decompressedData = await cachedValue.Data.Decompress((ECompressionType)cachedValue.CompressionType).ConfigureAwait(false);
                        await context.Response.Body.WriteAsync(decompressedData.AsMemory(0, decompressedData.Length)).ConfigureAwait(false);
                    }
                    else
                    {
                        await context.Response.Body.WriteAsync(cachedValue.Data.AsMemory(0, cachedValue.Data.Length)).ConfigureAwait(false);
                    }
                }
                else
                {
                    await context.Response.Body.WriteAsync(null).ConfigureAwait(false);
                }
                return;
            }

            cacheMetrics?.IncrementMisses();

            TimeSpan customCacheDuration = TimeSpan.Zero;
            if (context.Request.Query.TryGetValue(cacheOptions.CacheSecondsQueryParam, out StringValues cacheSecondsValue) && int.TryParse(cacheSecondsValue, out int seconds))
            {
                customCacheDuration = customCacheDuration.Add(TimeSpan.FromSeconds(seconds));
            }

            if (context.Request.Query.TryGetValue(cacheOptions.CacheMinutesQueryParam, out StringValues cacheMinutesValue) && int.TryParse(cacheMinutesValue, out int minutes))
            {
                customCacheDuration = customCacheDuration.Add(TimeSpan.FromMinutes(minutes));
            }

            if (context.Request.Query.TryGetValue(cacheOptions.CacheHoursQueryParam, out StringValues cacheHoursValue) && int.TryParse(cacheHoursValue, out int hours))
            {
                customCacheDuration = customCacheDuration.Add(TimeSpan.FromHours(hours));
            }

            // Capture the original response
            Stream originalBody = context.Response.Body;
            await using MemoryStream memoryStream = new();
            context.Response.Body = memoryStream;

            try
            {
                await next(context).ConfigureAwait(false);

                // Store the response value in cache if it was successful
                if (context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    byte[] responseData = memoryStream.ToArray();

                    // Check if adding this would exceed cache size limit
                    bool spaceAvailable = await EnsureCacheSpaceAvailableAsync(responseData.Length).ConfigureAwait(false);
                    if (spaceAvailable)
                    {
                        await cacheLock.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            CacheEntry entry = new()
                            {
                                Data = cacheOptions.UseCompression ? await responseData.Compress(cacheOptions.CompressionType).ConfigureAwait(false) : responseData,
                                Tags = ExtractCacheTags(context),
                                Headers = context.Response.Headers.Where(x => cacheOptions.HeadersToCache.ContainsInvariant(x.Key)).ToDictionary(h => h.Key, h => h.Value.ToString()), //Only cache needed headers
                                CompressionType = cacheOptions.UseCompression ? (short)cacheOptions.CompressionType : (short)0
                            };

                            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                                .SetSize(responseData.Length)
                                .SetAbsoluteExpiration(customCacheDuration == TimeSpan.Zero ? cacheOptions.DefaultCacheDuration : customCacheDuration)
                                .RegisterPostEvictionCallback(HandleEviction);

                            cache.Set(cacheKey, entry, cacheEntryOptions);
                            cacheMetrics?.AddToSize(responseData.Length);

                            lock (tagLock)
                            {
                                // Register tags
                                foreach (string tag in entry.Tags)
                                {
                                    cacheTracker.CacheTags.AddOrUpdate(tag, [cacheKey], (_, keys) =>
                                    {
                                        keys.Add(cacheKey);
                                        return keys;
                                    });

                                    cacheMetrics?.CacheTags.AddOrUpdate(tag, [cacheKey], (_, keys) =>
                                    {
                                        keys.Add(cacheKey);
                                        return keys;
                                    });
                                }
                            }
                        }
                        finally
                        {
                            cacheLock.Release();
                        }
                    }

                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBody).ConfigureAwait(false);
                }
                else
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBody).ConfigureAwait(false);
                }
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
        else
        {
            // Continue with the pipeline
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                if (!cacheOptions.SuppressLogs)
                {
                    logger.Warn(ex, "Task was canceled in memory cache middleware");
                }
            }
        }
    }

    private async Task EvictCacheAsync(HttpContext context, string cacheKey)
    {
        // Check for tag-based eviction
        string[]? tagsToEvict = context.Request.Query[cacheOptions.EvictTagQueryParam].ToString()
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tagsToEvict?.Length > 0)
        {
            await cacheLock.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (tagLock)
                {
                    foreach (string tag in tagsToEvict)
                    {
                        if (cacheTracker != null && cacheTracker.CacheTags.TryGetValue(tag, out HashSet<string>? keysToEvict))
                        {
                            foreach (string keyToEvict in keysToEvict)
                            {
                                if (cache.TryGetValue(keyToEvict, out CacheEntry? entry))
                                {
                                    cacheMetrics?.SubtractFromSize(entry?.Data.Length ?? 0);
                                    cache.Remove(keyToEvict);
                                    if (!cacheOptions.SuppressLogs)
                                    {
                                        logger.Info("Manually evicting {keyToEvict} because it had tag {tag}", [keyToEvict, tag.UrlEncodeReadable()]);
                                    }
                                }
                            }
                            cacheTracker.CacheTags.TryRemove(tag, out _);
                            cacheMetrics?.CacheTags.TryRemove(tag, out _);
                        }
                    }
                }
            }
            finally
            {
                cacheLock.Release();
            }
        }
        else
        {
            // Single entry eviction
            if (cache.TryGetValue(cacheKey, out CacheEntry? entry))
            {
                await cacheLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    cacheMetrics?.SubtractFromSize(entry?.Data.Length ?? 0);
                    cache.Remove(cacheKey);
                    if (!cacheOptions.SuppressLogs)
                    {
                        logger.Info("Manually evicting {cacheKey}", cacheKey);
                    }
                    RemoveCacheTags(cacheKey, entry?.Tags ?? []);
                }
                finally
                {
                    cacheLock.Release();
                }
            }
        }
    }

    private void HandleEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is not CacheEntry entry)
        {
            return;
        }

        cacheMetrics?.SubtractFromSize(entry.Data.Length);
        RemoveCacheTags(key.ToString() ?? string.Empty, entry.Tags);
        if (!cacheOptions.SuppressLogs)
        {
            logger.Info("Automatically evicting {key} for reason: {reason}", [key, reason]);
        }
    }

    private void RemoveCacheTags(string key, HashSet<string> tags)
    {
        lock (tagLock)
        {
            foreach (string tag in tags)
            {
                if (cacheTracker?.CacheTags.TryGetValue(tag, out HashSet<string>? keys) == true)
                {
                    keys.Remove(key);
                    if (keys.Count == 0)
                    {
                        cacheTracker.CacheTags.TryRemove(tag, out _);
                        cacheMetrics?.CacheTags.TryRemove(tag, out _);
                    }
                }
            }
        }
    }

    private async ValueTask<bool> EnsureCacheSpaceAvailableAsync(long requiredSize)
    {
        // If the item itself is larger than max cache size, it can never be cached
        if (requiredSize > cacheOptions.MaxCacheSizeInBytes)
        {
            cacheMetrics?.IncrementSkippedDueToSize();
            return false;
        }

        // If we have enough space, return immediately
        if (cacheMetrics?.CurrentCacheSize + requiredSize <= cacheOptions.MaxCacheSizeInBytes)
        {
            return true;
        }

        await cacheLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double check after acquiring lock
            if (cacheMetrics?.CurrentCacheSize + requiredSize <= cacheOptions.MaxCacheSizeInBytes)
            {
                return true;
            }

            // Get all tracked entries sorted by creation time
            IOrderedEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = cacheTracker.GetEntries().OrderBy(x => x.Value.TimeCreated);

            // Calculate how much space we need to free
            long spaceToFree = cacheMetrics?.CurrentCacheSize + requiredSize - cacheOptions.MaxCacheSizeInBytes ?? 0;
            long freedSpace = 0;

            // Remove entries until we have enough space
            foreach (KeyValuePair<string, CacheTracker.CacheEntryMetadata> entry in entries)
            {
                if (cache.TryGetValue(entry.Key, out CacheEntry? cacheEntry))
                {
                    // Remove the entry
                    cache.Remove(entry.Key);
                    cacheTracker?.RemoveEntry(entry.Key);
                    if (!cacheOptions.SuppressLogs)
                    {
                        logger.Info("Automatically evicting {key} because due to there not being enough space in cache for new value of size {size}", [entry.Key, entry.Value.Size.BytesToKb()]);
                    }

                    freedSpace += entry.Value.Size;

                    // Update metrics
                    cacheMetrics?.SubtractFromSize(entry.Value.Size);
                    cacheMetrics?.IncrementEvictionsForSpace();

                    // Clean up tags
                    RemoveCacheTags(entry.Key, cacheEntry?.Tags ?? []);
                }

                if (freedSpace >= spaceToFree)
                {
                    return true;
                }
            }

            // If we get here, we couldn't free enough space
            return false;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private HashSet<string> ExtractCacheTags(HttpContext context)
    {
        // Extract from header
        if (!context.Request.Headers.TryGetValue(cacheOptions.CacheTagHeader, out StringValues headerTags))
        {
            return [];
        }

        return new HashSet<string>(headerTags.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private async Task<string> GenerateCacheKey(HttpContext context)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append(context.Request.Path.Value);

        // Use StringValues directly instead of converting to string
        IOrderedEnumerable<KeyValuePair<string, StringValues>> queryValues = context.Request.Query.Where(x => !x.Key.StrComp(cacheOptions.EvictionQueryParam)).OrderBy(x => x.Key);

        if (queryValues.Any())
        {
            stringBuilder.Append('?');
            stringBuilder.AppendJoin('&', queryValues.Select(q => $"{q.Key}={q.Value}"));
        }

        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            using StreamReader reader = new(context.Request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync().ConfigureAwait(false);
            context.Request.Body.Position = 0;

            stringBuilder.Append('|').Append(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body))));
        }

        stringBuilder.Append('|').Append(context.Request.Headers.Accept);
        return stringBuilder.ToString();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && cacheLock != null)
        {
            cacheLock.Dispose();
        }
    }

    ~MemoryCacheMiddleware()
    {
        Dispose(false);
    }
}

// Extension method for easy middleware registration
public static class MemoryCacheEvictionMiddlewareExtensions
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public static IApplicationBuilder UseMemoryValueCaching(this IApplicationBuilder app, CacheOptions? options = null, bool trackMetrics = false)
    {
        options ??= new();
        CacheMetrics? metrics = trackMetrics ? app.ApplicationServices.GetRequiredService<CacheMetrics>() : null;
        CacheTracker tracker = app.ApplicationServices.GetRequiredService<CacheTracker>();
        return app.UseMiddleware<MemoryCacheMiddleware>(options, metrics, tracker);
    }

    public static IServiceCollection MemoryValueCaching(this IServiceCollection services)
    {
        // Register IMemoryCache if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IMemoryCache)))
        {
            services.AddMemoryCache();
        }

        // Register CacheTracker as singleton
        services.AddSingleton<CacheTracker>();

        // Register CacheMetrics as singleton if using metrics
        services.AddSingleton<CacheMetrics>();

        return services;
    }

    public static IEndpointRouteBuilder MapCacheMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/memory-cache-metrics", (CacheMetrics metrics) =>
        {
            try
            {
                return Results.Ok(
                new
                {
                    Hits = metrics.CacheHits,
                    Misses = metrics.CacheMisses,
                    HitRatio = metrics.CacheHits + metrics.CacheMisses == 0
                        ? 0
                        : (double)metrics.CacheHits / (metrics.CacheHits + metrics.CacheMisses),
                    CurrentSizeBytes = metrics.CurrentCacheSize,
                    CurrentSizeMB = Math.Round((double)metrics.CurrentCacheSize / (1024 * 1024), 2),
                    TagCount = metrics.CacheTags.Count,
                    Tags = metrics.CacheTags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error retrieving cache metrics");
            }
        })
        .WithName("GetMemoryCacheMetrics");

        return endpoints;
    }

    public static IEndpointRouteBuilder MapEvictionEndpoints(this IEndpointRouteBuilder endpoints, string? authorizationPolicyName = null)
    {
        // New endpoint for evicting by key
        RouteHandlerBuilder evictByKeyEndpoint = endpoints.MapPost("/api/memorycache/evict/key/{key}", ([FromServices] IMemoryCache cache, [FromServices] CacheTracker tracker, [FromServices] CacheMetrics? metrics, string key) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return Results.BadRequest("Cache key is required");
                }

                if (cache.TryGetValue(key, out CacheEntry? entry))
                {
                    // Remove from cache
                    cache.Remove(key);

                    // Update metrics
                    metrics?.SubtractFromSize(entry?.Data.Length ?? 0);

                    // Remove from tags
                    foreach (string tag in entry?.Tags ?? [])
                    {
                        if (tracker.CacheTags.TryGetValue(tag, out HashSet<string>? keys))
                        {
                            keys.Remove(key);
                            if (keys.Count == 0)
                            {
                                tracker.CacheTags.TryRemove(tag, out _);
                                metrics?.CacheTags.TryRemove(tag, out _);
                            }
                        }
                    }

                    logger.Info("Cache entry evicted for key: {key}", key);
                    return Results.Ok(1);
                }
                logger.Info("No cache entry found for key: {key}", key);
                return Results.Ok(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error evicting cache entry by key");
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error evicting cache entry");
            }
        })
        .WithName("EvictCacheByKey")
        .WithDisplayName("Evict Cache Entry by Key");

        // New endpoint for evicting by tag
        RouteHandlerBuilder evictByTagEndpoint = endpoints.MapPost("/api/memorycache/evict/tag/{tag}", ([FromServices] IMemoryCache cache, [FromServices] CacheTracker tracker, [FromServices] CacheMetrics? metrics, string tag, CancellationToken cancellationToken = default) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return Results.BadRequest("Cache tag is required");
                }

                if (tracker.CacheTags.TryGetValue(tag, out HashSet<string>? keysToEvict))
                {
                    int evictedCount = 0;

                    foreach (string keyToEvict in keysToEvict.ToArray()) // Create a copy to avoid modification during enumeration
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (cache.TryGetValue(keyToEvict, out CacheEntry? entry))
                        {
                            // Remove from cache
                            cache.Remove(keyToEvict);

                            // Update metrics
                            metrics?.SubtractFromSize(entry?.Data.Length ?? 0);

                            // Remove from all associated tags
                            foreach (string entryTag in entry?.Tags ?? [])
                            {
                                if (tracker.CacheTags.TryGetValue(entryTag, out HashSet<string>? tagKeys))
                                {
                                    tagKeys.Remove(keyToEvict);
                                    if (tagKeys.Count == 0)
                                    {
                                        tracker.CacheTags.TryRemove(entryTag, out _);
                                        metrics?.CacheTags.TryRemove(entryTag, out _);
                                    }
                                }
                            }
                            evictedCount++;
                        }
                    }

                    logger.Info("Evicted {count} cache entries with tag: {tag}", evictedCount, tag);
                    return Results.Ok(evictedCount);
                }

                logger.Info("No cache entries found with tag: {tag}", tag);
                return Results.Ok(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error evicting cache entries by tag");
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error evicting cache entries");
            }
        })
        .WithName("EvictCacheByTag")
        .WithDisplayName("Evict Cache Entries by Tag");

        RouteHandlerBuilder evictAllCacheEndpoint = endpoints.MapPost("/api/memorycache/evict/all", ([FromServices] IMemoryCache cache, [FromServices] CacheTracker tracker, [FromServices] CacheMetrics? metrics) =>
        {
            try
            {
                int cacheSize = 0;
                if (cache is MemoryCache concreteMemoryCache)
                {
                    cacheSize = concreteMemoryCache.Count;
                    concreteMemoryCache.Clear();
                    tracker = new();
                    metrics = new();
                    return Results.Ok(cacheSize);
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error evicting cache entries");
            }
            return Results.Problem(detail: "Unable to evict all cache items from concrete MemoryCache object", statusCode: StatusCodes.Status500InternalServerError, title: "Error evicting cache entries");
        })
        .WithName("EvictAllCache")
        .WithDisplayName("Evict All Cache Entries");

        if (!authorizationPolicyName.IsNullOrWhiteSpace())
        {
            evictByKeyEndpoint.RequireAuthorization(authorizationPolicyName);
            evictByTagEndpoint.RequireAuthorization(authorizationPolicyName);
            evictAllCacheEndpoint.RequireAuthorization(authorizationPolicyName);
        }

        return endpoints;
    }
}
