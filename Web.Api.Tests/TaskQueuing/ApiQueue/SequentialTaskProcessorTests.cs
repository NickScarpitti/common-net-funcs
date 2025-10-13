using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;

namespace Web.Api.Tests.TaskQueuing.ApiQueue;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public class SequentialTaskProcessorTests
{
  //[Fact]
  //public async Task EnqueueAsync_Should_Queue_And_Process_Task()
  //{
  //    BoundedChannelOptions options = new(10);
  //    SequentialTaskProcessor processor = new(options);

  //    int result = await processor.EnqueueAsync<int>(_ => Task.FromResult(42));

  //    result.ShouldBe(42);
  //    processor.Stats.QueuedTasks.ShouldBeGreaterThan(0);
  //    processor.Stats.ProcessedTasks.ShouldBeGreaterThanOrEqualTo(0);
    //}

    //[Fact]
    //public async Task EnqueueAsync_Should_Handle_Null_Result()
    //{
    //    BoundedChannelOptions options = new(10);
    //    SequentialTaskProcessor processor = new(options);

    //    object? result = await processor.EnqueueAsync<object?>(_ => Task.FromResult<object?>(null));

    //    result.ShouldBeNull();
    //}

    //[Fact]
    //public async Task EnqueueAsync_Should_Handle_Exception()
    //{
    //    BoundedChannelOptions options = new(10);
    //    SequentialTaskProcessor processor = new(options);

    //    await Should.ThrowAsync<InvalidOperationException>(async () => await processor.EnqueueAsync<int>(_ => throw new InvalidOperationException()));
    //    processor.Stats.FailedTasks.ShouldBeGreaterThanOrEqualTo(1);
    //}

    [Fact]
  public async Task GetAllQueueStatsAsync_Should_Return_Stats()
  {
    BoundedChannelOptions options = new(10);
    SequentialTaskProcessor processor = new(options);

    QueueStats stats = await processor.GetAllQueueStatsAsync();

    stats.ShouldNotBeNull();
    stats.EndpointKey.ShouldBe("All");
  }

  [Fact]
  public void Dispose_Should_Not_Throw()
  {
    BoundedChannelOptions options = new(10);
    SequentialTaskProcessor processor = new(options);

    processor.Dispose();
    processor.Dispose(); // Should be idempotent
  }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
