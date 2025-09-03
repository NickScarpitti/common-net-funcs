using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Common;
using CommonNetFuncs.Web.Requests.Rest.Options;
using Microsoft.AspNetCore.JsonPatch;
using NLog;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;
using static Newtonsoft.Json.JsonConvert;

namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

public sealed class RestHelpersResilient(IHttpClientFactory httpClientFactory)
{
    #region GET Methods

    public async Task<T?> Get<T>(RestHelperOptions options, CancellationToken cancellationToken = default)
    {
        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);

        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, false);
        return await wrappedClient.ExecuteWithResilience(async (client, _) =>
               {
                   logger.Info("{msg}", $"GET {options.Url} Attempt {attemptCounter + 1}");
                   bool forceRefresh = attemptCounter > 0;
                   Interlocked.Add(ref attemptCounter, 1);
                   RequestOptions<T> baseRequestOptions = new()
                    {
                        Url = $"{client.BaseAddress}{options.Url}",
                        HttpMethod = HttpMethod.Get,
                        BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                        Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                        HttpHeaders = headers,
                        JsonSerializerOptions = options.JsonSerializerOptions,
                        UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                        ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                        LogQuery = options.LogQuery,
                        MsgPackOptions = options.MsgPackOptions,
                    };

                   RestObject<T>? result = await client.RestObjectRequest<T, T>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

                   if (result == null)
                   {
                       return default;
                   }
                   return result.Result;
               }, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<T?> GetStreaming<T>(RestHelperOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add JSON headers for streaming
        options.HttpHeaders ??= [];
        foreach (KeyValuePair<string, string> header in JsonHeaders)
        {
            options.HttpHeaders.TryAdd(header.Key, header.Value);
        }

        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, true);
        IAsyncEnumerable<T?>? streamingResult =
            await wrappedClient.ExecuteStreamingWithResilience(async (client, _) =>
            {
                logger.Info("{msg}", $"GET (Streaming) {options.Url} Attempt {attemptCounter + 1}");
                bool forceRefresh = attemptCounter > 0;
                Interlocked.Add(ref attemptCounter, 1);

                RequestOptions<T> baseRequestOptions = new()
                {
                    Url = $"{client.BaseAddress}{options.Url}",
                    HttpMethod = HttpMethod.Get,
                    BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                    Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                    HttpHeaders = headers,
                    JsonSerializerOptions = options.JsonSerializerOptions,
                    UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                    ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                    LogQuery = options.LogQuery,
                    MsgPackOptions = options.MsgPackOptions
                };

                StreamingRestObject<T>? result = await client.StreamingRestObjectRequest<T, T>(baseRequestOptions, cancellationToken).ConfigureAwait(false);
                return result?.Result ?? AsyncEnumerable.Empty<T?>();
            }, cancellationToken).ConfigureAwait(false);

        if (streamingResult != null)
        {
            await foreach (T? item in streamingResult)
            {
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

    public async Task<T?> PostRequest<T>(RestHelperOptions options, T postObject, CancellationToken cancellationToken = default)
    {
        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, false);
        return await wrappedClient.ExecuteWithResilience(async (client, _) =>
               {
                   logger.Info("{msg}", $"POST {options.Url} Attempt {attemptCounter + 1}");
                   bool forceRefresh = attemptCounter > 0;
                   Interlocked.Add(ref attemptCounter, 1);

                   RequestOptions<T> baseRequestOptions = new()
                   {
                       Url = $"{client.BaseAddress}{options.Url}",
                       HttpMethod = HttpMethod.Post,
                       BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                       Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                       HttpHeaders = headers,
                       JsonSerializerOptions = options.JsonSerializerOptions,
                       UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                       ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                       LogQuery = options.LogQuery,
                       LogBody = options.LogBody,
                       MsgPackOptions = options.MsgPackOptions,
                       BodyObject = postObject,
                   };

                   RestObject<T>? result = await client.RestObjectRequest<T, T>(baseRequestOptions, cancellationToken);

                   if (result == null)
                   {
                       return default;
                   }
                   return result.Result;
               }, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<T?> PostRequestStreaming<T>(RestHelperOptions options, T postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add JSON headers for streaming POST
        options.HttpHeaders ??= [];
        foreach (KeyValuePair<string, string> header in JsonHeaders)
        {
            options.HttpHeaders.TryAdd(header.Key, header.Value);
        }

        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, true);
        IAsyncEnumerable<T?>? streamingResult =
            await wrappedClient.ExecuteStreamingWithResilience(async (client, _) =>
            {
                logger.Info("{msg}", $"POST (Streaming) {options.Url} Attempt {attemptCounter + 1}");
                bool forceRefresh = attemptCounter > 0;
                Interlocked.Add(ref attemptCounter, 1);

                RequestOptions<T> baseRequestOptions = new()
                {
                    Url = $"{client.BaseAddress}{options.Url}",
                    HttpMethod = HttpMethod.Post,
                    BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                    Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                    HttpHeaders = headers,
                    JsonSerializerOptions = options.JsonSerializerOptions,
                    UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                    ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                    LogQuery = options.LogQuery,
                    LogBody = options.LogBody,
                    MsgPackOptions = options.MsgPackOptions,
                    BodyObject = postObject,
                };

                StreamingRestObject<T>? result = await client.StreamingRestObjectRequest<T, T>(baseRequestOptions, cancellationToken).ConfigureAwait(false);
                return result?.Result ?? AsyncEnumerable.Empty<T?>();
            }, cancellationToken).ConfigureAwait(false);

        if (streamingResult != null)
        {
            await foreach (T? item in streamingResult)
            {
                yield return item;
            }
        }
        else
        {
            yield break;
        }
    }

    public async Task<T?> GenericPostRequest<T, UT>(RestHelperOptions options, UT postObject, CancellationToken cancellationToken = default)
    {
        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, false);
        return await wrappedClient.ExecuteWithResilience(async (client, _) =>
               {
                   logger.Info("{msg}", $"POST {options.Url} Attempt {attemptCounter + 1}");
                   bool forceRefresh = attemptCounter > 0;
                   Interlocked.Add(ref attemptCounter, 1);

                   RequestOptions<UT> baseRequestOptions = new()
                   {
                       Url = $"{client.BaseAddress}{options.Url}",
                       HttpMethod = HttpMethod.Post,
                       BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                       Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                       HttpHeaders = headers,
                       JsonSerializerOptions = options.JsonSerializerOptions,
                       UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                       ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                       LogQuery = options.LogQuery,
                       LogBody = options.LogBody,
                       MsgPackOptions = options.MsgPackOptions,
                       BodyObject = postObject,
                   };

                   RestObject<T>? result = await client.RestObjectRequest<T, UT>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

                   if (result == null)
                   {
                       return default;
                   }
                   return result.Result;
               }, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<T?> GenericPostRequestStreaming<T, UT>(RestHelperOptions options, UT postObject, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Add JSON headers for streaming POST
        options.HttpHeaders ??= [];
        foreach (KeyValuePair<string, string> header in JsonHeaders)
        {
            options.HttpHeaders.TryAdd(header.Key, header.Value);
        }

        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, true);
        IAsyncEnumerable<T?>? streamingResult =
            await wrappedClient.ExecuteStreamingWithResilience(async (client, _) =>
            {
                logger.Info("{msg}", $"POST (Streaming) {options.Url} Attempt {attemptCounter + 1}");
                bool forceRefresh = attemptCounter > 0;
                Interlocked.Add(ref attemptCounter, 1);

                RequestOptions<UT> baseRequestOptions = new()
                {
                    Url = $"{client.BaseAddress}{options.Url}",
                    HttpMethod = HttpMethod.Post,
                    BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                    Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                    HttpHeaders = headers,
                    JsonSerializerOptions = options.JsonSerializerOptions,
                    UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                    ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                    LogQuery = options.LogQuery,
                    LogBody = options.LogBody,
                    MsgPackOptions = options.MsgPackOptions,
                    BodyObject = postObject,
                };

                StreamingRestObject<T>? result = await client.StreamingRestObjectRequest<T, UT>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

                return result?.Result ?? AsyncEnumerable.Empty<T?>();
            }, cancellationToken).ConfigureAwait(false);
        if (streamingResult != null)
        {
            await foreach (T? item in streamingResult)
            {
                yield return item;
            }
        }
        else
        {
            yield break;
        }
    }

    public async Task<string?> StringPostRequest<T>(RestHelperOptions options, T postObject, CancellationToken cancellationToken = default)
    {
        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);

        int attemptCounter = 0;
        Dictionary<string, string> headers = GetHeaders(options, false);
        return await wrappedClient.ExecuteWithResilience(async (client, _) =>
               {
                   logger.Info("{msg}", $"POST {options.Url} Attempt {attemptCounter + 1}");
                   bool forceRefresh = attemptCounter > 0;
                   Interlocked.Add(ref attemptCounter, 1);

                   RequestOptions<T> baseRequestOptions = new()
                   {
                       Url = $"{client.BaseAddress}{options.Url}",
                       HttpMethod = HttpMethod.Post,
                       BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                       Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                       HttpHeaders = headers,
                       JsonSerializerOptions = options.JsonSerializerOptions,
                       UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                       ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                       LogQuery = options.LogQuery,
                       LogBody = options.LogBody,
                       MsgPackOptions = options.MsgPackOptions,
                       BodyObject = postObject,
                   };

                   return (await client.RestObjectRequest<string?, T>(baseRequestOptions, cancellationToken).ConfigureAwait(false)).Result;
               }
        , cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> PopulateBearerToken(RestHelperOptions options, bool forceRefresh = false)
    {
        return options.UseBearerToken ? !options.BearerToken.IsNullOrWhiteSpace() ? options.BearerToken :
        options.ResilienceOptions?.GetBearerTokenFunc != null ? await options.ResilienceOptions.GetBearerTokenFunc(options.ApiName, forceRefresh).ConfigureAwait(false) : null : null;
    }

    #endregion

    #region PATCH Methods

    public async Task<T?> PatchRequest<T>(RestHelperOptions options, T model, T oldModel, CancellationToken cancellationToken = default) where T : class
    {
        using HttpClient baseClient = httpClientFactory.CreateClient(options.ApiName);
        using ResilienceWrappedHttpClient wrappedClient = new(baseClient, options.ResilienceOptions);
        JsonPatchDocument patchDocument = PatchCreator.CreatePatch(oldModel, model);
        if (patchDocument.Operations.Count > 0)
        {
            int attemptCounter = 0;
            Dictionary<string, string> headers = GetHeaders(options, false);
            return await wrappedClient.ExecuteWithResilience(async (client, _) =>
                   {
                       bool forceRefresh = attemptCounter > 0;
                       Interlocked.Add(ref attemptCounter, 1);

                       RequestOptions<T> baseRequestOptions = new()
                       {
                           Url = $"{client.BaseAddress}{options.Url}",
                           HttpMethod = HttpMethod.Patch,
                           BearerToken = await PopulateBearerToken(options, forceRefresh).ConfigureAwait(false),
                           Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
                           HttpHeaders = headers,
                           JsonSerializerOptions = options.JsonSerializerOptions,
                           UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                           ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
                           LogQuery = options.LogQuery,
                           LogBody = options.LogBody,
                           MsgPackOptions = options.MsgPackOptions,
                           PatchDocument = new StringContent(SerializeObject(patchDocument), Encoding.UTF8, ContentTypes.Json),
                       };

                       RestObject<T>? result = await client.RestObjectRequest<T, T>(baseRequestOptions, cancellationToken).ConfigureAwait(false);

                       if (result == null)
                       {
                           return default;
                       }
                       return result.Result;
                   }, cancellationToken).ConfigureAwait(false);
        }
        return model;
    }

    #endregion

    #region Helper Methods

    private static Dictionary<string, string> GetHeaders(RestHelperOptions options, bool isStreaming)
    {
        ConcurrentDictionary<string, string> headers = options.HttpHeaders == null ?
            new ConcurrentDictionary<string, string>() :
            new ConcurrentDictionary<string, string>(options.HttpHeaders);
        return SetCompressionHttpHeaders(headers, options.CompressionOptions, isStreaming).ToDictionary();
    }

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<string, string> MemPackHeadersWithGzip = new([MemPackContentHeader, MemPackAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> MemPackHeadersWithBrotli = new([MemPackContentHeader, MemPackAcceptHeader, BrotliEncodingHeader]);

    private static readonly Dictionary<string, string> MsgPackHeadersWithGzip = new([MsgPackContentHeader, MsgPackAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> MsgPackHeadersWithBrotli = new([MsgPackContentHeader, MsgPackAcceptHeader, BrotliEncodingHeader]);

    private static readonly Dictionary<string, string> JsonHeadersWithGzip = new([JsonContentHeader, JsonAcceptHeader, GzipEncodingHeader]);
    private static readonly Dictionary<string, string> JsonHeadersWithBrotli = new([JsonContentHeader, JsonAcceptHeader, BrotliEncodingHeader]);

    internal static ConcurrentDictionary<string, string> SetCompressionHttpHeaders(ConcurrentDictionary<string, string>? httpHeaders, CompressionOptions? compressionOptions = null, bool isStreaming = false)
    {
        ConcurrentDictionary<string, string>? compressionHeaders = [];
        try
        {
            if (!isStreaming)
            {
                if (compressionOptions?.UseCompression == true)
                {
                    if (compressionOptions.CompressionType != null)
                    {
                        if (compressionOptions.CompressionType == ECompressionType.Gzip)
                        {
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithGzip);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithGzip);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithGzip);
                            }
                        }
                        else if (compressionOptions.CompressionType == ECompressionType.Brotli)
                        {
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithBrotli);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithBrotli);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithBrotli);
                            }
                        }
                        else
                        {
                            // Default to Gzip if unknown compression type
                            if (compressionOptions.UseMemPack)
                            {
                                compressionHeaders = new(MemPackHeadersWithGzip);
                            }
                            else if (compressionOptions.UseMsgPack)
                            {
                                compressionHeaders = new(MsgPackHeadersWithGzip);
                            }
                            else
                            {
                                compressionHeaders = new(JsonHeadersWithGzip);
                            }
                        }
                    }
                    else
                    {
                        if (compressionOptions.UseMemPack)
                        {
                            compressionHeaders = new(MemPackHeaders);
                        }
                        else if (compressionOptions.UseMsgPack)
                        {
                            compressionHeaders = new(MsgPackHeaders);
                        }
                        else
                        {
                            compressionHeaders = new(JsonHeaders);
                        }
                    }
                }
            }
            else
            {
                compressionHeaders = new(JsonNoEncodeHeaders); //Need to use JSON with no compression when streaming data
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (httpHeaders.AnyFast())
        {
            foreach (KeyValuePair<string, string> header in compressionHeaders)
            {
                httpHeaders.AddOrUpdate(header.Key, header.Value, (key, oldValue) => header.Value);
            }
            return httpHeaders ?? [];
        }
        return compressionHeaders;
    }

    #endregion
}

public sealed class CompressionOptions(bool UseCompression = false, ECompressionType? CompressionType = null, bool UseMemPack = false, bool UseMsgPack = false)
{
    public bool UseCompression { get; set; } = UseCompression;

    public ECompressionType? CompressionType { get; set; } = CompressionType;

    public bool UseMemPack { get; set; } = UseMemPack;

    public bool UseMsgPack { get; set; } = UseMsgPack;
}

;
