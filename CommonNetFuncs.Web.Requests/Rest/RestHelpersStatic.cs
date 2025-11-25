using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryPack;
using MemoryPack.Compression;
using MessagePack;
using Newtonsoft.Json;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Streams;
using static CommonNetFuncs.Core.Strings;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Common.EncodingTypes;
using static CommonNetFuncs.Web.Common.UriHelpers;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace CommonNetFuncs.Web.Requests.Rest;

public static class RestHelpersStatic
{
	private static readonly HttpMethod[] requestsWithBody = [HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch];
	private const double DefaultRequestTimeout = 100;

	//public static JsonSerializerOptions? JsonSerializerOptions { get; set; }
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	public static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles, PropertyNameCaseInsensitive = true };

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions.
	/// </summary>
	/// <typeparam name="T">Type of return object.</typeparam>
	/// <typeparam name="UT">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
	public static async Task<T?> RestRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		T? result = default;
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
			result = await HandleResponse<T, UT>(response, requestOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Info("{msg}", $"Task was expectedly canceled for {requestOptions.HttpMethod.ToString().ToUpper()} request to {requestOptions.Url}");
			}
			else
			{
				logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {requestOptions.Url}");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {requestOptions.Url}");
		}
		return result;
	}

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable.
	/// </summary>
	/// <typeparam name="T">Type of return object.</typeparam>
	/// <typeparam name="UT">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
	public static async IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerator<T?>? enumeratedReader = null;
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
					requestOptions.HttpHeaders = new([JsonAcceptHeader]);
				}
				else if (requestOptions.HttpHeaders.Remove(AcceptHeader))
				{
					requestOptions.HttpHeaders.AddDictionaryItem(JsonAcceptHeader);
				}

				httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
				httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

				//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
				response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
				enumeratedReader = HandleResponseAsync<T, UT>(response, requestOptions, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
			}
			catch (TaskCanceledException tcex)
			{
				string exceptionLocation = tcex.GetLocationOfException();
				if (requestOptions.ExpectTaskCancellation)
				{
					logger.Info("{msg}", $"Task was expectedly canceled for {requestOptions.HttpMethod.ToString().ToUpper()} request to {requestOptions.Url}");
				}
				else
				{
					logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {requestOptions.Url}");
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {requestOptions.Url}");
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
	/// <typeparam name="T">Type of return object.</typeparam>
	/// <typeparam name="UT">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
	public static async Task<RestObject<T>> RestObjectRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		RestObject<T> restObject = new();
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
			restObject.Result = await HandleResponse<T, UT>(restObject.Response, requestOptions, cancellationToken).ConfigureAwait(false);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Warn("{msg}", $"Run once REST task was canceled for {requestOptions.HttpMethod.ToString().ToUpper()} request to {requestOptions.Url}");
			}
			else
			{
				logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {requestOptions.Url}");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {requestOptions.Url}");
		}
		return restObject;
	}

	/// <summary>
	/// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable.
	/// </summary>
	/// <typeparam name="T">Type of return object.</typeparam>
	/// <typeparam name="UT">Type of object used in body (if any).</typeparam>
	/// <param name="client">HttpClient to execute REST request with.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
	public static async Task<StreamingRestObject<T>> StreamingRestObjectRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		StreamingRestObject<T> restObject = new();
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
				requestOptions.HttpHeaders = new([JsonAcceptHeader]);
			}
			else if (requestOptions.HttpHeaders.TryGetValue(AcceptHeader, out string? header) && header != Json && requestOptions.HttpHeaders.Remove(AcceptHeader))
			{
				requestOptions.HttpHeaders.AddDictionaryItem(JsonAcceptHeader);
			}

			httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
			httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

			//client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
			restObject.Response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, combinedTokenSource.Token).ConfigureAwait(false) ?? new();
			restObject.Result = HandleResponseAsync<T, UT>(restObject.Response, requestOptions, cancellationToken: cancellationToken);
		}
		catch (TaskCanceledException tcex)
		{
			string exceptionLocation = tcex.GetLocationOfException();
			if (requestOptions.ExpectTaskCancellation)
			{
				logger.Info("{msg}", $"Task was expectedly canceled for {requestOptions.HttpMethod.ToString().ToUpper()} request to {requestOptions.Url}");
			}
			else
			{
				logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {requestOptions.Url}");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {requestOptions.Url}");
		}

		return restObject;
	}

	/// <summary>
	/// Checks if the HTTP request was successful and then parses the response if it is.
	/// </summary>
	/// <typeparam name="T">Type of expected response content.</typeparam>
	/// <param name="response">Response message from the HTTP request.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Response content if HTTP request was successful</returns>
	internal static async Task<T?> HandleResponse<T, UT>(this HttpResponseMessage response, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
	{
		T? result = default;
		try
		{
			string? contentType = response.Content.Headers.ContentType?.ToString();
			string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

			await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				result = await ReadResponseStream<T>(responseStream, contentType, contentEncoding, requestOptions.UseNewtonsoftDeserializer, requestOptions.JsonSerializerOptions,
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
				logger.Warn("{msg}", $"{requestOptions.HttpMethod} request with URL {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.RedactedUrl)} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\n\tContent: {errorMessage}\n\t{(requestOptions.HttpHeaders != null ? $"Headers: {string.Join(", ", requestOptions.HttpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null)}");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
		}

		await LogResponse(requestOptions, result, true, cancellationToken).ConfigureAwait(false);

		return result;
	}

	/// <summary>
	/// Checks if the HTTP request was successful and then parses the response if it is.
	/// </summary>
	/// <typeparam name="T">Type of expected response content.</typeparam>
	/// <typeparam name="UT">Type of object used in body (if any).</typeparam>
	/// <param name="response">Response message from the HTTP request.</param>
	/// <param name="requestOptions">Configuration parameters for the REST request.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Response content if HTTP request was successful</returns>
	internal static async IAsyncEnumerable<T?> HandleResponseAsync<T, UT>(this HttpResponseMessage response, RequestOptions<UT> requestOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerator<T?>? enumeratedReader = null;
		Stream? responseStream = null;
		Stream? decompressedStream = null;
		try
		{
			try
			{
				string? contentType = response.Content.Headers.ContentType?.ToString();
				string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

				//response.Content.ReadFromJsonAsAsyncEnumerable<T>(cancellationToken: cancellationToken);

				responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					//enumeratedReader = responseStream.ReadResponseStreamAsync<T?>(contentType, contentEncoding, requestOptions.JsonSerializerOptions, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);

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

					enumeratedReader = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<T?>(streamToRead, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken)
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
					logger.Warn("{msg}", $"{requestOptions.HttpMethod} request with URL {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.RedactedUrl)} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\n\tContent: {errorMessage}\n\t{(requestOptions.HttpHeaders != null ? $"Headers: {string.Join(", ", requestOptions.HttpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null)}");
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
	/// <typeparam name="T">Type of expected response content.</typeparam>
	/// <param name="responseStream">Stream containing the response data.</param>
	/// <param name="contentType">Content type of the response.</param>
	/// <param name="contentEncoding">Content encoding of the response.</param>
	/// <param name="useNewtonsoftDeserializer">Whether to use Newtonsoft.Json for deserialization.</param>
	/// <param name="jsonSerializerOptions">JSON serializer options.</param>
	/// <param name="msgPackOptions">MessagePack serializer options.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Deserialized response content.</returns>
	public static async Task<T?> ReadResponseStream<T>(this Stream responseStream, string? contentType, string? contentEncoding, bool useNewtonsoftDeserializer,
				JsonSerializerOptions? jsonSerializerOptions = null, MsgPackOptions? msgPackOptions = null, CancellationToken cancellationToken = default)
	{
		T? result = default;
		try
		{
			if (responseStream.Length > 1)
			{
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
						result = await MessagePackSerializer.DeserializeAsync<T>(responseStream, messagePackOptions, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						result = await MessagePackSerializer.DeserializeAsync<T>(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
					}
				}
				else if (contentType.StrEq(MemPack)) // ***Will fail if trying to deserialize null value, ensure NoContent is sent back for nulls***
				{
					if (contentEncoding.StrEq(GZip))
					{
						//MemoryStream outputStream = new(); //Decompressed data will be written to this stream
						//try
						//{
						//	await responseStream.Decompress(ECompressionType.Gzip).CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
						//	//result = MemoryPackSerializer.Deserialize<T>(new(outputStream.ToArray())); //Access deserialize decompressed data from outputStream
						//	outputStream.Position = 0;
						//	result = await MemoryPackSerializer.DeserializeAsync<T>(outputStream, cancellationToken: cancellationToken).ConfigureAwait(false);
						//}
						//finally
						//{
						//	await outputStream.DisposeAsync().ConfigureAwait(false);
						//}
						await using Stream decompressedStream = responseStream.Decompress(ECompressionType.Gzip);
						result = await MemoryPackSerializer.DeserializeAsync<T>(decompressedStream, cancellationToken: cancellationToken).ConfigureAwait(false);
					}
					else if (contentEncoding.StrEq(Brotli))
					{
						BrotliDecompressor decompressor = new();
						result = MemoryPackSerializer.Deserialize<T>(decompressor.Decompress(new ReadOnlySpan<byte>(await responseStream.ReadStreamAsync(cancellationToken: cancellationToken).ConfigureAwait(false))));
					}
					else
					{
						result = await MemoryPackSerializer.DeserializeAsync<T>(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
					}
				}
				else if (contentType.ContainsInvariant("json"))//Assume JSON
				{
					//Deserialize as stream - More memory efficient than string deserialization
					Stream? outputStream = null; //Decompressed data will be written to this stream
					bool usedDecompression = false;

					try
					{
						if (contentEncoding.StrEq(GZip))
						{
							usedDecompression = true;
							outputStream = responseStream.Decompress(ECompressionType.Gzip);
							//await responseStream.DecompressStream(outputStream, ECompressionType.Gzip, cancellationToken: cancellationToken).ConfigureAwait(false);
						}
						else if (contentEncoding.StrEq(Brotli))
						{
							usedDecompression = true;
							outputStream = responseStream.Decompress(ECompressionType.Brotli);
							//await responseStream.DecompressStream(outputStream, ECompressionType.Brotli, cancellationToken: cancellationToken).ConfigureAwait(false);
						}

						outputStream ??= new MemoryStream();
						if (useNewtonsoftDeserializer)
						{
							//using StreamReader streamReader = new(outputStream.Length > 1 ? outputStream : responseStream);
							using StreamReader streamReader = new(usedDecompression ? outputStream : responseStream);
							await using JsonTextReader jsonReader = new(streamReader); //Newtonsoft
							Newtonsoft.Json.JsonSerializer serializer = new(); //Newtonsoft
							result = serializer.Deserialize<T>(jsonReader); //using static Newtonsoft.Json.JsonSerializer;
						}
						else
						{
							result = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(usedDecompression ? outputStream : responseStream, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
						}
					}
					finally
					{
						if (outputStream != null)
						{
							await outputStream.DisposeAsync().ConfigureAwait(false);
						}
					}
				}
				else if (contentType.ContainsInvariant("text")) //String encoding (error usually)
				{
					Stream? outputStream = null; //Decompressed data will be written to this stream
					bool usedDecompression = false;

					try
					{
						if (contentEncoding.StrEq(GZip))
						{
							usedDecompression = true;
							outputStream = responseStream.Decompress(ECompressionType.Gzip);
						}
						else if (contentEncoding.StrEq(Brotli))
						{
							usedDecompression = true;
							outputStream = responseStream.Decompress(ECompressionType.Brotli);
						}

						outputStream ??= new MemoryStream();
						string stringResult;
						using StreamReader reader = new(usedDecompression ? outputStream : responseStream, Encoding.UTF8);
						stringResult = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
						result = (T)(object)stringResult;
					}
					finally
					{
						if (outputStream != null)
						{
							await outputStream.DisposeAsync().ConfigureAwait(false);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
		}
		return result;
	}

	/// <summary>
	/// Reads the response stream and asynchronously deserializes it based on the content type and content encoding.
	/// </summary>
	/// <typeparam name="T">Type of the expected response content.</typeparam>
	/// <param name="responseStream">Stream containing the response data.</param>
	/// <param name="contentType">Content type of the response.</param>
	/// <param name="contentEncoding">Content encoding of the response.</param>
	/// <param name="jsonSerializerOptions">JSON serializer options.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>Deserialized response content.</returns>
	/// <exception cref="NotImplementedException"></exception>
	public static async IAsyncEnumerable<T?> ReadResponseStreamAsync<T>(this Stream responseStream, string? contentType, string? contentEncoding, JsonSerializerOptions? jsonSerializerOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

				await foreach (T? item in System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<T?>(streamToRead, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false))
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
	/// <typeparam name="T">Type of the post object being added to the HTTP request content.</typeparam>
	/// <param name="httpRequestMessage">HTTP request to add content to.</param>
	/// <param name="httpMethod">HTTP request method.</param>
	/// <param name="httpHeaders">Headers used in the HTTP request.</param>
	/// <param name="postObject">Object to add as the content (POST and PUT only).</param>
	/// <param name="patchDoc">Patch document for PATCH requests.</param>
	internal static void AddContent<T>(this HttpRequestMessage httpRequestMessage, HttpMethod httpMethod, Dictionary<string, string>? httpHeaders = null, T? postObject = default, HttpContent? patchDoc = null)
	{
		if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
		{
			if (httpHeaders?.Any(x => x.Key.StrEq("Content-Type") && x.Value.StrEq(MemPack)) ?? false)
			{
				httpRequestMessage.Content = new ByteArrayContent(MemoryPackSerializer.Serialize(postObject));
				httpRequestMessage.Content.Headers.ContentType = new(MemPack);
			}
			else if (httpHeaders?.Any(x => x.Key.StrEq("Content-Type") && x.Value.StrEq(MsgPack)) ?? false)
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
	internal static void AttachHeaders(this HttpRequestMessage httpRequestMessage, string? bearerToken, Dictionary<string, string>? httpHeaders)
	{
		//Changed this from inline if due to setting .Authorization to null if bearerToken is empty/null resulting in an exception during the post request: "A task was canceled"
		if (bearerToken != null || (bearerToken?.Length == 0 && !(httpHeaders?.Any(x => x.Key.StrEq("Authorization")) ?? false)))
		{
			try
			{
				httpRequestMessage.Headers.Authorization = new("Bearer", bearerToken);
			}
			catch (Exception ex)
			{
				logger.Warn(ex, "{msg}", $"Failed to add bearer token.\nDefault headers = {httpRequestMessage.Headers}\nNot validated headers = {httpRequestMessage.Headers.NonValidated}");
			}
		}

		if (httpHeaders.AnyFast())
		{
			foreach (KeyValuePair<string, string> header in httpHeaders!)
			{
				try
				{
					httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}
				catch (Exception ex)
				{
					logger.Warn(ex, "{msg}", $"Failed to add header {header.Key} with value {header.Value}.\nDefault headers = {httpRequestMessage.Headers}\nNot validated headers = {httpRequestMessage.Headers.NonValidated}");
				}
			}
		}
	}

	/// <summary>
	/// Gets the number of chunks and items per chunk to return with yield return to fit within MvcOptions.MaxIAsyncEnumerableBufferLimit.
	/// </summary>
	/// <param name="itemCount">Total number of items to transmit.</param>
	/// <param name="startingitemsPerChunk">
	/// Minimum chunk size to see if it fits within the buffer limit.<br/>Will increase from initial value until the number of chunks fits within the buffer limit.
	/// </param>
	/// <param name="bufferLimit">Maximum number of buffer operations allowed by IAsyncEnumerable. Default = 8192</param>
	/// <returns>itemsPerChunk and numberOfChunks</returns>
	public static (int itemsPerChunk, int numberOfChunks) GetChunkingParameters(int itemCount, int startingitemsPerChunk = 10000, int bufferLimit = 8192)
	{
		//IAsyncEnumerable is limited to MvcOptions.MaxIAsyncEnumerableBufferLimit which is 8192 by default
		int itemsPerChunk = startingitemsPerChunk;

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

	private static TimeSpan GetTimeout<T>(RequestOptions<T> requestOptions)
	{
		return TimeSpan.FromSeconds(requestOptions.Timeout is null or <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout);
	}

	private static async ValueTask LogRequest<T>(RequestOptions<T> requestOptions, CancellationToken cancellationToken)
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

	private static async Task LogResponse<T, UT>(RequestOptions<UT> requestOptions, T? response, bool includeHeader, CancellationToken cancellationToken)
	{
		if (requestOptions.LogResponse)
		{
			string? resultJson = null;
			if (requestOptions.UseNewtonsoftDeserializer)
			{
				Newtonsoft.Json.JsonSerializer serializer = new();
				TextWriter writer = new StringWriter();
				serializer.Serialize(writer, response, typeof(T));
				resultJson = writer.ToString();
			}
			else
			{
				await using MemoryStream outputStream = new();
				await System.Text.Json.JsonSerializer.SerializeAsync(outputStream, response, cancellationToken: cancellationToken).ConfigureAwait(false);
				resultJson = Encoding.UTF8.GetString(outputStream.ToArray());
			}

			string logText = string.Empty;
			if (includeHeader)
			{
				logText += $"HTTP Response for {requestOptions.HttpMethod.Method} @ {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri())}:\n";
			}

			logText += resultJson ?? "Empty Result";
			logger.Info(logText);
		}
	}
}
