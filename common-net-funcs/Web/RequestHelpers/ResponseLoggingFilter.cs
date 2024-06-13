using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Common_Net_Funcs.Web.RequestHelpers;

/// <summary>
/// Logging to highlight long wait times for responses
/// </summary>
public class ResponseLoggingFilter(ILogger<ResponseLoggingFilter> logger, IResponseLoggingConfig config) : IActionFilter
{
    private readonly ILogger<ResponseLoggingFilter> logger = logger;
    private readonly IResponseLoggingConfig config = config;
    private readonly Stopwatch stopwatch = new();

    public void OnActionExecuted(ActionExecutedContext context)
    {
        stopwatch.Stop();
        TimeSpan elapsedTime = stopwatch.Elapsed;
        if (elapsedTime >= TimeSpan.FromSeconds(config.ThresholdInSeconds))
        {
            logger.LogWarning($"Method {context.ActionDescriptor.DisplayName} took {elapsedTime} to complete with result: {context.Result}");
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        stopwatch.Restart();
    }
}
