using CommonNetFuncs.Web.Common.CachingSupportClasses;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Common.CachingSupportClasses.CacheOptionDefaults;

namespace Web.Common.Tests.CachingSupportClasses;

public class CacheOptionsTests
{
	[Fact]
	public void CacheOptions_DefaultValues_ShouldBeCorrect()
	{
		// Arrange & Act
		CacheOptions options = new();

		// Assert
		options.EvictionQueryParam.ShouldBe(DefaultEvictionQueryParam);
		options.UseCacheQueryParam.ShouldBe(DefaultCacheQueryParam);
		options.CacheSecondsQueryParam.ShouldBe(DefaultCacheSecondsQueryParam);
		options.CacheMinutesQueryParam.ShouldBe(DefaultCacheMinutesQueryParam);
		options.CacheHoursQueryParam.ShouldBe(DefaultCacheHoursQueryParam);
		options.CacheTagHeader.ShouldBe(DefaultCacheTagHeader);
		options.EvictTagQueryParam.ShouldBe(DefaultEvictTagQueryParam);
		options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(5));
		options.MaxCacheSizeInBytes.ShouldBe(DefaultCacheSize);
		options.HeadersToCache.ShouldBe(DefaultHeadersToCache);
		options.UseCompression.ShouldBeTrue();
		options.CompressionType.ShouldBe(ECompressionType.Gzip);
		options.SuppressLogs.ShouldBeTrue();
	}

	[Fact]
	public void CacheOptions_CanSetEvictionQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customEvict";

		// Act
		options.EvictionQueryParam = newValue;

		// Assert
		options.EvictionQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetUseCacheQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customCache";

		// Act
		options.UseCacheQueryParam = newValue;

		// Assert
		options.UseCacheQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetCacheSecondsQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customSeconds";

		// Act
		options.CacheSecondsQueryParam = newValue;

		// Assert
		options.CacheSecondsQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetCacheMinutesQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customMinutes";

		// Act
		options.CacheMinutesQueryParam = newValue;

		// Assert
		options.CacheMinutesQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetCacheHoursQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customHours";

		// Act
		options.CacheHoursQueryParam = newValue;

		// Assert
		options.CacheHoursQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetCacheTagHeader()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "X-Custom-Tags";

		// Act
		options.CacheTagHeader = newValue;

		// Assert
		options.CacheTagHeader.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetEvictTagQueryParam()
	{
		// Arrange
		CacheOptions options = new();
		const string newValue = "customEvictTags";

		// Act
		options.EvictTagQueryParam = newValue;

		// Assert
		options.EvictTagQueryParam.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetDefaultCacheDuration()
	{
		// Arrange
		CacheOptions options = new();
		TimeSpan newValue = TimeSpan.FromMinutes(10);

		// Act
		options.DefaultCacheDuration = newValue;

		// Assert
		options.DefaultCacheDuration.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetMaxCacheSizeInBytes()
	{
		// Arrange
		CacheOptions options = new();
		const long newValue = 50 * 1024 * 1024; // 50MB

		// Act
		options.MaxCacheSizeInBytes = newValue;

		// Assert
		options.MaxCacheSizeInBytes.ShouldBe(newValue);
	}

	[Fact]
	public void CacheOptions_CanSetHeadersToCache()
	{
		// Arrange
		CacheOptions options = new();
		var newHeaders = DefaultHeadersToCache.Add("X-Custom-Header");

		// Act
		options.HeadersToCache = newHeaders;

		// Assert
		options.HeadersToCache.ShouldBe(newHeaders);
		options.HeadersToCache.Count.ShouldBe(DefaultHeadersToCache.Count + 1);
	}

	[Fact]
	public void CacheOptions_CanSetUseCompression()
	{
		// Arrange
		CacheOptions options = new();

		// Act
		options.UseCompression = false;

		// Assert
		options.UseCompression.ShouldBeFalse();
	}

	[Fact]
	public void CacheOptions_CanSetCompressionType()
	{
		// Arrange
		CacheOptions options = new();

		// Act
		options.CompressionType = ECompressionType.Brotli;

		// Assert
		options.CompressionType.ShouldBe(ECompressionType.Brotli);
	}

	[Fact]
	public void CacheOptions_CanSetSuppressLogs()
	{
		// Arrange
		CacheOptions options = new();

		// Act
		options.SuppressLogs = false;

		// Assert
		options.SuppressLogs.ShouldBeFalse();
	}

	[Fact]
	public void CacheOptionDefaults_ConstantValues_ShouldBeCorrect()
	{
		// Assert
		DefaultEvictionQueryParam.ShouldBe("evictCacheEntry");
		DefaultCacheQueryParam.ShouldBe("useCache");
		DefaultCacheSecondsQueryParam.ShouldBe("cacheSeconds");
		DefaultCacheMinutesQueryParam.ShouldBe("cacheMinutes");
		DefaultCacheHoursQueryParam.ShouldBe("cacheHours");
		DefaultCacheTagHeader.ShouldBe("X-Cache-Tags");
		DefaultEvictTagQueryParam.ShouldBe("evictTags");
		DefaultCacheSize.ShouldBe(100 * 1024 * 1024);
	}

	[Fact]
	public void CacheOptionDefaults_HeadersToCache_ShouldContainExpectedHeaders()
	{
		// Assert
		DefaultHeadersToCache.ShouldContain("Content-Type");
		DefaultHeadersToCache.ShouldContain("Content-Language");
		DefaultHeadersToCache.ShouldContain("Content-Encoding");
		DefaultHeadersToCache.ShouldContain("Cache-Control");
		DefaultHeadersToCache.ShouldContain("Vary");
		DefaultHeadersToCache.Count.ShouldBe(5);
	}

	[Fact]
	public void CacheOptionDefaults_HeadersToCache_ShouldBeCaseInsensitive()
	{
		// Assert
		DefaultHeadersToCache.Contains("content-type").ShouldBeTrue();
		DefaultHeadersToCache.Contains("CONTENT-TYPE").ShouldBeTrue();
		DefaultHeadersToCache.Contains("Content-Type").ShouldBeTrue();
	}

	[Fact]
	public void CacheOptions_MultipleProperties_CanBeSetTogether()
	{
		// Arrange & Act
		CacheOptions options = new()
		{
			EvictionQueryParam = "evict",
			UseCacheQueryParam = "cache",
			DefaultCacheDuration = TimeSpan.FromHours(1),
			MaxCacheSizeInBytes = 200 * 1024 * 1024,
			UseCompression = false,
			SuppressLogs = false
		};

		// Assert
		options.EvictionQueryParam.ShouldBe("evict");
		options.UseCacheQueryParam.ShouldBe("cache");
		options.DefaultCacheDuration.ShouldBe(TimeSpan.FromHours(1));
		options.MaxCacheSizeInBytes.ShouldBe(200 * 1024 * 1024);
		options.UseCompression.ShouldBeFalse();
		options.SuppressLogs.ShouldBeFalse();
	}
}
