namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Implementation of IRestClient that wraps the static extension methods from RestHelpersStatic.
/// This allows the extension methods to be mockable for testing purposes.
/// </summary>
public sealed class RestClient(HttpClient httpClient) : IRestClient
{
	private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

	/// <inheritdoc/>
	public Uri? BaseAddress => httpClient.BaseAddress;

	/// <inheritdoc/>
	public Task<RestObject<TResponse>> RestObjectRequest<TResponse, TBody>(RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		return httpClient.RestObjectRequest<TResponse, TBody>(requestOptions, cancellationToken);
	}

	/// <inheritdoc/>
	public Task<StreamingRestObject<TResponse>> StreamingRestObjectRequest<TResponse, TBody>(RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		return httpClient.StreamingRestObjectRequest<TResponse, TBody>(requestOptions, cancellationToken);
	}
}
