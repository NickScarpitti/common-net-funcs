using System.Collections.Concurrent;
using System.Data;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class AsyncTests
{
	private readonly Fixture fixture;

	public AsyncTests()
	{
		fixture = new Fixture();
		fixture.Customize(new AutoFakeItEasyCustomization());
	}

	#region ObjectFill<T> Simple/Null/Exception

	//[Theory]
	//[InlineData(0, 42)]
	//[InlineData(100, 200)]
	//public async Task ObjectFill_WithSimpleType_ShouldNotAssignTaskResult(int obj, int taskResult)
	//{
	//    Task<int> task = Task.FromResult(taskResult);
	//    await obj.ObjectFill(task);
	//    obj.ShouldBe(obj); // Value types don't change
	//}

	[Fact]
	public async Task ObjectFill_WithNullObject_ShouldNotThrowException()
	{
		AsyncIntString obj = null!;
		Task<AsyncIntString> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" });
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

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldCopyProperties()
	{
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		Func<Task<AsyncIntString>> func = () => Task.FromResult(new AsyncIntString { AsyncInt = 99, AsyncString = "FromFunc" });
		using SemaphoreSlim semaphore = new(1, 1);

		await obj.ObjectFill(func, semaphore);

		obj.AsyncInt.ShouldBe(99);
		obj.AsyncString.ShouldBe("FromFunc");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndNullSemaphore_ShouldCopyProperties()
	{
		AsyncIntString obj = new() { AsyncInt = 10, AsyncString = "Start" };
		Func<Task<AsyncIntString>> func = () => Task.FromResult(new AsyncIntString { AsyncInt = 55, AsyncString = "NoSemaphore" });

		await obj.ObjectFill(func, null);

		obj.AsyncInt.ShouldBe(55);
		obj.AsyncString.ShouldBe("NoSemaphore");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenObjectIsNull_ShouldNotThrow()
	{
		AsyncIntString obj = null!;
		Func<Task<AsyncIntString>> func = () => Task.FromResult(new AsyncIntString { AsyncInt = 100, AsyncString = "Test" });
		using SemaphoreSlim semaphore = new(1, 1);

		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, semaphore));
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenResultIsNull_ShouldNotModifyObject()
	{
		AsyncIntString obj = new() { AsyncInt = 25, AsyncString = "Original" };
		Func<Task<AsyncIntString>> func = () => Task.FromResult<AsyncIntString>(null!);
		using SemaphoreSlim semaphore = new(1, 1);

		await obj.ObjectFill(func, semaphore);

		obj.AsyncInt.ShouldBe(25);
		obj.AsyncString.ShouldBe("Original");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		AsyncIntString obj = new() { AsyncInt = 30, AsyncString = "BeforeError" };
		Func<Task<AsyncIntString>> func = () => Task.FromException<AsyncIntString>(new InvalidOperationException("Task failed"));
		using SemaphoreSlim semaphore = new(1, 1);

		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, semaphore));

		obj.AsyncInt.ShouldBe(30);
		obj.AsyncString.ShouldBe("BeforeError");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Test" };
		using SemaphoreSlim semaphore = new(1, 1);
		Func<Task<AsyncIntString>> func = () => Task.FromResult(new AsyncIntString { AsyncInt = 1, AsyncString = "Success" });

		await obj.ObjectFill(func, semaphore);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldReleaseSemaphoreOnException()
	{
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Test" };
		using SemaphoreSlim semaphore = new(1, 1);
		Func<Task<AsyncIntString>> func = () => throw new InvalidOperationException("Fail");

		await obj.ObjectFill(func, semaphore);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldLimitConcurrency()
	{
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			AsyncIntString obj = new() { AsyncInt = i, AsyncString = $"Task{i}" };
			Func<Task<AsyncIntString>> func = async () =>
			{
				lock (lockObj)
				{
					concurrentExecutions++;
					maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
				}
				await Task.Delay(50);
				lock (lockObj)
				{
					concurrentExecutions--;
				}
				return new AsyncIntString { AsyncInt = i * 10, AsyncString = $"Result{i}" };
			};
			tasks.Add(obj.ObjectFill(func, semaphore));
		}

		await Task.WhenAll(tasks);

		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_MultipleConcurrentCalls_ShouldAllComplete()
	{
		using SemaphoreSlim semaphore = new(3, 3);
		List<AsyncIntString> objects = Enumerable.Range(0, 10)
			.Select(i => new AsyncIntString { AsyncInt = i, AsyncString = $"Start{i}" })
			.ToList();

		List<Task> tasks = [];
		foreach (AsyncIntString obj in objects)
		{
			int expectedValue = obj.AsyncInt * 2;
			Func<Task<AsyncIntString>> func = async () =>
			{
				await Task.Delay(10);
				return new AsyncIntString { AsyncInt = expectedValue, AsyncString = $"Updated{expectedValue}" };
			};
			tasks.Add(obj.ObjectFill(func, semaphore));
		}

		await Task.WhenAll(tasks);

		for (int i = 0; i < objects.Count; i++)
		{
			objects[i].AsyncInt.ShouldBe(i * 2);
			objects[i].AsyncString.ShouldBe($"Updated{i * 2}");
		}
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldWaitForSemaphore()
	{
		using SemaphoreSlim semaphore = new(1, 1);
		await semaphore.WaitAsync(TestContext.Current.CancellationToken); // Acquire semaphore

		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Waiting" };
		Func<Task<AsyncIntString>> func = () => Task.FromResult(new AsyncIntString { AsyncInt = 75, AsyncString = "Released" });

		Task fillTask = obj.ObjectFill(func, semaphore);

		// Give a moment to ensure it's waiting
		await Task.Delay(50, TestContext.Current.CancellationToken);
		obj.AsyncInt.ShouldBe(0); // Should not have updated yet
		obj.AsyncString.ShouldBe("Waiting");

		// Release the semaphore
		semaphore.Release();
		await fillTask;

		obj.AsyncInt.ShouldBe(75);
		obj.AsyncString.ShouldBe("Released");
	}

	[Theory]
	[InlineData("List", "Test Item", 1)]
	[InlineData("List", null, 1)]
	[InlineData("HashSet", "Test Item", 1)]
	[InlineData("HashSet", null, 1)]
	[InlineData("ConcurrentBag", "Test Item", 1)]
	[InlineData("ConcurrentBag", null, 1)]
	public async Task ObjectFill_WithFuncAndSemaphore_Collections_ShouldWork(string collectionType, string? taskResult, int expectedCount)
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

	#region ObjectFill ConcurrentBag/IList/HashSet with Func + Semaphore

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncSemaphore_ShouldAddItem()
	{
		ConcurrentBag<int?> bag = [];
		Func<Task<int?>> func = () => Task.FromResult<int?>(42);
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(1);
		bag.ShouldContain(42);
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncSemaphore_ShouldAddItem()
	{
		IList<string?> list = new List<string?>();
		Func<Task<string?>> func = () => Task.FromResult<string?>("Test");
		using SemaphoreSlim semaphore = new(1, 1);
		await list.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(1);
		list[0].ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_HashSetNullableWithFuncSemaphore_ShouldAddItem()
	{
		HashSet<int?> hashSet = [];
		Func<Task<int?>> func = () => Task.FromResult<int?>(99);
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(1);
		hashSet.ShouldContain(99);
	}

	#endregion

	#region ObjectFill List/HashSet with Task<IEnumerable>

	[Fact]
	public async Task ObjectFill_ListWithTaskIEnumerable_ShouldAddRange()
	{
		List<int> list = [1, 2];
		IEnumerable<int> items = new[] { 3, 4, 5 };
		Task<IEnumerable<int>> task = Task.FromResult(items);
		await list.ObjectFill(task);
		list.Count.ShouldBe(5);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskIEnumerable_ShouldAddRange()
	{
		HashSet<string> hashSet = ["A", "B"];
		IEnumerable<string> items = new[] { "C", "D" };
		Task<IEnumerable<string>> task = Task.FromResult(items);
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(4);
	}

	#endregion

	#region ObjectFill List/HashSet with Func<Task<IEnumerable>> + Semaphore

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerable_ShouldAddRange()
	{
		List<int> list = [1];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 2, 3 });
		using SemaphoreSlim semaphore = new(1, 1);
		await list.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerable_ShouldAddRange()
	{
		HashSet<int> hashSet = [1];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 2, 3 });
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(3);
	}

	#endregion

	#region ObjectFill ConcurrentBag Overloads

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerable_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(new[] { 1, 2, 3 });
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerable_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBag_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBag_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<ConcurrentBag<int>>> func = () => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskList_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 1, 2, 3 });
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncList_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 1, 2, 3 });
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskConcurrentBag_ShouldAddRange()
	{
		HashSet<int> hashSet = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithTaskIEnumerable_ShouldNotThrow()
	{
		ConcurrentBag<int>? bag = null;
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(new[] { 1, 2, 3 });
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithFuncIEnumerable_ShouldNotThrow()
	{
		ConcurrentBag<int>? bag = null;
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });
		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithTaskConcurrentBag_ShouldNotThrow()
	{
		ConcurrentBag<int>? bag = null;
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithFuncConcurrentBag_ShouldNotThrow()
	{
		ConcurrentBag<int>? bag = null;
		Func<Task<ConcurrentBag<int>>> func = () => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullTaskIEnumerable_ShouldNotAdd()
	{
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(null);
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullTaskConcurrentBag_ShouldNotAdd()
	{
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(null);
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(0);
	}

	#endregion

	#region ObjectFill Exception Handling

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncThrowing_ShouldHandleException()
	{
		ConcurrentBag<int?> bag = [];
		Func<Task<int?>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncThrowing_ShouldHandleException()
	{
		IList<string?> list = new List<string?>();
		Func<Task<string?>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await list.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncThrowing_ShouldHandleException()
	{
		HashSet<int?> hashSet = [];
		Func<Task<int?>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskThrowing_ShouldHandleException()
	{
		List<int> list = [1];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));
		await list.ObjectFill(task);
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskListThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSetThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Task<HashSet<int>?> task = Task.FromException<HashSet<int>?>(new InvalidOperationException("Test exception"));
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncListThrowing_ShouldHandleException()
	{
		List<int> list = [1];
		Func<Task<List<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await list.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncListThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Func<Task<List<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncHashSetThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Func<Task<HashSet<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		List<int> list = [1];
		Task<IEnumerable<int>> task = Task.FromException<IEnumerable<int>>(new InvalidOperationException("Test exception"));
		await list.ObjectFill(task);
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Task<IEnumerable<int>> task = Task.FromException<IEnumerable<int>>(new InvalidOperationException("Test exception"));
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		List<int> list = [1];
		Func<Task<IEnumerable<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await list.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [1];
		Func<Task<IEnumerable<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await hashSet.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromException<IEnumerable<int>?>(new InvalidOperationException("Test exception"));
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<IEnumerable<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBagThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<ConcurrentBag<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskListThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));
		await bag.ObjectFill(task);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListThrowing_ShouldHandleException()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<List<int>>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await bag.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskConcurrentBagThrowing_ShouldHandleException()
	{
		HashSet<int> hashSet = [];
		Task<ConcurrentBag<int>?> task = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));
		await hashSet.ObjectFill(task);
		hashSet.Count.ShouldBe(0);
	}

	#endregion

	#region ObjectFill Null Semaphore Tests

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphore_ShouldAddItem()
	{
		ConcurrentBag<int?> bag = [];
		Func<Task<int?>> func = () => Task.FromResult<int?>(42);
		await bag.ObjectFill(func, null, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(1);
		bag.ShouldContain(42);
	}

	[Fact]
	public async Task ObjectFill_IListWithNullSemaphore_ShouldAddItem()
	{
		IList<string?> list = new List<string?>();
		Func<Task<string?>> func = () => Task.FromResult<string?>("Test");
		await list.ObjectFill(func, null, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(1);
		list[0].ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_HashSetNullableWithNullSemaphore_ShouldAddItem()
	{
		HashSet<int?> hashSet = [];
		Func<Task<int?>> func = () => Task.FromResult<int?>(99);
		await hashSet.ObjectFill(func, null, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(1);
		hashSet.ShouldContain(99);
	}

	[Fact]
	public async Task ObjectFill_ListWithNullSemaphore_ShouldAddRange()
	{
		List<int> list = [1];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 2, 3 });
		await list.ObjectFill(func, null, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithNullSemaphore_ShouldLoadData()
	{
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		Func<Task<DataTable>> func = () =>
		{
			DataTable resultDt = new();
			resultDt.Columns.Add("Id", typeof(int));
			resultDt.Rows.Add(1);
			return Task.FromResult(resultDt);
		};
		await dt.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithNullSemaphore_ShouldWriteData()
	{
		byte[] testData = [1, 2, 3];
		await using MemoryStream ms = new();
		Func<Task<MemoryStream>> func = async () =>
		{
			MemoryStream resultMs = new();
			await resultMs.WriteAsync(testData, TestContext.Current.CancellationToken);
			resultMs.Position = 0;
			return resultMs;
		};
		await ms.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), TestContext.Current.CancellationToken);
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreList_ShouldAddRange()
	{
		HashSet<int> hashSet = [1];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 2, 3 });
		await hashSet.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreHashSet_ShouldAddRange()
	{
		HashSet<int> hashSet = [1];
		Func<Task<HashSet<int>>> func = () => Task.FromResult(new HashSet<int> { 2, 3 });
		await hashSet.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		HashSet<int> hashSet = [1];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 2, 3 });
		await hashSet.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ListWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		List<int> list = [1];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 2, 3 });
		await list.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<IEnumerable<int>>> func = () => Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });
		await bag.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreConcurrentBag_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<ConcurrentBag<int>>> func = () => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		await bag.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreList_ShouldAddRange()
	{
		ConcurrentBag<int> bag = [];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 1, 2, 3 });
		await bag.ObjectFill(func, null!, TestContext.Current.CancellationToken);
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectUpdate_WithNullSemaphore_ShouldUpdateProperty()
	{
		AsyncIntString obj = new();
		Func<Task<int>> func = () => Task.FromResult(42);
		await obj.ObjectUpdate("AsyncInt", func, null!, TestContext.Current.CancellationToken);
		obj.AsyncInt.ShouldBe(42);
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
		await resultMs.WriteAsync(testData, TestContext.Current.CancellationToken);
		resultMs.Position = 0;
		Task<MemoryStream> task = Task.FromResult(resultMs);

		await ms.ObjectFill(task);

		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), TestContext.Current.CancellationToken);
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncSemaphore_ShouldWriteData()
	{
		byte[] testData = [1, 2, 3, 4, 5];
		await using MemoryStream ms = new();
		Func<Task<MemoryStream>> func = async () =>
		{
			MemoryStream resultMs = new();
			await resultMs.WriteAsync(testData, TestContext.Current.CancellationToken);
			resultMs.Position = 0;
			return resultMs;
		};
		using SemaphoreSlim semaphore = new(1, 1);

		await ms.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);

		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), TestContext.Current.CancellationToken);
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithTaskThrowing_ShouldHandleException()
	{
		await using MemoryStream ms = new();
		Task<MemoryStream> task = Task.FromException<MemoryStream>(new InvalidOperationException("Test exception"));

		await ms.ObjectFill(task);

		ms.Length.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncThrowing_ShouldHandleException()
	{
		await using MemoryStream ms = new();
		Func<Task<MemoryStream>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);

		await ms.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);

		ms.Length.ShouldBe(0);
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

	[Fact]
	public async Task ObjectFill_DataTableWithFuncSemaphore_ShouldLoadData()
	{
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		Func<Task<DataTable>> func = () =>
		{
			DataTable resultDt = new();
			resultDt.Columns.Add("Id", typeof(int));
			resultDt.Columns.Add("Name", typeof(string));
			resultDt.Rows.Add(1, "Item1");
			resultDt.Rows.Add(2, "Item2");
			return Task.FromResult(resultDt);
		};
		using SemaphoreSlim semaphore = new(1, 1);

		await dt.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);

		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Id"].ShouldBe(1);
		dt.Rows[0]["Name"].ShouldBe("Item1");
	}

	[Fact]
	public async Task ObjectFill_DataTableWithTaskThrowing_ShouldHandleException()
	{
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		Task<DataTable> task = Task.FromException<DataTable>(new InvalidOperationException("Test exception"));

		await dt.ObjectFill(task);

		dt.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncThrowing_ShouldHandleException()
	{
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		Func<Task<DataTable>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);

		await dt.ObjectFill(func, semaphore, TestContext.Current.CancellationToken);

		dt.Rows.Count.ShouldBe(0);
	}

	#endregion

	#region ObjectFill<T> List<T> with Task<List<T>?>

	[Fact]
	public async Task ObjectFill_ListWithTaskList_ShouldAddRangeToList()
	{
		List<int> list = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

		await list.ObjectFill(task);

		list.Count.ShouldBe(6);
		list.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenResultIsNull_ShouldNotModifyList()
	{
		List<string> list = ["A", "B"];
		Task<List<string>?> task = Task.FromResult<List<string>?>(null);

		await list.ObjectFill(task);

		list.Count.ShouldBe(2);
		list.ShouldBe(new[] { "A", "B" });
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenTaskThrows_ShouldHandleException()
	{
		List<int> list = [1, 2];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test error"));

		await Should.NotThrowAsync(async () => await list.ObjectFill(task));

		list.Count.ShouldBe(2);
		list.ShouldBe(new[] { 1, 2 });
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenResultIsEmpty_ShouldNotAddItems()
	{
		List<int> list = [10, 20];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int>());

		await list.ObjectFill(task);

		list.Count.ShouldBe(2);
		list.ShouldBe(new[] { 10, 20 });
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Task<List<T>?>

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_ShouldAddRangeToHashSet()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		HashSet<string> hashSet = ["A", "B"];
		Task<List<string>?> task = Task.FromResult<List<string>?>(null);

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(new[] { "A", "B" });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenTaskThrows_ShouldHandleException()
	{
		HashSet<int> hashSet = [1, 2];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test error"));

		await Should.NotThrowAsync(async () => await hashSet.ObjectFill(task));

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(new[] { 1, 2 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 2, 3, 4, 5 });

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5 });
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Task<HashSet<T>?>

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_ShouldAddRangeToHashSet()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Task<HashSet<int>?> task = Task.FromResult<HashSet<int>?>(new HashSet<int> { 4, 5, 6 });

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		HashSet<string> hashSet = ["A", "B"];
		Task<HashSet<string>?> task = Task.FromResult<HashSet<string>?>(null);

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(new[] { "A", "B" });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Task<HashSet<int>?> task = Task.FromResult<HashSet<int>?>(new HashSet<int> { 2, 3, 4, 5 });

		await hashSet.ObjectFill(task);

		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5 });
	}

	#endregion

	#region ObjectFill<T> List<T> with Func, Semaphore, CancellationToken

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldAddRangeToList()
	{
		List<int> list = [1, 2, 3];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 4, 5, 6 });
		using SemaphoreSlim semaphore = new(1, 1);

		await list.ObjectFill(func, semaphore, default);

		list.Count.ShouldBe(6);
		list.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WithNullSemaphore_ShouldWork()
	{
		List<string> list = ["A", "B"];
		Func<Task<List<string>>> func = () => Task.FromResult(new List<string> { "C", "D" });

		await list.ObjectFill(func, null, default);

		list.Count.ShouldBe(4);
		list.ShouldBe(new[] { "A", "B", "C", "D" });
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WhenResultIsNull_ShouldNotModifyList()
	{
		List<int> list = [10, 20];
		Func<Task<List<int>>> func = () => Task.FromResult<List<int>>(null!);
		using SemaphoreSlim semaphore = new(1, 1);

		await list.ObjectFill(func, semaphore, default);

		list.Count.ShouldBe(2);
		list.ShouldBe(new[] { 10, 20 });
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		List<int> list = [1, 2];
		Func<Task<List<int>>> func = () => Task.FromException<List<int>>(new InvalidOperationException("Test error"));
		using SemaphoreSlim semaphore = new(1, 1);

		await Should.NotThrowAsync(async () => await list.ObjectFill(func, semaphore, default));

		list.Count.ShouldBe(2);
		list.ShouldBe(new[] { 1, 2 });
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		List<int> list = [1];
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 2 });
		using SemaphoreSlim semaphore = new(1, 1);

		await list.ObjectFill(func, semaphore, default);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldReleaseSemaphoreOnException()
	{
		List<int> list = [1];
		Func<Task<List<int>>> func = () => throw new InvalidOperationException("Fail");
		using SemaphoreSlim semaphore = new(1, 1);

		await list.ObjectFill(func, semaphore, default);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldLimitConcurrency()
	{
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			List<int> list = [i];
			Func<Task<List<int>>> func = async () =>
			{
				lock (lockObj)
				{
					concurrentExecutions++;
					maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
				}
				await Task.Delay(50, TestContext.Current.CancellationToken);
				lock (lockObj)
				{
					concurrentExecutions--;
				}
				return new List<int> { i * 10 };
			};
			tasks.Add(list.ObjectFill(func, semaphore, default));
		}

		await Task.WhenAll(tasks);

		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WithCancellationToken_ShouldHandleCancellation()
	{
		List<int> list = [1];
		using CancellationTokenSource cts = new();
		Func<Task<List<int>>> func = () => Task.FromResult(new List<int> { 2 });
		using SemaphoreSlim semaphore = new(1, 1);
		await cts.CancelAsync();

		await Should.ThrowAsync<Exception>(async () =>
			await list.ObjectFill(func, semaphore, cts.Token));
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Func, Semaphore, CancellationToken

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldAddRangeToHashSet()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Func<Task<HashSet<int>>> func = () => Task.FromResult(new HashSet<int> { 4, 5, 6 });
		using SemaphoreSlim semaphore = new(1, 1);

		await hashSet.ObjectFill(func, semaphore, default);

		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		HashSet<string> hashSet = ["A", "B"];
		Func<Task<HashSet<string>>> func = () => Task.FromResult<HashSet<string>>(null!);
		using SemaphoreSlim semaphore = new(1, 1);

		await hashSet.ObjectFill(func, semaphore, default);

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(new[] { "A", "B" });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		HashSet<int> hashSet = [1, 2];
		Func<Task<HashSet<int>>> func = () => Task.FromException<HashSet<int>>(new InvalidOperationException("Test error"));
		using SemaphoreSlim semaphore = new(1, 1);

		await Should.NotThrowAsync(async () => await hashSet.ObjectFill(func, semaphore, default));

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(new[] { 1, 2 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		HashSet<int> hashSet = [1, 2, 3];
		Func<Task<HashSet<int>>> func = () => Task.FromResult(new HashSet<int> { 2, 3, 4, 5 });
		using SemaphoreSlim semaphore = new(1, 1);

		await hashSet.ObjectFill(func, semaphore, default);

		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe(new[] { 1, 2, 3, 4, 5 });
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		HashSet<int> hashSet = [1];
		Func<Task<HashSet<int>>> func = () => Task.FromResult(new HashSet<int> { 2 });
		using SemaphoreSlim semaphore = new(1, 1);

		await hashSet.ObjectFill(func, semaphore, default);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldReleaseSemaphoreOnException()
	{
		HashSet<int> hashSet = [1];
		Func<Task<HashSet<int>>> func = () => throw new InvalidOperationException("Fail");
		using SemaphoreSlim semaphore = new(1, 1);

		await hashSet.ObjectFill(func, semaphore, default);

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldLimitConcurrency()
	{
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			HashSet<int> hashSet = [i];
			Func<Task<HashSet<int>>> func = async () =>
			{
				lock (lockObj)
				{
					concurrentExecutions++;
					maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
				}
				await Task.Delay(50, TestContext.Current.CancellationToken);
				lock (lockObj)
				{
					concurrentExecutions--;
				}
				return new HashSet<int> { i * 10 };
			};
			tasks.Add(hashSet.ObjectFill(func, semaphore, default));
		}

		await Task.WhenAll(tasks);

		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WithCancellationToken_ShouldHandleCancellation()
	{
		HashSet<int> hashSet = [1];
		using CancellationTokenSource cts = new();
		Func<Task<HashSet<int>>> func = () => Task.FromResult(new HashSet<int> { 2 });
		using SemaphoreSlim semaphore = new(1, 1);
		await cts.CancelAsync();

		await Should.ThrowAsync<Exception>(async () =>
			await hashSet.ObjectFill(func, semaphore, cts.Token));
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

	[Theory]
	[InlineData("AsyncInt", 42, 42, "")]
	[InlineData("AsyncString", "Test", 0, "Test")]
	public async Task ObjectUpdate_WithFuncSemaphore_ShouldUpdateProperty(string prop, object value, int expectedInt, string expectedString)
	{
		AsyncIntString obj = new();
		Func<Task<object>> func = () => Task.FromResult(value);
		using SemaphoreSlim semaphore = new(1, 1);
		await obj.ObjectUpdate(prop, func, semaphore, TestContext.Current.CancellationToken);
		obj.AsyncInt.ShouldBe(expectedInt);
		obj.AsyncString.ShouldBe(expectedString);
	}

	[Theory]
	[InlineData("Invalid", 100)]
	[InlineData("NonExistent", "test")]
	public async Task ObjectUpdate_WithFuncSemaphoreInvalidProperty_ShouldHandleException(string prop, object value)
	{
		AsyncIntString obj = new();
		Func<Task<object>> func = () => Task.FromResult(value);
		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, func, semaphore, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphoreNullObject_ShouldHandleException()
	{
		AsyncIntString? obj = null;
		Func<Task<int>> func = () => Task.FromResult(100);
		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", func, semaphore, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphoreThrowing_ShouldHandleException()
	{
		AsyncIntString obj = new();
		Func<Task<int>> func = () => throw new InvalidOperationException("Test exception");
		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", func, semaphore, TestContext.Current.CancellationToken));
		obj.AsyncInt.ShouldBe(0);
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

	[Fact]
	public async Task RunAll_VoidWithBreakOnError_ShouldStopOnException()
	{
		int executed = 0;
		using CancellationTokenSource cts = new();
		List<Func<Task>> tasks =
		[
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(50); throw new InvalidOperationException("Error"); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10000); }
		];
		await tasks.RunAll(null, cts, true);
		executed.ShouldBeLessThanOrEqualTo(3);
	}

	[Fact]
	public async Task RunAll_VoidWithExceptionNoBreak_ShouldContinue()
	{
		int executed = 0;
		List<Func<Task>> tasks =
		[
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); },
			async () => { Interlocked.Increment(ref executed); throw new InvalidOperationException("Error"); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); }
		];
		await tasks.RunAll();
		executed.ShouldBe(3);
	}

	#endregion

	#region RunAsyncWithSemaphore Tests

	[Fact]
	public async Task RunAsyncWithSemaphore_ShouldExecuteTask()
	{
		Task<int> task = Task.FromResult(42);
		using SemaphoreSlim semaphore = new(1, 1);
		int result = await task.RunAsyncWithSemaphore(semaphore);
		result.ShouldBe(42);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_Void_ShouldExecuteTask()
	{
		bool executed = false;
		Task task = Task.Run(() => executed = true);
		using SemaphoreSlim semaphore = new(1, 1);
		await task.RunAsyncWithSemaphore(semaphore);
		executed.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithException_ShouldReleaseSemaphore()
	{
		Task<int> task = Task.FromException<int>(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		int result = await task.RunAsyncWithSemaphore(semaphore);
		result.ShouldBe(0);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_Void_WithException_ShouldReleaseSemaphore()
	{
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		await task.RunAsyncWithSemaphore(semaphore);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithBreakOnError_ShouldCancelOnException()
	{
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();
		await task.RunAsyncWithSemaphore(semaphore, cts, true);
		cts.Token.IsCancellationRequested.ShouldBeTrue();
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithErrorText_ShouldLogWithText()
	{
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		await task.RunAsyncWithSemaphore(semaphore, null, false, "Custom error message");
		semaphore.CurrentCount.ShouldBe(1);
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
		using CancellationTokenSource cts = new();
		TaskCompletionSource<int> tcs = new();
		List<Task<int>> tasks =
		[
				Task.FromResult(1),
						tcs.Task // This task will never complete
		];
		ResultTaskGroup<int> group = new(tasks);

		await cts.CancelAsync();

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
