using System.Runtime.CompilerServices;
using System.Text;
using CommonNetFuncs.Web.Requests.Rest.Options;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.ObjectPool;
using NLog;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper.WrapperHelpers;
using static Newtonsoft.Json.JsonConvert;

namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

public sealed class RestHelpersWrapper(IRestClientFactory restClientFactory)
{
	//private readonly ILogger<RestHelpersWrapper> logger = logger;
	private readonly Logger logger = LogManager.GetCurrentClassLogger();

	private static readonly ObjectPool<Dictionary<string, string>> headerPool = new DefaultObjectPool<Dictionary<string, string>>(new DefaultPooledObjectPolicy<Dictionary<string, string>>());

	#region GET Methods

	/// <summary>
	/// Sends a GET request to the specified URL and returns the response deserialized into the specified type.
	/// </summary>
	/// <typeparam name="TResponse">The object type to be returned by the request.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The deserialized response object, or <see cref="null"/> if the request failed.</returns>
	public async Task<TResponse?> Get<TResponse>(RestHelperOptions options, CancellationToken cancellationToken = default)
	{
		RestObject<TResponse>? result = null;
		int attempts = 0;
		string? bearerToken = null;

		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("GET {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TResponse> baseRequestOptions = GetRequestOptions<TResponse>(options, client.BaseAddress, headers, HttpMethod.Get, bearerToken, default);

				result = await client.RestObjectRequest<TResponse, TResponse>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions!.MaxRetry)
				{
					logger.Warn("GET {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during GET request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during GET request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result == null ? default : result.Result;
	}

	/// <summary>
	/// Sends a GET request to the specified URL and returns the response as an asynchronous stream of the specified type.
	/// </summary>
	/// <typeparam name="TResponse">The object type to be returned by the request.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The deserialized response object, or <see cref="null"/> if the request failed.</returns>
	public async IAsyncEnumerable<TResponse?> GetStreaming<TResponse>(RestHelperOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		UpdateStreamingHeaders(options); // Ensure application/json is used for streaming

		StreamingRestObject<TResponse>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("GET (Streaming) {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TResponse> baseRequestOptions = GetRequestOptions<TResponse>(options, client.BaseAddress, headers, HttpMethod.Get, bearerToken, default);

				result = await client.StreamingRestObjectRequest<TResponse, TResponse>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("GET (Streaming) {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during GET Streaming request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during GET Streaming request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		if (result?.Result != null)
		{
			await foreach (TResponse? item in result.Result.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return item;
			}
		}
		else
		{
			yield break;
		}
	}

	#endregion

	#region POST Methods

	/// <summary>
	/// Sends a POST request to the specified URL with the provided object and returns the response deserialized into the specified type.
	/// </summary>
	/// <typeparam name="TBody">The object type to populate the request body with and / or be returned by the request.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="postObject">The object to be sent in the request body.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The deserialized response object, or <see cref="null"/> if the request failed.</returns>
	public async Task<TBody?> PostRequest<TBody>(RestHelperOptions options, TBody postObject, CancellationToken cancellationToken = default)
	{
		RestObject<TBody>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("POST {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TBody> baseRequestOptions = GetRequestOptions<TBody>(options, client.BaseAddress, headers, HttpMethod.Post, bearerToken, postObject);

				result = await client.RestObjectRequest<TBody, TBody>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("POST {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during POST request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during POST request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result == null ? default : result.Result;
	}

	/// <summary>
	/// Sends a POST request to the specified URL with the provided object and returns the response as an asynchronous stream of the specified type.
	/// </summary>
	/// <typeparam name="TBody">The object type to populate the request body with and / or be returned by the request stream.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="postObject">The object to be sent in the request body.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>An asynchronous stream of the response objects.</returns>
	public async IAsyncEnumerable<TBody?> PostRequestStreaming<TBody>(RestHelperOptions options, TBody postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		UpdateStreamingHeaders(options); // Ensure application/json is used for streaming

		StreamingRestObject<TBody>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("POST (Streaming) {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TBody> baseRequestOptions = GetRequestOptions<TBody>(options, client.BaseAddress, headers, HttpMethod.Post, bearerToken, postObject);

				result = await client.StreamingRestObjectRequest<TBody, TBody>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("POST (Streaming) {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during POST Streaming request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during POST Streaming request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		if (result?.Result != null)
		{
			await foreach (TBody? item in result.Result.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return item;
			}
		}
		else
		{
			yield break;
		}
	}

	/// <summary>
	/// Sends a POST request to the specified URL with the provided object and returns the response deserialized into the specified type.
	/// </summary>
	/// <typeparam name="TResponse">The object type to populate the request body with.</typeparam>
	/// <typeparam name="TBody">The object type to be returned in the response.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="postObject">The object to be sent in the request body.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The deserialized response object, or <see cref="null"/> if the request failed.</returns>
	public async Task<TResponse?> GenericPostRequest<TResponse, TBody>(RestHelperOptions options, TBody postObject, CancellationToken cancellationToken = default)
	{
		RestObject<TResponse>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("POST (Generic) {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TBody> baseRequestOptions = GetRequestOptions(options, client.BaseAddress, headers, HttpMethod.Post, bearerToken, postObject);

				result = await client.RestObjectRequest<TResponse, TBody>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("POST (Generic) {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during generic POST request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during generic POST request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result == null ? default : result.Result;
	}

	/// <summary>
	/// Sends a POST request to the specified URL with the provided object and returns the response as an asynchronous stream of the specified type.
	/// </summary>
	/// <typeparam name="TResponse">The object type to populate the request body with.</typeparam>
	/// <typeparam name="TBody">The object type to be returned in the stream.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="postObject">The object to be sent in the request body.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>An asynchronous stream of the response objects.</returns>
	public async IAsyncEnumerable<TResponse?> GenericPostRequestStreaming<TResponse, TBody>(RestHelperOptions options, TBody postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		UpdateStreamingHeaders(options); // Ensure application/json is used for streaming

		StreamingRestObject<TResponse>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("POST (Generic Streaming) {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TBody> baseRequestOptions = GetRequestOptions<TBody>(options, client.BaseAddress, headers, HttpMethod.Post, bearerToken, postObject);

				result = await client.StreamingRestObjectRequest<TResponse, TBody>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("POST (Generic Streaming) {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during generic POST streaming request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during generic POST streaming request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		if (result?.Result != null)
		{
			await foreach (TResponse? item in result.Result.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return item;
			}
		}
		else
		{
			yield break;
		}
	}

	/// <summary>
	/// Sends a POST request to the specified URL with the provided object and returns the response as a string.
	/// </summary>
	/// <typeparam name="TBody">The object type to populate the request body with.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="postObject">The object to be sent in the request body.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The response body as a string, or <see cref="null"/> if the request failed.</returns>
	public async Task<string?> StringPostRequest<TBody>(RestHelperOptions options, TBody postObject, CancellationToken cancellationToken = default)
	{
		RestObject<string?>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("POST (String) {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TBody> baseRequestOptions = GetRequestOptions<TBody>(options, client.BaseAddress, headers, HttpMethod.Post, bearerToken, postObject);

				result = await client.RestObjectRequest<string?, TBody>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("POST (String) {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during string POST request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during string POST request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result?.Result;
	}

	#endregion

	#region Update Methods

	/// <summary>
	/// Sends a PATCH request to the specified URL with the provided model, comparing it to the old model to create a JSON Patch document.
	/// </summary>
	/// <typeparam name="TEntity">Type of the object to be patched.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="model">The updated model to be sent in the request.</param>
	/// <param name="oldModel">The original model to compare against.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The returned value from the reques or <see cref="null"/> if the reuqest failed.</returns>
	public async Task<TEntity?> PatchRequest<TEntity>(RestHelperOptions options, TEntity model, TEntity oldModel, CancellationToken cancellationToken = default) where TEntity : class
	{
		JsonPatchDocument patchDocument = PatchCreator.CreatePatch(oldModel, model);

		if (patchDocument.Operations.Count == 0)
		{
			logger.Debug("No changes detected in the model; skipping PATCH request.");
			return model; // No changes detected, return the original model
		}

		RestObject<TEntity>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("PATCH {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TEntity> baseRequestOptions = GetRequestOptions<TEntity>(options, client.BaseAddress, headers, HttpMethod.Patch, bearerToken, default,
								patchDocument: new StringContent(SerializeObject(patchDocument), Encoding.UTF8, Json)); //new StringContent(System.Text.Json.JsonSerializer.Serialize(patchDocument), Encoding.UTF8, Json)); // System.Text.Json has issues producing JsonPatchDocument in the correct format

				result = await client.RestObjectRequest<TEntity, TEntity>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("PATCH {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during PATCH request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during PATCH request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result?.Result;
	}

	/// <summary>
	/// Sends a PUT request to the specified URL with the provided replacement model and returns the response deserialized into the specified type.
	/// </summary>
	/// <typeparam name="TEntity">Type of the object to be replaced and returned.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="replacementModel">The model to replace the existing resource with.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The returned value from the reques or <see cref="null"/> if the reuqest failed.</returns>
	public async Task<TEntity?> PutRequest<TEntity>(RestHelperOptions options, TEntity replacementModel, CancellationToken cancellationToken = default) where TEntity : class
	{
		RestObject<TEntity>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("PUT {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TEntity> baseRequestOptions = GetRequestOptions<TEntity>(options, client.BaseAddress, headers, HttpMethod.Put, bearerToken, replacementModel);

				result = await client.RestObjectRequest<TEntity, TEntity>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("PUT {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during PUT request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during PUT request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}
		return result?.Result;
	}

	/// <summary>
	/// Sends a DELETE request to the specified URL and returns the response deserialized into the specified type.
	/// </summary>
	/// <typeparam name="TEntity">Type of the expected return value.</typeparam>
	/// <param name="options">Options specifying the request details.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The returned value from the reques or <see cref="null"/> if the reuqest failed.</returns>
	public async Task<TEntity?> DeleteRequest<TEntity>(RestHelperOptions options, CancellationToken cancellationToken = default) where TEntity : class
	{
		RestObject<TEntity>? result = null;
		int attempts = 0;
		string? bearerToken = null;
		HttpResponseMessage? lastResponse = null;
		IRestClient client = restClientFactory.CreateClient(options.ApiName);
		options.ResilienceOptions ??= new();

		Dictionary<string, string> headers = headerPool.Get();
		try
		{
			headers.Clear();
			headers = GetHeaders(options, false);
			while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions.MaxRetry)
			{
				if (attempts > 0)
				{
					logger.Info("DELETE {Url} Attempt {AttemptNumber}", options.Url, attempts + 1);
				}

				if (options.UseBearerToken)
				{
					bearerToken = await PopulateBearerToken(options, attempts, lastResponse, bearerToken).ConfigureAwait(false);
				}

				RequestOptions<TEntity> baseRequestOptions = GetRequestOptions<TEntity>(options, client.BaseAddress, headers, HttpMethod.Delete, bearerToken, default);

				result = await client.RestObjectRequest<TEntity, TEntity>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

				if (!ShouldRetry(result.Response, options.ResilienceOptions))
				{
					break;
				}

				attempts++;
				lastResponse = result.Response;

				if (attempts >= options.ResilienceOptions.MaxRetry)
				{
					logger.Warn("DELETE {Url} still failing after max allowed attempts ({MaxRetry}).", options.Url, options.ResilienceOptions.MaxRetry);
					break;
				}
				await Task.Delay(GetWaitTime(options.ResilienceOptions, attempts), cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "Exception occurred during DELETE request to {Url}: {Message}", options.Url, ex.Message);
			throw new HttpRequestException("Error occurred during DELETE request", ex);
		}
		finally
		{
			headerPool.Return(headers);
		}

		return result?.Result;
	}

	#endregion
}
