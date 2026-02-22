using System.Diagnostics;
using System.Threading.Channels;
using CommonNetFuncs.Core;

namespace CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
// Individual Endpoint Queue


public class EndpointQueue : IDisposable
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private readonly Channel<QueuedTask> channel;
	private readonly ChannelWriter<QueuedTask> writer;
	private readonly ChannelReader<QueuedTask> reader;
	private readonly CancellationTokenSource cancellationTokenSource;
	private readonly Task processingTask;
	private readonly QueueStats stats;
	private readonly List<TimeSpan> processingTimes = new();
	private readonly Lock statsLock = new();
	private readonly int processTimeWindow;

	public string EndpointKey { get; }

	public QueueStats Stats => GetCurrentStats();

	public EndpointQueue(string endpointKey, BoundedChannelOptions boundedChannelOptions, int processTimeWindow = 1000)
	{
		EndpointKey = endpointKey;
		cancellationTokenSource = new CancellationTokenSource();

		channel = Channel.CreateBounded<QueuedTask>(boundedChannelOptions);
		writer = channel.Writer;
		reader = channel.Reader;

		this.processTimeWindow = processTimeWindow;
		stats = new QueueStats(endpointKey);

		// Start processing task

		processingTask = ProcessTasksAsync(cancellationTokenSource.Token);
	}

	public EndpointQueue(string endpointKey, UnboundedChannelOptions unboundedChannelOptions, int processTimeWindow = 1000)
	{
		EndpointKey = endpointKey;
		cancellationTokenSource = new CancellationTokenSource();

		channel = Channel.CreateUnbounded<QueuedTask>(unboundedChannelOptions);
		writer = channel.Writer;
		reader = channel.Reader;

		this.processTimeWindow = processTimeWindow;
		stats = new QueueStats(endpointKey);

		// Start processing task

		processingTask = ProcessTasksAsync(cancellationTokenSource.Token);
	}

	public async Task<T?> EnqueueAsync<T>(Func<CancellationToken, Task<T>> taskFunction, CancellationToken cancellationToken = default)
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

	private async Task ProcessTasksAsync(CancellationToken cancellationToken)
	{
		await foreach (QueuedTask task in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			try
			{
				logger.Debug("Processing task {TaskId} for endpoint {EndpointKey}", task.Id, EndpointKey);

				object? result = await task.TaskFunction(cancellationToken).ConfigureAwait(false);
				task.CompletionSource.SetResult(result);

				stopwatch.Stop();

				lock (statsLock)
				{
					stats.ProcessedTasks++;
					stats.LastProcessedAt = DateTime.UtcNow;
					processingTimes.Add(stopwatch.Elapsed);

					// Keep only last 100 processing times for average calculation

					if (processingTimes.Count > processTimeWindow)
					{
						processingTimes.RemoveAt(0);
					}
				}

				logger.Debug("Completed task {TaskId} for endpoint {EndpointKey} in {Duration}ms", task.Id, EndpointKey, stopwatch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				stopwatch.Stop();

				lock (statsLock)
				{
					stats.FailedTasks++;
				}

				logger.Error(ex, "Error processing task {TaskId} for endpoint {EndpointKey}", task.Id, EndpointKey);
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

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				writer.Complete();
				cancellationTokenSource.Cancel();

				try
				{
					processingTask.Wait(TimeSpan.FromSeconds(5));
				}
				catch (Exception ex)
				{
					logger.Warn(ex, "Error waiting for processing task to complete for endpoint {EndpointKey}", EndpointKey);
				}

				try
				{
					cancellationTokenSource.Dispose();
				}
				catch (Exception ex)
				{
					logger.Warn(ex, "Error disposing cancellationTokenSource");
				}

				try
				{
					channel?.Writer?.Complete();
				}
				catch (Exception ex)
				{
					logger.Warn(ex, "Error completing channel writer");
				}
			}
			disposed = true;
		}
	}

	~EndpointQueue()
	{
		Dispose(false);
	}
}
