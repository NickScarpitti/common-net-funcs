using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public class EndpointQueueServiceTests
{
  [Fact]
  public async Task ExecuteAsync_Bounded_Should_Invoke_Queue()
  {
    BoundedChannelOptions options = new(1);
    EndpointQueueService service = new();

    int result = await service.ExecuteAsync("key", _ => Task.FromResult(77), options);

    result.ShouldBe(77);
  }

  [Fact]
  public async Task ExecuteAsync_Unbounded_Should_Invoke_Queue()
  {
    UnboundedChannelOptions options = new();
    EndpointQueueService service = new();

    int result = await service.ExecuteAsync("key", _ => Task.FromResult(88), options);

    result.ShouldBe(88);
  }

  [Fact]
  public async Task GetQueueStatsAsync_Should_Return_Stats()
  {
    EndpointQueueService service = new();

    QueueStats stats = await service.GetQueueStatsAsync("key");

    stats.ShouldNotBeNull();
    stats.EndpointKey.ShouldBe("key");
  }

  [Fact]
  public async Task GetAllQueueStatsAsync_Should_Return_AllStats()
  {
    EndpointQueueService service = new();

    Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();

    allStats.ShouldNotBeNull();
    allStats.ShouldBeOfType<Dictionary<string, QueueStats>>();
  }

  [Fact]
  public void Dispose_Should_Not_Throw()
  {
    EndpointQueueService service = new();

    service.Dispose();
    service.Dispose(); // Should be idempotent
  }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
