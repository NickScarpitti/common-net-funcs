using System.Collections.Concurrent;
using System.Data;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.Core;
using static Xunit.TestContext;

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

	[Fact]
	public async Task ObjectFill_WithNullObject_ShouldNotThrowException()
	{
		// Arrange
		AsyncIntString obj = null!;

		// Act
		Task<AsyncIntString> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "Updated" });

		// Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_WithNullResult_ShouldNotThrowException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		AsyncIntString? taskResult = null;
		Task<AsyncIntString?> task = Task.FromResult(taskResult);

		// Act
		await obj.ObjectFill(task);

		// Assert
		obj.AsyncInt.ShouldBe(0);
		obj.AsyncString.ShouldBe("Original");
	}

	[Fact]
	public async Task ObjectFill_WhenTaskThrowsException_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		Task<AsyncIntString> task = Task.FromException<AsyncIntString>(new InvalidOperationException());

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
		obj.AsyncInt.ShouldBe(0);
		obj.AsyncString.ShouldBe("Original");
	}

	[Fact]
	public async Task ObjectFill_WithComplexType_ShouldCopyProperties()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		AsyncIntString taskResult = new() { AsyncInt = 42, AsyncString = "Updated" };
		Task<AsyncIntString> task = Task.FromResult(taskResult);

		// Act
		await obj.ObjectFill(task);

		// Assert
		obj.AsyncInt.ShouldBe(42);
		obj.AsyncString.ShouldBe("Updated");
	}

	[Fact]
	public async Task ObjectFill_WithFuncComplexType_ShouldCopyProperties()
	{
		// Arrange - Test Func<Task<T>> overload WITHOUT semaphore with complex type (covers line 53)
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 99, AsyncString = "FromFunc" });
		}

		// Act
		await obj.ObjectFill(func);

		// Assert
		obj.AsyncInt.ShouldBe(99);
		obj.AsyncString.ShouldBe("FromFunc");
	}

	[Fact]
	public async Task ObjectFill_WithFuncComplexTypeAndNullResult_ShouldNotModify()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 5, AsyncString = "Test" };
		static Task<AsyncIntString?> func()
		{
			return Task.FromResult<AsyncIntString?>(null);
		}

		// Act
		await obj.ObjectFill(func);

		// Assert
		obj.AsyncInt.ShouldBe(5);
		obj.AsyncString.ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndException_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1, AsyncString = "Original" };
		static Task<AsyncIntString> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(func));
		obj.AsyncInt.ShouldBe(1);
		obj.AsyncString.ShouldBe("Original");
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
		// Arrange
		dynamic collection = collectionType switch
		{
			"List" => new List<string>(),
			"HashSet" => new HashSet<string>(),
			"ConcurrentBag" => new ConcurrentBag<string>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		// Act
		Task<string?> task = Task.FromResult(taskResult);
		await Async.ObjectFill(collection, task);

		// Assert
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
		// Arrange
		dynamic collection = collectionType switch
		{
			"List" => new List<string>(),
			"HashSet" => new HashSet<string>(),
			"ConcurrentBag" => new ConcurrentBag<string>(),
			_ => throw new ArgumentException("Invalid collection type")
		};

		// Act
		Task<string> task = Task.FromException<string>(new InvalidOperationException());

		// Assert
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
		// Arrange
		dynamic collection = collectionType switch
		{
			"List" => new List<string>(),
			"HashSet" => new HashSet<string>(),
			"ConcurrentBag" => new ConcurrentBag<string>(),
			_ => throw new ArgumentException("Invalid collection type")
		};
		List<string> taskResult = ["A", "B", "C"];
		Task<IEnumerable<string>> task = Task.FromResult<IEnumerable<string>>(taskResult);

		// Act
		await Async.ObjectFill(collection, task);

		// Assert
		((IEnumerable<string>)collection).Count().ShouldBe(3);
	}

	#endregion

	#region ObjectFill<T> Func + Semaphore

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldCopyProperties()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Original" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 99, AsyncString = "FromFunc" });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert
		obj.AsyncInt.ShouldBe(99);
		obj.AsyncString.ShouldBe("FromFunc");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndNullSemaphore_ShouldCopyProperties()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 10, AsyncString = "Start" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 55, AsyncString = "NoSemaphore" });
		}

		// Act
		await obj.ObjectFill(func, null);

		// Assert
		obj.AsyncInt.ShouldBe(55);
		obj.AsyncString.ShouldBe("NoSemaphore");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenObjectIsNull_ShouldNotThrow()
	{
		// Arrange
		AsyncIntString obj = null!;
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 100, AsyncString = "Test" });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, semaphore));
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenResultIsNull_ShouldNotModifyObject()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 25, AsyncString = "Original" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult<AsyncIntString>(null!);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert
		obj.AsyncInt.ShouldBe(25);
		obj.AsyncString.ShouldBe("Original");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 30, AsyncString = "BeforeError" };
		static Task<AsyncIntString> func()
		{
			return Task.FromException<AsyncIntString>(new InvalidOperationException("Task failed"));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, semaphore));
		obj.AsyncInt.ShouldBe(30);
		obj.AsyncString.ShouldBe("BeforeError");
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Test" };
		using SemaphoreSlim semaphore = new(1, 1);
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 1, AsyncString = "Success" });
		}

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldReleaseSemaphoreOnException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Test" };
		using SemaphoreSlim semaphore = new(1, 1);
		static Task<AsyncIntString> func()
		{
			throw new InvalidOperationException("Fail");
		}

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
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

		// Act
		await Task.WhenAll(tasks);

		// Assert
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_MultipleConcurrentCalls_ShouldAllComplete()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(3, 3);
		List<AsyncIntString> objects = Enumerable.Range(0, 10).Select(i => new AsyncIntString { AsyncInt = i, AsyncString = $"Start{i}" }).ToList();

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

		// Act
		await Task.WhenAll(tasks);

		// Assert
		for (int i = 0; i < objects.Count; i++)
		{
			objects[i].AsyncInt.ShouldBe(i * 2);
			objects[i].AsyncString.ShouldBe($"Updated{i * 2}");
		}
	}

	[Fact]
	public async Task ObjectFill_WithFuncAndSemaphore_ShouldWaitForSemaphore()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		await semaphore.WaitAsync(Current.CancellationToken); // Acquire semaphore

		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Waiting" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 75, AsyncString = "Released" });
		}

		// Act
		Task fillTask = obj.ObjectFill(func, semaphore);

		// Give a moment to ensure it's waiting
		await Task.Delay(50, Current.CancellationToken);

		// Assert
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
		// Arrange
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

		// Act
		await Async.ObjectFill(collection, (Func<Task<string?>>)func, semaphore);

		// Assert
		((IEnumerable<string?>)collection).Count().ShouldBe(expectedCount);
	}

	#endregion

	#region ObjectFill ConcurrentBag/IList/HashSet with Func + Semaphore

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncSemaphore_ShouldAddItem()
	{
		// Arrange
		ConcurrentBag<int?> bag = [];
		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(1);
		bag.ShouldContain(42);
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncSemaphore_ShouldAddItem()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<string?> func()
		{
			return Task.FromResult<string?>("Test");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(1);
		list[0].ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_HashSetNullableWithFuncSemaphore_ShouldAddItem()
	{
		// Arrange
		HashSet<int?> hashSet = [];
		static Task<int?> func()
		{
			return Task.FromResult<int?>(99);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
		hashSet.ShouldContain(99);
	}

	#endregion

	#region ObjectFill List/HashSet with Task<IEnumerable>

	[Fact]
	public async Task ObjectFill_ListWithTaskIEnumerable_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1, 2];
		IEnumerable<int> items = new[] { 3, 4, 5 };
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(items);

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(5);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskIEnumerable_ShouldAddRange()
	{
		// Arrange
		HashSet<string> hashSet = ["A", "B"];
		IEnumerable<string> items = new[] { "C", "D" };
		Task<IEnumerable<string>?> task = Task.FromResult<IEnumerable<string>?>(items);

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(4);
	}

	#endregion

	#region ObjectFill List/HashSet with Func<Task<IEnumerable>> (no semaphore)

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerableNoSemaphore_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerableNoSemaphore_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	#endregion

	#region ObjectFill List/HashSet with Func<Task<IEnumerable>> + Semaphore

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerable_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerable_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	#endregion

	#region ObjectFill ConcurrentBag Overloads

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerable_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>([1, 2, 3]);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerable_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<IEnumerable<int>> func()
		{
			return Task.FromResult<IEnumerable<int>>([1, 2, 3]);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBag_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBag_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<ConcurrentBag<int>> func()
		{
			return Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskList_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 1, 2, 3 });

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncList_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 1, 2, 3 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskConcurrentBag_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithTaskIEnumerable_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>([1, 2, 3]);

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithFuncIEnumerable_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<IEnumerable<int>> func()
		{
			return Task.FromResult<IEnumerable<int>>([1, 2, 3]);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithTaskConcurrentBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([1, 2, 3]));

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_NullConcurrentBagWithFuncConcurrentBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<ConcurrentBag<int>> func()
		{
			return Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullTaskIEnumerable_ShouldNotAdd()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(null);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullTaskConcurrentBag_ShouldNotAdd()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(null);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagNoSemaphore_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([2, 3]));
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListNoSemaphore_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2, 3 });
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableNoSemaphore_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncConcurrentBagNoSemaphore_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([2, 3]));
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	#endregion

	#region ObjectFill Exception Handling

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int?> bag = [];
		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<string?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int?> hashSet = [];
		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskListThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSetThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		Task<HashSet<int>?> task = Task.FromException<HashSet<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncListThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncListThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncHashSetThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<HashSet<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1];
		Task<IEnumerable<int>?> task = Task.FromException<IEnumerable<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		Task<IEnumerable<int>?> task = Task.FromException<IEnumerable<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1];
		static Task<IEnumerable<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<IEnumerable<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<IEnumerable<int>?> task = Task.FromException<IEnumerable<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<IEnumerable<int>> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBagThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<ConcurrentBag<int>?> task = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<ConcurrentBag<int>> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskListThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await bag.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskConcurrentBagThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [];
		Task<ConcurrentBag<int>?> task = Task.FromException<ConcurrentBag<int>?>(new InvalidOperationException("Test exception"));

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetSingleItemWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<string?> hashSet = [];
		static Task<string?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetHashSetResultWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [];
		static Task<HashSet<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetListResultWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncListSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncConcurrentBagSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<ConcurrentBag<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagSingleItemWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<string?> bag = [];
		static Task<string?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagConcurrentBagResultWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<ConcurrentBag<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagListResultWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionaryWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await dict.ObjectFill("key1", func);

		// Assert
		dict.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionaryWithFuncAndSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dict.ObjectFill("key1", func, semaphore, Current.CancellationToken);

		// Assert
		dict.Count.ShouldBe(0);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Rows.Add(1);

		static Task<DataTable> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await dt.ObjectFill(func);

		// Assert
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncAndSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Rows.Add(1);

		static Task<DataTable> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dt.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		dt.Rows.Count.ShouldBe(1);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		await using MemoryStream ms = new();
		byte[] initialData = [1, 2, 3];
		await ms.WriteAsync(initialData, Current.CancellationToken);
		long initialLength = ms.Length;

		static Task<MemoryStream> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await ms.ObjectFill(func);

		// Assert
		ms.Length.ShouldBe(initialLength);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncAndSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		using MemoryStream ms = new();
		byte[] initialData = [1, 2, 3];
		await ms.WriteAsync(initialData, Current.CancellationToken);
		long initialLength = ms.Length;

		static Task<MemoryStream> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await ms.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		ms.Length.ShouldBe(initialLength);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncListNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1, 2];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncHashSetNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<HashSet<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncListNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [1, 2];
		static Task<ConcurrentBag<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [1, 2];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		ConcurrentBag<int> bag = [1, 2];
		static Task<IEnumerable<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerableNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1, 2];
		static Task<IEnumerable<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncIEnumerableNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<IEnumerable<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2); // No items added due to exception
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncConcurrentBagNoSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<ConcurrentBag<int>?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2); // No items added due to exception
	}

	#endregion

	#region ObjectFill Null Semaphore Tests

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphore_ShouldAddItem()
	{
		// Arrange
		ConcurrentBag<int?> bag = [];
		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		// Act
		await bag.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(1);
		bag.ShouldContain(42);
	}

	[Fact]
	public async Task ObjectFill_IListWithNullSemaphore_ShouldAddItem()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<string?> func()
		{
			return Task.FromResult<string?>("Test");
		}

		// Act
		await list.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(1);
		list[0].ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_HashSetNullableWithNullSemaphore_ShouldAddItem()
	{
		// Arrange
		HashSet<int?> hashSet = [];
		static Task<int?> func()
		{
			return Task.FromResult<int?>(99);
		}

		// Act
		await hashSet.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(1);
		hashSet.ShouldContain(99);
	}

	[Fact]
	public async Task ObjectFill_ListWithNullSemaphore_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2, 3 });
		}

		// Act
		await list.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithNullSemaphore_ShouldLoadData()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		static Task<DataTable> func()
		{
			DataTable resultDt = new();
			resultDt.Columns.Add("Id", typeof(int));
			resultDt.Rows.Add(1);
			return Task.FromResult(resultDt);
		}

		// Act
		await dt.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithNullSemaphore_ShouldWriteData()
	{
		// Arrange
		byte[] testData = [1, 2, 3];
		await using MemoryStream ms = new();
		async Task<MemoryStream> func()
		{
			MemoryStream resultMs = new();
			await resultMs.WriteAsync(testData, Current.CancellationToken);
			resultMs.Position = 0;
			return resultMs;
		}

		// Act
		await ms.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), Current.CancellationToken);
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreList_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2, 3 });
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreHashSet_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 2, 3 });
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ListWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>([2, 3]);
		}

		// Act
		await list.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreIEnumerable_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<IEnumerable<int>> func()
		{
			return Task.FromResult<IEnumerable<int>>([1, 2, 3]);
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreConcurrentBag_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<ConcurrentBag<int>> func()
		{
			return Task.FromResult(new ConcurrentBag<int>([1, 2, 3]));
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullSemaphoreList_ShouldAddRange()
	{
		// Arrange
		ConcurrentBag<int> bag = [];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 1, 2, 3 });
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectUpdate_WithNullSemaphore_ShouldUpdateProperty()
	{
		// Arrange
		AsyncIntString obj = new();
		static Task<int> func()
		{
			return Task.FromResult(42);
		}

		// Act
		await obj.ObjectUpdate("AsyncInt", func, null!, Current.CancellationToken);

		// Assert
		obj.AsyncInt.ShouldBe(42);
	}

	#endregion

	#region ObjectFill ConcurrentDictionary<TKey, TValue>

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithValidInputs_ShouldAddValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";
		const int expectedValue = 42;

		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert
		dictionary.ShouldContainKey(key);
		dictionary[key].ShouldBe(expectedValue);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithNullDictionary_ShouldNotThrow()
	{
		// Arrange
		ConcurrentDictionary<string, int?>? dictionary = null;
		const string key = "testKey";

		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () =>
			await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithNullSemaphore_ShouldAddValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";
		const int expectedValue = 99;

		static Task<int?> func()
		{
			return Task.FromResult<int?>(99);
		}

		// Act
		await dictionary.ObjectFill(key, func, null!, Current.CancellationToken);

		// Assert
		dictionary.ShouldContainKey(key);
		dictionary[key].ShouldBe(expectedValue);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithNullTaskResult_ShouldSetNullValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";

		static Task<int?> func()
		{
			return Task.FromResult<int?>(null);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert
		dictionary.ShouldContainKey(key);
		dictionary[key].ShouldBeNull();
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";

		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert
		dictionary.ShouldNotContainKey(key);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WhenTaskThrows_ShouldReleaseSemaphore()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";

		static Task<int?> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert - Semaphore should be available (count should be 1)
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
		ConcurrentDictionary<int, string?> dictionary = new();
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 6; i++)
		{
			int index = i;
			async Task<string?> func()
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
				return $"Result{index}";
			}
			tasks.Add(dictionary.ObjectFill(index, func, semaphore, Current.CancellationToken));
		}

		// Act
		await Task.WhenAll(tasks);

		// Assert
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
		dictionary.Count.ShouldBe(6);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_ShouldWaitForSemaphore()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";
		using SemaphoreSlim semaphore = new(1, 1);
		await semaphore.WaitAsync(Current.CancellationToken); // Acquire semaphore

		static Task<int?> func()
		{
			return Task.FromResult<int?>(75);
		}

		// Act
		Task fillTask = dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Give a moment to ensure it's waiting
		await Task.Delay(50, Current.CancellationToken);

		// Assert
		dictionary.ShouldNotContainKey(key); // Should not have updated yet

		// Release the semaphore
		semaphore.Release();
		await fillTask;

		dictionary.ShouldContainKey(key);
		dictionary[key].ShouldBe(75);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithCancellationToken_ShouldHandleCancellation()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";
		using SemaphoreSlim semaphore = new(0, 1); // Start with no available slots
		using CancellationTokenSource cts = new();

		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		// Act
		Task fillTask = dictionary.ObjectFill(key, func, semaphore, cts.Token);

		// Give a moment for the task to start waiting on the semaphore
		await Task.Delay(50, Current.CancellationToken);

		// Cancel while waiting
		await cts.CancelAsync();

		// The method should complete without throwing (it catches the exception)
		await fillTask;

		// Assert - Value should not be added becauseof cancellation
		dictionary.ShouldNotContainKey(key);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_MultipleCalls_ShouldUpdateValues()
	{
		// Arrange
		ConcurrentDictionary<int, string?> dictionary = new();
		using SemaphoreSlim semaphore = new(3, 3);

		List<Task> tasks = [];
		for (int i = 0; i < 10; i++)
		{
			int index = i;
			async Task<string?> func()
			{
				await Task.Delay(10, Current.CancellationToken);
				return $"Value{index}";
			}
			tasks.Add(dictionary.ObjectFill(index, func, semaphore, Current.CancellationToken));
		}

		// Act
		await Task.WhenAll(tasks);

		// Assert
		dictionary.Count.ShouldBe(10);
		for (int i = 0; i < 10; i++)
		{
			dictionary[i].ShouldBe($"Value{i}");
		}
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithComplexValueType_ShouldAddValue()
	{
		// Arrange
		ConcurrentDictionary<string, AsyncIntString?> dictionary = new();
		const string key = "testKey";
		AsyncIntString expectedValue = new() { AsyncInt = 100, AsyncString = "TestValue" };

		Task<AsyncIntString?> func()
		{
			return Task.FromResult<AsyncIntString?>(new AsyncIntString
			{
				AsyncInt = 100,
				AsyncString = "TestValue"
			});
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert
		dictionary.ShouldContainKey(key);
		dictionary[key].ShouldNotBeNull();
		dictionary[key]!.AsyncInt.ShouldBe(expectedValue.AsyncInt);
		dictionary[key]!.AsyncString.ShouldBe(expectedValue.AsyncString);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_OverwriteExistingKey_ShouldUpdateValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dictionary = new();
		const string key = "testKey";
		dictionary[key] = 10; // Set initial value

		static Task<int?> func()
		{
			return Task.FromResult<int?>(42);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dictionary.ObjectFill(key, func, semaphore, Current.CancellationToken);

		// Assert
		dictionary[key].ShouldBe(42); // Value should be updated
	}

	#endregion

	#region ObjectFill<T> MemoryStream

	[Theory]
	[InlineData(5)]
	[InlineData(0)]
	public async Task ObjectFill_WithMemoryStream_ShouldWriteDataFromTaskResult(int length)
	{
		// Arrange
		byte[] testData = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
		await using MemoryStream ms = new();
		await using MemoryStream resultMs = new();
		await resultMs.WriteAsync(testData, Current.CancellationToken);
		resultMs.Position = 0;
		Task<MemoryStream> task = Task.FromResult(resultMs);

		// Act
		await ms.ObjectFill(task);

		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), Current.CancellationToken);

		// Assert
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncSemaphore_ShouldWriteData()
	{
		// Arrange
		byte[] testData = [1, 2, 3, 4, 5];
		await using MemoryStream ms = new();
		async Task<MemoryStream> func()
		{
			MemoryStream resultMs = new();
			await resultMs.WriteAsync(testData, Current.CancellationToken);
			resultMs.Position = 0;
			return resultMs;
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await ms.ObjectFill(func, semaphore, Current.CancellationToken);

		ms.Position = 0;
		byte[] buffer = new byte[testData.Length];
		await ms.ReadAsync(buffer.AsMemory(0, testData.Length), Current.CancellationToken);

		// Assert
		buffer.ShouldBe(testData);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithTaskThrowing_ShouldHandleException()
	{
		// Arrange
		await using MemoryStream ms = new();
		Task<MemoryStream> task = Task.FromException<MemoryStream>(new InvalidOperationException("Test exception"));

		// Act
		await ms.ObjectFill(task);

		// Assert
		ms.Length.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		await using MemoryStream ms = new();
		static Task<MemoryStream> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await ms.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		ms.Length.ShouldBe(0);
	}

	#endregion

	#region ObjectFill<T> DataTable

	[Fact]
	public async Task ObjectFill_WithDataTable_ShouldLoadDataFromTaskResult()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		using DataTable resultDt = new();
		resultDt.Columns.Add("Id", typeof(int));
		resultDt.Columns.Add("Name", typeof(string));
		resultDt.Rows.Add(1, "Item1");
		resultDt.Rows.Add(2, "Item2");
		Task<DataTable> task = Task.FromResult(resultDt);

		// Act
		await dt.ObjectFill(task);

		// Assert
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Id"].ShouldBe(1);
		dt.Rows[0]["Name"].ShouldBe("Item1");
		dt.Rows[1]["Id"].ShouldBe(2);
		dt.Rows[1]["Name"].ShouldBe("Item2");
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncNoSemaphore_ShouldLoadData()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		static Task<DataTable> func()
		{
			DataTable resultDt = new();
			resultDt.Columns.Add("Id", typeof(int));
			resultDt.Columns.Add("Name", typeof(string));
			resultDt.Rows.Add(1, "Test1");
			resultDt.Rows.Add(2, "Test2");
			return Task.FromResult(resultDt);
		}

		// Act
		await dt.ObjectFill(func);

		// Assert
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Id"].ShouldBe(1);
		dt.Rows[0]["Name"].ShouldBe("Test1");
		dt.Rows[1]["Id"].ShouldBe(2);
		dt.Rows[1]["Name"].ShouldBe("Test2");
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncNoSemaphore_WhenResultIsNull_ShouldNotModify()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		static Task<DataTable> func()
		{
			return Task.FromResult<DataTable>(null!);
		}

		// Act
		await dt.ObjectFill(func);

		// Assert
		dt.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncSemaphore_ShouldLoadData()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));
		static Task<DataTable> func()
		{
			DataTable resultDt = new();
			resultDt.Columns.Add("Id", typeof(int));
			resultDt.Columns.Add("Name", typeof(string));
			resultDt.Rows.Add(1, "Item1");
			resultDt.Rows.Add(2, "Item2");
			return Task.FromResult(resultDt);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dt.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Id"].ShouldBe(1);
		dt.Rows[0]["Name"].ShouldBe("Item1");
	}

	[Fact]
	public async Task ObjectFill_DataTableWithTaskThrowing_ShouldHandleException()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		Task<DataTable> task = Task.FromException<DataTable>(new InvalidOperationException("Test exception"));

		// Act
		await dt.ObjectFill(task);

		// Assert
		dt.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncThrowing_ShouldHandleException()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		static Task<DataTable> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await dt.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		dt.Rows.Count.ShouldBe(0);
	}

	#endregion

	#region ObjectFill<T> List<T> with Task<List<T>?>

	[Fact]
	public async Task ObjectFill_ListWithTaskList_ShouldAddRangeToList()
	{
		// Arrange
		List<int> list = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(6);
		list.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenResultIsNull_ShouldNotModifyList()
	{
		// Arrange
		List<string> list = ["A", "B"];
		Task<List<string>?> task = Task.FromResult<List<string>?>(null);

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(2);
		list.ShouldBe(["A", "B"]);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1, 2];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await list.ObjectFill(task));

		list.Count.ShouldBe(2);
		list.ShouldBe([1, 2]);
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskList_WhenResultIsEmpty_ShouldNotAddItems()
	{
		// Arrange
		List<int> list = [10, 20];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int>());

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(2);
		list.ShouldBe([10, 20]);
	}

	#endregion

	#region ObjectFill<T> List<T> with Func<Task<List<T>?>>

	[Fact]
	public async Task ObjectFill_ListWithFuncList_ShouldAddRangeToList()
	{
		// Arrange
		List<int> list = [1, 2, 3];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(6);
		list.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncList_WhenResultIsNull_ShouldNotModifyList()
	{
		// Arrange
		List<string> list = ["A", "B"];
		static Task<List<string>?> func()
		{
			return Task.FromResult<List<string>?>(null);
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(2);
		list.ShouldBe(["A", "B"]);
	}

	#endregion

	#region ObjectFill<T> List<T> with Func<Task<IEnumerable<T>?>>

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerable_ShouldAddRangeToList()
	{
		// Arrange
		List<int> list = [1, 2, 3];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>(new List<int> { 4, 5, 6 });
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(6);
		list.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncIEnumerable_WhenResultIsNull_ShouldNotModifyList()
	{
		// Arrange
		List<string> list = ["A", "B"];
		static Task<IEnumerable<string>?> func()
		{
			return Task.FromResult<IEnumerable<string>?>(null);
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(2);
		list.ShouldBe(["A", "B"]);
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Task<List<T>?>

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		// Arrange
		HashSet<string> hashSet = ["A", "B"];
		Task<List<string>?> task = Task.FromResult<List<string>?>(null);

		// Act
		await hashSet.ObjectFill(task);


		// Assert
		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(["A", "B"]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		Task<List<int>?> task = Task.FromException<List<int>?>(new InvalidOperationException("Test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await hashSet.ObjectFill(task));

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe([1, 2]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskList_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 2, 3, 4, 5 });

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe([1, 2, 3, 4, 5]);
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Func<Task<List<T>?>>

	[Fact]
	public async Task ObjectFill_HashSetWithFuncList_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncList_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		// Arrange
		HashSet<string> hashSet = ["A", "B"];
		static Task<List<string>?> func()
		{
			return Task.FromResult<List<string>?>(null);
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(["A", "B"]);
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Task<HashSet<T>?>

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<HashSet<int>?> task = Task.FromResult<HashSet<int>?>(new HashSet<int> { 4, 5, 6 });

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		// Arrange
		HashSet<string> hashSet = ["A", "B"];
		Task<HashSet<string>?> task = Task.FromResult<HashSet<string>?>(null);

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(["A", "B"]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSet_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		Task<HashSet<int>?> task = Task.FromResult<HashSet<int>?>(new HashSet<int> { 2, 3, 4, 5 });

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe([1, 2, 3, 4, 5]);
	}

	#endregion

	#region ObjectFill<T> List<T> with Func, Semaphore, CancellationToken

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldAddRangeToList()
	{
		// Arrange
		List<int> list = [1, 2, 3];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 4, 5, 6 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(6);
		list.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WithNullSemaphore_ShouldWork()
	{
		// Arrange
		List<string> list = ["A", "B"];
		static Task<List<string>?> func()
		{
			return Task.FromResult<List<string>?>(new List<string> { "C", "D" });
		}

		// Act
		await list.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(4);
		list.ShouldBe(["A", "B", "C", "D"]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WhenResultIsNull_ShouldNotModifyList()
	{
		// Arrange
		List<int> list = [10, 20];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(null);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(2);
		list.ShouldBe([10, 20]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		List<int> list = [1, 2];
		static Task<List<int>?> func()
		{
			return Task.FromException<List<int>?>(new InvalidOperationException("Test error"));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await list.ObjectFill(func, semaphore, default));

		list.Count.ShouldBe(2);
		list.ShouldBe([1, 2]);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		// Arrange
		List<int> list = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldReleaseSemaphoreOnException()
	{
		// Arrange
		List<int> list = [1];
		static Task<List<int>?> func()
		{
			throw new InvalidOperationException("Fail");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			List<int> list = [i];
			async Task<List<int>?> func()
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
				return new List<int> { i * 10 };
			}
			tasks.Add(list.ObjectFill(func, semaphore, Current.CancellationToken));
		}

		// Act
		await Task.WhenAll(tasks);

		// Assert
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncSemaphore_WithCancellationToken_ShouldHandleCancellation()
	{
		// Arrange
		List<int> list = [1];
		using CancellationTokenSource cts = new();
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await cts.CancelAsync();

		// Assert
		await Should.ThrowAsync<Exception>(async () => await list.ObjectFill(func, semaphore, cts.Token));
	}

	#endregion

	#region ObjectFill<T> HashSet<T> with Func, Semaphore, CancellationToken

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 4, 5, 6 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(6);
		hashSet.ShouldBe([1, 2, 3, 4, 5, 6]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenResultIsNull_ShouldNotModifyHashSet()
	{
		// Arrange
		HashSet<string> hashSet = ["A", "B"];
		static Task<HashSet<string>?> func()
		{
			return Task.FromResult<HashSet<string>?>(null);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe(["A", "B"]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<HashSet<int>?> func()
		{
			return Task.FromException<HashSet<int>?>(new InvalidOperationException("Test error"));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await hashSet.ObjectFill(func, semaphore, default));

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldBe([1, 2]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WhenResultHasDuplicates_ShouldOnlyAddUnique()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2, 3];
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 2, 3, 4, 5 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(5);
		hashSet.ShouldBe([1, 2, 3, 4, 5]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldReleaseSemaphoreOnSuccess()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 2 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldReleaseSemaphoreOnException()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<HashSet<int>?> func()
		{
			throw new InvalidOperationException("Fail");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
		int concurrentExecutions = 0;
		int maxConcurrent = 0;
		object lockObj = new();
		using SemaphoreSlim semaphore = new(2, 2);

		List<Task> tasks = [];
		for (int i = 0; i < 5; i++)
		{
			HashSet<int> hashSet = [i];
			async Task<HashSet<int>?> func()
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
				return new HashSet<int> { i * 10 };
			}
			tasks.Add(hashSet.ObjectFill(func, semaphore, Current.CancellationToken));
		}

		// Act
		await Task.WhenAll(tasks);

		// Assert
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSemaphore_WithCancellationToken_ShouldHandleCancellation()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		using CancellationTokenSource cts = new();
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 2 });
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await cts.CancelAsync();

		// Assert
		await Should.ThrowAsync<Exception>(async () => await hashSet.ObjectFill(func, semaphore, cts.Token));
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncListSemaphore_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 3, 4 });
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(4);
		hashSet.ShouldBe([1, 2, 3, 4]);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncConcurrentBagSemaphore_ShouldAddRangeToHashSet()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int>([3, 4]));
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await hashSet.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(4);
		hashSet.ShouldContain(1);
		hashSet.ShouldContain(2);
		hashSet.ShouldContain(3);
		hashSet.ShouldContain(4);
	}

	#endregion

	#region ObjectUpdate

	[Theory]
	[InlineData("AsyncInt", 42, 42, "Orig")]
	[InlineData("AsyncString", "Updated", 0, "Updated")]
	public async Task ObjectUpdate_ShouldUpdateSpecifiedProperty(string prop, object value, int expectedInt, string expectedString)
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
		Task<object> task = Task.FromResult(value);

		// Act
		await obj.ObjectUpdate(prop, task);

		// Assert
		obj.AsyncInt.ShouldBe(expectedInt);
		obj.AsyncString.ShouldBe(expectedString);
	}

	[Theory]
	[InlineData("NonExistentProperty", 42)]
	public async Task ObjectUpdate_WithInvalidPropertyName_ShouldHandleException(string prop, object value)
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 0, AsyncString = "Orig" };
		Task<object> task = Task.FromResult(value);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, task));
	}

	[Fact]
	public async Task ObjectUpdate_WithNullObject_ShouldHandleException()
	{
		// Arrange
		AsyncIntString? obj = null;
		Task<int> task = Task.FromResult(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", task));
	}

	[Theory]
	[InlineData(42.5)]
	public async Task ObjectUpdate_WithTypeConversion_ShouldHandleCompatibleTypes(decimal value)
	{
		// Arrange
		AsyncIntString obj = new() { AsyncDecimal = 0 };
		Task<decimal> task = Task.FromResult(value);

		// Act
		await obj.ObjectUpdate("AsyncDecimal", task);

		// Assert
		obj.AsyncDecimal.ShouldBe(value);
	}

	[Theory]
	[InlineData("AsyncInt", 42, 42, "")]
	[InlineData("AsyncString", "Test", 0, "Test")]
	public async Task ObjectUpdate_WithFuncSemaphore_ShouldUpdateProperty(string prop, object value, int expectedInt, string expectedString)
	{
		// Arrange
		AsyncIntString obj = new();
		Task<object> func()
		{
			return Task.FromResult(value);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectUpdate(prop, func, semaphore, Current.CancellationToken);

		// Assert
		obj.AsyncInt.ShouldBe(expectedInt);
		obj.AsyncString.ShouldBe(expectedString);
	}

	[Theory]
	[InlineData("Invalid", 100)]
	[InlineData("NonExistent", "test")]
	public async Task ObjectUpdate_WithFuncSemaphoreInvalidProperty_ShouldHandleException(string prop, object value)
	{
		// Arrange
		AsyncIntString obj = new();
		Task<object> func()
		{
			return Task.FromResult(value);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(prop, func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphoreNullObject_ShouldHandleException()
	{
		// Arrange
		AsyncIntString? obj = null;
		static Task<int> func()
		{
			return Task.FromResult(100);
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphoreThrowing_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new();
		static Task<int> func()
		{
			throw new InvalidOperationException("Test exception");
		}

		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AsyncInt", func, semaphore, Current.CancellationToken));

		// Assert
		obj.AsyncInt.ShouldBe(0);
	}

	#endregion

	#region RunAll

	[Fact]
	public async Task RunAll_ShouldExecuteAllTasksAndReturnResults()
	{
		// Arrange
		List<Func<Task<string>>> tasks =
		[
			() => Task.FromResult("Result1"),
			() => Task.FromResult("Result2"),
			() => Task.FromResult("Result3")
		];

		// Act
		ConcurrentBag<string> results = await tasks.RunAll();

		// Assert
		results.Count.ShouldBe(3);
		results.ShouldContain("Result1");
		results.ShouldContain("Result2");
		results.ShouldContain("Result3");
	}

	[Fact]
	public async Task RunAll_WithSemaphore_ShouldLimitConcurrentExecution()
	{
		// Arrange
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

		// Act
		ConcurrentBag<int> results = await tasks.RunAll(semaphore);

		// Assert
		results.Count.ShouldBe(4);
		results.Sum().ShouldBe(10);
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task RunAll_WithTaskException_ShouldContinueExecution()
	{
		// Arrange
		List<Func<Task<int>>> tasks =
		[
			() => Task.FromResult(1),
			() => Task.FromException<int>(new InvalidOperationException("Test exception")),
			() => Task.FromResult(3)
		];

		// Act
		ConcurrentBag<int> results = await tasks.RunAll();

		// Assert
		results.Count.ShouldBe(2);
		results.Sum().ShouldBe(4);
	}

	[Fact]
	public async Task RunAll_WithTaskExceptionAndBreakOnError_ShouldStopExecution()
	{
		// Arrange
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

		// Act
		await tasks.RunAll(null, cts, true);

		// Assert
		executedTasks.ShouldBeLessThanOrEqualTo(3);
	}
	[Fact]
	public async Task RunAll_WithMixedSuccessAndFailure_ShouldReturnSuccessfulResults()
	{
		// Arrange
		List<Func<Task<int>>> tasks =
		[
				() => Task.FromResult(1),
						() => Task.FromException<int>(new InvalidOperationException("First exception")),
						() => Task.FromResult(3),
						() => Task.FromException<int>(new InvalidOperationException("Second exception")),
						() => Task.FromResult(5)
		];

		// Act
		ConcurrentBag<int> results = await tasks.RunAll();

		// Assert
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
		// Arrange
		int executed = 0;
		List<Func<Task>> tasks = Enumerable.Range(0, count).Select(_ => new Func<Task>(async () =>
		{
			await Task.Delay(10);
			Interlocked.Increment(ref executed);
		})).ToList();

		// Act
		await tasks.RunAll();

		// Assert
		executed.ShouldBe(count);
	}

	[Fact]
	public async Task RunAll_VoidWithBreakOnError_ShouldStopOnException()
	{
		// Arrange
		int executed = 0;
		using CancellationTokenSource cts = new();
		List<Func<Task>> tasks =
		[
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(50); throw new InvalidOperationException("Error"); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10000); }
		];

		// Act
		await tasks.RunAll(null, cts, true);

		// Assert
		executed.ShouldBeLessThanOrEqualTo(3);
	}

	[Fact]
	public async Task RunAll_VoidWithExceptionNoBreak_ShouldContinue()
	{
		// Arrange
		int executed = 0;
		List<Func<Task>> tasks =
		[
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); },
			async () => { Interlocked.Increment(ref executed); throw new InvalidOperationException("Error"); },
			async () => { Interlocked.Increment(ref executed); await Task.Delay(10); }
		];

		// Act
		await tasks.RunAll();

		// Assert
		executed.ShouldBe(3);
	}

	#endregion

	#region RunAsyncWithSemaphore Tests

	[Fact]
	public async Task RunAsyncWithSemaphore_ShouldExecuteTask()
	{
		// Arrange
		Task<int> task = Task.FromResult(42);
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		int result = await task.RunAsyncWithSemaphore(semaphore);

		// Assert
		result.ShouldBe(42);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_Void_ShouldExecuteTask()
	{
		// Arrange
		bool executed = false;
		Task task = Task.Run(() => executed = true);
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await task.RunAsyncWithSemaphore(semaphore);

		// Assert
		executed.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithException_ShouldReleaseSemaphore()
	{
		// Arrange
		Task<int> task = Task.FromException<int>(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		int result = await task.RunAsyncWithSemaphore(semaphore);

		// Assert
		result.ShouldBe(0);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_Void_WithException_ShouldReleaseSemaphore()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await task.RunAsyncWithSemaphore(semaphore);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithBreakOnError_ShouldCancelOnException()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();

		// Act
		await task.RunAsyncWithSemaphore(semaphore, cts, true);

		// Assert
		cts.Token.IsCancellationRequested.ShouldBeTrue();
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithErrorText_ShouldLogWithText()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await task.RunAsyncWithSemaphore(semaphore, null, false, "Custom error message");

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	#endregion

	#region ResultTaskGroup<T> Tests

	[Fact]
	public async Task ResultTaskGroup_RunTasks_ShouldReturnAllResults()
	{
		// Arrange
		List<Task<int>> tasks =
		[
				Task.FromResult(1),
						Task.FromResult(2),
						Task.FromResult(3)
		];
		ResultTaskGroup<int> group = new(tasks);

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(3);
		results.ShouldContain(1);
		results.ShouldContain(2);
		results.ShouldContain(3);
	}

	[Fact]
	public async Task ResultTaskGroup_RunTasks_WithEmptyTasks_ShouldReturnEmptyArray()
	{
		// Arrange
		ResultTaskGroup<string> group = new();

		// Act
		string[] results = await group.RunTasks();

		// Assert
		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task ResultTaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
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

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(6);
		results.All(x => x == 42).ShouldBeTrue();
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task ResultTaskGroup_RunTasks_WithoutSemaphore()
	{
		// Arrange
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

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(6);
		results.All(x => x == 42).ShouldBeTrue();
	}

	[Fact]
	public async Task ResultTaskGroup_RunTasks_WithCancellation_ShouldRespectToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		TaskCompletionSource<int> tcs = new();
		List<Task<int>> tasks =
		[
				Task.FromResult(1),
						tcs.Task // This task will never complete
		];
		ResultTaskGroup<int> group = new(tasks);

		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await group.RunTasks(cts.Token));
	}

	[Fact]
	public async Task ResultTaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
	{
		// Arrange
		List<Task<int>> tasks =
		[
				Task.FromResult(1),
						Task.FromException<int>(new InvalidOperationException("fail"))
		];
		ResultTaskGroup<int> group = new(tasks);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () => await group.RunTasks());
	}

	#endregion

	#region TaskGroup Tests

	[Fact]
	public async Task TaskGroup_RunTasks_ShouldRunAllTasks()
	{
		// Arrange
		int executed = 0;
		List<Task> tasks = Enumerable.Range(0, 4)
				.Select(_ => new Task(() =>
				{
					Task.Delay(10).GetAwaiter().GetResult();
					Interlocked.Increment(ref executed);
				})).ToList();

		TaskGroup group = new(tasks);

		// Act
		await group.RunTasks();

		// Assert
		executed.ShouldBe(4);
	}

	[Fact]
	public async Task TaskGroup_RunTasks_WithEmptyTasks_ShouldNotThrow()
	{
		// Arrange
		TaskGroup group = new();

		// Act & Assert
		await Should.NotThrowAsync(group.RunTasks());
	}

	[Fact]
	public async Task TaskGroup_RunTasks_WithSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
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

		// Act
		await group.RunTasks();

		// Assert
		maxConcurrent.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task TaskGroup_RunTasks_WhenTaskThrows_ShouldPropagateException()
	{
		// Arrange
		List<Task> tasks =
		[
				Task.CompletedTask,
						Task.FromException(new InvalidOperationException("fail"))
		];
		TaskGroup group = new(tasks);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(group.RunTasks());
	}

	#endregion

	#region ObjectFill with Task<T> (direct Task, not Func)

	[Fact]
	public async Task ObjectFill_WithDirectTaskT_ShouldCopyProperties()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1, AsyncString = "original" };
		Task<AsyncIntString> task = Task.FromResult(new AsyncIntString { AsyncInt = 42, AsyncString = "updated" });

		// Act
		await obj.ObjectFill(task);

		// Assert
		obj.AsyncInt.ShouldBe(42);
		obj.AsyncString.ShouldBe("updated");
	}

	[Fact]
	public async Task ObjectFill_WithDirectTaskT_WhenTaskResultIsNull_ShouldNotModifyObject()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1, AsyncString = "original" };
		Task<AsyncIntString?> task = Task.FromResult<AsyncIntString?>(null);

		// Act
		await obj.ObjectFill(task);

		// Assert
		obj.AsyncInt.ShouldBe(1);
		obj.AsyncString.ShouldBe("original");
	}

	[Fact]
	public async Task ObjectFill_WithDirectTaskT_WhenObjectIsNull_ShouldNotThrow()
	{
		// Arrange
		AsyncIntString? obj = null;
		Task<AsyncIntString?> task = Task.FromResult<AsyncIntString?>(new AsyncIntString { AsyncInt = 42 });

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_WithDirectTaskT_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		Task<AsyncIntString> task = Task.FromException<AsyncIntString>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
	}

	#endregion

	#region ObjectFill ConcurrentDictionary with Task<TValue?>

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTask_ShouldAddValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		const string key = "testKey";
		Task<int?> task = Task.FromResult<int?>(42);

		// Act
		await dict.ObjectFill(key, task);

		// Assert
		dict.ShouldContainKey(key);
		dict[key].ShouldBe(42);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTask_WhenResultIsNull_ShouldSetNullValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		const string key = "testKey";
		Task<int?> task = Task.FromResult<int?>(null);

		// Act
		await dict.ObjectFill(key, task);

		// Assert
		dict.ShouldContainKey(key);
		dict[key].ShouldBeNull();
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTask_WhenDictionaryIsNull_ShouldNotThrow()
	{
		// Arrange
		ConcurrentDictionary<string, int?>? dict = null;
		const string key = "testKey";
		Task<int?> task = Task.FromResult<int?>(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await dict.ObjectFill(key, task));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTask_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		const string key = "testKey";
		Task<int?> task = Task.FromException<int?>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await dict.ObjectFill(key, task));
		dict.ShouldNotContainKey(key);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTaskFunc_ShouldAddValue()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		const string key = "testKey";

		Task<int?> func() => Task.FromResult<int?>(42);

		// Act
		await dict.ObjectFill(key, func);

		// Assert
		dict.ShouldContainKey(key);
		dict[key].ShouldBe(42);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTaskFunc_WhenDictionaryIsNull_ShouldNotThrow()
	{
		// Arrange
		ConcurrentDictionary<string, int?>? dict = null;
		const string key = "testKey";

		Task<int?> func() => Task.FromResult<int?>(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await dict.ObjectFill(key, func));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentDictionary_WithDirectTaskFunc_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		ConcurrentDictionary<string, int?> dict = new();
		const string key = "testKey";

		Task<int?> func() => Task.FromException<int?>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await dict.ObjectFill(key, func));
		dict.ShouldNotContainKey(key);
	}

	#endregion

	#region ObjectFill DataTable with Task<DataTable>

	[Fact]
	public async Task ObjectFill_DataTable_WithDirectTask_ShouldLoadData()
	{
		// Arrange
		DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Columns.Add("Name", typeof(string));

		DataTable resultTable = new();
		resultTable.Columns.Add("Id", typeof(int));
		resultTable.Columns.Add("Name", typeof(string));
		resultTable.Rows.Add(1, "Test");
		resultTable.Rows.Add(2, "Data");

		Task<DataTable> task = Task.FromResult(resultTable);

		// Act
		await dt.ObjectFill(task);

		// Assert
		dt.Rows.Count.ShouldBe(2);
		dt.Rows[0]["Id"].ShouldBe(1);
		dt.Rows[0]["Name"].ShouldBe("Test");
	}

	[Fact]
	public async Task ObjectFill_DataTable_WithDirectTask_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		DataTable dt = new();
		Task<DataTable> task = Task.FromException<DataTable>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await dt.ObjectFill(task));
	}

	#endregion

	#region ObjectFill MemoryStream with Task<MemoryStream>

	[Fact]
	public async Task ObjectFill_MemoryStream_WithDirectTask_ShouldWriteData()
	{
		// Arrange
		using MemoryStream ms = new();
		byte[] data = [1, 2, 3, 4, 5];
		using MemoryStream resultMs = new(data);
		Task<MemoryStream> task = Task.FromResult(resultMs);

		// Act
		await ms.ObjectFill(task);

		// Assert
		ms.Length.ShouldBe(5);
		ms.ToArray().ShouldBe(data);
	}

	[Fact]
	public async Task ObjectFill_MemoryStream_WithDirectTask_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		using MemoryStream ms = new();
		Task<MemoryStream> task = Task.FromException<MemoryStream>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await ms.ObjectFill(task));
	}

	#endregion

	#region ObjectUpdate with Task<TTask>

	[Fact]
	public async Task ObjectUpdate_WithDirectTask_ShouldUpdateProperty()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1, AsyncString = "original" };
		Task<int> task = Task.FromResult(42);

		// Act
		await obj.ObjectUpdate(nameof(AsyncIntString.AsyncInt), task);

		// Assert
		obj.AsyncInt.ShouldBe(42);
	}

	[Fact]
	public async Task ObjectUpdate_WithDirectTask_WithInvalidPropertyName_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		Task<int> task = Task.FromResult(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("InvalidProperty", task));
	}

	[Fact]
	public async Task ObjectUpdate_WithDirectTask_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		Task<int> task = Task.FromException<int>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(nameof(AsyncIntString.AsyncInt), task));
	}

	[Fact]
	public async Task ObjectUpdate_WithFunc_WithInvalidPropertyName_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };

		static Task<int> func() => Task.FromResult(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("InvalidProperty", func));
	}

	[Fact]
	public async Task ObjectUpdate_WithFunc_WhenTaskThrows_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };

		static Task<int> func() => Task.FromException<int>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(nameof(AsyncIntString.AsyncInt), func));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphore_WithInvalidPropertyName_ShouldHandleException()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<int> func() => Task.FromResult(42);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("InvalidProperty", func, semaphore));
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncSemaphore_WhenTaskThrows_ShouldHandleExceptionAndReleaseSemaphore()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<int> func() => Task.FromException<int>(new InvalidOperationException("test error"));

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate(nameof(AsyncIntString.AsyncInt), func, semaphore));
		semaphore.CurrentCount.ShouldBe(1);
	}

	#endregion

	#region RunAsyncWithSemaphore with errorText

	[Fact]
	public async Task RunAsyncWithSemaphore_WithErrorText_ShouldIncludeErrorTextInLog()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		Task task = Task.FromException(new InvalidOperationException("test error"));
		const string errorText = "Custom error message";

		// Act & Assert
		await Should.NotThrowAsync(async () => await task.RunAsyncWithSemaphore(semaphore, errorText: errorText));
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithBreakOnError_ShouldCancel()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();
		Task task = Task.FromException(new InvalidOperationException("test error"));

		// Act
		await task.RunAsyncWithSemaphore(semaphore, cts, breakOnError: true);

		// Assert
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithErrorTextAndBreakOnError_ShouldCancelAndIncludeErrorText()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();
		Task task = Task.FromException(new InvalidOperationException("test error"));
		const string errorText = "Custom error message";

		// Act
		await task.RunAsyncWithSemaphore(semaphore, cts, breakOnError: true, errorText: errorText);

		// Assert
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithErrorText_ShouldIncludeErrorTextInLog()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		Task<int> task = Task.FromException<int>(new InvalidOperationException("test error"));
		const string errorText = "Custom error message";

		// Act
		int? result = await task.RunAsyncWithSemaphore(semaphore, errorText: errorText);

		// Assert
		result.ShouldBe(default(int)); // Returns default(int) not null
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithBreakOnError_ShouldCancel()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();
		Task<int> task = Task.FromException<int>(new InvalidOperationException("test error"));

		// Act
		int? result = await task.RunAsyncWithSemaphore(semaphore, cts, breakOnError: true);

		// Assert
		result.ShouldBe(default(int)); // Returns default(int) not null
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithErrorTextAndBreakOnError_ShouldCancelAndIncludeErrorText()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();
		Task<int> task = Task.FromException<int>(new InvalidOperationException("test error"));
		const string errorText = "Custom error message";

		// Act
		int? result = await task.RunAsyncWithSemaphore(semaphore, cts, breakOnError: true, errorText: errorText);

		// Assert
		result.ShouldBe(default(int)); // Returns default(int) not null
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_Success_ShouldReturnResult()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		Task<int> task = Task.FromResult(42);

		// Act
		int? result = await task.RunAsyncWithSemaphore(semaphore);

		// Assert
		result.ShouldBe(42);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_Success_ShouldComplete()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		Task task = Task.CompletedTask;

		// Act & Assert
		await Should.NotThrowAsync(async () => await task.RunAsyncWithSemaphore(semaphore));
		semaphore.CurrentCount.ShouldBe(1);
	}

	#endregion

	#region Additional Edge Cases

	[Fact]
	public async Task ObjectFill_IList_WithNullObjectAndSemaphore_ShouldNotAdd()
	{
		// Arrange
		IList<int?>? list = null;
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<int?> func() => Task.FromResult<int?>(42);

		// Act
		await list!.ObjectFill(func, semaphore, cancellationToken: Current.CancellationToken);

		// Assert
		list.ShouldBeNull();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSet_WithNullObjectAndSemaphore_ShouldNotAdd()
	{
		// Arrange
		HashSet<int?>? set = null;
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<int?> func() => Task.FromResult<int?>(42);

		// Act
		await set!.ObjectFill(func, semaphore, cancellationToken: Current.CancellationToken);

		// Assert
		set.ShouldBeNull();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBag_WithNullObjectAndSemaphore_ShouldNotAdd()
	{
		// Arrange
		ConcurrentBag<int?>? bag = null;
		using SemaphoreSlim semaphore = new(1, 1);

		static Task<int?> func() => Task.FromResult<int?>(42);

		// Act
		await bag!.ObjectFill(func, semaphore, cancellationToken: Current.CancellationToken);

		// Assert
		bag.ShouldBeNull();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAll_WithCancellationDuringExecution_ShouldStopProcessing()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(2, 2);
		using CancellationTokenSource cts = new();

		List<Func<Task<int>>> tasks =
		[
			async () =>
			{
				await Task.Delay(10);
				return 1;
			},
			async () =>
			{
				await Task.Delay(10);
				throw new InvalidOperationException("error");
			},
			async () =>
			{
				await Task.Delay(10);
				return 3;
			}
		];

		// Act
		ConcurrentBag<int> _ = await tasks.RunAll(semaphore, cts, breakOnError: true);

		// Assert
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(2);
	}

	[Fact]
	public async Task RunAll_WithoutBreakOnError_ShouldContinueOnException()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);

		List<Func<Task<int>>> tasks =
		[
			() => Task.FromResult(1),
			() => Task.FromException<int>(new InvalidOperationException("error")),
			() => Task.FromResult(3)
		];

		// Act
		ConcurrentBag<int> results = await tasks.RunAll(semaphore, breakOnError: false);

		// Assert
		results.Count.ShouldBe(2);
		results.ShouldContain(1);
		results.ShouldContain(3);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAll_Void_WithCancellationDuringExecution_ShouldStopProcessing()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(2, 2);
		using CancellationTokenSource cts = new();
		int executionCount = 0;

		List<Func<Task>> tasks =
		[
			async () =>
			{
				Interlocked.Increment(ref executionCount);
				await Task.Delay(10);
			},
			async () =>
			{
				Interlocked.Increment(ref executionCount);
				await Task.Delay(10);
				throw new InvalidOperationException("error");
			},
			async () =>
			{
				Interlocked.Increment(ref executionCount);
				await Task.Delay(10);
			}
		];

		// Act
		await tasks.RunAll(semaphore, cts, breakOnError: true);

		// Assert
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(2);
	}

	[Fact]
	public async Task RunAll_Void_WithoutBreakOnError_ShouldContinueOnException()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		int executionCount = 0;

		List<Func<Task>> tasks =
		[
			() =>
			{
				Interlocked.Increment(ref executionCount);
				return Task.CompletedTask;
			},
			() =>
			{
				Interlocked.Increment(ref executionCount);
				return Task.FromException(new InvalidOperationException("error"));
			},
			() =>
			{
				Interlocked.Increment(ref executionCount);
				return Task.CompletedTask;
			}
		];

		// Act
		await tasks.RunAll(semaphore, breakOnError: false);

		// Assert
		executionCount.ShouldBe(3);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ResultTaskGroup_WithCancellationToken_ShouldRespectCancellation()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<Task<int>> tasks = [Task.FromResult(1), Task.FromResult(2)];
		ResultTaskGroup<int> group = new(tasks);

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await group.RunTasks(cts.Token));
	}

	[Fact]
	public async Task TaskGroup_WithCancellationToken_ShouldRespectCancellation()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<Task> tasks = [Task.CompletedTask, Task.CompletedTask];
		TaskGroup group = new(tasks);

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(async () => await group.RunTasks(cts.Token));
	}

	#endregion

	#region ObjectFill<T> Simple Type Tests (IsSimpleType branch coverage)

	[Fact]
	public async Task ObjectFill_WithSimpleTypeTask_ShouldNotCopyProperties()
	{
		// Arrange - Test that simple types don't go through CopyPropertiesTo path
		string obj = "original";
		Task<string> task = Task.FromResult("updated");

		// Act
		await obj.ObjectFill(task);

		// Assert - obj remains unchanged as simple types don't support property copying
		obj.ShouldBe("original");
	}

	[Fact]
	public async Task ObjectFill_WithSimpleTypeFuncTask_ShouldNotCopyProperties()
	{
		// Arrange - Test Func<Task<T>> overload with simple type
		string obj = "original";
		static Task<string> func()
		{
			return Task.FromResult("updated");
		}

		// Act
		await obj.ObjectFill(func);

		// Assert - obj remains unchanged
		obj.ShouldBe("original");
	}

	[Fact]
	public async Task ObjectFill_WithSimpleTypeFuncTaskAndSemaphore_ShouldNotCopyProperties()
	{
		// Arrange - Test Func<Task<T>> with semaphore overload with simple type
		string obj = "original";
		static Task<string> func()
		{
			return Task.FromResult("updated");
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert - obj remains unchanged
		obj.ShouldBe("original");
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_WithSimpleTypeNullString_ShouldNotCopyProperties()
	{
		// Arrange - Test with null simple type
		string? obj = null;
		Task<string?> task = Task.FromResult<string?>("value");

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_WithSimpleTypeFuncTaskNullSemaphore_ShouldWork()
	{
		// Arrange - Test Func<Task<T>> with null semaphore and simple type
		string obj = "test";
		static Task<string> func()
		{
			return Task.FromResult("new");
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectFill(func, null));
	}

	#endregion

	#region ObjectFill Additional Collection Coverage

	[Fact]
	public async Task ObjectFill_IListWithNullTask_ShouldNotThrow()
	{
		// Arrange
		IList<int?> list = new List<int?> { 1, 2 };
		Task<int?> task = Task.FromResult<int?>(null);

		// Act & Assert
		await Should.NotThrowAsync(async () => await list.ObjectFill(task));
		list.Count.ShouldBe(3);
		list[2].ShouldBeNull();
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncTask_ShouldAddItem()
	{
		// Arrange
		IList<string?> list = new List<string?> { "a" };
		static Task<string?> func()
		{
			return Task.FromResult<string?>("b");
		}

		// Act
		await list.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(2);
		list[1].ShouldBe("b");
	}

	[Fact]
	public async Task ObjectFill_IListWithNullObject_ShouldNotThrow()
	{
		// Arrange
		IList<int>? list = null;
		static Task<int> func()
		{
			return Task.FromResult(5);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await list!.ObjectFill(func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_HashSetWithNullObject_ShouldNotThrow()
	{
		// Arrange
		HashSet<int>? hashSet = null;
		static Task<int> func()
		{
			return Task.FromResult(5);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await hashSet!.ObjectFill(func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithNullObject_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<int> func()
		{
			return Task.FromResult(5);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag!.ObjectFill(func, semaphore, Current.CancellationToken));
	}

	[Fact]
	public async Task ObjectFill_ListWithNullSemaphore_ShouldWork()
	{
		// Arrange
		List<int> list = [1, 2];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 3, 4 });
		}

		// Act
		await list.ObjectFill(func, null, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(4);
		list.ShouldContain(3);
		list.ShouldContain(4);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithSemaphoreAndNullSemaphore_ShouldWork()
	{
		// Arrange
		HashSet<int> hashSet = [1, 2];
		static Task<HashSet<int>?> func()
		{
			return Task.FromResult<HashSet<int>?>(new HashSet<int> { 3, 4 });
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(4);
		hashSet.ShouldContain(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithIEnumerableAndNullSemaphore_ShouldWork()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>(new[] { 2, 3 });
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithConcurrentBagAndNullSemaphore_ShouldWork()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int> { 2, 3 });
		}

		// Act
		await hashSet.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithIEnumerableAndNullSemaphore_ShouldWork()
	{
		// Arrange
		ConcurrentBag<int>? bag = new();
		bag.Add(1);
		static Task<IEnumerable<int>> func()
		{
			return Task.FromResult<IEnumerable<int>>(new[] { 2, 3 });
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithConcurrentBagAndNullSemaphore_ShouldWork()
	{
		// Arrange
		ConcurrentBag<int>? bag = new();
		bag.Add(1);
		static Task<ConcurrentBag<int>> func()
		{
			return Task.FromResult(new ConcurrentBag<int> { 2, 3 });
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithListAndNullSemaphore_ShouldWork()
	{
		// Arrange
		ConcurrentBag<int>? bag = new();
		bag.Add(1);
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2, 3 });
		}

		// Act
		await bag.ObjectFill(func, null!, Current.CancellationToken);

		// Assert
		bag.Count.ShouldBe(3);
	}

	#endregion

	#region ObjectUpdate Additional Coverage

	[Fact]
	public async Task ObjectUpdate_WithInvalidPropertyName_ShouldThrowAndLogError()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 10 };
		Task<int> task = Task.FromResult(20);

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("InvalidProperty", task));
		// The method catches and logs the exception, so it won't throw
		obj.AsyncInt.ShouldBe(10); // Value should remain unchanged
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncTask_ShouldUpdateProperty()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 5, AsyncString = "old" };
		static Task<string> func()
		{
			return Task.FromResult("new");
		}

		// Act
		await obj.ObjectUpdate("AsyncString", func);

		// Assert
		obj.AsyncString.ShouldBe("new");
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncTaskAndInvalidPropertyName_ShouldLogError()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 10 };
		static Task<int> func()
		{
			return Task.FromResult(20);
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("NonExistent", func));
		obj.AsyncInt.ShouldBe(10);
	}

	[Fact]
	public async Task ObjectUpdate_WithSemaphoreAndValidProperty_ShouldUpdate()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncDecimal = 1.5m };
		static Task<decimal> func()
		{
			return Task.FromResult(3.7m);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectUpdate("AsyncDecimal", func, semaphore, Current.CancellationToken);

		// Assert
		obj.AsyncDecimal.ShouldBe(3.7m);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectUpdate_WithSemaphoreAndInvalidProperty_ShouldReleaseSemaphore()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncInt = 1 };
		static Task<int> func()
		{
			return Task.FromResult(2);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectUpdate("BadProperty", func, semaphore, Current.CancellationToken);

		// Assert
		semaphore.CurrentCount.ShouldBe(1); // Semaphore should be released
	}

	[Fact]
	public async Task ObjectUpdate_WithNullSemaphore_ShouldWork()
	{
		// Arrange
		AsyncIntString obj = new() { AsyncFloat = 1.0f };
		static Task<float> func()
		{
			return Task.FromResult(2.5f);
		}

		// Act
		await obj.ObjectUpdate("AsyncFloat", func, null!, Current.CancellationToken);

		// Assert
		obj.AsyncFloat.ShouldBe(2.5f);
	}

	[Fact]
	public async Task ObjectUpdate_WithTaskAndNoProperties_ShouldLogErrorAndNotThrow()
	{
		// Arrange - Test type with no properties triggers "Unable to get properties" exception
		EmptyClass obj = new();
		Task<int> task = Task.FromResult(42);

		// Act & Assert - Should not throw, exception is caught and logged
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AnyProperty", task));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncTaskAndNoProperties_ShouldLogErrorAndNotThrow()
	{
		// Arrange - Test type with no properties triggers "Unable to get properties" exception
		EmptyClass obj = new();
		static Task<int> func()
		{
			return Task.FromResult(42);
		}

		// Act & Assert - Should not throw, exception is caught and logged
		await Should.NotThrowAsync(async () => await obj.ObjectUpdate("AnyProperty", func));
	}

	[Fact]
	public async Task ObjectUpdate_WithFuncTaskSemaphoreAndNoProperties_ShouldLogErrorAndReleaseSemaphore()
	{
		// Arrange - Test type with no properties triggers "Unable to get properties" exception
		EmptyClass obj = new();
		static Task<int> func()
		{
			return Task.FromResult(42);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await obj.ObjectUpdate("AnyProperty", func, semaphore, Current.CancellationToken);

		// Assert - Should not throw, exception is caught and logged, semaphore is released
		semaphore.CurrentCount.ShouldBe(1);
	}

	#endregion

	#region RunAll Additional Coverage

	[Fact]
	public async Task RunAll_WithNullSemaphore_ShouldRunAllTasks()
	{
		// Arrange
		int count = 0;
		List<Func<Task<int>>> tasks =
		[
			async () => { await Task.Delay(1); return Interlocked.Increment(ref count); },
			async () => { await Task.Delay(1); return Interlocked.Increment(ref count); },
			async () => { await Task.Delay(1); return Interlocked.Increment(ref count); }
		];

		// Act
		ConcurrentBag<int> results = await tasks.RunAll(null, null, false);

		// Assert
		results.Count.ShouldBe(3);
		count.ShouldBe(3);
	}

	[Fact]
	public async Task RunAll_NonGenericWithNullSemaphore_ShouldRunAllTasks()
	{
		// Arrange
		int count = 0;
		List<Func<Task>> tasks =
		[
			async () => { await Task.Delay(1); Interlocked.Increment(ref count); },
			async () => { await Task.Delay(1); Interlocked.Increment(ref count); },
			async () => { await Task.Delay(1); Interlocked.Increment(ref count); }
		];

		// Act
		await tasks.RunAll(null, null, false);

		// Assert
		count.ShouldBe(3);
	}

	[Fact]
	public async Task RunAll_WithBreakOnErrorFalse_ShouldContinueAfterError()
	{
		// Arrange
		int successCount = 0;
		using SemaphoreSlim semaphore = new(2, 2);
		List<Func<Task<int>>> tasks =
		[
			async () => { await Task.Delay(1); Interlocked.Increment(ref successCount); return 1; },
			async () => { await Task.Delay(1); throw new InvalidOperationException("Test error"); },
			async () => { await Task.Delay(10); Interlocked.Increment(ref successCount); return 3; }
		];

		// Act
		ConcurrentBag<int> results = await tasks.RunAll(semaphore, null, breakOnError: false);

		// Assert
		successCount.ShouldBe(2);
		results.Count.ShouldBe(2); // Only successful tasks add results
	}

	[Fact]
	public async Task RunAll_NonGenericWithBreakOnErrorFalse_ShouldContinueAfterError()
	{
		// Arrange
		int successCount = 0;
		using SemaphoreSlim semaphore = new(2, 2);
		List<Func<Task>> tasks =
		[
			async () => { await Task.Delay(1); Interlocked.Increment(ref successCount); },
			async () => { await Task.Delay(1); throw new InvalidOperationException("Test error"); },
			async () => { await Task.Delay(10); Interlocked.Increment(ref successCount); }
		];

		// Act
		await tasks.RunAll(semaphore, null, breakOnError: false);

		// Assert
		successCount.ShouldBe(2);
	}

	#endregion

	#region RunAsyncWithSemaphore Additional Coverage

	[Fact]
	public async Task RunAsyncWithSemaphore_WithNullCancellationTokenSource_ShouldWork()
	{
		// Arrange
		Task task = Task.CompletedTask;
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await task.RunAsyncWithSemaphore(semaphore, null, false, null);

		// Assert
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithErrorTextParameter_ShouldLogWithErrorText()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await task.RunAsyncWithSemaphore(semaphore, null, false, "Custom error message"));
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithNullCancellationTokenSource_ShouldReturnResult()
	{
		// Arrange
		Task<int> task = Task.FromResult(42);
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		int result = await task.RunAsyncWithSemaphore(semaphore, null, false, null);

		// Assert
		result.ShouldBe(42);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithErrorTextParameter_ShouldReturnDefault()
	{
		// Arrange
		Task<int> task = Task.FromException<int>(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		int result = await task.RunAsyncWithSemaphore(semaphore, null, false, "Error context");

		// Assert
		result.ShouldBe(default(int));
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphoreT_WithBreakOnErrorTrue_ShouldCancelTokenSource()
	{
		// Arrange
		Task<int> task = Task.FromException<int>(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);
		using CancellationTokenSource cts = new();

		// Act
		int result = await task.RunAsyncWithSemaphore(semaphore, cts, breakOnError: true, null);

		// Assert
		result.ShouldBe(default(int));
		cts.Token.IsCancellationRequested.ShouldBeTrue();
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithNullErrorText_ShouldLogCorrectly()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await task.RunAsyncWithSemaphore(semaphore, null, false, null));
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task RunAsyncWithSemaphore_WithWhitespaceErrorText_ShouldLogWithoutErrorText()
	{
		// Arrange
		Task task = Task.FromException(new InvalidOperationException("Test"));
		using SemaphoreSlim semaphore = new(1, 1);

		// Act & Assert
		await Should.NotThrowAsync(async () => await task.RunAsyncWithSemaphore(semaphore, null, false, "   "));
		semaphore.CurrentCount.ShouldBe(1);
	}

	#endregion

	#region ResultTaskGroup and TaskGroup Additional Coverage

	[Fact]
	public async Task ResultTaskGroup_WithEmptyTaskList_ShouldReturnEmptyArray()
	{
		// Arrange
		ResultTaskGroup<int> group = new();

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.ShouldBeEmpty();
	}

	[Fact]
	public async Task TaskGroup_WithEmptyTaskList_ShouldReturnImmediately()
	{
		// Arrange
		TaskGroup group = new();

		// Act & Assert
		await Should.NotThrowAsync(async () => await group.RunTasks());
		group.Tasks.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ResultTaskGroup_WithNullSemaphore_ShouldRunAllTasks()
	{
		// Arrange
		List<Task<int>> tasks = [new Task<int>(() => 1), new Task<int>(() => 2), new Task<int>(() => 3)];
		ResultTaskGroup<int> group = new(tasks, null);

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(3);
		results.ShouldContain(1);
		results.ShouldContain(2);
		results.ShouldContain(3);
	}

	[Fact]
	public async Task TaskGroup_WithNullSemaphore_ShouldRunAllTasks()
	{
		// Arrange
		int count = 0;
		List<Task> tasks =
		[
			new Task(() => Interlocked.Increment(ref count)),
			new Task(() => Interlocked.Increment(ref count)),
			new Task(() => Interlocked.Increment(ref count))
		];
		TaskGroup group = new(tasks, null);

		// Act
		await group.RunTasks();

		// Assert
		count.ShouldBe(3);
		group.Tasks.Count.ShouldBe(0); // Tasks should be cleared
	}

	[Fact]
	public async Task ResultTaskGroup_WithAlreadyStartedTasks_ShouldHandleCorrectly()
	{
		// Arrange
		Task<int> task1 = Task.FromResult(1);
		Task<int> task2 = Task.FromResult(2);
		List<Task<int>> tasks = [task1, task2];
		ResultTaskGroup<int> group = new(tasks, null);

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(2);
		results.ShouldContain(1);
		results.ShouldContain(2);
	}

	[Fact]
	public async Task TaskGroup_WithAlreadyStartedTasks_ShouldHandleCorrectly()
	{
		// Arrange
		Task task1 = Task.CompletedTask;
		Task task2 = Task.CompletedTask;
		List<Task> tasks = [task1, task2];
		TaskGroup group = new(tasks, null);

		// Act
		await group.RunTasks();

		// Assert
		group.Tasks.Count.ShouldBe(0); // Tasks should be cleared
	}

	[Fact]
	public async Task ResultTaskGroup_WithSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		int maxConcurrent = 0;
		int currentConcurrent = 0;
#pragma warning disable S2925 // Do not use 'Thread.Sleep()' in a test.
		List<Task<int>> tasks =
		[
			new Task<int>(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
				return 1;
			}),
			new Task<int>(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
				return 2;
			}),
			new Task<int>(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
				return 3;
			})
		];
#pragma warning restore S2925 // Do not use 'Thread.Sleep()' in a test.
		ResultTaskGroup<int> group = new(tasks, semaphore);

		// Act
		int[] results = await group.RunTasks();

		// Assert
		results.Length.ShouldBe(3);
		maxConcurrent.ShouldBe(1); // With semaphore(1,1), only 1 should run at a time
	}

	[Fact]
	public async Task TaskGroup_WithSemaphore_ShouldLimitConcurrency()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);
		int maxConcurrent = 0;
		int currentConcurrent = 0;
#pragma warning disable S2925 // Do not use 'Thread.Sleep()' in a test.
		List<Task> tasks =
		[
			new Task(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
			}),
			new Task(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
			}),
			new Task(() =>
			{
				int current = Interlocked.Increment(ref currentConcurrent);
				if (current > maxConcurrent) maxConcurrent = current;
				Thread.Sleep(10);
				Interlocked.Decrement(ref currentConcurrent);
			})
		];
		TaskGroup group = new(tasks, semaphore);
#pragma warning restore S2925 // Do not use 'Thread.Sleep()' in a test.
		// Act
		await group.RunTasks();

		// Assert
		maxConcurrent.ShouldBe(1);
		group.Tasks.Count.ShouldBe(0);
	}

	[Fact]
	public async Task ResultTaskGroup_WithNullCancellationToken_ShouldUseNew()
	{
		// Arrange
		List<Task<int>> tasks = [Task.FromResult(1), Task.FromResult(2)];
		ResultTaskGroup<int> group = new(tasks);

		// Act
		int[] results = await group.RunTasks(null);

		// Assert
		results.Length.ShouldBe(2);
	}

	[Fact]
	public async Task TaskGroup_WithNullCancellationToken_ShouldUseNew()
	{
		// Arrange
		List<Task> tasks = [Task.CompletedTask, Task.CompletedTask];
		TaskGroup group = new(tasks);

		// Act & Assert
		await Should.NotThrowAsync(async () => await group.RunTasks(null));
	}

	#endregion

	#region Additional ObjectFill Coverage for Uncovered Branches

	[Fact]
	public async Task ObjectFill_FuncTaskWithSemaphoreAndComplexType_ShouldCopyProperties()
	{
		// Arrange - Test the Func<Task<T>> with semaphore overload with complex type to cover line 86-88
		AsyncIntString obj = new() { AsyncInt = 1, AsyncString = "orig" };
		static Task<AsyncIntString> func()
		{
			return Task.FromResult(new AsyncIntString { AsyncInt = 10, AsyncString = "new" });
		}
		using SemaphoreSlim semaphore = new(2, 2);

		// Act
		await obj.ObjectFill(func, semaphore);

		// Assert
		obj.AsyncInt.ShouldBe(10);
		obj.AsyncString.ShouldBe("new");
		semaphore.CurrentCount.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_ListWithIListOverload_ShouldAddNonNullValue()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<int> list = new List<int> { 1 };
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		Task<int> task = Task.FromResult(2);

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(2);
		list[1].ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_IListWithFuncAndSemaphore_ShouldAdd()
	{
		// Arrange - Test IList with semaphore (covers line 139)
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<int> list = new List<int> { 1, 2 };
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<int> func()
		{
			return Task.FromResult(3);
		}
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		await list.ObjectFill(func, semaphore, Current.CancellationToken);

		// Assert
		list.Count.ShouldBe(3);
		list[2].ShouldBe(3);
		semaphore.CurrentCount.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_IListWithTask_WhenExceptionThrown_ShouldLogAndNotThrow()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		Task<string?> task = Task.FromException<string?>(new InvalidOperationException("Test exception"));

		// Act & Assert - Should not throw, exception is caught and logged
		await list.ObjectFill(task);

		list.Count.ShouldBe(0); // No item added due to exception
	}

	[Fact]
	public async Task ObjectFill_IListWithFunc_WhenExceptionThrown_ShouldLogAndNotThrow()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?>();
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<string?> func() => Task.FromException<string?>(new InvalidOperationException("Test exception"));

		// Act & Assert - Should not throw, exception is caught and logged
		await list.ObjectFill(func);

		list.Count.ShouldBe(0); // No item added due to exception
	}

	[Fact]
	public async Task ObjectFill_IListWithFunc_ShouldAddItem()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<int> list = new List<int> { 1, 2 };
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<int> func()
		{
			return Task.FromResult(3);
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(3);
		list[2].ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_IListWithFunc_NullableType_ShouldAddNullItem()
	{
		// Arrange
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
		IList<string?> list = new List<string?> { "a", "b" };
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		static Task<string?> func()
		{
			return Task.FromResult<string?>(null);
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(3);
		list[2].ShouldBeNull();
	}

	[Fact]
	public async Task ObjectFill_ListWithTaskListAndNullResult_ShouldNotModify()
	{
		// Arrange
		List<int> list = [1, 2];
		Task<List<int>?> task = Task.FromResult<List<int>?>(null);

		// Act
		await list.ObjectFill(task);

		// Assert
		list.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_ListWithFuncListAndNonNullResult_ShouldAddRange()
	{
		// Arrange
		List<int> list = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 2, 3 });
		}

		// Act
		await list.ObjectFill(func);

		// Assert
		list.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskSingleItem_ShouldAdd()
	{
		// Arrange
		HashSet<string?> hashSet = ["a"];
		Task<string?> task = Task.FromResult<string?>("b");

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(2);
		hashSet.ShouldContain("b");
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncSingleItem_ShouldAdd()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		static Task<int> func()
		{
			return Task.FromResult(2);
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithTaskHashSetAndNullResult_ShouldNotModify()
	{
		// Arrange
		HashSet<int> hashSet = [1];
		Task<HashSet<int>?> task = Task.FromResult<HashSet<int>?>(null);

		// Act
		await hashSet.ObjectFill(task);

		// Assert
		hashSet.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_HashSetWithFuncHashSet_ShouldAddRange()
	{
		// Arrange
		HashSet<string> hashSet = ["a"];
		static Task<HashSet<string>?> func()
		{
			return Task.FromResult<HashSet<string>?>(new HashSet<string> { "b", "c" });
		}

		// Act
		await hashSet.ObjectFill(func);

		// Assert
		hashSet.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskSingleItem_ShouldAdd()
	{
		// Arrange
		ConcurrentBag<int> bag = new();
		bag.Add(1);
		Task<int> task = Task.FromResult(2);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncSingleItem_ShouldAdd()
	{
		// Arrange
		ConcurrentBag<string?> bag = new();
		bag.Add("a");
		static Task<string?> func()
		{
			return Task.FromResult<string?>("b");
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerableAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(new[] { 1, 2 });

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>(new[] { 1, 2 });
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskIEnumerableAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		Task<IEnumerable<int>?> task = Task.FromResult<IEnumerable<int>?>(null);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncIEnumerableAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<IEnumerable<int>?> func()
		{
			return Task.FromResult<IEnumerable<int>?>(null);
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBagAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int> { 1 });

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(new ConcurrentBag<int> { 1 });
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskConcurrentBagAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		Task<ConcurrentBag<int>?> task = Task.FromResult<ConcurrentBag<int>?>(null);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncConcurrentBagAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<ConcurrentBag<int>?> func()
		{
			return Task.FromResult<ConcurrentBag<int>?>(null);
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskListAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		Task<List<int>?> task = Task.FromResult<List<int>?>(new List<int> { 1 });

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(task));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListAndNullBag_ShouldNotThrow()
	{
		// Arrange
		ConcurrentBag<int>? bag = null;
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(new List<int> { 1 });
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await bag.ObjectFill(func));
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithTaskListAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		Task<List<int>?> task = Task.FromResult<List<int>?>(null);

		// Act
		await bag.ObjectFill(task);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_ConcurrentBagWithFuncListAndNullResult_ShouldNotModify()
	{
		// Arrange
		ConcurrentBag<int> bag = [1];
		static Task<List<int>?> func()
		{
			return Task.FromResult<List<int>?>(null);
		}

		// Act
		await bag.ObjectFill(func);

		// Assert
		bag.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithTaskAndNullResult_ShouldNotModify()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Rows.Add(1);
		Task<DataTable> task = Task.FromResult<DataTable>(null!);

		// Act & Assert
		await Should.NotThrowAsync(async () => await dt.ObjectFill(task));
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_DataTableWithFuncAndNullResult_ShouldNotModify()
	{
		// Arrange
		using DataTable dt = new();
		dt.Columns.Add("Id", typeof(int));
		dt.Rows.Add(1);
		static Task<DataTable> func()
		{
			return Task.FromResult<DataTable>(null!);
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await dt.ObjectFill(func));
		dt.Rows.Count.ShouldBe(1);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithTaskAndNullResult_ShouldNotModify()
	{
		// Arrange
		using MemoryStream ms = new();
		ms.WriteByte(1);
		long originalLength = ms.Length;
		Task<MemoryStream> task = Task.FromResult<MemoryStream>(null!);

		// Act & Assert
		await Should.NotThrowAsync(async () => await ms.ObjectFill(task));
		ms.Length.ShouldBe(originalLength);
	}

	[Fact]
	public async Task ObjectFill_MemoryStreamWithFuncAndNullResult_ShouldNotModify()
	{
		// Arrange
		using MemoryStream ms = new();
		ms.WriteByte(1);
		long originalLength = ms.Length;
		static Task<MemoryStream> func()
		{
			return Task.FromResult<MemoryStream>(null!);
		}

		// Act & Assert
		await Should.NotThrowAsync(async () => await ms.ObjectFill(func));
		ms.Length.ShouldBe(originalLength);
	}

	#endregion

	#region Helper Classes

#pragma warning disable S2094 // Classes should not be empty
	private class EmptyClass;
#pragma warning restore S2094 // Classes should not be empty

	#endregion
}
