using System.Collections.Immutable;
using System.Net;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Requests.Rest.Options;
using NLog;
using static System.Net.HttpStatusCode;
using static CommonNetFuncs.Compression.Streams;
using static CommonNetFuncs.Core.Random;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static CommonNetFuncs.Web.Requests.Rest.RestHelperConstants;

namespace CommonNetFuncs.Web.Requests.Rest.RestHelperWrapper;

internal static class WrapperHelpers
{
	private static readonly Logger logger = LogManager.GetCurrentClassLogger();
	private static readonly ImmutableDictionary<string, string> MemPackHeadersWithGzip = ImmutableDictionary.CreateRange([MemPackContentHeader, MemPackAcceptHeader, GzipEncodingHeader]);
	private static readonly ImmutableDictionary<string, string> MemPackHeadersWithBrotli = ImmutableDictionary.CreateRange([MemPackContentHeader, MemPackAcceptHeader, BrotliEncodingHeader]);

	private static readonly ImmutableDictionary<string, string> MsgPackHeadersWithGzip = ImmutableDictionary.CreateRange([MsgPackContentHeader, MsgPackAcceptHeader, GzipEncodingHeader]);
	private static readonly ImmutableDictionary<string, string> MsgPackHeadersWithBrotli = ImmutableDictionary.CreateRange([MsgPackContentHeader, MsgPackAcceptHeader, BrotliEncodingHeader]);

	private static readonly ImmutableDictionary<string, string> JsonHeadersWithGzip = ImmutableDictionary.CreateRange([JsonContentHeader, JsonAcceptHeader, GzipEncodingHeader]);
	private static readonly ImmutableDictionary<string, string> JsonHeadersWithBrotli = ImmutableDictionary.CreateRange([JsonContentHeader, JsonAcceptHeader, BrotliEncodingHeader]);

	/// <summary>
	/// Populates an existing dictionary with headers instead of creating a new one (for use with pooling).
	/// </summary>
	internal static void PopulateHeaders(Dictionary<string, string> headers, RestHelperOptions options, bool isStreaming)
	{
		// Add custom headers from options
		if (options.HttpHeaders?.Count > 0)
		{
			foreach (KeyValuePair<string, string> header in options.HttpHeaders)
			{
				headers[header.Key] = header.Value;
			}
		}

		// Add compression headers
		SetCompressionHttpHeadersInPlace(headers, options.CompressionOptions, isStreaming);
	}

	/// <summary>
	/// Populates existing dictionary with compression headers instead of creating a new one.
	/// </summary>
	private static void SetCompressionHttpHeadersInPlace(Dictionary<string, string> headers, CompressionOptions? compressionOptions = null, bool isStreaming = false)
	{
		IEnumerable<KeyValuePair<string, string>>? compressionHeaders = null;

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
							compressionHeaders = MemPackHeadersWithGzip;
						}
						else if (compressionOptions.UseMsgPack)
						{
							compressionHeaders = MsgPackHeadersWithGzip;
						}
						else
						{
							compressionHeaders = JsonHeadersWithGzip;
						}
					}
					else if (compressionOptions.CompressionType == ECompressionType.Brotli)
					{
						if (compressionOptions.UseMemPack)
						{
							compressionHeaders = MemPackHeadersWithBrotli;
						}
						else if (compressionOptions.UseMsgPack)
						{
							compressionHeaders = MsgPackHeadersWithBrotli;
						}
						else
						{
							compressionHeaders = JsonHeadersWithBrotli;
						}
					}
					else
					{
						if (compressionOptions.UseMemPack)
						{
							compressionHeaders = MemPackHeadersWithGzip;
						}
						else if (compressionOptions.UseMsgPack)
						{
							compressionHeaders = MsgPackHeadersWithGzip;
						}
						else
						{
							compressionHeaders = JsonHeadersWithGzip;
						}
					}
				}
				else
				{
					if (compressionOptions.UseMemPack)
					{
						compressionHeaders = MemPackHeaders;
					}
					else if (compressionOptions.UseMsgPack)
					{
						compressionHeaders = MsgPackHeaders;
					}
					else
					{
						compressionHeaders = JsonHeaders;
					}
				}
			}
		}
		else
		{
			compressionHeaders = JsonNoEncodeHeaders; //Need to use JSON with no compression when streaming data
		}

		// Add compression headers to the existing dictionary if they don't already exist
		if (compressionHeaders != null)
		{
#pragma warning disable S3267 // Loops should be simplified using the "Where" LINQ method
			foreach (KeyValuePair<string, string> header in compressionHeaders)
			{
				if (!headers.ContainsKey(header.Key))
				{
					headers[header.Key] = header.Value;
				}
			}
#pragma warning restore S3267 // Loops should be simplified using the "Where" LINQ method
		}
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
		options.HttpHeaders ??= new Dictionary<string, string>();
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

	internal static RequestOptions<T> GetRequestOptions<T>(RestHelperOptions options, Uri? baseAddress, IDictionary<string, string> headers, HttpMethod httpMethod, string? bearerToken, T? postObject = default, HttpContent? patchDocument = null)
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
