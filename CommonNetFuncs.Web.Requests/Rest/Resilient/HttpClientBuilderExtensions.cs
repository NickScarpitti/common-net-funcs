using System.Net;
using CommonNetFuncs.Core.CollectionClasses;
using CommonNetFuncs.Web.Requests.Rest.Options;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using static System.Net.HttpStatusCode;

namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

public static class HttpClientBuilderExtensions
{
    private static readonly FixedLRUDictionary<ResilienceOptionsKey, ResiliencePipeline<HttpResponseMessage>> PipelineCache = new(100);

    public static IHttpClientBuilder AddRestHelperResilience(this IHttpClientBuilder builder, Action<ResilienceOptions>? configureOptions = null)
    {
        ResilienceOptions options = new();
        configureOptions?.Invoke(options);

        // Create cache key based on options
        ResilienceOptionsKey cacheKey = CreateCacheKey(options);

        ResiliencePipeline<HttpResponseMessage> pipeline = PipelineCache.GetOrAdd(cacheKey, _ => BuildPipeline(options));

        return builder.AddHttpMessageHandler(() => new ResiliencePipelineHandler(pipeline));
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(ResilienceOptions options)
    {
        ResiliencePipelineBuilder<HttpResponseMessage> builder = new();

        // Add timeout if specified
        if (options.TimeoutValue.HasValue && options.TimeoutValue > TimeSpan.Zero)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = options.TimeoutValue.Value
            });
        }

        // Add retry if not RunOnce
        if (!options.RunOnce)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = options.MaxRetry,
                Delay = options.RetryDelay,
                BackoffType = options.DelayBackoffType,
                UseJitter = options.UseJitter,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(response => options.ShouldRetryFunc == null ?
                        ShouldRetry(response, options) :
                        options.ShouldRetryFunc(response, options))
            });
        }

        return builder.Build();
    }

    private static ResilienceOptionsKey CreateCacheKey(ResilienceOptions options)
    {
        return new ResilienceOptionsKey(options);
    }

    public readonly struct ResilienceOptionsKey(ResilienceOptions options) : IEquatable<ResilienceOptionsKey>
    {
        private readonly int hashCode = HashCode.Combine(options.MaxRetry, options.RetryDelay, options.DelayBackoffType, options.TimeoutValue, options.UseJitter, options.RunOnce,
            options.NullOk, options.ShouldRetryByStatusFunc?.Method);

        public bool Equals(ResilienceOptionsKey other)
        {
            return hashCode == other.hashCode;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object? obj)
        {
            return obj is ResilienceOptionsKey resilienceOptionsKey && Equals(resilienceOptionsKey);
        }

        public static bool operator ==(ResilienceOptionsKey left, ResilienceOptionsKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResilienceOptionsKey left, ResilienceOptionsKey right)
        {
            return !(left == right);
        }
    }

    private static bool ShouldRetry(HttpResponseMessage response, ResilienceOptions options)
    {
        // Don't retry on Unauthorized if RunOnce is true
        if (options.RunOnce && response.StatusCode == Unauthorized)
        {
            return false;
        }

        // If response is successful, don't retry
        if (response.IsSuccessStatusCode)
        {
            return false;
        }

        // Handle null/empty content based on NullOk setting
        if (!options.NullOk && response.Content?.Headers?.ContentLength == 0)
        {
            return true; // Retry if we don't allow null and got empty content
        }

        // Standard retry conditions for non-success status codes
        if (options.ShouldRetryByStatusFunc == null)
        {
            return ShouldRetryBasedOnStatusCode(response.StatusCode);
        }

        return options.ShouldRetryByStatusFunc(response.StatusCode);
    }

    public static bool ShouldRetryBasedOnStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
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
            Unauthorized => false,
            Forbidden => false,
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
}
