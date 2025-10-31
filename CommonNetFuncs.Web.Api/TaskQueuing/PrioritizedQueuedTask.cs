using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace CommonNetFuncs.Web.Api.TaskQueuing;

public class PrioritizedQueuedTask(Func<CancellationToken, Task<object?>> taskFunction) : QueuedTask(taskFunction)
{
  public int Priority { get; set; } // Higher number = higher priority

  public TaskPriority PriorityLevel { get; set; }

  public TimeSpan? Timeout { get; set; }

  public CancellationTokenSource CancellationTokenSource { get; set; } = new();

  public bool IsCancelled => CancellationTokenSource.Token.IsCancellationRequested;

  public int CompareTo(PrioritizedQueuedTask other)
  {
    if (other == null)
    {
      return 1;
    }

    // Higher priority first, then FIFO for same priority
    int priorityComparison = other.Priority.CompareTo(Priority);
    return priorityComparison != 0 ? priorityComparison : QueuedAt.CompareTo(other.QueuedAt);
  }
}
