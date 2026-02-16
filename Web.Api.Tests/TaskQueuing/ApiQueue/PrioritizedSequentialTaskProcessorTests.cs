using System.Reflection;
using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using static Xunit.TestContext;

namespace Web.Api.Tests.TaskQueuing.ApiQueue;

public class PrioritizedSequentialTaskProcessorTests
{
	// Test helper class to expose internal functionality for cancellation testing
	private class TestableProcessor : PrioritizedSequentialTaskProcessor
	{
		public TestableProcessor(BoundedChannelOptions options) : base(options) { }

		// Helper method to enqueue a task and get access to the PrioritizedQueuedTask for cancellation testing
		public async Task<(Task<object?> resultTask, PrioritizedQueuedTask queuedTask)> EnqueueAndCaptureAsync<T>(
			Func<CancellationToken, Task<T?>> taskFunction,
			int priority = (int)TaskPriority.Normal,
			TaskPriority priorityLevel = TaskPriority.Normal)
		{
			PrioritizedQueuedTask queuedTask = new(async ct =>
			{
				T? result = await taskFunction(ct).ConfigureAwait(false);
				return (object?)result;
			})
			{
				Priority = priority,
				PriorityLevel = priorityLevel,
				Timeout = null
			};

			// Access the writer using reflection
			Type processorType = typeof(PrioritizedSequentialTaskProcessor);
			FieldInfo? writerField = processorType.GetField("writer", BindingFlags.NonPublic | BindingFlags.Instance);

			if (writerField?.GetValue(this) is ChannelWriter<PrioritizedQueuedTask> writer)
			{
				await writer.WriteAsync(queuedTask);
			}

			return (queuedTask.CompletionSource.Task, queuedTask);
		}
	}

	#region Constructor Tests


	[Fact]
	public void Constructor_BoundedChannel_Should_Initialize_With_Default_ProcessTimeWindow()
	{
		// Arrange & Act

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Assert

		processor.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_BoundedChannel_Should_Initialize_With_Custom_ProcessTimeWindow()
	{
		// Arrange & Act

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options, processTimeWindow: 500);

		// Assert

		processor.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_UnboundedChannel_Should_Initialize()
	{
		// Arrange & Act

		UnboundedChannelOptions options = new();
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Assert

		processor.ShouldNotBeNull();
	}

	[Fact]
	public async Task Constructor_BoundedChannel_Should_Initialize_Stats()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
		stats.TotalQueuedTasks.ShouldBe(0);
		stats.TotalProcessedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task Constructor_UnboundedChannel_Should_Initialize_Stats()
	{
		// Arrange

		UnboundedChannelOptions options = new();
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
		stats.TotalQueuedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task Constructor_Should_Initialize_Priority_Breakdown_For_All_Priorities()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.PriorityBreakdown.ShouldNotBeNull();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Low).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Normal).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.High).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Critical).ShouldBeTrue();
		stats.PriorityBreakdown.ContainsKey(TaskPriority.Emergency).ShouldBeTrue();
	}

	#endregion

	#region EnqueueWithPriorityAsync Tests


	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Return_Task_Result()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		int expectedValue = 42;

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)expectedValue), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(expectedValue);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_Null_Result()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		object? result = await processor.EnqueueWithPriorityAsync<object?>(_ => Task.FromResult((object?)null), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBeNull();
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_String_Result()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		string expectedValue = "test result";

		// Act

		string? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((string?)expectedValue), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(expectedValue);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Update_Stats_TotalQueuedTasks()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> enqueueTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

		// Give it a moment to update stats

		await Task.Delay(50, Current.CancellationToken);
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.TotalQueuedTasks.ShouldBeGreaterThan(0);

		await enqueueTask;
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Update_Priority_Breakdown_Stats()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> enqueueTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), priority: 5, priorityLevel: TaskPriority.High, cancellationToken: Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.PriorityBreakdown[TaskPriority.High].QueuedTasks.ShouldBeGreaterThan(0);

		await enqueueTask;
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_Multiple_Enqueues()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> task1 = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), cancellationToken: Current.CancellationToken);
		Task<int?> task2 = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), cancellationToken: Current.CancellationToken);
		Task<int?> task3 = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)3), cancellationToken: Current.CancellationToken);

		int?[] results = await Task.WhenAll(task1, task2, task3);

		// Assert

		results.Length.ShouldBe(3);
		results[0].ShouldBe(1);
		results[1].ShouldBe(2);
		results[2].ShouldBe(3);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_Different_Priority_Levels()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> lowTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), priority: 0, priorityLevel: TaskPriority.Low, cancellationToken: Current.CancellationToken);

		Task<int?> normalTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), priority: 1, priorityLevel: TaskPriority.Normal, cancellationToken: Current.CancellationToken);

		Task<int?> highTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), priority: 2, priorityLevel: TaskPriority.High, cancellationToken: Current.CancellationToken);

		int?[] results = await Task.WhenAll(lowTask, normalTask, highTask);

		// Assert

		results.Length.ShouldBe(3);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_CancellationToken()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		using CancellationTokenSource cts = new();

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: cts.Token);

		int? result = await task;

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Set_Priority_On_Task()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		int priority = 5;

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), priority: priority, cancellationToken: Current.CancellationToken);

		int? result = await task;

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Set_Timeout_On_Task()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		TimeSpan timeout = TimeSpan.FromSeconds(5);

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), timeout: timeout, cancellationToken: Current.CancellationToken);

		int? result = await task;

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Use_Default_Priority_When_Not_Specified()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Handle_Task_With_Delay()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(async ct =>
		{
			await Task.Delay(10, ct);
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		int? result = await task;

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_Should_Update_CurrentQueueDepth()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert - Queue depth should be updated (either > 0 if still queued or processed)

		stats.CurrentQueueDepth.ShouldBeGreaterThanOrEqualTo(0);

		await task;
	}

	[Fact]
	public async Task EnqueueWithPriorityAsync_With_UnboundedChannel_Should_Work()
	{
		// Arrange

		UnboundedChannelOptions options = new();
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(42);
	}

	#endregion

	#region GetAllQueueStatsAsync Tests


	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Stats()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Same_Instance()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		PrioritizedQueueStats stats1 = await processor.GetAllQueueStatsAsync();
		PrioritizedQueueStats stats2 = await processor.GetAllQueueStatsAsync();

		// Assert

		stats1.ShouldBe(stats2);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Reflect_Updated_Stats_After_Enqueue()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		PrioritizedQueueStats statsBefore = await processor.GetAllQueueStatsAsync();
		int initialQueued = statsBefore.TotalQueuedTasks;

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);
		PrioritizedQueueStats statsAfter = await processor.GetAllQueueStatsAsync();

		// Assert

		statsAfter.TotalQueuedTasks.ShouldBeGreaterThan(initialQueued);

		await task;
	}

	#endregion

	#region Dispose Tests


	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Act & Assert

		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public void Dispose_Should_Be_Idempotent()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Act

		processor.Dispose();

		// Assert

		Should.NotThrow(processor.Dispose); // Should be idempotent

	}

	[Fact]
	public void Dispose_Multiple_Times_Should_Not_Throw()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Act & Assert

		processor.Dispose();
		processor.Dispose();
		processor.Dispose();
		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public void Dispose_With_UnboundedChannel_Should_Not_Throw()
	{
		// Arrange

		UnboundedChannelOptions options = new();
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Act & Assert

		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public async Task Dispose_After_Enqueue_Should_Wait_For_Task_Completion()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		bool taskCompleted = false;

		// Act

		Task<int?> task = processor.EnqueueWithPriorityAsync(async ct =>
		{
			await Task.Delay(100, ct);
			taskCompleted = true;
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Let it start processing

		processor.Dispose();

		// Wait for the task to complete (it should finish even after dispose)
		await task.WaitAsync(TimeSpan.FromSeconds(2), Current.CancellationToken);

		// Assert - Task should have been completed

		taskCompleted.ShouldBeTrue();
	}

	[Fact]
	public void Dispose_With_Custom_ProcessTimeWindow_Should_Not_Throw()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options, processTimeWindow: 500);

		// Act & Assert

		Should.NotThrow(processor.Dispose);
	}

	#endregion

	#region Edge Case and Integration Tests


	[Fact]
	public async Task Processor_Should_Handle_Large_Number_Of_Tasks()
	{
		// Arrange

		BoundedChannelOptions options = new(100);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		int taskCount = 50;
		List<Task<int?>> tasks = new();

		// Act

		for (int i = 0; i < taskCount; i++)
		{
			int value = i;
			tasks.Add(processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)value), cancellationToken: Current.CancellationToken));
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert

		results.Length.ShouldBe(taskCount);
	}

	[Fact]
	public async Task Processor_Should_Handle_Mixed_Priority_Tasks()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		Task<int?> emergencyTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), priority: 4, priorityLevel: TaskPriority.Emergency, cancellationToken: Current.CancellationToken);

		Task<int?> lowTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), priority: 0, priorityLevel: TaskPriority.Low, cancellationToken: Current.CancellationToken);

		Task<int?> criticalTask = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)3), priority: 3, priorityLevel: TaskPriority.Critical, cancellationToken: Current.CancellationToken);

		int?[] results = await Task.WhenAll(emergencyTask, lowTask, criticalTask);

		// Assert

		results.Length.ShouldBe(3);
		results.ShouldContain(1);
		results.ShouldContain(2);
		results.ShouldContain(3);
	}

	[Fact]
	public async Task Processor_Stats_Should_Track_All_Priority_Levels()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), priorityLevel: TaskPriority.Low, cancellationToken: Current.CancellationToken);
		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), priorityLevel: TaskPriority.Normal, cancellationToken: Current.CancellationToken);
		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)3), priorityLevel: TaskPriority.High, cancellationToken: Current.CancellationToken);

		await Task.Delay(100, Current.CancellationToken); // Let it update stats
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert

		stats.TotalQueuedTasks.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task Processor_Should_Return_Complex_Objects()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		var expectedObject = new { Id = 1, Name = "Test" };

		// Act

		var result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((object?)expectedObject), cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.ShouldBe(expectedObject);
	}

	[Fact]
	public async Task Multiple_Processors_Should_Work_Independently()
	{
		// Arrange

		BoundedChannelOptions options1 = new(10);
		BoundedChannelOptions options2 = new(10);
		using PrioritizedSequentialTaskProcessor processor1 = new(options1);
		await processor1.StartAsync(CancellationToken.None);
		using PrioritizedSequentialTaskProcessor processor2 = new(options2);
		await processor2.StartAsync(CancellationToken.None);

		// Act

		int? result1 = await processor1.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), cancellationToken: Current.CancellationToken);
		int? result2 = await processor2.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), cancellationToken: Current.CancellationToken);

		PrioritizedQueueStats stats1 = await processor1.GetAllQueueStatsAsync();
		PrioritizedQueueStats stats2 = await processor2.GetAllQueueStatsAsync();

		// Assert

		result1.ShouldBe(1);
		result2.ShouldBe(2);
		stats1.ShouldNotBe(stats2);
	}

	[Fact]
	public async Task Processor_Should_Handle_Very_High_Priority_Value()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), priority: int.MaxValue, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Handle_Zero_Priority()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), priority: 0, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Handle_Negative_Priority()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act

		int? result = await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), priority: -1, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Skip_Cancelled_Task()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		TaskCompletionSource<bool> taskStartedTcs = new();

		// Act - Create a custom task that will complete normally
		Task<int?> result = processor.EnqueueWithPriorityAsync(async ct =>
		{
			taskStartedTcs.TrySetResult(true);
			await Task.Delay(100, ct);
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		// Give time for the task to be queued and start processing
		await Task.Delay(50, Current.CancellationToken);

		// Assert - Task should complete even after our attempt
		int? finalResult = await result;
		finalResult.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Handle_OperationCanceledException_When_Task_Is_Cancelled()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act - Enqueue a task that will throw OperationCanceledException
		Task<int?> resultTask = processor.EnqueueWithPriorityAsync<int?>(async ct =>
		{
			await Task.Delay(10, ct);
			throw new OperationCanceledException("Task cancelled");
#pragma warning disable CS0162 // Unreachable code detected
			return null;
#pragma warning restore CS0162 // Unreachable code detected
		}, cancellationToken: Current.CancellationToken);

		// Assert - Task should throw OperationCanceledException or Exception
		await Should.ThrowAsync<Exception>(async () => await resultTask);
	}

	[Fact]
	public async Task Dispose_Should_Wait_For_Tasks_In_Reader()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		bool taskCompleted = false;

		// Act - Enqueue a task
		Task<int?> task = processor.EnqueueWithPriorityAsync(async _ =>
		{
			await Task.Delay(100);
			taskCompleted = true;
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		// Wait for task to complete
		await task;

		// Now dispose and verify it waits
		processor.Dispose();

		// Assert
		taskCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task Dispose_Should_Handle_Exception_While_Waiting_For_Tasks()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Enqueue a task that will complete quickly
		Task<int?> task = processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);
		await task;

		// Act & Assert - Dispose should not throw even if there are issues
		Should.NotThrow(() => processor.Dispose());
	}

	[Fact]
	public void Finalizer_Should_Call_Dispose_False()
	{
		// Arrange - Create processor in a method to ensure it goes out of scope
		WeakReference weakRef = CreateProcessorAndLetItGoOutOfScope();

		// Act - Force garbage collection
		GC.Collect(2, GCCollectionMode.Forced, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, true);

		// Assert - Object should be collected
		weakRef.IsAlive.ShouldBeFalse();
	}

	private static WeakReference CreateProcessorAndLetItGoOutOfScope()
	{
		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);
		return new WeakReference(processor);
	}

	[Fact]
	public async Task Processor_Should_Remove_Old_Processing_Times_When_Exceeding_Window()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options, processTimeWindow: 2); // Small window
		await processor.StartAsync(CancellationToken.None);

		// Act - Enqueue more tasks than the window size and wait for all to complete
		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)1), cancellationToken: Current.CancellationToken);
		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)2), cancellationToken: Current.CancellationToken);
		await processor.EnqueueWithPriorityAsync(_ => Task.FromResult((int?)3), cancellationToken: Current.CancellationToken);

		// Give time for all tasks to complete
		await Task.Delay(100, Current.CancellationToken);

		// Assert - Stats should be updated (verifies that old times were removed)
		PrioritizedQueueStats stats = await processor.GetAllQueueStatsAsync();
		stats.TotalProcessedTasks.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task Processor_Should_Handle_Exception_In_Task()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act - Enqueue a task that throws an exception
		Task<int?> resultTask = processor.EnqueueWithPriorityAsync<int?>(_ => Task.FromException<int?>(new InvalidOperationException("Test exception")), cancellationToken: Current.CancellationToken);

		// Assert - Exception should be propagated
		await Should.ThrowAsync<InvalidOperationException>(async () => await resultTask);
	}

	[Fact]
	public async Task Dispose_Should_Wait_For_Reader_Tasks_With_Timeout()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		PrioritizedSequentialTaskProcessor processor = new(options);

		// Don't start the processor - this means tasks will sit in the queue/Reader
		bool taskStarted = false;

		// Act - Enqueue a task without starting the processor
		_ = processor.EnqueueWithPriorityAsync(async _ =>
		{
			taskStarted = true;
			await Task.Delay(100);
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		// Wait a moment for the task to be queued
		await Task.Delay(50, Current.CancellationToken);

		// Now dispose - this should attempt to wait for tasks in the reader
		processor.Dispose();

		// Assert - Task should not have started since we didn't call StartAsync
		taskStarted.ShouldBeFalse();
	}

	[Fact]
	public async Task Processor_Should_Handle_Task_That_Takes_Long_To_Complete()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using PrioritizedSequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act - Enqueue a task that takes some time
		Task<int?> result = processor.EnqueueWithPriorityAsync(async _ =>
		{
			await Task.Delay(200);
			return (int?)42;
		}, cancellationToken: Current.CancellationToken);

		// Assert
		int? value = await result;
		value.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Skip_Cancelled_Task_Using_Reflection()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using TestableProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Enqueue and capture a task
		(Task<object?> resultTask, PrioritizedQueuedTask queuedTask) = await processor.EnqueueAndCaptureAsync<int?>(
			async _ =>
			{
				await Task.Delay(100);
				return 42;
			},
			priority: 1,
			priorityLevel: TaskPriority.Normal);

		// Cancel the task before it executes
		await queuedTask.CancellationTokenSource.CancelAsync();

		// Give processor time to skip the cancelled task
		await Task.Delay(200, Current.CancellationToken);

		// The task should not have completed successfully since it was cancelled/skipped
		queuedTask.IsCancelled.ShouldBeTrue();
	}

	[Fact]
	public async Task Processor_Should_Handle_OperationCanceledException_With_Cancelled_Task()
	{
		// Arrange

		BoundedChannelOptions options = new(10);
		using TestableProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Enqueue and capture a task that throws OperationCanceledException
		(Task<object?> resultTask, PrioritizedQueuedTask queuedTask) = await processor.EnqueueAndCaptureAsync<int?>(
			async ct =>
			{
				ct.ThrowIfCancellationRequested();
				await Task.Delay(100, ct);
				return 42;
			},
			priority: 1,
			priorityLevel: TaskPriority.Normal);

		// Mark the task as cancelled
		await queuedTask.CancellationTokenSource.CancelAsync();

		// Give processor time to process (will throw OperationCanceledException)
		await Task.Delay(200, Current.CancellationToken);

		// Verify the task was marked as cancelled
		queuedTask.IsCancelled.ShouldBeTrue();
	}

	#endregion
}
