using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.Web.Middleware;

/// <summary>
/// Endpoint filter for minimal APIs to highlight long wait times for responses
/// </summary>
public sealed class UseResponseLoggingEndpointFilter(ILogger<UseResponseLoggingEndpointFilter> logger, IResponseLoggingConfig config) : IEndpointFilter
{
	private readonly ILogger<UseResponseLoggingEndpointFilter> logger = logger;
	private readonly IResponseLoggingConfig config = config;

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		object? result = await next(context).ConfigureAwait(false);
		stopwatch.Stop();

		TimeSpan elapsedTime = stopwatch.Elapsed;
		if (elapsedTime >= TimeSpan.FromSeconds(config.ThresholdInSeconds) && logger.IsEnabled(LogLevel.Warning))
		{
			logger.LogWarning("Endpoint {DisplayName} took {ElapsedTime} to complete with status code: {StatusCode}", context.HttpContext.GetEndpoint()?.DisplayName, elapsedTime, context.HttpContext.Response.StatusCode);
		}

		return result;
	}
}
