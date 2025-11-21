namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Implementation of IRestClient that wraps the static extension methods from RestHelpersStatic.
/// This allows the extension methods to be mockable for testing purposes.
/// </summary>
public sealed class RestClient : IRestClient
{
	private readonly HttpClient _httpClient;

	public RestClient(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	/// <inheritdoc/>
	public Uri? BaseAddress => _httpClient.BaseAddress;

	/// <inheritdoc/>
	public Task<RestObject<T>> RestObjectRequest<T, UT>(RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		return _httpClient.RestObjectRequest<T, UT>(requestOptions, cancellationToken);
	}

	/// <inheritdoc/>
	public Task<StreamingRestObject<T>> StreamingRestObjectRequest<T, UT>(RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		return _httpClient.StreamingRestObjectRequest<T, UT>(requestOptions, cancellationToken);
	}
}
