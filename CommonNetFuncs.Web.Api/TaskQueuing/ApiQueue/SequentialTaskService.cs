namespace CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;

public interface ISequentialTaskService
{
	Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default);
}

// Service Implementation
public class SequentialTaskService(SequentialTaskProcessor processor) : ISequentialTaskService
{
	private readonly SequentialTaskProcessor processor = processor;

	public async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default)
	{
		return await processor.EnqueueAsync(taskFunction, cancellationToken).ConfigureAwait(false);
	}
}
