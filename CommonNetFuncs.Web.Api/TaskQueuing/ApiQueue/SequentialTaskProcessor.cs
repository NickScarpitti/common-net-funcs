using System.Diagnostics;
using System.Threading.Channels;
using CommonNetFuncs.Core;
using Microsoft.Extensions.Hosting;

namespace CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;

public class SequentialTaskProcessor : BackgroundService
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly ChannelWriter<QueuedTask> writer;
	private readonly ChannelReader<QueuedTask> reader;
	private readonly QueueStats stats;
	private readonly List<TimeSpan> processingTimes = new();
	private readonly Lock statsLock = new();
	private readonly int processTimeWindow;

	public SequentialTaskProcessor(BoundedChannelOptions boundedChannelOptions, int processTimeWindow = 1000)
	{
		cancellationTokenSource = new CancellationTokenSource();

		Channel<QueuedTask> queue = Channel.CreateBounded<QueuedTask>(boundedChannelOptions);
		writer = queue.Writer;
		reader = queue.Reader;

		this.processTimeWindow = processTimeWindow;
		stats = new QueueStats("All");
	}

	public SequentialTaskProcessor(UnboundedChannelOptions unboundedChannelOptions, int processTimeWindow = 1000)
	{
		cancellationTokenSource = new CancellationTokenSource();

		Channel<QueuedTask> queue = Channel.CreateUnbounded<QueuedTask>(unboundedChannelOptions);
		writer = queue.Writer;
		reader = queue.Reader;

		this.processTimeWindow = processTimeWindow;
		stats = new QueueStats("All");
	}

	public QueueStats Stats => GetCurrentStats();

	public virtual async Task<T?> EnqueueAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default)
	{
		QueuedTask queuedTask = new(async ct => await taskFunction(ct).ConfigureAwait(false));
		await writer.WriteAsync(queuedTask, cancellationToken).ConfigureAwait(false);

		lock (statsLock)
		{
			stats.QueuedTasks++;
		}

		object? result = await queuedTask.CompletionSource.Task;
		return (T?)result;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (QueuedTask task in reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			try
			{
				logger.Debug("Processing task {TaskId}", task.Id);

				object? result = await task.TaskFunction(stoppingToken).ConfigureAwait(false);
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

				logger.Debug("Completed task {TaskId}", task.Id);
			}
			catch (Exception ex)
			{
				stopwatch.Stop();

				lock (statsLock)
				{
					stats.FailedTasks++;
				}

				logger.Error(ex, "Error processing task {TaskId}", task.Id);
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

	public virtual Task<QueueStats> GetAllQueueStatsAsync()
	{
		return Task.FromResult(stats);
	}

	public override void Dispose()
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
			logger.Warn(ex, "Error waiting for processing queued task to complete");
		}

		cancellationTokenSource.Dispose();
		base.Dispose(); // Call the dispose method of the base class to ensure proper cleanup
	}
}
