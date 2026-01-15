using System.Collections.Frozen;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryPack;
using MessagePack;
using Newtonsoft.Json;

using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Common.EncodingTypes;
using static CommonNetFuncs.Web.Common.UriHelpers;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace CommonNetFuncs.Web.Requests.Rest;

public static class RestHelpersStatic
{
	private static readonly FrozenSet<HttpMethod> requestsWithBody = [HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch];
	private const double DefaultRequestTimeout = 100;

	//public static JsonSerializerOptions? JsonSerializerOptions { get; set; }
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	public static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles, PropertyNameCaseInsensitive = true };

	private const string RestErrorLocationTemplate = "{ErrorLocation} Error URL: {Url}";

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Object of type <typeparamref name="TResponse"/> resulting from the request - Null if not success.</returns>
	public static async Task<TResponse?> RestRequest<TResponse, TBody>(this HttpClient client, RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		TResponse? result = default;
		try
		{
			await LogRequest(requestOptions, cancellationToken).ConfigureAwait(false);

			using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			combinedTokenSource.CancelAfter(GetTimeout(requestOptions));
			//using CancellationTokenSource tokenSource = new(GetTimeout(requestOptions));

			using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);

			httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
			httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

			//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
			using HttpResponseMessage response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
			result = await HandleResponse<TResponse, TBody>(response, requestOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Info("Task was expectedly canceled for {HttpMethod} request to {Url}", requestOptions.HttpMethod.ToString().ToUpper(), requestOptions.Url);
			}
			else
			{
				logger.Error(tcex, RestErrorLocationTemplate, exceptionLocation, requestOptions.Url);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, RestErrorLocationTemplate, ex.GetLocationOfException(), requestOptions.Url);
		}
		return result;
	}

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="TResponse"/> resulting from the request - Null if not success.</returns>
	public static async IAsyncEnumerable<TResponse?> StreamingRestRequest<TResponse, TBody>(this HttpClient client, RequestOptions<TBody> requestOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerator<TResponse?>? enumeratedReader = null;
		HttpResponseMessage? response = null;
		try
		{
			try
			{
				await LogRequest(requestOptions, cancellationToken).ConfigureAwait(false);

				using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				combinedTokenSource.CancelAfter(GetTimeout(requestOptions));

				using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);

				//Ensure JSON header is being used
				if (requestOptions.HttpHeaders == null)
				{
					requestOptions.HttpHeaders = new Dictionary<string, string>([JsonAcceptHeader]);
				}
				else if (requestOptions.HttpHeaders.Remove(AcceptHeader))
				{
					requestOptions.HttpHeaders.AddDictionaryItem(JsonAcceptHeader);
				}

				httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
				httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

				//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
				response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
				enumeratedReader = HandleResponseAsync<TResponse, TBody>(response, requestOptions, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
			}
			catch (TaskCanceledException tcex)
			{
				string exceptionLocation = tcex.GetLocationOfException();
				if (requestOptions.ExpectTaskCancellation)
				{
					logger.Info("Task was expectedly canceled for {HttpMethod} request to {Url}", requestOptions.HttpMethod.ToString().ToUpper(), requestOptions.Url);
				}
				else
				{
					logger.Error(tcex, RestErrorLocationTemplate, exceptionLocation, requestOptions.Url);
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, RestErrorLocationTemplate, ex.GetLocationOfException(), requestOptions.Url);
			}

			if (enumeratedReader != null)
			{
				while (await enumeratedReader.MoveNextAsync().ConfigureAwait(false))
				{
					yield return enumeratedReader!.Current;
				}
			}
			else
			{
				yield break;
			}
		}
		finally
		{
			response?.Dispose();
			await (enumeratedReader?.DisposeAsync() ?? ValueTask.CompletedTask);
		}
	}

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Object of type <typeparamref name="TResponse"/> resulting from the request - Null if not success.</returns>
	public static async Task<RestObject<TResponse>> RestObjectRequest<TResponse, TBody>(this HttpClient client, RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		RestObject<TResponse> restObject = new();
		try
		{
			await LogRequest(requestOptions, cancellationToken).ConfigureAwait(false);

			using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			combinedTokenSource.CancelAfter(GetTimeout(requestOptions));
			//using CancellationTokenSource tokenSource = new(GetTimeout(requestOptions));

			using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);
			httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
			httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

			//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
			restObject.Response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
			restObject.Result = await HandleResponse<TResponse, TBody>(restObject.Response, requestOptions, cancellationToken).ConfigureAwait(false);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Warn("Run once REST task was canceled for {HttpMethod} request to {Url}", requestOptions.HttpMethod.ToString().ToUpper(), requestOptions.Url);
			}
			else
			{
				logger.Error(tcex, RestErrorLocationTemplate, exceptionLocation, requestOptions.Url);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, RestErrorLocationTemplate, ex.GetLocationOfException(), requestOptions.Url);
		}
		return restObject;
	}

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable.
	/// </summary>
	/// <typeparam name="TResponse">Type of return object.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="TResponse"/> resulting from the request - Null if not success.</returns>
	public static async Task<StreamingRestObject<TResponse>> StreamingRestObjectRequest<TResponse, TBody>(this HttpClient client, RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		StreamingRestObject<TResponse> restObject = new();
		try
		{
			await LogRequest(requestOptions, cancellationToken).ConfigureAwait(false);

			using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			combinedTokenSource.CancelAfter(GetTimeout(requestOptions));
			//using CancellationTokenSource tokenSource = new(GetTimeout(requestOptions));
			using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);

			//Ensure JSON header is being used
			if (requestOptions.HttpHeaders == null)
			{
				requestOptions.HttpHeaders = new Dictionary<string, string>([JsonAcceptHeader]);
			}
			else if (requestOptions.HttpHeaders.TryGetValue(AcceptHeader, out string? header) && header != Json && requestOptions.HttpHeaders.Remove(AcceptHeader))
			{
				requestOptions.HttpHeaders.AddDictionaryItem(JsonAcceptHeader);
			}

			httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
			httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

			//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
			restObject.Response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
			restObject.Result = HandleResponseAsync<TResponse, TBody>(restObject.Response, requestOptions, cancellationToken: cancellationToken);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Info("Task was expectedly canceled for {HttpMethod} request to {Url}", requestOptions.HttpMethod.ToString().ToUpper(), requestOptions.Url);
			}
			else
			{
				logger.Error(tcex, RestErrorLocationTemplate, exceptionLocation, requestOptions.Url);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, RestErrorLocationTemplate, ex.GetLocationOfException(), requestOptions.Url);
		}

		return restObject;
	}

	/// <summary>
	/// Checks if the HTTP request was successful and then parses the response if it is.
	/// </summary>
	/// <typeparam name="TResponse">Type of expected response content.</typeparam>
	/// <param name="response">Response message from the HTTP request.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Response content if HTTP request was successful</returns>
	internal static async Task<TResponse?> HandleResponse<TResponse, TBody>(this HttpResponseMessage response, RequestOptions<TBody> requestOptions, CancellationToken cancellationToken = default)
	{
		TResponse? result = default;
		try
		{
			string? contentType = response.Content.Headers.ContentType?.ToString();
			string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

			await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				result = await ReadResponseStream<TResponse>(responseStream, contentType, contentEncoding, requestOptions.UseNewtonsoftDeserializer, requestOptions.JsonSerializerOptions,
					requestOptions.MsgPackOptions, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				string? errorMessage = null;
				if (contentType.ContainsInvariant(JsonProblem))
				{
					ProblemDetailsWithErrors? problemDetails = await ReadResponseStream<ProblemDetailsWithErrors>(responseStream, contentType, contentEncoding, requestOptions.UseNewtonsoftDeserializer,
						requestOptions.JsonSerializerOptions, requestOptions.MsgPackOptions, cancellationToken).ConfigureAwait(false) ?? new();
					errorMessage = $"({problemDetails.Status}) {problemDetails.Title}\n\t\t{string.Join("\n\t\t", problemDetails.Errors.Select(x => $"{x.Key}:\n\t\t\t{string.Join("\n\t\t\t", x.Value)}"))}";
				}
				else
				{
					errorMessage = await ReadResponseStream<string>(responseStream, Text, contentEncoding, requestOptions.UseNewtonsoftDeserializer,
						requestOptions.JsonSerializerOptions, requestOptions.MsgPackOptions, cancellationToken).ConfigureAwait(false);
				}
				logger.Warn("{HttpMethod} request with URL {URL} failed with the following response:\n\t{StatusCode}: {ReasonPhrase}\n\tContent: {ErrorMessage}\n\t{Headers}",
					requestOptions.HttpMethod, requestOptions.LogQuery ? requestOptions.Url : requestOptions.RedactedUrl, response.StatusCode, response.ReasonPhrase, errorMessage,
					requestOptions.HttpHeaders != null ? $"Headers: {string.Join(", ", requestOptions.HttpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}

		await LogResponse(requestOptions, result, true, cancellationToken).ConfigureAwait(false);

		return result;
	}

	/// <summary>
	/// Checks if the HTTP request was successful and then parses the response if it is.
	/// </summary>
	/// <typeparam name="TResponse">Type of expected response content.</typeparam>
	/// <typeparam name="TBody">Type of object used in body (if any).</typeparam>
	/// <param name="response">Response message from the HTTP request.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Response content if HTTP request was successful</returns>
	internal static async IAsyncEnumerable<TResponse?> HandleResponseAsync<TResponse, TBody>(this HttpResponseMessage response, RequestOptions<TBody> requestOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerator<TResponse?>? enumeratedReader = null;
		Stream? responseStream = null;
		Stream? decompressedStream = null;
		try
		{
			try
			{
				string? contentType = response.Content.Headers.ContentType?.ToString();
				string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

				//response.Content.ReadFromJsonAsAsyncEnumerable<TBody>(cancellationToken: cancellationToken);

				responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					//enumeratedReader = responseStream.ReadResponseStreamAsync<TBody?>(contentType, contentEncoding, requestOptions.JsonSerializerOptions, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);

					// Apply decompression if needed
					Stream streamToRead = responseStream;
					if (contentEncoding.StrEq(GZip))
					{
						decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
						streamToRead = decompressedStream;
					}
					else if (contentEncoding.StrEq(Brotli))
					{
						decompressedStream = responseStream.Decompress(ECompressionType.Brotli);
						streamToRead = decompressedStream;
					}

					enumeratedReader = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<TResponse?>(streamToRead, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken)
						.GetAsyncEnumerator(cancellationToken);
				}
				else
				{
					string? errorMessage = null;
					if (contentType.ContainsInvariant(JsonProblem))
					{
						ProblemDetailsWithErrors? problemDetails = await ReadResponseStream<ProblemDetailsWithErrors>(responseStream, contentType, contentEncoding, false, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new();
						errorMessage = $"({problemDetails.Status}) {problemDetails.Title}\n\t\t{string.Join("\n\t\t", problemDetails.Errors.Select(x => $"{x.Key}:\n\t\t\t{string.Join("\n\t\t\t", x.Value)}"))}";
					}
					else
					{
						errorMessage = await ReadResponseStream<string>(responseStream, Text, contentEncoding, false, cancellationToken: cancellationToken).ConfigureAwait(false);
					}
					logger.Warn("{HttpMethod} request with URL {URL} failed with the following response:\n\t{StatusCode}: {ReasonPhrase}\n\tContent: {ErrorMessage}\n\t{Headers}",
						requestOptions.HttpMethod, requestOptions.LogQuery ? requestOptions.Url : requestOptions.RedactedUrl, response.StatusCode, response.ReasonPhrase, errorMessage,
						requestOptions.HttpHeaders != null ? $"Headers: {string.Join(", ", requestOptions.HttpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null);
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

			}

			if (requestOptions.LogResponse)
			{
				logger.Info("HTTP Response for {Method} @ {Url}:", requestOptions.HttpMethod.Method, requestOptions.LogQuery ? requestOptions.Url : requestOptions.RedactedUrl);
			}

			if (enumeratedReader != null)
			{
				while (await enumeratedReader.MoveNextAsync().ConfigureAwait(false))
				{
					cancellationToken.ThrowIfCancellationRequested();

					await LogResponse(requestOptions, enumeratedReader.Current, false, cancellationToken).ConfigureAwait(false);

					yield return enumeratedReader!.Current;
				}
			}
			else
			{
				if (requestOptions.LogResponse)
				{
					logger.Info("Empty Result");
				}
				yield break;
			}
		}
		finally
		{
			await (decompressedStream?.DisposeAsync() ?? ValueTask.CompletedTask);
			await (responseStream?.DisposeAsync() ?? ValueTask.CompletedTask);
			await (enumeratedReader?.DisposeAsync() ?? ValueTask.CompletedTask);
		}
	}

	/// <summary>
	/// Reads the response stream and deserializes it based on the content type and content encoding.
	/// </summary>
	/// <typeparam name="TResponse">Type of expected response content.</typeparam>
	/// <param name="responseStream">Stream containing the response data.</param>
	/// <param name="contentType">Content type of the response.</param>
	/// <param name="contentEncoding">Content encoding of the response.</param>
	/// <param name="useNewtonsoftDeserializer">Whether to use Newtonsoft.Json for deserialization.</param>
	/// <param name="jsonSerializerOptions">JSON serializer options.</param>
	/// <param name="msgPackOptions">MessagePack serializer options.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Deserialized response content.</returns>
	public static async Task<TResponse?> ReadResponseStream<TResponse>(this Stream responseStream, string? contentType, string? contentEncoding, bool useNewtonsoftDeserializer,
				JsonSerializerOptions? jsonSerializerOptions = null, MsgPackOptions? msgPackOptions = null, CancellationToken cancellationToken = default)
	{
		TResponse? result = default;
		try
		{
			if (responseStream.CanSeek && responseStream.Length <= 1)
			{
				return result; // Early exit for empty streams
			}

			//if (responseStream.Length <= 1)
			//{
			//	return result; // Early exit for empty streams
			//}

			if (contentType.StrEq(MsgPack)) //Message Pack uses native compression
			{
				if (msgPackOptions?.UseMsgPackCompression == true || msgPackOptions?.UseMsgPackUntrusted == true)
				{
					MessagePackSerializerOptions messagePackOptions = MessagePackSerializerOptions.Standard;
					if (msgPackOptions.UseMsgPackCompression)
					{
						messagePackOptions = messagePackOptions.WithCompression(MessagePackCompression.Lz4BlockArray);
					}

					if (msgPackOptions.UseMsgPackUntrusted)
					{
						messagePackOptions = messagePackOptions.WithSecurity(MessagePackSecurity.UntrustedData);
					}
					result = await MessagePackSerializer.DeserializeAsync<TResponse>(responseStream, messagePackOptions, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					result = await MessagePackSerializer.DeserializeAsync<TResponse>(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
				}
			}
			else if (contentType.StrEq(MemPack)) // ***Will fail if trying to deserialize null value, ensure NoContent is sent back for nulls***
			{
				if (contentEncoding.StrEq(GZip))
				{
					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
					result = await MemoryPackSerializer.DeserializeAsync<TResponse>(decompressedStream, cancellationToken: cancellationToken).ConfigureAwait(false);
				}
				else if (contentEncoding.StrEq(Brotli))
				{
					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Brotli);
					result = await MemoryPackSerializer.DeserializeAsync<TResponse>(decompressedStream, cancellationToken: cancellationToken).ConfigureAwait(false);
				}
				else
				{
					result = await MemoryPackSerializer.DeserializeAsync<TResponse>(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
				}
			}
			else if (contentType.ContainsInvariant("json"))//Assume JSON
			{
				//Deserialize as stream - More memory efficient than string deserialization
				//Stream streamToRead = responseStream;
				//Stream? decompressedStream = null;

				//try
				//{
				if (contentEncoding.StrEq(GZip))
				{
					//decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
					//streamToRead = decompressedStream;
					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
					result = useNewtonsoftDeserializer
						? await DeserializeWithNewtonsoft<TResponse>(decompressedStream)
						: await System.Text.Json.JsonSerializer.DeserializeAsync<TResponse>(decompressedStream, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
				}
				else if (contentEncoding.StrEq(Brotli))
				{
					//decompressedStream = responseStream.Decompress(ECompressionType.Brotli);
					//streamToRead = decompressedStream;

					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Brotli);
					result = useNewtonsoftDeserializer
						? await DeserializeWithNewtonsoft<TResponse>(decompressedStream)
						: await System.Text.Json.JsonSerializer.DeserializeAsync<TResponse>(decompressedStream, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
				}
				else if (!useNewtonsoftDeserializer)
				{
					result = await System.Text.Json.JsonSerializer.DeserializeAsync<TResponse>(responseStream, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					result = await DeserializeWithNewtonsoft<TResponse>(responseStream);
				}

				//if (useNewtonsoftDeserializer)
				//{
				//	using StreamReader streamReader = new(streamToRead);
				//	await using JsonTextReader jsonReader = new(streamReader);
				//	Newtonsoft.Json.JsonSerializer serializer = new();
				//	result = serializer.Deserialize<TBody>(jsonReader);
				//}
				//else
				//{
				//	result = await System.Text.Json.JsonSerializer.DeserializeAsync<TBody>(streamToRead, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
				//}
				//}
				//finally
				//{
				//	if (decompressedStream != null)
				//	{
				//		await decompressedStream.DisposeAsync().ConfigureAwait(false);
				//	}
				//}
			}
			else if (contentType.ContainsInvariant("text")) //String encoding (error usually)
			{
				if (contentEncoding.StrEq(GZip))
				{
					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
					using StreamReader reader = new(decompressedStream, Encoding.UTF8);
					string stringResult = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
					result = (TResponse)(object)stringResult;
				}
				else if (contentEncoding.StrEq(Brotli))
				{
					await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Brotli);
					using StreamReader reader = new(decompressedStream, Encoding.UTF8);
					string stringResult = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
					result = (TResponse)(object)stringResult;
				}
				else
				{
					using StreamReader reader = new(responseStream, Encoding.UTF8);
					string stringResult = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
					result = (TResponse)(object)stringResult;
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

		}
		return result;
	}

	private static async Task<TResponse?> DeserializeWithNewtonsoft<TResponse>(Stream decompressedStream)
	{
		using StreamReader streamReader = new(decompressedStream);
		await using JsonTextReader jsonReader = new(streamReader);
		Newtonsoft.Json.JsonSerializer serializer = new();
		return serializer.Deserialize<TResponse>(jsonReader);
	}

	/// <summary>
	/// Reads the response stream and asynchronously deserializes it based on the content type and content encoding.
	/// </summary>
	/// <typeparam name="TResponse">Type of the expected response content.</typeparam>
	/// <param name="responseStream">Stream containing the response data.</param>
	/// <param name="contentType">Content type of the response.</param>
	/// <param name="contentEncoding">Content encoding of the response.</param>
	/// <param name="jsonSerializerOptions">JSON serializer options.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Deserialized response content.</returns>
	/// <exception cref="NotImplementedException"></exception>
	public static async IAsyncEnumerable<TResponse?> ReadResponseStreamAsync<TResponse>(this Stream responseStream, string? contentType, string? contentEncoding, JsonSerializerOptions? jsonSerializerOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (contentType.ContainsInvariant("json"))
		{
			Stream streamToRead = responseStream;
			try
			{
				if (contentEncoding.StrEq(GZip))
				{
					streamToRead = responseStream.Decompress(ECompressionType.Gzip);
				}
				else if (contentEncoding.StrEq(Brotli))
				{
					streamToRead = responseStream.Decompress(ECompressionType.Brotli);
				}

				await foreach (TResponse? item in System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<TResponse?>(streamToRead, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false))
				{
#pragma warning disable S2955 // Generic parameters not constrained to reference types should not be compared to "null"
					if (item != null)
					{
						yield return item;
					}
#pragma warning restore S2955 // Generic parameters not constrained to reference types should not be compared to "null"
				}
			}
			finally
			{
				await streamToRead.DisposeAsync().ConfigureAwait(false);
			}
		}
		else
		{
			throw new NotImplementedException($"Content type {contentType.UrlEncodeReadable(cancellationToken: cancellationToken)} is not available");
		}
	}

	/// <summary>
	/// Adds content to HTTP request if not using GET HTTP request method.
	/// </summary>
	/// <typeparam name="TBody">Type of the post object being added to the HTTP request content.</typeparam>
	/// <param name="httpRequestMessage">HTTP request to add content to.</param>
	/// <param name="httpMethod">HTTP request method.</param>
	/// <param name="httpHeaders">Headers used in the HTTP request.</param>
	/// <param name="postObject">Object to add as the content (POST and PUT only).</param>
	/// <param name="patchDoc">Patch document for PATCH requests.</param>
	internal static void AddContent<TBody>(this HttpRequestMessage httpRequestMessage, HttpMethod httpMethod, IDictionary<string, string>? httpHeaders = null, TBody? postObject = default, HttpContent? patchDoc = null)
	{
		if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
		{
			string? contentTypeValue = null;
			bool hasContentType = httpHeaders?.TryGetValue("Content-Type", out contentTypeValue) == true;

			if (hasContentType && contentTypeValue.StrEq(MemPack))
			{
				httpRequestMessage.Content = new ByteArrayContent(MemoryPackSerializer.Serialize(postObject));
				httpRequestMessage.Content.Headers.ContentType = new(MemPack);
			}
			else if (hasContentType && contentTypeValue!.StrEq(MsgPack))
			{
				httpRequestMessage.Content = new ByteArrayContent(MessagePackSerializer.Serialize(postObject));
				httpRequestMessage.Content.Headers.ContentType = new(MsgPack);
			}
			else
			{
				httpRequestMessage.Content = JsonContent.Create(postObject, new MediaTypeHeaderValue(Json));
			}
		}
		else if (httpMethod == HttpMethod.Patch && patchDoc != null)
		{
			httpRequestMessage.Content = patchDoc;
			httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
		}
	}

	/// <summary>
	/// Attaches headers to client from httpHeaders if applicable, else only attaches authorization.
	/// </summary>
	/// <param name="httpRequestMessage">HTTP request to add headers to.</param>
	/// <param name="bearerToken">Token used for bearer authentication</param>
	/// <param name="httpHeaders">Dictionary of headers</param>
	internal static void AttachHeaders(this HttpRequestMessage httpRequestMessage, string? bearerToken, IDictionary<string, string>? httpHeaders)
	{
		if (!bearerToken.IsNullOrEmpty())
		{
			httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerToken}");
		}

		if (httpHeaders.AnyFast())
		{
			foreach (KeyValuePair<string, string> header in httpHeaders!)
			{
				httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
		}
	}

	/// <summary>
	/// Gets the number of chunks and items per chunk to return with yield return to fit within MvcOptions.MaxIAsyncEnumerableBufferLimit.
	/// </summary>
	/// <param name="itemCount">Total number of items to transmit.</param>
	/// <param name="startingItemsPerChunk">
	/// Minimum chunk size to see if it fits within the buffer limit.<br/>Will increase from initial value until the number of chunks fits within the buffer limit.
	/// </param>
	/// <param name="bufferLimit">Maximum number of buffer operations allowed by IAsyncEnumerable. Default = 8192</param>
	/// <returns>itemsPerChunk and numberOfChunks</returns>
	public static (int itemsPerChunk, int numberOfChunks) GetChunkingParameters(int itemCount, int startingItemsPerChunk = 10000, int bufferLimit = 8192)
	{
		//IAsyncEnumerable is limited to MvcOptions.MaxIAsyncEnumerableBufferLimit which is 8192 by default
		int itemsPerChunk = startingItemsPerChunk;

		//int numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);
		decimal numberOfChunksDecimal = (decimal)itemCount / itemsPerChunk;
		int numberOfChunks = (int)numberOfChunksDecimal + (numberOfChunksDecimal > 0 && numberOfChunksDecimal % 1 != 0 ? 1 : 0);
		while (numberOfChunks >= bufferLimit)
		{
			itemsPerChunk += 1000;

			//numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);
			numberOfChunksDecimal = (decimal)itemCount / itemsPerChunk;
			numberOfChunks = (int)numberOfChunksDecimal + (numberOfChunksDecimal > 0 && numberOfChunksDecimal % 1 != 0 ? 1 : 0);
		}
		return (itemsPerChunk, numberOfChunks);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static TimeSpan GetTimeout<TBody>(RequestOptions<TBody> requestOptions)
	{
		return TimeSpan.FromSeconds(requestOptions.Timeout is null or <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout);
	}

	private static async ValueTask LogRequest<TBody>(RequestOptions<TBody> requestOptions, CancellationToken cancellationToken)
	{
		if (requestOptions.LogRequest)
		{
			string method = requestOptions.HttpMethod.Method;
			string url = requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri();
			logger.Info("HTTP Request: {Method} {Url}", method, url);
		}

		if (requestOptions.LogBody && requestsWithBody.Contains(requestOptions.HttpMethod))
		{
#pragma warning disable S2955 // Generic parameters not constrained to reference types should not be compared to "null"
			string body = requestOptions.BodyObject != null
				? System.Text.Json.JsonSerializer.Serialize(requestOptions.BodyObject, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions)
				: requestOptions.PatchDocument != null
					? await requestOptions.PatchDocument.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
					: string.Empty;
#pragma warning restore S2955 // Generic parameters not constrained to reference types should not be compared to "null"
			logger.Info("Request Body: {Body}", body);
		}
	}

	private static async Task LogResponse<TResponse, TBody>(RequestOptions<TBody> requestOptions, TResponse? response, bool includeHeader, CancellationToken cancellationToken)
	{
		if (requestOptions.LogResponse)
		{
			string? resultJson = null;
			if (requestOptions.UseNewtonsoftDeserializer)
			{
				Newtonsoft.Json.JsonSerializer serializer = new();
				TextWriter writer = new StringWriter();
				serializer.Serialize(writer, response, typeof(TResponse));
				resultJson = writer.ToString();
			}
			else
			{
				await using MemoryStream outputStream = new();
				await System.Text.Json.JsonSerializer.SerializeAsync(outputStream, response, cancellationToken: cancellationToken).ConfigureAwait(false);
				resultJson = Encoding.UTF8.GetString(outputStream.ToArray());
			}

			if (includeHeader)
			{
				logger.Info("HTTP Response for {Method} @ {Url}: {Result}",
					requestOptions.HttpMethod.Method,
					requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri(),
					resultJson ?? "Empty Result");
			}
			else
			{
				logger.Info("{Result}", resultJson ?? "Empty Result");
			}
		}
	}
}
