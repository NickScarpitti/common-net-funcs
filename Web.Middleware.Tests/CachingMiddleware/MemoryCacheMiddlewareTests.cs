using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry;

namespace Web.Middleware.Tests.CachingMiddleware;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public sealed class MemoryCacheMiddlewareTests
{
	private readonly IFixture _fixture;
	private readonly IMemoryCache _cache;
	private readonly CacheOptions _options;
	private readonly CacheMetrics _metrics;
	private readonly CacheTracker _tracker;
	private readonly RequestDelegate _next;
	private readonly HttpContext _context;

	public MemoryCacheMiddlewareTests()
	{
		_fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
		_cache = A.Fake<IMemoryCache>();
		_options = new CacheOptions();
		_metrics = new CacheMetrics();
		_tracker = new CacheTracker();
		_next = A.Fake<RequestDelegate>();
		_context = new DefaultHttpContext();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithoutCacheParams_CallsNextDelegate()
	{
		// Arrange
		MemoryCacheMiddleware middleware = new(_next, _cache, _options, _metrics, _tracker);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
	}

	[RetryTheory(3)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task InvokeAsync_WithCacheParams_HandlesFlags(bool useCache, bool evictCache)
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
				{
						{ _options.UseCacheQueryParam, useCache.ToString() },
						{ _options.EvictionQueryParam, evictCache.ToString() }
				};
		_context.Request.Query = new QueryCollection(queryDict);

		// Setup TryGetValue to return true and a dummy entry if eviction is expected
		if (evictCache)
		{
			object? outValue = new CacheEntry
			{
				Data = Encoding.UTF8.GetBytes("dummy"),
				Headers = new Dictionary<string, string>()
			};
			A.CallTo(() => _cache.TryGetValue(A<object>._, out outValue)).Returns(true);
		}

		MemoryCacheMiddleware middleware = new(_next, _cache, _options, _metrics, _tracker);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		if (evictCache)
		{
			A.CallTo(() => _cache.Remove(A<string>._)).MustHaveHappened();
		}
		if (!useCache)
		{
			A.CallTo(() => _next(_context)).MustHaveHappenedOnceExactly();
		}
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCacheHit_ReturnsCachedResponse()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
				{
						{ _options.UseCacheQueryParam, "true" }
				};
		_context.Request.Query = new QueryCollection(queryDict);

		byte[] cachedData = Encoding.UTF8.GetBytes("cached response");
		object? outValue = new CacheEntry()
		{
			Data = cachedData,
			Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
		};

		// Mock the interface method instead of the extension method
		A.CallTo(() => _cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		MemoryStream responseStream = new();
		_context.Response.Body = responseStream;

		MemoryCacheMiddleware middleware = new(_next, _cache, _options, _metrics, _tracker);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		responseStream.Position = 0;
		string result = await new StreamReader(responseStream).ReadToEndAsync();
		result.ShouldBe("cached response");
		_metrics.CacheHits().ShouldBe(1);
		A.CallTo(() => _next(_context)).MustNotHaveHappened();
	}

	[RetryTheory(3)]
	[InlineData("10", null, null)]
	[InlineData(null, "5", null)]
	[InlineData(null, null, "2")]
	public async Task InvokeAsync_WithCustomCacheDuration_SetsCacheOptions(
			string? seconds, string? minutes, string? hours)
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
				{
						{ _options.UseCacheQueryParam, "true" }
				};

		if (seconds != null)
		{
			queryDict.Add(_options.CacheSecondsQueryParam, seconds);
		}

		if (minutes != null)
		{
			queryDict.Add(_options.CacheMinutesQueryParam, minutes);
		}

		if (hours != null)
		{
			queryDict.Add(_options.CacheHoursQueryParam, hours);
		}

		_context.Request.Query = new QueryCollection(queryDict);
		_context.Response.StatusCode = StatusCodes.Status200OK;

		MemoryCacheMiddleware middleware = new(_next, _cache, _options, _metrics, _tracker);

		// Act
		await middleware.InvokeAsync(_context);

		// Assert
		_metrics.CacheMisses().ShouldBe(1);
		A.CallTo(() => _cache.CreateEntry(A<object>._)).MustHaveHappened();
	}
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
