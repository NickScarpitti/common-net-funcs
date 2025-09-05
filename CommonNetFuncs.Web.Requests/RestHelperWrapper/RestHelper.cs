using CommonNetFuncs.Web.Requests.Rest;
using Microsoft.Extensions.Logging;
using Polly;
using static CommonNetFuncs.Core.Random;
using static CommonNetFuncs.Web.Requests.RestHelperWrapper.Headers;
using static System.Net.HttpStatusCode;

namespace CommonNetFuncs.Web.Requests.RestHelperWrapper;

public sealed class RestHelper(ILogger<RestHelper> logger, IHttpClientFactory httpClientFactory)
{
    //private static readonly RestHelpers restHelpers = new();
    private static readonly MsgPackOptions msgPackOptions = new() { UseMsgPackCompression = true, UseMsgPackUntrusted = true };
    private readonly ILogger<RestHelper> logger = logger;

    public async Task<T?> Get<T>(RestHelperOptions options, CancellationToken cancellationToken = default)
    {
        RestObject<T>? result = null;
        int attempts = 0;
        string? brearerToken = null;
        Dictionary<string, string> headers = GetHeaders(options, false);

        while ((result?.Response == null || !result.Response.IsSuccessStatusCode) && attempts < options.ResilienceOptions?.MaxRetry)
        {
            if (attempts > 0)
            {
                logger.LogInformation("{msg}", $"GET {options.Url} Attempt {attempts + 1}");
            }

            if (options.UseBearerToken)
            {
                if (options.BearerToken != null && attempts == 0)
                {
                    brearerToken = options.BearerToken;
                }
                else if (options.ResilienceOptions.GetBearerTokenFunc != null)
                {
                    brearerToken = await options.ResilienceOptions.GetBearerTokenFunc(options.ApiName, attempts > 0);
                }
            }

            using HttpClient client = httpClientFactory.CreateClient(options.ApiName);
            RequestOptions<T> baseRequestOptions = new()
            {
                Url = $"{client.BaseAddress}{options.Url}",
                HttpMethod = HttpMethod.Get,
                BearerToken = brearerToken,
                Timeout = options.ResilienceOptions.TimeoutValue?.TotalSeconds,
                HttpHeaders = headers,
                JsonSerializerOptions = options.JsonSerializerOptions,
                UseNewtonsoftDeserializer = options.UseNewtonsoftDeserializer,
                ExpectTaskCancellation = options.ResilienceOptions.RunOnce,
                LogQuery = options.LogQuery,
                MsgPackOptions = msgPackOptions
            };

            result = await client.RestObjectRequest<T, T>(baseRequestOptions, cancellationToken);
            if ((options.ResilienceOptions.RunOnce && result.Response?.StatusCode != Unauthorized) || ((options.ResilienceOptions.NullOk || result?.Response != null) && result?.Response?.IsSuccessStatusCode == true))
            {
                break;
            }
            attempts++;

            Thread.Sleep(GetWaitTime(options.ResilienceOptions.DelayBackoffType, options.ResilienceOptions.RetryDelay, options.ResilienceOptions.UseJitter, attempts));
        }

        if (result == null)
        {
            return default;
        }
        return result.Result;
    }

    private static TimeSpan GetWaitTime(DelayBackoffType delayBackoffType, TimeSpan baseRetryDelay, bool useJitter, int attempts)
    {
        TimeSpan waitTime = delayBackoffType switch
        {
            DelayBackoffType.Constant => baseRetryDelay + (useJitter ? TimeSpan.FromMilliseconds(baseRetryDelay.TotalMilliseconds * (GetRandomInt(0, 51) - 25) / 100f) : TimeSpan.FromMilliseconds(0)),
            DelayBackoffType.Linear => baseRetryDelay * attempts,
            DelayBackoffType.Exponential => TimeSpan.FromMilliseconds(Math.Pow(baseRetryDelay.TotalMilliseconds, attempts)),
            _ => baseRetryDelay,
        };

        if (useJitter)
        {
            waitTime += GetJitter(waitTime);
        }

        return waitTime;
    }

    private static TimeSpan GetJitter(this TimeSpan baseRetryDelay)
    {
        return TimeSpan.FromMilliseconds(baseRetryDelay.TotalMilliseconds * (GetRandomInt(0, 51) - 25) / 100f);
    }
}
