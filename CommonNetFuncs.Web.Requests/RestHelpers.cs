using System.Buffers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MemoryPack;
using MemoryPack.Compression;
using MessagePack;
using Newtonsoft.Json;
using NLog;
using System.Text;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Common;
using CommonNetFuncs.Compression;
using static CommonNetFuncs.Compression.Streams;

namespace CommonNetFuncs.Web.Requests;

/// <summary>
/// Helper class to get around not being able to pass primitive types directly to a generic type
/// </summary>
/// <typeparam name="T">Primitive type to pass to the REST request</typeparam>
public class RestObject<T>// where T : class
{
    public T? Result { get; set; }
    public HttpResponseMessage? Response { get; set; }
}

public class MsgPackOptions
{
    public bool UseMsgPackCompression { get; set; } = false;
    public bool UseMsgPackUntrusted { get; set; } = false;
}

public static class RestHelperConstants
{
    public const string ContentTypeHeader = "Content-Type";
    public const string AcceptEncodingHeader = "Accept-Encoding";
    public const string AcceptHeader = "Accept";

    public static readonly KeyValuePair<string, string> BrotliEncodingHeader = new(AcceptEncodingHeader, EncodingTypes.Brotli);
    public static readonly KeyValuePair<string, string> GzipEncodingHeader = new(AcceptEncodingHeader, EncodingTypes.GZip);

    public static readonly KeyValuePair<string, string> MemPackContentHeader = new(ContentTypeHeader, ContentTypes.MemPack);
    public static readonly KeyValuePair<string, string> MsgPackContentHeader = new(ContentTypeHeader, ContentTypes.MsgPack);
    public static readonly KeyValuePair<string, string> JsonContentHeader = new(ContentTypeHeader, ContentTypes.Json);

    public static readonly KeyValuePair<string, string> MemPackAcceptHeader = new(AcceptHeader, ContentTypes.MemPack);
    public static readonly KeyValuePair<string, string> MsgPackAcceptHeader = new(AcceptHeader, ContentTypes.MsgPack);
    public static readonly KeyValuePair<string, string> JsonAcceptHeader = new(AcceptHeader, ContentTypes.Json);

    public static readonly Dictionary<string, string> MemPackHeaders = new([MemPackContentHeader, MemPackAcceptHeader]);
    public static readonly Dictionary<string, string> MsgPackHeaders = new([MsgPackContentHeader, MsgPackAcceptHeader]);
    public static readonly Dictionary<string, string> JsonHeaders = new([JsonContentHeader, JsonAcceptHeader]);
}

/// <summary>
/// Helper functions that send requests to specified URI and return resulting values where applicable
/// Source1: https://medium.com/@srikanth.gunnala/generic-wrapper-to-consume-asp-net-web-api-rest-service-641b50462c0
/// Source2: https://stackoverflow.com/questions/43692053/how-can-i-create-a-jsonpatchdocument-from-comparing-two-c-sharp-objects
/// </summary>
public static class RestHelpers
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    //Use static client here instead of individual using statements to prevent maxing out the number of connections
    private const double DefaultRequestTimeout = 100; //Default timeout for HttpClient
    private static readonly SocketsHttpHandler SocketsHttpHandler = new() { MaxConnectionsPerServer = 100, KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always, KeepAlivePingDelay = TimeSpan.FromSeconds(15), KeepAlivePingTimeout = TimeSpan.FromMinutes(60) };
    private static readonly HttpClient Client = new(SocketsHttpHandler) { Timeout = Timeout.InfiniteTimeSpan }; //Use infinite timespan here to force using token specified timeout
    private static readonly List<HttpMethod> RequestsWithBody = [HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch];

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public static Task<T?> Get<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, bool useNewtonsoftDeserializer = false,
        bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static Task<T?> PostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null) where T : class
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result in string format
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>String resulting from the POST request - Null if not success</returns>
    public static Task<string?> StringPostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null) where T : class
    {
        return GenericRestRequest<string?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static Task<T?> DeleteRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a PUT request against the provided URL with the putObject in the body
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="putObject">The object to be edited</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    public static Task<T?> PutRequest<T>(string url, T putObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Put, putObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a PATCH request against the provided URL with the patchDoc in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="patchDoc">Patch document for PATCH requests</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <returns>Object of type T resulting from the PATCH request - Null if not success</returns>
    public static Task<T?> PatchRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T?, HttpContent>(url, HttpMethod.Patch, default, bearerToken, timeout, httpHeaders, patchDoc, useNewtonsoftDeserializer,
            expectTaskCancellation, logQuery, logBody, msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="patchDoc">Patch document for making PATCH requests</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    public static async Task<T?> GenericRestRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true,
        bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        T? result = default;
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && RequestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? System.Text.Json.JsonSerializer.Serialize(postObject) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            using HttpResponseMessage response = await Client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            result = await HandleResponse<T>(response, httpMethod.ToString(), url, useNewtonsoftDeserializer, msgPackOptions);
        }
        catch (TaskCanceledException tcex)
        {
            string exceptionLocation = tcex.GetLocationOfException();
            if (expectTaskCancellation)
            {
                logger.Info("{msg}", $"Task was expectedly canceled for {httpMethod.ToString().ToUpper()} request to {url}");
            }
            else
            {
                logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {url}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {url}");
        }
        return result;
    }

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public static Task<RestObject<T>> GetRestObject<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static Task<RestObject<T>> PostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result in string format inside of a RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>String resulting from the POST request - Null if not success</returns>
    public static Task<RestObject<string?>> StringPostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<string?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result request RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static Task<RestObject<T>> DeleteRestObjectRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a PATCH request against the provided URL with the patchDoc in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="patchDoc">Patch document for PATCH requests</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <returns>Object of type T resulting from the PATCH request - Null if not success</returns>
    public static Task<RestObject<T>> PatchRestObjectRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, HttpContent>(url, HttpMethod.Patch, default, bearerToken, timeout, httpHeaders, patchDoc, useNewtonsoftDeserializer, expectTaskCancellation, logQuery, logBody, msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="patchDoc">Patch document for making PATCH requests</param>
    /// <param name="useNewtonsoftDeserializer">When true, Newtonsoft.Json will be used to deserialize the response instead of system.Text.Json</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static async Task<RestObject<T>> GenericRestObjectRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true,
        bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        RestObject<T> restObject = new();
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && RequestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? System.Text.Json.JsonSerializer.Serialize(postObject) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            restObject.Response = await Client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            restObject.Result = await HandleResponse<T>(restObject.Response, httpMethod.ToString(), url, useNewtonsoftDeserializer, msgPackOptions, httpHeaders);
        }
        catch (TaskCanceledException tcex)
        {
            string exceptionLocation = tcex.GetLocationOfException();
            if (expectTaskCancellation)
            {
                logger.Info("{msg}", $"Task was expectedly canceled for {httpMethod.ToString().ToUpper()} request to {url}");
            }
            else
            {
                logger.Error(tcex, "{msg}", $"{exceptionLocation} Error URL: {url}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error URL: {url}");
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
    private static async Task<T?> HandleResponse<T>(HttpResponseMessage response, string httpMethod, string url, bool useNewtonsoftDeserializer, MsgPackOptions? msgPackOptions = null, Dictionary<string, string>? httpHeaders = null)
    {
        T? result = default;
        try
        {
            string? contentType = response.Content.Headers.ContentType?.ToString();
            string? contentEncoding = response.Content.Headers.ContentEncoding?.ToString();

            await using Stream responseStream = await response.Content.ReadAsStreamAsync();
            if (response.IsSuccessStatusCode)
            {
                result = await ReadResponseStream<T>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, msgPackOptions);
            }
            else
            {
                //string errorMessage = response.Content.Headers.ContentType.ToNString().ContainsInvariant("json") ?
                //    JToken.Parse(await response.Content.ReadAsStringAsync()).ToString(Formatting.Indented) :
                //    await response.Content.ReadAsStringAsync();
                string? errorMessage = await ReadResponseStream<string>(responseStream, contentType, contentEncoding, useNewtonsoftDeserializer, msgPackOptions);
                logger.Warn("{msg}", $"{httpMethod.ToUpper()} request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n{errorMessage}\n{(httpHeaders != null ? $"Headers: {string.Join(", ", httpHeaders.Select(x => $"{x.Key}: {x.Value}"))}" : null)}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return result;
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
                        result = await MessagePackSerializer.DeserializeAsync<T>(responseStream, messagePackOptions);
                    }
                    else
                    {
                        result = await MessagePackSerializer.DeserializeAsync<T>(responseStream);
                    }
                }
                else if (contentType.StrEq(ContentTypes.MemPack)) //NOTE:: Will fail if trying to deserialize null value, ensure NoContent is sent back for nulls
                {
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        //await using GZipStream gzipStream = new(responseStream, CompressionMode.Decompress);
                        //await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                        //gzipStream.CopyTo(outputStream);
                        //outputStream.Position = 0;

                        await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip);
                        result = MemoryPackSerializer.Deserialize<T>(new(outputStream.ToArray())); //Access deserialize decompressed data from outputStream
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        BrotliDecompressor decompressor = new();
                        ReadOnlySequence<byte> decompressedBuffer = decompressor.Decompress(new ReadOnlySpan<byte>(await responseStream.ReadStreamAsync()));
                        result = MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
                    }
                    else
                    {
                        result = await MemoryPackSerializer.DeserializeAsync<T>(responseStream);
                    }
                }
                else if (contentType.ContainsInvariant("json"))//Assume JSON
                {
                    //Deserialize as stream - More memory efficient than string deserialization
                    await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip);
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Brotli);
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
                        result = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(responseStream);
                    }
                }
                else if (contentType.ContainsInvariant("text")) //String encoding (error usually)
                {
                    await using MemoryStream outputStream = new(); //Decompressed data will be written to this stream
                    if (contentEncoding.StrEq(EncodingTypes.GZip))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Gzip);
                    }
                    else if (contentEncoding.StrEq(EncodingTypes.Brotli))
                    {
                        await responseStream.DecompressStream(outputStream, ECompressionType.Brotli);
                    }
                    using StreamReader reader = new(outputStream, Encoding.UTF8);
                    string stringResult = reader.ReadToEnd();
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

    /// <summary>
    /// Adds content to HTTP request if not using GET HTTP request method
    /// </summary>
    /// <typeparam name="T">Type of the post object being added to the HTTP request content</typeparam>
    /// <param name="httpRequestMessage">HTTP request to add content to</param>
    /// <param name="httpMethod">HTTP request method</param>
    /// <param name="httpHeaders">Headers used in the HTTP request</param>
    /// <param name="postObject">Object to add as the content (POST and PUT only)</param>
    /// <param name="patchDoc">Patch document for PATCH requests</param>
    private static void AddContent<T>(this HttpRequestMessage httpRequestMessage, HttpMethod httpMethod, Dictionary<string, string>? httpHeaders = null, T? postObject = default, HttpContent? patchDoc = null)
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
    private static void AttachHeaders(this HttpRequestMessage httpRequestMessage, string? bearerToken, Dictionary<string, string>? httpHeaders)
    {
        //Changed this from inline if due to setting .Authorization to null if bearerToken is empty/null resulting in an exception during the post request: "A task was canceled"
        if (bearerToken != null || bearerToken?.Length == 0 && !(httpHeaders?.Where(x => x.Key.StrEq("Authorization")).Any() ?? false))
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
