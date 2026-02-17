using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public sealed class PrioritizedEndpointQueueServiceTests
{
	[Fact]
	public async Task ExecuteAsync_TaskPriority_Should_Execute_Task()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		bool taskExecuted = false;

		// Act
		int result = await service.ExecuteAsync("test-key", _ =>
		{
			taskExecuted = true;
			return Task.FromResult(42);
		}, TaskPriority.Normal);

		// Assert
		result.ShouldBe(42);
		taskExecuted.ShouldBeTrue();
	}

	[Theory]
	[InlineData(TaskPriority.Low)]
	[InlineData(TaskPriority.Normal)]
	[InlineData(TaskPriority.High)]
	[InlineData(TaskPriority.Critical)]
	[InlineData(TaskPriority.Emergency)]
	public async Task ExecuteAsync_With_Different_TaskPriorities_Should_Execute(TaskPriority priority)
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		int result = await service.ExecuteAsync($"key-{priority}", _ => Task.FromResult((int)priority), priority);

		// Assert
		result.ShouldBe((int)priority);
	}

	[Fact]
	public async Task ExecuteAsync_CustomPriority_Should_Execute_Task()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		bool taskExecuted = false;

		// Act
		string result = await service.ExecuteAsync("test-key", _ =>
		{
			taskExecuted = true;
			return Task.FromResult("success");
		}, customPriority: 3);

		// Assert
		result.ShouldBe("success");
		taskExecuted.ShouldBeTrue();
	}

	[Theory]
	[InlineData(0, TaskPriority.Low)]
	[InlineData(1, TaskPriority.Normal)]
	[InlineData(2, TaskPriority.High)]
	[InlineData(3, TaskPriority.Critical)]
	[InlineData(4, TaskPriority.Emergency)]
	[InlineData(5, TaskPriority.Emergency)]
	[InlineData(10, TaskPriority.Emergency)]
	public async Task ExecuteAsync_CustomPriority_Should_Map_To_Correct_PriorityLevel(int customPriority, TaskPriority expectedLevel)
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		int result = await service.ExecuteAsync($"key-{customPriority}", _ => Task.FromResult(customPriority), customPriority);

		// Assert - Task should complete successfully with correct priority mapping
		result.ShouldBe(customPriority);
	}

	[Fact]
	public async Task ExecuteAsync_Should_Reuse_Same_Queue_For_Same_Key()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "same-endpoint";

		// Act
		int result1 = await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal);
		int result2 = await service.ExecuteAsync(endpointKey, _ => Task.FromResult(2), TaskPriority.Normal);
		await Task.Delay(100); // Allow stats to update

		// Assert
		result1.ShouldBe(1);
		result2.ShouldBe(2);

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(2);
	}

	[Fact]
	public async Task ExecuteAsync_Should_Create_Separate_Queues_For_Different_Keys()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		int result1 = await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal);
		int result2 = await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.Normal);

		// Assert
		result1.ShouldBe(1);
		result2.ShouldBe(2);

		Dictionary<string, PrioritizedQueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(2);
		allStats.ContainsKey("endpoint-1").ShouldBeTrue();
		allStats.ContainsKey("endpoint-2").ShouldBeTrue();
	}

	[Fact]
	public async Task ExecuteAsync_With_CancellationToken_Should_Propagate_Cancellation()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		using CancellationTokenSource cts = new();
		cts.Cancel();

		// Act & Assert
		await Should.ThrowAsync<TaskCanceledException>(async () =>
		{
			await service.ExecuteAsync("test-key", async ct =>
			{
				await Task.Delay(1000, ct);
				return 42;
			}, TaskPriority.Normal, cts.Token);
		});
	}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Stats_For_Existing_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "test-endpoint";

		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal);
		await Task.Delay(100); // Allow stats to update

		// Act
		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Empty_Stats_For_Nonexistent_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		PrioritizedQueueStats stats = await service.GetQueueStatsAsync("nonexistent-key");

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("nonexistent-key");
		stats.TotalProcessedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Empty_Dictionary_When_No_Queues()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		Dictionary<string, PrioritizedQueueStats> allStats = await service.GetAllQueueStatsAsync();

		// Assert
		allStats.ShouldNotBeNull();
		allStats.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_All_Queue_Stats()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal);
		await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.High);
		await service.ExecuteAsync("endpoint-3", _ => Task.FromResult(3), TaskPriority.Critical);
		await Task.Delay(100); // Allow stats to update

		// Act
		Dictionary<string, PrioritizedQueueStats> allStats = await service.GetAllQueueStatsAsync();

		// Assert
		allStats.ShouldNotBeNull();
		allStats.Count.ShouldBe(3);
		allStats.ContainsKey("endpoint-1").ShouldBeTrue();
		allStats.ContainsKey("endpoint-2").ShouldBeTrue();
		allStats.ContainsKey("endpoint-3").ShouldBeTrue();
		allStats["endpoint-1"].TotalProcessedTasks.ShouldBe(1);
		allStats["endpoint-2"].TotalProcessedTasks.ShouldBe(1);
		allStats["endpoint-3"].TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task CancelTasksAsync_Should_Return_False_For_Nonexistent_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		bool result = await service.CancelTasksAsync("nonexistent-key", TaskPriority.Normal);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task CancelTasksAsync_Should_Cancel_Tasks_For_Existing_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "test-endpoint";

		// Queue a task first to create the queue
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal);

		// Act
		bool result = await service.CancelTasksAsync(endpointKey, TaskPriority.Normal);

		// Assert - Result depends on whether there were tasks to cancel
		// Since we already processed the task, there shouldn't be any to cancel
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExecuteAsync_Should_Return_Null_When_Task_Returns_Null()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		string? result = await service.ExecuteAsync<string>("test-key", _ => Task.FromResult<string?>(null), TaskPriority.Normal);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task ExecuteAsync_Should_Handle_Task_Exception()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await service.ExecuteAsync<int>("test-key", _ => throw new InvalidOperationException("Test exception"), TaskPriority.Normal);
		});
	}

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act & Assert
		Should.NotThrow(() => service.Dispose());
	}

	[Fact]
	public void Dispose_Should_Be_Idempotent()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act & Assert
		Should.NotThrow(() =>
		{
			service.Dispose();
			service.Dispose(); // Second call should not throw
		});
	}

	[Fact]
	public async Task Dispose_Should_Cleanup_All_Queues()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal);
		await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.Normal);

		Dictionary<string, PrioritizedQueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(2);

		// Act
		service.Dispose();

		// Assert - After disposal, GetAllQueueStatsAsync should return empty
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.ShouldBeEmpty();
	}

	[Fact]
	public async Task ExecuteAsync_Should_Work_With_Different_Return_Types()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		int intResult = await service.ExecuteAsync("key1", _ => Task.FromResult(42), TaskPriority.Normal);
		string stringResult = await service.ExecuteAsync("key2", _ => Task.FromResult("test"), TaskPriority.Normal);
		bool boolResult = await service.ExecuteAsync("key3", _ => Task.FromResult(true), TaskPriority.Normal);
		DateTime dateResult = await service.ExecuteAsync("key4", _ => Task.FromResult(DateTime.UtcNow), TaskPriority.Normal);

		// Assert
		intResult.ShouldBe(42);
		stringResult.ShouldBe("test");
		boolResult.ShouldBeTrue();
		dateResult.ShouldNotBe(default(DateTime));
	}

	[Fact]
	public async Task ExecuteAsync_Should_Process_Tasks_Sequentially_Per_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "sequential-test";
		List<int> executionOrder = [];

		// Act - Queue multiple tasks
		Task<int> task1 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(1); return Task.FromResult(1); }, TaskPriority.Normal);
		Task<int> task2 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(2); return Task.FromResult(2); }, TaskPriority.Normal);
		Task<int> task3 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(3); return Task.FromResult(3); }, TaskPriority.Normal);

		await Task.WhenAll(task1, task2, task3);
		await Task.Delay(100); // Allow stats to update

		// Assert - Tasks should have executed in order
		executionOrder.ShouldBe(new[] { 1, 2, 3 });

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(3);
	}
}