using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;

public static class EndpointQueueExtensions
{
    public static async Task<T?> ExecuteQueuedAsync<T>(this ControllerBase controller, IEndpointQueueService queueService, Func<CancellationToken, Task<T>> taskFunction, BoundedChannelOptions boundedChannelOptions, string? customKey = null)
    {
        string endpointKey = customKey ?? GenerateEndpointKey(controller);
        return await queueService.ExecuteAsync(endpointKey, taskFunction, boundedChannelOptions, controller.HttpContext.RequestAborted);
    }

    public static async Task<T?> ExecuteQueuedAsync<T>(this ControllerBase controller, IEndpointQueueService queueService, Func<CancellationToken, Task<T>> taskFunction, UnboundedChannelOptions unboundedChannelOptions, string? customKey = null)
    {
        string endpointKey = customKey ?? GenerateEndpointKey(controller);
        return await queueService.ExecuteAsync(endpointKey, taskFunction, unboundedChannelOptions, controller.HttpContext.RequestAborted);
    }

    private static string GenerateEndpointKey(ControllerBase controller)
    {
        string controllerName = controller.GetType().Name.Replace("Controller", string.Empty);
        string actionName = controller.ControllerContext.ActionDescriptor.ActionName;
        return $"{controllerName}.{actionName}";
    }

    public static IEndpointRouteBuilder EndpointQueueMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/endpoint-queue-metrics", async ([FromServices] EndpointQueueService queueService) =>
        {
            try
            {
                Dictionary<string, QueueStats> stats = await queueService.GetAllQueueStatsAsync();
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error retrieving endpoint queue metrics");
            }
        })
        .WithName("GetEndpointQueueMetrics");

        endpoints.MapGet("/api/endpoint-queue-metrics/{endpointKey}", async ([FromServices] EndpointQueueService queueService, string endpointKey) =>
        {
            try
            {
                QueueStats stats = await queueService.GetQueueStatsAsync(endpointKey);
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
