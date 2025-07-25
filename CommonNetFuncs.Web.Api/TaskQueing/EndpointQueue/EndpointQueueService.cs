using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CommonNetFuncs.Web.Api.TaskQueing.EndpointQueue;

public interface IEndpointQueueService
{
    Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, BoundedChannelOptions boundedChannelOptions, CancellationToken cancellationToken = default);

    Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, UnboundedChannelOptions unboundedChannelOptions, CancellationToken cancellationToken = default);

    Task<QueueStats> GetQueueStatsAsync(string endpointKey);

    Task<Dictionary<string, QueueStats>> GetAllQueueStatsAsync();
}

public class EndpointQueueService : IEndpointQueueService, IDisposable
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<string, EndpointQueue> queues = new();
    private readonly Timer cleanupTimer;

    public EndpointQueueService()
    {
        // Cleanup unused queues every 5 minutes
        cleanupTimer = new Timer(CleanupUnusedQueues, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, BoundedChannelOptions boundedChannelOptions, CancellationToken cancellationToken = default)
    {
        EndpointQueue queue = GetOrCreateQueue(endpointKey, boundedChannelOptions);
        return await queue.EnqueueAsync(taskFunction, cancellationToken);
    }

    public async Task<T?> ExecuteAsync<T>(string endpointKey, Func<CancellationToken, Task<T>> taskFunction, UnboundedChannelOptions unboundedChannelOptions, CancellationToken cancellationToken = default)
    {
        EndpointQueue queue = GetOrCreateQueue(endpointKey, unboundedChannelOptions);
        return await queue.EnqueueAsync(taskFunction, cancellationToken);
    }

    public Task<QueueStats> GetQueueStatsAsync(string endpointKey)
    {
        QueueStats stats = queues.TryGetValue(endpointKey, out EndpointQueue? queue) ? queue.Stats : new QueueStats(endpointKey);
        return Task.FromResult(stats);
    }

    public Task<Dictionary<string, QueueStats>> GetAllQueueStatsAsync()
    {
        Dictionary<string, QueueStats> allStats = queues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Stats);
        return Task.FromResult(allStats);
    }

    private EndpointQueue GetOrCreateQueue(string endpointKey, BoundedChannelOptions boundedChannelOptions)
    {
        return queues.GetOrAdd(endpointKey, key =>
        {
            logger.Info("Creating new queue for endpoint: {EndpointKey}", key);
            return new EndpointQueue(key, boundedChannelOptions);
        });
    }

    private EndpointQueue GetOrCreateQueue(string endpointKey, UnboundedChannelOptions unboundedChannelOptions)
    {
        return queues.GetOrAdd(endpointKey, key =>
        {
            logger.Info("Creating new queue for endpoint: {EndpointKey}", key);
            return new EndpointQueue(key, unboundedChannelOptions);
        });
    }

    private void CleanupUnusedQueues(object? state)
    {
        DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-30); // Remove queues unused for 30 minutes
        List<string> keysToRemove = [];

        foreach (KeyValuePair<string, EndpointQueue> kvp in queues)
        {
            EndpointQueue queue = kvp.Value;
            if (queue.Stats.LastProcessedAt.HasValue && queue.Stats.LastProcessedAt < cutoffTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            if (queues.TryRemove(key, out EndpointQueue? queue))
            {
                logger.Info("Removing unused queue for endpoint: {EndpointKey}", key);
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

                foreach (EndpointQueue queue in queues.Values)
                {
                    queue.Dispose();
                }

                queues.Clear();
            }
            disposed = true;
        }
    }

    ~EndpointQueueService()
    {
        Dispose(false);
    }
}
