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
    /// Executes a REST request against the provided URL with the requestOptions
    /// </summary>
    /// <typeparam name="T">Type of return object</typeparam>
    /// <typeparam name="UT">Type of object used in body (if any)</typeparam>
    /// <param name="client">HttpClient to execute REST request with</param>
    /// <param name="requestOptions">Configuration parameters for the REST request</param>
    /// <returns>Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
    public static async Task<T?> RestRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
    {
        T? result = default;
        try
        {
            logger.Info("{msg}", $"{requestOptions.HttpMethod.ToString().ToUpper()} URL: {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri())}" + (requestOptions.LogBody && requestsWithBody.Contains(requestOptions.HttpMethod) ?
                $" | {(requestOptions.BodyObject != null ? System.Text.Json.JsonSerializer.Serialize(requestOptions.BodyObject, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions) : requestOptions.PatchDocument != null ? await requestOptions.PatchDocument.ReadAsStringAsync(cancellationToken).ConfigureAwait(false) : string.Empty)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(requestOptions.Timeout == null || requestOptions.Timeout <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout));
            using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);
            httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
            httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

            //client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
            using HttpResponseMessage response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, tokenSource.Token).ConfigureAwait(false) ?? new();
            result = await HandleResponse<T>(response, requestOptions.HttpMethod.ToString(), requestOptions.Url, requestOptions.UseNewtonsoftDeserializer, requestOptions.JsonSerializerOptions, requestOptions.MsgPackOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
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
    /// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable
    /// </summary>
    /// <typeparam name="T">Type of return object</typeparam>
    /// <typeparam name="UT">Type of object used in body (if any)</typeparam>
    /// <param name="client">HttpClient to execute REST request with</param>
    /// <param name="requestOptions">Configuration parameters for the REST request</param>
    /// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
    public static async IAsyncEnumerable<T?> StreamingRestRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T?>? enumeratedReader = null;
        HttpResponseMessage? response = null;
        try
        {
            try
            {
                logger.Info("{msg}", $"{requestOptions.HttpMethod.ToString().ToUpper()} URL: {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri())}" + (requestOptions.LogBody && requestsWithBody.Contains(requestOptions.HttpMethod) ?
                    $" | {(requestOptions.BodyObject != null ? System.Text.Json.JsonSerializer.Serialize(requestOptions.BodyObject, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions) : requestOptions.PatchDocument != null ? await requestOptions.PatchDocument.ReadAsStringAsync(cancellationToken).ConfigureAwait(false) : string.Empty)}" : string.Empty));
                using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(requestOptions.Timeout == null || requestOptions.Timeout <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout));
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
                response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, tokenSource.Token).ConfigureAwait(false) ?? new();
                enumeratedReader = HandleResponseAsync<T>(response, requestOptions.HttpMethod.ToString(), requestOptions.Url, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
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
    /// Executes a REST request against the provided URL with the requestOptions
    /// </summary>
    /// <typeparam name="T">Type of return object</typeparam>
    /// <typeparam name="UT">Type of object used in body (if any)</typeparam>
    /// <param name="client">HttpClient to execute REST request with</param>
    /// <param name="requestOptions">Configuration parameters for the REST request</param>
    /// <returns>Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
    public static async Task<RestObject<T>> RestObjectRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
    {
        RestObject<T> restObject = new();
        try
        {
            logger.Info("{msg}", $"{requestOptions.HttpMethod.ToString().ToUpper()} URL: {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri())}" + (requestOptions.LogBody && requestsWithBody.Contains(requestOptions.HttpMethod) ?
                $" | {(requestOptions.BodyObject != null ? System.Text.Json.JsonSerializer.Serialize(requestOptions.BodyObject, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions) : requestOptions.PatchDocument != null ? await requestOptions.PatchDocument.ReadAsStringAsync(cancellationToken).ConfigureAwait(false) : string.Empty)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(requestOptions.Timeout == null || requestOptions.Timeout <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout));
            using HttpRequestMessage httpRequestMessage = new(requestOptions.HttpMethod, requestOptions.Url);
            httpRequestMessage.AttachHeaders(requestOptions.BearerToken, requestOptions.HttpHeaders);
            httpRequestMessage.AddContent(requestOptions.HttpMethod, requestOptions.HttpHeaders, requestOptions.BodyObject, requestOptions.PatchDocument);

            //client.Timeout = requestOptions.Timeout == null ? client.Timeout : TimeSpan.FromSeconds((long)requestOptions.Timeout);
            restObject.Response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, tokenSource.Token).ConfigureAwait(false) ?? new();
            restObject.Result = await HandleResponse<T>(restObject.Response, requestOptions.HttpMethod.ToString(), requestOptions.Url, requestOptions.UseNewtonsoftDeserializer, requestOptions.JsonSerializerOptions, requestOptions.MsgPackOptions, requestOptions.HttpHeaders, cancellationToken).ConfigureAwait(false);
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
    /// Executes a REST request against the provided URL with the requestOptions and streams the results using IAsyncEnumerable
    /// </summary>
    /// <typeparam name="T">Type of return object</typeparam>
    /// <typeparam name="UT">Type of object used in body (if any)</typeparam>
    /// <param name="client">HttpClient to execute REST request with</param>
    /// <param name="requestOptions">Configuration parameters for the REST request</param>
    /// <returns>An IAsyncEnumerable of the Object of type <typeparamref name="T"/> resulting from the request - Null if not success.</returns>
    public static async Task<StreamingRestObject<T>> StreamingRestObjectRequest<T, UT>(this HttpClient client, RequestOptions<UT> requestOptions, CancellationToken cancellationToken = default)
    {
        StreamingRestObject<T> restObject = new();
        try
        {
            logger.Info("{msg}", $"{requestOptions.HttpMethod.ToString().ToUpper()} URL: {(requestOptions.LogQuery ? requestOptions.Url : requestOptions.Url.GetRedactedUri())}" + (requestOptions.LogBody && requestsWithBody.Contains(requestOptions.HttpMethod) ?
                $" | {(requestOptions.BodyObject != null ? System.Text.Json.JsonSerializer.Serialize(requestOptions.BodyObject, requestOptions.JsonSerializerOptions ?? defaultJsonSerializerOptions) : requestOptions.PatchDocument != null ? await requestOptions.PatchDocument.ReadAsStringAsync(cancellationToken).ConfigureAwait(false) : string.Empty)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(requestOptions.Timeout == null || requestOptions.Timeout <= 0 ? DefaultRequestTimeout : (double)requestOptions.Timeout));
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
            restObject.Response = await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead, tokenSource.Token).ConfigureAwait(false) ?? new();
            restObject.Result = HandleResponseAsync<T>(restObject.Response, requestOptions.HttpMethod.ToString(), requestOptions.Url, cancellationToken: cancellationToken);
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
    /// Checks if the HTTP request was successful and then parses the response if it is
    /// </summary>
    /// <typeparam name="T">Type of expected response content</typeparam>
    /// <param name="response">Response message from the HTTP request</param>
    /// <param name="httpMethod">HTTP method used to make the HTTP request</param>
    /// <param name="url">URL HTTP request was made against</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <returns>Response content if HTTP request was successful</returns>
    internal static async Task<T?> HandleResponse<T>(this HttpResponseMessage response, string httpMethod, string url, bool useNewtonsoftDeserializer, JsonSerializerOptions? jsonSerializerOptions = null,
        MsgPackOptions? msgPackOptions = null, Dictionary<string, string>? httpHeaders = null, CancellationToken cancellationToken = default)
    {
        T? result = default;
        try
        {
            string? contentType = response.Content.Headers.ContentType?.ToString();
            string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                result = await ReadResponseStream<T>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, jsonSerializerOptions, msgPackOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                string? errorMessage = null;
                if (contentType.ContainsInvariant(JsonProblem))
                {
                    ProblemDetailsWithErrors? problemDetails = await ReadResponseStream<ProblemDetailsWithErrors>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, jsonSerializerOptions, msgPackOptions, cancellationToken).ConfigureAwait(false) ?? new();
                    errorMessage = $"({problemDetails.Status}) {problemDetails.Title}\n\t\t{string.Join("\n\t\t", problemDetails.Errors.Select(x => $"{x.Key}:\n\t\t\t{string.Join("\n\t\t\t", x.Value)}"))}";
                }
                else
                {
                    errorMessage = await ReadResponseStream<string>(responseStream, Text, contentEncoding, useNewtonsoftDeserializer, jsonSerializerOptions, msgPackOptions, cancellationToken).ConfigureAwait(false);
                }
                logger.Warn("{msg}", $"{httpMethod.ToUpper()} request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\n\tContent: {errorMessage}\n\t{(httpHeaders != null ? $"Headers: {string.Join(", ", httpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null)}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return result;
    }

    /// <summary>
    /// Checks if the HTTP request was successful and then parses the response if it is
    /// </summary>
    /// <typeparam name="T">Type of expected response content</typeparam>
    /// <param name="response">Response message from the HTTP request</param>
    /// <param name="httpMethod">HTTP method used to make the HTTP request</param>
    /// <param name="url">URL HTTP request was made against</param>
    /// <returns>Response content if HTTP request was successful</returns>
    internal static async IAsyncEnumerable<T?> HandleResponseAsync<T>(this HttpResponseMessage response, string httpMethod, string url, Dictionary<string, string>? httpHeaders = null,
        JsonSerializerOptions? jsonSerializerOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<T?>? enumeratedReader = null;
        Stream? responseStream = null;
        try
        {
            try
            {
                string? contentType = response.Content.Headers.ContentType?.ToString();
                string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

                response.Content.ReadFromJsonAsAsyncEnumerable<T>(cancellationToken: cancellationToken);

                responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    enumeratedReader = responseStream.ReadResponseStreamAsync<T?>(contentType, contentEncoding, jsonSerializerOptions, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
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
                    logger.Warn("{msg}", $"{httpMethod.ToUpper()} request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\n\tContent: {errorMessage}\n\t{(httpHeaders != null ? $"Headers: {string.Join(", ", httpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null)}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }

            if (enumeratedReader != null)
            {
                while (await enumeratedReader.MoveNextAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
            responseStream?.Dispose();
            await (enumeratedReader?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
    }

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
                        MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                        try
                        {
                            responseStream.Decompress(ECompressionType.Gzip).CopyTo(outputStream);
                            result = MemoryPackSerializer.Deserialize<T>(new(outputStream.ToArray())); //Access deserialize decompressed data from outputStream
                        }
                        finally
                        {
                            await outputStream.DisposeAsync().ConfigureAwait(false);
                        }
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
                    //await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
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
                    //await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
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
                        string stringResult;
                        //using StreamReader reader = new(outputStream.Length == 0 ? responseStream : outputStream, Encoding.UTF8);
                        using StreamReader reader = new(usedDecompression ? outputStream : responseStream, Encoding.UTF8);
                        stringResult = reader.ReadToEnd();
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

                await foreach (T? item in System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<T?>(streamToRead, jsonSerializerOptions ?? defaultJsonSerializerOptions, cancellationToken))
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                streamToRead.Dispose();
            }
        }
        else
        {
            throw new NotImplementedException($"Content type {contentType.UrlEncodeReadable(cancellationToken: cancellationToken)} is not available");
        }
    }

    /// <summary>
    /// Adds content to HTTP request if not using GET HTTP request method
    /// </summary>
    /// <typeparam name="T">Type of the post object being added to the HTTP request content</typeparam>
    /// <param name="httpRequestMessage">HTTP request to add content to</param>
    /// <param name="httpMethod">HTTP request method</param>
    /// <param name="httpHeaders">Headers used in the HTTP request</param>
    /// <param name="postObject">Object to add as the content (POST and PUT only)</param>
    /// <param name="patchDoc">Patch document for PATCH requests</param>
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
    /// <param name="bearerToken">Token used for bearer authentication</param>
    /// <param name="httpHeaders">Dictionary of headers</param>
    internal static void AttachHeaders(this HttpRequestMessage httpRequestMessage, string? bearerToken, Dictionary<string, string>? httpHeaders)
    {
        //Changed this from inline if due to setting .Authorization to null if bearerToken is empty/null resulting in an exception during the post request: "A task was canceled"
        if (bearerToken != null || (bearerToken?.Length == 0 && !(httpHeaders?.Where(x => x.Key.StrEq("Authorization")).Any() ?? false)))
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
    /// Gets the number of chunks and items per chunk to return with yield return to fit within MvcOptions.MaxIAsyncEnumerableBufferLimit
    /// </summary>
    /// <param name="itemCount">Total number of items to transmit</param>
    /// <param name="startingitemsPerChunk">
    /// Minimum chunk size to see if it fits within the buffer limit.<br/>Will increase from initial value until the number of chunks fits within the buffer limit
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
}
