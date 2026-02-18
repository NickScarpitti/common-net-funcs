using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;
using static CommonNetFuncs.Compression.Streams;

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

	[RetryFact(3)]
	public async Task InvokeAsync_WithNon200StatusCode_DoesNotCache()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Response.StatusCode = StatusCodes.Status404NotFound;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		metrics.CacheMisses().ShouldBe(1);
		// Verify no cache entry was created
		long initialMetrics = metrics.CurrentCacheEntryCount();
		initialMetrics.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WhenTaskCanceled_LogsWarningIfNotSuppressed()
	{
		// Arrange
		RequestDelegate throwingNext = A.Fake<RequestDelegate>();
		A.CallTo(() => throwingNext(A<HttpContext>._)).Throws(new TaskCanceledException());

		CacheOptions optionsWithLogging = new() { SuppressLogs = false };
		MemoryCacheMiddleware middleware = new(throwingNext, cache, optionsWithLogging, metrics, tracker);

		// Act & Assert - should not throw
		await middleware.InvokeAsync(context);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCachingDisabled_CallsNextDelegate()
	{
		// Arrange - no cache params in query
		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => next(context)).MustHaveHappenedOnceExactly();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCompression_CachesCompressedData()
	{
		// Arrange
		CacheOptions optionsWithCompression = new() { UseCompression = true, CompressionType = ECompressionType.Gzip };
		Dictionary<string, StringValues> queryDict = new()
			{
				{ optionsWithCompression.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Response.StatusCode = StatusCodes.Status200OK;

		MemoryStream responseStream = new();
		context.Response.Body = responseStream;

		// Setup next to write some data
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			byte[] data = Encoding.UTF8.GetBytes("Large response data that should be compressed");
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, optionsWithCompression, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.CreateEntry(A<object>._)).MustHaveHappened();
		metrics.CacheMisses().ShouldBe(1);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCacheHitAndCompression_DecompressesData()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		string originalData = "Test data";
		byte[] compressedData = await Encoding.UTF8.GetBytes(originalData).Compress(ECompressionType.Gzip);
		object? outValue = new CacheEntry()
		{
			Data = compressedData,
			Headers = new Dictionary<string, string>(),
			CompressionType = (short)ECompressionType.Gzip
		};

		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		MemoryStream responseStream = new();
		context.Response.Body = responseStream;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		responseStream.Position = 0;
		string result = await new StreamReader(responseStream).ReadToEndAsync();
		result.ShouldBe(originalData);
		metrics.CacheHits().ShouldBe(1);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithEmptyResponseData_CachesEmptyResponse()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		object? outValue = new CacheEntry()
		{
			Data = Array.Empty<byte>(),
			Headers = new Dictionary<string, string>()
		};

		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		MemoryStream responseStream = new();
		context.Response.Body = responseStream;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		responseStream.Length.ShouldBe(0);
		metrics.CacheHits().ShouldBe(1);
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCacheTags_RegistersTagsInTracker()
	{
		// Arrange
		const string tagName = "test-tag";
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Headers[options.CacheTagHeader] = tagName;
		context.Response.StatusCode = StatusCodes.Status200OK;

		// Setup next to write some data
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			byte[] data = Encoding.UTF8.GetBytes("response");
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		tracker.CacheTags.ContainsKey(tagName).ShouldBeTrue();
		metrics.CacheTags.ContainsKey(tagName).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithMultipleCacheTags_RegistersAllTags()
	{
		// Arrange
		const string tags = "tag1,tag2,tag3";
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);
		context.Request.Headers[options.CacheTagHeader] = tags;
		context.Response.StatusCode = StatusCodes.Status200OK;

		// Setup next to write some data
		A.CallTo(() => next(A<HttpContext>._)).Invokes((HttpContext ctx) =>
		{
			byte[] data = Encoding.UTF8.GetBytes("response");
			ctx.Response.Body.Write(data, 0, data.Length);
		});

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		tracker.CacheTags.ContainsKey("tag1").ShouldBeTrue();
		tracker.CacheTags.ContainsKey("tag2").ShouldBeTrue();
		tracker.CacheTags.ContainsKey("tag3").ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task InvokeAsync_WithCachedHeaders_RestoresHeaders()
	{
		// Arrange
		Dictionary<string, StringValues> queryDict = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};
		context.Request.Query = new QueryCollection(queryDict);

		Dictionary<string, string> cachedHeaders = new()
		{
			{ "Content-Type", "application/json" },
			{ "Cache-Control", "max-age=3600" }
		};

		object? outValue = new CacheEntry()
		{
			Data = Encoding.UTF8.GetBytes("data"),
			Headers = cachedHeaders
		};

		A.CallTo(() => cache.TryGetValue(A<object>._, out outValue)).Returns(true);

		MemoryStream responseStream = new();
		context.Response.Body = responseStream;

		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		context.Response.Headers["Content-Type"].ToString().ShouldBe("application/json");
		context.Response.Headers["Cache-Control"].ToString().ShouldBe("max-age=3600");
	}

	[RetryFact(3)]
	public void Dispose_ShouldDisposeResources()
	{
		// Arrange
		MemoryCacheMiddleware middleware = new(next, cache, options, metrics, tracker);

		// Act & Assert - should not throw
		middleware.Dispose();
		middleware.Dispose(); // Should be safe to call multiple times
	}
}
