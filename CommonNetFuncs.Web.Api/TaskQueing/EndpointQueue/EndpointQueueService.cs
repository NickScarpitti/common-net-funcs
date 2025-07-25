using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    private readonly ConcurrentDictionary<string, EndpointQueue> _queues = new();
    private readonly ILogger<EndpointQueueService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _cleanupTimer;

    public EndpointQueueService(ILogger<EndpointQueueService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Cleanup unused queues every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedQueues, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
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
        QueueStats stats = _queues.TryGetValue(endpointKey, out EndpointQueue? queue) ? queue.Stats : new QueueStats(endpointKey);
        return Task.FromResult(stats);
    }

    public Task<Dictionary<string, QueueStats>> GetAllQueueStatsAsync()
    {
        Dictionary<string, QueueStats> allStats = _queues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Stats);
        return Task.FromResult(allStats);
    }

    private EndpointQueue GetOrCreateQueue(string endpointKey, BoundedChannelOptions boundedChannelOptions)
    {
        return _queues.GetOrAdd(endpointKey, key =>
        {
            _logger.LogInformation("Creating new queue for endpoint: {EndpointKey}", key);
            ILogger<EndpointQueue> logger = _serviceProvider.GetRequiredService<ILogger<EndpointQueue>>();
            return new EndpointQueue(key, logger, boundedChannelOptions);
        });
    }

    private EndpointQueue GetOrCreateQueue(string endpointKey, UnboundedChannelOptions unboundedChannelOptions)
    {
        return _queues.GetOrAdd(endpointKey, key =>
        {
            _logger.LogInformation("Creating new queue for endpoint: {EndpointKey}", key);
            ILogger<EndpointQueue> logger = _serviceProvider.GetRequiredService<ILogger<EndpointQueue>>();
            return new EndpointQueue(key, logger, unboundedChannelOptions);
        });
    }

    private void CleanupUnusedQueues(object? state)
    {
        DateTime cutoffTime = DateTime.UtcNow.AddMinutes(-30); // Remove queues unused for 30 minutes
        List<string> keysToRemove = [];

        foreach (KeyValuePair<string, EndpointQueue> kvp in _queues)
        {
            EndpointQueue queue = kvp.Value;
            if (queue.Stats.LastProcessedAt.HasValue && queue.Stats.LastProcessedAt < cutoffTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            if (_queues.TryRemove(key, out EndpointQueue? queue))
            {
                _logger.LogInformation("Removing unused queue for endpoint: {EndpointKey}", key);
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
                _cleanupTimer?.Dispose();

                foreach (EndpointQueue queue in _queues.Values)
                {
                    queue.Dispose();
                }

                _queues.Clear();
            }
            disposed = true;
        }
    }

    ~EndpointQueueService()
    {
        Dispose(false);
    }
}
