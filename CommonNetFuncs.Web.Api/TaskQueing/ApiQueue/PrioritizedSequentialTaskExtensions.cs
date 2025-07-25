using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public static class PrioritizedSequentialTaskExtensions
{
    public static IEndpointRouteBuilder EndpointQueueMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/prioritized-sequential-api-tasks-metrics", async ([FromServices] PrioritizedSequentialTaskProcessor processor) =>
        {
            try
            {
                PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();
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
