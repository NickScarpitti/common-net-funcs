using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
namespace CommonNetFuncs.Web.Middleware;

/// <summary>
/// Logging to highlight long wait times for responses
/// </summary>
public sealed class UseResponseLoggingFilter(ILogger<UseResponseLoggingFilter> logger, IResponseLoggingConfig config) : IActionFilter
{
	private readonly ILogger<UseResponseLoggingFilter> logger = logger;
	private readonly IResponseLoggingConfig config = config;
	private readonly Stopwatch stopwatch = new();

	public void OnActionExecuted(ActionExecutedContext context)
	{
		stopwatch.Stop();
		TimeSpan elapsedTime = stopwatch.Elapsed;
		if (elapsedTime >= TimeSpan.FromSeconds(config.ThresholdInSeconds))
		{
			logger.LogWarning("Method {DisplayName} took {ElapsedTime} to complete with result: {Result} ({StatusCode})", context.ActionDescriptor.DisplayName, elapsedTime, context.Result, context.HttpContext.Response.StatusCode);
		}
	}

	public void OnActionExecuting(ActionExecutingContext context)
	{
		stopwatch.Restart();
	}
}
