using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class CacheKeyGenerationTests
{
	private readonly IMemoryCache cache;
	private readonly CacheOptions options;
	private readonly CacheMetrics metrics;
	private readonly CacheTracker tracker;
	private readonly HttpContext context;

	public CacheKeyGenerationTests()
	{
		cache = A.Fake<IMemoryCache>();
		options = new CacheOptions();
		metrics = new CacheMetrics();
		tracker = new CacheTracker();
		context = new DefaultHttpContext();
	}

	[RetryTheory(3)]
	[InlineData("/api/test", "param1=value1", "GET")]
	[InlineData("/api/test", "", "GET")]
	[InlineData("/api/test", "param1=value1&param2=value2", "GET")]
	public async Task GenerateCacheKey_WithVariousRequests_GeneratesUniqueKeys(string path, string queryString, string method)
	{
		// Arrange
		context.Request.Method = method;
		context.Request.Path = path;

		// Combine all query parameters
		Dictionary<string, StringValues> queryParams = new()
			{
				{ options.UseCacheQueryParam, "true" }
			};

		if (!string.IsNullOrEmpty(queryString))
		{
			foreach (string param in queryString.Split('&'))
			{
				string[] parts = param.Split('=');
				queryParams.Add(parts[0], new StringValues(parts[1]));
			}
		}

		context.Request.Query = new QueryCollection(queryParams);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.CreateEntry(A<string>.That.Matches(key => key.Contains(path) &&
			key.Contains(options.UseCacheQueryParam) &&
			(string.IsNullOrEmpty(queryString) || queryString.Split('&', StringSplitOptions.None).All(param => key.Contains(param)))))).MustHaveHappened();
	}

	[RetryFact(3)]
	public async Task GenerateCacheKey_WithPostRequest_IncludesBodyHash()
	{
		// Arrange
		context.Request.Method = "POST";
		context.Request.Path = "/api/test";
		const string body = "test body content";
		byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
		context.Request.Body = new MemoryStream(bodyBytes);

		Dictionary<string, StringValues> queryDict = new()
				{
						{ options.UseCacheQueryParam, "true" }
				};

		context.Request.Query = new QueryCollection(queryDict);

		MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: cache, cacheOptions: options, cacheMetrics: metrics, cacheTracker: tracker);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		A.CallTo(() => cache.CreateEntry(A<string>.That.Contains("|"))).MustHaveHappened();
		context.Request.Body.Position.ShouldBe(0); // Verify body position is reset
	}
}
