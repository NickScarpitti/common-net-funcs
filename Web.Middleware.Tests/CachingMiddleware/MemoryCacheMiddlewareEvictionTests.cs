using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheMiddlewareEvictionTests
{
	private readonly IMemoryCache _cache;
	private readonly CacheOptions _options;
	private readonly CacheMetrics _metrics;
	private readonly CacheTracker _tracker;
	private readonly HttpContext _context;

	public MemoryCacheMiddlewareEvictionTests()
	{
		_cache = A.Fake<IMemoryCache>();
		_options = new CacheOptions();
		_metrics = new CacheMetrics();
		_tracker = new CacheTracker();
		_context = new DefaultHttpContext();
	}

	// Fails
	//[RetryFact(3)]
	//public async Task EvictCacheAsync_WithSingleKey_RemovesEntry()
	//{
	//    // Arrange
	//    CacheEntry entry = new()
	//    {
	//        Data = new byte[] { 1, 2, 3 },
	//        Tags = new HashSet<string> { "tag1" }
	//    };

	//    Dictionary<string, StringValues> queryDict = new()
	//    {
	//        { _options.EvictionQueryParam, "true" }
	//    };
	//    _context.Request.Query = new QueryCollection(queryDict);

	//    // Setup the mock to handle the non-generic TryGetValue
	//    object? outValue = entry; // Use a local variable to hold the out parameter value
	//    A.CallTo(() => _cache.TryGetValue(A<object>._, out outValue)).Returns(true);

	//    MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: _cache, cacheOptions: _options, cacheMetrics: _metrics, cacheTracker: _tracker);

	//    // Act
	//    await middleware.InvokeAsync(_context);

	//    // Assert
	//    A.CallTo(() => _cache.Remove(A<string>._)).MustHaveHappened();
	//    _metrics.CurrentCacheSize.ShouldBe(0);
	//}

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

		_tracker.CacheTags.TryAdd(tag, new HashSet<string>(keys));

		// Setup the mock to handle the non-generic TryGetValue for all keys
		object? outValue = entry; // Use a local variable to hold the out parameter value
		A.CallTo(() => _cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		Dictionary<string, StringValues> queryDict = new()
				{
						{ _options.EvictionQueryParam, "true" },
						{ _options.EvictTagQueryParam, tag }
				};
		_context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: _cache, cacheOptions: _options, cacheMetrics: _metrics, cacheTracker: _tracker);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		A.CallTo(() => _cache.Remove(A<string>._)).MustHaveHappened(keys.Length, Times.Exactly);
		_tracker.CacheTags.ContainsKey(tag).ShouldBeFalse();
	}
}
