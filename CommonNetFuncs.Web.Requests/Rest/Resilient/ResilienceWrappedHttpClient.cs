//using System.Net;
//using System.Net.Sockets;
//using CommonNetFuncs.Web.Requests.Rest.Options;
//using Microsoft.AspNetCore.Connections;
//using NLog;
//using Polly;
//using Polly.Retry;
//using Polly.Timeout;
//using static CommonNetFuncs.Web.Requests.Rest.Resilient.HttpClientBuilderExtensions;
//using static System.Net.HttpStatusCode;

//namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

//public sealed class ResilienceWrappedHttpClient : IDisposable
//{
//    private readonly ResiliencePipeline? customPipeline;
//    private bool disposed;

//    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

//    public ResilienceWrappedHttpClient(HttpClient httpClient, ResilienceOptions? resilienceOptions = null, string? attemptLogBaseString = null)
//    {
//        UnderlyingClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

//        // Build custom resilience pipeline if options provided
//        if (resilienceOptions != null)
//        {
//            ResiliencePipelineBuilder pipelineBuilder = new();

//            pipelineBuilder.AddRetry(new RetryStrategyOptions
//            {
//                MaxRetryAttempts = resilienceOptions.MaxRetry,
//                Delay = resilienceOptions.RetryDelay,
//                BackoffType = resilienceOptions.DelayBackoffType,
//                UseJitter = resilienceOptions.UseJitter,
//                ShouldHandle = args =>
//                {
//                    if (resilienceOptions.RunOnce)
//                    {
//                        return new ValueTask<bool>(false); // No retries
//                    }

//                    HttpStatusCode? statusCode = null;
//                    if (args.Outcome.Result is HttpResponseMessage response)
//                    {
//                        statusCode = response.StatusCode;

//                        // Check if it's a 401/403 and we have a token refresh function
//                        if ((statusCode is Unauthorized or Forbidden) && resilienceOptions.GetBearerTokenFunc != null)
//                        {
//                            resilienceOptions.RefreshToken = true;
//                            return new ValueTask<bool>(true); // Retry for auth failures
//                        }
//                    }

//                    // Handle exceptions
//                    if (args.Outcome.Exception != null)
//                    {
//                        if (args.Outcome.Exception is OperationCanceledException)
//                        {
//                            if (args.Outcome.Exception.InnerException is TimeoutException)
//                            {
//                                return new ValueTask<bool>(true); // Treat as timeout
//                            }
//                            return new ValueTask<bool>(false); // Treat as cancellation, do not retry
//                        }

//                        return new ValueTask<bool>(
//                            args.Outcome.Exception is HttpRequestException or TaskCanceledException or
//                            TimeoutRejectedException or ConnectionResetException or SocketException);
//                    }

//                    // Handle HTTP response results - cast the result to HttpResponseMessage
//                    if (statusCode != null)
//                    {
//                        bool shouldRetry = resilienceOptions.ShouldRetryByStatusFunc?.Invoke((HttpStatusCode)statusCode) ?? ShouldRetryBasedOnStatusCode((HttpStatusCode)statusCode);
//                        return new ValueTask<bool>(shouldRetry);
//                    }

//                    return new ValueTask<bool>(false);
//                },
//                OnRetry = args =>
//                {
//                    int attemptNumber = args.AttemptNumber + 1;

//                    // Extract URL from the response if available
//                    HttpResponseMessage? responseMessage = args.Outcome.Result as HttpResponseMessage;

//                    // Refresh token if needed
//                    if (resilienceOptions.RefreshToken && resilienceOptions.GetBearerTokenFunc != null)
//                    {
//                        // Reset the flag
//                        resilienceOptions.RefreshToken = false;
//                    }

//                    // Enhanced logging with URL
//                    logger.Warn("{baseLogString} - Retry attempt {attemptNumber}", attemptLogBaseString ?? "Request", attemptNumber);

//                    // Log the failure reason
//                    if (args.Outcome.Exception != null)
//                    {
//                        logger.Warn("Retry reason: {exceptionMessage}", args.Outcome.Exception.Message);
//                    }
//                    else if (responseMessage != null)
//                    {
//                        logger.Warn("Retry reason: HTTP {statusCode}", responseMessage.StatusCode);
//                    }

//                    // Dispose response to prevent resource leaks
//                    responseMessage?.Dispose();

//                    return ValueTask.CompletedTask;
//                }
//            });

//            // Add timeout if specified
//            if (resilienceOptions.TimeoutValue.HasValue)
//            {
//                pipelineBuilder.AddTimeout(resilienceOptions.TimeoutValue.Value);
//            }

//            customPipeline = pipelineBuilder.Build();
//        }
//    }

//    // Expose the underlying HttpClient
//    public HttpClient UnderlyingClient { get; }

//    // Wrapper method that applies custom resilience to existing static methods
//    public async Task<TObj?> ExecuteWithResilience<TObj>(Func<HttpClient, CancellationToken, Task<TObj?>> operation, CancellationToken cancellationToken = default)
//    {
//        if (customPipeline != null)
//        {
//            return await customPipeline.ExecuteAsync(async (_) => await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
//        }
//        else
//        {
//            return await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false);
//        }

//        //ResilienceContext resilienceContext = ResilienceContextPool.Shared.Get();

//        //try
//        //{
//        //    // Store operation info in context
//        //    resilienceContext.Properties.Set(
//        //    new ResiliencePropertyKey<string>("OperationType"),
//        //    operation.Method.Name);

//        //    // Store base URL if available
//        //    if (UnderlyingClient.BaseAddress != null)
//        //    {
//        //        resilienceContext.Properties.Set(
//        //        new ResiliencePropertyKey<string>("BaseUrl"),
//        //        UnderlyingClient.BaseAddress.ToString());
//        //    }

//        //    return await customPipeline!.ExecuteAsync(
//        //        callback: async (context) =>
//        //        {
//        //            string operationType = context.Properties.GetValue(
//        //                new ResiliencePropertyKey<string>("OperationType"), "Unknown");

//        //            string baseUrl = context.Properties.GetValue(
//        //                new ResiliencePropertyKey<string>("BaseUrl"), "Unknown");

//        //            logger.Info("Executing {operationType} operation on {baseUrl}",
//        //                operationType, baseUrl);

//        //            // Return the result directly, not wrapped in ValueTask
//        //            return await operation(UnderlyingClient, context.CancellationToken).ConfigureAwait(false);
//        //        },
//        //        context: resilienceContext).ConfigureAwait(false);
//        //}
//        //finally
//        //{
//        //    ResilienceContextPool.Shared.Return(resilienceContext);
//        //}
//    }

//    // Wrapper method for streaming operations
//    public async Task<IAsyncEnumerable<TObj?>> ExecuteStreamingWithResilience<TObj>(Func<HttpClient, CancellationToken, Task<IAsyncEnumerable<TObj?>>> operation, CancellationToken cancellationToken = default)
//    {
//        if (customPipeline != null)
//        {
//            return await customPipeline.ExecuteAsync(async (_) => await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
//        }
//        else
//        {
//            return await operation(UnderlyingClient, cancellationToken).ConfigureAwait(false);
//        }
//    }

//    ~ResilienceWrappedHttpClient()
//    {
//        Dispose(false);
//    }

//    public void Dispose()
//    {
//        Dispose(true);
//        GC.SuppressFinalize(this);
//    }

//    public void Dispose(bool disposing)
//    {
//        if (!disposed && disposing)
//        {
//            // Don't dispose HttpClient - it's managed by HttpClientFactory
//            disposed = true;
//        }
//    }
//}
