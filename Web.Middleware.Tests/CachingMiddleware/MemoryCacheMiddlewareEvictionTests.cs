using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheMiddlewareEvictionTests
{
	private readonly IMemoryCache cache;
	private readonly CacheOptions options;
	private readonly CacheMetrics metrics;
	private readonly CacheTracker tracker;
	private readonly HttpContext context;

	public MemoryCacheMiddlewareEvictionTests()
	{
		cache = A.Fake<IMemoryCache>();
		options = new CacheOptions();
		metrics = new CacheMetrics();
		tracker = new CacheTracker();
		context = new DefaultHttpContext();
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithTags_RemovesAllTaggedEntries()
	{
		// Arrange
		const string tag = "test-tag";
		string[] keys = new[] { "key1", "key2" };
		CacheEntry entry = new()
		{
			Data = new byte[] { 1, 2, 3 },
			Tags = new HashSet<string> { tag }
		};

		tracker.CacheTags.TryAdd(tag, new HashSet<string>(keys));

		// Setup the mock to handle the non-generic TryGetValue for all keys
		object? outValue = entry; // Use a local variable to hold the out parameter value
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.EvictionQueryParam, "true" },
				{ options.EvictTagQueryParam, tag }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.Remove(A<string>._)).MustHaveHappened(keys.Length, Times.Exactly);
		tracker.CacheTags.ContainsKey(tag).ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithMultipleTags_RemovesAllTaggedEntries()
	{
		// Arrange
		const string tags = "tag1,tag2";
		CacheEntry entry = new()
		{
			Data = new byte[] { 1, 2, 3 },
			Tags = new HashSet<string> { "tag1" }
		};

		tracker.CacheTags.TryAdd("tag1", new HashSet<string> { "key1" });
		tracker.CacheTags.TryAdd("tag2", new HashSet<string> { "key2" });

		object? outValue = entry;
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.EvictionQueryParam, "true" },
				{ options.EvictTagQueryParam, tags }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.Remove(A<string>._)).MustHaveHappened();
		tracker.CacheTags.ContainsKey("tag1").ShouldBeFalse();
		tracker.CacheTags.ContainsKey("tag2").ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithSingleKey_RemovesEntry()
	{
		// Arrange
		CacheEntry entry = new()
		{
			Data = new byte[] { 1, 2, 3 },
			Tags = new HashSet<string> { "tag1" }
		};

		tracker.CacheTags.TryAdd("tag1", new HashSet<string> { "TestKey" });

		object? outValue = entry;
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" },
				{ options.EvictionQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.Remove(A<string>._)).MustHaveHappened();
		metrics.EvictedDueToRemoved().ShouldBe(1);
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithSingleKey_WithSuppressedLogs_DoesNotLog()
	{
		// Arrange
		CacheOptions optionsWithSuppressedLogs = new() { SuppressLogs = true };
		CacheEntry entry = new()
		{
			Data = new byte[] { 1, 2, 3 },
			Tags = new HashSet<string>()
		};

		object? outValue = entry;
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		Dictionary<string, StringValues> queryDict = new()
			{
				{ optionsWithSuppressedLogs.UseCacheQueryParam, "true" },
				{ optionsWithSuppressedLogs.EvictionQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: optionsWithSuppressedLogs, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.Remove(A<string>._)).MustHaveHappened();
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithNonExistentKey_DoesNotThrow()
	{
		// Arrange
		object? outValue = null;
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(false);

		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" },
				{ options.EvictionQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert - should not throw and should not attempt to remove non-existent key
		A.CallTo(() => cache.Remove(A<string>._)).MustNotHaveHappened();
	}

	[RetryFact(3)]
	public async Task EvictCacheAsync_WithTagNotInTracker_DoesNotThrow()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.EvictionQueryParam, "true" },
				{ options.EvictTagQueryParam, "nonexistent-tag" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert - should not throw and tag should still not be in tracker
		tracker.CacheTags.ContainsKey("nonexistent-tag").ShouldBeFalse();
		A.CallTo(() => cache.Remove(A<string>._)).MustNotHaveHappened();
	}
}
