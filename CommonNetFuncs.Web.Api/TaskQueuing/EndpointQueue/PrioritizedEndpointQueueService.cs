using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

public interface IPrioritizedEndpointQueueService
{
	Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, TaskPriority priority = TaskPriority.Normal, CancellationToken cancellationToken = default);

	Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, int customPriority, CancellationToken cancellationToken = default);

	Task<PrioritizedQueueStats> GetQueueStatsAsync(string endpointKey);

	Task<Dictionary<string, PrioritizedQueueStats>> GetAllQueueStatsAsync();

	Task<bool> CancelTasksAsync(string endpointKey, TaskPriority priority);
}

public sealed class PrioritizedEndpointQueueService : IPrioritizedEndpointQueueService, IDisposable
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	private readonly ConcurrentDictionary<string, PrioritizedEndpointQueue> queues = new();
	private readonly IServiceProvider serviceProvider;
	private readonly Timer cleanupTimer;
	private readonly double cutoffTimeMinutes = 30.0; // Minutes

	public PrioritizedEndpointQueueService(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;

		// Cleanup unused queues every 5 minutes
		cleanupTimer = new Timer(CleanupUnusedQueues, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
	}

	public PrioritizedEndpointQueueService(IServiceProvider serviceProvider, TimeSpan cleanupInterval)
	{
		this.serviceProvider = serviceProvider;

		// Cleanup unused queues every specified interval
		cleanupTimer = new Timer(CleanupUnusedQueues, null, cleanupInterval, cleanupInterval);
	}

	public PrioritizedEndpointQueueService(IServiceProvider serviceProvider, TimeSpan cleanupInterval, double cutoffTimeMinutes)
	{
		this.serviceProvider = serviceProvider;
		this.cutoffTimeMinutes = Math.Abs(cutoffTimeMinutes);

		// Cleanup unused queues every specified interval
		cleanupTimer = new Timer(CleanupUnusedQueues, null, cleanupInterval, cleanupInterval);
	}

	public async Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, TaskPriority priority = TaskPriority.Normal, CancellationToken cancellationToken = default)
	{
		return await ExecuteAsync(endpointKey, taskFunction, (int)priority, cancellationToken).ConfigureAwait(false);
	}

	public async Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, int customPriority, CancellationToken cancellationToken = default)
	{
		PrioritizedEndpointQueue queue = GetOrCreateQueue(endpointKey);
		TaskPriority priorityLevel = GetPriorityLevel(customPriority);
		return await queue.EnqueueAsync(taskFunction, customPriority, priorityLevel, cancellationToken).ConfigureAwait(false);
	}

	public Task<PrioritizedQueueStats> GetQueueStatsAsync(string endpointKey)
	{
		PrioritizedQueueStats stats = queues.TryGetValue(endpointKey, out PrioritizedEndpointQueue? queue) ? queue.Stats : new(endpointKey);

		return Task.FromResult(stats);
	}

	public Task<Dictionary<string, PrioritizedQueueStats>> GetAllQueueStatsAsync()
	{
		Dictionary<string, PrioritizedQueueStats> allStats = queues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Stats);

		return Task.FromResult(allStats);
	}

	public async Task<bool> CancelTasksAsync(string endpointKey, TaskPriority priority)
	{
		if (queues.TryGetValue(endpointKey, out PrioritizedEndpointQueue? queue))
		{
			return await queue.CancelTasksByPriorityAsync(priority).ConfigureAwait(false);
		}
		return false;
	}

	private PrioritizedEndpointQueue GetOrCreateQueue(string endpointKey)
	{
		return queues.GetOrAdd(endpointKey, key =>
		{
			NLog.Logger localLogger = serviceProvider.GetService<NLog.Logger>() ?? NLog.LogManager.GetCurrentClassLogger();
			localLogger.Info("Creating new prioritized queue for endpoint: {EndpointKey}", key);
			return new PrioritizedEndpointQueue(key);
		});
	}

	private static TaskPriority GetPriorityLevel(int priority)
	{
		return priority switch
		{
			>= 4 => TaskPriority.Emergency,
			3 => TaskPriority.Critical,
			2 => TaskPriority.High,
			1 => TaskPriority.Normal,
			_ => TaskPriority.Low
		};
	}

	private void CleanupUnusedQueues(object? state)
	{
		DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-cutoffTimeMinutes);
		List<string> keysToRemove = [];

		foreach (KeyValuePair<string, PrioritizedEndpointQueue> kvp in queues)
		{
			PrioritizedEndpointQueue queue = kvp.Value;
			PrioritizedQueueStats stats = queue.Stats;

			if (stats.LastProcessedAt.HasValue && stats.LastProcessedAt < cutoffTime && stats.CurrentQueueDepth == 0)
			{
				keysToRemove.Add(kvp.Key);
			}
		}

		foreach (string key in keysToRemove)
		{
			if (queues.TryRemove(key, out PrioritizedEndpointQueue? queue))
			{
				logger.Info("Removing unused prioritized queue for endpoint: {EndpointKey}", key);
				queue.Dispose();
			}
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
				cleanupTimer?.Dispose();

				foreach (PrioritizedEndpointQueue queue in queues.Values)
				{
					queue.Dispose();
				}

				queues.Clear();
			}
			disposed = true;
		}
	}

	~PrioritizedEndpointQueueService()
	{
		Dispose(false);
	}
}
