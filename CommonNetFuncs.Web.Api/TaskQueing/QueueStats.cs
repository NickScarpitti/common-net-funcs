namespace CommonNetFuncs.Web.Api.TaskQueing;

public class QueueStats(string endpointKey)
{
  public string EndpointKey { get; set; } = endpointKey;

  public int QueuedTasks { get; set; }

  public int ProcessedTasks { get; set; }

  public int FailedTasks { get; set; }

  public DateTime? LastProcessedAt { get; set; }

  public TimeSpan? AverageProcessingTime { get; set; }
}
