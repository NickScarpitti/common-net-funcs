using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static CommonNetFuncs.Core.Collections;

namespace CommonNetFuncs.Web.Api.TaskQueing.ApiQueue;

public class SequentialTaskProcessor : BackgroundService, IDisposable
{
    private readonly Channel<QueuedTask> queue;
    private readonly ILogger<SequentialTaskProcessor> logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly ChannelWriter<QueuedTask> writer;
    private readonly ChannelReader<QueuedTask> reader;
    private readonly QueueStats stats;
    private readonly List<TimeSpan> processingTimes = new();
    private readonly Lock statsLock = new();
    private readonly int processTimeWindow;

    public SequentialTaskProcessor(ILogger<SequentialTaskProcessor> logger, BoundedChannelOptions boundedChannelOptions, int processTimeWindow = 1000)
    {
        this.logger = logger;
        cancellationTokenSource = new CancellationTokenSource();

        queue = Channel.CreateBounded<QueuedTask>(boundedChannelOptions);
        writer = queue.Writer;
        reader = queue.Reader;

        this.processTimeWindow = processTimeWindow;
        stats = new QueueStats("All");
    }

    public SequentialTaskProcessor(ILogger<SequentialTaskProcessor> logger, UnboundedChannelOptions unboundedChannelOptions, int processTimeWindow = 1000)
    {
        this.logger = logger;
        cancellationTokenSource = new CancellationTokenSource();

        queue = Channel.CreateUnbounded<QueuedTask>(unboundedChannelOptions);
        writer = queue.Writer;
        reader = queue.Reader;

        this.processTimeWindow = processTimeWindow;
        stats = new QueueStats("All");
    }

    public QueueStats Stats => GetCurrentStats();

    public async Task<T?> EnqueueAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default)
    {
        QueuedTask queuedTask = new(async ct => await taskFunction(ct));
        await writer.WriteAsync(queuedTask, cancellationToken);

        lock (statsLock)
        {
            stats.QueuedTasks++;
        }

        object? result = await queuedTask.CompletionSource.Task;
        return (T?)result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (QueuedTask task in reader.ReadAllAsync(stoppingToken))
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                logger.LogInformation("Processing task {TaskId}", task.Id);

                object? result = await task.TaskFunction(stoppingToken);
                task.CompletionSource.SetResult(result);

                lock (statsLock)
                {
                    stats.ProcessedTasks++;
                    stats.LastProcessedAt = DateTime.UtcNow;
                    processingTimes.Add(stopwatch.Elapsed);

                    if (processingTimes.Count > processTimeWindow)
                    {
                        processingTimes.RemoveAt(0);
                    }
                }

                logger.LogInformation("Completed task {TaskId}", task.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                lock (statsLock)
                {
                    stats.FailedTasks++;
                }

                logger.LogError(ex, "Error processing task {TaskId}", task.Id);
                task.CompletionSource.SetException(ex);
            }
        }
    }

    private QueueStats GetCurrentStats()
    {
        lock (statsLock)
        {
            QueueStats currentStats = new(stats.EndpointKey)
            {
                QueuedTasks = stats.QueuedTasks,
                ProcessedTasks = stats.ProcessedTasks,
                FailedTasks = stats.FailedTasks,
                LastProcessedAt = stats.LastProcessedAt
            };

            if (processingTimes.AnyFast())
            {
                currentStats.AverageProcessingTime = TimeSpan.FromMilliseconds(processingTimes.Average(t => t.TotalMilliseconds));
            }

            return currentStats;
        }
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
                writer.Complete();
                cancellationTokenSource.Cancel();

                try
                {
                    while (reader.Count > 0)
                    {
                        if (!reader.TryRead(out QueuedTask? processingTask))
                        {
                            break;
                        }
                        // Wait for the processing task to complete
                        processingTask.CompletionSource.Task.Wait(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error waiting for processing queued task to complete");
                }

                cancellationTokenSource.Dispose();
                writer?.Complete();
            }
            disposed = true;
        }
    }

    ~SequentialTaskProcessor()
    {
        Dispose(false);
    }
}
