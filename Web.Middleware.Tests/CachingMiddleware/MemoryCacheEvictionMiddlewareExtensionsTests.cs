using CommonNetFuncs.Web.Common.CachingSupportClasses;
using CommonNetFuncs.Web.Middleware.CachingMiddleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using xRetry.v3;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class MemoryCacheEvictionMiddlewareExtensionsTests
{
	private readonly IFixture fixture;

	public MemoryCacheEvictionMiddlewareExtensionsTests()
	{
		fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
	}

	[RetryFact(3)]
	public void MemoryValueCaching_ShouldRegisterIMemoryCache()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		services.MemoryValueCaching();

		// Assert
		services.ShouldContain(x => x.ServiceType == typeof(IMemoryCache));
	}

	[RetryFact(3)]
	public void MemoryValueCaching_ShouldRegisterCacheTracker()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		services.MemoryValueCaching();

		// Assert
		ServiceDescriptor? trackerDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(CacheTracker));
		trackerDescriptor.ShouldNotBeNull();
		trackerDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
	}

	[RetryFact(3)]
	public void MemoryValueCaching_ShouldRegisterCacheMetrics()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		services.MemoryValueCaching();

		// Assert
		ServiceDescriptor? metricsDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(CacheMetrics));
		metricsDescriptor.ShouldNotBeNull();
		metricsDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
	}

	[RetryFact(3)]
	public void MemoryValueCaching_ShouldReturnServiceCollection()
	{
		// Arrange
		ServiceCollection services = new();

		// Act
		IServiceCollection result = services.MemoryValueCaching();

		// Assert
		result.ShouldBe(services);
	}

	[RetryFact(3)]
	public void MemoryValueCaching_WhenIMemoryCacheAlreadyRegistered_ShouldNotDuplicateRegistration()
	{
		// Arrange
		ServiceCollection services = new();
		services.AddMemoryCache();
		int initialMemoryCacheCount = services.Count(x => x.ServiceType == typeof(IMemoryCache));

		// Act
		services.MemoryValueCaching();

		// Assert
		int finalMemoryCacheCount = services.Count(x => x.ServiceType == typeof(IMemoryCache));
		finalMemoryCacheCount.ShouldBe(initialMemoryCacheCount);
	}

	[RetryFact(3)]
	public void UseMemoryValueCaching_WithNoOptions_ShouldUseDefaultOptions()
	{
		// Arrange
		ServiceCollection services = new();
		services.MemoryValueCaching();
		ServiceProvider serviceProvider = services.BuildServiceProvider();

		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.ApplicationServices).Returns(serviceProvider);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseMemoryValueCaching();

		// Assert
		result.ShouldBe(builder);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).MustHaveHappened();
	}

	[RetryFact(3)]
	public void UseMemoryValueCaching_WithCustomOptions_ShouldUseProvidedOptions()
	{
		// Arrange
		ServiceCollection services = new();
		services.MemoryValueCaching();
		ServiceProvider serviceProvider = services.BuildServiceProvider();

		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.ApplicationServices).Returns(serviceProvider);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		CacheOptions options = new()
		{
			MaxCacheSizeInBytes = 1024 * 1024,
			UseCacheQueryParam = "customCache"
		};

		// Act
		IApplicationBuilder result = builder.UseMemoryValueCaching(options);

		// Assert
		result.ShouldBe(builder);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).MustHaveHappened();
	}

	[RetryFact(3)]
	public void UseMemoryValueCaching_WithTrackMetricsTrue_ShouldResolveMetrics()
	{
		// Arrange
		ServiceCollection services = new();
		services.MemoryValueCaching();
		ServiceProvider serviceProvider = services.BuildServiceProvider();

		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.ApplicationServices).Returns(serviceProvider);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseMemoryValueCaching(trackMetrics: true);

		// Assert
		result.ShouldBe(builder);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).MustHaveHappened();
	}

	[RetryFact(3)]
	public void UseMemoryValueCaching_WithTrackMetricsFalse_ShouldNotRequireMetrics()
	{
		// Arrange
		ServiceCollection services = new();
		services.AddMemoryCache();
		services.AddSingleton<CacheTracker>();
		ServiceProvider serviceProvider = services.BuildServiceProvider();

		IApplicationBuilder builder = A.Fake<IApplicationBuilder>();
		A.CallTo(() => builder.ApplicationServices).Returns(serviceProvider);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).Returns(builder);

		// Act
		IApplicationBuilder result = builder.UseMemoryValueCaching(trackMetrics: false);

		// Assert
		result.ShouldBe(builder);
		A.CallTo(() => builder.Use(A<Func<RequestDelegate, RequestDelegate>>._)).MustHaveHappened();
	}
}
