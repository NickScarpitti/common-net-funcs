namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Default implementation of IRestClientFactory that wraps IHttpClientFactory.
/// </summary>
public sealed class RestClientFactory(IHttpClientFactory httpClientFactory) : IRestClientFactory
{
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

	/// <inheritdoc/>
	public IRestClient CreateClient(string apiName)
	{
		HttpClient httpClient = _httpClientFactory.CreateClient(apiName);
		return new RestClient(httpClient);
	}
}
