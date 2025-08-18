﻿using CommonNetFuncs.Core;

namespace Core.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly
public sealed class RunBatchesTests
{
    private readonly Fixture _fixture = new();

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
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize);

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
            byte[] data = _fixture.CreateMany<byte>(batch[0]).ToArray();
            await using MemoryStream stream = new(data);
            _ = await stream.ReadStreamAsync();
            processedItems.AddRange(batch);
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize);

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
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize);

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
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return false; // Always fail
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30, breakOnFail);

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
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                using MemoryStream stream = new(data);
                _ = stream.ToArray();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = items.RunBatchedProcess(BatchProcessor, 30);

        // Assert
        result.ShouldBeTrue();
        processedItems.Count.ShouldBe(100);
        processedItems.Order().SequenceEqual(items).ShouldBeTrue();
    }

    [Fact]
    public async Task RunBatchedProcess_HandlesDistinctItems()
    {
        // Arrange
        List<int> processedItems = [];
        List<int> items = Enumerable.Range(1, 100).Concat(Enumerable.Range(1, 100)).ToList(); // Duplicates

        async Task<bool> BatchProcessor(IEnumerable<int> batch)
        {
            foreach (int item in batch)
            {
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30);

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
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, batchSize);

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
        List<int> items = _fixture.CreateMany<int>(100).ToList();

        async Task<bool> BatchProcessor(IEnumerable<int> batch)
        {
            foreach (int item in batch)
            {
                byte[] data = _fixture.CreateMany<byte>(item).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30);

        // Assert
        result.ShouldBeTrue();
        processedItems.SequenceEqual(items).ShouldBeTrue();
    }

    [Fact]
    public async Task RunBatchedProcess_HandlesCustomObjects()
    {
        // Arrange
        List<TestItem> processedItems = [];
        List<TestItem> items = _fixture.CreateMany<TestItem>(100).ToList();

        async Task<bool> BatchProcessor(IEnumerable<TestItem> batch)
        {
            foreach (TestItem item in batch)
            {
                byte[] data = _fixture.CreateMany<byte>(100).ToArray();
                await using MemoryStream stream = new(data);
                _ = await stream.ReadStreamAsync();
                processedItems.Add(item);
            }
            return true;
        }

        // Act
        bool result = await items.RunBatchedProcessAsync(BatchProcessor, 30);

        // Assert
        result.ShouldBeTrue();
        processedItems.Count.ShouldBe(100);
        processedItems.SequenceEqual(items).ShouldBeTrue();
    }

    private sealed class TestItem
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
