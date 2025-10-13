using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace CommonNetFuncs.Web.Api.TaskQueuing;

public class PrioritizedQueueStats(string endpointKey) : QueueStats(endpointKey)
{
  public int TotalQueuedTasks { get; set; }

  public int TotalProcessedTasks { get; set; }

  public int TotalFailedTasks { get; set; }

  public int TotalCancelledTasks { get; set; }

  public Dictionary<TaskPriority, PriorityStats> PriorityBreakdown { get; set; } = new();

  public int CurrentQueueDepth { get; set; }

  public TaskPriority? CurrentProcessingPriority { get; set; }
}
