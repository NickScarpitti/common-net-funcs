using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public class PrioritizedSequentialTaskProcessor : BackgroundService
{
    private readonly Channel<PriorityQueuedTask> queue;
    private readonly ILogger<PrioritizedSequentialTaskProcessor> logger;
    private readonly ChannelReader<PriorityQueuedTask> reader;
    private readonly PriorityQueue<PriorityQueuedTask, int> priorityQueue = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public PrioritizedSequentialTaskProcessor(ILogger<PrioritizedSequentialTaskProcessor> logger, BoundedChannelOptions boundedChannelOptions)
    {
        this.logger = logger;
        queue = Channel.CreateBounded<PriorityQueuedTask>(boundedChannelOptions);
        reader = queue.Reader;
    }

    public PrioritizedSequentialTaskProcessor(ILogger<PrioritizedSequentialTaskProcessor> logger, UnboundedChannelOptions unboundedChannelOptions)
    {
        this.logger = logger;
        queue = Channel.CreateUnbounded<PriorityQueuedTask>(unboundedChannelOptions);
        reader = queue.Reader;
    }

    public async Task<T?> EnqueueWithPriorityAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, int priority = 0, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        PriorityQueuedTask queuedTask = new(async ct => await taskFunction(ct))
        {
            Priority = priority,
            Timeout = timeout,
            TaskFunction = async ct => await taskFunction(ct)
        };

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            priorityQueue.Enqueue(queuedTask, -priority); // Negative for max-heap behavior
        }
        finally
        {
            semaphore.Release();
        }

        return (T?)await queuedTask.CompletionSource.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (PriorityQueuedTask task in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Processing task {TaskId}", task.Id);

                object? result = await task.TaskFunction(stoppingToken);
                task.CompletionSource.SetResult(result);

                logger.LogInformation("Completed task {TaskId}", task.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing task {TaskId}", task.Id);
                task.CompletionSource.SetException(ex);
            }
        }
    }
}
