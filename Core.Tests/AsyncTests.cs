using System.Collections.Concurrent;
using System.Data;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;
using static Xunit.TestContext;

namespace Core.Tests;

public enum ExecutionMode { Task, FuncWithSemaphore, FuncWithoutSemaphore }
public enum ExceptionScenario { NoException, TaskException, FuncException }
public enum NullScenario { NotNull, ObjectNull, ResultNull, SemaphoreNull }
public enum CollectionType { List, HashSet, ConcurrentBag }
public enum DataSourceType { IEnumerable, ConcurrentBag, List, HashSet }
public enum SemaphorePresence { WithSemaphore, WithoutSemaphore }

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

	[Theory]
	[InlineData(NullScenario.NotNull, false)]
	[InlineData(NullScenario.ResultNull, false)]
	[InlineData(NullScenario.ObjectNull, false)]
	public async Task ObjectFill_WithNullScenariosAndExceptions_ShouldHandleCorrectly(NullScenario nullScenario, bool throwException)
	{
		AsyncIntString? obj = nullScenario == NullScenario.ObjectNull ? null : new() { AsyncInt = 0, AsyncString = "Original" };
		Task<AsyncIntString?> task = nullScenario == NullScenario.ResultNull
			? Task.FromResult<AsyncIntString?>(null)
			: throwException
				? Task.FromException<AsyncIntString?>(new InvalidOperationException())
				: Task.FromResult<AsyncIntString?>(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" });

		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));

		if (nullScenario != NullScenario.ObjectNull && obj != null)
		{
			if (nullScenario == NullScenario.NotNull && !throwException)
			{
				obj.AsyncInt.ShouldBe(42);
				obj.AsyncString.ShouldBe("Updated");
			}
			else
			{
				obj.AsyncInt.ShouldBe(0);
				obj.AsyncString.ShouldBe("Original");
			}
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ObjectFill_WhenTaskThrowsException_ShouldHandleException(bool throwException)
	{
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		Task<AsyncIntString> task = throwException
			? Task.FromException<AsyncIntString>(new InvalidOperationException())
			: Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" });
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
		if (throwException)
		{
			obj.AsyncInt.ShouldBe(0);
			obj.AsyncString.ShouldBe("Original");
		}
		else
		{
			obj.AsyncInt.ShouldBe(42);
			obj.AsyncString.ShouldBe("Updated");
		}
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
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 99, AsyncString = "FromFunc" });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		await obj.ObjectFill(func, semaphore);

		obj.AsyncInt.ShouldBe(99);
		obj.AsyncString.ShouldBe("FromFunc");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndNullSemaphore_ShouldCopyProperties()
	{
		AsyncIntString obj = new() { AsyncInt = 10, AsyncString = "Start" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 55, AsyncString = "NoSemaphore" });
		}

		await obj.ObjectFill(func, null);

		obj.AsyncInt.ShouldBe(55);
		obj.AsyncString.ShouldBe("NoSemaphore");
	}

	[Theory]
	[InlineData(NullScenario.ObjectNull, false)]
	[InlineData(NullScenario.ResultNull, false)]
	[InlineData(NullScenario.NotNull, true)]
	public async Task ObjectFill_WithFuncAndSemaphore_NullAndExceptionScenarios(NullScenario nullScenario, bool throwException)
	{
		AsyncIntString? obj = nullScenario == NullScenario.ObjectNull
			? null
			: new() { AsyncInt = nullScenario == NullScenario.NotNull ? 30 : 25, AsyncString = nullScenario == NullScenario.NotNull ? "BeforeError" : "Original" };

		Task<AsyncIntString?> func()
		{
			if (throwException)
			{

				return Task.FromException<AsyncIntString?>(new InvalidOperationException("Task failed"));
			}


			if (nullScenario == NullScenario.ResultNull)
			{

				return Task.FromResult<AsyncIntString?>(null);
			}


			return Task.FromResult<AsyncIntString?>(new AsyncIntString { AsyncInt = 100, AsyncString = "Test" });
		}

		using SemaphoreSlim semaphore = new(1, 1);

#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, semaphore));
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.

		if (obj != null && nullScenario != NullScenario.ObjectNull)
		{
			if (nullScenario == NullScenario.NotNull && throwException)
			{
				obj.AsyncInt.ShouldBe(30);
				obj.AsyncString.ShouldBe("BeforeError");
			}
			else if (nullScenario == NullScenario.ResultNull)
			{
				obj.AsyncInt.ShouldBe(25);
				obj.AsyncString.ShouldBe("Original");
			}
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldReleaseSemaphore(bool throwException)
	{
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Test" };
		using SemaphoreSlim semaphore = new(1, 1);
		Task<AsyncIntString> func()
		{
			if (throwException)
			{
				throw new InvalidOperationException("Fail");
			}


			return Task.FromResult(new AsyncIntString { AsyncInt = 1, AsyncString = "Success" });
		}

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
			async Task<AsyncIntString> func()
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
			}
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
			async Task<AsyncIntString> func()
			{
				await Task.Delay(10);
				return new AsyncIntString { AsyncInt = expectedValue, AsyncString = $"Updated{expectedValue}" };
			}
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
		await semaphore.WaitAsync(Current.CancellationToken); // Acquire semaphore

		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Waiting" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 75, AsyncString = "Released" });
		}

		Task fillTask = obj.ObjectFill(func, semaphore);

		// Give a moment to ensure it's waiting
		await Task.Delay(50, Current.CancellationToken);
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

	[Theory]
	[InlineData(CollectionType.ConcurrentBag, false)]
	[InlineData(CollectionType.ConcurrentBag, true)]
	[InlineData(CollectionType.List, false)]
	[InlineData(CollectionType.List, true)]
	[InlineData(CollectionType.HashSet, false)]
	[InlineData(CollectionType.HashSet, true)]
	public async Task ObjectFill_CollectionsWithFuncSemaphore_ShouldHandleAdditions(CollectionType collectionType, bool throwException)
	{
		dynamic collection = collectionType switch
		{
			CollectionType.ConcurrentBag => new ConcurrentBag<int?>(),
			CollectionType.List => new List<int?>(),
			CollectionType.HashSet => new HashSet<int?>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		Task<int?> func()
		{
			if (throwException)
			{
				throw new InvalidOperationException("Test exception");
			}


			return Task.FromResult<int?>(99);
		}

		using SemaphoreSlim semaphore = new(1, 1);
		if (collectionType == CollectionType.List)
		{
			await ((IList<int?>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
		}
		else if (collectionType == CollectionType.HashSet)
		{
			await ((HashSet<int?>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
		}
		else
		{
			await ((ConcurrentBag<int?>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
		}

		if (throwException)
		{
			((IEnumerable<int?>)collection).Count().ShouldBe(0);
		}
		else
		{
			((IEnumerable<int?>)collection).Count().ShouldBe(1);
		}
	}

	#endregion

	#region ObjectFill List/HashSet with Task<IEnumerable>

	[Theory]
	[InlineData(CollectionType.List, ExecutionMode.Task)]
	[InlineData(CollectionType.List, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.HashSet, ExecutionMode.Task)]
	[InlineData(CollectionType.HashSet, ExecutionMode.FuncWithSemaphore)]
	public async Task ObjectFill_WithTaskIEnumerable_ShouldAddRange(CollectionType collectionType, ExecutionMode executionMode)
	{
		dynamic collection = collectionType switch
		{
			CollectionType.List => new List<int> { 1, 2 },
			CollectionType.HashSet => new HashSet<int> { 1, 2 },
			_ => throw new ArgumentException("Invalid collection type")
		};

		if (executionMode == ExecutionMode.Task)
		{
			IEnumerable<int> items = new[] { 3, 4, 5 };
			Task<IEnumerable<int>> task = Task.FromResult(items);
			await Async.ObjectFill(collection, task);
		}
		else
		{
			static Task<IEnumerable<int>> func()
			{
				return Task.FromResult<IEnumerable<int>>([3, 4, 5]);
			}
			using SemaphoreSlim semaphore = new(1, 1);
			if (collectionType == CollectionType.List)
			{
				await ((List<int>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
			}
			else
			{
				await ((HashSet<int>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
			}

		}

		((IEnumerable<int>)collection).Count().ShouldBe(5);
	}

	#endregion

	#region ObjectFill ConcurrentBag Overloads

	[Theory]
	[InlineData(DataSourceType.IEnumerable, ExecutionMode.Task)]
	[InlineData(DataSourceType.IEnumerable, ExecutionMode.FuncWithSemaphore)]
	[InlineData(DataSourceType.ConcurrentBag, ExecutionMode.Task)]
	[InlineData(DataSourceType.ConcurrentBag, ExecutionMode.FuncWithSemaphore)]
	[InlineData(DataSourceType.List, ExecutionMode.Task)]
	[InlineData(DataSourceType.List, ExecutionMode.FuncWithSemaphore)]
	public async Task ObjectFill_ConcurrentBagOverloads_ShouldAddRange(DataSourceType dataSourceType, ExecutionMode executionMode)
	{
		ConcurrentBag<int> bag = [];
		using SemaphoreSlim? semaphore = executionMode == ExecutionMode.FuncWithSemaphore ? new(1, 1) : null;

		if (executionMode == ExecutionMode.Task)
		{
			switch (dataSourceType)
			{
				case DataSourceType.IEnumerable:
					Task<IEnumerable<int>?> taskIEnumerable = Task.FromResult<IEnumerable<int>?>([1, 2, 3]);
					await bag.ObjectFill(taskIEnumerable);
					break;
				case DataSourceType.ConcurrentBag:
					Task<ConcurrentBag<int>?> taskConcurrentBag = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));
					await bag.ObjectFill(taskConcurrentBag);
					break;
				case DataSourceType.List:
					Task<List<int>?> taskList = Task.FromResult<List<int>?>(new List<int> { 1, 2, 3 });
					await bag.ObjectFill(taskList);
					break;
			}
		}
		else
		{
			switch (dataSourceType)
			{
				case DataSourceType.IEnumerable:
					Task<IEnumerable<int>> funcIEnumerable() => Task.FromResult<IEnumerable<int>>([1, 2, 3]);
					await bag.ObjectFill(funcIEnumerable, semaphore!, Current.CancellationToken);
					break;
				case DataSourceType.ConcurrentBag:
					Task<ConcurrentBag<int>> funcConcurrentBag() => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
					await bag.ObjectFill(funcConcurrentBag, semaphore!, Current.CancellationToken);
					break;
				case DataSourceType.List:
					Task<List<int>> funcList() => Task.FromResult(new List<int> { 1, 2, 3 });
					await bag.ObjectFill(funcList, semaphore!, Current.CancellationToken);
					break;
			}
		}

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

	[Theory]
	[InlineData(DataSourceType.IEnumerable, ExecutionMode.Task)]
	[InlineData(DataSourceType.IEnumerable, ExecutionMode.FuncWithSemaphore)]
	[InlineData(DataSourceType.ConcurrentBag, ExecutionMode.Task)]
	[InlineData(DataSourceType.ConcurrentBag, ExecutionMode.FuncWithSemaphore)]
	public async Task ObjectFill_ConcurrentBagNull_ShouldNotThrow(DataSourceType dataSourceType, ExecutionMode executionMode)
	{
		ConcurrentBag<int>? bag = null;
		using SemaphoreSlim? semaphore = executionMode == ExecutionMode.FuncWithSemaphore ? new(1, 1) : null;

		if (executionMode == ExecutionMode.Task)
		{
			if (dataSourceType == DataSourceType.IEnumerable)
			{
				Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>([1, 2, 3]);
				await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
			}
			else
			{
				Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));
				await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
			}
		}
		else
		{
			if (dataSourceType == DataSourceType.IEnumerable)
			{
				Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>([1, 2, 3]);
				await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore!, Current.CancellationToken));
			}
			else
			{
				Task<ConcurrentBag<int>> func() => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
				await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore!, Current.CancellationToken));
			}
		}
	}

	[Theory]
	[InlineData(DataSourceType.IEnumerable)]
	[InlineData(DataSourceType.ConcurrentBag)]
	public async Task ObjectFill_ConcurrentBagWithNullTaskResult_ShouldNotAdd(DataSourceType dataSourceType)
	{
		ConcurrentBag<int> bag = [];
		if (dataSourceType == DataSourceType.IEnumerable)
		{
			Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(null);
			await bag.ObjectFill(task);
		}
		else
		{
			Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(null);
			await bag.ObjectFill(task);
		}
		bag.Count.ShouldBe(0);
	}

	#endregion

	#region ObjectFill Exception Handling

	[Theory]
	[InlineData(CollectionType.List, DataSourceType.List, ExecutionMode.Task)]
	[InlineData(CollectionType.List, DataSourceType.IEnumerable, ExecutionMode.Task)]
	[InlineData(CollectionType.List, DataSourceType.List, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.List, DataSourceType.IEnumerable, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.HashSet, DataSourceType.List, ExecutionMode.Task)]
	[InlineData(CollectionType.HashSet, DataSourceType.HashSet, ExecutionMode.Task)]
	[InlineData(CollectionType.HashSet, DataSourceType.IEnumerable, ExecutionMode.Task)]
	[InlineData(CollectionType.HashSet, DataSourceType.List, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.HashSet, DataSourceType.HashSet, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.HashSet, DataSourceType.IEnumerable, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.IEnumerable, ExecutionMode.Task)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.IEnumerable, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.ConcurrentBag, ExecutionMode.Task)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.ConcurrentBag, ExecutionMode.FuncWithSemaphore)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.List, ExecutionMode.Task)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.List, ExecutionMode.FuncWithSemaphore)]
	public async Task ObjectFill_WithTaskOrFuncThrowing_ShouldHandleException(CollectionType collectionType, DataSourceType dataSourceType, ExecutionMode executionMode)
	{
		dynamic collection = collectionType switch
		{
			CollectionType.List => new List<int> { 1 },
			CollectionType.HashSet => dataSourceType == DataSourceType.ConcurrentBag ? new HashSet<int>() : new HashSet<int> { 1 },
			CollectionType.ConcurrentBag => new ConcurrentBag<int>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		int expectedCount = collectionType == CollectionType.ConcurrentBag ? 0 : (dataSourceType == DataSourceType.ConcurrentBag && collectionType == CollectionType.HashSet) ? 0 : 1;

		using SemaphoreSlim? semaphore = executionMode == ExecutionMode.FuncWithSemaphore ? new(1, 1) : null;
		if (executionMode == ExecutionMode.Task)
		{
			switch (dataSourceType)
			{
				case DataSourceType.List:
					Task<List<int>?> taskList = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));
					await Async.ObjectFill(collection, taskList);
					break;
				case DataSourceType.HashSet:
					Task<HashSet<int>?> taskHashSet = Task.FromException<HashSet<int>?>(new InvalidOperationException("Test exception"));
					await Async.ObjectFill(collection, taskHashSet);
					break;
				case DataSourceType.IEnumerable:
					Task<IEnumerable<int>?> taskIEnumerable = Task.FromException<IEnumerable<int>?>(new InvalidOperationException("Test exception"));
					await Async.ObjectFill(collection, taskIEnumerable);
					break;
				case DataSourceType.ConcurrentBag:
					Task<ConcurrentBag<int>?> taskConcurrentBag = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));
					await Async.ObjectFill(collection, taskConcurrentBag);
					break;
			}
		}
		else
		{
			switch (dataSourceType)
			{
				case DataSourceType.List:
					Task<List<int>> funcList() => throw new InvalidOperationException("Test exception");
					if (collectionType == CollectionType.List)
					{
						await ((List<int>)collection).ObjectFill(funcList, semaphore!, Current.CancellationToken);
					}

					else if (collectionType == CollectionType.HashSet)
					{
						await ((HashSet<int>)collection).ObjectFill(funcList, semaphore!, Current.CancellationToken);
					}
					else
					{
						await ((ConcurrentBag<int>)collection).ObjectFill(funcList, semaphore!, Current.CancellationToken);
					}


					break;
				case DataSourceType.HashSet:
					Task<HashSet<int>> funcHashSet() => throw new InvalidOperationException("Test exception");
					await ((HashSet<int>)collection).ObjectFill(funcHashSet, semaphore!, Current.CancellationToken);
					break;
				case DataSourceType.IEnumerable:
					Task<IEnumerable<int>> funcIEnumerable() => throw new InvalidOperationException("Test exception");
					if (collectionType == CollectionType.List)
					{
						await ((List<int>)collection).ObjectFill(funcIEnumerable, semaphore!, Current.CancellationToken);
					}

					else if (collectionType == CollectionType.HashSet)
					{
						await ((HashSet<int>)collection).ObjectFill(funcIEnumerable, semaphore!, Current.CancellationToken);
					}
					else
					{
						await ((ConcurrentBag<int>)collection).ObjectFill(funcIEnumerable, semaphore!, Current.CancellationToken);
					}


					break;
				case DataSourceType.ConcurrentBag:
					Task<ConcurrentBag<int>> funcConcurrentBag() => throw new InvalidOperationException("Test exception");
					await ((ConcurrentBag<int>)collection).ObjectFill(funcConcurrentBag, semaphore!, Current.CancellationToken);
					break;
			}
		}

		((IEnumerable<int>)collection).Count().ShouldBe(expectedCount);
	}

	#endregion

	#region ObjectFill Null Semaphore Tests

	[Theory]
	[InlineData(CollectionType.ConcurrentBag)]
	[InlineData(CollectionType.List)]
	[InlineData(CollectionType.HashSet)]
	public async Task ObjectFill_CollectionsWithNullSemaphore_ShouldAddItem(CollectionType collectionType)
	{
		dynamic collection = collectionType switch
		{
			CollectionType.ConcurrentBag => new ConcurrentBag<int?>(),
			CollectionType.List => (IList<int?>)new List<int?>(),
			CollectionType.HashSet => new HashSet<int?>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		Task<int?> func() => Task.FromResult<int?>(collectionType == CollectionType.ConcurrentBag ? 42 : 99);

		if (collectionType == CollectionType.List)
		{
			await ((IList<int?>)collection).ObjectFill(func, null!, Current.CancellationToken);
		}

		else if (collectionType == CollectionType.HashSet)
		{
			await ((HashSet<int?>)collection).ObjectFill(func, null!, Current.CancellationToken);
		}
		else
		{
			await ((ConcurrentBag<int?>)collection).ObjectFill(func, null!, Current.CancellationToken);
		}


		((IEnumerable<int?>)collection).Count().ShouldBe(1);
	}

	[Theory]
	[InlineData(CollectionType.List, DataSourceType.List)]
	[InlineData(CollectionType.List, DataSourceType.IEnumerable)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.IEnumerable)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.ConcurrentBag)]
	[InlineData(CollectionType.ConcurrentBag, DataSourceType.List)]
	public async Task ObjectFill_WithNullSemaphore_ShouldAddRange(CollectionType collectionType, DataSourceType dataSourceType)
	{
		dynamic collection = collectionType switch
		{
			CollectionType.List => new List<int> { 1 },
			CollectionType.HashSet => new HashSet<int> { 1 },
			CollectionType.ConcurrentBag => new ConcurrentBag<int>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		int expectedCount = collectionType == CollectionType.ConcurrentBag ? 2 : 3;  // ConcurrentBag starts empty

		switch (dataSourceType)
		{
			case DataSourceType.List:
				Task<List<int>> funcList() => Task.FromResult(new List<int> { 2, 3 });
				if (collectionType == CollectionType.List)
				{
					await ((List<int>)collection).ObjectFill(funcList, null!, Current.CancellationToken);
				}

				else if (collectionType == CollectionType.HashSet)
				{
					await ((HashSet<int>)collection).ObjectFill(funcList, null!, Current.CancellationToken);
				}
				else
				{
					await ((ConcurrentBag<int>)collection).ObjectFill(funcList, null!, Current.CancellationToken);
				}


				break;
			case DataSourceType.IEnumerable:
				Task<IEnumerable<int>> funcIEnumerable() => Task.FromResult<IEnumerable<int>>([2, 3]);
				if (collectionType == CollectionType.List)
				{
					await ((List<int>)collection).ObjectFill(funcIEnumerable, null!, Current.CancellationToken);
				}

				else if (collectionType == CollectionType.HashSet)
				{
					await ((HashSet<int>)collection).ObjectFill(funcIEnumerable, null!, Current.CancellationToken);
				}
				else
				{
					await ((ConcurrentBag<int>)collection).ObjectFill(funcIEnumerable, null!, Current.CancellationToken);
				}


				break;
			case DataSourceType.ConcurrentBag:
				Task<ConcurrentBag<int>> funcConcurrentBag() => Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
				await ((ConcurrentBag<int>)collection).ObjectFill(funcConcurrentBag, null!, Current.CancellationToken);
				expectedCount = 3;
				break;
		}

		((IEnumerable<int>)collection).Count().ShouldBe(expectedCount);
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithTaskListSemaphoreNull_ShouldReplaceWithListContent()
	{
		HashSet<int> collection = new() { 1 };
		static Task<List<int>> func() => Task.FromResult(new List<int> { 2, 3 });

		await collection.ObjectFill(func, null!, Current.CancellationToken);

		collection.Count.ShouldBe(3); // HashSet adds items from List: {1, 2, 3}
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithTaskIEnumerableSemaphoreNull_ShouldReplaceWithIEnumerableContent()
	{
		HashSet<int> collection = new() { 1 };
		static Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>([2, 3]);

		await collection.ObjectFill(func, null!, Current.CancellationToken);

		collection.Count.ShouldBe(3); // HashSet adds items from IEnumerable: {1, 2, 3}
	}

	#endregion

	#region ObjectFill<T> MemoryStream

	[Theory]
	[InlineData(ExecutionMode.Task, false)]
	[InlineData(ExecutionMode.FuncWithSemaphore, false)]
	[InlineData(ExecutionMode.FuncWithoutSemaphore, false)]
	[InlineData(ExecutionMode.Task, true)]
	[InlineData(ExecutionMode.FuncWithSemaphore, true)]
	public async Task ObjectFill_MemoryStream_ShouldWriteDataOrHandleException(ExecutionMode executionMode, bool throwException)
	{
		byte[] testData = [1, 2, 3, 4, 5];
		await using MemoryStream ms = new();
		using SemaphoreSlim? semaphore = executionMode == ExecutionMode.FuncWithSemaphore ? new(1, 1) : null;

		if (executionMode == ExecutionMode.Task)
		{
			if (throwException)
			{
				Task<MemoryStream> task = Task.FromException<MemoryStream>(new InvalidOperationException("Test exception"));
				await ms.ObjectFill(task);
			}
			else
			{
				await using MemoryStream resultMs = new();
				await resultMs.WriteAsync(testData, Current.CancellationToken);
				resultMs.Position = 0;
				Task<MemoryStream> task = Task.FromResult(resultMs);
				await ms.ObjectFill(task);
			}
		}
		else
		{
			async Task<MemoryStream> func()
			{
				if (throwException)
				{
					throw new InvalidOperationException("Test exception");
				}


				MemoryStream resultMs = new();
				await resultMs.WriteAsync(testData, Current.CancellationToken);
				resultMs.Position = 0;
				return resultMs;
			}

			if (executionMode == ExecutionMode.FuncWithSemaphore)
			{
				await ms.ObjectFill(func, semaphore!, Current.CancellationToken);
			}
			else
			{
				await ms.ObjectFill(func, null!, Current.CancellationToken);
			}

		}

		if (throwException)
		{
			ms.Length.ShouldBe(0);
		}
		else
		{
			ms.Position = 0;
			byte[] buffer = new byte[testData.Length];
			await ms.ReadAsync(buffer.AsMemory(0, testData.Length), Current.CancellationToken);
			buffer.ShouldBe(testData);
		}
	}

	#endregion

	#region ObjectFill<T> DataTable

	[Theory]
	[InlineData(ExecutionMode.Task, false)]
	[InlineData(ExecutionMode.FuncWithSemaphore, false)]
	[InlineData(ExecutionMode.FuncWithoutSemaphore, false)]
	[InlineData(ExecutionMode.Task, true)]
	[InlineData(ExecutionMode.FuncWithSemaphore, true)]
	public async Task ObjectFill_DataTable_ShouldLoadDataOrHandleException(ExecutionMode executionMode, bool throwException)
	{
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		using SemaphoreSlim? semaphore = executionMode == ExecutionMode.FuncWithSemaphore ? new(1, 1) : null;
		if (executionMode == ExecutionMode.Task)
		{
			if (throwException)
			{
				Task<DataTable> task = Task.FromException<DataTable>(new InvalidOperationException("Test exception"));
				await dt.ObjectFill(task);
			}
			else
			{
				using DataTable resultDt = new();
				resultDt.Columns.Add("Id", typeof(int));
				resultDt.Columns.Add("Name", typeof(string));
				resultDt.Rows.Add(1, "Item1");
				resultDt.Rows.Add(2, "Item2");
				Task<DataTable> task = Task.FromResult(resultDt);
				await dt.ObjectFill(task);
			}
		}
		else
		{
			Task<DataTable> func()
			{
				if (throwException)
				{
					throw new InvalidOperationException("Test exception");
				}


				DataTable resultDt = new();
				resultDt.Columns.Add("Id", typeof(int));
				resultDt.Columns.Add("Name", typeof(string));
				resultDt.Rows.Add(1, "Item1");
				return Task.FromResult(resultDt);
			}

			if (executionMode == ExecutionMode.FuncWithSemaphore)
			{
				await dt.ObjectFill(func, semaphore!, Current.CancellationToken);
			}
			else
			{
				await dt.ObjectFill(func, null!, Current.CancellationToken);
			}

		}

		if (throwException)
		{
			dt.Rows.Count.ShouldBe(0);
		}
		else
		{
			dt.Rows.Count.ShouldBe(executionMode == ExecutionMode.Task ? 2 : 1);
			dt.Rows[0]["Id"].ShouldBe(1);
			dt.Rows[0]["Name"].ShouldBe("Item1");
		}
	}

	#endregion

	#region ObjectFill<T> List<T> with Task<List<T>?>

	[Theory]
	[InlineData(CollectionType.List, NullScenario.NotNull)]
	[InlineData(CollectionType.List, NullScenario.ResultNull)]
	[InlineData(CollectionType.HashSet, NullScenario.NotNull)]
	[InlineData(CollectionType.HashSet, NullScenario.ResultNull)]
	public async Task ObjectFill_ListWithTaskList_ShouldAddOrHandleNull(CollectionType collectionType, NullScenario nullScenario)
	{
		if (collectionType == CollectionType.List)
		{
			List<int> list = [1, 2, 3];
			Task<List<int>?> task = nullScenario == NullScenario.ResultNull
				? Task.FromResult<List<int>?>(null)
				: Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

			await list.ObjectFill(task);

			if (nullScenario == NullScenario.ResultNull)
			{
				list.Count.ShouldBe(3);
				list.ShouldBe([1, 2, 3]);
			}
			else
			{
				list.Count.ShouldBe(6);
				list.ShouldBe([1, 2, 3, 4, 5, 6]);
			}
		}
		else // HashSet
		{
			HashSet<int> hashSet = [1, 2, 3];
			Task<List<int>?> task = nullScenario == NullScenario.ResultNull
				? Task.FromResult<List<int>?>(null)
				: Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

			await hashSet.ObjectFill(task);

			if (nullScenario == NullScenario.ResultNull)
			{
				hashSet.Count.ShouldBe(3);
				hashSet.ShouldBe([1, 2, 3]);
			}
			else
			{
				hashSet.Count.ShouldBe(6);
				hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
			}
		}
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ObjectFill_HashSetWithTaskHashSet_ShouldHandleDuplicates(bool hasDuplicates)
	{
		HashSet<int> hashSet = [1, 2, 3];
		Task<HashSet<int>?> task = hasDuplicates
			? Task.FromResult<HashSet<int>?>(new HashSet<int> { 2, 3, 4, 5 })
			: Task.FromResult<HashSet<int>?>(new HashSet<int> { 4, 5, 6 });

		await hashSet.ObjectFill(task);

		if (hasDuplicates)
		{
			hashSet.Count.ShouldBe(5);
			hashSet.ShouldBe([1, 2, 3, 4, 5]);
		}
		else
		{
			hashSet.Count.ShouldBe(6);
			hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
		}
	}

	#endregion

	#region ObjectFill<T> List<T> with Func, Semaphore, CancellationToken

	public enum ObjectFillCollectionType { List, HashSet }
	public enum ObjectFillFuncScenario { Normal, ReturnsNull, ThrowsException, NullSemaphore }

	[Theory]
	[InlineData(ObjectFillCollectionType.List, ObjectFillFuncScenario.Normal)]
	[InlineData(ObjectFillCollectionType.List, ObjectFillFuncScenario.ReturnsNull)]
	[InlineData(ObjectFillCollectionType.List, ObjectFillFuncScenario.NullSemaphore)]
	[InlineData(ObjectFillCollectionType.List, ObjectFillFuncScenario.ThrowsException)]
	[InlineData(ObjectFillCollectionType.HashSet, ObjectFillFuncScenario.Normal)]
	[InlineData(ObjectFillCollectionType.HashSet, ObjectFillFuncScenario.ReturnsNull)]
	[InlineData(ObjectFillCollectionType.HashSet, ObjectFillFuncScenario.ThrowsException)]
	public async Task ObjectFill_Collection_WithVariousScenarios_HandlesCorrectly(ObjectFillCollectionType collectionType, ObjectFillFuncScenario scenario)
	{
		switch (collectionType)
		{
			case ObjectFillCollectionType.List:
				await TestObjectFillList(scenario);
				break;
			case ObjectFillCollectionType.HashSet:
				await TestObjectFillHashSet(scenario);
				break;
		}
	}

	private static async Task TestObjectFillList(ObjectFillFuncScenario scenario)
	{
		List<int> collection = new() { 1, 2, 3 };
		using SemaphoreSlim? semaphore = scenario == ObjectFillFuncScenario.NullSemaphore ? null : new(1, 1);

		Task<List<int>> func() => scenario switch
		{
			ObjectFillFuncScenario.Normal or ObjectFillFuncScenario.NullSemaphore => Task.FromResult(new List<int> { 4, 5, 6 }),
			ObjectFillFuncScenario.ReturnsNull => Task.FromResult<List<int>>(null!),
			ObjectFillFuncScenario.ThrowsException => Task.FromException<List<int>>(new InvalidOperationException("Test error")),
			_ => throw new ArgumentOutOfRangeException(nameof(scenario))
		};

		await Async.ObjectFill(collection, func, semaphore, Current.CancellationToken);

		int expectedCount = scenario is ObjectFillFuncScenario.Normal or ObjectFillFuncScenario.NullSemaphore ? 6 : 3;
		collection.Count.ShouldBe(expectedCount);
	}

	private static async Task TestObjectFillHashSet(ObjectFillFuncScenario scenario)
	{
		HashSet<int> collection = new() { 1, 2, 3 };
		using SemaphoreSlim semaphore = new(1, 1);

		Task<HashSet<int>> func() => scenario switch
		{
			ObjectFillFuncScenario.Normal => Task.FromResult(new HashSet<int> { 4, 5, 6 }),
			ObjectFillFuncScenario.ReturnsNull => Task.FromResult<HashSet<int>>(null!),
			ObjectFillFuncScenario.ThrowsException => Task.FromException<HashSet<int>>(new InvalidOperationException("Test error")),
			_ => throw new ArgumentOutOfRangeException(nameof(scenario))
		};

		await Async.ObjectFill(collection, func, semaphore, Current.CancellationToken);

		int expectedCount = scenario == ObjectFillFuncScenario.Normal ? 6 : 3;
		collection.Count.ShouldBe(expectedCount);
	}

	[Theory]
	[InlineData(CollectionType.List, false)]
	[InlineData(CollectionType.List, true)]
	[InlineData(CollectionType.HashSet, false)]
	[InlineData(CollectionType.HashSet, true)]
	public async Task ObjectFill_WithFuncSemaphore_ShouldReleaseSemaphore(CollectionType collectionType, bool throwException)
	{
		dynamic collection = collectionType == CollectionType.List ? new List<int> { 1 } : (dynamic)new HashSet<int> { 1 };
		using SemaphoreSlim semaphore = new(1, 1);

		if (collectionType == CollectionType.List)
		{
			Task<List<int>> func()
			{
				if (throwException)
				{
					throw new InvalidOperationException("Fail");
				}


				return Task.FromResult(new List<int> { 2 });
			}
			await ((List<int>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
		}
		else
		{
			Task<HashSet<int>> func()
			{
				if (throwException)
				{
					throw new InvalidOperationException("Fail");
				}


				return Task.FromResult(new HashSet<int> { 2 });
			}
			await ((HashSet<int>)collection).ObjectFill(func, semaphore, Current.CancellationToken);
		}

		semaphore.CurrentCount.ShouldBe(1);
	}

	[Theory]
	[InlineData(CollectionType.List)]
	[InlineData(CollectionType.HashSet)]
	public async Task ObjectFill_WithFuncSemaphore_ShouldLimitConcurrency(CollectionType collectionType)
	{
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			int index = i;
			if (collectionType == CollectionType.List)
			{
				List<int> collection = new() { i };
				async Task<List<int>> func()
				{
					lock (lockObj)
					{
						concurrentExecutions++;
						maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
					}
					await Task.Delay(50, Current.CancellationToken);
					lock (lockObj)
					{
						concurrentExecutions--;
					}
					return new List<int> { index * 10 };
				}
				tasks.Add(collection.ObjectFill(func, semaphore, Current.CancellationToken));
			}
			else
			{
				HashSet<int> collection = new() { i };
				async Task<HashSet<int>> func()
				{
					lock (lockObj)
					{
						concurrentExecutions++;
						maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
					}
					await Task.Delay(50, Current.CancellationToken);
					lock (lockObj)
					{
						concurrentExecutions--;
					}
					return new HashSet<int> { index * 10 };
				}
				tasks.Add(collection.ObjectFill(func, semaphore, Current.CancellationToken));
			}
		}

		await Task.WhenAll(tasks);

		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
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
		Task<object> func()
		{
			return Task.FromResult(value);
		}

		using SemaphoreSlim semaphore = new(1, 1);
		await obj.ObjectUpdate(prop, func, semaphore, Current.CancellationToken);
		obj.AsyncInt.ShouldBe(expectedInt);
		obj.AsyncString.ShouldBe(expectedString);
	}

	[Theory]
	[InlineData("Invalid", 100)]
	[InlineData("NonExistent", "test")]
	public async Task ObjectUpdate_WithFuncSemaphoreInvalidProperty_ShouldHandleException(string prop, object value)
	{
		AsyncIntString obj = new();
		Task<object> func()
		{
			return Task.FromResult(value);
		}

		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, func, semaphore, Current.CancellationToken));
	}

	[Theory]
	[InlineData(NullScenario.ObjectNull, false)]
	[InlineData(NullScenario.NotNull, true)]
	public async Task ObjectUpdate_WithFuncSemaphore_ShouldHandleNullOrException(NullScenario nullScenario, bool throwException)
	{
		AsyncIntString? obj = nullScenario == NullScenario.ObjectNull ? null : new();
		Task<int> func()
		{
			if (throwException)
			{
				throw new InvalidOperationException("Test exception");
			}


			return Task.FromResult(100);
		}

		using SemaphoreSlim semaphore = new(1, 1);
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", func, semaphore, Current.CancellationToken));
		if (obj != null && !throwException)
		{
			obj.AsyncInt.ShouldBe(100);
		}
		else if (obj != null)
		{
			obj.AsyncInt.ShouldBe(0);
		}

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
		using CancellationTokenSource cts = new();

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
	[InlineData(3, false)]
	[InlineData(5, false)]
	[InlineData(3, true)]
	public async Task RunAll_VoidTasks_ShouldExecuteTasksOrStopOnError(int count, bool breakOnError)
	{
		int executed = 0;
		using CancellationTokenSource? cts = breakOnError ? new() : null;
		List<Func<Task>> tasks = [];
		for (int i = 0; i < count; i++)
		{
			int index = i;
			tasks.Add(async () =>
			{
				Interlocked.Increment(ref executed);
				await Task.Delay(index == 1 && breakOnError ? 50 : 10);
				if (breakOnError && index == 1)
				{
					throw new InvalidOperationException("Error");
				}

			});
		}
		if (breakOnError)
		{
			await tasks.RunAll(null, cts, true);
		}
		else
		{
			await tasks.RunAll();
		}

		if (breakOnError)
		{
			executed.ShouldBeLessThanOrEqualTo(count);
		}
		else
		{
			executed.ShouldBe(count);
		}
	}

	#endregion

	#region RunAsyncWithSemaphore Tests

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task RunAsyncWithSemaphore_ShouldExecuteAndReleaseSemaphore(bool throwException, bool isVoid)
	{
		using SemaphoreSlim semaphore = new(1, 1);
		if (isVoid)
		{
			bool executed = !throwException;
			Task task = throwException
				? Task.FromException(new InvalidOperationException("Test"))
				: Task.Run(() => executed = true);
			await task.RunAsyncWithSemaphore(semaphore);
			if (!throwException)
			{
				executed.ShouldBeTrue();
			}
		}
		else
		{
			Task<int> task = throwException
				? Task.FromException<int>(new InvalidOperationException("Test"))
				: Task.FromResult(42);
			int result = await task.RunAsyncWithSemaphore(semaphore);
			if (throwException)
			{
				result.ShouldBe(0);
			}
			else
			{
				result.ShouldBe(42);
			}

		}
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

	[Fact]
	public async Task ObjectFill_HashSet_WithFuncHashSetAndSemaphore_ShouldReleaseOnSuccess()
	{
		// Arrange
		HashSet<string> collection = ["initial"];
		using SemaphoreSlim semaphore = new(1, 1);

		// Wrap semaphore to track releases
		static async Task<HashSet<string>> func()
		{
			await Task.Delay(10, Current.CancellationToken);
			return ["added"];
		}

		// Act
		await semaphore.WaitAsync(Current.CancellationToken);
		semaphore.Release();

		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(2);
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithFuncListAndSemaphore_ShouldReleaseOnSuccess()
	{
		// Arrange
		HashSet<string> collection = ["initial"];
		using SemaphoreSlim semaphore = new(1, 1);

		static async Task<List<string>> func()
		{
			await Task.Delay(10, Current.CancellationToken);
			return ["added1", "added2"];
		}

		// Act
		await semaphore.WaitAsync(Current.CancellationToken);
		semaphore.Release();

		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(3);
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithFuncHashSetAndSemaphore_ShouldReleaseOnException()
	{
		// Arrange
		HashSet<string> collection = ["initial"];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<HashSet<string>> func() => Task.FromException<HashSet<string>>(new InvalidOperationException("Test error"));

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(1); // Collection unchanged
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released even on exception
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithFuncListAndSemaphore_ShouldReleaseOnException()
	{
		// Arrange
		HashSet<string> collection = ["initial"];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<List<string>> func() => Task.FromException<List<string>>(new InvalidOperationException("Test error"));

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(1); // Collection unchanged
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released even on exception
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithFuncHashSetAndNullSemaphore_ShouldWork()
	{
		// Arrange
		HashSet<string> collection = ["initial"];

		static Task<HashSet<string>> func() => Task.FromResult(new HashSet<string> { "added1", "added2" });

		// Act
		await collection.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithComplexType_ShouldWork()
	{
		// Arrange
		HashSet<TestModel> collection = [new() { Id = 1, Name = "Initial" }];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<HashSet<TestModel>> func() => Task.FromResult(new HashSet<TestModel>(new TestModelEqualityComparer())
		{
			new() { Id = 2, Name = "Added" }
		});

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(2);
	}

	private class TestModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	private class TestModelEqualityComparer : IEqualityComparer<TestModel>
	{
		public bool Equals(TestModel? x, TestModel? y) => x?.Id == y?.Id;
		public int GetHashCode(TestModel obj) => obj.Id.GetHashCode();
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithFuncIEnumerableAndSemaphore_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>([3, 4, 5]);

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(5);
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithFuncIEnumerableAndNullSemaphore_ShouldWork()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];

		static Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>([3, 4, 5]);

		// Act
		await collection.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(5);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithFuncIEnumerableAndSemaphore_ShouldReleaseOnException()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<IEnumerable<int>> func() => throw new InvalidOperationException("Test exception");

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should still be released
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithNullResult_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>(null!);

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(2); // Should remain unchanged
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBag_WithFuncIEnumerable_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? collection = null;
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<IEnumerable<int>> func() => Task.FromResult<IEnumerable<int>>([3, 4, 5]);

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should still be released
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithTaskList_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		Task<List<int>?> task = Task.FromResult<List<int>?>([3, 4, 5]);

		// Act
		await collection.ObjectFill(task);

		// Assert
		collection.Count.ShouldBe(5);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithTaskListNull_ShouldNotAdd()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		Task<List<int>?> task = Task.FromResult<List<int>?>(null);

		// Act
		await collection.ObjectFill(task);

		// Assert
		collection.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithFuncListAndSemaphore_ShouldAddRangeAndRelease()
	{
		// Arrange
		ConcurrentBag<int> collection = [1, 2];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<List<int>> func() => Task.FromResult<List<int>>([3, 4, 5]);

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(5);
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithFuncConcurrentBagAndSemaphore_ShouldAddRangeAndRelease()
	{
		// Arrange
		ConcurrentBag<string> collection = ["a", "b"];
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<ConcurrentBag<string>> func() => Task.FromResult<ConcurrentBag<string>>(new(["c", "d", "e"]));

		// Act
		await collection.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		collection.Count.ShouldBe(5);
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithTaskConcurrentBagWithValues_ShouldAddAllElements()
	{
		// Arrange
		HashSet<string> hashSet = ["existing"];
		ConcurrentBag<string> bag = new(["value1", "value2", "value3"]);
		Task<ConcurrentBag<string>?> task = Task.FromResult<ConcurrentBag<string>?>(bag);

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(4);
		hashSet.ShouldContain("existing");
		hashSet.ShouldContain("value1");
		hashSet.ShouldContain("value2");
		hashSet.ShouldContain("value3");
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithTaskConcurrentBagNull_ShouldNotAdd()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(null);

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithTaskConcurrentBagThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<ConcurrentBag<int>?> task = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await hashSet.ObjectFill(task));
		hashSet.Count.ShouldBe(3); // Should remain unchanged
	}

	#endregion
}
