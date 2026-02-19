using System.Reflection;
using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.ApiQueue;
using static Xunit.TestContext;

namespace Web.Api.Tests.TaskQueuing.ApiQueue;

public class SequentialTaskProcessorTests
{
	// Test helper class to expose internal functionality for cancellation testing
	private class TestableProcessor(BoundedChannelOptions options) : SequentialTaskProcessor(options)
	{
		// Helper method to enqueue a task and get access to the QueuedTask for cancellation testing

		public async Task<(Task<object?> resultTask, QueuedTask queuedTask)> EnqueueAndCaptureAsync<T>(Func<CancellationToken, Task<T?>> taskFunction)
		{
			QueuedTask queuedTask = new(async ct => await taskFunction(ct).ConfigureAwait(false));

			// Use reflection to access the private writer field
			FieldInfo? writerField = typeof(SequentialTaskProcessor).GetField("writer", BindingFlags.NonPublic | BindingFlags.Instance);

			if (writerField?.GetValue(this) is ChannelWriter<QueuedTask> writer)
			{
				await writer.WriteAsync(queuedTask).ConfigureAwait(false);
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
		using SequentialTaskProcessor processor = new(options);

		// Assert
		processor.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_BoundedChannel_Should_Initialize_With_Custom_ProcessTimeWindow()
	{
		// Arrange & Act
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options, processTimeWindow: 500);

		// Assert
		processor.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_UnboundedChannel_Should_Initialize()
	{
		// Arrange & Act
		UnboundedChannelOptions options = new();
		using SequentialTaskProcessor processor = new(options);

		// Assert
		processor.ShouldNotBeNull();
	}

	[Fact]
	public async Task Constructor_BoundedChannel_Should_Initialize_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);

		// Act
		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
		stats.QueuedTasks.ShouldBe(0);
		stats.ProcessedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task Constructor_UnboundedChannel_Should_Initialize_Stats()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		using SequentialTaskProcessor processor = new(options);

		// Act
		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
		stats.QueuedTasks.ShouldBe(0);
	}

	#endregion

	#region EnqueueAsync Tests

	[Fact]
	public async Task EnqueueAsync_Should_Return_Task_Result()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		int expectedValue = 42;

		// Act
		int? result = await processor.EnqueueAsync(_ => Task.FromResult((int?)expectedValue), cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedValue);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Null_Result()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		object? result = await processor.EnqueueAsync<object?>(_ => Task.FromResult((object?)null), cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_String_Result()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		string expectedValue = "test result";

		// Act
		string? result = await processor.EnqueueAsync(_ => Task.FromResult((string?)expectedValue), cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedValue);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_Stats_TotalQueuedTasks()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> enqueueTask = processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

		// Give it a moment to update stats
		await Task.Delay(50, Current.CancellationToken);
		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.QueuedTasks.ShouldBeGreaterThan(0);

		await enqueueTask;
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Multiple_Enqueues()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task1 = processor.EnqueueAsync(_ => Task.FromResult((int?)1), cancellationToken: Current.CancellationToken);
		Task<int?> task2 = processor.EnqueueAsync(_ => Task.FromResult((int?)2), cancellationToken: Current.CancellationToken);
		Task<int?> task3 = processor.EnqueueAsync(_ => Task.FromResult((int?)3), cancellationToken: Current.CancellationToken);

		int?[] results = await Task.WhenAll(task1, task2, task3);

		// Assert
		results.Length.ShouldBe(3);
		results[0].ShouldBe(1);
		results[1].ShouldBe(2);
		results[2].ShouldBe(3);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_CancellationToken()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		using CancellationTokenSource cts = new();

		// Act
		Task<int?> task = processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: cts.Token);

		int? result = await task;

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Task_With_Delay()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task = processor.EnqueueAsync(async ct => { await Task.Delay(10, ct); return (int?)42; }, cancellationToken: Current.CancellationToken);

		int? result = await task;

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task EnqueueAsync_With_UnboundedChannel_Should_Work()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		int? result = await processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);

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
		using SequentialTaskProcessor processor = new(options);

		// Act
		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Return_Same_Instance()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);

		// Act
		QueueStats stats1 = await processor.GetAllQueueStatsAsync();
		QueueStats stats2 = await processor.GetAllQueueStatsAsync();

		// Assert
		stats1.ShouldBe(stats2);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Reflect_Updated_Stats_After_Enqueue()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		QueueStats statsBefore = await processor.GetAllQueueStatsAsync();
		int initialQueued = statsBefore.QueuedTasks;

		// Act
		Task<int?> task = processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);
		QueueStats statsAfter = await processor.GetAllQueueStatsAsync();

		// Assert
		statsAfter.QueuedTasks.ShouldBeGreaterThan(initialQueued);

		await task;
	}

	[Fact]
	public async Task Stats_Property_Should_Return_Current_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);

		// Act
		QueueStats stats = processor.Stats;

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("All");
	}

	[Fact]
	public async Task Stats_Property_Should_Return_Updated_Stats_After_Processing()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task = processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);
		await task;
		await Task.Delay(50, Current.CancellationToken); // Give stats time to update

		QueueStats stats = processor.Stats;

		// Assert
		stats.ProcessedTasks.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetAllQueueStatsAsync_Should_Include_LastProcessedAt_After_Processing()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task = processor.EnqueueAsync(_ => Task.FromResult((int?)42), cancellationToken: Current.CancellationToken);
		await task;
		await Task.Delay(50, Current.CancellationToken);

		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert
		stats.LastProcessedAt.ShouldNotBeNull();
	}
	#endregion

	#region Dispose Tests

	[Fact]
	public void Dispose_Should_Not_Throw()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);

		// Act & Assert
		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public void Dispose_Should_Be_Idempotent()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);

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
		SequentialTaskProcessor processor = new(options);

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
		SequentialTaskProcessor processor = new(options);

		// Act & Assert
		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public async Task Dispose_After_Enqueue_Should_Wait_For_Task_Completion()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		bool taskCompleted = false;

		// Act
		Task<int?> task = processor.EnqueueAsync(async ct => { await Task.Delay(100, ct); taskCompleted = true; return (int?)42; }, cancellationToken: Current.CancellationToken);

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
		SequentialTaskProcessor processor = new(options, processTimeWindow: 500);

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
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		int taskCount = 50;
		List<Task<int?>> tasks = new();

		// Act
		for (int i = 0; i < taskCount; i++)
		{
			int index = i;
			tasks.Add(processor.EnqueueAsync(_ => Task.FromResult((int?)index), cancellationToken: Current.CancellationToken));
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert
		results.Length.ShouldBe(taskCount);
	}

	[Fact]
	public async Task Processor_Should_Return_Complex_Objects()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		var expectedObject = new { Id = 1, Name = "Test" };

		// Act
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
		var result = await processor.EnqueueAsync(_ => Task.FromResult(expectedObject), cancellationToken: Current.CancellationToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
	}

	[Fact]
	public async Task Multiple_Processors_Should_Work_Independently()
	{
		// Arrange
		BoundedChannelOptions options1 = new(10);
		BoundedChannelOptions options2 = new(10);
		using SequentialTaskProcessor processor1 = new(options1);
		using SequentialTaskProcessor processor2 = new(options2);
		await processor1.StartAsync(CancellationToken.None);
		await processor2.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task1 = processor1.EnqueueAsync(_ => Task.FromResult((int?)1), cancellationToken: Current.CancellationToken);
		Task<int?> task2 = processor2.EnqueueAsync(_ => Task.FromResult((int?)2), cancellationToken: Current.CancellationToken);

		int?[] results = await Task.WhenAll(task1, task2);

		// Assert
		results[0].ShouldBe(1);
		results[1].ShouldBe(2);
	}

	[Fact]
	public async Task Dispose_Should_Wait_For_Tasks_In_Reader()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		bool taskCompleted = false;

		// Act - Enqueue a task
		Task<int?> task = processor.EnqueueAsync(async _ => { await Task.Delay(100, _); taskCompleted = true; return (int?)42; }, cancellationToken: Current.CancellationToken);

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
		SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> _ = processor.EnqueueAsync<int?>(_ => throw new InvalidOperationException("Test exception"), cancellationToken: Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Dispose should handle the exception gracefully
		Should.NotThrow(processor.Dispose);
	}

	[Fact]
	public void Finalizer_Should_Call_Dispose_False()
	{
		// Arrange & Act
		WeakReference weakRef = CreateProcessorAndLetItGoOutOfScope();

		// Force garbage collection
#pragma warning disable S1215 // Refactor the code to remove this use of 'GC.Collect'.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
#pragma warning restore S1215 // Refactor the code to remove this use of 'GC.Collect'.


		// Assert - Processor should be garbage collected
		weakRef.IsAlive.ShouldBeFalse();
	}

	private static WeakReference CreateProcessorAndLetItGoOutOfScope()
	{
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);
		return new WeakReference(processor);
	}

	[Fact]
	public async Task Processor_Should_Remove_Old_Processing_Times_When_Exceeding_Window()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options, processTimeWindow: 5); // Small window
		await processor.StartAsync(CancellationToken.None);

		// Act - Enqueue more tasks than the window size
		for (int i = 0; i < 10; i++)
		{
			await processor.EnqueueAsync(_ => Task.FromResult((int?)i), cancellationToken: Current.CancellationToken);
		}

		await Task.Delay(100, Current.CancellationToken);

		QueueStats stats = await processor.GetAllQueueStatsAsync();

		// Assert - Processing should have completed all tasks
		stats.ProcessedTasks.ShouldBe(10);
	}

	[Fact]
	public async Task Processor_Should_Handle_Exception_In_Task()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () => await processor.EnqueueAsync<int?>(_ => throw new InvalidOperationException("Test exception"), cancellationToken: Current.CancellationToken));
	}

	[Fact]
	public async Task Dispose_Should_Wait_For_Reader_Tasks_With_Timeout()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		SequentialTaskProcessor processor = new(options);

		// Don't start the processor - this means tasks will sit in the queue/Reader
		bool taskStarted = false;

		// Act - Enqueue a task without starting the processor
		_ = processor.EnqueueAsync(async _ => { taskStarted = true; await Task.Delay(100, _); return (int?)42; }, cancellationToken: Current.CancellationToken);

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
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);

		// Act
		Task<int?> task = processor.EnqueueAsync(async ct => { await Task.Delay(200, ct); return (int?)42; }, cancellationToken: Current.CancellationToken);

		int? result = await task;

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task Processor_Should_Update_FailedTasks_Count_On_Exception()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		using SequentialTaskProcessor processor = new(options);
		await processor.StartAsync(CancellationToken.None);
		QueueStats statsBefore = await processor.GetAllQueueStatsAsync();
		int initialFailedCount = statsBefore.FailedTasks;

		// Act
		try
		{
			await processor.EnqueueAsync<int?>(_ => throw new InvalidOperationException("Test"), cancellationToken: Current.CancellationToken);
		}
		catch
		{
			// Expected exception
		}

		await Task.Delay(50, Current.CancellationToken);
		QueueStats statsAfter = await processor.GetAllQueueStatsAsync();

		// Assert
		statsAfter.FailedTasks.ShouldBeGreaterThan(initialFailedCount);
	}

	#endregion
}
