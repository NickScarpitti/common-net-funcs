using System.Net.Http.Json;
using Common_Net_Funcs.Tools;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Common_Net_Funcs.Tools.DataValidation;
using static Common_Net_Funcs.Tools.DebugHelpers;
using static Newtonsoft.Json.JsonConvert;

namespace Common_Net_Funcs.Web;

/// <summary>
/// Helper class to get around not being able to pass primitive types directly to a generic type
/// </summary>
/// <typeparam name="T">Primitive type to pass to the REST request</typeparam>
public class RestObject<T>// where T : class
{
    public T? Result { get; set; }
    public HttpResponseMessage? Response { get; set; }
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
    private static readonly SocketsHttpHandler SocketsHttpHandler = new() { MaxConnectionsPerServer = 100, KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always, KeepAlivePingDelay = TimeSpan.FromSeconds(15), KeepAlivePingTimeout = TimeSpan.FromMinutes(60)};
    private static readonly HttpClient Client = new(SocketsHttpHandler) { Timeout = Timeout.InfiniteTimeSpan }; //Use infinite timespan here to force using token specified timeout
    private static readonly List<HttpMethod> RequestsWithBody = new() { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch };

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public static async Task<T?> Get<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static async Task<T?> PostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null) where T : class
    {
        return await GenericRestRequest<T?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result in string format
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>String resulting from the POST request - Null if not success</returns>
    public static async Task<string?> StringPostRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null) where T : class
    {
        return await GenericRestRequest<string?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static async Task<T?> DeleteRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestRequest<T?, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a PUT request against the provided URL with the putObject in the body
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="putObject">The object to be edited</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    public static async Task<T?> PutRequest<T>(string url, T putObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestRequest<T?, T>(url, HttpMethod.Put, putObject, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a PATCH request against the provided URL with the patchDoc in the body and returns the result
    /// </summary>
    /// <param name="url"></param>
    /// <param name="patchDoc"></param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <returns>Object of type T resulting from the PATCH request - Null if not success</returns>
    public static async Task<T?> PatchRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestRequest<T?, HttpContent>(url, HttpMethod.Patch, default, bearerToken, timeout, httpHeaders, patchDoc);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static async Task<T?> GenericRestRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null)
    {
        T? result = default;
        try
        {
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            AttachHeaders(bearerToken, httpHeaders, httpRequestMessage);
            logger.Info($"{httpMethod.ToString().ToUpper()} URL: {url}{(RequestsWithBody.Contains(httpMethod) ? $" | {(postObject != null ? SerializeObject(postObject) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty)}");
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                httpRequestMessage.Content = JsonContent.Create(postObject, new("application/json"));
            }
            else if (httpMethod == HttpMethod.Patch)
            {
                httpRequestMessage.Content = patchDoc;
            }
            using HttpResponseMessage response = await Client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();

                    Type returnType = typeof(T);
                    if (returnType == typeof(string) || Nullable.GetUnderlyingType(returnType) == typeof(string))
                    {
                        result = (T)Convert.ChangeType(x.Result, typeof(T)); //Makes it so the result will be accepted as a string in generic terms
                    }
                    else if(x.Result?.Length > 0)
                    {
                        result = DeserializeObject<T>(x.Result);
                    }
                });
            }
            else
            {
                logger.Warn($"{httpMethod.ToString().ToUpper()} request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error URL: {url}");
        }
        return result;
    }

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public static async Task<RestObject<T>> GetRestObject<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestObjectRequest<T, T>(url, HttpMethod.Get, default, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static async Task<RestObject<T>> PostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestObjectRequest<T, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result in string format inside of a RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>String resulting from the POST request - Null if not success</returns>
    public static async Task<RestObject<string?>> StringPostRestObjectRequest<T>(string url, T postObject, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestObjectRequest<string?, T>(url, HttpMethod.Post, postObject, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result request RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static async Task<RestObject<T>> DeleteRestObjectRequest<T>(string url, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestObjectRequest<T, T>(url, HttpMethod.Delete, default, bearerToken, timeout, httpHeaders);
    }

    /// <summary>
    /// Executes a PATCH request against the provided URL with the patchDoc in the body and returns the result
    /// </summary>
    /// <param name="url"></param>
    /// <param name="patchDoc"></param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <returns>Object of type T resulting from the PATCH request - Null if not success</returns>
    public static async Task<RestObject<T>> PatchRestObjectRequest<T>(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null)
    {
        return await GenericRestObjectRequest<T, HttpContent>(url, HttpMethod.Patch, default, bearerToken, timeout, httpHeaders, patchDoc);
    }

    /// <summary>
    /// Executes a POST request against the provided URL with the postObject in the body and returns the result RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the POST request - Null if not success</returns>
    public static async Task<RestObject<T>> GenericRestObjectRequest<T, UT>(string url, HttpMethod httpMethod, UT? postObject = default, string? bearerToken = null, double? timeout = null, Dictionary<string, string>? httpHeaders = null, HttpContent? patchDoc = null)
    {
        RestObject<T> restObject = new();
        try
        {
            using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
            using HttpRequestMessage httpRequestMessage = new(httpMethod, url);
            AttachHeaders(bearerToken, httpHeaders, httpRequestMessage);
            logger.Info($"{httpMethod.ToString().ToUpper()} URL: {url}{(RequestsWithBody.Contains(httpMethod) ? $" | {(postObject != null ? SerializeObject(postObject) : patchDoc?.ReadAsStringAsync().Result)}" : string.Empty)}");
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                httpRequestMessage.Content = JsonContent.Create(postObject, new("application/json"));
            }
            else if (httpMethod == HttpMethod.Patch)
            {
                httpRequestMessage.Content = patchDoc;
            }
            restObject.Response = await Client.SendAsync(httpRequestMessage, tokenSource.Token).ConfigureAwait(false) ?? new();
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();

                    Type returnType = typeof(T);
                    if (returnType == typeof(string) || Nullable.GetUnderlyingType(returnType) == typeof(string))
                    {
                        restObject.Result = (T)Convert.ChangeType(x.Result, typeof(T));
                    }
                    else if (x.Result?.Length > 0)
                    {
                        restObject.Result = DeserializeObject<T>(x.Result);
                    }
                });
            }
            else
            {
                logger.Warn($"{httpMethod.ToString().ToUpper()} request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error URL: {url}");
        }
        return restObject;
    }

    /// <summary>
    /// Converts two like models to JObjects and passes them into the FillPatchForObject method to create a JSON patch document
    /// From Source2
    /// </summary>
    /// <param name="originalObject"></param>
    /// <param name="modifiedObject"></param>
    /// <returns>JsonPatchDocument document of changes from originalObject to modifiedObject</returns>
    public static JsonPatchDocument CreatePatch(object originalObject, object modifiedObject)
    {
        JObject original = JObject.FromObject(originalObject);
        JObject modified = JObject.FromObject(modifiedObject);

        JsonPatchDocument patch = new();
        FillPatchForObject(original, modified, patch, "/");

        return patch;
    }

    /// <summary>
    /// Compares two JObjects together and populates a JsonPatchDocument with the differences
    /// From Source2
    /// </summary>
    /// <param name="orig">Original object to be compared to</param>
    /// <param name="mod">Modified version of the original object</param>
    /// <param name="patch">The json patch document to write the patch instructions to</param>
    /// <param name="path"></param>
    private static void FillPatchForObject(JObject orig, JObject mod, JsonPatchDocument patch, string path)
    {
        string[] origNames = orig.Properties().Select(x => x.Name).ToArray();
        string[] modNames = mod.Properties().Select(x => x.Name).ToArray();

        // Names removed in modified
        foreach (var k in origNames.Except(modNames))
        {
            JProperty? prop = orig.Property(k);
            patch.Remove(path + prop!.Name);
        }

        // Names added in modified
        foreach (var k in modNames.Except(origNames))
        {
            JProperty? prop = mod.Property(k);
            patch.Add(path + prop!.Name, prop.Value);
        }

        // Present in both
        foreach (var k in origNames.Intersect(modNames))
        {
            JProperty? origProp = orig.Property(k);
            JProperty? modProp = mod.Property(k);

            if (origProp?.Value.Type != modProp?.Value.Type)
            {
                patch.Replace(path + modProp?.Name, modProp?.Value);
            }
            else if(origProp?.Value.Type == JTokenType.Float)
            {
                decimal? origDec = null;
                decimal? modDec = null;
                if(decimal.TryParse(origProp?.Value.ToString(Formatting.None), out decimal origDecimal))
                {
                    origDec = origDecimal;
                }
                if (decimal.TryParse(modProp?.Value.ToString(Formatting.None), out decimal modDecimal))
                {
                    modDec = modDecimal;
                }

                if (modDec != origDec)
                {
                    if (origProp?.Value.Type == JTokenType.Object)
                    {
                        // Recurse into objects
                        FillPatchForObject(origProp.Value as JObject ?? new(), modProp?.Value as JObject ?? new(), patch, path + modProp?.Name + "/");
                    }
                    else
                    {
                        // Replace values directly
                        patch.Replace(path + modProp?.Name, modProp?.Value);
                    }
                }
            }
            else if (((origProp?.Value.ToString(Formatting.None) ?? null) != modProp?.Value.ToString(Formatting.None)) && origProp?.Value.Type != JTokenType.Date)
            {
                if (origProp?.Value.Type == JTokenType.Object)
                {
                    // Recurse into objects
                    FillPatchForObject(origProp.Value as JObject ?? new(), modProp?.Value as JObject ?? new(), patch, path + modProp?.Name + "/");
                }
                else
                {
                    // Replace values directly
                    patch.Replace(path + modProp?.Name, modProp?.Value);
                }
            }
            else if (origProp?.Value.Type == JTokenType.Date && modProp?.Value.Type == JTokenType.Date)
            {
                string originalDts = origProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");
                string modDts = modProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");

                bool originalSucceed = DateTime.TryParse(originalDts, out DateTime originalDate);
                bool modSucceed = DateTime.TryParse(modDts, out DateTime modDate);

                if (modSucceed && originalDate != modDate)
                {
                    // Replace values directly
                    patch.Replace(path + modProp.Name, modProp.Value);
                }
            }
        }
    }

    /// <summary>
    /// Attaches headers to client from httpHeaders if applicable, else only attaches authorization.
    /// </summary>
    /// <param name="bearerToken">Token used for bearer authentication</param>
    /// <param name="httpHeaders">Dictionary of headers</param>
    private static void AttachHeaders(string? bearerToken, Dictionary<string, string>? httpHeaders, HttpRequestMessage httpRequestMessage)
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
                logger.Warn(ex, $"Failed to add bearer token.\nDefault headers = {httpRequestMessage.Headers}\nNot validated headers = {httpRequestMessage.Headers.NonValidated}");
            }
        }

        if (httpHeaders?.Any() == true)
        {
            foreach (KeyValuePair<string, string> header in httpHeaders)
            {
                try
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Failed to add header {header.Key} with value {header.Value}.\nDefault headers = {httpRequestMessage.Headers}\nNot validated headers = {httpRequestMessage.Headers.NonValidated}");
                }
            }
        }
    }

    public static (int itemsPerChunk, int numberOfChunks) GetChunkingParameters(int itemCount, int startingitemsPerChunk = 10000)
    {
        //IAsyncEnumerable is limited to MvcOptions.MaxIAsyncEnumerableBufferLimit which is 8192 by default
        int itemsPerChunk = startingitemsPerChunk;
        int numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);

        while (numberOfChunks >= 8192)
        {
            itemsPerChunk += 1000;
            numberOfChunks = (int)MathHelpers.Ceiling((decimal)itemCount / itemsPerChunk, 1);
        }

        return (itemsPerChunk, numberOfChunks);
    }
}
