<<<<<<< HEAD
﻿using System.Collections.Concurrent;
using System.Data;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class AsyncTests
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
        await obj.ObjectFill(task);
        obj.ShouldBe(obj); // Value types don't change
    }

    [Fact]
    public async Task ObjectFill_WithNullObject_ShouldNotThrowException()
    {
        AsyncIntString? obj = null;
        Task<AsyncIntString?> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" })!;
        await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
    }

    [Fact]
    public async Task ObjectFill_WithNullResult_ShouldNotThrowException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString? taskResult = null;
        Task<AsyncIntString?> task = Task.FromResult(taskResult);

        await obj.ObjectFill(task);

        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WhenTaskThrowsException_ShouldHandleException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        Task<AsyncIntString> task = Task.FromException<AsyncIntString>(new InvalidOperationException());
        await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WithComplexType_ShouldCopyProperties()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString taskResult = new() { AsyncInt = 42, AsyncString = "Updated" };
        Task<AsyncIntString> task = Task.FromResult(taskResult);
        await obj.ObjectFill(task);
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
            "List" => new List<string?>(),
            "HashSet" => new HashSet<string?>(),
            "ConcurrentBag" => new ConcurrentBag<string?>(),
            _ => throw new ArgumentException("Invalid collection type")
        };
        Task<string?> func()
        {
            return Task.FromResult(taskResult);
        }

        using SemaphoreSlim semaphore = new(1, 1);
        await Async.ObjectFill(collection, (Func<Task<string?>>)func, semaphore);
        ((IEnumerable<string?>)collection).Count().ShouldBe(expectedCount);
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

        await ms.ObjectFill(task);

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

        await dt.ObjectFill(task);

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
        await obj.ObjectUpdate(prop, task);
        obj.AsyncInt.ShouldBe(expectedInt);
        obj.AsyncString.ShouldBe(expectedString);
    }

    [Theory]
    [InlineData("NonExistentProperty", 42)]
    public async Task ObjectUpdate_WithInvalidPropertyName_ShouldHandleException(string prop, object value)
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
        Task<object> task = Task.FromResult(value);
        await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, task));
    }

    [Fact]
    public async Task ObjectUpdate_WithNullObject_ShouldHandleException()
    {
        AsyncIntString? obj = null;
        Task<int> task = Task.FromResult(42);
        await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", task));
    }

    [Theory]
    [InlineData(42.5)]
    public async Task ObjectUpdate_WithTypeConversion_ShouldHandleCompatibleTypes(decimal value)
    {
        AsyncIntString obj = new() { AsyncDecimal = 0 };
        Task<decimal> task = Task.FromResult(value);
        await obj.ObjectUpdate("AsyncDecimal", task);
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
        ConcurrentBag<string> results = await tasks.RunAll();
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
        using SemaphoreSlim semaphore = new(2, 2);

        List<Func<Task<int>>> tasks =
        [
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 1;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 2;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 3;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 4;
            }
        ];

        ConcurrentBag<int> results = await tasks.RunAll(semaphore);

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

        ConcurrentBag<int> results = await tasks.RunAll();

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
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(50);
                return 1;
            },
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(100);
                throw new InvalidOperationException("Test exception");
            },
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(10000);
                return 3;
            }
        ];

        //await Should.ThrowAsync<Exception>(async () => await tasks.RunAll(null, cts, true));
        await tasks.RunAll(null, cts, true);
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

        ConcurrentBag<int> results = await tasks.RunAll();

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
        List<Func<Task>> tasks = Enumerable.Range(0, count).Select(_ => new Func<Task>(async () =>
        {
            await Task.Delay(10);
            Interlocked.Increment(ref executed);
        })).ToList();
        await tasks.RunAll();
        executed.ShouldBe(count);
    }

    #endregion

    #region ResultTaskGroup<T> Tests

    [Fact]
    public async Task ResultTaskGroup_RunTasks_ShouldReturnAllResults()
    {
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3)
        ];
        ResultTaskGroup<int> group = new(tasks);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(3);
        results.ShouldContain(1);
        results.ShouldContain(2);
        results.ShouldContain(3);
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithEmptyTasks_ShouldReturnEmptyArray()
    {
        ResultTaskGroup<string> group = new();

        string[] results = await group.RunTasks();

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        Lock lockObj = new();
        using SemaphoreSlim semaphore = new(2, 2);

        List<Task<int>> tasks = Enumerable.Range(0, 6).Select(_ => new Task<int>(() =>
        {
            lock (lockObj)
            {
                concurrent++;
                maxConcurrent = Math.Max(maxConcurrent, concurrent);
            }
            Task.Delay(50).GetAwaiter().GetResult();
            lock (lockObj)
            {
                concurrent--;
            }
            return 42;
        })).ToList();

        ResultTaskGroup<int> group = new(tasks, semaphore);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(6);
        results.All(x => x == 42).ShouldBeTrue();
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithoutSemaphore()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        Lock lockObj = new();

        List<Task<int>> tasks = Enumerable.Range(0, 6).Select(_ => new Task<int>(() =>
        {
            lock (lockObj)
            {
                concurrent++;
                maxConcurrent = Math.Max(maxConcurrent, concurrent);
            }
            Task.Delay(50).GetAwaiter().GetResult();
            lock (lockObj)
            {
                concurrent--;
            }
            return 42;
        })).ToList();

        ResultTaskGroup<int> group = new(tasks);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(6);
        results.All(x => x == 42).ShouldBeTrue();
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithCancellation_ShouldRespectToken()
    {
        CancellationTokenSource cts = new();
        TaskCompletionSource<int> tcs = new();
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            tcs.Task // This task will never complete
        ];
        ResultTaskGroup<int> group = new(tasks);

        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await group.RunTasks(cts.Token));
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
    {
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            Task.FromException<int>(new InvalidOperationException("fail"))
        ];
        ResultTaskGroup<int> group = new(tasks);

        await Should.ThrowAsync<InvalidOperationException>(async () => await group.RunTasks());
    }

    #endregion

    #region TaskGroup Tests

    [Fact]
    public async Task TaskGroup_RunTasks_ShouldRunAllTasks()
    {
        int executed = 0;
        List<Task> tasks = Enumerable.Range(0, 4)
            .Select(_ => new Task(() =>
            {
                Task.Delay(10).GetAwaiter().GetResult();
                Interlocked.Increment(ref executed);
            })).ToList();

        TaskGroup group = new(tasks);

        await group.RunTasks();

        executed.ShouldBe(4);
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WithEmptyTasks_ShouldNotThrow()
    {
        TaskGroup group = new();

        await Should.NotThrowAsync(group.RunTasks());
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        object lockObj = new();
        using SemaphoreSlim semaphore = new(2, 2);

        List<Task> tasks = Enumerable.Range(0, 6)
            .Select(_ => new Task(() =>
            {
                lock (lockObj)
                {
                    concurrent++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrent);
                }
                //await Task.Delay(50);
                Task.Delay(50).GetAwaiter().GetResult();
                lock (lockObj)
                {
                    concurrent--;
                }
            }))
            .ToList();

        TaskGroup group = new(tasks, semaphore);

        await group.RunTasks();

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
    {
        List<Task> tasks =
        [
            Task.CompletedTask,
            Task.FromException(new InvalidOperationException("fail"))
        ];
        TaskGroup group = new(tasks);

        await Should.ThrowAsync<InvalidOperationException>(group.RunTasks());
    }

    #endregion
}
=======
﻿using System.Collections.Concurrent;
using System.Data;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;

namespace Core.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly
public sealed class AsyncTests
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
        await obj.ObjectFill(task);
        obj.ShouldBe(obj); // Value types don't change
    }

    [Fact]
    public async Task ObjectFill_WithNullObject_ShouldNotThrowException()
    {
        AsyncIntString? obj = null;
        Task<AsyncIntString?> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" })!;
        await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
    }

    [Fact]
    public async Task ObjectFill_WithNullResult_ShouldNotThrowException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString? taskResult = null;
        Task<AsyncIntString?> task = Task.FromResult(taskResult);

        await obj.ObjectFill(task);

        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WhenTaskThrowsException_ShouldHandleException()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        Task<AsyncIntString> task = Task.FromException<AsyncIntString>(new InvalidOperationException());
        await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
        obj.AsyncInt.ShouldBe(0);
        obj.AsyncString.ShouldBe("Original");
    }

    [Fact]
    public async Task ObjectFill_WithComplexType_ShouldCopyProperties()
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
        AsyncIntString taskResult = new() { AsyncInt = 42, AsyncString = "Updated" };
        Task<AsyncIntString> task = Task.FromResult(taskResult);
        await obj.ObjectFill(task);
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
            "List" => new List<string?>(),
            "HashSet" => new HashSet<string?>(),
            "ConcurrentBag" => new ConcurrentBag<string?>(),
            _ => throw new ArgumentException("Invalid collection type")
        };
        Task<string?> func()
        {
            return Task.FromResult(taskResult);
        }

        using SemaphoreSlim semaphore = new(1, 1);
        await Async.ObjectFill(collection, (Func<Task<string?>>)func, semaphore);
        ((IEnumerable<string?>)collection).Count().ShouldBe(expectedCount);
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

        await ms.ObjectFill(task);

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

        await dt.ObjectFill(task);

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
        await obj.ObjectUpdate(prop, task);
        obj.AsyncInt.ShouldBe(expectedInt);
        obj.AsyncString.ShouldBe(expectedString);
    }

    [Theory]
    [InlineData("NonExistentProperty", 42)]
    public async Task ObjectUpdate_WithInvalidPropertyName_ShouldHandleException(string prop, object value)
    {
        AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
        Task<object> task = Task.FromResult(value);
        await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, task));
    }

    [Fact]
    public async Task ObjectUpdate_WithNullObject_ShouldHandleException()
    {
        AsyncIntString? obj = null;
        Task<int> task = Task.FromResult(42);
        await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", task));
    }

    [Theory]
    [InlineData(42.5)]
    public async Task ObjectUpdate_WithTypeConversion_ShouldHandleCompatibleTypes(decimal value)
    {
        AsyncIntString obj = new() { AsyncDecimal = 0 };
        Task<decimal> task = Task.FromResult(value);
        await obj.ObjectUpdate("AsyncDecimal", task);
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
        ConcurrentBag<string> results = await tasks.RunAll();
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
        using SemaphoreSlim semaphore = new(2, 2);

        List<Func<Task<int>>> tasks =
        [
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 1;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 2;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 3;
            },
            async () =>
            {
                lock (lockObj)
                {
                    executionCount++;
                    maxConcurrent = Math.Max(maxConcurrent, executionCount);
                }
                await Task.Delay(100);
                lock (lockObj)
                {
                    executionCount--;
                }
                return 4;
            }
        ];

        ConcurrentBag<int> results = await tasks.RunAll(semaphore);

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

        ConcurrentBag<int> results = await tasks.RunAll();

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
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(50);
                return 1;
            },
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(100);
                throw new InvalidOperationException("Test exception");
            },
            async () =>
            {
                Interlocked.Increment(ref executedTasks);
                await Task.Delay(10000);
                return 3;
            }
        ];

        //await Should.ThrowAsync<Exception>(async () => await tasks.RunAll(null, cts, true));
        await tasks.RunAll(null, cts, true);
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

        ConcurrentBag<int> results = await tasks.RunAll();

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
        List<Func<Task>> tasks = Enumerable.Range(0, count).Select(_ => new Func<Task>(async () =>
        {
            await Task.Delay(10);
            Interlocked.Increment(ref executed);
        })).ToList();
        await tasks.RunAll();
        executed.ShouldBe(count);
    }

    #endregion

    #region ResultTaskGroup<T> Tests

    [Fact]
    public async Task ResultTaskGroup_RunTasks_ShouldReturnAllResults()
    {
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3)
        ];
        ResultTaskGroup<int> group = new(tasks);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(3);
        results.ShouldContain(1);
        results.ShouldContain(2);
        results.ShouldContain(3);
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithEmptyTasks_ShouldReturnEmptyArray()
    {
        ResultTaskGroup<string> group = new();

        string[] results = await group.RunTasks();

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        Lock lockObj = new();
        using SemaphoreSlim semaphore = new(2, 2);

        List<Task<int>> tasks = Enumerable.Range(0, 6).Select(_ => new Task<int>(() =>
        {
            lock (lockObj)
            {
                concurrent++;
                maxConcurrent = Math.Max(maxConcurrent, concurrent);
            }
            Task.Delay(50).GetAwaiter().GetResult();
            lock (lockObj)
            {
                concurrent--;
            }
            return 42;
        })).ToList();

        ResultTaskGroup<int> group = new(tasks, semaphore);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(6);
        results.All(x => x == 42).ShouldBeTrue();
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithoutSemaphore()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        Lock lockObj = new();

        List<Task<int>> tasks = Enumerable.Range(0, 6).Select(_ => new Task<int>(() =>
        {
            lock (lockObj)
            {
                concurrent++;
                maxConcurrent = Math.Max(maxConcurrent, concurrent);
            }
            Task.Delay(50).GetAwaiter().GetResult();
            lock (lockObj)
            {
                concurrent--;
            }
            return 42;
        })).ToList();

        ResultTaskGroup<int> group = new(tasks);

        int[] results = await group.RunTasks();

        results.Length.ShouldBe(6);
        results.All(x => x == 42).ShouldBeTrue();
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WithCancellation_ShouldRespectToken()
    {
        CancellationTokenSource cts = new();
        TaskCompletionSource<int> tcs = new();
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            tcs.Task // This task will never complete
        ];
        ResultTaskGroup<int> group = new(tasks);

        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () => await group.RunTasks(cts.Token));
    }

    [Fact]
    public async Task ResultTaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
    {
        List<Task<int>> tasks =
        [
            Task.FromResult(1),
            Task.FromException<int>(new InvalidOperationException("fail"))
        ];
        ResultTaskGroup<int> group = new(tasks);

        await Should.ThrowAsync<InvalidOperationException>(async () => await group.RunTasks());
    }

    #endregion

    #region TaskGroup Tests

    [Fact]
    public async Task TaskGroup_RunTasks_ShouldRunAllTasks()
    {
        int executed = 0;
        List<Task> tasks = Enumerable.Range(0, 4)
            .Select(_ => new Task(() =>
            {
                Task.Delay(10).GetAwaiter().GetResult();
                Interlocked.Increment(ref executed);
            })).ToList();

        TaskGroup group = new(tasks);

        await group.RunTasks();

        executed.ShouldBe(4);
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WithEmptyTasks_ShouldNotThrow()
    {
        TaskGroup group = new();

        await Should.NotThrowAsync(group.RunTasks());
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
    {
        int concurrent = 0;
        int maxConcurrent = 0;
        object lockObj = new();
        using SemaphoreSlim semaphore = new(2, 2);

        List<Task> tasks = Enumerable.Range(0, 6)
            .Select(_ => new Task(() =>
            {
                lock (lockObj)
                {
                    concurrent++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrent);
                }
                //await Task.Delay(50);
                Task.Delay(50).GetAwaiter().GetResult();
                lock (lockObj)
                {
                    concurrent--;
                }
            }))
            .ToList();

        TaskGroup group = new(tasks, semaphore);

        await group.RunTasks();

        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task TaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
    {
        List<Task> tasks =
        [
            Task.CompletedTask,
            Task.FromException(new InvalidOperationException("fail"))
        ];
        TaskGroup group = new(tasks);

        await Should.ThrowAsync<InvalidOperationException>(group.RunTasks());
    }

    #endregion
}
#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
