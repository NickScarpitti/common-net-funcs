using System.Text.Json;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Common;
using NLog;
using static CommonNetFuncs.Web.Requests.RestHelperConstants;
using static CommonNetFuncs.Web.Requests.RestHelpersStatic;

namespace CommonNetFuncs.Web.Requests;

/// <summary>
/// Helper functions that send requests to specified URI and return resulting values where applicable
/// Source1: https://medium.com/@srikanth.gunnala/generic-wrapper-to-consume-asp-net-web-api-rest-service-641b50462c0
/// Source2: https://stackoverflow.com/questions/43692053/how-can-i-create-a-jsonpatchdocument-from-comparing-two-c-sharp-objects
/// </summary>
public class RestHelpersCommon(HttpClient client, JsonSerializerOptions? jsonSerializerOptions = null) //: IAsyncDisposable
{
    public readonly HttpClient client = client;
    public readonly JsonSerializerOptions? jsonSerializerOptions = jsonSerializerOptions;
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    //Use static client here instead of individual using statements to prevent maxing out the number of connections
    private const double DefaultRequestTimeout = 100; //Default timeout for HttpClient
    private static readonly HttpMethod[] requestsWithBody = [HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch];

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
    public Task<T?> Get<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, bool useNewtonsoftDeserializer = false,
        bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public IAsyncEnumerable<T?> Get<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool expectTaskCancellation = false, bool logQuery = true)
    {
        return GenericStreamingRestRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false);
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
    public Task<T?> PostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null) where T : class
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public IAsyncEnumerable<T?> PostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true) where T : class
    {
        return GenericStreamingRestRequest<T?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody);
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
    public Task<string?> StringPostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
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
    public Task<T?> DeleteRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public IAsyncEnumerable<T?> DeleteRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, bool expectTaskCancellation = false, bool logQuery = true)
    {
        return GenericStreamingRestRequest<T?, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false);
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
    public Task<T?> PutRequest<T>(string url, T putObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestRequest<T?, T>(url, HttpMethod.Put, putObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a PUT request against the provided URL with the putObject in the body
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="putObject">The object to be edited</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    public IAsyncEnumerable<T?> PutRequest<T>(string url, T putObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, bool expectTaskCancellation = false,
        bool logQuery = true, bool logBody = true)
    {
        return GenericStreamingRestRequest<T?, T>(url, HttpMethod.Put, putObject, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody);
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
    public Task<T?> PatchRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
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
    public async Task<T?> GenericRestRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true,
        bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        T? result = default;
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && requestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? JsonSerializer.Serialize(postObject, jsonSerializerOptions ?? defaultJsonSerializerOptions) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            using HttpResponseMessage response = await client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
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
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="patchDoc">Patch document for making PATCH requests</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    public async IAsyncEnumerable<T?> GenericStreamingRestRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true)
    {
        IAsyncEnumerator<T?>? enumeratedReader = null;
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && requestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? JsonSerializer.Serialize(postObject, jsonSerializerOptions ?? defaultJsonSerializerOptions) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);

            //Ensure json header is being used
            if (httpHeaders == null) {
                httpHeaders = new(JsonAcceptHeader.SingleToList());
            }
            else if (httpHeaders.Remove(AcceptHeader))
            {
                httpHeaders.AddDictionaryItem(JsonAcceptHeader);
            }

            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            using HttpResponseMessage response = await client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            enumeratedReader = HandleResponseAsync<T>(response, httpMethod.ToString(), url).GetAsyncEnumerator();
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

        if (enumeratedReader != null)
        {
            while (await enumeratedReader.MoveNextAsync())
            {
                yield return enumeratedReader!.Current;
            }
        }
        else
        {
            yield break;
        }
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
    public Task<RestObject<T>> GetRestObject<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public Task<StreamingRestObject<T>> GetRestObject<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool expectTaskCancellation = false, bool logQuery = true)
    {
        return GenericStreamingRestObjectRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false);
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
    public Task<RestObject<T>> PostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public Task<StreamingRestObject<T>> PostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true)
    {
        return GenericStreamingRestObjectRequest<T, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: logBody);
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
    public Task<RestObject<string?>> StringPostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
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
    public Task<RestObject<T>> DeleteRestObjectRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true, MsgPackOptions? msgPackOptions = null)
    {
        return GenericRestObjectRequest<T, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer: useNewtonsoftDeserializer,
            expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false, msgPackOptions: msgPackOptions);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result request RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public Task<StreamingRestObject<T>> DeleteRestObjectRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
        bool expectTaskCancellation = false, bool logQuery = true)
    {
        return GenericStreamingRestObjectRequest<T, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders, expectTaskCancellation: expectTaskCancellation, logQuery: logQuery, logBody: false);
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
    public Task<RestObject<T>> PatchRestObjectRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null,
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
    public async Task<RestObject<T>> GenericRestObjectRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool useNewtonsoftDeserializer = false, bool expectTaskCancellation = false, bool logQuery = true,
        bool logBody = true, MsgPackOptions? msgPackOptions = null)
    {
        RestObject<T> restObject = new();
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && requestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? JsonSerializer.Serialize(postObject, jsonSerializerOptions ?? defaultJsonSerializerOptions) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            restObject.Response = await client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
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
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API URL</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request in seconds. Defaults to 100s if not provided</param>
    /// <param name="httpHeaders">Custom HTTP Headers to send with the request</param>
    /// <param name="patchDoc">Patch document for making PATCH requests</param>
    /// <param name="expectTaskCancellation">If true, will only log info instead of an error when a TaskCanceledException exception is thrown</param>
    /// <param name="logQuery">If true, logger will display the query string of request.</param>
    /// <param name="logBody">If true, logger will display the body of the request.</param>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    public async Task<StreamingRestObject<T>> GenericStreamingRestObjectRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null,
        Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null, bool expectTaskCancellation = false, bool logQuery = true, bool logBody = true)
    {
        StreamingRestObject<T> restObject = new();
        try
        {
            logger.Info("{msg}", $"{httpMethod.ToString().ToUpper()} URL: {(logQuery ? url : url.GetRedactedUri())}" + (logBody && requestsWithBody.Contains(httpMethod) ?
                $" | {(postObject != null ? JsonSerializer.Serialize(postObject, jsonSerializerOptions ?? defaultJsonSerializerOptions) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty));
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);

            //Ensure json header is being used
            if (httpHeaders == null)
            {
                httpHeaders = new(JsonAcceptHeader.SingleToList());
            }
            else if (httpHeaders.Remove(AcceptHeader))
            {
                httpHeaders.AddDictionaryItem(JsonAcceptHeader);
            }

            httpRequestMessage.AttachHeaders(bearerToken, httpHeaders);
            httpRequestMessage.AddContent(httpMethod, httpHeaders, postObject, patchDoc);

            restObject.Response = await client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            restObject.Result = HandleResponseAsync<T>(restObject.Response, httpMethod.ToString(), url);
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

    //private bool disposed;
    //// Implement IAsyncDisposable pattern
    //protected virtual ValueTask DisposeAsyncCore()
    //{
    //    // Since HttpClient only implements IDisposable, we'll use synchronous disposal
    //    client?.Dispose();
    //    return ValueTask.CompletedTask;
    //}

    //public async ValueTask DisposeAsync()
    //{
    //    if (!disposed)
    //    {
    //        disposed = true;
    //        await DisposeAsyncCore().ConfigureAwait(false);
    //        GC.SuppressFinalize(this);
    //    }
    //}

    //protected void ThrowIfDisposed()
    //{
    //    if (!disposed)
    //    {
    //        return;
    //    }
    //    throw new ObjectDisposedException(nameof(RestHelpersCommon));
    //}
}

public class RestHelpersCommonFactory(IHttpClientFactory httpClientFactory, string? clientName = null) : RestHelpersCommon(clientName.IsNullOrWhiteSpace() ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(clientName))
{
}
//public class RestHelpersCommonFactory(IHttpClientFactory httpClientFactory, string? clientName = null)
//{
//    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
//    private readonly string clientName = clientName ?? string.Empty;

//    private RestHelpersCommon CreateHelper()
//    {
//        HttpClient client = string.IsNullOrEmpty(clientName) ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(clientName);
//        return new RestHelpersCommon(client);
//    }

//    public async Task<T?> Get<T>(string url, string? bearerToken = null, double? timeout = null,
//        Dictionary<string, string>? httpHeaders = null, bool useNewtonsoftDeserializer = false,
//        bool expectTaskCancellation = false, bool logQuery = true,
//        MsgPackOptions? msgPackOptions = null)
//    {
//        await using RestHelpersCommon helper = CreateHelper();
//        return await helper.Get<T>(url, bearerToken, timeout, httpHeaders, useNewtonsoftDeserializer,
//            expectTaskCancellation, logQuery, msgPackOptions);
//    }

//    // Implement other methods similarly, delegating to a new RestHelpersCommon instance
//}
