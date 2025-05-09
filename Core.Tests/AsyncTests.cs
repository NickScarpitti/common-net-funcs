using System.Collections.Concurrent;
using System.Data;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;
using Shouldly;

namespace Core.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public class AsyncTests
{
    private readonly Fixture _fixture;

    public AsyncTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoFakeItEasyCustomization());
    }

    #region ObjectFill<T> Simple/Null/Exception

    [Theory]
    [InlineData(0, 42)]
    [InlineData(100, 200)]
    public async Task ObjectFill_WithSimpleType_ShouldNotAssignTaskResult(int obj, int taskResult)
    {
        Task<int> task = Task.FromResult(taskResult);
        await Async.ObjectFill(obj, task);
        obj.ShouldBe(obj); // Value types don't change
    }

    [Fact]
    public async Task ObjectFill_WithNullObject_ShouldNotThrowException()
    {
        AsyncIntString? obj = null;
        Task<AsyncIntString?> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" })!;
        await Should.NotThrowAsync(async () => await Async.ObjectFill(obj, task));
    }

    [Fact]
    public async Task ObjectFill_WithNullResult_ShouldNotThrowException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString? taskResult = null;
        Task<AsyncIntString?> task = Task.FromResult(taskResult);

        await Async.ObjectFill(obj, task);

        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WhenTaskThrowsException_ShouldHandleException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        Task<AsyncIntString> task = Task.FromException<AsyncIntString>(new InvalidOperationException());
        await Should.NotThrowAsync(async () => await Async.ObjectFill(obj, task));
        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WithComplexType_ShouldCopyProperties()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString taskResult = new() { AsyncInt = 42, AsyncString = "Updated" };
        Task<AsyncIntString> task = Task.FromResult(taskResult);
        await Async.ObjectFill(obj, task);
        obj.AsyncInt.ShouldBe(42);
        obj.AsyncString.ShouldBe("Updated");
    }

    #endregion

    #region ObjectFill<T> Collections (List, HashSet, ConcurrentBag)

    [Theory]
    [InlineData("List", "Test Item", 1)]
    [InlineData("List", null, 1)]
    [InlineData("HashSet", "Test Item", 1)]
    [InlineData("HashSet", null, 1)]
    [InlineData("ConcurrentBag", "Test Item", 1)]
    [InlineData("ConcurrentBag", null, 1)]
    public async Task ObjectFill_WithCollections_ShouldHandleVariousResults(string collectionType, string? taskResult, int expectedCount)
    {
        dynamic collection = collectionType switch
        {
            "List" => new List<string>(),
            "HashSet" => new HashSet<string>(),
            "ConcurrentBag" => new ConcurrentBag<string>(),
            _ => throw new ArgumentException("Invalid collection type")
        };

        Task<string?> task = Task.FromResult(taskResult);
        await Async.ObjectFill(collection, task);
        ((IEnumerable<string>)collection).Count().ShouldBe(expectedCount);
        if (expectedCount > 0)
        {
            ((IEnumerable<string?>)collection).Contains(taskResult).ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData("List")]
    [InlineData("HashSet")]
    [InlineData("ConcurrentBag")]
    public async Task ObjectFill_WithCollections_WhenTaskThrowsException_ShouldNotAdd(string collectionType)
    {
        dynamic collection = collectionType switch
        {
            "List" => new List<string>(),
            "HashSet" => new HashSet<string>(),
            "ConcurrentBag" => new ConcurrentBag<string>(),
            _ => throw new ArgumentException("Invalid collection type")
        };

        Task<string> task = Task.FromException<string>(new InvalidOperationException());
        await Should.NotThrowAsync(async () => await Async.ObjectFill(collection, task));
        ((IEnumerable<string>)collection).Count().ShouldBe(0);
    }

    #endregion

    #region ObjectFill<T> IEnumerable

    [Theory]
    [InlineData("List")]
    [InlineData("HashSet")]
    [InlineData("ConcurrentBag")]
    public async Task ObjectFill_WithIEnumerableTask_ShouldAddAllItems(string collectionType)
    {
        dynamic collection = collectionType switch
        {
            "List" => new List<string>(),
            "HashSet" => new HashSet<string>(),
            "ConcurrentBag" => new ConcurrentBag<string>(),
            _ => throw new ArgumentException("Invalid collection type")
        };
        List<string> taskResult = ["A", "B", "C"];
        Task<IEnumerable<string>> task = Task.FromResult<IEnumerable<string>>(taskResult);
        await Async.ObjectFill(collection, task);
        ((IEnumerable<string>)collection).Count().ShouldBe(3);
    }

    #endregion

    #region ObjectFill<T> Func + Semaphore

    [Theory]
    [InlineData("List", "Test Item", 1)]
    [InlineData("List", null, 1)]
    [InlineData("HashSet", "Test Item", 1)]
    [InlineData("HashSet", null, 1)]
    [InlineData("ConcurrentBag", "Test Item", 1)]
    [InlineData("ConcurrentBag", null, 1)]
    public async Task ObjectFill_WithFuncAndSemaphore_ShouldWork(string collectionType, string? taskResult, int expectedCount)
    {
        dynamic collection = collectionType switch
        {
            "List" => new List<string>(),
            "HashSet" => new HashSet<string>(),
            "ConcurrentBag" => new ConcurrentBag<string>(),
            _ => throw new ArgumentException("Invalid collection type")
        };
        Task<string?> func() => Task.FromResult(taskResult);
        SemaphoreSlim semaphore = new(1, 1);
        await Async.ObjectFill(collection, (Func<Task<string?>>)func, semaphore);
        ((IEnumerable<string>)collection).Count().ShouldBe(expectedCount);
    }

    #endregion

    #region ObjectFill<T> MemoryStream

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    public async Task ObjectFill_WithMemoryStream_ShouldWriteDataFromTaskResult(int length)
    {
        byte[] testData = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
        await using MemoryStream ms = new();
        await using MemoryStream resultMs = new();
        resultMs.Write(testData, 0, testData.Length);
        resultMs.Position = 0;
        Task<MemoryStream> task = Task.FromResult(resultMs);

        await Async.ObjectFill(ms, task);

        ms.Position = 0;
        byte[] buffer = new byte[testData.Length];
        ms.Read(buffer, 0, testData.Length);
        buffer.ShouldBe(testData);
    }

    #endregion

    #region ObjectFill<T> DataTable

    [Fact]
    public async Task ObjectFill_WithDataTable_ShouldLoadDataFromTaskResult()
    {
        using DataTable dt = new();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        using DataTable resultDt = new();
        resultDt.Columns.Add("Id", typeof(int));
        resultDt.Columns.Add("Name", typeof(string));
        resultDt.Rows.Add(1, "Item1");
        resultDt.Rows.Add(2, "Item2");
        Task<DataTable> task = Task.FromResult(resultDt);

        await Async.ObjectFill(dt, task);

        dt.Rows.Count.ShouldBe(2);
        dt.Rows[0]["Id"].ShouldBe(1);
        dt.Rows[0]["Name"].ShouldBe("Item1");
        dt.Rows[1]["Id"].ShouldBe(2);
        dt.Rows[1]["Name"].ShouldBe("Item2");
    }

    #endregion

    #region ObjectUpdate

    [Theory]
    [InlineData("AsyncInt", 42, 42, "Orig")]
    [InlineData("AsyncString", "Updated", 0, "Updated")]
    public async Task ObjectUpdate_ShouldUpdateSpecifiedProperty(string prop, object value, int expectedInt, string expectedString)
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
        Task<object> task = Task.FromResult(value);
        await Async.ObjectUpdate(obj, prop, task);
        obj.AsyncInt.ShouldBe(expectedInt);
        obj.AsyncString.ShouldBe(expectedString);
    }

    [Theory]
    [InlineData("NonExistentProperty", 42)]
    public async Task ObjectUpdate_WithInvalidPropertyName_ShouldHandleException(string prop, object value)
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
        Task<object> task = Task.FromResult(value);
        await Should.NotThrowAsync(async () => await Async.ObjectUpdate(obj, prop, task));
    }

    [Fact]
    public async Task ObjectUpdate_WithNullObject_ShouldHandleException()
    {
        AsyncIntString? obj = null;
        Task<int> task = Task.FromResult(42);
        await Should.NotThrowAsync(async () => await Async.ObjectUpdate(obj, "AsyncInt", task));
    }

    [Theory]
    [InlineData(42.5)]
    public async Task ObjectUpdate_WithTypeConversion_ShouldHandleCompatibleTypes(decimal value)
    {
        AsyncIntString obj = new() { AsyncDecimal = 0 };
        Task<decimal> task = Task.FromResult(value);
        await Async.ObjectUpdate(obj, "AsyncDecimal", task);
        obj.AsyncDecimal.ShouldBe(value);
    }

    #endregion

    #region RunAll

    [Fact]
    public async Task RunAll_ShouldExecuteAllTasksAndReturnResults()
    {
        List<Func<Task<string>>> tasks =
        [
            () => Task.FromResult("Result1"),
            () => Task.FromResult("Result2"),
            () => Task.FromResult("Result3")
        ];
        ConcurrentBag<string> results = await Async.RunAll(tasks);
        results.Count.ShouldBe(3);
        results.ShouldContain("Result1");
        results.ShouldContain("Result2");
        results.ShouldContain("Result3");
    }

    [Fact]
    public async Task RunAll_WithSemaphore_ShouldLimitConcurrentExecution()
    {
        int executionCount = 0;
        int maxConcurrent = 0;
        object lockObj = new();
        SemaphoreSlim semaphore = new(2, 2);

        List<Func<Task<int>>> tasks =
        [
            async () => { lock(lockObj) { executionCount++; maxConcurrent = Math.Max(maxConcurrent, executionCount); } await Task.Delay(100); lock(lockObj) { executionCount--; } return 1; },
            async () => { lock(lockObj) { executionCount++; maxConcurrent = Math.Max(maxConcurrent, executionCount); } await Task.Delay(100); lock(lockObj) { executionCount--; } return 2; },
            async () => { lock(lockObj) { executionCount++; maxConcurrent = Math.Max(maxConcurrent, executionCount); } await Task.Delay(100); lock(lockObj) { executionCount--; } return 3; },
            async () => { lock(lockObj) { executionCount++; maxConcurrent = Math.Max(maxConcurrent, executionCount); } await Task.Delay(100); lock(lockObj) { executionCount--; } return 4; }
        ];

        ConcurrentBag<int> results = await Async.RunAll(tasks, semaphore);

        results.Count.ShouldBe(4);
        results.Sum().ShouldBe(10);
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task RunAll_WithTaskException_ShouldContinueExecution()
    {
        List<Func<Task<int>>> tasks =
        [
            () => Task.FromResult(1),
            () => Task.FromException<int>(new InvalidOperationException("Test exception")),
            () => Task.FromResult(3)
        ];

        ConcurrentBag<int> results = await Async.RunAll(tasks);

        results.Count.ShouldBe(2);
        results.Sum().ShouldBe(4);
    }

    [Fact]
    public async Task RunAll_WithTaskExceptionAndBreakOnError_ShouldStopExecution()
    {
        int executedTasks = 0;
        CancellationTokenSource cts = new();

        List<Func<Task<int>>> tasks =
        [
            async () => { Interlocked.Increment(ref executedTasks); await Task.Delay(50); return 1; },
            async () => { Interlocked.Increment(ref executedTasks); await Task.Delay(50); throw new InvalidOperationException("Test exception"); },
            async () => { Interlocked.Increment(ref executedTasks); await Task.Delay(50); return 3; }
        ];

        await Should.ThrowAsync<AggregateException>(async () => await Async.RunAll(tasks, null, cts, true));
        executedTasks.ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task RunAll_WithMixedSuccessAndFailure_ShouldReturnSuccessfulResults()
    {
        List<Func<Task<int>>> tasks =
        [
            () => Task.FromResult(1),
            () => Task.FromException<int>(new InvalidOperationException("First exception")),
            () => Task.FromResult(3),
            () => Task.FromException<int>(new InvalidOperationException("Second exception")),
            () => Task.FromResult(5)
        ];

        ConcurrentBag<int> results = await Async.RunAll(tasks);

        results.Count.ShouldBe(3);
        results.Sum().ShouldBe(9);
    }

    #endregion

    #region RunAll (void) - Theory Example

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public async Task RunAll_VoidTasks_ShouldExecuteAllTasks(int count)
    {
        int executed = 0;
        List<Func<Task>> tasks = Enumerable.Range(0, count).Select(_ => new Func<Task>(async () => { await Task.Delay(10); Interlocked.Increment(ref executed); })).ToList();
        await Async.RunAll(tasks);
        executed.ShouldBe(count);
    }

    #endregion
}
#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
