using System.Threading.Channels;
using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace Web.Api.Tests.TaskQueuing.EndpointQueue;

public class EndpointQueueTests : IDisposable
{
	private readonly List<CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue> _queuesToDispose = new();

	[Fact]
	public void Constructor_BoundedChannelOptions_Should_Initialize()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		const string endpointKey = "test-endpoint";

		// Act
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new(endpointKey, options);
		_queuesToDispose.Add(queue);

		// Assert
		queue.EndpointKey.ShouldBe(endpointKey);
		queue.Stats.ShouldNotBeNull();
		queue.Stats.EndpointKey.ShouldBe(endpointKey);
	}

	[Fact]
	public void Constructor_UnboundedChannelOptions_Should_Initialize()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		const string endpointKey = "test-endpoint-unbounded";

		// Act
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new(endpointKey, options);
		_queuesToDispose.Add(queue);

		// Assert
		queue.EndpointKey.ShouldBe(endpointKey);
		queue.Stats.ShouldNotBeNull();
		queue.Stats.EndpointKey.ShouldBe(endpointKey);
	}

	[Fact]
	public void Constructor_BoundedChannelOptions_WithCustomProcessTimeWindow_Should_Initialize()
	{
		// Arrange
		BoundedChannelOptions options = new(5);
		const string endpointKey = "test-endpoint-custom-window";

		// Act
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new(endpointKey, options, processTimeWindow: 500);
		_queuesToDispose.Add(queue);

		// Assert
		queue.EndpointKey.ShouldBe(endpointKey);
	}

	[Fact]
	public void Constructor_UnboundedChannelOptions_WithCustomProcessTimeWindow_Should_Initialize()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		const string endpointKey = "test-endpoint-unbounded-custom-window";

		// Act
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new(endpointKey, options, processTimeWindow: 500);
		_queuesToDispose.Add(queue);

		// Assert
		queue.EndpointKey.ShouldBe(endpointKey);
	}

	[Fact]
	public async Task EnqueueAsync_BoundedChannel_Should_Execute_Task()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);
		const int expectedResult = 42;

		// Act
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(expectedResult), TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task EnqueueAsync_UnboundedChannel_Should_Execute_Task()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);
		const int expectedResult = 99;

		// Act
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(expectedResult), TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_QueuedTasks_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(2), TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.QueuedTasks.ShouldBe(2);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_ProcessedTasks_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(2), TestContext.Current.CancellationToken);

		// Wait a bit for processing
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.ProcessedTasks.ShouldBe(2);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Update_LastProcessedAt()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);
		DateTime startTime = DateTime.UtcNow;

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);

		// Wait a bit for processing
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.LastProcessedAt.ShouldNotBeNull();
		stats.LastProcessedAt.Value.ShouldBeGreaterThanOrEqualTo(startTime);
	}

	[Fact]
	public async Task EnqueueAsync_Should_Calculate_AverageProcessingTime()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		await queue.EnqueueAsync(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			return 1;
		}, TestContext.Current.CancellationToken);

		await queue.EnqueueAsync(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			return 2;
		}, TestContext.Current.CancellationToken);

		// Wait for processing
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.AverageProcessingTime.ShouldNotBeNull();
		stats.AverageProcessingTime.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task EnqueueAsync_TaskException_Should_Update_FailedTasks_Stats()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await queue.EnqueueAsync<int>(_ => throw new InvalidOperationException("Test exception"), TestContext.Current.CancellationToken);
		});

		// Wait a bit for stats to update
		await Task.Delay(50, TestContext.Current.CancellationToken);

		QueueStats stats = queue.Stats;
		stats.FailedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task EnqueueAsync_Multiple_Tasks_Should_Execute_Sequentially()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);
		List<int> executionOrder = new();
		object lockObj = new();

		// Act
		Task<int?> task1 = queue.EnqueueAsync<int?>(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(1); }
			return 1;
		}, TestContext.Current.CancellationToken);

		Task<int?> task2 = queue.EnqueueAsync<int?>(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(2); }
			return 2;
		}, TestContext.Current.CancellationToken);

		Task<int?> task3 = queue.EnqueueAsync<int?>(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			lock (lockObj) { executionOrder.Add(3); }
			return 3;
		}, TestContext.Current.CancellationToken);

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
	public async Task EnqueueAsync_With_CancellationToken_Should_Work()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);
		using CancellationTokenSource cts = new();

		// Act
		int? result = await queue.EnqueueAsync(_ => Task.FromResult(42), cts.Token);

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public async Task Stats_Should_Return_Correct_EndpointKey()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		const string endpointKey = "my-endpoint";
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new(endpointKey, options);
		_queuesToDispose.Add(queue);

		// Act
		QueueStats stats = queue.Stats;

		// Assert
		stats.EndpointKey.ShouldBe(endpointKey);
	}

	[Fact]
	public async Task ProcessTimeWindow_Should_Limit_ProcessingTimes_Count()
	{
		// Arrange
		BoundedChannelOptions options = new(100);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options, processTimeWindow: 10);
		_queuesToDispose.Add(queue);

		// Act - Queue more tasks than the window size
		for (int i = 0; i < 15; i++)
		{
			await queue.EnqueueAsync(_ => Task.FromResult(i), TestContext.Current.CancellationToken);
		}

		// Wait for processing
		await Task.Delay(100, TestContext.Current.CancellationToken);

		// Assert - Stats should still work correctly
		QueueStats stats = queue.Stats;
		stats.ProcessedTasks.ShouldBe(15);
		stats.AverageProcessingTime.ShouldNotBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_With_Async_Task_Should_Complete()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		string? result = await queue.EnqueueAsync(async _ =>
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
			return "async result";
		}, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("async result");
	}

	[Fact]
	public async Task EnqueueAsync_Null_Result_Should_Work()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		string? result = await queue.EnqueueAsync(_ => Task.FromResult<string?>(null), TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void Dispose_Should_Complete_Processing()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);

		// Act
		queue.Dispose();

		// Assert - Should not throw
		Should.NotThrow(() => queue.Dispose()); // Can call Dispose multiple times
	}

	[Fact]
	public void Dispose_Multiple_Times_Should_Be_Safe()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);

		// Act & Assert - Should not throw when disposed multiple times
		queue.Dispose();
		queue.Dispose();
		queue.Dispose();

		Should.NotThrow(() => queue.Dispose());
	}

	[Fact]
	public async Task Dispose_After_Tasks_Complete_Should_Work()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);
		await queue.EnqueueAsync(_ => Task.FromResult(2), TestContext.Current.CancellationToken);

		// Wait for completion
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Dispose after tasks are done
		queue.Dispose();

		// Assert - Should not throw
		Should.NotThrow(() => queue.Dispose());
	}

	[Fact]
	public void Dispose_With_Unbounded_Queue_Should_Work()
	{
		// Arrange
		UnboundedChannelOptions options = new();
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);

		// Act & Assert
		Should.NotThrow(() => queue.Dispose());
	}

	[Fact]
	public async Task ProcessTasksAsync_Should_Handle_Long_Running_Task()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act - Queue a longer running task
		int? result = await queue.EnqueueAsync(async _ =>
		{
			await Task.Delay(100, TestContext.Current.CancellationToken);
			return 42;
		}, TestContext.Current.CancellationToken);

		// Wait for stats to update
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(42);

		QueueStats stats = queue.Stats;
		stats.ProcessedTasks.ShouldBe(1);
	}

	[Fact]
	public async Task Dispose_Should_Wait_For_Pending_Tasks()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		bool taskExecuted = false;

		// Act
		Task<bool?> task = queue.EnqueueAsync<bool?>(async _ =>
		{
			await Task.Delay(50, TestContext.Current.CancellationToken);
			taskExecuted = true;
			return true;
		}, TestContext.Current.CancellationToken);

		// Small delay to ensure task is being processed
		await Task.Delay(10, TestContext.Current.CancellationToken);

		queue.Dispose();
		await task;

		// Assert
		taskExecuted.ShouldBeTrue();
	}

	[Fact]
	public async Task Stats_With_No_ProcessingTimes_Should_Have_Null_Average()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act - Get stats before any processing
		QueueStats stats = queue.Stats;

		// Assert
		stats.AverageProcessingTime.ShouldBeNull();
	}

	[Fact]
	public async Task EnqueueAsync_Multiple_Exception_Cases_Should_Track_All_Failures()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await queue.EnqueueAsync<int>(_ => throw new InvalidOperationException("Error 1"), TestContext.Current.CancellationToken);
		});

		await Should.ThrowAsync<ArgumentException>(async () =>
		{
			await queue.EnqueueAsync<int>(_ => throw new ArgumentException("Error 2"), TestContext.Current.CancellationToken);
		});

		await Should.ThrowAsync<NotImplementedException>(async () =>
		{
			await queue.EnqueueAsync<int>(_ => throw new NotImplementedException("Error 3"), TestContext.Current.CancellationToken);
		});

		// Wait for stats to update
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.FailedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task EnqueueAsync_Mixed_Success_And_Failure_Should_Track_Both()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);

		await Should.ThrowAsync<InvalidOperationException>(async () =>
		{
			await queue.EnqueueAsync<int>(_ => throw new InvalidOperationException("Error"), TestContext.Current.CancellationToken);
		});

		await queue.EnqueueAsync(_ => Task.FromResult(2), TestContext.Current.CancellationToken);

		// Wait for stats to update
		await Task.Delay(50, TestContext.Current.CancellationToken);

		// Assert
		QueueStats stats = queue.Stats;
		stats.ProcessedTasks.ShouldBe(2);
		stats.FailedTasks.ShouldBe(1);
		stats.QueuedTasks.ShouldBe(3);
	}

	[Fact]
	public async Task EnqueueAsync_Complex_Object_Should_Work()
	{
		// Arrange
		BoundedChannelOptions options = new(10);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act
		TestComplexObject? result = await queue.EnqueueAsync(_ => Task.FromResult(new TestComplexObject
		{
			Id = 123,
			Name = "Test",
			Values = new List<int> { 1, 2, 3 }
		}), TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(123);
		result.Name.ShouldBe("Test");
		result.Values.ShouldBe(new List<int> { 1, 2, 3 });
	}

	[Fact]
	public async Task Stats_Should_Be_Thread_Safe()
	{
		// Arrange
		BoundedChannelOptions options = new(100);
		CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue = new("test", options);
		_queuesToDispose.Add(queue);

		// Act - Queue multiple tasks concurrently
		List<Task> tasks = new();
		for (int i = 0; i < 50; i++)
		{
			tasks.Add(Task.Run(async () =>
			{
				await queue.EnqueueAsync(_ => Task.FromResult(1), TestContext.Current.CancellationToken);
			}, TestContext.Current.CancellationToken));
		}

		await Task.WhenAll(tasks);

		// Wait for processing
		await Task.Delay(200, TestContext.Current.CancellationToken);

		// Assert - Stats should be accurate
		QueueStats stats = queue.Stats;
		stats.QueuedTasks.ShouldBe(50);
		stats.ProcessedTasks.ShouldBe(50);
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
				foreach (CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue.EndpointQueue queue in _queuesToDispose)
				{
					queue?.Dispose();
				}
				_queuesToDispose.Clear();
			}
			_disposed = true;
		}
	}

	private class TestComplexObject
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public List<int> Values { get; set; } = new();
	}
}
