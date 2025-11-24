using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;

namespace Web.Api.Tests.TaskQueuing.ApiQueue;

public class PrioritizedSequentialTaskProcessorTests
{
	//[Fact]
	//public async Task EnqueueWithPriorityAsync_Should_Handle_Null_Result()
	//{
	//    BoundedChannelOptions options = new(10);
	//    PrioritizedSequentialTaskProcessor processor = new(options);

	//    object? result = await processor.EnqueueWithPriorityAsync<object?>(_ => Task.FromResult<object?>(null));

	//    result.ShouldBeNull();
	//}

	//[Fact]
	//public async Task EnqueueWithPriorityAsync_Should_Handle_Exception()
	//{
	//    BoundedChannelOptions options = new(10);
	//    PrioritizedSequentialTaskProcessor processor = new(options);

	//    await Should.ThrowAsync<InvalidOperationException>(async () => await processor.EnqueueWithPriorityAsync<int>(_ => throw new InvalidOperationException()));
	//}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Stats()
	{
		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		processor.Dispose();
		Should.NotThrow(processor.Dispose); // Should be idempotent
	}

	//[Fact]
	//public async Task EnqueueWithPriorityAsync_Should_Timeout_When_Specified()
	//{
	//    BoundedChannelOptions options = new(10);
	//    PrioritizedSequentialTaskProcessor processor = new(options);
	//    int? result = await processor.EnqueueWithPriorityAsync(_ => Task.Delay(2000, _).ContinueWith(_ => 42), priority: 1, timeout: TimeSpan.FromMilliseconds(1000));
	//    result.ShouldBeNull(); // Should return null due to timeout
	//}
}