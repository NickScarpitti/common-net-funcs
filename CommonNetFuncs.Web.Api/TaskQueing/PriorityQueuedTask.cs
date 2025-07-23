namespace CommonNetFuncs.Web.Api.TaskQueing;

public class PriorityQueuedTask(Func<CancellationToken, Task<object?>> taskFunction) : QueuedTask(taskFunction)
{
    public int Priority { get; set; } // Higher number = higher priority

    public TimeSpan? Timeout { get; set; }
}
