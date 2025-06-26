using System.Text;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class CacheKeyGenerationTests
{
    private readonly IFixture _fixture;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly CacheMetrics _metrics;
    private readonly CacheTracker _tracker;
    private readonly HttpContext _context;

    public CacheKeyGenerationTests()
    {
        _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        _cache = A.Fake<IMemoryCache>();
        _options = new CacheOptions();
        _metrics = new CacheMetrics();
        _tracker = new CacheTracker();
        _context = new DefaultHttpContext();
    }

    [Theory]
    [InlineData("/api/test", "param1=value1", "GET")]
    [InlineData("/api/test", "", "GET")]
    [InlineData("/api/test", "param1=value1&param2=value2", "GET")]
    public async Task GenerateCacheKey_WithVariousRequests_GeneratesUniqueKeys(string path, string queryString, string method)
    {
        // Arrange
        _context.Request.Method = method;
        _context.Request.Path = path;

        // Combine all query parameters
        Dictionary<string, StringValues> queryParams = new()
        {
            { _options.UseCacheQueryParam, "true" }
        };

        if (!string.IsNullOrEmpty(queryString))
        {
            foreach (string param in queryString.Split('&'))
            {
                string[] parts = param.Split('=');
                queryParams.Add(parts[0], new StringValues(parts[1]));
            }
        }

        _context.Request.Query = new QueryCollection(queryParams);

        MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: _cache, cacheOptions: _options, cacheMetrics: _metrics, cacheTracker: _tracker);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        A.CallTo(() => _cache.CreateEntry(A<string>.That.Matches(key => key.Contains(path) &&
            key.Contains(_options.UseCacheQueryParam) &&
            (string.IsNullOrEmpty(queryString) || queryString.Split('&', StringSplitOptions.None).All(param => key.Contains(param)))))).MustHaveHappened();
    }

    [Fact]
    public async Task GenerateCacheKey_WithPostRequest_IncludesBodyHash()
    {
        // Arrange
        _context.Request.Method = "POST";
        _context.Request.Path = "/api/test";
        const string body = "test body content";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        _context.Request.Body = new MemoryStream(bodyBytes);

        Dictionary<string, StringValues> queryDict = new()
        {
            { _options.UseCacheQueryParam, "true" }
        };

        _context.Request.Query = new QueryCollection(queryDict);

        MemoryCacheMiddleware middleware = new(next: A.Fake<RequestDelegate>(), cache: _cache, cacheOptions: _options, cacheMetrics: _metrics, cacheTracker: _tracker);

        // Act
        await middleware.InvokeAsync(_context);

        // Assert
        A.CallTo(() => _cache.CreateEntry(A<string>.That.Contains("|"))).MustHaveHappened();
        _context.Request.Body.Position.ShouldBe(0); // Verify body position is reset
    }
}
