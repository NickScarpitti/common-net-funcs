using System.Diagnostics;
using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;
using Microsoft.Extensions.Hosting;

namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public class PrioritizedSequentialTaskProcessor : BackgroundService
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly Channel<PrioritizedQueuedTask> queue;
    private readonly ChannelReader<PrioritizedQueuedTask> reader;
    private readonly PriorityQueue<PrioritizedQueuedTask, int> priorityQueue = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly Lock statsLock = new();
    private readonly PrioritizedQueueStats stats;
    private readonly Dictionary<TaskPriority, List<TimeSpan>> processingTimesByPriority = new();
    private readonly int processTimeWindow = 1000;

    public PrioritizedSequentialTaskProcessor(BoundedChannelOptions boundedChannelOptions, int processTimeWindow = 1000)
    {
        queue = Channel.CreateBounded<PrioritizedQueuedTask>(boundedChannelOptions);

        this.processTimeWindow = processTimeWindow;
        stats = new PrioritizedQueueStats("All");

        foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
        {
            stats.PriorityBreakdown[priority] = new PriorityStats();
            processingTimesByPriority[priority] = new List<TimeSpan>();
        }

        reader = queue.Reader;
    }

    public PrioritizedSequentialTaskProcessor(UnboundedChannelOptions unboundedChannelOptions)
    {
        queue = Channel.CreateUnbounded<PrioritizedQueuedTask>(unboundedChannelOptions);

        stats = new PrioritizedQueueStats("All");
        foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
        {
            stats.PriorityBreakdown[priority] = new PriorityStats();
            processingTimesByPriority[priority] = new List<TimeSpan>();
        }

        reader = queue.Reader;
    }

    public virtual async Task<T?> EnqueueWithPriorityAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, int priority = (int)TaskPriority.Normal, TaskPriority priorityLevel = TaskPriority.Normal,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        PrioritizedQueuedTask queuedTask = new(async ct => await taskFunction(ct))
        {
            Priority = priority,
            Timeout = timeout
        };

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            priorityQueue.Enqueue(queuedTask, -priority); // Negative for max-heap behavior

            lock (statsLock)
            {
                stats.TotalQueuedTasks++;
                stats.PriorityBreakdown[priorityLevel].QueuedTasks++;
                stats.CurrentQueueDepth = priorityQueue.Count;
            }
        }
        finally
        {
            semaphore.Release();
        }

        return (T?)await queuedTask.CompletionSource.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (PrioritizedQueuedTask currentTask in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (currentTask.IsCancelled)
                {
                    logger.Debug("Skipping cancelled task {TaskId}", currentTask.Id);
                    continue;
                }

                logger.Debug("Processing task {TaskId}", currentTask.Id);

                // Process the task
                Stopwatch stopwatch = Stopwatch.StartNew();

                lock (statsLock)
                {
                    stats.CurrentQueueDepth = priorityQueue.Count;
                    stats.CurrentProcessingPriority = currentTask.PriorityLevel;
                }

                object? result = await currentTask.TaskFunction(stoppingToken);
                currentTask.CompletionSource.SetResult(result);

                stopwatch.Stop();

                lock (statsLock)
                {
                    stats.TotalProcessedTasks++;
                    stats.LastProcessedAt = DateTime.UtcNow;

                    PriorityStats priorityStats = stats.PriorityBreakdown[currentTask.PriorityLevel];
                    priorityStats.ProcessedTasks++;
                    priorityStats.LastProcessedAt = DateTime.UtcNow;

                    List<TimeSpan> processingTimes = processingTimesByPriority[currentTask.PriorityLevel];
                    processingTimes.Add(stopwatch.Elapsed);

                    if (processingTimes.Count > processTimeWindow)
                    {
                        processingTimes.RemoveAt(0);
                    }
                }

                logger.Debug("Completed task {TaskId}", currentTask.Id);
            }
            catch (OperationCanceledException) when (currentTask.IsCancelled)
            {
                logger.Debug("Task {TaskId} was cancelled", currentTask.Id);
                // Task was already marked as cancelled, no need to update stats
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error processing task {TaskId}", currentTask.Id);
                currentTask.CompletionSource.SetException(ex);
            }
            finally
            {
                lock (statsLock)
                {
                    stats.CurrentProcessingPriority = null;
                }
            }
        }
    }

    public Task<PrioritizedQueueStats> GetAllQueueStatsAsync()
    {
        return Task.FromResult(stats);
    }

    private bool disposed;

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                try
                {
                    while (reader.Count > 0)
                    {
                        if (!reader.TryRead(out PrioritizedQueuedTask? processingTask))
                        {
                            break;
                        }
                        // Wait for the processing task to complete
                        processingTask.CompletionSource.Task.Wait(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Error waiting for processing queued task to complete");
                }
            }
            disposed = true;
        }
    }

    ~PrioritizedSequentialTaskProcessor()
    {
        Dispose(false);
    }
}
