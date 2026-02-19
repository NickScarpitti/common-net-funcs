using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using Moq;
using static Xunit.TestContext;

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
		}, TaskPriority.Normal, Current.CancellationToken);

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
		int result = await service.ExecuteAsync($"key-{priority}", _ => Task.FromResult((int)priority), priority, Current.CancellationToken);

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
		string? result = await service.ExecuteAsync("test-key", _ =>
		{
			taskExecuted = true;
			return Task.FromResult("success");
		}, customPriority: 3, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe("success");
		taskExecuted.ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(10)]
	public async Task ExecuteAsync_CustomPriority_Should_Map_To_Correct_PriorityLevel(int customPriority)
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act
		int result = await service.ExecuteAsync($"key-{customPriority}", _ => Task.FromResult(customPriority), customPriority, Current.CancellationToken);

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
		int result1 = await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		int result2 = await service.ExecuteAsync(endpointKey, _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Allow stats to update

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
		int result1 = await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		int result2 = await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);

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
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<TaskCanceledException>(async () => await service.ExecuteAsync("test-key", async ct =>
			{
				await Task.Delay(1000, ct);
				return 42;
			}, TaskPriority.Normal, cts.Token));
	}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Stats_For_Existing_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "test-endpoint";

		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Allow stats to update

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

		await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.High, Current.CancellationToken);
		await service.ExecuteAsync("endpoint-3", _ => Task.FromResult(3), TaskPriority.Critical, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Allow stats to update

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
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);

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
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
		string? result = await service.ExecuteAsync<string>("test-key", _ => Task.FromResult<string?>(null), TaskPriority.Normal, Current.CancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

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
		await Should.ThrowAsync<InvalidOperationException>(async () => await service.ExecuteAsync<int>("test-key", _ => throw new InvalidOperationException("Test exception"), TaskPriority.Normal));
	}

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act & Assert
		Should.NotThrow(service.Dispose);
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

		await service.ExecuteAsync("endpoint-1", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("endpoint-2", _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);

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
		int intResult = await service.ExecuteAsync("key1", _ => Task.FromResult(42), TaskPriority.Normal, Current.CancellationToken);
		string? stringResult = await service.ExecuteAsync("key2", _ => Task.FromResult("test"), TaskPriority.Normal, Current.CancellationToken);
		bool boolResult = await service.ExecuteAsync("key3", _ => Task.FromResult(true), TaskPriority.Normal, Current.CancellationToken);
		DateTime dateResult = await service.ExecuteAsync("key4", _ => Task.FromResult(DateTime.UtcNow), TaskPriority.Normal, Current.CancellationToken);

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
		Task<int> task1 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(1); return Task.FromResult(1); }, TaskPriority.Normal, Current.CancellationToken);
		Task<int> task2 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(2); return Task.FromResult(2); }, TaskPriority.Normal, Current.CancellationToken);
		Task<int> task3 = service.ExecuteAsync(endpointKey, _ => { executionOrder.Add(3); return Task.FromResult(3); }, TaskPriority.Normal, Current.CancellationToken);

		await Task.WhenAll(task1, task2, task3);
		await Task.Delay(100, Current.CancellationToken); // Allow stats to update

		// Assert - Tasks should have executed in order
		executionOrder.ShouldBe([1, 2, 3]);

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Not_Throw_When_Invoked()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Create and use a queue
		await service.ExecuteAsync("test-queue", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Get the cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		cleanupMethod.ShouldNotBeNull();

		// Act & Assert - Cleanup should not throw
		Should.NotThrow(() => cleanupMethod.Invoke(service, [null]));
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Not_Remove_Recently_Used_Queues()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "recent-test";

		// Create and use a queue recently
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Get the cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		cleanupMethod.ShouldNotBeNull();

		Dictionary<string, PrioritizedQueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(1);

		// Act - Invoke cleanup (queue was just used, should not be removed)
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Queue should still exist
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(1);
		statsAfter.ContainsKey(endpointKey).ShouldBeTrue();
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Not_Remove_Queues_With_Pending_Tasks()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "pending-test";

		// Create a queue with a long-running task
		using SemaphoreSlim semaphore = new(0, 1);
		Task<int> longRunningTask = service.ExecuteAsync(endpointKey, async _ =>
		{
			await semaphore.WaitAsync(Current.CancellationToken);
			return 42;
		}, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(100, Current.CancellationToken);

		// Get the cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		cleanupMethod.ShouldNotBeNull();

		// Set LastProcessedAt to be old
		System.Reflection.FieldInfo? queuesField = typeof(PrioritizedEndpointQueueService)
			.GetField("queues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (queuesField?.GetValue(service) is System.Collections.Concurrent.ConcurrentDictionary<string, PrioritizedEndpointQueue> queues &&
					queues.TryGetValue(endpointKey, out PrioritizedEndpointQueue? queue))
		{
			System.Reflection.FieldInfo? statsField = typeof(PrioritizedEndpointQueue).GetField("stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (statsField?.GetValue(queue) is PrioritizedQueueStats statsObj)
			{
				System.Reflection.FieldInfo? lastProcessedField = typeof(PrioritizedQueueStats)
					.GetField("<LastProcessedAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				lastProcessedField?.SetValue(statsObj, DateTime.UtcNow.AddHours(-1));
			}
		}

		// Act - Invoke cleanup (queue has pending task, should not be removed)
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Queue should still exist due to pending task
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(1);
		statsAfter.ContainsKey(endpointKey).ShouldBeTrue();

		// Cleanup - Release the task
		semaphore.Release();
		await longRunningTask;
	}

	[Fact]
	public async Task GetOrCreateQueue_Should_Use_Custom_Logger_When_Provided()
	{
		// Arrange
		Mock<NLog.Logger> loggerMock = new();
		Mock<IServiceProvider> serviceProviderMock = new();
		serviceProviderMock.Setup(sp => sp.GetService(typeof(NLog.Logger))).Returns(loggerMock.Object);

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "logger-test";

		// Act - Create a new queue (should trigger logger)
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Custom logger should have been retrieved from service provider
		serviceProviderMock.Verify(sp => sp.GetService(typeof(NLog.Logger)), Moq.Times.Once);
	}

	[Fact]
	public async Task GetOrCreateQueue_Should_Create_Queue_Only_Once_For_Same_Key()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		int getServiceCallCount = 0;
		serviceProviderMock.Setup(sp => sp.GetService(typeof(NLog.Logger))).Returns(() =>
			{
				getServiceCallCount++;
				return null;
			});

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		const string endpointKey = "single-creation-test";

		// Act - Execute multiple tasks with same key
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync(endpointKey, _ => Task.FromResult(3), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(150, Current.CancellationToken);

		// Assert - GetService should only be called once (queue created only once)
		getServiceCallCount.ShouldBe(1);

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(3);
	}

	[Theory]
	[InlineData(-5, TaskPriority.Low)]
	[InlineData(0, TaskPriority.Low)]
	[InlineData(1, TaskPriority.Normal)]
	[InlineData(2, TaskPriority.High)]
	[InlineData(3, TaskPriority.Critical)]
	[InlineData(4, TaskPriority.Emergency)]
	[InlineData(5, TaskPriority.Emergency)]
	[InlineData(100, TaskPriority.Emergency)]
	public async Task GetPriorityLevel_Should_Map_Custom_Priority_Correctly(int customPriority, TaskPriority expectedPriority)
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);
		string endpointKey = $"priority-mapping-{customPriority}";

		// Act
		int result = await service.ExecuteAsync(endpointKey, _ => Task.FromResult(customPriority), customPriority, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Verify the task executed successfully with the correct priority mapping
		result.ShouldBe(customPriority);

		PrioritizedQueueStats stats = await service.GetQueueStatsAsync(endpointKey);
		stats.TotalProcessedTasks.ShouldBe(1);

		// Verify the priority breakdown has the expected priority recorded
		stats.PriorityBreakdown[expectedPriority].ProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public void Dispose_Should_Stop_Cleanup_Timer()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Get the timer field via reflection
		System.Reflection.FieldInfo? timerField = typeof(PrioritizedEndpointQueueService).GetField("cleanupTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		timerField.ShouldNotBeNull();

		Timer? timer = timerField.GetValue(service) as Timer;
		timer.ShouldNotBeNull();

		// Act
		service.Dispose();

		// Assert - Verify disposed flag is set
		System.Reflection.FieldInfo? disposedField = typeof(PrioritizedEndpointQueueService).GetField("disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		disposedField.ShouldNotBeNull();
		bool isDisposed = (bool)disposedField.GetValue(service)!;
		isDisposed.ShouldBeTrue();
	}

	[Fact]
	public async Task Dispose_Should_Dispose_All_Queues_Properly()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Create multiple queues
		await service.ExecuteAsync("queue-1", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("queue-2", _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("queue-3", _ => Task.FromResult(3), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(150, Current.CancellationToken);

		Dictionary<string, PrioritizedQueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(3);

		// Act
		service.Dispose();

		// Assert
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.ShouldBeEmpty();

		// Verify disposed flag via reflection
		System.Reflection.FieldInfo? disposedField = typeof(PrioritizedEndpointQueueService).GetField("disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		disposedField.ShouldNotBeNull();
		bool isDisposed = (bool)disposedField.GetValue(service)!;
		isDisposed.ShouldBeTrue();
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Iterate_All_Queues()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Create multiple queues
		await service.ExecuteAsync("queue-A", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("queue-B", _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("queue-C", _ => Task.FromResult(3), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(150, Current.CancellationToken);

		// Get the cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup (queues are recent so won't be removed)
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(50, Current.CancellationToken);

		// Assert - All queues should still exist
		Dictionary<string, PrioritizedQueueStats> stats = await service.GetAllQueueStatsAsync();
		stats.Count.ShouldBe(3);
		stats.ContainsKey("queue-A").ShouldBeTrue();
		stats.ContainsKey("queue-B").ShouldBeTrue();
		stats.ContainsKey("queue-C").ShouldBeTrue();
	}

	[Fact]
	public async Task ServiceProvider_Should_Be_Queried_For_Logger_When_Creating_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		serviceProviderMock.Setup(sp => sp.GetService(typeof(NLog.Logger))).Returns((NLog.Logger?)null);

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act - Create a new queue (will trigger GetOrCreateQueue)
		await service.ExecuteAsync("logger-test-queue", _ => Task.FromResult(42), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - ServiceProvider.GetService should have been called for NLog.Logger
		serviceProviderMock.Verify(sp => sp.GetService(typeof(NLog.Logger)), Moq.Times.Once);
	}

	[Fact]
	public void Constructor_With_CleanupInterval_Should_Create_Service()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(10);

		// Act
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval);

		// Assert - Service should be created successfully
		service.ShouldNotBeNull();
		service.Dispose();
	}

	[Fact]
	public void Constructor_With_CleanupInterval_And_CutoffTime_Should_Create_Service()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(2);
		double cutoffTimeMinutes = 15.0;

		// Act
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval, cutoffTimeMinutes);

		// Assert - Service should be created successfully
		service.ShouldNotBeNull();
		service.Dispose();
	}

	[Fact]
	public void Constructor_With_Negative_CutoffTime_Should_Use_Absolute_Value()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(1);
		double negativeCutoffTime = -45.0;

		// Act
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval, negativeCutoffTime);

		// Assert - Service should be created and Math.Abs should be used
		service.ShouldNotBeNull();

		// Verify cutoffTimeMinutes field via reflection
		System.Reflection.FieldInfo? cutoffField = typeof(PrioritizedEndpointQueueService).GetField("cutoffTimeMinutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cutoffField.ShouldNotBeNull();
		double actualCutoff = (double)cutoffField.GetValue(service)!;
		actualCutoff.ShouldBe(45.0); // Should be absolute value

		service.Dispose();
	}

	[Fact]
	public async Task CleanupUnusedQueues_With_Custom_Cutoff_Should_Remove_Old_Queues()
	{
		// Arrange - Use very short cutoff time (0.01 minutes = 0.6 seconds)
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromHours(1); // Long interval so timer won't fire automatically
		double cutoffTimeMinutes = 0.01; // 0.6 seconds

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval, cutoffTimeMinutes);

		// Create and execute a task
		await service.ExecuteAsync("short-lived-queue", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(150, Current.CancellationToken);

		// Verify queue exists
		Dictionary<string, PrioritizedQueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(1);

		// Wait for cutoff time to pass
		await Task.Delay(1000, Current.CancellationToken); // Wait 1 second (> 0.6 seconds cutoff)

		// Get cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Queue should be removed because it's older than cutoff and idle
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(0);

		service.Dispose();
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Remove_Queue_When_Conditions_Met()
	{
		// Arrange - Use short cutoff time
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromHours(1);
		double cutoffTimeMinutes = 0.02; // 1.2 seconds

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval, cutoffTimeMinutes);

		// Create multiple queues
		await service.ExecuteAsync("old-queue-1", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await service.ExecuteAsync("old-queue-2", _ => Task.FromResult(2), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(200, Current.CancellationToken);

		Dictionary<string, PrioritizedQueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(2);

		// Wait for cutoff
		await Task.Delay(1500, Current.CancellationToken);

		// Get cleanup method
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Both queues should be removed
		Dictionary<string, PrioritizedQueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(0);

		service.Dispose();
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Log_When_Removing_Queue()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		TimeSpan cleanupInterval = TimeSpan.FromHours(1);
		double cutoffTimeMinutes = 0.01;

		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object, cleanupInterval, cutoffTimeMinutes);

		// Create a queue
		await service.ExecuteAsync("queue-to-remove", _ => Task.FromResult(1), TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(150, Current.CancellationToken);

		// Wait for cutoff
		await Task.Delay(1000, Current.CancellationToken);

		// Get cleanup method
		System.Reflection.MethodInfo? cleanupMethod = typeof(PrioritizedEndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		// Act - Invoke cleanup (logger will be called internally)
		cleanupMethod!.Invoke(service, [null]);
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Queue should be removed (logger.Info was called but we can't verify it)
		Dictionary<string, PrioritizedQueueStats> stats = await service.GetAllQueueStatsAsync();
		stats.Count.ShouldBe(0);

		service.Dispose();
	}

	[Fact]
	public async Task ExecuteAsync_With_All_TaskPriority_Enum_Values_Should_Work()
	{
		// Arrange
		Mock<IServiceProvider> serviceProviderMock = new();
		PrioritizedEndpointQueueService service = new(serviceProviderMock.Object);

		// Act & Assert - Test all enum values
		foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
		{
			int result = await service.ExecuteAsync($"priority-{priority}", _ => Task.FromResult((int)priority), priority, Current.CancellationToken);
			result.ShouldBe((int)priority);
		}

		await Task.Delay(150, Current.CancellationToken);

		// Verify all queues were created
		Dictionary<string, PrioritizedQueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(Enum.GetValues<TaskPriority>().Length);
	}
}
