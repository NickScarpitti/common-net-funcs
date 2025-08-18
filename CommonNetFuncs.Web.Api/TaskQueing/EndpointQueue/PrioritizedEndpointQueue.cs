using System.Diagnostics;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;

public class PrioritizedEndpointQueue : IDisposable
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly PriorityQueue<PrioritizedQueuedTask, PrioritizedQueuedTask> priorityQueue = new();
    private readonly SemaphoreSlim queueSemaphore = new(1, 1);
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task processingTask;
    private readonly PrioritizedQueueStats stats;
    private readonly Dictionary<TaskPriority, List<TimeSpan>> processingTimesByPriority = new();
    private readonly Lock statsLock = new();
    private readonly ManualResetEventSlim newTaskEvent = new(false);
    private readonly int processTimeWindow = 1000;

    public string EndpointKey { get; }

    public PrioritizedQueueStats Stats => GetCurrentStats();

    public PrioritizedEndpointQueue(string endpointKey, int processTimeWindow = 1000)
    {
        EndpointKey = endpointKey;
        cancellationTokenSource = new CancellationTokenSource();

        this.processTimeWindow = processTimeWindow;
        stats = new PrioritizedQueueStats(endpointKey);

        // Initialize priority breakdown
        foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
        {
            stats.PriorityBreakdown[priority] = new PriorityStats();
            processingTimesByPriority[priority] = new List<TimeSpan>();
        }

        // Start processing task
        processingTask = ProcessTasksAsync(cancellationTokenSource.Token);
    }

    public async Task<T?> EnqueueAsync<T>(Func<CancellationToken, Task<T>> taskFunction,
        int priority, TaskPriority priorityLevel, CancellationToken cancellationToken = default)
    {
        PrioritizedQueuedTask queuedTask = new(async ct => await taskFunction(ct).ConfigureAwait(false))
        {
            Priority = priority,
            PriorityLevel = priorityLevel,
        };

        await queueSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            priorityQueue.Enqueue(queuedTask, queuedTask);

            lock (statsLock)
            {
                stats.TotalQueuedTasks++;
                stats.PriorityBreakdown[priorityLevel].QueuedTasks++;
                stats.CurrentQueueDepth = priorityQueue.Count;
            }

            newTaskEvent.Set(); // Signal that a new task is available
        }
        finally
        {
            queueSemaphore.Release();
        }

        logger.Debug("Enqueued task {TaskId} with priority {Priority} ({PriorityLevel}) for endpoint {EndpointKey}",
            queuedTask.Id, priority, priorityLevel, EndpointKey);

        object? result = await queuedTask.CompletionSource.Task;
        return (T?)result;
    }

    public virtual async Task<bool> CancelTasksByPriorityAsync(TaskPriority priority)
    {
        int cancelledCount = 0;

        await queueSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            List<PrioritizedQueuedTask> tasksToCancel = new();
            List<PrioritizedQueuedTask> tasksToKeep = new();

            // Extract all tasks
            while (priorityQueue.TryDequeue(out PrioritizedQueuedTask? task, out _))
            {
                if (task.PriorityLevel == priority && !task.IsCancelled)
                {
                    tasksToCancel.Add(task);
                }
                else
                {
                    tasksToKeep.Add(task);
                }
            }

            // Re-enqueue tasks we want to keep
            foreach (PrioritizedQueuedTask task in tasksToKeep)
            {
                priorityQueue.Enqueue(task, task);
            }

            // Cancel the tasks
            foreach (PrioritizedQueuedTask task in tasksToCancel)
            {
                task.CancellationTokenSource.Cancel();
                task.CompletionSource.SetCanceled();
                cancelledCount++;
            }

            lock (statsLock)
            {
                stats.TotalCancelledTasks += cancelledCount;
                stats.PriorityBreakdown[priority].CancelledTasks += cancelledCount;
                stats.CurrentQueueDepth = priorityQueue.Count;
            }
        }
        finally
        {
            queueSemaphore.Release();
        }

        logger.Info("Cancelled {Count} tasks with priority {Priority} for endpoint {EndpointKey}",
            cancelledCount, priority, EndpointKey);

        return cancelledCount > 0;
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            PrioritizedQueuedTask? currentTask = null;

            // Wait for tasks or cancellation
            await queueSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (priorityQueue.TryDequeue(out currentTask, out _))
                {
                    lock (statsLock)
                    {
                        stats.CurrentQueueDepth = priorityQueue.Count;
                        stats.CurrentProcessingPriority = currentTask.PriorityLevel;
                    }
                }
                else
                {
                    lock (statsLock)
                    {
                        stats.CurrentProcessingPriority = null;
                    }
                }
            }
            finally
            {
                queueSemaphore.Release();
            }

            if (currentTask == null)
            {
                // No tasks available, wait for new tasks
                try
                {
                    newTaskEvent.Wait(cancellationToken);
                    newTaskEvent.Reset();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }

            // Process the task
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                if (currentTask.IsCancelled)
                {
                    logger.Debug("Skipping cancelled task {TaskId} for endpoint {EndpointKey}",
                        currentTask.Id, EndpointKey);
                    continue;
                }

                logger.Debug("Processing task {TaskId} with priority {Priority} ({PriorityLevel}) for endpoint {EndpointKey}",
                    currentTask.Id, currentTask.Priority, currentTask.PriorityLevel, EndpointKey);

                using CancellationTokenSource combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, currentTask.CancellationTokenSource.Token);

                object? result = await currentTask.TaskFunction(combinedCts.Token).ConfigureAwait(false);
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

                logger.Debug("Completed task {TaskId} with priority {Priority} for endpoint {EndpointKey} in {Duration}ms",
                    currentTask.Id, currentTask.Priority, EndpointKey, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (currentTask.IsCancelled)
            {
                logger.Debug("Task {TaskId} was cancelled for endpoint {EndpointKey}", currentTask.Id, EndpointKey);
                // Task was already marked as cancelled, no need to update stats
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                lock (statsLock)
                {
                    stats.TotalFailedTasks++;
                    stats.PriorityBreakdown[currentTask.PriorityLevel].FailedTasks++;
                }

                logger.Error(ex, "Error processing task {TaskId} with priority {Priority} for endpoint {EndpointKey}",
                    currentTask.Id, currentTask.Priority, EndpointKey);
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

    private PrioritizedQueueStats GetCurrentStats()
    {
        lock (statsLock)
        {
            PrioritizedQueueStats currentStats = new(stats.EndpointKey)
            {
                EndpointKey = stats.EndpointKey,
                TotalQueuedTasks = stats.TotalQueuedTasks,
                TotalProcessedTasks = stats.TotalProcessedTasks,
                TotalFailedTasks = stats.TotalFailedTasks,
                TotalCancelledTasks = stats.TotalCancelledTasks,
                LastProcessedAt = stats.LastProcessedAt,
                CurrentQueueDepth = stats.CurrentQueueDepth,
                CurrentProcessingPriority = stats.CurrentProcessingPriority,
                PriorityBreakdown = new Dictionary<TaskPriority, PriorityStats>()
            };

            // Calculate overall average processing time
            List<TimeSpan> allProcessingTimes = processingTimesByPriority.Values.SelectMany(times => times).ToList();
            if (allProcessingTimes.AnyFast())
            {
                currentStats.AverageProcessingTime = TimeSpan.FromMilliseconds(allProcessingTimes.Average(t => t.TotalMilliseconds));
            }

            // Copy priority breakdown with calculated averages
            foreach (KeyValuePair<TaskPriority, PriorityStats> kvp in stats.PriorityBreakdown)
            {
                PriorityStats priorityStats = new()
                {
                    QueuedTasks = kvp.Value.QueuedTasks,
                    ProcessedTasks = kvp.Value.ProcessedTasks,
                    FailedTasks = kvp.Value.FailedTasks,
                    CancelledTasks = kvp.Value.CancelledTasks,
                    LastProcessedAt = kvp.Value.LastProcessedAt
                };

                List<TimeSpan> processingTimes = processingTimesByPriority[kvp.Key];
                if (processingTimes.AnyFast())
                {
                    priorityStats.AverageProcessingTime = TimeSpan.FromMilliseconds(processingTimes.Average(t => t.TotalMilliseconds));
                }

                currentStats.PriorityBreakdown[kvp.Key] = priorityStats;
            }

            return currentStats;
        }
    }

    private bool disposed;

    public void Dispose()
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
                cancellationTokenSource.Cancel();
                newTaskEvent.Set(); // Wake up processing task for shutdown

                try
                {
                    processingTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Error waiting for processing task to complete for endpoint {EndpointKey}", EndpointKey);
                }

                cancellationTokenSource.Dispose();
                queueSemaphore.Dispose();
                newTaskEvent.Dispose();
            }
            disposed = true;
        }
    }

    ~PrioritizedEndpointQueue()
    {
        Dispose(false);
    }
}
