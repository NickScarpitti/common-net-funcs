namespace CommonNetFuncs.Web.Api.TaskQueing;

public class PriorityStats
{
  public int QueuedTasks { get; set; }

  public int ProcessedTasks { get; set; }

  public int FailedTasks { get; set; }

  public int CancelledTasks { get; set; }

  public TimeSpan? AverageProcessingTime { get; set; }

  public DateTime? LastProcessedAt { get; set; }
}
