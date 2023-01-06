using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using CommonNetCoreFuncs.Tools;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace CommonNetCoreFuncs.Web;

public class RestObject<T> where T : class
{
    public T? Result { get; set; }
    public HttpResponseMessage? Response { get; set; }
}

/// <summary>
/// Helper functions that send requests to specified URI and return resulting values where applicable
/// Source1: https://medium.com/@srikanth.gunnala/generic-wrapper-to-consume-asp-net-web-api-rest-service-641b50462c0
/// Source2: https://stackoverflow.com/questions/43692053/how-can-i-create-a-jsonpatchdocument-from-comparing-two-c-sharp-objects
/// </summary>
/// <typeparam name="T"></typeparam>
public static class RestHelpers<T> where T : class
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    //Use static client here instead of individual using statements to prevent maxing out the number of connections
    private const double DefaultRequestTimeout = 100; //Default timeout for HttpClient
    private static readonly HttpClient client = new() { Timeout = Timeout.InfiniteTimeSpan }; //Use infinite timespan here to force using token specified timeout

    /// <summary>
    /// Executes a GET request against the specified URL and returns the result
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the GET request - Null if not success</returns>
    public static async Task<T?> Get(string url, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        T? result = null;
        try
        {
            logger.Info($"GET URL: {url}");
            HttpResponseMessage response = client.GetAsync(new Uri(url), tokenSource.Token).Result;
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"GET request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Get Error" + $"URL:{url}");
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
    public static async Task<RestObject<T>> GetRestOject(string url, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;
        
        RestObject<T> restObject = new();
        try
        {
            logger.Info($"GET URL: {url}");
            restObject.Response = client.GetAsync(new Uri(url), tokenSource.Token).Result;
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"GET request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Get Error" + $"URL:{url}");
        }
        return restObject;
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
    public static async Task<T?> PostRequest(string url, T? postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        T? result = null;
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            HttpResponseMessage response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "PostRequest Error" + $"URL:{url}");
        }
        return result;
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
    public static async Task<RestObject<T>> PostRestObjectRequest(string url, T? postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        RestObject<T> restObject = new();
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            restObject.Response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "PostRequest Error" + $"URL:{url}");
        }
        return restObject;
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
    public static async Task<T?> GenericPostRequest<UT>(string url, UT postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        T? result = null;
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            HttpResponseMessage response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GenericPostRequest Error" + $"URL:{url}");
        }
        return result;
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
    public static async Task<RestObject<T>> GenericPostRestObjectRequest<UT>(string url, UT postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        RestObject<T> restObject = new();
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            restObject.Response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "GenericPostRequest Error" + $"URL:{url}");
        }
        return restObject;
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
    public static async Task<string?> StringPostRequest(string url, T postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        string? result = null;
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            HttpResponseMessage response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = x.Result;
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "StringPostRequest Error" + $"URL:{url}");
        }
        return result;
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
    public static async Task<RestObject<string>> StringPostRestObjectRequest(string url, T postObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        RestObject<string> restObject = new();
        try
        {
            logger.Info($"POST URL: {url} | {JsonConvert.SerializeObject(postObject)}");
            restObject.Response = await client.PostAsync(url, postObject, new JsonMediaTypeFormatter(), tokenSource.Token).ConfigureAwait(false);
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = x.Result;
                });
            }
            else
            {
                logger.Warn($"POST request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "StringPostRequest Error" + $"URL:{url}");
        }
        return restObject;
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static async Task<T?> DeleteRequest(string url, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        T? result = null;
        try
        {
            logger.Debug($"DELETE URL: {url}");
            HttpResponseMessage response = await client.DeleteAsync(url, tokenSource.Token).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"DELETE request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "DeleteRequest Error" + $"URL:{url}");
        }
        return result;
    }

    /// <summary>
    /// Executes a DELETE request against the provided URL with the deleteObject in the body and returns the result request RestObject
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="postObject">The object to be created</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    /// <exception cref="ObjectDisposedException">Ignore.</exception>
    /// <returns>Object of type T resulting from the DELETE request - Null if not success</returns>
    public static async Task<RestObject<T>> DeleteRestObjectRequest(string url, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        RestObject<T> restObject = new();
        try
        {
            logger.Debug($"DELETE URL: {url}");
            restObject.Response = await client.DeleteAsync(url, tokenSource.Token).ConfigureAwait(false);
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"DELETE request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "DeleteRequest Error" + $"URL:{url}");
        }
        return restObject;
    }

    /// <summary>
    /// Executes a PUT request against the provided URL with the putObject in the body
    /// </summary>
    /// <param name="url">API Url</param>
    /// <param name="putObject">The object to be edited</param>
    /// <param name="bearerToken">Bearer token to add to the request if provided</param>
    /// <param name="timeout">Timeout setting for the request. Defaults to 100s if not provided</param>
    /// <exception cref="HttpRequestException">Ignore.</exception>
    public static async Task PutRequest(string url, T putObject, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        HttpResponseMessage response = await client.PutAsync(url, putObject, new JsonMediaTypeFormatter()).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
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
    public static async Task<T?> PatchRequest(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        T? result = null;
        try
        {
            logger.Debug($"PATCH URL: {url} | {JsonConvert.SerializeObject(patchDoc)}");
            HttpResponseMessage response = await client.PatchAsync(url, patchDoc, tokenSource.Token).ConfigureAwait(false);
            //response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"PATCH request with URL {url} failed with the following response:\n\t{response.StatusCode}: {response.ReasonPhrase}\nContent:\n\t{response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "PatchRequest Error" + $"URL:{url}");
        }
        return result;
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
    public static async Task<RestObject<T>> PatchRestObjectRequest(string url, HttpContent patchDoc, string? bearerToken = null, double? timeout = null)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeout == null || timeout <= 0 ? DefaultRequestTimeout : (double)timeout));
        client.DefaultRequestHeaders.Authorization = !string.IsNullOrWhiteSpace(bearerToken) ? new AuthenticationHeaderValue("Bearer", bearerToken) : null;

        RestObject<T> restObject = new();
        try
        {
            logger.Debug($"PATCH URL: {url} | {JsonConvert.SerializeObject(patchDoc)}");
            restObject.Response = await client.PatchAsync(url, patchDoc, tokenSource.Token).ConfigureAwait(false);
            //response.EnsureSuccessStatusCode();
            if (restObject.Response.IsSuccessStatusCode)
            {
                await restObject.Response.Content.ReadAsStringAsync().ContinueWith((Task<string> x) =>
                {
                    if (x.IsFaulted) throw x.Exception ?? new();
                    restObject.Result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }
            else
            {
                logger.Warn($"PATCH request with URL {url} failed with the following response:\n\t{restObject.Response.StatusCode}: {restObject.Response.ReasonPhrase}\nContent:\n\t{restObject.Response.Content}");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "PatchRequest Error" + $"URL:{url}");
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
            else if ((!(origProp?.Value.ToString(Formatting.None) ?? null).StrEq(modProp?.Value.ToString(Formatting.None)) && origProp?.Value.Type != JTokenType.Date))
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
}
