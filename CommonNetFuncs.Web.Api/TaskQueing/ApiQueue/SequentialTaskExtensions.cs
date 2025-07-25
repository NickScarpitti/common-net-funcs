using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;

namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public static class SequentialTaskExtensions
{
    public static IEndpointRouteBuilder EndpointQueueMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/sequential-api-tasks-metrics", async ([FromServices] EndpointQueueService queueService) =>
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
        .WithName("GetSequentialApiTasksMetrics");

        return endpoints;
    }
}
