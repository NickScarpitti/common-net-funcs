using System.Buffers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using CommonNetFuncs.Compression;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Common;
using MemoryPack;
using MemoryPack.Compression;
using MessagePack;
using Newtonsoft.Json;
using NLog;
using static CommonNetFuncs.Compression.Streams;

namespace CommonNetFuncs.Web.Requests;

public static class RestHelpersStatic
{
    public static System.Text.Json.JsonSerializerOptions? JsonSerializerOptions { get; set; }
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static readonly System.Text.Json.JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles, PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Checks if the HTTP request was successful and then parses the response if it is
    /// </summary>
    /// <typeparam name="T">Type of expected response content</typeparam>
    /// <param name="response">Response message from the HTTP request</param>
    /// <param name="httpMethod">HTTP method used to make the HTTP request</param>
    /// <param name="url">URL HTTP request was made against</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <returns>Response content if HTTP request was successful</returns>
    internal static async Task<T?> HandleResponse<T>(HttpResponseMessage response, string httpMethod, string url, bool useNewtonsoftDeserializer, MsgPackOptions? msgPackOptions = null, Dictionary<string, string>? httpHeaders = null)
    {
        T? result = default;
        try
        {
            string? contentType = response.Content.Headers.ContentType?.ToString();
            string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

            await using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                result = await ReadResponseStream<T>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, msgPackOptions).ConfigureAwait(false);
            }
            else
            {
                string? errorMessage = null;
                if (contentType.ContainsInvariant(ContentTypes.JsonProblem))
                {
                    ProblemDetailsWithErrors? problemDetails = await ReadResponseStream<ProblemDetailsWithErrors>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, msgPackOptions).ConfigureAwait(false) ?? new();
                    errorMessage = $"({problemDetails.Status}) {problemDetails.Title}\n\t\t{string.Join("\n\t\t", problemDetails.Errors.Select(x => $"{x.Key}:\n\t\t\t{string.Join("\n\t\t\t", x.Value)}"))}";
                }
                else
                {
                    errorMessage = await ReadResponseStream<string>(responseStream, ContentTypes.Text, contentEncoding, useNewtonsoftDeserializer, msgPackOptions).ConfigureAwait(false);
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
    internal static async IAsyncEnumerable<T?> HandleResponseAsync<T>(HttpResponseMessage response, string httpMethod, string url, Dictionary<string, string>? httpHeaders = null)
    {
        Stream? responseStream = null;
        try
        {
            IAsyncEnumerator<T?>? enumeratedReader = null;
            try
            {
                string? contentType = response.Content.Headers.ContentType?.ToString();
                string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

                response.Content.ReadFromJsonAsAsyncEnumerable<T>();

                responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    enumeratedReader = ReadResponseStreamAsync<T?>(responseStream, contentType, contentEncoding).GetAsyncEnumerator();
                }
                else
                {
                    string? errorMessage = null;
                    if (contentType.ContainsInvariant(ContentTypes.JsonProblem))
                    {
                        ProblemDetailsWithErrors? problemDetails = await ReadResponseStream<ProblemDetailsWithErrors>(responseStream, contentType, contentEncoding, false).ConfigureAwait(false) ?? new();
                        errorMessage = $"({problemDetails.Status}) {problemDetails.Title}\n\t\t{string.Join("\n\t\t", problemDetails.Errors.Select(x => $"{x.Key}:\n\t\t\t{string.Join("\n\t\t\t", x.Value)}"))}";
                    }
                    else
                    {
                        errorMessage = await ReadResponseStream<string>(responseStream, ContentTypes.Text, contentEncoding, false).ConfigureAwait(false);
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
        }
    }

    public static async Task<T?> ReadResponseStream<T>(Stream responseStream, string? contentType, string? contentEncoding, bool useNewtonsoftDeserializer, MsgPackOptions? msgPackOptions = null)
    {
        T? result = default;
        try
        {
            if (responseStream.Length > 1)
            {
                if (contentType.StrEq(ContentTypes.MsgPack)) //Message Pack uses native compression
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
                        result = await MessagePackSerializer.DeserializeAsync<T>(responseStream, messagePackOptions).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await MessagePackSerializer.DeserializeAsync<T>(responseStream).ConfigureAwait(false);
                    }
                }
                else if (contentType.StrEq(ContentTypes.MemPack)) //NOTE:: Will fail if trying to deserialize null value, ensure NoContent is sent back for nulls
                {
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip).ConfigureAwait(false);
                        result = MemoryPackSerializer.Deserialize<T>(new(outputStream.ToArray())); //Access deserialize decompressed data from outputStream
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        BrotliDecompressor decompressor = new();
                        ReadOnlySequence<byte> decompressedBuffer = decompressor.Decompress(new ReadOnlySpan<byte>(await responseStream.ReadStreamAsync().ConfigureAwait(false)));
                        result = MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
                    }
                    else
                    {
                        result = await MemoryPackSerializer.DeserializeAsync<T>(responseStream).ConfigureAwait(false);
                    }
                }
                else if (contentType.ContainsInvariant("json"))//Assume JSON
                {
                    //Deserialize as stream - More memory efficient than string deserialization
                    await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip).ConfigureAwait(false);
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Brotli).ConfigureAwait(false);
                    }

                    if (useNewtonsoftDeserializer)
                    {
                        using StreamReader streamReader = new(outputStream.Length > 1 ? outputStream : responseStream);
                        await using JsonTextReader jsonReader = new(streamReader); //Newtonsoft
                        JsonSerializer serializer = new(); //Newtonsoft
                        result = serializer.Deserialize<T>(jsonReader); //using static Newtonsoft.Json.JsonSerializer;
                    }
                    else
                    {
                        result = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(outputStream.Length > 0 ? outputStream : responseStream, JsonSerializerOptions ?? defaultJsonSerializerOptions).ConfigureAwait(false);
                    }
                }
                else if (contentType.ContainsInvariant("text")) //String encoding (error usually)
                {
                    await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip).ConfigureAwait(false);
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Brotli).ConfigureAwait(false);
                    }

                    string stringResult;
                    using StreamReader reader = new(outputStream.Length == 0 ? responseStream : outputStream, Encoding.UTF8);
                    stringResult = reader.ReadToEnd();
                    result = (T)(object)stringResult;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return result;
    }

    public static async IAsyncEnumerable<T?> ReadResponseStreamAsync<T>(Stream responseStream, string? contentType, string? contentEncoding)
    {
        if (contentType.ContainsInvariant("json"))
        {
            await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream

            //Deserialize as stream - More memory efficient than string deserialization
            if (contentEncoding.StrEq(EncodingTypes.GZip))
            {
                await responseStream.DecompressStream(outputStream, ECompressionType.Gzip).ConfigureAwait(false);
            }
            else if (contentEncoding.StrEq(EncodingTypes.Brotli))
            {
                await responseStream.DecompressStream(outputStream, ECompressionType.Brotli).ConfigureAwait(false);
            }

            await foreach (T? item in System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<T?>(outputStream.Length > 1 ? outputStream : responseStream, JsonSerializerOptions ?? defaultJsonSerializerOptions))
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
        else
        {
            throw new NotImplementedException($"Content type {contentType.UrlEncodeReadable()} is not available");
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
            if (httpHeaders?.Any(x => x.Key.StrEq("Content-Type") && x.Value.StrEq(ContentTypes.MemPack)) ?? false)
            {
                httpRequestMessage.Content = new ByteArrayContent(MemoryPackSerializer.Serialize(postObject));
                httpRequestMessage.Content.Headers.ContentType = new(ContentTypes.MemPack);
            }
            else if (httpHeaders?.Any(x => x.Key.StrEq("Content-Type") && x.Value.StrEq(ContentTypes.MsgPack)) ?? false)
            {
                httpRequestMessage.Content = new ByteArrayContent(MessagePackSerializer.Serialize(postObject));
                httpRequestMessage.Content.Headers.ContentType = new(ContentTypes.MsgPack);
            }
            else
            {
                httpRequestMessage.Content = JsonContent.Create(postObject, new MediaTypeHeaderValue(ContentTypes.Json));
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
    /// <param name="startingitemsPerChunk">Minimum chunk size to see if it fits within the buffer limit.<br/>Will increase from initial value until the number of chunks fits within the buffer limit</param>
    /// <param name="bufferLimit">Maximum number of buffer operations allowed by IAsyncEnumerable. Default = 8192</param>
    /// <returns>itemsPerChunk and numberOfChunks</returns>
    public static (int itemsPerChunk, int numberOfChunks) GetChunkingParameters(int itemCount, int startingitemsPerChunk = 10000, int bufferLimit = 8192)
    {
        //IAsyncEnumerable is limited to MvcOptions.MaxIAsyncEnumerableBufferLimit which is 8192 by default
        int itemsPerChunk = startingitemsPerChunk;
        int numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);

        while (numberOfChunks >= bufferLimit)
        {
            itemsPerChunk += 1000;
            numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);
        }
        return (itemsPerChunk, numberOfChunks);
    }
}
