using CommonNetFuncs.Web.Api.TaskQueuing;
using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;
using static Xunit.TestContext;

namespace Web.Api.Tests;

public class PrioritizedQueuedTaskTests
{
	private enum ComparisonType
	{
		HigherPriority,
		LowerPriority,
		NegativePriorities
	}

	private enum OperatorType
	{
		Equals,
		NotEquals,
		LessThan,
		LessThanOrEquals,
		GreaterThan,
		GreaterThanOrEquals
	}

	private enum NullPosition
	{
		Both,
		Left,
		Right
	}

	[Fact]
	public void Constructor_Should_Create_Task_With_TaskFunction()
	{
		// Arrange
		Func<CancellationToken, Task<object?>> taskFunction = _ => Task.FromResult<object?>(42);

		// Act
		PrioritizedQueuedTask task = new(taskFunction);

		// Assert
		task.ShouldNotBeNull();
		task.Priority.ShouldBe(0); // Default value
		task.CancellationTokenSource.ShouldNotBeNull();
		task.IsCancelled.ShouldBeFalse();
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	[InlineData(100)]
	[InlineData(-5)]
	public void Priority_Property_Should_Be_Settable(int priority)
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		task.Priority = priority;

		// Assert
		task.Priority.ShouldBe(priority);
	}

	[Theory]
	[InlineData(TaskPriority.Low)]
	[InlineData(TaskPriority.Normal)]
	[InlineData(TaskPriority.High)]
	[InlineData(TaskPriority.Critical)]
	[InlineData(TaskPriority.Emergency)]
	public void PriorityLevel_Property_Should_Be_Settable(TaskPriority priorityLevel)
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		task.PriorityLevel = priorityLevel;

		// Assert
		task.PriorityLevel.ShouldBe(priorityLevel);
	}

	[Fact]
	public void Timeout_Property_Should_Be_Settable()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));
		TimeSpan timeout = TimeSpan.FromSeconds(30);

		// Act
		task.Timeout = timeout;

		// Assert
		task.Timeout.ShouldBe(timeout);
	}

	[Fact]
	public void Timeout_Property_Should_Accept_Null()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null))
		{
			Timeout = TimeSpan.FromSeconds(10)
		};

		// Act
		task.Timeout = null;

		// Assert
		task.Timeout.ShouldBeNull();
	}

	[Fact]
	public void CancellationTokenSource_Property_Should_Be_Settable()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));
		CancellationTokenSource newCts = new();

		// Act
		task.CancellationTokenSource = newCts;

		// Assert
		task.CancellationTokenSource.ShouldBe(newCts);
	}

	[Fact]
	public void IsCancelled_Should_Return_False_When_Not_Cancelled()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act & Assert
		task.IsCancelled.ShouldBeFalse();
	}

	[Fact]
	public void IsCancelled_Should_Return_True_When_Cancelled()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		task.CancellationTokenSource.Cancel();

		// Assert
		task.IsCancelled.ShouldBeTrue();
	}

	[Fact]
	public void CompareTo_Should_Return_1_When_Other_Is_Null()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		int result = task.CompareTo(null);

		// Assert
		result.ShouldBe(1);
	}

	[Fact]
	public void CompareTo_Should_Prioritize_Higher_Priority_First()
	{
		// Arrange
		PrioritizedQueuedTask lowPriorityTask = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 1
		};
		PrioritizedQueuedTask highPriorityTask = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};

		// Act
		int result = lowPriorityTask.CompareTo(highPriorityTask);

		// Assert
		result.ShouldBeGreaterThan(0); // Low priority comes after high priority
	}

	[Fact]
	public async Task CompareTo_Should_Use_FIFO_For_Same_Priority()
	{
		// Arrange
		PrioritizedQueuedTask earlierTask = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Delay to ensure different QueuedAt times
		await Task.Delay(10, Current.CancellationToken);

		PrioritizedQueuedTask laterTask = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Act
		int result = earlierTask.CompareTo(laterTask);

		// Assert
		result.ShouldBeLessThan(0); // Earlier task comes first (FIFO)
	}

	[Theory]
	[InlineData(ComparisonType.LowerPriority, 1, 10)]  // Low priority comes after high priority
	[InlineData(ComparisonType.HigherPriority, 10, 5)] // High priority comes before low priority
	[InlineData(ComparisonType.NegativePriorities, -10, -5)] // -10 has lower priority than -5
	public void CompareTo_WithDifferentPriorities_ReturnsExpectedComparison(ComparisonType type, int priority1, int priority2)
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null)) { Priority = priority1 };
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null)) { Priority = priority2 };

		// Act
		int result = task1.CompareTo(task2);

		// Assert
		switch (type)
		{
			case ComparisonType.LowerPriority:
				result.ShouldBeGreaterThan(0); // Low priority comes after high priority
				break;
			case ComparisonType.HigherPriority:
				result.ShouldBeLessThan(0); // High priority comes before low priority
				break;
			case ComparisonType.NegativePriorities:
				result.ShouldBeGreaterThan(0); // -10 has lower priority than -5
				break;
		}
	}

	[Fact]
	public void Equals_Should_Return_True_For_Same_Reference()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		bool result = task.Equals(task);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void Equals_Should_Return_False_For_Null()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act
		bool result = task.Equals(null);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void Equals_Should_Throw_NotImplementedException_For_Different_Objects()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null));
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null));

		// Act & Assert
		Should.Throw<NotImplementedException>(() => task1.Equals(task2));
	}

	[Fact]
	public void GetHashCode_Should_Throw_NotImplementedException()
	{
		// Arrange
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));

		// Act & Assert
		Should.Throw<NotImplementedException>(() => task.GetHashCode());
	}

	[Theory]
	[InlineData(OperatorType.Equals, NullPosition.Both, true)]
	[InlineData(OperatorType.Equals, NullPosition.Left, false)]
	[InlineData(OperatorType.Equals, NullPosition.Right, false)]
	[InlineData(OperatorType.NotEquals, NullPosition.Both, false)]
	[InlineData(OperatorType.NotEquals, NullPosition.Left, true)]
	public void Operators_WithNullOperands_ReturnExpectedResults(OperatorType operatorType, NullPosition nullPos, bool expected)
	{
		// Arrange
		PrioritizedQueuedTask? task1 = nullPos is NullPosition.Both or NullPosition.Left
			? null
			: new(_ => Task.FromResult<object?>(null));

		PrioritizedQueuedTask? task2 = nullPos is NullPosition.Both or NullPosition.Right
			? null
			: new(_ => Task.FromResult<object?>(null));

		// Act
		bool result = operatorType switch
		{
			OperatorType.Equals => task1 == task2,
			OperatorType.NotEquals => task1 != task2,
			_ => throw new ArgumentOutOfRangeException(nameof(operatorType))
		};

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(OperatorType.LessThan, NullPosition.Left, true)]
	[InlineData(OperatorType.LessThan, NullPosition.Both, false)]
	[InlineData(OperatorType.LessThanOrEquals, NullPosition.Left, true)]
	[InlineData(OperatorType.LessThanOrEquals, NullPosition.Both, true)]
	[InlineData(OperatorType.GreaterThan, NullPosition.Left, false)]
	[InlineData(OperatorType.GreaterThan, NullPosition.Both, false)]
	[InlineData(OperatorType.GreaterThanOrEquals, NullPosition.Left, false)]
	[InlineData(OperatorType.GreaterThanOrEquals, NullPosition.Both, true)]
	public void ComparisonOperators_WithNullOperands_ReturnExpectedResults(OperatorType operatorType, NullPosition nullPos, bool expected)
	{
		// Arrange
		PrioritizedQueuedTask? task1 = nullPos is NullPosition.Both or NullPosition.Left
			? null
			: new(_ => Task.FromResult<object?>(null)) { Priority = 5 };

		PrioritizedQueuedTask? task2 = nullPos is NullPosition.Both or NullPosition.Right
			? null
			: new(_ => Task.FromResult<object?>(null)) { Priority = 10 };

		// Act
		bool result = operatorType switch
		{
			OperatorType.LessThan => task1 < task2,
			OperatorType.LessThanOrEquals => task1 <= task2,
			OperatorType.GreaterThan => task1 > task2,
			OperatorType.GreaterThanOrEquals => task1 >= task2,
			_ => throw new ArgumentOutOfRangeException(nameof(operatorType))
		};

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void OperatorLessThan_Should_Return_True_When_Left_Has_Lower_Priority()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};

		// Act
		bool result = task1 < task2;

		// Assert
		// Higher priority (10) comes before lower priority (5) in sort order
		// So task2 < task1, meaning task1 is NOT < task2
		result.ShouldBeFalse();
	}

	[Fact]
	public void OperatorLessThan_Should_Return_True_When_Left_Has_Higher_Priority()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Act
		bool result = task1 < task2;

		// Assert
		// Higher priority (10) comes before lower priority (5) in sort order
		// So task1 < task2 should be true
		result.ShouldBeTrue();
	}

	[Fact]
	public void OperatorLessThanOrEqual_Should_Return_True_When_Priorities_Are_Equal()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Act
		bool result = task1 <= task2; // Depends on QueuedAt time, but should work

		// Assert
		// Since task1 was created first, it should be <= task2
		result.ShouldBeTrue();
	}

	[Fact]
	public void OperatorGreaterThan_Should_Return_True_When_Left_Has_Lower_Priority()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};

		// Act
		bool result = task1 > task2;

		// Assert
		// Lower priority (5) comes after higher priority (10) in sort order
		// So task1 > task2 should be true
		result.ShouldBeTrue();
	}

	[Fact]
	public void OperatorGreaterThan_Should_Return_False_When_Left_Has_Higher_Priority()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Act
		bool result = task1 > task2;

		// Assert
		// Higher priority (10) comes before lower priority (5) in sort order
		// So task1 > task2 should be false
		result.ShouldBeFalse();
	}

	[Fact]
	public void OperatorGreaterThanOrEqual_Should_Return_True_When_Left_Has_Lower_Priority()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 10
		};

		// Act
		bool result = task1 >= task2;

		// Assert
		// Lower priority (5) comes after higher priority (10) in sort order
		// So task1 >= task2 should be true
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task OperatorGreaterThanOrEqual_Should_Handle_Equal_Priorities()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		await Task.Delay(10, Current.CancellationToken); // Ensure different QueuedAt times

		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null))
		{
			Priority = 5
		};

		// Act
		bool result = task2 >= task1;

		// Assert
		//task2 was created later, so it should be >= task1 (comes after in FIFO)
		result.ShouldBeTrue();
	}

	[Fact]
	public void Multiple_Tasks_Should_Sort_Correctly_By_Priority()
	{
		// Arrange
		List<PrioritizedQueuedTask> tasks =
		[
			new(_ => Task.FromResult<object?>(null)) { Priority = 5 },
			new(_ => Task.FromResult<object?>(null)) { Priority = 10 },
			new(_ => Task.FromResult<object?>(null)) { Priority = 1 },
			new(_ => Task.FromResult<object?>(null)) { Priority = 7 }
		];

		// Act
		tasks.Sort();

		// Assert
		tasks[0].Priority.ShouldBe(10); // Highest priority first
		tasks[1].Priority.ShouldBe(7);
		tasks[2].Priority.ShouldBe(5);
		tasks[3].Priority.ShouldBe(1); // Lowest priority last
	}

	[Fact]
	public void CancellationTokenSource_Should_Be_Independent_For_Each_Task()
	{
		// Arrange
		PrioritizedQueuedTask task1 = new(_ => Task.FromResult<object?>(null));
		PrioritizedQueuedTask task2 = new(_ => Task.FromResult<object?>(null));

		// Act
		task1.CancellationTokenSource.Cancel();

		// Assert
		task1.IsCancelled.ShouldBeTrue();
		task2.IsCancelled.ShouldBeFalse();
	}

	[Fact]
	public void QueuedAt_Should_Be_Set_On_Construction()
	{
		// Arrange
		DateTime beforeCreation = DateTime.UtcNow;

		// Act
		PrioritizedQueuedTask task = new(_ => Task.FromResult<object?>(null));
		DateTime afterCreation = DateTime.UtcNow;

		// Assert
		task.QueuedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
		task.QueuedAt.ShouldBeLessThanOrEqualTo(afterCreation);
	}
}
