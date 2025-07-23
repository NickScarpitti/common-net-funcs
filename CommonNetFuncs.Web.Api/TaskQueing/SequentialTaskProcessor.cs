using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommonNetFuncs.Web.Api.TaskQueing;

public class SequentialTaskProcessor : BackgroundService
{
    private readonly Channel<QueuedTask> queue;
    private readonly ILogger<SequentialTaskProcessor> logger;
    private readonly ChannelWriter<QueuedTask> writer;
    private readonly ChannelReader<QueuedTask> reader;

    public SequentialTaskProcessor(ILogger<SequentialTaskProcessor> logger)
    {
        this.logger = logger;
        BoundedChannelOptions options = new(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        queue = Channel.CreateBounded<QueuedTask>(options);
        writer = queue.Writer;
        reader = queue.Reader;
    }

    public async Task<T?> EnqueueAsync<T>(Func<CancellationToken, Task<T?>> taskFunction, CancellationToken cancellationToken = default)
    {
        QueuedTask queuedTask = new(async ct => await taskFunction(ct));
        await writer.WriteAsync(queuedTask, cancellationToken);

        object? result = await queuedTask.CompletionSource.Task;
        return (T?)result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (QueuedTask task in reader.ReadAllAsync(stoppingToken))
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
