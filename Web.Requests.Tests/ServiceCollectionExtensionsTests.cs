using CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Requests.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions to verify dependency injection configuration
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
	#region AddRestClientFactory Tests

	[Fact]
	public void AddRestClientFactory_ShouldRegisterFactory_AsSingleton()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IServiceCollection result = services.AddRestClientFactory();

		// Assert
		result.ShouldBe(services); // Should return same instance for chaining

		ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRestClientFactory));
		descriptor.ShouldNotBeNull();
		descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
		descriptor.ImplementationType.ShouldBe(typeof(RestClientFactory));
	}

	[Fact]
	public void AddRestClientFactory_ShouldNotRegisterDuplicate_WhenCalledMultipleTimes()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		services.AddRestClientFactory();
		services.AddRestClientFactory();

		// Assert
		int count = services.Count(d => d.ServiceType == typeof(IRestClientFactory));
		count.ShouldBe(1); // TryAddSingleton should prevent duplicates
	}

	[Fact]
	public void AddRestClientFactory_ShouldResolveFactory_FromServiceProvider()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		services.AddHttpClient(); // Required for RestClientFactory
		services.AddRestClientFactory();

		// Act
		ServiceProvider provider = services.BuildServiceProvider();
		IRestClientFactory? factory = provider.GetService<IRestClientFactory>();

		// Assert
		factory.ShouldNotBeNull();
		factory.ShouldBeOfType<RestClientFactory>();
	}

	#endregion

	#region AddRestHelpersWrapper Tests

	[Fact]
	public void AddRestHelpersWrapper_ShouldRegisterWrapper_AsSingleton()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IServiceCollection result = services.AddRestHelpersWrapper();

		// Assert
		result.ShouldBe(services); // Should return same instance for chaining

		ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(RestHelpersWrapper));
		descriptor.ShouldNotBeNull();
		descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
		descriptor.ImplementationType.ShouldBe(typeof(RestHelpersWrapper));
	}

	[Fact]
	public void AddRestHelpersWrapper_ShouldNotRegisterDuplicate_WhenCalledMultipleTimes()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		services.AddRestHelpersWrapper();
		services.AddRestHelpersWrapper();

		// Assert
		int count = services.Count(d => d.ServiceType == typeof(RestHelpersWrapper));
		count.ShouldBe(1); // TryAddSingleton should prevent duplicates
	}

	[Fact]
	public void AddRestHelpersWrapper_ShouldResolveWrapper_FromServiceProvider()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		services.AddHttpClient(); // Required for dependencies
		services.AddRestClientFactory(); // RestHelpersWrapper depends on IRestClientFactory
		services.AddRestHelpersWrapper();

		// Act
		ServiceProvider provider = services.BuildServiceProvider();
		RestHelpersWrapper? wrapper = provider.GetService<RestHelpersWrapper>();

		// Assert
		wrapper.ShouldNotBeNull();
		wrapper.ShouldBeOfType<RestHelpersWrapper>();
	}

	#endregion

	#region AddRestClient(apiName) Tests

	[Fact]
	public void AddRestClient_WithApiName_ShouldRegisterFactoryAndWrapper()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi");

		// Assert
		builder.ShouldNotBeNull();

		// Verify factory is registered
		services.Any(d => d.ServiceType == typeof(IRestClientFactory)).ShouldBeTrue();

		// Verify wrapper is registered
		services.Any(d => d.ServiceType == typeof(RestHelpersWrapper)).ShouldBeTrue();

		// Verify HttpClient is configured
		services.Any(d => d.ServiceType == typeof(IHttpClientFactory)).ShouldBeTrue();
	}

	[Fact]
	public void AddRestClient_WithApiName_ShouldReturnHttpClientBuilder()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi");

		// Assert
		builder.ShouldNotBeNull();
		builder.ShouldBeAssignableTo<IHttpClientBuilder>();
	}

	[Fact]
	public void AddRestClient_WithApiName_ShouldAllowFurtherConfiguration()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi");
		builder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
		HttpClient client = factory.CreateClient("TestApi");

		// Assert
		client.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
	}

	[Fact]
	public void AddRestClient_WithApiName_ShouldNotRegisterDuplicates_WhenCalledMultipleTimes()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		services.AddRestClient("TestApi1");
		services.AddRestClient("TestApi2");

		// Assert
		int factoryCount = services.Count(d => d.ServiceType == typeof(IRestClientFactory));
		int wrapperCount = services.Count(d => d.ServiceType == typeof(RestHelpersWrapper));

		factoryCount.ShouldBe(1); // TryAddSingleton prevents duplicates
		wrapperCount.ShouldBe(1); // TryAddSingleton prevents duplicates
	}

	#endregion

	#region AddRestClient(apiName, configureClient) Tests

	[Fact]
	public void AddRestClient_WithConfiguration_ShouldRegisterFactoryAndWrapper()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi", client =>
		{
			client.BaseAddress = new Uri("https://api.example.com");
		});

		// Assert
		builder.ShouldNotBeNull();
		services.Any(d => d.ServiceType == typeof(IRestClientFactory)).ShouldBeTrue();
		services.Any(d => d.ServiceType == typeof(RestHelpersWrapper)).ShouldBeTrue();
	}

	[Fact]
	public void AddRestClient_WithConfiguration_ShouldApplyConfiguration()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		Uri expectedBaseAddress = new("https://api.example.com");

		// Act
		services.AddRestClient("TestApi", client =>
		{
			client.BaseAddress = expectedBaseAddress;
			client.Timeout = TimeSpan.FromSeconds(60);
		});

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
		HttpClient client = factory.CreateClient("TestApi");

		// Assert
		client.BaseAddress.ShouldBe(expectedBaseAddress);
		client.Timeout.ShouldBe(TimeSpan.FromSeconds(60));
	}

	[Fact]
	public void AddRestClient_WithConfiguration_ShouldReturnHttpClientBuilder()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi", client => { });

		// Assert
		builder.ShouldNotBeNull();
		builder.ShouldBeAssignableTo<IHttpClientBuilder>();
	}

	[Fact]
	public void AddRestClient_WithConfiguration_ShouldAllowChainingWithBuilder()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		bool messageHandlerAdded = false;

		// Act
		services.AddRestClient("TestApi", client =>
		{
			client.BaseAddress = new Uri("https://api.example.com");
		})
		.ConfigurePrimaryHttpMessageHandler(() =>
		{
			messageHandlerAdded = true;
			return new HttpClientHandler();
		});

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
		HttpClient _ = factory.CreateClient("TestApi");

		// Assert
		messageHandlerAdded.ShouldBeTrue();
	}

	#endregion

	#region AddRestClient(apiName, configureClient with IServiceProvider) Tests

	[Fact]
	public void AddRestClient_WithServiceProviderConfiguration_ShouldRegisterFactoryAndWrapper()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi", (provider, client) =>
		{
			client.BaseAddress = new Uri("https://api.example.com");
		});

		// Assert
		builder.ShouldNotBeNull();
		services.Any(d => d.ServiceType == typeof(IRestClientFactory)).ShouldBeTrue();
		services.Any(d => d.ServiceType == typeof(RestHelpersWrapper)).ShouldBeTrue();
	}

	[Fact]
	public void AddRestClient_WithServiceProviderConfiguration_ShouldApplyConfiguration()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		Uri expectedBaseAddress = new("https://api.example.com");

		// Act
		services.AddRestClient("TestApi", (provider, client) =>
		{
			client.BaseAddress = expectedBaseAddress;
			client.Timeout = TimeSpan.FromSeconds(90);
		});

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
		HttpClient client = factory.CreateClient("TestApi");

		// Assert
		client.BaseAddress.ShouldBe(expectedBaseAddress);
		client.Timeout.ShouldBe(TimeSpan.FromSeconds(90));
	}

	[Fact]
	public void AddRestClient_WithServiceProviderConfiguration_ShouldProvideServiceProvider()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		services.AddSingleton<TestConfigService>();
		bool serviceProviderReceived = false;
		TestConfigService? configService = null;

		// Act
		services.AddRestClient("TestApi", (provider, client) =>
		{
			serviceProviderReceived = provider != null;
			configService = provider?.GetService<TestConfigService>();
			client.BaseAddress = new Uri(configService?.BaseUrl ?? "https://default.com");
		});

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
		HttpClient client = factory.CreateClient("TestApi");

		// Assert
		serviceProviderReceived.ShouldBeTrue();
		configService.ShouldNotBeNull();
		client.BaseAddress!.ToString().ShouldBe("https://test-config.com/");
	}

	[Fact]
	public void AddRestClient_WithServiceProviderConfiguration_ShouldReturnHttpClientBuilder()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		IHttpClientBuilder builder = services.AddRestClient("TestApi", (provider, client) => { });

		// Assert
		builder.ShouldNotBeNull();
		builder.ShouldBeAssignableTo<IHttpClientBuilder>();
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void ServiceCollectionExtensions_ShouldSupportMultipleNamedClients()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();

		// Act
		services.AddRestClient("Api1", client => client.BaseAddress = new Uri("https://api1.example.com"));
		services.AddRestClient("Api2", client => client.BaseAddress = new Uri("https://api2.example.com"));
		services.AddRestClient("Api3", client => client.BaseAddress = new Uri("https://api3.example.com"));

		ServiceProvider provider = services.BuildServiceProvider();
		IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

		HttpClient client1 = factory.CreateClient("Api1");
		HttpClient client2 = factory.CreateClient("Api2");
		HttpClient client3 = factory.CreateClient("Api3");

		// Assert
		client1.BaseAddress!.ToString().ShouldBe("https://api1.example.com/");
		client2.BaseAddress!.ToString().ShouldBe("https://api2.example.com/");
		client3.BaseAddress!.ToString().ShouldBe("https://api3.example.com/");

		// Verify only one instance of factory and wrapper
		int factoryCount = services.Count(d => d.ServiceType == typeof(IRestClientFactory));
		int wrapperCount = services.Count(d => d.ServiceType == typeof(RestHelpersWrapper));
		factoryCount.ShouldBe(1);
		wrapperCount.ShouldBe(1);
	}

	[Fact]
	public void ServiceCollectionExtensions_ShouldAllowResolvingAllServices()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		services.AddRestClient("TestApi");

		// Act
		ServiceProvider provider = services.BuildServiceProvider();

		IRestClientFactory? factory = provider.GetService<IRestClientFactory>();
		RestHelpersWrapper? wrapper = provider.GetService<RestHelpersWrapper>();
		IHttpClientFactory? httpFactory = provider.GetService<IHttpClientFactory>();

		// Assert
		factory.ShouldNotBeNull();
		wrapper.ShouldNotBeNull();
		httpFactory.ShouldNotBeNull();
	}

	[Fact]
	public void ServiceCollectionExtensions_ShouldUseSameInstanceForSingletons()
	{
		// Arrange
		IServiceCollection services = new ServiceCollection();
		services.AddRestClient("TestApi");

		// Act
		ServiceProvider provider = services.BuildServiceProvider();

		IRestClientFactory? factory1 = provider.GetService<IRestClientFactory>();
		IRestClientFactory? factory2 = provider.GetService<IRestClientFactory>();

		RestHelpersWrapper? wrapper1 = provider.GetService<RestHelpersWrapper>();
		RestHelpersWrapper? wrapper2 = provider.GetService<RestHelpersWrapper>();

		// Assert
		ReferenceEquals(factory1, factory2).ShouldBeTrue(); // Singleton should return same instance
		ReferenceEquals(wrapper1, wrapper2).ShouldBeTrue(); // Singleton should return same instance
	}

	#endregion

	#region Helper Classes

	private sealed class TestConfigService
	{
#pragma warning disable S2325 // Make 'BaseUrl' a static property
#pragma warning disable CA1822 // Mark members as static
		public string BaseUrl => "https://test-config.com";
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore S2325 // Make 'BaseUrl' a static property
	}

	#endregion
}
