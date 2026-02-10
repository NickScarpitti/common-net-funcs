using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheMiddlewareTests
{
	private readonly IMemoryCache cache;
	private readonly CacheOptions options;
	private readonly CacheMetrics metrics;
	private readonly CacheTracker tracker;
	private readonly RequestDelegate next;
	private readonly HttpContext context;

	public MemoryCacheMiddlewareTests()
	{
		cache = A.Fake<IMemoryCache>();
		options = new CacheOptions();
		metrics = new CacheMetrics();
		tracker = new CacheTracker();
		next = A.Fake<RequestDelegate>();
		context = new DefaultHttpContext();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithoutCacheParams_CallsNextDelegate()
	{
		// Arrange
		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryTheory(3)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task InvokeAsync_WithCacheParams_HandlesFlags(bool useCache, bool evictCache)
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, useCache.ToString() },
				{ options.EvictionQueryParam, evictCache.ToString() }
			};
		context.Request.Query = new QueryCollection(queryDict);

		// Setup TryGetValue to return true and a dummy entry if eviction is expected
		if (evictCache)
		{
			object? outValue = new CacheEntry
			{
				Data = Encoding.UTF8.GetBytes("dummy"),
				Headers = new Dictionary<string, string>()
			};
			A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);
		}

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		if (evictCache)
		{
			A.CallTo(() => cache.Remove(A<string>._)).MustHaveHappened();
		}
		if (!useCache)
		{
			A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
		}
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCacheHit_ReturnsCachedResponse()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		byte[] cachedData = Encoding.UTF8.GetBytes("cached response");
		object? outValue = new CacheEntry()
		{
			Data = cachedData,
			Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
		};

		// Mock the interface method instead of the extension method
		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		MemoryStream responseStream = new();
		context.Response.Body = responseStream;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		responseStream.Position = 0;
		string result = await new StreamReader(responseStream).ReadToEndAsync();
		result.ShouldBe("cached response");
		metrics.CacheHits().ShouldBe(1);
		A.CallTo(() => next(context)).MustNotHaveHappened();
	}

	[RetryTheory(3)]
	[InlineData("10", null, null)]
	[InlineData(null, "5", null)]
	[InlineData(null, null, "2")]
	public async Task InvokeAsync_WithCustomCacheDuration_SetsCacheOptions(string? seconds, string? minutes, string? hours)
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};

		if (seconds != null)
		{
			queryDict.Add(options.CacheSecondsQueryParam, seconds);
		}

		if (minutes != null)
		{
			queryDict.Add(options.CacheMinutesQueryParam, minutes);
		}

		if (hours != null)
		{
			queryDict.Add(options.CacheHoursQueryParam, hours);
		}

		context.Request.Query = new QueryCollection(queryDict);
		context.Response.StatusCode = StatusCodes.Status200OK;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		metrics.CacheMisses().ShouldBe(1);
		A.CallTo(() => cache.CreateEntry(A<object>._)).MustHaveHappened();
	}
}
