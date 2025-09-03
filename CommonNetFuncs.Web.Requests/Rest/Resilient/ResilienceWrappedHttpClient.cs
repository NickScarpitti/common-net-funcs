using System.Net;
using CommonNetFuncs.Web.Requests.Rest.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using static System.Net.HttpStatusCode;

namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

public sealed class ResilienceWrappedHttpClient : IDisposable
{
    private readonly ResiliencePipeline? customPipeline;
    private bool disposed;

    public ResilienceWrappedHttpClient(HttpClient httpClient, ResilienceOptions? resilienceOptions = null)
    {
        UnderlyingClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Build custom resilience pipeline if options provided
        if (resilienceOptions != null)
        {
            ResiliencePipelineBuilder pipelineBuilder = new();

            pipelineBuilder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = resilienceOptions.MaxRetry,
                Delay = resilienceOptions.RetryDelay,
                BackoffType = resilienceOptions.DelayBackoffType,
                UseJitter = resilienceOptions.UseJitter,
                ShouldHandle = args =>
                {
                    // Handle exceptions
                    if (args.Outcome.Exception != null)
                    {
                        return new ValueTask<bool>(
                            args.Outcome.Exception is HttpRequestException ||
                            args.Outcome.Exception is TaskCanceledException ||
                            args.Outcome.Exception is TimeoutRejectedException);
                    }

                    // Handle HTTP response results - cast the result to HttpResponseMessage
                    if (args.Outcome.Result is HttpResponseMessage response)
                    {
                        bool shouldRetry = resilienceOptions.ShouldRetryByStatusFunc?.Invoke(response.StatusCode) ?? IsTransientHttpStatusCode(response.StatusCode);
                        return new ValueTask<bool>(shouldRetry);
                    }

                    return new ValueTask<bool>(false);
                }
            });

            // Add timeout if specified
            if (resilienceOptions.TimeoutValue.HasValue)
            {
                pipelineBuilder.AddTimeout(resilienceOptions.TimeoutValue.Value);
            }

            customPipeline = pipelineBuilder.Build();
        }
    }

    // Expose the underlying HttpClient
    public HttpClient UnderlyingClient { get; }

    // Wrapper method that applies custom resilience to existing static methods
    public async Task<T?> ExecuteWithResilience<T>(Func<HttpClient, CancellationToken, Task<T?>> operation, CancellationToken cancellationToken = default)
    {
        if (customPipeline != null)
        {
            return await customPipeline.ExecuteAsync(async (_) => await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false);
        }
    }

    // Wrapper method for streaming operations
    public async Task<IAsyncEnumerable<T?>> ExecuteStreamingWithResilience<T>(Func<HttpClient, CancellationToken, Task<IAsyncEnumerable<T?>>> operation, CancellationToken cancellationToken = default)
    {
        if (customPipeline != null)
        {
            return await customPipeline.ExecuteAsync(async (_) => await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransientHttpStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == RequestTimeout ||
               statusCode == TooManyRequests ||
               statusCode == InternalServerError ||
               statusCode == BadGateway ||
               statusCode == ServiceUnavailable ||
               statusCode == GatewayTimeout;
    }

    ~ResilienceWrappedHttpClient()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            // Don't dispose HttpClient - it's managed by HttpClientFactory
            disposed = true;
        }
    }
}
