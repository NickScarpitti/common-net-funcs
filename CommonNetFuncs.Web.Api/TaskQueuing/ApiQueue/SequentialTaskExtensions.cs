using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;

public static class SequentialTaskExtensions
{
	public static IEndpointRouteBuilder EndpointQueueMetrics(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/api/sequential-api-tasks-metrics", async ([FromServices] SequentialTaskProcessor processor) =>
		{
			try
			{
				QueueStats stats = await processor.GetAllQueueStatsAsync().ConfigureAwait(false);
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
