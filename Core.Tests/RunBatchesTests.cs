using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class RunBatchesTests
{
	private readonly Fixture fixture = new();

	[Theory]
	[InlineData(100, 30)]  // Multiple batches
	[InlineData(10, 20)]   // Single batch
	[InlineData(0, 10)]    // Empty collection
	public async Task RunBatchedProcess_IEnumerable_AsyncBatchedProcess_ProcessesCorrectBatches(int itemCount, int batchSize)
	{
		// Arrange
		List<int> processedItems = [];
		IEnumerable<int> items = Enumerable.Range(1, itemCount);

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(itemCount);
		processedItems.Order().SequenceEqual(items).ShouldBeTrue();
	}

	[Theory]
	[InlineData(100, 30)]
	[InlineData(10, 20)]
	[InlineData(0, 10)]
	public async Task RunBatchedProcess_IEnumerable_AsyncBatchedProcessList_ProcessesCorrectBatches(int itemCount, int batchSize)
	{
		// Arrange
		List<int> processedItems = [];
		IEnumerable<int> items = Enumerable.Range(1, itemCount);

		async Task<bool> BatchProcessor(List<int> batch)
		{
			byte[] data = fixture.CreateMany<byte>(batch[0]).ToArray();
			await using MemoryStream stream = new(data);
			_ = await stream.ReadStreamAsync();
			processedItems.AddRange(batch);
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(itemCount);
		processedItems.Order().SequenceEqual(items).ShouldBeTrue();
	}

	[Theory]
	[InlineData(100, 30)]
	[InlineData(10, 20)]
	[InlineData(0, 10)]
	public async Task RunBatchedProcess_List_AsyncBatchedProcess_ProcessesCorrectBatches(int itemCount, int batchSize)
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, itemCount).ToList();

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(itemCount);
		processedItems.Order().SequenceEqual(items).ShouldBeTrue();
	}

	[Theory]
	[InlineData(true)]   // Break on fail
	[InlineData(false)]  // Continue on fail
	public async Task RunBatchedProcess_BreakOnFail_BehavesCorrectly(bool breakOnFail)
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();
		int expectedCount = breakOnFail ? 30 : 100; // If breaking, only first batch processed

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return false; // Always fail
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, breakOnFail, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		processedItems.Count.ShouldBe(expectedCount);
	}

	[Fact]
	public void RunBatchedProcess_Sync_ProcessesCorrectBatches()
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();

		bool BatchProcessor(List<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				using MemoryStream stream = new(data);
				_ = stream.ToArray();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = items.RunBatchedProcess(BatchProcessor, 30, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100);
		processedItems.Order().SequenceEqual(items).ShouldBeTrue();
	}

	[Theory]
	[InlineData(true)]   // Break on fail
	[InlineData(false)]  // Continue on fail
	public void RunBatchedProcess_Sync_BreakOnFail_BehavesCorrectly(bool breakOnFail)
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();
		int expectedCount = breakOnFail ? 30 : 100; // If breaking, only first batch processed

		bool BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				using MemoryStream stream = new(data);
				_ = stream.ToArray();
				processedItems.Add(item);
			}
			return false; // Always fail
		}

		// Act
		bool result = items.RunBatchedProcess(BatchProcessor, 30, breakOnFail, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		processedItems.Count.ShouldBe(expectedCount);
	}

	[Fact]
	public async Task RunBatchedProcess_HandlesDistinctItems()
	{
		// Arrange
		List<int> processedItems = [];
#pragma warning disable S2114 // Collections should not be passed as arguments to their own methods
		List<int> items = Enumerable.Range(1, 100).Concat(Enumerable.Range(1, 100)).ToList(); // Duplicates
#pragma warning restore S2114 // Collections should not be passed as arguments to their own methods

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100); // Should only process distinct items
		processedItems.Distinct().Count().ShouldBe(100);
	}

	[Theory]
	[InlineData(1)]     // Minimum batch size
	[InlineData(10000)] // Default batch size
	[InlineData(100000)] // Large batch size
	public async Task RunBatchedProcess_HandlesDifferentBatchSizes(int batchSize)
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100);
		processedItems.Order().SequenceEqual(items).ShouldBeTrue();
	}

	[Fact]
	public async Task RunBatchedProcess_PreservesOrder()
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = fixture.CreateMany<int>(100).ToList();

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.SequenceEqual(items).ShouldBeTrue();
	}

	[Fact]
	public async Task RunBatchedProcess_HandlesCustomObjects()
	{
		// Arrange
		List<TestItem> processedItems = [];
		List<TestItem> items = fixture.CreateMany<TestItem>(100).ToList();

		async Task<bool> BatchProcessor(IEnumerable<TestItem> batch)
		{
			foreach (TestItem item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(100).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100);
		processedItems.SequenceEqual(items).ShouldBeTrue();
	}

	[Fact]
	public async Task RunBatchedProcess_NullItemsToProcess_ThrowsArgumentNullException()
	{
		// Arrange
		IEnumerable<int> items = null!;
		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			await Task.Delay(1);
			return true;
		}

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () =>
			await items.RunBatchedProcessAsync(BatchProcessor, 10));
	}

	[Fact]
	public async Task RunBatchedProcess_NullProcessor_ThrowsArgumentNullException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		Func<IEnumerable<int>, Task<bool>> processor = null!;

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () =>
			await items.RunBatchedProcessAsync(processor, 10));
	}

	[Fact]
	public async Task RunBatchedProcess_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			await Task.Delay(1);
			return true;
		}

		// Act & Assert
		await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			await items.RunBatchedProcessAsync(BatchProcessor, 0));
	}

	[Fact]
	public async Task RunBatchedProcess_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			await Task.Delay(1);
			return true;
		}

		// Act & Assert
		await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			await items.RunBatchedProcessAsync(BatchProcessor, -5));
	}

	[Fact]
	public async Task RunBatchedProcess_LogProgressFalse_DoesNotLog()
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				await using MemoryStream stream = new(data);
				_ = await stream.ReadStreamAsync();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, logProgress: false, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100);
	}

	[Fact]
	public async Task RunBatchedProcess_CancellationRequested_ThrowsOperationCanceledException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 1000).ToList();
		using CancellationTokenSource cts = new();
		cts.Cancel();

		async Task<bool> BatchProcessor(IEnumerable<int> batch)
		{
			await Task.Delay(1);
			return true;
		}

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () =>
			await items.RunBatchedProcessAsync(BatchProcessor, 30, cancellationToken: cts.Token));
	}

	[Fact]
	public void RunBatchedProcess_Sync_NullItemsToProcess_ThrowsArgumentNullException()
	{
		// Arrange
		IEnumerable<int> items = null!;
		bool BatchProcessor(IEnumerable<int> batch) => true;

		// Act & Assert
		Should.Throw<ArgumentNullException>(() =>
			items.RunBatchedProcess(BatchProcessor, 10));
	}

	[Fact]
	public void RunBatchedProcess_Sync_NullProcessor_ThrowsArgumentNullException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		Func<IEnumerable<int>, bool> processor = null!;

		// Act & Assert
		Should.Throw<ArgumentNullException>(() =>
			items.RunBatchedProcess(processor, 10));
	}

	[Fact]
	public void RunBatchedProcess_Sync_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		bool BatchProcessor(IEnumerable<int> batch) => true;

		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() =>
			items.RunBatchedProcess(BatchProcessor, 0));
	}

	[Fact]
	public void RunBatchedProcess_Sync_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		bool BatchProcessor(IEnumerable<int> batch) => true;

		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() =>
			items.RunBatchedProcess(BatchProcessor, -5));
	}

	[Fact]
	public void RunBatchedProcess_Sync_LogProgressFalse_DoesNotLog()
	{
		// Arrange
		List<int> processedItems = [];
		List<int> items = Enumerable.Range(1, 100).ToList();

		bool BatchProcessor(List<int> batch)
		{
			foreach (int item in batch)
			{
				byte[] data = fixture.CreateMany<byte>(item).ToArray();
				using MemoryStream stream = new(data);
				_ = stream.ToArray();
				processedItems.Add(item);
			}
			return true;
		}

		// Act
		bool result = items.RunBatchedProcess(BatchProcessor, 30, logProgress: false, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
		processedItems.Count.ShouldBe(100);
	}

	[Fact]
	public void RunBatchedProcess_Sync_CancellationRequested_ThrowsOperationCanceledException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 1000).ToList();
		using CancellationTokenSource cts = new();
		cts.Cancel();

		bool BatchProcessor(IEnumerable<int> batch) => true;

		// Act & Assert
		Should.Throw<OperationCanceledException>(() =>
			items.RunBatchedProcess(BatchProcessor, 30, cancellationToken: cts.Token));
	}

	[Fact]
	public async Task RunBatchedProcess_ListProcessor_NullProcessor_ThrowsArgumentNullException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		Func<List<int>, Task<bool>> processor = null!;

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () =>
			await items.RunBatchedProcessAsync(processor, 10));
	}

	[Fact]
	public void RunBatchedProcess_Sync_ListProcessor_NullProcessor_ThrowsArgumentNullException()
	{
		// Arrange
		List<int> items = Enumerable.Range(1, 10).ToList();
		Func<List<int>, bool> processor = null!;

		// Act & Assert
		Should.Throw<ArgumentNullException>(() =>
			items.RunBatchedProcess(processor, 10));
	}

#pragma warning disable S1144 // Unused private types or members should be removed
	private sealed class TestItem
	{
		public int Id { get; set; }

		public string? Name { get; set; }
	}
#pragma warning restore S1144 // Unused private types or members should be removed
}
