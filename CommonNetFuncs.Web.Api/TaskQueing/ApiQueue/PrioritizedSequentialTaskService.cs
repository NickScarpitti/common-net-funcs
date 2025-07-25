namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public interface IPrioritizedSequentialTaskService
{
    Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, int priority = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

// Service Implementation
public class PrioritizedSequentialTaskService(PrioritizedSequentialTaskProcessor processor) : IPrioritizedSequentialTaskService
{
    private readonly PrioritizedSequentialTaskProcessor processor = processor;

    public async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, int priority = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return await processor.EnqueueWithPriorityAsync(taskFunction, priority, timeout, cancellationToken);
    }
}
