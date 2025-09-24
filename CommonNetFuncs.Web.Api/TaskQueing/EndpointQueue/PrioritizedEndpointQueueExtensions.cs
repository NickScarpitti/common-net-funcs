using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;

public enum TaskPriority
{
  Low = 0,
  Normal = 1,
  High = 2,
  Critical = 3,
  Emergency = 4
}

public static class PrioritizedEndpointQueueExtensions
{
  public static async Task<T?> ExecutePrioritizedAsync<T>(
        this ControllerBase controller,
        IPrioritizedEndpointQueueService queueService,
        Func<CancellationToken, Task<T>> taskFunction,
        TaskPriority priority = TaskPriority.Normal,
        string? customKey = null)
  {
    string endpointKey = customKey ?? GenerateEndpointKey(controller);
    return await queueService.ExecuteAsync(endpointKey, taskFunction, priority, controller.HttpContext?.RequestAborted ?? default).ConfigureAwait(false);
  }

  public static async Task<T?> ExecutePrioritizedAsync<T>(
        this ControllerBase controller,
        IPrioritizedEndpointQueueService queueService,
        Func<CancellationToken, Task<T>> taskFunction,
        int customPriority,
        string? customKey = null)
  {
    string endpointKey = customKey ?? GenerateEndpointKey(controller);
    return await queueService.ExecuteAsync(endpointKey, taskFunction, customPriority, controller.HttpContext?.RequestAborted ?? default).ConfigureAwait(false);
  }

  private static string GenerateEndpointKey(ControllerBase controller)
  {
    string controllerName = controller.GetType().Name.Replace("Controller", string.Empty);
    string actionName = controller.ControllerContext?.ActionDescriptor?.ActionName ?? "NoAction";
    return $"{controllerName}.{actionName}";
  }

  public static IEndpointRouteBuilder EndpointQueueMetrics(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/api/endpoint-queue-metrics", async ([FromServices] PrioritizedEndpointQueueService queueService) =>
    {
      try
      {
        Dictionary<string, PrioritizedQueueStats> stats = await queueService.GetAllQueueStatsAsync().ConfigureAwait(false);
        return Results.Ok(stats);
      }
      catch (Exception ex)
      {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error retrieving endpoint queue metrics");
      }
    })
        .WithName("GetEndpointQueueMetrics");

    endpoints.MapGet("/api/endpoint-queue-metrics/{endpointKey}", async ([FromServices] PrioritizedEndpointQueueService queueService, string endpointKey) =>
    {
      try
      {
        PrioritizedQueueStats stats = await queueService.GetQueueStatsAsync(endpointKey).ConfigureAwait(false);
        return Results.Ok(stats);
      }
      catch (Exception ex)
      {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error retrieving endpoint queue metrics");
      }
    })
       .WithName("GetEndpointQueueMetrics");

    return endpoints;
  }
}
