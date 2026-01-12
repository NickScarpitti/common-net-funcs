namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

/// <summary>
/// Interface for REST client operations, enabling testability by wrapping static extension methods.
/// </summary>
public interface IRestClient
{
	/// <summary>
	/// Gets the base address of the REST client.
	/// </summary>
	Uri? BaseAddress { get; }

	/// <summary>
	/// Executes a REST request and returns both the result and response.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>RestObject containing the result and HTTP response.</returns>
	Task<RestObject<TResponse>> RestObjectRequest<TResponse, TBody>(RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes a REST request and returns the response as an asynchronous stream.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>StreamingRestObject containing the async enumerable result and HTTP response.</returns>
	Task<StreamingRestObject<TResponse>> StreamingRestObjectRequest<TResponse, TBody>(RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default);
}
