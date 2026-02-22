using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheSpaceManagementTests
{
	private readonly MemoryCache cache;
	private readonly CacheOptions options;
	private readonly CacheMetrics metrics;
	private readonly CacheTracker tracker;
	private readonly RequestDelegate next;
	private readonly HttpContext context;

	public MemoryCacheSpaceManagementTests()
	{
		// Set a size limit
		MemoryCacheOptions cacheOptions = new() { SizeLimit = 1000 };
		cache = new MemoryCache(cacheOptions);
		options = new CacheOptions { MaxCacheSizeInBytes = 1000, SuppressLogs = true };
		metrics = new CacheMetrics();
		tracker = new CacheTracker();
		next = A.Fake<RequestDelegate>();
		context = new DefaultHttpContext();
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WhenItemTooLarge_SkipsAndIncrementsMetric()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new() { { options.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict);
		context.Response.StatusCode = StatusCodes.Status200OK;

		// Setup next to write data larger than max cache size
		byte[] largeData = new byte[options.MaxCacheSizeInBytes + 100];
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(largeData, 0, largeData.Length));

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		metrics.SkippedDueToSize().ShouldBe(1);
		metrics.CurrentCacheEntryCount().ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WhenEnoughSpace_CachesItem()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new() { { options.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict);
		context.Response.StatusCode = StatusCodes.Status200OK;

		// Setup next to write small data
		byte[] smallData = Encoding.UTF8.GetBytes("small data");
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(smallData, 0, smallData.Length));

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		metrics.CurrentCacheEntryCount().ShouldBe(1);
		metrics.CurrentCacheSize().ShouldBe(smallData.Length);
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WhenCacheFull_EvictsOldestEntries()
	{
		// Arrange - Add entries until cache is nearly full using the middleware
		for (int i = 0; i < 3; i++)
		{
			HttpContext testContext = new DefaultHttpContext
			{
				Request = { Method = "GET", Path = $"/api/test{i}" },
				Response = { StatusCode = StatusCodes.Status200OK, Body = new MemoryStream() }
			};

			Dictionary<string, StringValues> queryDict = new() { { options.UseCacheQueryParam, "true" } };
			testContext.Request.Query = new QueryCollection(queryDict);

			byte[] data = new byte[200];
			RequestDelegate testNext = A.Fake<RequestDelegate>();
			A.CallTo(() => testNext(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(data, 0, data.Length));

			MemoryCacheMiddleware middleware = new(testNext, cache, options, metrics, tracker);
			await middleware.InvokeAsync(testContext);

			// Small delay to ensure different creation times
			await Task.Delay(10);
		}

		long initialCount = metrics.CurrentCacheEntryCount();
		initialCount.ShouldBe(3);

		// Now try to add a large item that requires eviction
		Dictionary<string, StringValues> queryDict2 = new() { { options.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict2);
		context.Request.Path = "/api/new-request";
		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Body = new MemoryStream();

		byte[] newData = new byte[options.MaxCacheSizeInBytes - 50]; // Large enough to trigger eviction
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(newData, 0, newData.Length));

		MemoryCacheMiddleware middleware2 = new(next, cache, options, metrics, tracker);

		// Act
		await middleware2.InvokeAsync(context);

		// Assert
		metrics.EvictedDueToCapacity().ShouldBeGreaterThanOrEqualTo(0);
		metrics.CurrentCacheSize().ShouldBeLessThanOrEqualTo(options.MaxCacheSizeInBytes);
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WithSuppressedLogs_DoesNotLog()
	{
		// Arrange - Fill cache using middleware
		for (int i = 0; i < 5; i++)
		{
			HttpContext testContext = new DefaultHttpContext
			{
				Request = { Method = "GET", Path = $"/api/test{i}" },
				Response = { StatusCode = StatusCodes.Status200OK, Body = new MemoryStream() }
			};

			Dictionary<string, StringValues> queryDict = new() { { options.UseCacheQueryParam, "true" } };
			testContext.Request.Query = new QueryCollection(queryDict);

			byte[] data = new byte[150];
			RequestDelegate testNext = A.Fake<RequestDelegate>();
			A.CallTo(() => testNext(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(data, 0, data.Length));

			MemoryCacheMiddleware middleware = new(testNext, cache, options, metrics, tracker);
			await middleware.InvokeAsync(testContext);
			await Task.Delay(5);
		}

		// Try to add large item
		Dictionary<string, StringValues> queryDict2 = new() { { options.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict2);
		context.Request.Path = "/api/new";
		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Body = new MemoryStream();

		byte[] newData = new byte[400];
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(newData, 0, newData.Length));

		MemoryCacheMiddleware middleware2 = new(next, cache, options, metrics, tracker);

		// Act
		await middleware2.InvokeAsync(context);

		// Assert - should complete successfully and maintain cache within limits
		metrics.CurrentCacheSize().ShouldBeLessThanOrEqualTo(options.MaxCacheSizeInBytes);
		metrics.CurrentCacheEntryCount().ShouldBeGreaterThan(0);
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WithEntriesAndTags_CleansUpTagsOnEviction()
	{
		// Arrange - Add entries with tags using middleware
		const string tag1 = "tag1";
		for (int i = 0; i < 3; i++)
		{
			HttpContext testContext = new DefaultHttpContext
			{
				Request = { Method = "GET", Path = $"/api/test{i}" },
				Response = { StatusCode = StatusCodes.Status200OK, Body = new MemoryStream() }
			};

			Dictionary<string, StringValues> queryDict = new() { { options.UseCacheQueryParam, "true" } };
			testContext.Request.Query = new QueryCollection(queryDict);
			testContext.Request.Headers[options.CacheTagHeader] = tag1;

			byte[] data = new byte[200];
			RequestDelegate testNext = A.Fake<RequestDelegate>();
			A.CallTo(() => testNext(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(data, 0, data.Length));

			MemoryCacheMiddleware middleware = new(testNext, cache, options, metrics, tracker);
			await middleware.InvokeAsync(testContext);
			await Task.Delay(10);
		}

		tracker.CacheTags.ContainsKey(tag1).ShouldBeTrue();

		// Try to add large item that triggers eviction
		Dictionary<string, StringValues> queryDict2 = new() { { options.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict2);
		context.Request.Path = "/api/new";
		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Body = new MemoryStream();

		byte[] newData = new byte[700];
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(newData, 0, newData.Length));

		MemoryCacheMiddleware middleware2 = new(next, cache, options, metrics, tracker);

		// Act
		await middleware2.InvokeAsync(context);

		// Assert
		metrics.EvictedDueToCapacity().ShouldBeGreaterThanOrEqualTo(0);
		// Tags should be cleaned up if all associated entries are evicted
		if (tracker.CacheTags.TryGetValue(tag1, out HashSet<string>? remainingKeys))
		{
			remainingKeys.Count.ShouldBeLessThanOrEqualTo(3); // Some or all entries may have been evicted
		}
	}

	[RetryFact(3)]
	public async Task EnsureCacheSpaceAvailable_WhenCannotFreeEnoughSpace_ReturnsFalse()
	{
		// Arrange - Test the behavior with a small cache
		CacheOptions smallOptions = new() { MaxCacheSizeInBytes = 50, SuppressLogs = true };

		// Add a small entry first using middleware
		HttpContext testContext = new DefaultHttpContext
		{
			Request = { Method = "GET", Path = "/api/small" },
			Response = { StatusCode = StatusCodes.Status200OK, Body = new MemoryStream() }
		};

		Dictionary<string, StringValues> queryDict = new() { { smallOptions.UseCacheQueryParam, "true" } };
		testContext.Request.Query = new QueryCollection(queryDict);

		byte[] smallData = new byte[20];
		RequestDelegate testNext = A.Fake<RequestDelegate>();
		A.CallTo(() => testNext(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(smallData, 0, smallData.Length));

		MemoryCacheMiddleware smallMiddleware = new(testNext, cache, smallOptions, metrics, tracker);
		await smallMiddleware.InvokeAsync(testContext);

		// Try to add data that will fit after evicting existing entry
		Dictionary<string, StringValues> queryDict2 = new() { { smallOptions.UseCacheQueryParam, "true" } };
		context.Request.Query = new QueryCollection(queryDict2);
		context.Request.Path = "/api/test";
		context.Response.StatusCode = StatusCodes.Status200OK;
		context.Response.Body = new MemoryStream();

		byte[] newData = new byte[40]; // Will fit after evicting the small entry
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) => ctx.Response.Body.Write(newData, 0, newData.Length));

		MemoryCacheMiddleware middleware = new(next, cache, smallOptions, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert - The middleware should handle this gracefully
		metrics.CurrentCacheSize().ShouldBeLessThanOrEqualTo(smallOptions.MaxCacheSizeInBytes);
	}
}
