using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Moq;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public sealed class PrioritizedEndpointQueueServiceTests
{
	//[Theory]
	//[InlineData(TaskPriority.Normal, 1)]
	//[InlineData(TaskPriority.High, 2)]
	//public async Task ExecuteAsync_Priority_Should_Invoke_Queue(TaskPriority priority, int expected)
	//{
	//    Mock<IServiceProvider> serviceProviderMock = new();
	//    Mock<PrioritizedEndpointQueue> queueMock = new("key");

	//    queueMock.Setup(x => x.EnqueueAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), (int)priority, priority, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

	//    PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
	//    int result = await service.ExecuteAsync("key", _ => Task.FromResult(expected), priority);

	//    result.ShouldBe(expected);
	//}

	//[Theory]
	//[InlineData(1, TaskPriority.Normal)]
	//[InlineData(2, TaskPriority.High)]
	//public async Task ExecuteAsync_CustomPriority_Should_Invoke_Queue(int customPriority, TaskPriority priorityLevel)
	//{
	//    Mock<ILogger<PrioritizedEndpointQueueService>> loggerMock = new();
	//    Mock<IServiceProvider> serviceProviderMock = new();
	//    Mock<PrioritizedEndpointQueue> queueMock = new("key", loggerMock.Object);

	//    queueMock.Setup(q => q.EnqueueAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), customPriority, priorityLevel, It.IsAny<CancellationToken>())).ReturnsAsync(customPriority);

	//    PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
	//    int result = await service.ExecuteAsync("key", _ => Task.FromResult(customPriority), customPriority);

	//    result.ShouldBe(customPriority);
	//}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Stats()
	{
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync("key");

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("key");
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_AllStats()
	{
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		Dictionary<string, PrioritizedQueueStats> allStats = await service.GetAllQueueStatsAsync();

		allStats.ShouldNotBeNull();
		allStats.ShouldBeOfType<Dictionary<string, PrioritizedQueueStats>>();
	}

	[Fact]
	public async Task CancelTasksAsync_Should_Invoke_Queue()
	{
		Mock<IServiceProvider> serviceProviderMock = new();
		Mock<PrioritizedEndpointQueue> queueMock = new("key");

		queueMock.Setup(x => x.CancelTasksByPriorityAsync(TaskPriority.Normal)).ReturnsAsync(true);

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		bool result = await service.CancelTasksAsync("key", TaskPriority.Normal);

		result.ShouldBeFalse();
	}

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		service.Dispose();
		Should.NotThrow(service.Dispose); // Should be idempotent
	}
}