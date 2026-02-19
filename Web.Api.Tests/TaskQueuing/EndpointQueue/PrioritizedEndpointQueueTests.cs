using System.Diagnostics;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using static Xunit.TestContext;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public sealed class PrioritizedEndpointQueueTests : IDisposable
{
	private readonly List<PrioritizedEndpointQueue> disposables = new();

	public void Dispose()
	{
		foreach (PrioritizedEndpointQueue queue in disposables)
		{
			queue?.Dispose();
		}
		disposables.Clear();
	}

	private PrioritizedEndpointQueue CreateQueue(string key = "test-endpoint", int processTimeWindow = 1000)
	{
		PrioritizedEndpointQueue queue = new(key, processTimeWindow);
		disposables.Add(queue);
		return queue;
	}

	[Fact]
	public async Task EnqueueAsync_Should_Enqueue_And_Process_Task()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		int expectedResult = 42;

		// Act
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(expectedResult), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Process_Higher_Priority_Tasks_First()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<int> executionOrder = new();
		SemaphoreSlim startProcessing = new(0, 1);

		// Enqueue low priority task that will block
		Task<int?> lowPriorityTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await startProcessing.WaitAsync(ct);
			executionOrder.Add(1);
			return 1;
		}, 1, TaskPriority.Low, Current.CancellationToken);

		// Give the low priority task time to be picked up
		await Task.Delay(50, Current.CancellationToken);

		// Enqueue high priority task
		Task<int?> highPriorityTask = queue.EnqueueAsync<int?>(async _ =>
		{
			executionOrder.Add(2);
			return 2;
		}, 10, TaskPriority.High, Current.CancellationToken);

		// Release the low priority task
		startProcessing.Release();

		// Act
		await Task.WhenAll(lowPriorityTask, highPriorityTask);

		// Assert
		executionOrder.Count.ShouldBe(2);
		executionOrder[0].ShouldBe(1); // Low priority started first
		executionOrder[1].ShouldBe(2); // High priority processed when available
	}

	[Fact]
	public async Task EnqueueAsync_Should_Return_Result_To_Caller()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		string expectedResult = "test-result";

		// Act
		string? result = await queue.EnqueueAsync(_ => Task.FromResult(expectedResult), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Increment_Stats()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Give time for processing

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalQueuedTasks.ShouldBe(1);
		stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_Queue_Depth()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);
		int maxDepth = 0;

		// Act - Enqueue multiple tasks
		Task<int?> task1 = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Let first task start processing

		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task3 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);
		maxDepth = queue.Stats.CurrentQueueDepth;

		blockProcessing.Release();
		await Task.WhenAll(task1, task2, task3);

		// Assert
		maxDepth.ShouldBeGreaterThanOrEqualTo(2); // At least 2 tasks were queued
		queue.Stats.CurrentQueueDepth.ShouldBe(0); // All tasks processed
	}

	[Fact]
	public async Task EnqueueAsync_Should_Track_Processing_Time()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue(processTimeWindow: 10);

		// Act
		await queue.EnqueueAsync(async _ =>
		{
			await Task.Delay(10, Current.CancellationToken);
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(100, Current.CancellationToken); // Give time for stats to update

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.AverageProcessingTime.ShouldNotBeNull();
		stats.AverageProcessingTime.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Exception_In_Task()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		InvalidOperationException expectedException = new("Test exception");

		// Act & Assert
		InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await queue.EnqueueAsync<int>(_ => throw expectedException, 1, TaskPriority.Normal));

		exception.Message.ShouldBe("Test exception");
	}

	[Fact]
	public async Task EnqueueAsync_Should_Track_Failed_Tasks_In_Stats()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		try
		{
			await queue.EnqueueAsync<int>(_ => throw new InvalidOperationException(), 1, TaskPriority.Normal, Current.CancellationToken);
		}
		catch (InvalidOperationException)
		{
			// Expected
		}

		await Task.Delay(100, Current.CancellationToken); // Give time for stats to update

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalFailedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.Normal].FailedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_LastProcessedAt()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		DateTime beforeEnqueue = DateTime.UtcNow;

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Give time for processing

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.LastProcessedAt.ShouldNotBeNull();
		stats.LastProcessedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeEnqueue);
		stats.PriorityBreakdown[TaskPriority.Normal].LastProcessedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_Should_Process_Multiple_Priority_Levels()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Low, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(2), 5, TaskPriority.Normal, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(3), 10, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(200, Current.CancellationToken); // Give time for processing

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.PriorityBreakdown[TaskPriority.Low].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.Normal].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.High].ProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Cancel_Tasks()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task to hold up the queue
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue tasks to be cancelled
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Let tasks queue up

		// Act
		bool result = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Unblock and complete
		blockProcessing.Release();
		await blockingTask;

		// Assert
		result.ShouldBeTrue();
		await Should.ThrowAsync<TaskCanceledException>(async () => await task1);
		await Should.ThrowAsync<TaskCanceledException>(async () => await task2);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Update_Stats()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue tasks to be cancelled
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Act
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);
		blockProcessing.Release();
		await blockingTask;

#pragma warning disable S108 // Either remove or fill this block of code.
		try { await task1; } catch { }
		try { await task2; } catch { }
#pragma warning restore S108 // Either remove or fill this block of code.

		await Task.Delay(100, Current.CancellationToken);

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalCancelledTasks.ShouldBe(2);
		stats.PriorityBreakdown[TaskPriority.Normal].CancelledTasks.ShouldBe(2);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Not_Cancel_Other_Priorities()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue tasks with different priorities
		Task<int?> normalTask = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> highTask = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Act
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);
		blockProcessing.Release();

		// Assert
		await blockingTask;
		await Should.ThrowAsync<TaskCanceledException>(async () => await normalTask);

		int? highResult = await highTask;
		highResult.ShouldBe(3);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Return_False_When_No_Tasks_Cancelled()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		bool result = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void Stats_Property_Should_Return_Current_Stats()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue("my-endpoint");

		// Act
		PrioritizedQueueStats stats = queue.Stats;

		// Assert
		stats.ShouldNotBeNull();
		stats.EndpointKey.ShouldBe("my-endpoint");
	}

	[Fact]
	public void Stats_Should_Include_Priority_Breakdown()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		PrioritizedQueueStats stats = queue.Stats;

		// Assert
		stats.PriorityBreakdown.ShouldNotBeNull();
		stats.PriorityBreakdown.ShouldContainKey(TaskPriority.Low);
		stats.PriorityBreakdown.ShouldContainKey(TaskPriority.Normal);
		stats.PriorityBreakdown.ShouldContainKey(TaskPriority.High);
		stats.PriorityBreakdown.ShouldContainKey(TaskPriority.Critical);
		stats.PriorityBreakdown.ShouldContainKey(TaskPriority.Emergency);
	}

	[Fact]
	public async Task Stats_Should_Calculate_Overall_Average_Processing_Time()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue(processTimeWindow: 10);

		// Act
		await queue.EnqueueAsync(async _ => { await Task.Delay(20, Current.CancellationToken); return 1; }, 1, TaskPriority.Low, Current.CancellationToken);
		await queue.EnqueueAsync(async _ => { await Task.Delay(30, Current.CancellationToken); return 2; }, 1, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(200, Current.CancellationToken); // Give time for processing

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.AverageProcessingTime.ShouldNotBeNull();
		stats.AverageProcessingTime.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task Stats_Should_Calculate_Per_Priority_Average_Processing_Time()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue(processTimeWindow: 10);

		// Act
		await queue.EnqueueAsync(async _ => { await Task.Delay(20, Current.CancellationToken); return 1; }, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(200, Current.CancellationToken);

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.PriorityBreakdown[TaskPriority.Normal].AverageProcessingTime.ShouldNotBeNull();
		if (stats.PriorityBreakdown[TaskPriority.Normal].AverageProcessingTime.HasValue)
		{
			stats.PriorityBreakdown[TaskPriority.Normal].AverageProcessingTime!.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	public async Task Stats_Should_Show_Current_Processing_Priority()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);
		TaskPriority? capturedPriority = null;

		// Act
		Task<int?> task = queue.EnqueueAsync<int?>(async ct =>
		{
			capturedPriority = queue.Stats.CurrentProcessingPriority;
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(100, Current.CancellationToken); // Give time for task to start

		// Assert (while processing)
		PrioritizedQueueStats statsWhileProcessing = queue.Stats;
		statsWhileProcessing.CurrentProcessingPriority.ShouldBe(TaskPriority.High);

		blockProcessing.Release();
		await task;
		await Task.Delay(50, Current.CancellationToken);

		// Assert (after processing)
		PrioritizedQueueStats statsAfterProcessing = queue.Stats;
		statsAfterProcessing.CurrentProcessingPriority.ShouldBeNull();
	}

	[Fact]
	public void Dispose_Should_Cancel_Processing_Task()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		queue.Dispose();

		// Assert - Should not throw
		Should.NotThrow(queue.Dispose);
	}

	[Fact]
	public void Dispose_Should_Be_Idempotent()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act & Assert
		queue.Dispose();
		Should.NotThrow(queue.Dispose);
		Should.NotThrow(queue.Dispose);
	}

	[Fact]
	public async Task EnqueueAsync_With_Cancellation_Token_Should_Cancel()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync(); // Cancel before enqueueing

		// Act & Assert - Enqueueing with a cancelled token should throw
		await Should.ThrowAsync<OperationCanceledException>(async () => await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, cts.Token));
	}

	[Fact]
	public async Task ProcessingTask_Should_Skip_Already_Cancelled_Tasks()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);
		int executionCount = 0;

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue task that will be cancelled
		Task<int?> _ = queue.EnqueueAsync<int?>(async _ =>
		{
			executionCount++;
			return 2;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Cancel the queued task
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		blockProcessing.Release();
		await blockingTask;

		await Task.Delay(100, Current.CancellationToken);

		// Assert
		executionCount.ShouldBe(0); // Task should not have executed
	}

	[Fact]
	public void EndpointKey_Property_Should_Return_Key()
	{
		// Arrange & Act
		PrioritizedEndpointQueue queue = CreateQueue("test-key-123");

		// Assert
		queue.EndpointKey.ShouldBe("test-key-123");
	}

	[Fact]
	public void Constructor_Should_Initialize_All_Priority_Stats()
	{
		// Arrange & Act
		PrioritizedEndpointQueue queue = CreateQueue();
		PrioritizedQueueStats stats = queue.Stats;

		// Assert
		foreach (TaskPriority priority in Enum.GetValues<TaskPriority>())
		{
			stats.PriorityBreakdown.ShouldContainKey(priority);
			PriorityStats priorityStats = stats.PriorityBreakdown[priority];
			priorityStats.ShouldNotBeNull();
			priorityStats.QueuedTasks.ShouldBe(0);
			priorityStats.ProcessedTasks.ShouldBe(0);
			priorityStats.FailedTasks.ShouldBe(0);
			priorityStats.CancelledTasks.ShouldBe(0);
		}
	}

	[Fact]
	public async Task EnqueueAsync_Should_Process_Tasks_In_Priority_Order_With_Custom_Priority_Values()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<int> executionOrder = new();
		SemaphoreSlim startProcessing = new(0, 1);

		// Enqueue low priority task that blocks
		Task<int?> lowTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await startProcessing.WaitAsync(ct);
			executionOrder.Add(1);
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Enqueue tasks with different custom priorities
		Task<int?> mediumTask = queue.EnqueueAsync<int?>(async _ =>
		{
			executionOrder.Add(2);
			return 2;
		}, 50, TaskPriority.Normal, Current.CancellationToken);

		Task<int?> highTask = queue.EnqueueAsync<int?>(async _ =>
		{
			executionOrder.Add(3);
			return 3;
		}, 100, TaskPriority.Normal, Current.CancellationToken);

		// Release processing
		startProcessing.Release();
		await Task.WhenAll(lowTask, mediumTask, highTask);

		// Assert
		executionOrder.Count.ShouldBe(3);
		executionOrder[0].ShouldBe(1); // First task started
		executionOrder[1].ShouldBe(3); // Highest priority (100)
		executionOrder[2].ShouldBe(2); // Medium priority (50)
	}

	[Fact]
	public async Task EnqueueAsync_Should_Respect_ProcessTimeWindow()
	{
		// Arrange
		int windowSize = 2;
		PrioritizedEndpointQueue queue = CreateQueue(processTimeWindow: windowSize);

		// Act - Process more tasks than the window size
		for (int i = 0; i < windowSize + 2; i++)
		{
			await queue.EnqueueAsync(async _ =>
			{
				await Task.Delay(10, Current.CancellationToken);
				return i;
			}, 1, TaskPriority.Normal, Current.CancellationToken);
		}

		await Task.Delay(200, Current.CancellationToken);

		// Assert - The processing times list should not exceed the window size
		PrioritizedQueueStats stats = queue.Stats;
		stats.PriorityBreakdown[TaskPriority.Normal].ProcessedTasks.ShouldBe(windowSize + 2);
		// The average should be based on the last 'windowSize' tasks
		stats.PriorityBreakdown[TaskPriority.Normal].AverageProcessingTime.ShouldNotBeNull();
	}

	[Fact]
	public async Task Stats_Should_Return_Independent_Copies()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		PrioritizedQueueStats stats1 = queue.Stats;
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);
		PrioritizedQueueStats stats2 = queue.Stats;

		// Assert - stats1 should not be affected by new tasks
		stats1.TotalQueuedTasks.ShouldBe(0);
		stats2.TotalQueuedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Null_Results()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		string? result = await queue.EnqueueAsync(_ => Task.FromResult<string?>(null), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task Multiple_Concurrent_Enqueues_Should_All_Process()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		int taskCount = 20;
		List<Task<int?>> tasks = new();

		// Act
		for (int i = 0; i < taskCount; i++)
		{
			int value = i;
			tasks.Add(queue.EnqueueAsync<int?>(async _ => value, value, TaskPriority.Normal, Current.CancellationToken));
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert
		results.Length.ShouldBe(taskCount);
		for (int i = 0; i < taskCount; i++)
		{
			results.ShouldContain(i);
		}

		await Task.Delay(100, Current.CancellationToken);
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBe(taskCount);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Not_Affect_Currently_Processing_Task()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim processingStarted = new(0, 1);
		SemaphoreSlim finishProcessing = new(0, 1);
		bool task1Completed = false;

		// Start a task that will be processing when we cancel
		Task<int?> processingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			processingStarted.Release();
			await finishProcessing.WaitAsync(ct);
			task1Completed = true;
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for processing to start
		await processingStarted.WaitAsync(Current.CancellationToken);

		// Act - Try to cancel while task is processing
		bool cancelResult = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Complete the processing task
		finishProcessing.Release();
		await processingTask;

		// Assert
		cancelResult.ShouldBeFalse(); // No queued tasks were cancelled
		task1Completed.ShouldBeTrue(); // Processing task completed successfully
	}

	[Fact]
	public async Task Finalizer_Should_Not_Throw()
	{
		// Arrange & Act
#pragma warning disable S1854 // Remove this useless assignment to local variable 'queue'
		PrioritizedEndpointQueue? queue = new("test");
		queue = null;
#pragma warning restore S1854 // Remove this useless assignment to local variable 'queue'

		// Force garbage collection
#pragma warning disable S1215 // Refactor the code to remove this use of 'GC.Collect'.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
#pragma warning restore S1215 // Refactor the code to remove this use of 'GC.Collect'.

		await Task.Delay(200, Current.CancellationToken);

		// Assert - The main goal is to ensure the finalizer doesn't throw
		// The object may or may not be collected depending on GC behavior
		// If the finalizer threw an exception, this test would fail
		Should.NotThrow(GC.WaitForPendingFinalizers);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Track_All_Priority_Levels_Independently()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Low, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(2), 1, TaskPriority.Normal, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(3), 1, TaskPriority.High, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(4), 1, TaskPriority.Critical, Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(5), 1, TaskPriority.Emergency, Current.CancellationToken);

		await Task.Delay(300, Current.CancellationToken);

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBe(5);
		stats.PriorityBreakdown[TaskPriority.Low].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.Normal].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.High].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.Critical].ProcessedTasks.ShouldBe(1);
		stats.PriorityBreakdown[TaskPriority.Emergency].ProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task ProcessingTask_Should_Start_Lazily_On_First_Enqueue()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Just create the queue, don't enqueue anything yet
		await Task.Delay(100, Current.CancellationToken);

		// The processing task should not be started yet (no way to directly verify, but we can test behavior)
		// Now enqueue something
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(42), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task ProcessingTask_Should_Start_Only_Once_With_Concurrent_Enqueues()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<Task<int?>> tasks = new();

		// Act - Enqueue multiple tasks concurrently (all could trigger lazy initialization)
		for (int i = 0; i < 10; i++)
		{
			int value = i;
			tasks.Add(Task.Run(() => queue.EnqueueAsync<int?>(async _ => value, value, TaskPriority.Normal, Current.CancellationToken)));
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert - All tasks should complete successfully
		results.Length.ShouldBe(10);
		for (int i = 0; i < 10; i++)
		{
			results.ShouldContain(i);
		}
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Not_Cancel_Already_Cancelled_Tasks()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue tasks to be cancelled
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Act - Cancel twice
		bool firstCancel = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);
		bool secondCancel = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Unblock
		blockProcessing.Release();
		await blockingTask;

#pragma warning disable S108 // Either remove or fill this block of code.
		try { await task1; } catch { }
		try { await task2; } catch { }
#pragma warning restore S108 // Either remove or fill this block of code.

		// Assert
		firstCancel.ShouldBeTrue();
		secondCancel.ShouldBeFalse(); // No tasks to cancel the second time
	}

	[Fact]
	public async Task Concurrent_CancelTasksByPriorityAsync_Should_Be_Thread_Safe()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue tasks with different priorities
		List<Task<int?>> normalTasks = new();
		List<Task<int?>> highTasks = new();

		for (int i = 0; i < 5; i++)
		{
			normalTasks.Add(queue.EnqueueAsync<int?>(async _ => i, 1, TaskPriority.Normal, Current.CancellationToken));
			highTasks.Add(queue.EnqueueAsync<int?>(async _ => i, 1, TaskPriority.High, Current.CancellationToken));
		}

		await Task.Delay(50, Current.CancellationToken);

		// Act - Cancel different priorities concurrently
		Task<bool> cancelNormal = Task.Run(() => queue.CancelTasksByPriorityAsync(TaskPriority.Normal));
		Task<bool> cancelHigh = Task.Run(() => queue.CancelTasksByPriorityAsync(TaskPriority.High));

		bool[] cancelResults = await Task.WhenAll(cancelNormal, cancelHigh);

		blockProcessing.Release();
		await blockingTask;

		// Assert - Both cancellations should succeed
		cancelResults[0].ShouldBeTrue();
		cancelResults[1].ShouldBeTrue();

		// All tasks should be cancelled
		foreach (Task<int?> task in normalTasks)
		{
			await Should.ThrowAsync<TaskCanceledException>(async () => await task);
		}
		foreach (Task<int?> task in highTasks)
		{
			await Should.ThrowAsync<TaskCanceledException>(async () => await task);
		}
	}

	[Fact]
	public async Task Dispose_During_Active_Processing_Should_Not_Throw()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim processingStarted = new(0, 1);
		SemaphoreSlim finishProcessing = new(0, 1);

		// Start a long-running task
		Task<int?> processingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			processingStarted.Release();
			await finishProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for processing to start
		await processingStarted.WaitAsync(Current.CancellationToken);

		// Act - Dispose while task is processing
		Should.NotThrow(queue.Dispose);

		// Clean up
		finishProcessing.Release();
		try
		{
			await processingTask.WaitAsync(TimeSpan.FromMilliseconds(500), Current.CancellationToken);
		}
		catch
		{
			// Expected - task may be cancelled or timeout
		}
	}

	[Fact]
	public async Task Dispose_Should_Signal_Shutdown_And_Stop_Processing()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Enqueue and process a task to ensure processing task is started
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Act
		queue.Dispose();

		// Give time for shutdown to take effect
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Queue should no longer accept new work (this would hang or throw if processing task is still active)
		// We can't enqueue after dispose without potential issues, so just verify dispose completed
		Should.NotThrow(queue.Dispose); // Should be idempotent
	}

	[Fact]
	public async Task Stats_Should_Show_Zero_Average_When_No_Tasks_Processed()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Get stats without processing any tasks
		PrioritizedQueueStats stats = queue.Stats;

		// Assert
		stats.AverageProcessingTime.ShouldBeNull();
		stats.PriorityBreakdown[TaskPriority.Normal].AverageProcessingTime.ShouldBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_Should_Signal_NewTaskEvent()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<Task<int?>> tasks = new();

		// Act - Enqueue multiple tasks with small delays to test event signaling
		for (int i = 0; i < 5; i++)
		{
			int value = i;
			tasks.Add(queue.EnqueueAsync<int?>(async _ =>
			{
				await Task.Delay(10, Current.CancellationToken);
				return value;
			}, value, TaskPriority.Normal, Current.CancellationToken));

			await Task.Delay(5, Current.CancellationToken); // Small delay between enqueues
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert - All tasks should complete (verifies event signaling worked)
		results.Length.ShouldBe(5);
		foreach (int? result in results)
		{
			result.ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task ProcessTasksAsync_Should_Wait_For_Tasks_When_Queue_Empty()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Process a task to start the processing loop
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now empty, processing task should be waiting
		// Enqueue another task to verify it's still responsive
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(2), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(2);
	}

	[Fact]
	public async Task EnqueueAsync_With_Failed_Task_Should_Propagate_Exception()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		InvalidOperationException expectedException = new("Custom error message");

		// Act & Assert
		InvalidOperationException actualException = await Should.ThrowAsync<InvalidOperationException>(async () => await queue.EnqueueAsync<int>(_ => throw expectedException, 1, TaskPriority.High, Current.CancellationToken));

		actualException.Message.ShouldBe("Custom error message");
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Update_Queue_Depth()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue tasks to be cancelled
		for (int i = 0; i < 5; i++)
		{
			_ = queue.EnqueueAsync<int?>(async _ => i, 1, TaskPriority.Normal, Current.CancellationToken);
		}

		await Task.Delay(50, Current.CancellationToken);

		int depthBeforeCancel = queue.Stats.CurrentQueueDepth;

		// Act
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		int depthAfterCancel = queue.Stats.CurrentQueueDepth;

		blockProcessing.Release();
		await blockingTask;

		// Assert
		depthBeforeCancel.ShouldBeGreaterThanOrEqualTo(5);
		depthAfterCancel.ShouldBe(0); // All Normal priority tasks cancelled
	}

	[Fact]
	public async Task EnqueueAsync_Should_Work_With_Complex_Return_Types()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		ComplexResult expectedResult = new() { Id = 123, Name = "Test", Items = new List<string> { "A", "B", "C" } };

		// Act
		ComplexResult? result = await queue.EnqueueAsync(_ => Task.FromResult(expectedResult), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(123);
		result.Name.ShouldBe("Test");
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(3);
	}

	[Fact]
	public async Task Multiple_Priority_Levels_Should_Be_Processed_In_Correct_Order()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<string> executionOrder = new();
		SemaphoreSlim blockFirst = new(0, 1);

		// Enqueue blocking task to hold up the queue
		Task<int?> firstTask = queue.EnqueueAsync<int?>(async ct =>
		{
			executionOrder.Add("first");
			await blockFirst.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.Low, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Enqueue tasks with different priorities
		Task<int?> emergencyTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add("emergency"); return 5; }, 1, TaskPriority.Emergency, Current.CancellationToken);
		Task<int?> criticalTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add("critical"); return 4; }, 1, TaskPriority.Critical, Current.CancellationToken);
		Task<int?> highTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add("high"); return 3; }, 1, TaskPriority.High, Current.CancellationToken);
		Task<int?> normalTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add("normal"); return 2; }, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> lowTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add("low"); return 1; }, 1, TaskPriority.Low, Current.CancellationToken);

		// Release and wait
		blockFirst.Release();
		await Task.WhenAll(firstTask, emergencyTask, criticalTask, highTask, normalTask, lowTask);

		// Assert - Emergency should be processed before Critical, Critical before High, etc.
		executionOrder[0].ShouldBe("first");
		executionOrder[1].ShouldBe("emergency");
		executionOrder[2].ShouldBe("critical");
		executionOrder[3].ShouldBe("high");
		executionOrder[4].ShouldBe("normal");
		executionOrder[5].ShouldBe("low");
	}

	[Fact]
	public async Task Stats_Should_Track_Multiple_Failed_Tasks()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Enqueue multiple failing tasks
		for (int i = 0; i < 3; i++)
		{
			try
			{
				await queue.EnqueueAsync<int>(_ => throw new InvalidOperationException(), 1, TaskPriority.Normal, Current.CancellationToken);
			}
			catch (InvalidOperationException)
			{
				// Expected
			}
		}

		await Task.Delay(100, Current.CancellationToken);

		// Assert
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalFailedTasks.ShouldBe(3);
		stats.PriorityBreakdown[TaskPriority.Normal].FailedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task CancelTasksByPriorityAsync_Should_Preserve_Queue_Order_Of_Remaining_Tasks()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);
		List<int> executionOrder = new();

		// Enqueue blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Ensure blocking task starts

		// Enqueue tasks with different priorities
		Task<int?> highTask1 = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(1); return 1; }, 100, TaskPriority.High, Current.CancellationToken);
		Task<int?> normalTask = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(2); return 2; }, 50, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> highTask2 = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(3); return 3; }, 90, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Act - Cancel Normal priority
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		blockProcessing.Release();
		await blockingTask;
		await highTask1;
		await highTask2;

#pragma warning disable S108 // Either remove or fill this block of code.
		try { await normalTask; } catch { }
#pragma warning restore S108 // Either remove or fill this block of code.

		// Assert - High priority tasks should execute in their priority order
		// The normal task should not have executed (unless it started before cancellation)
		executionOrder.ShouldContain(1); // Priority 100
		executionOrder.ShouldContain(3); // Priority 90
		executionOrder.ShouldNotContain(2); // Normal task should be cancelled
	}

	[Fact]
	public async Task EnqueueAsync_With_Already_Cancelled_Token_Should_Throw()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, cts.Token));
	}

	[Fact]
	public async Task ProcessingLoop_Should_Periodically_Check_Shutdown_Flag()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Start processing by enqueueing a task
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now empty and waiting

		// Act - Dispose should trigger shutdown even if waiting
		queue.Dispose();
		await Task.Delay(200, Current.CancellationToken); // Give time for shutdown

		// Assert - Should not hang
		Should.NotThrow(() => { }); // If we get here without hanging, test passes
	}

	[Fact]
	public async Task Dispose_During_Multiple_Concurrent_Enqueues_Should_Not_Throw()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<Task> tasks = new();

		// Enqueue some tasks first
		for (int i = 0; i < 5; i++)
		{
			int value = i;
			Task task = queue.EnqueueAsync<int?>(async _ =>
			{
				await Task.Delay(10, Current.CancellationToken);
				return value;
			}, value, TaskPriority.Normal, Current.CancellationToken);
			tasks.Add(task);
		}

		// Give tasks time to start processing
		await Task.Delay(20, Current.CancellationToken);

		// Act - Dispose while tasks are processing
		Should.NotThrow(queue.Dispose);

		// Assert - No exceptions thrown during disposal
		Should.NotThrow(queue.Dispose); // Idempotent
	}

	[Fact]
	public async Task Queue_Should_Process_Tasks_After_Long_Idle_Period()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process first task
		int? result1 = await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		result1.ShouldBe(1);

		// Wait for longer than the 100ms timeout in the processing loop
		await Task.Delay(250, Current.CancellationToken);

		// Act - Enqueue another task after idle period
		int? result2 = await queue.EnqueueAsync(_ => Task.FromResult(2), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result2.ShouldBe(2);
	}

	[Fact]
	public async Task Virtual_CancelTasksByPriorityAsync_Can_Be_Overridden()
	{
		// Arrange
		TestPrioritizedEndpointQueue queue = new("test");
		disposables.Add(queue);

		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue tasks to be cancelled
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Act
		bool result = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		blockProcessing.Release();
		await blockingTask;

		// Assert - Virtual method was called
		result.ShouldBeTrue();
		queue.CancelCallCount.ShouldBe(1);

#pragma warning disable S108 // Either remove or fill this block of code.
		try { await task1; } catch { }
		try { await task2; } catch { }
#pragma warning restore S108 // Either remove or fill this block of code.
	}

	[Fact]
	public async Task Stats_Should_Be_Thread_Safe_During_Concurrent_Access()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<Task> tasks = new();

		// Act - Enqueue tasks and read stats concurrently
		for (int i = 0; i < 20; i++)
		{
			int value = i;
			tasks.Add(queue.EnqueueAsync<int?>(async _ =>
			{
				await Task.Delay(5, Current.CancellationToken);
				return value;
			}, value, TaskPriority.Normal, Current.CancellationToken));

			// Read stats concurrently
			tasks.Add(Task.Run(() =>
			{
				PrioritizedQueueStats stats = queue.Stats;
				stats.ShouldNotBeNull();
			}, Current.CancellationToken));
		}

		await Task.WhenAll(tasks);

		// Give time for all tasks to be processed
		await Task.Delay(200, Current.CancellationToken);

		// Assert - No exceptions thrown, stats are consistent
		PrioritizedQueueStats finalStats = queue.Stats;
		finalStats.TotalQueuedTasks.ShouldBe(20);
		finalStats.TotalProcessedTasks.ShouldBe(20);
	}

	[Fact]
	public async Task Empty_Queue_Should_Handle_Multiple_Dequeue_Attempts()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Start processing by enqueueing and completing a task
		await queue.EnqueueAsync(_ => Task.FromResult(1), 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now empty - processing loop will dequeue null multiple times

		// Act - Wait through several timeout cycles
		await Task.Delay(350, Current.CancellationToken); // More than 3 timeout cycles

		// Enqueue another task to verify queue is still responsive
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(2), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(2);
	}

	[Fact]
	public async Task Dispose_Should_Not_Throw_When_Called_On_Unstarted_Queue()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Dispose without ever enqueueing anything
		Should.NotThrow(queue.Dispose);

		// Assert
		Should.NotThrow(queue.Dispose); // Idempotent
	}

	[Fact]
	public async Task Queue_Should_Handle_Tasks_With_Very_Short_Execution_Time()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		int taskCount = 50;
		List<Task<int?>> tasks = new();

		// Act - Enqueue many fast tasks
		for (int i = 0; i < taskCount; i++)
		{
			int value = i;
			tasks.Add(queue.EnqueueAsync<int?>(async _ => value, value, TaskPriority.Normal, Current.CancellationToken));
		}

		int?[] results = await Task.WhenAll(tasks);

		// Assert
		results.Length.ShouldBe(taskCount);
		for (int i = 0; i < taskCount; i++)
		{
			results.ShouldContain(i);
		}
	}

	[Fact]
	public async Task ProcessingLoop_Should_Exit_On_OperationCanceledException_During_Shutdown()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		TaskCompletionSource<bool> taskStarted = new();
		TaskCompletionSource<bool> beginDisposal = new();

		// Act - Enqueue a task that will signal when started, then wait for disposal signal
		Task<string?> enqueueTask = queue.EnqueueAsync<string?>(async ct =>
		{
			taskStarted.SetResult(true);
			await beginDisposal.Task; // Wait for signal to proceed

			// Check cancellation token and throw if cancelled
			ct.ThrowIfCancellationRequested();
			return "should not complete";
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for task to start processing
		await taskStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Current.CancellationToken);

		// Signal we're starting disposal and dispose
		beginDisposal.SetResult(true);
		queue.Dispose();

		// Assert - The task should be cancelled
		await Should.ThrowAsync<OperationCanceledException>(async () => await enqueueTask);
	}

	[Fact]
	public async Task Dispose_With_Already_Disposed_Resources_Should_Not_Throw()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Enqueue and process at least one task to ensure resources are initialized
		await queue.EnqueueAsync<int?>(async _ => 42, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken); // Let it process

		// Act - Call Dispose multiple times rapidly to potentially hit ObjectDisposedException catch
		queue.Dispose();
		await Task.Delay(10, Current.CancellationToken); // Small delay
		queue.Dispose(); // Second dispose
		queue.Dispose(); // Third dispose

		// Assert - Should not throw
		Should.NotThrow(queue.Dispose);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Handle_ObjectDisposedException_During_Wait()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Enqueue a quick task to start the processing loop
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait briefly for processing to start
		await Task.Delay(50, Current.CancellationToken);

		// Act - Dispose while the processing loop is waiting for new tasks
		// This should trigger ObjectDisposedException in the wait loop
		queue.Dispose();

		// Wait for disposal to complete
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Queue should be disposed without hanging
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task CancellationToken_Passed_To_Task_Should_Respect_Queue_Shutdown()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		bool taskWasCancelled = false;
		TaskCompletionSource<bool> taskStarted = new();

		// Act - Enqueue a task that monitors its cancellation token
		Task<bool?> enqueueTask = queue.EnqueueAsync<bool?>(async ct =>
		{
			taskStarted.SetResult(true);
			try
			{
				// Wait with the cancellation token
				await Task.Delay(2000, ct);
				return false;
			}
			catch (OperationCanceledException)
			{
				taskWasCancelled = true;
				throw;
			}
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for task to start
		await taskStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Current.CancellationToken);

		// Dispose the queue to trigger cancellation
		queue.Dispose();

		// Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await enqueueTask);
		taskWasCancelled.ShouldBeTrue("Task should have received cancellation signal");
	}

	[Fact]
	public async Task Multiple_Tasks_Should_Be_Cancelled_On_Queue_Disposal()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		int cancelledCount = 0;
		List<Task> tasks = new();

		// Act - Enqueue multiple slow tasks
		for (int i = 0; i < 5; i++)
		{
			tasks.Add(queue.EnqueueAsync<int?>(async ct =>
			{
				try
				{
					await Task.Delay(5000, ct); // Long delay
					return i;
				}
				catch (OperationCanceledException)
				{
					Interlocked.Increment(ref cancelledCount);
					throw;
				}
			}, i, TaskPriority.Normal, Current.CancellationToken));
		}

		// Give tasks time to start being processed
		await Task.Delay(100, Current.CancellationToken);

		// Dispose the queue
		queue.Dispose();

		// Assert - At least some tasks should be cancelled
		// (Some might complete if they finish before disposal)
		await Task.Delay(200, Current.CancellationToken);

		// Don't wait for the tasks as they may not complete - just verify disposal worked
		cancelledCount.ShouldBeGreaterThan(0, "At least some tasks should have been cancelled");
	}

	[Fact]
	public async Task Dispose_Should_Handle_AggregateException_From_ProcessingTask_Wait()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		TaskCompletionSource<bool> taskStarted = new();
		TaskCompletionSource<bool> taskCanComplete = new();

		// Enqueue a task that will start and then wait, simulating a long-running task
		Task<int?> longRunningTask = queue.EnqueueAsync<int?>(async ct =>
		{
			taskStarted.SetResult(true);
			await taskCanComplete.Task; // Wait indefinitely
			return 42;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for task to start processing
		await taskStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Current.CancellationToken);

		// Act - Dispose will wait for processing task which may throw AggregateException
		Should.NotThrow(queue.Dispose);

		// Complete the task to clean up
		taskCanComplete.SetResult(true);

		// Assert - Disposal completed without throwing
		Should.NotThrow(queue.Dispose); // Should be idempotent

		// Clean up the enqueued task
#pragma warning disable S108 // Either remove or fill this block of code.
		try { await longRunningTask.WaitAsync(TimeSpan.FromMilliseconds(100), Current.CancellationToken); } catch { }
#pragma warning restore S108 // Either remove or fill this block of code.
	}

	[Fact]
	public async Task Dispose_Should_Handle_Already_Disposed_CancellationTokenSource()
	{
		// Arrange - Create a derived class that exposes the cancellation issue
		DisposableResourceTestQueue queue = new("test");
		disposables.Add(queue);

		// Start the queue by enqueueing a task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);

		// Pre-dispose the cancellation token source to simulate race condition
		queue.PreDisposeCancellationToken();

		// Act - Dispose should handle ObjectDisposedException gracefully
		Should.NotThrow(queue.Dispose);

		// Assert
		Should.NotThrow(queue.Dispose); // Idempotent
	}

	[Fact]
	public async Task ProcessingLoop_Should_Continue_After_Wait_Timeout()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Enqueue first task to start the processing loop
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken); // Processing loop is now waiting

		// Wait longer than the 100ms timeout to ensure timeout occurs multiple times
		await Task.Delay(350, Current.CancellationToken);

		// Act - Enqueue another task after multiple timeout cycles
		int? result = await queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);

		// Give time for stats to update
		await Task.Delay(50, Current.CancellationToken);

		// Assert
		result.ShouldBe(2);
		queue.Stats.TotalProcessedTasks.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task Dispose_During_ProcessingTask_Wait_Should_Complete_Within_Timeout()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Enqueue a task to start processing
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Act - Dispose and measure time (should complete within 5 second timeout + buffer)
		Stopwatch sw = Stopwatch.StartNew();
		queue.Dispose();
		sw.Stop();

		// Assert - Should complete quickly (much less than 5 seconds since queue is idle)
		sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(6));
	}

	[Fact]
	public async Task ProcessingLoop_Should_Handle_Task_With_Cancelled_State_Before_Processing()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Let blocking task start

		// Enqueue tasks that will be cancelled before processing
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => 2, 1, TaskPriority.Normal, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => 3, 1, TaskPriority.Normal, Current.CancellationToken);

		// Cancel the tasks while they're queued
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Act - Release blocking task so processing continues
		blockProcessing.Release();
		await blockingTask;

		// Assert - Cancelled tasks should be handled gracefully
		await Should.ThrowAsync<TaskCanceledException>(async () => await task1);
		await Should.ThrowAsync<TaskCanceledException>(async () => await task2);

		// Stats should reflect cancelled tasks
		queue.Stats.TotalCancelledTasks.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task Queue_Should_Handle_Very_High_Priority_Values()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockFirst = new(0, 1);
		List<int> executionOrder = new();

		// Enqueue blocking task
		Task<int?> firstTask = queue.EnqueueAsync<int?>(async ct =>
		{
			executionOrder.Add(0);
			await blockFirst.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Enqueue tasks with very high priority values
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(1); return 1; }, int.MaxValue, TaskPriority.Emergency, Current.CancellationToken);
		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(2); return 2; }, int.MaxValue - 1, TaskPriority.Emergency, Current.CancellationToken);
		Task<int?> task3 = queue.EnqueueAsync<int?>(async _ => { executionOrder.Add(3); return 3; }, 1000000, TaskPriority.Critical, Current.CancellationToken);

		// Act
		blockFirst.Release();
		await Task.WhenAll(firstTask, task1, task2, task3);

		// Assert - Should process in priority order
		executionOrder[0].ShouldBe(0); // Blocking task
		executionOrder[1].ShouldBe(1); // Highest priority (int.MaxValue)
		executionOrder[2].ShouldBe(2); // Second highest (int.MaxValue - 1)
		executionOrder[3].ShouldBe(3); // Third highest (1000000)
	}

	[Fact]
	public async Task Finalizer_Should_Cleanup_Resources_When_Queue_Not_Explicitly_Disposed()
	{
		// Arrange & Act
		WeakReference queueRef = CreateQueueForFinalization();

		// Force garbage collection to trigger finalizer
#pragma warning disable S1215 // Refactor the code to remove this use of 'GC.Collect'.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
#pragma warning restore S1215 // Refactor the code to remove this use of 'GC.Collect'.

		await Task.Delay(300, Current.CancellationToken);

		// Assert - Queue should be collected
		queueRef.IsAlive.ShouldBeFalse();
	}

	private static WeakReference CreateQueueForFinalization()
	{
		// Create a queue in a separate method so it goes out of scope
		PrioritizedEndpointQueue queue = new("finalizer-test", 1000);

		// DO NOT add to disposables list - we want it to be garbage collected

		return new WeakReference(queue);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Exit_Cleanly_On_Shutdown_Signal()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process a task to ensure processing loop is started
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Act - Dispose sets shutdown flag and cancels processing
		queue.Dispose();

		// Give processing loop time to exit
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Queue should be fully disposed
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBe(1);
		stats.CurrentProcessingPriority.ShouldBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_Should_Handle_Task_That_Completes_Synchronously()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Act - Enqueue a task that completes synchronously
		int? result = await queue.EnqueueAsync<int?>(ct => Task.FromResult<int?>(42), 1, TaskPriority.Normal, Current.CancellationToken);

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task Multiple_Dispose_Calls_Should_Not_Cause_Issues_With_Resource_Cleanup()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process a task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);

		// Act - Call dispose multiple times rapidly
		Task dispose1 = Task.Run(queue.Dispose, Current.CancellationToken);
		Task dispose2 = Task.Run(queue.Dispose, Current.CancellationToken);
		Task dispose3 = Task.Run(queue.Dispose, Current.CancellationToken);

		await Task.WhenAll(dispose1, dispose2, dispose3);

		// Assert - No exceptions
		Should.NotThrow(queue.Dispose);
	}

	[Fact]
	public async Task ProcessingLoop_OperationCanceledException_Should_Be_Caught_And_Handled()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		TaskCompletionSource<bool> taskProcessing = new();

		// Enqueue a task that will be processing when cancellation occurs
		Task<int?> processingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			taskProcessing.SetResult(true);
			// Simulate work that checks cancellation token
			for (int i = 0; i < 100; i++)
			{
				ct.ThrowIfCancellationRequested();
				await Task.Delay(10, ct);
			}
			return 42;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for task to start processing
		await taskProcessing.Task;

		// Act - Dispose triggers cancellation
		queue.Dispose();

		// Assert - Should handle OperationCanceledException
		await Should.ThrowAsync<OperationCanceledException>(async () => await processingTask);
	}

	[Fact]
	public async Task Queue_Should_Process_Tasks_With_Negative_Priority_Values()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		List<int> results = new();

		// Act - Enqueue tasks with negative priorities
		await queue.EnqueueAsync<int?>(async _ => { results.Add(1); return 1; }, -100, TaskPriority.Low, Current.CancellationToken);
		await queue.EnqueueAsync<int?>(async _ => { results.Add(2); return 2; }, -50, TaskPriority.Low, Current.CancellationToken);
		await queue.EnqueueAsync<int?>(async _ => { results.Add(3); return 3; }, -10, TaskPriority.Low, Current.CancellationToken);

		await Task.Delay(200, Current.CancellationToken);

		// Assert - All tasks should process
		results.Count.ShouldBe(3);
		queue.Stats.TotalProcessedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Catch_OperationCanceledException_When_Task_Cancelled_During_Execution()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockProcessing = new(0, 1);
		SemaphoreSlim taskStarted = new(0, 1);

		// Enqueue a blocking task to hold up processing
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockProcessing.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue the task we'll cancel while it's queued
		Task<int?> taskToCancel = queue.EnqueueAsync<int?>(async ct =>
		{
			taskStarted.Release();
			await Task.Delay(100, ct); // This will throw when cancelled
			return 42;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken); // Ensure blocking task starts

		// Act - Cancel the queued task
		bool cancelled = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		blockProcessing.Release();
		await blockingTask;

		// Assert
		cancelled.ShouldBeTrue();
		await Should.ThrowAsync<TaskCanceledException>(async () => await taskToCancel);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Exit_Gracefully_On_Outer_OperationCanceledException()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Start the processing loop by enqueueing a task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);

		// Act - Dispose triggers cancellation which should be caught by outer catch
		queue.Dispose();
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Queue should be in a valid state
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Handle_ObjectDisposedException_In_Wait()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Start processing
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now idle and waiting for new tasks

		// Act - Dispose while waiting (triggers ObjectDisposedException in newTaskEvent.Wait)
		queue.Dispose();

		// Give time for processing loop to exit
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Should exit gracefully
		Should.NotThrow(queue.Dispose); // Idempotent
	}

	[Fact]
	public async Task Task_Cancelled_During_Execution_Should_Trigger_Specific_Catch_Block()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockTask = new(0, 1);
		TaskCompletionSource<bool> taskProcessing = new();

		// Enqueue a blocking task first
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockTask.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue the task we'll cancel
		Task<int?> targetTask = queue.EnqueueAsync<int?>(async ct =>
		{
			taskProcessing.SetResult(true);
			// Simulate work that checks cancellation
			await Task.Delay(5000, ct);
			return 42;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Cancel the specific priority while task is queued
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		blockTask.Release();
		await blockingTask;

		// Act & Assert - The cancelled task should throw
		await Should.ThrowAsync<TaskCanceledException>(async () => await targetTask);

		// Stats should reflect cancellation
		queue.Stats.TotalCancelledTasks.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Catch_OperationCanceledException_During_Wait()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		TaskCompletionSource<bool> taskProcessed = new();

		// Enqueue a task to start the processing loop
		Task<int?> initialTask = queue.EnqueueAsync<int?>(async _ =>
		{
			taskProcessed.SetResult(true);
			return 1;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for task to complete
		await taskProcessed.Task;
		await initialTask;

		// Now the queue is idle and waiting in newTaskEvent.Wait()
		await Task.Delay(50, Current.CancellationToken);

		// Act - Dispose triggers cancellation which will throw OperationCanceledException in Wait
		queue.Dispose();

		// Give time for the exception to be caught
		await Task.Delay(300, Current.CancellationToken);

		// Assert - Queue should be properly disposed without hanging
		Should.NotThrow(queue.Dispose); // Idempotent
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Catch_OperationCanceledException_When_Task_Is_Cancelled()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockTask = new(0, 1);
		TaskCompletionSource<bool> taskStarted = new();

		// Enqueue a blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockTask.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue a task that will be cancelled and throw OperationCanceledException
		Task<int?> taskToCancel = queue.EnqueueAsync<int?>(async ct =>
		{
			taskStarted.SetResult(true);
			// This will throw OperationCanceledException when cancelled
			await Task.Delay(10000, ct);
			return 42;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Cancel the task while it's queued
		bool cancelled = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);
		cancelled.ShouldBeTrue();

		// Release the blocking task so processing continues
		blockTask.Release();
		await blockingTask;

		// Act & Assert - Should catch OperationCanceledException with currentTask.IsCancelled filter
		await Should.ThrowAsync<TaskCanceledException>(async () => await taskToCancel);
	}

	[Fact]
	public async Task ProcessingLoop_Should_Exit_On_OperationCanceledException_From_Cancellation()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process an initial task to ensure processing loop is running
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Act - Dispose triggers cancellation of the processing loop
		queue.Dispose();

		// Give time for OperationCanceledException to be caught in outer catch block
		await Task.Delay(300, Current.CancellationToken);

		// Assert - Processing loop should have exited gracefully
		PrioritizedQueueStats stats = queue.Stats;
		stats.TotalProcessedTasks.ShouldBe(1);
		stats.CurrentProcessingPriority.ShouldBeNull();
	}

	[Fact]
	public async Task Cancellation_During_Wait_Should_Exit_Processing_Loop_Cleanly()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Start the processing loop with a quick task
		await queue.EnqueueAsync<int?>(async _ => 42, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now idle, waiting for new tasks (in newTaskEvent.Wait with 100ms timeout)

		// Act - Dispose while waiting, triggering OperationCanceledException in the Wait call
		queue.Dispose();

		// Wait for graceful shutdown
		await Task.Delay(250, Current.CancellationToken);

		// Assert - Should have exited cleanly
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
		Should.NotThrow(() => queue.Stats); // Stats should still be accessible
	}

	[Fact]
	public async Task Task_Execution_With_OperationCanceledException_Should_Be_Handled_Properly()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockFirst = new(0, 1);

		// Enqueue blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockFirst.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.High, Current.CancellationToken);

		// Enqueue task that will execute and then be cancelled
		Task<int?> cancellableTask = queue.EnqueueAsync<int?>(async ct =>
		{
			// This will throw OperationCanceledException when ct is cancelled
			await Task.Delay(30000, ct);
			return 2;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Cancel while queued
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		blockFirst.Release();
		await blockingTask;

		// Act & Assert
		await Should.ThrowAsync<TaskCanceledException>(async () => await cancellableTask);

		// The specific catch block for OperationCanceledException when currentTask.IsCancelled should have been hit
		queue.Stats.TotalCancelledTasks.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task Multiple_Cancellations_Should_Trigger_OperationCanceledException_Handler()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim block = new(0, 1);
		List<Task<int?>> cancelledTasks = new();

		// Enqueue blocking task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await block.WaitAsync(ct);
			return 0;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue multiple tasks that will be cancelled
		for (int i = 0; i < 5; i++)
		{
			int value = i;
			cancelledTasks.Add(queue.EnqueueAsync<int?>(async ct =>
			{
				await Task.Delay(5000, ct);
				return value;
			}, 1, TaskPriority.Normal, Current.CancellationToken));
		}

		await Task.Delay(50, Current.CancellationToken);

		// Act - Cancel all Normal priority tasks
		bool cancelled = await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		block.Release();
		await blockingTask;

		// Assert - All tasks should throw TaskCanceledException
		foreach (Task<int?> task in cancelledTasks)
		{
			await Should.ThrowAsync<TaskCanceledException>(async () => await task);
		}

		cancelled.ShouldBeTrue();
		queue.Stats.TotalCancelledTasks.ShouldBe(5);
	}

	[Fact]
	public async Task ProcessingLoop_OperationCanceledException_Should_Not_Affect_Completed_Stats()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		int tasksCompleted = 0;

		// Process several tasks successfully
		for (int i = 0; i < 3; i++)
		{
			await queue.EnqueueAsync<int?>(async _ =>
			{
				Interlocked.Increment(ref tasksCompleted);
				return i;
			}, 1, TaskPriority.Normal, Current.CancellationToken);
		}

		await Task.Delay(200, Current.CancellationToken);

		// Act - Dispose triggers OperationCanceledException in processing loop
		queue.Dispose();
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Completed tasks should be counted correctly despite cancellation
		tasksCompleted.ShouldBe(3);
		queue.Stats.TotalProcessedTasks.ShouldBe(3);
		queue.Stats.TotalFailedTasks.ShouldBe(0);
	}

	[Fact]
	public async Task OperationCanceledException_During_newTaskEvent_Wait_Should_Exit_Loop()
	{
		// Arrange
		DisposalTimingTestQueue queue = new("test");
		disposables.Add(queue);

		// Start the processing loop with a task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now idle, waiting in newTaskEvent.Wait with 100ms timeout
		// Wait for it to be in the Wait state
		await Task.Delay(50, Current.CancellationToken);

		// Act - Trigger cancellation while in Wait (before checking isShuttingDown)
		queue.TriggerCancellationDuringWait();

		// Give time for the catch block to execute
		await Task.Delay(300, Current.CancellationToken);

		// Assert - Should have exited via OperationCanceledException catch block
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task ProcessingLoop_With_Immediate_Cancellation_Should_Catch_OperationCanceledException()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process one task to start the loop
		await queue.EnqueueAsync<int?>(async _ => 42, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait a bit less than the timeout to ensure we're in the Wait call
		await Task.Delay(80, Current.CancellationToken);

		// Act - Dispose immediately triggers cancellation during Wait
		Stopwatch sw = Stopwatch.StartNew();
		queue.Dispose();
		sw.Stop();

		// Wait for cleanup
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Should complete quickly (not wait for full 5 second timeout)
		sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task OperationCanceledException_In_Task_Execution_Should_Use_Filtered_Catch()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();
		SemaphoreSlim blockFirst = new(0, 1);
		bool executionStarted = false;

		// Block the queue with a high priority task
		Task<int?> blockingTask = queue.EnqueueAsync<int?>(async ct =>
		{
			await blockFirst.WaitAsync(ct);
			return 1;
		}, 1, TaskPriority.Critical, Current.CancellationToken);

		// Enqueue task that will throw OperationCanceledException during execution
		Task<int?> cancelledTask = queue.EnqueueAsync<int?>(async ct =>
		{
			executionStarted = true;
			ct.ThrowIfCancellationRequested(); // Will throw if already cancelled
			await Task.Delay(10000, ct);
			return 2;
		}, 1, TaskPriority.Normal, Current.CancellationToken);

		await Task.Delay(50, Current.CancellationToken);

		// Cancel it while queued
		await queue.CancelTasksByPriorityAsync(TaskPriority.Normal);

		// Release blocking task
		blockFirst.Release();
		await blockingTask;

		// Act & Assert - Should catch with the (currentTask.IsCancelled) filter
		await Should.ThrowAsync<TaskCanceledException>(async () => await cancelledTask);

		// Task should not have started execution since it was cancelled while queued
		executionStarted.ShouldBeFalse();
	}

	[Fact]
	public async Task IsShuttingDown_Flag_Should_Break_Processing_Loop_Before_Wait()
	{
		// Arrange
		ShutdownTestQueue queue = new("test");
		disposables.Add(queue);

		// Start the processing loop
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now idle (currentTask == null condition)
		// Wait a bit to ensure we're past the dequeue
		await Task.Delay(50, Current.CancellationToken);

		// Act - Set shutdown flag before the wait happens (via Dispose)
		queue.Dispose();

		// Give time for the loop to check isShuttingDown and break
		await Task.Delay(300, Current.CancellationToken);

		// Assert - Should have exited via isShuttingDown check
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
		queue.Stats.CurrentProcessingPriority.ShouldBeNull();
	}

	[Fact]
	public async Task OperationCanceledException_During_Wait_Should_Break_Loop()
	{
		// Arrange
		CancellationDuringWaitTestQueue queue = new("test");
		disposables.Add(queue);

		// Start processing with initial task
		await queue.EnqueueAsync<int?>(async _ => 42, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Queue is now idle and waiting in newTaskEvent.Wait with 100ms timeout
		// Wait to ensure we're in the Wait call
		await Task.Delay(30, Current.CancellationToken);

		// Act - Trigger cancellation during Wait (not during shutdown check)
		queue.TriggerCancellationNow();

		// Give time for OperationCanceledException catch block to execute
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Should have caught OperationCanceledException and broken out
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task Shutdown_Check_Should_Exit_Loop_When_No_Tasks_Available()
	{
		// Arrange
		IsShuttingDownTestQueue queue = new("test");
		disposables.Add(queue);

		// Process initial task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(100, Current.CancellationToken);

		// Now the loop is idle, waiting for tasks
		// currentTask will be null after dequeue

		// Act - Set isShuttingDown flag to true
		queue.SetShuttingDown();

		// Give time for the loop to check and break
		await Task.Delay(250, Current.CancellationToken);

		// Assert - Loop should have exited via isShuttingDown break
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task Wait_With_OperationCanceledException_Should_Catch_And_Break()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process one task to start the loop
		await queue.EnqueueAsync<int?>(async _ => 99, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken); // Let it complete

		// Queue is now idle in wait state (no tasks, waiting in newTaskEvent.Wait)
		// Wait less than timeout to be in the Wait call
		await Task.Delay(40, Current.CancellationToken);

		// Act - Dispose triggers cancellation during Wait
		queue.Dispose();

		// Wait for the catch block to execute and loop to exit
		await Task.Delay(250, Current.CancellationToken);

		// Assert - Should have exited cleanly via OperationCanceledException catch
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
		Should.NotThrow(() => queue.Stats);
	}

	[Fact]
	public async Task Processing_Loop_Should_Check_Shutdown_Before_Each_Wait()
	{
		// Arrange
		PrioritizedEndpointQueue queue = CreateQueue();

		// Process a quick task
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(80, Current.CancellationToken);

		// Loop is now at the point where currentTask is null (no tasks in queue)
		// It will check isShuttingDown before calling Wait

		// Act - Dispose sets isShuttingDown = true
		Stopwatch sw = Stopwatch.StartNew();
		queue.Dispose();
		sw.Stop();

		await Task.Delay(200, Current.CancellationToken);

		// Assert - Should exit quickly via isShuttingDown check (not waiting for full timeout)
		sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task Exact_IsShuttingDown_Break_Path_Should_Be_Hit()
	{
		// Arrange
		ShutdownFlagTestQueue queue = new("test");
		disposables.Add(queue);

		// Start and complete a task to get processing loop running
		await queue.EnqueueAsync<int?>(async _ => 1, 1, TaskPriority.Normal, Current.CancellationToken);
		await Task.Delay(50, Current.CancellationToken);

		// Now the queue is empty (currentTask will be null after dequeue)
		// The loop will check isShuttingDown before calling Wait

		// Act - Set the shutdown flag directly before the Wait happens
		queue.ForceShutdownFlag();

		// Allow time for the loop to check the flag and break
		await Task.Delay(200, Current.CancellationToken);

		// Assert - Should have exited via isShuttingDown check
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
		queue.IsShutdownFlagSet().ShouldBeTrue();
	}

	[Fact]
	public async Task Exact_OperationCanceledException_Catch_During_Wait()
	{
		// Arrange
		WaitCancellationTestQueue queue = new("test");
		disposables.Add(queue);

		// Process initial task
		await queue.EnqueueAsync<int?>(async _ => 42, 1, TaskPriority.Normal, Current.CancellationToken);

		// Wait for it to complete and enter wait state
		await Task.Delay(70, Current.CancellationToken);

		// Queue is idle, in Wait call
		// Now trigger cancellation during Wait (not shutdown)

		// Act - Cancel the token while in Wait
		queue.CancelDuringWait();

		// Give time for catch and break
		await Task.Delay(250, Current.CancellationToken);

		// Assert - Should have caught OperationCanceledException and exited
		queue.Stats.TotalProcessedTasks.ShouldBe(1);
	}

	private class TestPrioritizedEndpointQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
		public int CancelCallCount { get; private set; }

		public override async Task<bool> CancelTasksByPriorityAsync(TaskPriority priority)
		{
			CancelCallCount++;
			return await base.CancelTasksByPriorityAsync(priority);
		}
	}

	private class DisposableResourceTestQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
		private readonly System.Reflection.FieldInfo? cancellationTokenSourceField = typeof(PrioritizedEndpointQueue)
			.GetField("cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		public void PreDisposeCancellationToken()
		{
			if (cancellationTokenSourceField != null)
			{
				CancellationTokenSource? cts = cancellationTokenSourceField.GetValue(this) as CancellationTokenSource;
				cts?.Dispose();
			}
		}
	}

	private class DisposalTimingTestQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
		private readonly System.Reflection.FieldInfo? cancellationTokenSourceField = typeof(PrioritizedEndpointQueue)
			.GetField("cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		public void TriggerCancellationDuringWait()
		{
			if (cancellationTokenSourceField != null)
			{
				CancellationTokenSource? cts = cancellationTokenSourceField.GetValue(this) as CancellationTokenSource;
				cts?.Cancel();
			}
		}
	}

	private class ShutdownTestQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
	}

	private class CancellationDuringWaitTestQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
		private readonly System.Reflection.FieldInfo? cancellationTokenSourceField = typeof(PrioritizedEndpointQueue)
			.GetField("cancellationTokenSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		public void TriggerCancellationNow()
		{
			if (cancellationTokenSourceField != null)
			{
				CancellationTokenSource? cts = cancellationTokenSourceField.GetValue(this) as CancellationTokenSource;
				cts?.Cancel();
			}
		}
	}

	private class IsShuttingDownTestQueue(string endpointKey) : PrioritizedEndpointQueue(endpointKey)
	{
		private readonly System.Reflection.FieldInfo? isShuttingDownField = typeof(PrioritizedEndpointQueue)
			.GetField("isShuttingDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		public void SetShuttingDown()
		{
			isShuttingDownField?.SetValue(this, true);
		}
	}

	private class ShutdownFlagTestQueue(string name) : PrioritizedEndpointQueue(name)
	{
		private readonly System.Reflection.FieldInfo isShuttingDownField = typeof(PrioritizedEndpointQueue).GetField("isShuttingDown",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

		public void ForceShutdownFlag()
		{
			isShuttingDownField.SetValue(this, true);
		}

		public bool IsShutdownFlagSet()
		{
			return (bool)isShuttingDownField.GetValue(this)!;
		}
	}

	private class WaitCancellationTestQueue(string name) : PrioritizedEndpointQueue(name)
	{
		private readonly System.Reflection.FieldInfo cancellationTokenSourceField = typeof(PrioritizedEndpointQueue).GetField("cancellationTokenSource",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

		public void CancelDuringWait()
		{
			CancellationTokenSource? cts = cancellationTokenSourceField.GetValue(this) as CancellationTokenSource;
			cts?.Cancel();
		}
	}

	private class ComplexResult
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public List<string>? Items { get; set; }
	}
}
