namespace CommonNetFuncs.Web.Api.TaskQueing;

public interface ISequentialTaskService
{
    Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default);
}

// Service Implementation
public class SequentialTaskService(SequentialTaskProcessor processor) : ISequentialTaskService
{
    private readonly SequentialTaskProcessor _processor = processor;

    public async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default)
    {
        return await _processor.EnqueueAsync(taskFunction, cancellationToken);
    }
}
