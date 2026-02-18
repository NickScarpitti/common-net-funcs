using System.Collections.Concurrent;
using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public class EndpointQueueServiceTests : IDisposable
{
	private readonly List<EndpointQueueService> _servicesToDispose = new();

	[Fact]
	public async Task ExecuteAsync_Bounded_Should_Invoke_Queue()
	{
		// Arrange

		BoundedChannelOptions options = new(1);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		int? result = await service.ExecuteAsync("key", _ => Task.FromResult(77), options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldBe(77);
	}

	[Fact]
	public async Task ExecuteAsync_Unbounded_Should_Invoke_Queue()
	{
		// Arrange

		UnboundedChannelOptions options = new();
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		int? result = await service.ExecuteAsync("key", _ => Task.FromResult(88), options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldBe(88);
	}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Stats()
	{
		// Arrange

		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		QueueStats stats = await service.GetQueueStatsAsync("key");

		// Assert

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("key");
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_AllStats()
	{
		// Arrange

		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();

		// Assert

		allStats.ShouldNotBeNull();
		allStats.ShouldBeOfType<Dictionary<string, QueueStats>>();
	}

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		// Arrange

		EndpointQueueService service = new();

		// Act & Assert

		service.Dispose();
		Should.NotThrow(service.Dispose); // Should be idempotent

	}

	[Fact]
	public async Task ExecuteAsync_Bounded_Should_Reuse_Same_Queue_For_Same_Key()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string key = "test-endpoint";

		// Act

		int? result1 = await service.ExecuteAsync(key, _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		int? result2 = await service.ExecuteAsync(key, _ => Task.FromResult(2), options, TestContext.Current.CancellationToken);
		int? result3 = await service.ExecuteAsync(key, _ => Task.FromResult(3), options, TestContext.Current.CancellationToken);

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert

		result1.ShouldBe(1);
		result2.ShouldBe(2);
		result3.ShouldBe(3);

		QueueStats stats = await service.GetQueueStatsAsync(key);
		stats.QueuedTasks.ShouldBe(3);
		stats.ProcessedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task ExecuteAsync_Unbounded_Should_Reuse_Same_Queue_For_Same_Key()
	{
		// Arrange

		UnboundedChannelOptions options = new();
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string key = "test-endpoint-unbounded";

		// Act

		int? result1 = await service.ExecuteAsync(key, _ => Task.FromResult(10), options, TestContext.Current.CancellationToken);
		int? result2 = await service.ExecuteAsync(key, _ => Task.FromResult(20), options, TestContext.Current.CancellationToken);

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert

		result1.ShouldBe(10);
		result2.ShouldBe(20);

		QueueStats stats = await service.GetQueueStatsAsync(key);
		stats.QueuedTasks.ShouldBe(2);
		stats.ProcessedTasks.ShouldBe(2);
	}

	[Fact]
	public async Task ExecuteAsync_Should_Create_Separate_Queues_For_Different_Keys()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		int? result1 = await service.ExecuteAsync("endpoint1", _ => Task.FromResult(100), options, TestContext.Current.CancellationToken);
		int? result2 = await service.ExecuteAsync("endpoint2", _ => Task.FromResult(200), options, TestContext.Current.CancellationToken);
		int? result3 = await service.ExecuteAsync("endpoint3", _ => Task.FromResult(300), options, TestContext.Current.CancellationToken);

		// Assert

		result1.ShouldBe(100);
		result2.ShouldBe(200);
		result3.ShouldBe(300);

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(3);
		allStats.ShouldContainKey("endpoint1");
		allStats.ShouldContainKey("endpoint2");
		allStats.ShouldContainKey("endpoint3");
	}

	[Fact]
	public async Task GetQueueStatsAsync_Should_Return_Empty_Stats_For_NonExistent_Queue()
	{
		// Arrange

		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string nonExistentKey = "non-existent-endpoint";

		// Act

		QueueStats stats = await service.GetQueueStatsAsync(nonExistentKey);

		// Assert

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe(nonExistentKey);
		stats.QueuedTasks.ShouldBe(0);
		stats.ProcessedTasks.ShouldBe(0);
		stats.FailedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Empty_Dictionary_When_No_Queues_Exist()
	{
		// Arrange

		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();

		// Assert

		allStats.ShouldNotBeNull();
		allStats.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ExecuteAsync_Bounded_Should_Execute_Tasks_Sequentially()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string key = "sequential-test";
		List<int> executionOrder = new();
		object lockObj = new();

		// Act

		Task<int> task1 = service.ExecuteAsync(key, async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(1); }
			return 1;
		}, options, TestContext.Current.CancellationToken);

		Task<int> task2 = service.ExecuteAsync(key, async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(2); }
			return 2;
		}, options, TestContext.Current.CancellationToken);

		Task<int> task3 = service.ExecuteAsync(key, async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(3); }
			return 3;
		}, options, TestContext.Current.CancellationToken);

		// Assert

		int? result1 = await task1;
		int? result2 = await task2;
		int? result3 = await task3;

		result1.ShouldBe(1);
		result2.ShouldBe(2);
		result3.ShouldBe(3);
		executionOrder.ShouldBe(new List<int> { 1, 2, 3 });
	}

	[Fact]
	public async Task ExecuteAsync_Should_Handle_Task_Exceptions()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string key = "exception-test";

		// Act & Assert

		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await service.ExecuteAsync<int>(key, _ => throw new InvalidOperationException("Test exception"), options, TestContext.Current.CancellationToken);
		});

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Verify stats were updated

		QueueStats stats = await service.GetQueueStatsAsync(key);
		stats.FailedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task ExecuteAsync_Mixed_Bounded_And_Unbounded_Should_Work()
	{
		// Arrange

		BoundedChannelOptions boundedOptions = new(5);
		UnboundedChannelOptions unboundedOptions = new();
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act - Create queue with bounded options first

		int? result1 = await service.ExecuteAsync("mixed-key", _ => Task.FromResult(1), boundedOptions, TestContext.Current.CancellationToken);

		// Try to use the same key with unbounded options (should reuse existing queue)

		int? result2 = await service.ExecuteAsync("mixed-key", _ => Task.FromResult(2), unboundedOptions, TestContext.Current.CancellationToken);

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert

		result1.ShouldBe(1);
		result2.ShouldBe(2);

		QueueStats stats = await service.GetQueueStatsAsync("mixed-key");
		stats.QueuedTasks.ShouldBe(2);
	}

	[Fact]
	public async Task ExecuteAsync_With_Null_Result_Should_Work()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act

		string? result = await service.ExecuteAsync("null-test", _ => Task.FromResult<string?>(null), options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldBeNull();
	}

	[Fact]
	public async Task ExecuteAsync_With_Complex_Object_Should_Work()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		TestData testData = new()
		{
			Id = 42,
			Name = "Test",
			Values = new List<string> { "A", "B", "C" }
		};

		// Act

		TestData? result = await service.ExecuteAsync("complex-test", _ => Task.FromResult(testData), options, TestContext.Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Id.ShouldBe(42);
		result.Name.ShouldBe("Test");
		result.Values.Count.ShouldBe(3);
	}

	[Fact]
	public async Task Multiple_Services_Should_Be_Independent()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service1 = new();
		EndpointQueueService service2 = new();
		_servicesToDispose.Add(service1);
		_servicesToDispose.Add(service2);

		// Act

		int? result1 = await service1.ExecuteAsync("shared-key", _ => Task.FromResult(100), options, TestContext.Current.CancellationToken);
		int? result2 = await service2.ExecuteAsync("shared-key", _ => Task.FromResult(200), options, TestContext.Current.CancellationToken);

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert

		result1.ShouldBe(100);
		result2.ShouldBe(200);

		// Services should have independent stats

		QueueStats stats1 = await service1.GetQueueStatsAsync("shared-key");
		QueueStats stats2 = await service2.GetQueueStatsAsync("shared-key");

		stats1.QueuedTasks.ShouldBe(1);
		stats2.QueuedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task Dispose_Should_Dispose_All_Queues()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();

		// Create multiple queues

		_ = await service.ExecuteAsync("queue1", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		_ = await service.ExecuteAsync("queue2", _ => Task.FromResult(2), options, TestContext.Current.CancellationToken);
		_ = await service.ExecuteAsync("queue3", _ => Task.FromResult(3), options, TestContext.Current.CancellationToken);

		Dictionary<string, QueueStats> statsBeforeDispose = await service.GetAllQueueStatsAsync();
		statsBeforeDispose.Count.ShouldBe(3);

		// Act

		service.Dispose();

		// Assert - Service should still respond but queues are disposed

		Dictionary<string, QueueStats> statsAfterDispose = await service.GetAllQueueStatsAsync();
		statsAfterDispose.Count.ShouldBe(0);
	}

	[Fact]
	public async Task Dispose_Multiple_Times_Should_Be_Safe()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_ = await service.ExecuteAsync("test", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);

		// Act & Assert

		service.Dispose();
		service.Dispose();
		service.Dispose();

		Should.NotThrow(() => service.Dispose());
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Include_All_Queue_Stats()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Create multiple queues with different activity

		_ = await service.ExecuteAsync("endpoint-a", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		_ = await service.ExecuteAsync("endpoint-a", _ => Task.FromResult(2), options, TestContext.Current.CancellationToken);

		_ = await service.ExecuteAsync("endpoint-b", _ => Task.FromResult(10), options, TestContext.Current.CancellationToken);

		_ = await service.ExecuteAsync("endpoint-c", _ => Task.FromResult(100), options, TestContext.Current.CancellationToken);
		_ = await service.ExecuteAsync("endpoint-c", _ => Task.FromResult(200), options, TestContext.Current.CancellationToken);
		_ = await service.ExecuteAsync("endpoint-c", _ => Task.FromResult(300), options, TestContext.Current.CancellationToken);

		// Give async stats tracking a moment to complete

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Act

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();

		// Assert

		allStats.Count.ShouldBe(3);
		allStats["endpoint-a"].QueuedTasks.ShouldBe(2);
		allStats["endpoint-b"].QueuedTasks.ShouldBe(1);
		allStats["endpoint-c"].QueuedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task ExecuteAsync_Concurrent_Requests_To_Different_Endpoints_Should_Work()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Act - Execute tasks concurrently to different endpoints

		Task[] tasks = new Task[10];
		for (int i = 0; i < 10; i++)
		{
			int captured = i;
			tasks[i] = Task.Run(async () =>
			{
				int? result = await service.ExecuteAsync($"endpoint-{captured}", _ => Task.FromResult(captured * 10), options, TestContext.Current.CancellationToken);
				result.ShouldBe(captured * 10);
			}, TestContext.Current.CancellationToken);
		}

		await Task.WhenAll(tasks);

		// Assert

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(10);
	}

	[Fact]
	public async Task ExecuteAsync_Should_Track_LastProcessedAt()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);
		const string key = "timestamp-test";
		DateTime startTime = DateTime.UtcNow;

		// Act

		_ = await service.ExecuteAsync(key, _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);

		// Wait a bit to ensure processing completes

		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert

		QueueStats stats = await service.GetQueueStatsAsync(key);
		stats.LastProcessedAt.ShouldNotBeNull();
		stats.LastProcessedAt!.Value.ShouldBeGreaterThanOrEqualTo(startTime);
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Not_Remove_Recently_Used_Queues()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Create a queue by executing a task

		_ = await service.ExecuteAsync("test-queue", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);

		// Get the cleanup method using reflection

		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService).GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - invoke cleanup (should not remove recently used queue)

		cleanupMethod!.Invoke(service, new object?[] { null });

		// Assert - queue should still exist

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(1);
		allStats.ShouldContainKey("test-queue");
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Remove_Old_Unused_Queues()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Create a queue

		_ = await service.ExecuteAsync("old-queue", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure processing completes

		// Get the private queues dictionary using reflection

		System.Reflection.FieldInfo? queuesField = typeof(EndpointQueueService).GetField("queues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		queuesField.ShouldNotBeNull();
		ConcurrentDictionary<string, CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue>? queuesDict = queuesField!.GetValue(service) as System.Collections.Concurrent.ConcurrentDictionary<string, CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue>;
		queuesDict.ShouldNotBeNull();

		// Get the queue and its internal stats field

		queuesDict!.TryGetValue("old-queue", out CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue? queue).ShouldBeTrue();
		queue.ShouldNotBeNull();

		System.Reflection.FieldInfo? statsField = typeof(CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue).GetField("stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		statsField.ShouldNotBeNull();
		QueueStats? statsObj = statsField!.GetValue(queue) as QueueStats;
		statsObj.ShouldNotBeNull();

		// Manipulate the stats to have an old LastProcessedAt timestamp

		statsObj!.LastProcessedAt = DateTime.UtcNow.AddHours(-1); // Set to 1 hour ago

		// Get the cleanup method using reflection

		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService).GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - invoke cleanup (should remove the old queue)

		cleanupMethod!.Invoke(service, new object?[] { null });

		// Assert - queue should be removed

		Dictionary<string, QueueStats> allStats = await service.GetAllQueueStatsAsync();
		allStats.Count.ShouldBe(0);
	}

	[Fact]
	public void Constructor_With_CleanupInterval_Should_Create_Service()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(10);

		// Act
		EndpointQueueService service = new(cleanupInterval);
		_servicesToDispose.Add(service);

		// Assert - Service should be created successfully
		service.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_With_CleanupInterval_And_CutoffTime_Should_Create_Service()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(2);
		double cutoffTimeMinutes = 15.0;

		// Act
		EndpointQueueService service = new(cleanupInterval, cutoffTimeMinutes);
		_servicesToDispose.Add(service);

		// Assert - Service should be created successfully
		service.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_With_Negative_CutoffTime_Should_Use_Absolute_Value()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(1);
		double negativeCutoffTime = -45.0;

		// Act
		EndpointQueueService service = new(cleanupInterval, negativeCutoffTime);
		_servicesToDispose.Add(service);

		// Assert - Service should be created and Math.Abs should be used
		service.ShouldNotBeNull();

		// Verify cutoffTimeMinutes field via reflection
		System.Reflection.FieldInfo? cutoffField = typeof(EndpointQueueService)
			.GetField("cutoffTimeMinutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cutoffField.ShouldNotBeNull();
		double actualCutoff = (double)cutoffField.GetValue(service)!;
		actualCutoff.ShouldBe(45.0); // Should be absolute value
	}

	[Fact]
	public async Task CleanupUnusedQueues_With_Custom_Cutoff_Should_Remove_Old_Queues()
	{
		// Arrange - Use very short cutoff time (0.01 minutes = 0.6 seconds)
		TimeSpan cleanupInterval = TimeSpan.FromHours(1); // Long interval so timer won't fire automatically
		double cutoffTimeMinutes = 0.01; // 0.6 seconds

		EndpointQueueService service = new(cleanupInterval, cutoffTimeMinutes);
		_servicesToDispose.Add(service);

		BoundedChannelOptions options = new(10);

		// Create and execute a task
		await service.ExecuteAsync("short-lived-queue", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		await Task.Delay(150, TestContext.Current.CancellationToken);

		// Verify queue exists
		Dictionary<string, QueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(1);

		// Wait for cutoff time to pass
		await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait 1 second (> 0.6 seconds cutoff)

		// Get cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Queue should be removed because it's older than cutoff
		Dictionary<string, QueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(0);
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Remove_Multiple_Old_Queues()
	{
		// Arrange - Use short cutoff time
		TimeSpan cleanupInterval = TimeSpan.FromHours(1);
		double cutoffTimeMinutes = 0.02; // 1.2 seconds

		EndpointQueueService service = new(cleanupInterval, cutoffTimeMinutes);
		_servicesToDispose.Add(service);

		BoundedChannelOptions options = new(10);

		// Create multiple queues
		await service.ExecuteAsync("old-queue-1", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		await service.ExecuteAsync("old-queue-2", _ => Task.FromResult(2), options, TestContext.Current.CancellationToken);
		await service.ExecuteAsync("old-queue-3", _ => Task.FromResult(3), options, TestContext.Current.CancellationToken);
		await Task.Delay(200, TestContext.Current.CancellationToken);

		Dictionary<string, QueueStats> statsBefore = await service.GetAllQueueStatsAsync();
		statsBefore.Count.ShouldBe(3);

		// Wait for cutoff
		await Task.Delay(1500, TestContext.Current.CancellationToken);

		// Get cleanup method
		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - All queues should be removed
		Dictionary<string, QueueStats> statsAfter = await service.GetAllQueueStatsAsync();
		statsAfter.Count.ShouldBe(0);
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Log_When_Removing_Queue()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromHours(1);
		double cutoffTimeMinutes = 0.01;

		EndpointQueueService service = new(cleanupInterval, cutoffTimeMinutes);
		_servicesToDispose.Add(service);

		BoundedChannelOptions options = new(10);

		// Create a queue
		await service.ExecuteAsync("queue-to-remove", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		await Task.Delay(150, TestContext.Current.CancellationToken);

		// Wait for cutoff
		await Task.Delay(1000, TestContext.Current.CancellationToken);

		// Get cleanup method
		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		// Act - Invoke cleanup (logger will be called internally)
		cleanupMethod!.Invoke(service, [null]);
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Queue should be removed (logger.Info was called but we can't verify it)
		Dictionary<string, QueueStats> stats = await service.GetAllQueueStatsAsync();
		stats.Count.ShouldBe(0);
	}

	[Fact]
	public async Task CleanupUnusedQueues_Should_Iterate_All_Queues()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromHours(1);
		double cutoffTimeMinutes = 30.0; // Default cutoff, queues won't be removed

		EndpointQueueService service = new(cleanupInterval, cutoffTimeMinutes);
		_servicesToDispose.Add(service);

		BoundedChannelOptions options = new(10);

		// Create multiple queues
		await service.ExecuteAsync("queue-A", _ => Task.FromResult(1), options, TestContext.Current.CancellationToken);
		await service.ExecuteAsync("queue-B", _ => Task.FromResult(2), options, TestContext.Current.CancellationToken);
		await service.ExecuteAsync("queue-C", _ => Task.FromResult(3), options, TestContext.Current.CancellationToken);
		await Task.Delay(150, TestContext.Current.CancellationToken);

		// Get the cleanup method via reflection
		System.Reflection.MethodInfo? cleanupMethod = typeof(EndpointQueueService)
			.GetMethod("CleanupUnusedQueues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cleanupMethod.ShouldNotBeNull();

		// Act - Invoke cleanup (queues are recent so won't be removed)
		cleanupMethod.Invoke(service, [null]);
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert - All queues should still exist
		Dictionary<string, QueueStats> stats = await service.GetAllQueueStatsAsync();
		stats.Count.ShouldBe(3);
		stats.ContainsKey("queue-A").ShouldBeTrue();
		stats.ContainsKey("queue-B").ShouldBeTrue();
		stats.ContainsKey("queue-C").ShouldBeTrue();
	}

	[Fact]
	public async Task Default_Constructor_Should_Use_Default_CutoffTime()
	{
		// Arrange & Act
		EndpointQueueService service = new();
		_servicesToDispose.Add(service);

		// Assert - Verify cutoffTimeMinutes field is 30.0 (default)
		System.Reflection.FieldInfo? cutoffField = typeof(EndpointQueueService)
			.GetField("cutoffTimeMinutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cutoffField.ShouldNotBeNull();
		double actualCutoff = (double)cutoffField.GetValue(service)!;
		actualCutoff.ShouldBe(30.0); // Default value

		// Service should work normally
		BoundedChannelOptions options = new(10);
		int? result = await service.ExecuteAsync("test", _ => Task.FromResult(42), options, TestContext.Current.CancellationToken);
		result.ShouldBe(42);
	}

	[Fact]
	public void Constructor_With_CleanupInterval_Should_Use_Default_CutoffTime()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(10);

		// Act
		EndpointQueueService service = new(cleanupInterval);
		_servicesToDispose.Add(service);

		// Assert - Verify cutoffTimeMinutes field is 30.0 (default)
		System.Reflection.FieldInfo? cutoffField = typeof(EndpointQueueService)
			.GetField("cutoffTimeMinutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cutoffField.ShouldNotBeNull();
		double actualCutoff = (double)cutoffField.GetValue(service)!;
		actualCutoff.ShouldBe(30.0); // Default value
	}

	[Fact]
	public void Constructor_With_Zero_CutoffTime_Should_Use_Absolute_Value()
	{
		// Arrange
		TimeSpan cleanupInterval = TimeSpan.FromMinutes(1);
		double zeroCutoffTime = 0.0;

		// Act
		EndpointQueueService service = new(cleanupInterval, zeroCutoffTime);
		_servicesToDispose.Add(service);

		// Assert - Math.Abs(0) = 0
		System.Reflection.FieldInfo? cutoffField = typeof(EndpointQueueService)
			.GetField("cutoffTimeMinutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		cutoffField.ShouldNotBeNull();
		double actualCutoff = (double)cutoffField.GetValue(service)!;
		actualCutoff.ShouldBe(0.0);
	}

	private bool _disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				foreach (EndpointQueueService service in _servicesToDispose)
				{
					service?.Dispose();
				}
				_servicesToDispose.Clear();
			}
			_disposed = true;
		}
	}

	private class TestData
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public List<string> Values { get; set; } = new();
	}
}
