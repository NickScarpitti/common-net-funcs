using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheEndpointsTests
{
	[RetryFact(3)]
	public async Task MapCacheMetrics_ReturnsAllMetricsFields_Successfully()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapCacheMetrics());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();

		// Add comprehensive test metrics
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementMisses();
		metrics.AddToSize(2048);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();
		metrics.IncrementSkippedDueToSize();
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Removed);

		// Add tags
		metrics.CacheTags.TryAdd("tag1", ["key1", "key2"]);
		metrics.CacheTags.TryAdd("tag2", ["key3"]);

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/memory-cache-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync();

		// Verify all fields are present
		content.ShouldContain("\"Hits\":3");
		content.ShouldContain("\"Misses\":1");
		content.ShouldContain("\"HitRatio\":\"75%\""); // 3 hits / 4 total = 0.75 = 75%
		content.ShouldContain("\"SkippedDueToSize\":1");
		content.ShouldContain("\"CurrentSizeBytes\":\"2048B\"");
		content.ShouldContain("\"CurrentSize\":");
		content.ShouldContain("\"CacheEntries\":2");
		content.ShouldContain("\"EntriesCountByTag\":2");
		content.ShouldContain("\"Tags\":");
		content.ShouldContain("\"tag1\":2");
		content.ShouldContain("\"tag2\":1");
		content.ShouldContain("\"EvictionReason\":");
		content.ShouldContain("\"Capacity\":2");
		content.ShouldContain("\"ManuallyRemoved\":1");
	}

	[RetryFact(3)]
	public async Task MapCacheMetrics_WithZeroValues_ReturnsCorrectMetrics()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapCacheMetrics());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act - Don't add any metrics, all should be zero
		HttpResponseMessage response = await client.GetAsync("/api/memory-cache-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync();

		content.ShouldContain("\"Hits\":0");
		content.ShouldContain("\"Misses\":0");
		content.ShouldContain("\"HitRatio\":\"0%\""); // Division by zero case
		content.ShouldContain("\"SkippedDueToSize\":0");
		content.ShouldContain("\"CurrentSizeBytes\":\"0B\"");
		content.ShouldContain("\"CacheEntries\":0");
		content.ShouldContain("\"EntriesCountByTag\":0");
		content.ShouldContain("\"Capacity\":0");
		content.ShouldContain("\"ManuallyRemoved\":0");
	}

	[RetryFact(3)]
	public async Task MapCacheMetrics_WithOnlyMisses_CalculatesZeroHitRatio()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapCacheMetrics());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();

		// Add only misses, no hits
		metrics.IncrementMisses();
		metrics.IncrementMisses();
		metrics.IncrementMisses();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/memory-cache-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync();

		content.ShouldContain("\"Hits\":0");
		content.ShouldContain("\"Misses\":3");
		content.ShouldContain("\"HitRatio\":\"0%\""); // 0 hits / 3 total = 0%
	}

	[RetryFact(3)]
	public async Task MapCacheMetrics_WithLargeSizeValue_FormatsCorrectly()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapCacheMetrics());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();

		// Add large size value to test formatting
		long largeSize = 1024 * 1024 * 5; // 5 MB
		metrics.AddToSize(largeSize);

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/memory-cache-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync();

		content.ShouldContain($"\"CurrentSizeBytes\":\"{largeSize}B\"");
		content.ShouldContain("\"CurrentSize\":"); // Should have formatted size
	}

	[RetryFact(3)]
	public async Task MapCacheMetrics_WithManyTags_ReturnsAllTags()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapCacheMetrics());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();

		// Add multiple tags with different key counts
		metrics.CacheTags.TryAdd("users", ["user1", "user2", "user3"]);
		metrics.CacheTags.TryAdd("products", ["prod1", "prod2"]);
		metrics.CacheTags.TryAdd("orders", ["order1"]);

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/memory-cache-metrics");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string content = await response.Content.ReadAsStringAsync();

		content.ShouldContain("\"EntriesCountByTag\":3");
		content.ShouldContain("\"users\":3");
		content.ShouldContain("\"products\":2");
		content.ShouldContain("\"orders\":1");
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByKey_WithEmptyKey_ReturnsBadRequest()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act - Use a key that will be matched by route but is whitespace
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/key/%20%20", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Cache key is required");
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByKey_WithExistingKey_EvictsSuccessfully()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();
		CacheTracker tracker = host.Services.GetRequiredService<CacheTracker>();

		// Add a cache entry
		string cacheKey = "test-key";
		CacheEntry entry = new()
		{
			Data = Encoding.UTF8.GetBytes("test data"),
			Headers = new Dictionary<string, string>(),
			Tags = ["tag1"]
		};
		cache.Set(cacheKey, entry);
		tracker.CacheTags.TryAdd("tag1", [cacheKey]);
		metrics.AddToSize(entry.Data.Length);
		metrics.IncrementCacheEntryCount();

		// Act
		HttpResponseMessage response = await client.PostAsync($"/api/memorycache/evict/key/{cacheKey}", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(1);
		cache.TryGetValue(cacheKey, out CacheEntry? _).ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByKey_WithNonExistentKey_ReturnsZero()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/key/nonexistent", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByTag_WithEmptyTag_ReturnsBadRequest()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act - Use a tag that will be matched by route but is whitespace
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/tag/%20%20", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Cache tag is required");
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByTag_WithExistingTag_EvictsAllEntriesWithTag()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();
		CacheTracker tracker = host.Services.GetRequiredService<CacheTracker>();

		// Add cache entries with the same tag
		string tag = "test-tag";
		string key1 = "key1";
		string key2 = "key2";

		CacheEntry entry1 = new()
		{
			Data = Encoding.UTF8.GetBytes("data1"),
			Headers = new Dictionary<string, string>(),
			Tags = [tag]
		};
		CacheEntry entry2 = new()
		{
			Data = Encoding.UTF8.GetBytes("data2"),
			Headers = new Dictionary<string, string>(),
			Tags = [tag]
		};

		cache.Set(key1, entry1);
		cache.Set(key2, entry2);
		tracker.CacheTags.TryAdd(tag, [key1, key2]);
		metrics.AddToSize(entry1.Data.Length + entry2.Data.Length);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();

		// Act
		HttpResponseMessage response = await client.PostAsync($"/api/memorycache/evict/tag/{tag}", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(2);
		cache.TryGetValue(key1, out CacheEntry? _).ShouldBeFalse();
		cache.TryGetValue(key2, out CacheEntry? _).ShouldBeFalse();
		tracker.CacheTags.ContainsKey(tag).ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByTag_WithNonExistentTag_ReturnsZero()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/tag/nonexistent", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictAll_ClearsCache_Successfully()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();
		CacheTracker tracker = host.Services.GetRequiredService<CacheTracker>();

		// Add cache entries
		CacheEntry entry1 = new()
		{
			Data = Encoding.UTF8.GetBytes("data1"),
			Headers = new Dictionary<string, string>(),
			Tags = ["tag1"]
		};
		CacheEntry entry2 = new()
		{
			Data = Encoding.UTF8.GetBytes("data2"),
			Headers = new Dictionary<string, string>(),
			Tags = ["tag2"]
		};

		cache.Set("key1", entry1);
		cache.Set("key2", entry2);
		tracker.CacheTags.TryAdd("tag1", ["key1"]);
		tracker.CacheTags.TryAdd("tag2", ["key2"]);
		metrics.AddToSize(entry1.Data.Length + entry2.Data.Length);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();

		// Act
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/all", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBeGreaterThanOrEqualTo(0); // Returns the count of entries before clearing
		tracker.CacheTags.Count.ShouldBe(0);
		metrics.CurrentCacheSize().ShouldBe(0);
		metrics.CurrentCacheEntryCount().ShouldBe(0);
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_WithAuthorizationPolicy_RequiresAuthorization()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
						services.AddAuthentication("TestScheme")
							.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
						services.AddAuthorization(options => options.AddPolicy("TestPolicy", policy =>
								policy.RequireAuthenticatedUser()));
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseAuthentication();
						app.UseAuthorization();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints("TestPolicy"));
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();

		// Act - request without authentication
		HttpResponseMessage response = await client.PostAsync("/api/memorycache/evict/key/test", null);

		// Assert - should be unauthorized or forbidden
		(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden).ShouldBeTrue();
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByKey_WithTags_RemovesFromAllTags()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();
		CacheTracker tracker = host.Services.GetRequiredService<CacheTracker>();

		// Add a cache entry with multiple tags
		string cacheKey = "test-key";
		CacheEntry entry = new()
		{
			Data = Encoding.UTF8.GetBytes("test data"),
			Headers = new Dictionary<string, string>(),
			Tags = ["tag1", "tag2", "tag3"]
		};
		cache.Set(cacheKey, entry);
		tracker.CacheTags.TryAdd("tag1", [cacheKey]);
		tracker.CacheTags.TryAdd("tag2", [cacheKey]);
		tracker.CacheTags.TryAdd("tag3", [cacheKey]);
		metrics.CacheTags.TryAdd("tag1", new HashSet<string> { cacheKey });
		metrics.CacheTags.TryAdd("tag2", new HashSet<string> { cacheKey });
		metrics.CacheTags.TryAdd("tag3", new HashSet<string> { cacheKey });
		metrics.AddToSize(entry.Data.Length);
		metrics.IncrementCacheEntryCount();

		// Act
		HttpResponseMessage response = await client.PostAsync($"/api/memorycache/evict/key/{cacheKey}", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(1);

		// Verify all tags were removed
		tracker.CacheTags.ContainsKey("tag1").ShouldBeFalse();
		tracker.CacheTags.ContainsKey("tag2").ShouldBeFalse();
		tracker.CacheTags.ContainsKey("tag3").ShouldBeFalse();
		metrics.CacheTags.ContainsKey("tag1").ShouldBeFalse();
		metrics.CacheTags.ContainsKey("tag2").ShouldBeFalse();
		metrics.CacheTags.ContainsKey("tag3").ShouldBeFalse();
	}

	[RetryFact(3)]
	public async Task MapEvictionEndpoints_EvictByTag_WithMultipleEntriesWithSameTag_EvictsAll()
	{
		// Arrange
		using IHost host = await new HostBuilder()
			.ConfigureWebHost(webBuilder => webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddRouting();
						services.MemoryValueCaching();
					})
					.Configure(app =>
					{
						app.UseRouting();
						app.UseEndpoints(endpoints => endpoints.MapEvictionEndpoints());
					}))
			.StartAsync();

		HttpClient client = host.GetTestClient();
		IMemoryCache cache = host.Services.GetRequiredService<IMemoryCache>();
		CacheMetrics metrics = host.Services.GetRequiredService<CacheMetrics>();
		CacheTracker tracker = host.Services.GetRequiredService<CacheTracker>();

		// Add cache entries with overlapping tags
		string tag = "shared-tag";
		string key1 = "key1";
		string key2 = "key2";
		string key3 = "key3";

		CacheEntry entry1 = new()
		{
			Data = Encoding.UTF8.GetBytes("data1"),
			Headers = new Dictionary<string, string>(),
			Tags = [tag, "tag1"]
		};
		CacheEntry entry2 = new()
		{
			Data = Encoding.UTF8.GetBytes("data2"),
			Headers = new Dictionary<string, string>(),
			Tags = [tag, "tag2"]
		};
		CacheEntry entry3 = new()
		{
			Data = Encoding.UTF8.GetBytes("data3"),
			Headers = new Dictionary<string, string>(),
			Tags = [tag]
		};

		cache.Set(key1, entry1);
		cache.Set(key2, entry2);
		cache.Set(key3, entry3);
		tracker.CacheTags.TryAdd(tag, [key1, key2, key3]);
		tracker.CacheTags.TryAdd("tag1", [key1]);
		tracker.CacheTags.TryAdd("tag2", [key2]);
		metrics.AddToSize(entry1.Data.Length + entry2.Data.Length + entry3.Data.Length);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();

		// Act
		HttpResponseMessage response = await client.PostAsync($"/api/memorycache/evict/tag/{tag}", null);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		int result = await response.Content.ReadFromJsonAsync<int>();
		result.ShouldBe(3);
		cache.TryGetValue(key1, out CacheEntry? _).ShouldBeFalse();
		cache.TryGetValue(key2, out CacheEntry? _).ShouldBeFalse();
		cache.TryGetValue(key3, out CacheEntry? _).ShouldBeFalse();
		tracker.CacheTags.ContainsKey(tag).ShouldBeFalse();
		tracker.CacheTags.ContainsKey("tag1").ShouldBeFalse();
		tracker.CacheTags.ContainsKey("tag2").ShouldBeFalse();
	}
}

// Test authentication handler for testing authorization
internal class TestAuthHandler(IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
	: Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
	{
		// Return authentication failure for testing
		return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
	}
}
