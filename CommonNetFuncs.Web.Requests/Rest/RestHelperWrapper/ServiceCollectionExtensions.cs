using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Extension methods for configuring REST client services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the REST client factory to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddRestClientFactory(this IServiceCollection services)
	{
		services.TryAddSingleton<IRestClientFactory, RestClientFactory>();
		return services;
	}

	/// <summary>
	/// Adds the REST client factory to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddRestHelpersWrapper(this IServiceCollection services)
	{
		services.TryAddSingleton<RestHelpersWrapper>();
		return services;
	}

	/// <summary>
	/// Adds the REST client factory and configures a named HttpClient for the specified API.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="apiName">The name of the API.</param>
	/// <returns>An IHttpClientBuilder for further configuration.</returns>
	public static IHttpClientBuilder AddRestClient(this IServiceCollection services, string apiName)
	{
		services.AddRestClientFactory();
		services.AddRestHelpersWrapper();
		return services.AddHttpClient(apiName);
	}

	/// <summary>
	/// Adds the REST client factory and configures a named HttpClient for the specified API.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="apiName">The name of the API.</param>
	/// <param name="configureClient">Action to configure the HttpClient.</param>
	/// <returns>An IHttpClientBuilder for further configuration.</returns>
	public static IHttpClientBuilder AddRestClient(this IServiceCollection services, string apiName, Action<HttpClient> configureClient)
	{
		services.AddRestClientFactory();
		services.AddRestHelpersWrapper();
		return services.AddHttpClient(apiName, configureClient);
	}

	/// <summary>
	/// Adds the REST client factory and configures a named HttpClient for the specified API.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="apiName">The name of the API.</param>
	/// <param name="configureClient">Action to configure the HttpClient with service provider.</param>
	/// <returns>An IHttpClientBuilder for further configuration.</returns>
	public static IHttpClientBuilder AddRestClient(this IServiceCollection services, string apiName, Action<IServiceProvider, HttpClient> configureClient)
	{
		services.AddRestClientFactory();
		services.AddRestHelpersWrapper();
		return services.AddHttpClient(apiName, configureClient);
	}
}
