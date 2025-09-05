using System.Net;
using System.Net.Sockets;
using CommonNetFuncs.Core.CollectionClasses;
using CommonNetFuncs.Web.Requests.Rest.Options;
using CommonNetFuncs.Web.Requests.RestHelperWrapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using static System.Net.HttpStatusCode;

namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

public static class HttpClientBuilderExtensions
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private static readonly FixedLRUDictionary<ResilienceOptionsKey, ResiliencePipeline<HttpResponseMessage>> PipelineCache = new(100);

    public static IHttpClientBuilder AddRestHelperResilience(this IHttpClientBuilder builder, Action<ResilienceOptions>? configureOptions = null)
    {
        ResilienceOptions options = new();
        configureOptions?.Invoke(options);

        //// Create cache key based on options
        ResilienceOptionsKey cacheKey = CreateCacheKey(options);

        builder.AddHttpMessageHandler(() => new MethodUrlTrackingHandler());
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
        //if (!options.RunOnce)
        //{
        //    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        //    {
        //        MaxRetryAttempts = options.MaxRetry,
        //        Delay = options.RetryDelay,
        //        BackoffType = options.DelayBackoffType,
        //        UseJitter = options.UseJitter,
        //        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        //            .Handle<HttpRequestException>()
        //            .Handle<OperationCanceledException>(response =>
        //            {
        //                // Differentiate between timeout and cancellation
        //                if (response.InnerException is TimeoutException)
        //                {
        //                    return true; // Treat as timeout
        //                }
        //                return false; // Treat as cancellation, do not retry
        //            })
        //            .Handle<ConnectionResetException>()
        //            .Handle<TaskCanceledException>()
        //            .Handle<TimeoutRejectedException>()
        //            .HandleResult(response => options.ShouldRetryFunc == null ?
        //                ShouldRetry(response, options) :
        //                options.ShouldRetryFunc(response, options)),
        //        OnRetry = args =>
        //        {
        //            int attemptNumber = args.AttemptNumber + 1; // Polly's AttemptNumber is zero-based
        //            string uri = args.Outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "Unknown URI";
        //            args.Outcome.Result?.Dispose(); // Dispose the response if any
        //            logger.Warn(uri == null ? $"Retrying HTTP request - Attempt {attemptNumber}" : $"{uri} - Attempt {attemptNumber}");
        //            return ValueTask.CompletedTask;
        //        }
        //    });
        //}

        //if (!options.RunOnce)
        //{
        //    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        //    {
        //        MaxRetryAttempts = options.MaxRetry,
        //        Delay = options.RetryDelay,
        //        BackoffType = options.DelayBackoffType,
        //        UseJitter = options.UseJitter,
        //        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        //            .Handle<HttpRequestException>()
        //            .Handle<TaskCanceledException>()
        //            .Handle<TimeoutRejectedException>()
        //           .Handle<SocketException>()
        //            .Handle<OperationCanceledException>(response =>
        //            {
        //                // Differentiate between timeout and cancellation
        //                if (response.InnerException is TimeoutException)
        //                {
        //                    return true; // Treat as timeout
        //                }
        //                return false; // Treat as cancellation, do not retry
        //            })
        //            .Handle<ConnectionResetException>()
        //            .HandleResult(response => options.ShouldRetryFunc == null ?
        //                ShouldRetry(response, options) :
        //                options.ShouldRetryFunc(response, options)),
        //        OnRetry = args =>
        //        {
        //            // Get additional context information
        //            string operationType = args.Context.Properties.GetValue(new ResiliencePropertyKey<string>("OperationType"), "Unknown");

        //            // Log retry attempt with delay information
        //            logger.Warn("Retry attempt {attemptNumber} for {operationType} operation. " +
        //                       "Delay: {delay}ms. Outcome: {outcome}",
        //                args.AttemptNumber + 1,
        //                operationType,
        //                args.RetryDelay.TotalMilliseconds,
        //                args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());

        //            return ValueTask.CompletedTask;
        //        }
        //    });

        // Add timeout if specified
        if (options.TimeoutValue.HasValue)
        {
            builder.AddTimeout(options.TimeoutValue.Value);
        }

        // Add retry with URL logging
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
                .Handle<ConnectionResetException>()
                .Handle<SocketException>()
                .Handle<OperationCanceledException>(response =>
                {
                    // Differentiate between timeout and cancellation
                    if (response.InnerException is TimeoutException)
                    {
                        return true; // Treat as timeout
                    }
                    return false; // Treat as cancellation, do not retry
                })
                .HandleResult(response =>
                {
                    if (options.RunOnce)
                    {
                        return false; // No retries
                    }

                    // Check if it's a 401/403 and we have a token refresh function
                    //if ((response.StatusCode is Unauthorized or Forbidden) && options.GetBearerTokenFunc != null)
                    //{
                    //    options.RefreshToken = true;
                    //    return true; // Retry for auth failures
                    //}
                    if (options.GetBearerTokenFunc != null)
                    {
                        options.RefreshToken = true;
                        return true; // Retry for auth failures
                    }

                    return options.ShouldRetryFunc == null ? ShouldRetry(response, options) : options.ShouldRetryFunc(response, options);
                }),
            OnRetry = args =>
            {
                int attemptNumber = args.AttemptNumber + 1;

                // Extract URL from multiple sources
                string requestUrl = ExtractRequestUrl(args);
                string requestMethod = ExtractRequestMethod(args);

                logger?.Warn("Retry attempt {AttemptNumber} for {RequestMethod} {RequestUrl}", attemptNumber, requestMethod, requestUrl);

                // Log failure reason
                if (args.Outcome.Exception != null)
                {
                    logger?.Warn("Retry reason: {ExceptionMessage}",
                        args.Outcome.Exception.Message);
                }
                else if (args.Outcome.Result != null)
                {
                    logger?.Warn("Retry reason: HTTP {StatusCode}", args.Outcome.Result.StatusCode);
                }

                args.Outcome.Result?.Dispose();
                return ValueTask.CompletedTask;
            }
        });

        return builder.Build();
    }

    private static string ExtractRequestUrl(OnRetryArguments<HttpResponseMessage> args)
    {
        // Try to get URL from response's request message
        if (args.Outcome.Result?.RequestMessage?.RequestUri != null)
        {
            return args.Outcome.Result.RequestMessage.RequestUri.ToString();
        }

        // Try to get from request options
        if (args.Outcome.Result?.RequestMessage?.Options != null &&
            args.Outcome.Result.RequestMessage.Options.TryGetValue(new("RequestUrl"), out object? urlObj) &&
            urlObj is string url)
        {
            return url;
        }

        // Fallback
        return "URL not available";
    }

    private static string ExtractRequestMethod(OnRetryArguments<HttpResponseMessage> args)
    {
        // Try to get URL from response's request message
        if (args.Outcome.Result?.RequestMessage?.Method != null)
        {
            return args.Outcome.Result.RequestMessage.Method.ToString();
        }

        // Try to get from request options
        if (args.Outcome.Result?.RequestMessage?.Options != null &&
            args.Outcome.Result.RequestMessage.Options.TryGetValue(new("RequestMethod"), out object? methodObj) &&
            methodObj is string method)
        {
            return method;
        }

        // Fallback
        return "Method not available";
    }

    // Custom resilience handler that passes logger to context
    private class ResilienceHandler(ResiliencePipeline<HttpResponseMessage> pipeline, ILogger? logger) : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline = pipeline;
        private readonly ILogger? _logger = logger;
        private static readonly ResiliencePropertyKey<ILogger> LoggerKey = new("Logger");

        // Fix for CS0411: Specify type arguments explicitly for ExecuteAsync
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ResilienceContext context = ResilienceContextPool.Shared.Get(cancellationToken);

            try
            {
                // Store logger in context for retry callback access
                if (_logger != null)
                {
                    context.Properties.Set(LoggerKey, _logger);
                }

                // Specify type arguments explicitly and use ConfigureAwait(false) to fix CRR0029
                return await _pipeline.ExecuteAsync<HttpResponseMessage, CancellationToken>(
                    async (_, token) => await base.SendAsync(request, token).ConfigureAwait(false), context, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }
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

    internal static bool ShouldRetry(HttpResponseMessage response, ResilienceOptions options)
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

// Add this class to HttpClientBuilderExtensions.cs or a separate file
public class MethodUrlTrackingHandler : DelegatingHandler
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static readonly HttpRequestOptionsKey<string> RequestMethodKey = new("RequestMethod");
    private static readonly HttpRequestOptionsKey<string> RequestUrlKey = new("RequestUrl");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? requestUrl = request.RequestUri?.ToString();
        if (requestUrl != null)
        {
            request.Options.Set(RequestUrlKey, requestUrl);
        }

        string? requestMethod = request.Method.Method;
        if (requestMethod != null)
        {
            request.Options.Set(RequestMethodKey, requestMethod);
        }

        // Log initial attempt
        logger.Info("Starting {Method} request to {Url} - Attempt 1", requestMethod ?? "Unknown Method", requestUrl ?? "Unknown URL");

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Ensure response has request reference
        response.RequestMessage ??= request;

        return response;
    }
}
