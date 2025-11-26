using System.Net;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Requests.Rest.Options;
using NLog;
using static System.Net.HttpStatusCode;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Random;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;


namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

internal static class WrapperHelpers
{
  internal static Dictionary<string, string> GetHeaders(RestHelperOptions options, bool isStreaming)
  {
    Dictionary<string, string> headers = options.HttpHeaders?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>();
    return SetCompressionHttpHeaders(headers, options.CompressionOptions, isStreaming).ToDictionary();
  }

  private static readonly Logger logger = LogManager.GetCurrentClassLogger();
  private static readonly Dictionary<string, string> MemPackHeadersWithGzip = new([MemPackContentHeader, MemPackAcceptHeader, GzipEncodingHeader]);
  private static readonly Dictionary<string, string> MemPackHeadersWithBrotli = new([MemPackContentHeader, MemPackAcceptHeader, BrotliEncodingHeader]);

  private static readonly Dictionary<string, string> MsgPackHeadersWithGzip = new([MsgPackContentHeader, MsgPackAcceptHeader, GzipEncodingHeader]);
  private static readonly Dictionary<string, string> MsgPackHeadersWithBrotli = new([MsgPackContentHeader, MsgPackAcceptHeader, BrotliEncodingHeader]);

  private static readonly Dictionary<string, string> JsonHeadersWithGzip = new([JsonContentHeader, JsonAcceptHeader, GzipEncodingHeader]);
  private static readonly Dictionary<string, string> JsonHeadersWithBrotli = new([JsonContentHeader, JsonAcceptHeader, BrotliEncodingHeader]);

  internal static Dictionary<string, string> SetCompressionHttpHeaders(Dictionary<string, string>? httpHeaders, CompressionOptions? compressionOptions = null, bool isStreaming = false)
  {
    Dictionary<string, string>? compressionHeaders = [];
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
      logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());

    }

    if (httpHeaders.AnyFast())
    {
      foreach (KeyValuePair<string, string> header in compressionHeaders.Where(header => !httpHeaders.ContainsKey(header.Key)))
      {
        httpHeaders.TryAdd(header.Key, header.Value);
      }

      return httpHeaders ?? [];
    }
    return compressionHeaders;
  }

  internal static TimeSpan GetWaitTime(ResilienceOptions resilienceOptions, int attempts)
  {
    TimeSpan waitTime = resilienceOptions.DelayBackoffType switch
    {
      EDelayBackoffType.Constant => resilienceOptions.RetryDelay + (resilienceOptions.UseJitter ? TimeSpan.FromMilliseconds(resilienceOptions.RetryDelay.TotalMilliseconds * (GetRandomInt(0, 51) - 25) / 100f) : TimeSpan.FromMilliseconds(0)),
      EDelayBackoffType.Linear => resilienceOptions.RetryDelay * attempts,
      EDelayBackoffType.Exponential => TimeSpan.FromMilliseconds(Math.Pow(resilienceOptions.RetryDelay.TotalMilliseconds, attempts)),
      _ => resilienceOptions.RetryDelay,
    };

    if (resilienceOptions.UseJitter)
    {
      waitTime += GetJitter(waitTime);
    }

    return waitTime;
  }

  private static TimeSpan GetJitter(TimeSpan baseRetryDelay)
  {
    return TimeSpan.FromMilliseconds(baseRetryDelay.TotalMilliseconds * (GetRandomInt(0, 51) - 25) / 100f);
  }

  internal static async Task<string?> PopulateBearerToken(RestHelperOptions options, int attempts, HttpResponseMessage? lastResponse, string? currentBearerToken)
  {
    if (!options.BearerToken.IsNullOrWhiteSpace() && attempts == 0)
    {
      return options.BearerToken;
    }
    else if (options.ResilienceOptions?.GetBearerTokenFunc != null && ((attempts == 0 && options.BearerToken.IsNullOrWhiteSpace()) || (lastResponse?.StatusCode is Unauthorized or Forbidden))) // Only refresh token if we got a 401 or 403
    {
      return await options.ResilienceOptions.GetBearerTokenFunc(options.ApiName, attempts > 0).ConfigureAwait(false);
    }

    return currentBearerToken;
  }

  internal static void UpdateStreamingHeaders(RestHelperOptions options)
  {
    options.HttpHeaders ??= [];
    options.HttpHeaders[AcceptHeader] = Json; // When streaming, we always want to use JSON
  }

  internal static bool ShouldRetry(HttpResponseMessage? response, ResilienceOptions options)
  {
    if (options.ShouldRetryFunc != null && options.ShouldRetryFunc(response, options))
    {
      return false;
    }
    else
    {
      if ((options.RunOnce && response?.StatusCode != Unauthorized) || ((options.NullOk || response != null) && response?.IsSuccessStatusCode == true))
      {
        return false;
      }

      // If custom retry function is provided, use it
      if (options.ShouldRetryByStatusFunc != null && response != null)
      {
        return options.ShouldRetryByStatusFunc(response.StatusCode);
      }

      // Default retry behavior when no custom function is provided
      if (response != null)
      {
        return ShouldRetryBasedOnStatusCode(response.StatusCode);
      }

      return false;
    }
  }

  private static bool ShouldRetryBasedOnStatusCode(HttpStatusCode statusCode)
  {
    return statusCode switch
    {
      // Auth errors - retry for token refresh
      Unauthorized => true,
      Forbidden => true,

      // 5xx server errors - should retry
      InternalServerError => true,
      BadGateway => true,
      ServiceUnavailable => true,
      GatewayTimeout => true,
      InsufficientStorage => true,
      NetworkAuthenticationRequired => true,

      // 4xx client errors that might be transient
      RequestTimeout => true,
      TooManyRequests => true,

      // Don't retry these 4xx errors
      BadRequest => false,
      NotFound => false,
      MethodNotAllowed => false,
      NotAcceptable => false,
      Conflict => false,
      Gone => false,
      UnprocessableEntity => false,

      // For any other status codes, don't retry by default
      _ => false
    };
  }

  internal static RequestOptions<T> GetRequestOptions<T>(RestHelperOptions options, Uri? baseAddress, Dictionary<string, string> headers, HttpMethod httpMethod, string? bearerToken, T? postObject = default, HttpContent? patchDocument = null)
  {
    RequestOptions<T> baseRequestOptions = new()
    {
      Url = $"{baseAddress}{options.Url}",
      HttpMethod = httpMethod,
      BearerToken = bearerToken,
      Timeout = options.ResilienceOptions?.TimeoutValue?.TotalSeconds,
      HttpHeaders = headers,
      JsonSerializerOptions = options.JsonSerializerOptions,
      UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
      ExpectTaskCancellation = options.ResilienceOptions?.RunOnce ?? false,
      LogQuery = options.LogQuery,
      LogBody = options.LogBody,
      MsgPackOptions = options.MsgPackOptions
    };

    if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
    {
      baseRequestOptions.BodyObject = postObject;
    }

    if (httpMethod == HttpMethod.Patch)
    {
      baseRequestOptions.PatchDocument = patchDocument;
    }

    return baseRequestOptions;
  }
}
