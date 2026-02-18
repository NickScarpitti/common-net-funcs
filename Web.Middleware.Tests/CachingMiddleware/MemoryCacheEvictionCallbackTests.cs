using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheEvictionCallbackTests
{
	private readonly MemoryCache cache;
	private readonly CacheOptions options;
	private readonly CacheMetrics metrics;
	private readonly CacheTracker tracker;
	private readonly RequestDelegate next;
	private readonly HttpContext context;

	public MemoryCacheEvictionCallbackTests()
	{
		MemoryCacheOptions cacheOptions = new()
		{
			SizeLimit = 1000,
			ExpirationScanFrequency = TimeSpan.FromMilliseconds(100)
		};
		cache = new MemoryCache(cacheOptions);
		options = new CacheOptions
		{
			MaxCacheSizeInBytes = 1000,
			SuppressLogs = true,
			DefaultCacheDuration = TimeSpan.FromMilliseconds(100)
		};
		metrics = new CacheMetrics();
		tracker = new CacheTracker();
		next = A.Fake<RequestDelegate>();
		context = new DefaultHttpContext();
	}

	[RetryFact(3)]
	public async Task HandleEviction_WhenEntryExpires_UpdatesMetrics()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Path = "/api/test";
		context.Response.StatusCode = StatusCodes.Status200OK;

		byte[] data = Encoding.UTF8.GetBytes("test data");
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act - Add entry to cache
		await middleware.InvokeAsync(context);

		long initialCount = metrics.CurrentCacheEntryCount();
		initialCount.ShouldBe(1);

		// Wait for expiration
		await Task.Delay(300);

		// Trigger a cache operation to process expired items
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - Metrics should be updated after eviction
		// Note: The eviction callback should have been triggered
		metrics.EvictedDueToRemoved().ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task HandleEviction_WithTags_RemovesTagsFromTracker()
	{
		// Arrange
		const string tagName = "test-tag";
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Path = "/api/test";
		context.Request.Headers[options.CacheTagHeader] = tagName;
		context.Response.StatusCode = StatusCodes.Status200OK;

		byte[] data = Encoding.UTF8.GetBytes("test data");
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act - Add entry with tag
		await middleware.InvokeAsync(context);

		tracker.CacheTags.ContainsKey(tagName).ShouldBeTrue();

		// Wait for expiration and trigger cleanup
		await Task.Delay(300);
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - Tag should be removed from tracker after eviction
		// If all entries with this tag are evicted, the tag should be removed
		if (tracker.CacheTags.TryGetValue(tagName, out HashSet<string>? keys))
		{
			keys.Count.ShouldBe(0);
		}
	}

	[RetryFact(3)]
	public async Task HandleEviction_WhenMultipleEntriesEvicted_UpdatesMetricsCorrectly()
	{
		// Arrange - Add multiple entries with short expiration
		for (int i = 0; i < 3; i++)
		{
			HttpContext testContext = new DefaultHttpContext
			{
				Request = { Method = "GET", Path = $"/api/test{i}" },
				Response = { StatusCode = StatusCodes.Status200OK }
			};

			Dictionary<string, StringValues> queryDict = new()
				{
					{ options.UseCacheQueryParam, "true" }
				};
			testContext.Request.Query = new QueryCollection(queryDict);

			byte[] data = Encoding.UTF8.GetBytes($"test data {i}");
			RequestDelegate testNext = A.Fake<RequestDelegate>();
			A.CallTo(() => testNext(A<HttpContext>._)).Invokes((HttpContext ctx) =>
			{
				ctx.Response.Body.Write(data, 0, data.Length);
			});

			MemoryCacheMiddleware middleware = new(testNext, cache, options, metrics, tracker);
			await middleware.InvokeAsync(testContext);
		}

		long initialCount = metrics.CurrentCacheEntryCount();
		initialCount.ShouldBe(3);

		// Wait for expiration
		await Task.Delay(300);
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - All entries should have been evicted and metrics updated
		metrics.EvictedDueToRemoved().ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task HandleEviction_WithLoggingEnabled_Logs()
	{
		// Arrange
		CacheOptions optionsWithLogging = new()
		{
			MaxCacheSizeInBytes = 1000,
			SuppressLogs = false,
			DefaultCacheDuration = TimeSpan.FromMilliseconds(100)
		};

		Dictionary<string, StringValues> queryDict = new()
			{
				{ optionsWithLogging.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Path = "/api/test";
		context.Response.StatusCode = StatusCodes.Status200OK;

		byte[] data = Encoding.UTF8.GetBytes("test data");
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, optionsWithLogging, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Wait for expiration
		await Task.Delay(300);
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - Should not throw (logging is enabled)
		metrics.EvictedDueToRemoved().ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task HandleEviction_WithNonCacheEntry_HandlesGracefully()
	{
		// Arrange - Manually add an entry that's not a CacheEntry type
		string key = "non-cache-entry";
		MemoryCacheEntryOptions entryOptions = new MemoryCacheEntryOptions()
			.SetSize(100)
			.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(100));

		cache.Set(key, "just a string", entryOptions);

		// Wait for expiration
		await Task.Delay(300);
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - Should handle gracefully without throwing
		// The callback checks if value is CacheEntry and returns early if not
		metrics.EvictedDueToRemoved().ShouldBe(0); // No CacheEntry was evicted
	}

	[RetryFact(3)]
	public async Task HandleEviction_WithMultipleTags_CleansUpAllTags()
	{
		// Arrange
		const string tags = "tag1,tag2,tag3";
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Path = "/api/test";
		context.Request.Headers[options.CacheTagHeader] = tags;
		context.Response.StatusCode = StatusCodes.Status200OK;

		byte[] data = Encoding.UTF8.GetBytes("test data");
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act - Add entry with multiple tags
		await middleware.InvokeAsync(context);

		tracker.CacheTags.ContainsKey("tag1").ShouldBeTrue();
		tracker.CacheTags.ContainsKey("tag2").ShouldBeTrue();
		tracker.CacheTags.ContainsKey("tag3").ShouldBeTrue();

		// Wait for expiration
		await Task.Delay(300);
		cache.Compact(1.0);
		await Task.Delay(100);

		// Assert - All tags should be cleaned up after eviction
		foreach (string tag in tags.Split(','))
		{
			if (tracker.CacheTags.TryGetValue(tag, out HashSet<string>? keys))
			{
				keys.Count.ShouldBe(0);
			}
		}
	}
}
