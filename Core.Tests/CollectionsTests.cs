using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using CommonNetFuncs.Core;
using CommonNetFuncs.Core.CollectionClasses;
using FastExpressionCompiler;
using static Xunit.TestContext;

namespace Core.Tests;

// Helper classes for testing

public sealed class TestClass
{
	public int Id { get; set; }

	public string? Name { get; set; }

	public string? Description { get; set; }

	public int Value { get; set; }

	public bool IsActive { get; set; }

	public DateTime Date { get; set; }

	public DateOnly DateOnly { get; set; }
}

public enum ETest
{
	Monday = DayOfWeek.Monday,
	Tuesday = DayOfWeek.Tuesday,
	Wednesday = DayOfWeek.Wednesday,
	Thursday = DayOfWeek.Thursday,
	Friday = DayOfWeek.Friday
}

public sealed class CollectionsTests
{
	private readonly Fixture fixture;

	public CollectionsTests()
	{
		fixture = new Fixture();
	}

	public enum AnyFastCollectionType
	{
		ICollection,
		IList,
		ConcurrentBag,
		Array,
		Dictionary,
		ConcurrentDictionary
	}

	#region AnyFast Tests


	[Theory]
	[InlineData(AnyFastCollectionType.ICollection)]
	[InlineData(AnyFastCollectionType.IList)]
	[InlineData(AnyFastCollectionType.ConcurrentBag)]
	[InlineData(AnyFastCollectionType.Array)]
	[InlineData(AnyFastCollectionType.Dictionary)]
	[InlineData(AnyFastCollectionType.ConcurrentDictionary)]
	public void AnyFast_WithNullCollection_ReturnsFalse(AnyFastCollectionType collectionType)
	{
		// Act

		bool result = collectionType switch
		{
			AnyFastCollectionType.ICollection => ((ICollection<string>?)null).AnyFast(),
			AnyFastCollectionType.IList => ((IList<string>?)null).AnyFast(),
			AnyFastCollectionType.ConcurrentBag => ((ConcurrentBag<string>?)null).AnyFast(),
			AnyFastCollectionType.Array => ((string[]?)null).AnyFast(),
			AnyFastCollectionType.Dictionary => ((IDictionary<string, string>?)null).AnyFast(),
			AnyFastCollectionType.ConcurrentDictionary => ((ConcurrentDictionary<string, string>?)null).AnyFast(),
			_ => throw new ArgumentOutOfRangeException(nameof(collectionType))
		};

		// Assert

		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithICollection_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		List<string> collection = new();
		for (int i = 0; i < count; i++)
		{
			collection.Add(fixture.Create<string>());
		}

		// Act

		bool result = collection.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithIList_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		List<string> list = new();
		for (int i = 0; i < count; i++)
		{
			list.Add(fixture.Create<string>());
		}

		// Act

		bool result = list.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithConcurrentBag_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		ConcurrentBag<string> bag = new();
		for (int i = 0; i < count; i++)
		{
			bag.Add(fixture.Create<string>());
		}

		// Act

		bool result = bag.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithArray_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		string[] array = new string[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = fixture.Create<string>();
		}

		// Act

		bool result = array.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithDictionary_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		Dictionary<string, string> dict = new();
		for (int i = 0; i < count; i++)
		{
			dict.Add(fixture.Create<string>(), fixture.Create<string>());
		}

		// Act

		bool result = dict.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	[Fact]
	public void AnyFast_WithNullConcurrentDictionary_ReturnsFalse()
	{
		// Act

		bool result = ((ConcurrentDictionary<string, string>?)null).AnyFast();

		// Assert

		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(5, true)]
	public void AnyFast_WithConcurrentDictionary_ReturnsExpectedResult(int count, bool expected)
	{
		// Arrange

		ConcurrentDictionary<string, string> dict = new();
		for (int i = 0; i < count; i++)
		{
			dict.TryAdd(fixture.Create<string>(), fixture.Create<string>());
		}

		// Act

		bool result = dict.AnyFast();

		// Assert

		result.ShouldBe(expected);
	}

	#endregion

	#region AddDictionaryItem Tests


	[Fact]
	public void AddDictionaryItem_AddsItemToDictionary()
	{
		// Arrange

		Dictionary<string, int> dict = new();
		KeyValuePair<string, int> pair = new("test", 42);

		// Act

		dict.AddDictionaryItem(pair);

		// Assert

		dict.ShouldContainKey("test");
		dict["test"].ShouldBe(42);
	}

	[Fact]
	public void AddDictionaryItem_WithExistingKey_DoesNotOverwrite()
	{
		// Arrange

		Dictionary<string, int> dict = new() { { "test", 42 } };
		KeyValuePair<string, int> pair = new("test", 99);

		// Act

		dict.AddDictionaryItem(pair);

		// Assert

		dict["test"].ShouldBe(42);
	}

	#endregion

	#region AddDictionaryItems Tests


	[Fact]
	public void AddDictionaryItems_AddsMultipleItemsToDictionary()
	{
		// Arrange

		Dictionary<string, int> dict = new();
		List<KeyValuePair<string, int>> pairs = new()
				{
						new KeyValuePair<string, int>("test1", 42),
						new KeyValuePair<string, int>("test2", 99)
				};

		// Act

		dict.AddDictionaryItems(pairs, cancellationToken: Current.CancellationToken);

		// Assert

		dict.ShouldContainKey("test1");
		dict.ShouldContainKey("test2");
		dict["test1"].ShouldBe(42);
		dict["test2"].ShouldBe(99);
	}

	[Fact]
	public void AddDictionaryItems_WithExistingKeys_DoesNotOverwrite()
	{
		// Arrange

		Dictionary<string, int> dict = new() { { "test1", 42 } };
		List<KeyValuePair<string, int>> pairs = new()
				{
						new KeyValuePair<string, int>("test1", 99),
						new KeyValuePair<string, int>("test2", 100)
				};

		// Act

		dict.AddDictionaryItems(pairs, cancellationToken: Current.CancellationToken);

		// Assert

		dict["test1"].ShouldBe(42);
		dict["test2"].ShouldBe(100);
	}

	#endregion

	#region AddRange and AddRangeParallel Tests


	[Fact]
	public void AddRange_AddsItemsToConcurrentBag()
	{
		// Arrange

		ConcurrentBag<string> bag = new();
		List<string?> items = new() { "test1", "test2", null };

		// Act

		bag.AddRange(items, cancellationToken: Current.CancellationToken);

		// Assert

		bag.Count.ShouldBe(2);
		bag.ShouldContain("test1");
		bag.ShouldContain("test2");
	}

	[Fact]
	public void AddRangeParallel_AddsItemsToConcurrentBag()
	{
		// Arrange

		ConcurrentBag<string> bag = new();
		List<string?> items = new() { "test1", "test2", null };

		// Act

		bag.AddRangeParallel(items, cancellationToken: Current.CancellationToken);

		// Assert

		bag.Count.ShouldBe(2);
		bag.ShouldContain("test1");
		bag.ShouldContain("test2");
	}

	[Fact]
	public void AddRangeParallel_WithCustomParallelOptions()
	{
		// Arrange

		ConcurrentBag<string> bag = new();
		List<string?> items = new() { "test1", "test2", null };
		ParallelOptions options = new() { MaxDegreeOfParallelism = 2 };

		// Act

		bag.AddRangeParallel(items, options, cancellationToken: Current.CancellationToken);

		// Assert

		bag.Count.ShouldBe(2);
		bag.ShouldContain("test1");
		bag.ShouldContain("test2");
	}

	[Fact]
	public void AddRange_AddsItemsToHashSet()
	{
		// Arrange

		HashSet<string> hashSet = new();
		List<string?> items = new() { "test1", "test2", null };

		// Act

		hashSet.AddRange(items, cancellationToken: Current.CancellationToken);

		// Assert

		hashSet.Count.ShouldBe(2);
		hashSet.ShouldContain("test1");
		hashSet.ShouldContain("test2");
	}

	#endregion

	#region SetValue Tests

	public enum SetValueMethodType
	{
		SetValue,
		SetValueEnumerate,
		SetValueParallel
	}

	public enum SetValueScenario
	{
		AppliesActionToAllItems,
		AppliesActionToAllItems_ForStrings,
		WithIList_AppliesFunction_ForStrings,
		WithEnumerable_AppliesFunction_ForStrings,
		WithCustomMaxDegreeOfParallelism
	}

	public enum SetValueNullScenario
	{
		ThrowsOnNullItems,
		ThrowsOnNullUpdateMethod,
		ThrowsOnNullItems_ForStrings,
		ThrowsOnNullUpdateMethod_ForStrings
	}

	[Theory]
	[InlineData(SetValueMethodType.SetValue, SetValueScenario.AppliesActionToAllItems)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueScenario.AppliesActionToAllItems)]
	[InlineData(SetValueMethodType.SetValueParallel, SetValueScenario.AppliesActionToAllItems)]
	[InlineData(SetValueMethodType.SetValue, SetValueScenario.AppliesActionToAllItems_ForStrings)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueScenario.AppliesActionToAllItems_ForStrings)]
	[InlineData(SetValueMethodType.SetValue, SetValueScenario.WithIList_AppliesFunction_ForStrings)]
	[InlineData(SetValueMethodType.SetValue, SetValueScenario.WithEnumerable_AppliesFunction_ForStrings)]
	[InlineData(SetValueMethodType.SetValueParallel, SetValueScenario.WithCustomMaxDegreeOfParallelism)]
	public void SetValue_VariousScenarios_WorkCorrectly(SetValueMethodType methodType, SetValueScenario scenario)
	{
		// Arrange & Act & Assert
		switch (scenario)
		{
			case SetValueScenario.AppliesActionToAllItems:
				{
					List<TestClass> items = new() { new TestClass { Name = "test1" }, new TestClass { Name = "test2" } };
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							items.SetValue(item => item.Name = item.Name?.ToUpper(), cancellationToken: Current.CancellationToken);
							items.Count.ShouldBe(2);
							items.ShouldBeSubsetOf(items);
							items.ShouldBeUnique();
							items[0].Name.ShouldBe("TEST1");
							items[1].Name.ShouldBe("TEST2");
							break;
						case SetValueMethodType.SetValueEnumerate:
							{
								IEnumerable<TestClass> result = items.SetValueEnumerate(item => item.Name = item.Name?.ToUpper(), cancellationToken: Current.CancellationToken);
								result.Count().ShouldBe(items.Count);
								result.ShouldBeSubsetOf(items);
								result.ShouldBeUnique();
								items[0].Name.ShouldBe("TEST1");
								items[1].Name.ShouldBe("TEST2");
								break;
							}
						case SetValueMethodType.SetValueParallel:
							items.SetValueParallel(item => item.Name = item.Name?.ToUpper(), cancellationToken: Current.CancellationToken);
							items.Count.ShouldBe(2);
							items.ShouldContain(item => item.Name == "TEST1");
							items.ShouldContain(item => item.Name == "TEST2");
							break;
					}
					break;
				}
			case SetValueScenario.AppliesActionToAllItems_ForStrings:
				{
					List<string> items = new() { "test1", "test2" };
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							items.SetValue(s => s?.ToUpper(), cancellationToken: Current.CancellationToken);
							items.Count.ShouldBe(2);
							items[0].ShouldBe("TEST1");
							items[1].ShouldBe("TEST2");
							break;
						case SetValueMethodType.SetValueEnumerate:
							{
								List<string?> result = items.SetValueEnumerate(s => s?.ToUpper(), cancellationToken: Current.CancellationToken).ToList();
								result.Count.ShouldBe(2);
								result[0].ShouldBe("TEST1");
								result[1].ShouldBe("TEST2");
								break;
							}
					}
					break;
				}
			case SetValueScenario.WithIList_AppliesFunction_ForStrings:
				{
					List<string?> items = new() { "test1", "test2", "test3" };
					items.SetValue(s => s?.ToUpper(), cancellationToken: Current.CancellationToken);
					items[0].ShouldBe("TEST1");
					items[1].ShouldBe("TEST2");
					items[2].ShouldBe("TEST3");
					break;
				}
			case SetValueScenario.WithEnumerable_AppliesFunction_ForStrings:
				{
					string[] array = new[] { "test1", "test2", "test3" };
					IEnumerable<string?> items = array.Where(x => x != null);
					Should.NotThrow(() => items.SetValue(s => s?.ToUpper()));
					array[0].ShouldBe("test1"); // Original unchanged
					break;
				}
			case SetValueScenario.WithCustomMaxDegreeOfParallelism:
				{
					List<TestClass> items = new() { new TestClass { Name = "test1" }, new TestClass { Name = "test2" } };
					items.SetValueParallel(item => item.Name = item.Name?.ToUpper(), 2, cancellationToken: Current.CancellationToken);
					items.Count.ShouldBe(2);
					items.ShouldContain(item => item.Name == "TEST1");
					items.ShouldContain(item => item.Name == "TEST2");
					break;
				}
		}
	}

	[Theory]
	[InlineData(SetValueMethodType.SetValue, SetValueNullScenario.ThrowsOnNullItems)]
	[InlineData(SetValueMethodType.SetValue, SetValueNullScenario.ThrowsOnNullUpdateMethod)]
	[InlineData(SetValueMethodType.SetValue, SetValueNullScenario.ThrowsOnNullItems_ForStrings)]
	[InlineData(SetValueMethodType.SetValue, SetValueNullScenario.ThrowsOnNullUpdateMethod_ForStrings)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueNullScenario.ThrowsOnNullItems)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueNullScenario.ThrowsOnNullUpdateMethod)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueNullScenario.ThrowsOnNullItems_ForStrings)]
	[InlineData(SetValueMethodType.SetValueEnumerate, SetValueNullScenario.ThrowsOnNullUpdateMethod_ForStrings)]
	[InlineData(SetValueMethodType.SetValueParallel, SetValueNullScenario.ThrowsOnNullItems)]
	[InlineData(SetValueMethodType.SetValueParallel, SetValueNullScenario.ThrowsOnNullUpdateMethod)]
	public void SetValue_WithNullArguments_ThrowsArgumentNullException(SetValueMethodType methodType, SetValueNullScenario scenario)
	{
		// Arrange & Act & Assert
		switch (scenario)
		{
			case SetValueNullScenario.ThrowsOnNullItems:
				{
					IEnumerable<TestClass>? items = null;
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							Should.Throw<ArgumentNullException>(() => items!.SetValue(_ => { }));
							break;
						case SetValueMethodType.SetValueEnumerate:
							Should.Throw<ArgumentNullException>(() => items!.SetValueEnumerate(_ => { }));
							break;
						case SetValueMethodType.SetValueParallel:
							Should.Throw<ArgumentNullException>(() => items!.SetValueParallel(_ => { }));
							break;
					}
					break;
				}
			case SetValueNullScenario.ThrowsOnNullUpdateMethod:
				{
					List<TestClass> items = new() { new TestClass() };
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							Should.Throw<ArgumentNullException>(() => items.SetValue(null!));
							break;
						case SetValueMethodType.SetValueEnumerate:
							Should.Throw<ArgumentNullException>(() => items.SetValueEnumerate(null!));
							break;
						case SetValueMethodType.SetValueParallel:
							Should.Throw<ArgumentNullException>(() => items.SetValueParallel(null!));
							break;
					}
					break;
				}
			case SetValueNullScenario.ThrowsOnNullItems_ForStrings:
				{
					IEnumerable<string?>? items = null;
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							Should.Throw<ArgumentNullException>(() => items!.SetValue(s => s));
							break;
						case SetValueMethodType.SetValueEnumerate:
							Should.Throw<ArgumentNullException>(() => items!.SetValueEnumerate(s => s));
							break;
					}
					break;
				}
			case SetValueNullScenario.ThrowsOnNullUpdateMethod_ForStrings:
				{
					List<string?> items = new() { "test" };
					switch (methodType)
					{
						case SetValueMethodType.SetValue:
							Should.Throw<ArgumentNullException>(() => items.SetValue(null!));
							break;
						case SetValueMethodType.SetValueEnumerate:
							Should.Throw<ArgumentNullException>(() => items.SetValueEnumerate(null!));
							break;
					}
					break;
				}
		}
	}

	#endregion

	#region SetValue for Array Tests

	public enum ArraySetValueScenario
	{
		AppliesActionToAllElements,
		EmptyArrayDoesNothing,
		ThrowsOnNullArray,
		ThrowsOnNullUpdateMethod
	}

	[Theory]
	[InlineData(ArraySetValueScenario.AppliesActionToAllElements)]
	[InlineData(ArraySetValueScenario.EmptyArrayDoesNothing)]
	[InlineData(ArraySetValueScenario.ThrowsOnNullArray)]
	[InlineData(ArraySetValueScenario.ThrowsOnNullUpdateMethod)]
	public void SetValue_ForArray_VariousScenarios_WorkCorrectly(ArraySetValueScenario scenario)
	{
		// Arrange & Act & Assert
		switch (scenario)
		{
			case ArraySetValueScenario.AppliesActionToAllElements:
				{
					int[,] array = new int[,] { { 1, 2 }, { 3, 4 } };
					array.SetValue((arr, indices) => arr.SetValue(((int)arr.GetValue(indices)!) * 2, indices), cancellationToken: Current.CancellationToken);
					array[0, 0].ShouldBe(2);
					array[0, 1].ShouldBe(4);
					array[1, 0].ShouldBe(6);
					array[1, 1].ShouldBe(8);
					break;
				}
			case ArraySetValueScenario.EmptyArrayDoesNothing:
				{
					int[] array = Array.Empty<int>();
#pragma warning disable S1854 // Unused assignments should be removed
					Should.NotThrow(() => array.SetValue((arr, indices) => arr = indices));
#pragma warning restore S1854 // Unused assignments should be removed
					break;
				}
			case ArraySetValueScenario.ThrowsOnNullArray:
				{
					Array? array = null;
					Should.Throw<ArgumentNullException>(() => Collections.SetValue(array!, (_, __) => { }));
					break;
				}
			case ArraySetValueScenario.ThrowsOnNullUpdateMethod:
				{
					int[] array = new int[] { 1, 2, 3 };
					Should.Throw<ArgumentNullException>(() => Collections.SetValue(array, (Action<Array, int[]>)null!));
					break;
				}
		}
	}

	#endregion


	public enum SelectMethodType
	{
		SelectNonEmpty,
		SelectNonNull
	}

	#region SelectNonEmpty and SelectNonNull Tests


	[Fact]
	public void SelectNonEmpty_ReturnsNonEmptyStrings()
	{
		// Arrange

		List<string?> items = new() { "test1", string.Empty, null, "  ", "test2" };

		// Act

		IEnumerable<string> result = items.SelectNonEmpty();

		// Assert

		result.ShouldNotBeNull();
		result.Count().ShouldBe(2);
		result.ShouldContain("test1");
		result.ShouldContain("test2");
	}

	[Theory]
	[InlineData(SelectMethodType.SelectNonEmpty)]
	[InlineData(SelectMethodType.SelectNonNull)]
	public void Select_WithNullCollection_ReturnsNull(SelectMethodType methodType)
	{
		switch (methodType)
		{
			case SelectMethodType.SelectNonEmpty:
				IEnumerable<string>? resultEmpty = Collections.SelectNonEmpty(null);
				resultEmpty.ShouldBeNull();
				break;
			case SelectMethodType.SelectNonNull:
				IEnumerable<TestClass>? resultNull = Collections.SelectNonNull<TestClass>(null);
				resultNull.ShouldBeNull();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(methodType));
		}
	}

	#endregion

	#region SingleToList Tests

	public enum SingleToListScenario
	{
		WithNonNullObject_ReturnsListWithObject,
		WithNullObject_ReturnsEmptyList,
		WithNonEmptyString_ReturnsListWithString,
		WithEmptyString_ReturnsEmptyList,
		WithEmptyStringAndAllowEmptyTrue_ReturnsListWithEmptyString,
		WithNullString_ReturnsEmptyList
	}

	[Theory]
	[InlineData(SingleToListScenario.WithNonNullObject_ReturnsListWithObject)]
	[InlineData(SingleToListScenario.WithNullObject_ReturnsEmptyList)]
	[InlineData(SingleToListScenario.WithNonEmptyString_ReturnsListWithString)]
	[InlineData(SingleToListScenario.WithEmptyString_ReturnsEmptyList)]
	[InlineData(SingleToListScenario.WithEmptyStringAndAllowEmptyTrue_ReturnsListWithEmptyString)]
	[InlineData(SingleToListScenario.WithNullString_ReturnsEmptyList)]
	public void SingleToList_VariousScenarios_WorkCorrectly(SingleToListScenario scenario)
	{
		// Arrange & Act & Assert
		switch (scenario)
		{
			case SingleToListScenario.WithNonNullObject_ReturnsListWithObject:
				{
					TestClass obj = new() { Name = "test" };
					List<TestClass> result = obj.SingleToList();
					result.Count.ShouldBe(1);
					result[0].ShouldBe(obj);
					break;
				}
			case SingleToListScenario.WithNullObject_ReturnsEmptyList:
				{
					List<TestClass> result = Collections.SingleToList<TestClass>(null);
					result.Count.ShouldBe(0);
					break;
				}
			case SingleToListScenario.WithNonEmptyString_ReturnsListWithString:
				{
					const string str = "test";
					List<string> result = str.SingleToList();
					result.Count.ShouldBe(1);
					result[0].ShouldBe(str);
					break;
				}
			case SingleToListScenario.WithEmptyString_ReturnsEmptyList:
				{
					List<string> result = string.Empty.SingleToList(allowEmptyValues: false);
					result.Count.ShouldBe(0);
					break;
				}
			case SingleToListScenario.WithEmptyStringAndAllowEmptyTrue_ReturnsListWithEmptyString:
				{
					List<string> result = string.Empty.SingleToList(allowEmptyValues: true);
					result.Count.ShouldBe(1);
					result[0].ShouldBe(string.Empty);
					break;
				}
			case SingleToListScenario.WithNullString_ReturnsEmptyList:
				{
					List<string> result = Collections.SingleToList(null, allowEmptyValues: true);
					result.Count.ShouldBe(0);
					break;
				}
		}
	}

	#endregion

	#region GetObjectByPartial Tests


	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetObjectByPartial_FindsMatchingObject(bool ignoreDefaultValues)
	{
		// Arrange

		List<TestClass> list = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 2, Name = "test2" },
						new TestClass { Id = 3, Name = "test3" }
				};

		TestClass partial = new() { Id = 2 };

		// Act

		TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Id.ShouldBe(2);
		result.Name.ShouldBe("test2");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetObjectByPartial_WithNoMatch_ReturnsNull(bool ignoreDefaultValues)
	{
		// Arrange

		List<TestClass> list = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 2, Name = "test2" }
				};

		TestClass partial = new() { Id = 3 };

		// Act

		TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetObjectByPartial_WithMultipleProperties_FindsCorrectMatch(bool ignoreDefaultValues)
	{
		// Arrange

		List<TestClass> list = new()
				{
						new TestClass { Id = 1, Name = "test1", Value = 10 },
						new TestClass { Id = 2, Name = "test2", Value = 20 },
						new TestClass { Id = 2, Name = "test3", Value = 30 }
				};

		TestClass partial = new() { Id = 2, Name = "test3" };

		// Act

		TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues, cancellationToken: Current.CancellationToken);

		// Assert

		if (ignoreDefaultValues)
		{
			result.ShouldNotBeNull();
			result.Id.ShouldBe(2);
			result.Name.ShouldBe("test3");
			result.Value.ShouldBe(30);
		}
		else
		{
			result.ShouldBeNull();
		}
	}

	#endregion

	#region ToList, ToListParallel, ToEnumerableParallel, ToEnumerableStreaming Tests


	[Fact]
	public void ToList_ConvertsDataTableToList()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(bool));
		dataTable.Columns.Add(nameof(TestClass.Description), typeof(string));

		dataTable.Rows.Add(1, "test1", true, null);
		dataTable.Rows.Add(2, "test2", false, null);

		// Act

		List<TestClass> result = dataTable.ToList<TestClass>(cancellationToken: Current.CancellationToken);

		// Assert

		result.Count.ShouldBe(2);
		result[0]!.Id.ShouldBe(1);
		result[0]!.Name.ShouldBe("test1");
		result[0]!.IsActive.ShouldBeTrue();
		result[0]!.Description.ShouldBeNull();
		result[1]!.Id.ShouldBe(2);
		result[1]!.Name.ShouldBe("test2");
		result[1]!.IsActive.ShouldBeFalse();
		result[1]!.Description.ShouldBeNull();
	}

	[Fact]
	public void ToList_WithConvertShortToBool_ConvertsCorrectly()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(short));
		dataTable.Columns.Add(nameof(TestClass.Date), typeof(DateTime));
		dataTable.Columns.Add(nameof(TestClass.DateOnly), typeof(DateOnly));

		dataTable.Rows.Add(1, "test1", (short)1, DateTime.MinValue, DateOnly.MinValue);
		dataTable.Rows.Add(2, "test2", (short)0, DateTime.MaxValue, DateOnly.MaxValue);

		// Act

		List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true, cancellationToken: Current.CancellationToken);

		// Assert

		result.Count.ShouldBe(2);
		result[0]!.Id.ShouldBe(1);
		result[0]!.Name.ShouldBe("test1");
		result[0]!.IsActive.ShouldBeTrue();
		result[0]!.Date.ShouldBe(DateTime.MinValue);
		result[0]!.DateOnly.ShouldBe(DateOnly.MinValue);
		result[1]!.Id.ShouldBe(2);
		result[1]!.Name.ShouldBe("test2");
		result[1]!.IsActive.ShouldBeFalse();
		result[1]!.Date.ShouldBe(DateTime.MaxValue);
		result[1]!.DateOnly.ShouldBe(DateOnly.MaxValue);
	}

	[Fact]
	public void ToList_WithConvertShortToBoolMixedDateTypes_ConvertsCorrectly()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(short));
		dataTable.Columns.Add(nameof(TestClass.Date), typeof(DateOnly));
		dataTable.Columns.Add(nameof(TestClass.DateOnly), typeof(DateTime));

		dataTable.Rows.Add(1, "test1", (short)1, DateOnly.MinValue, DateTime.MinValue);
		dataTable.Rows.Add(2, "test2", (short)0, DateOnly.MaxValue, DateTime.MaxValue);

		// Act

		List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true, cancellationToken: Current.CancellationToken);

		// Assert

		result.Count.ShouldBe(2);
		result[0]!.Id.ShouldBe(1);
		result[0]!.Name.ShouldBe("test1");
		result[0]!.IsActive.ShouldBeTrue();
		result[0]!.Date.ShouldBe(DateTime.MinValue);
		result[0]!.DateOnly.ShouldBe(DateOnly.MinValue);
		result[1]!.Id.ShouldBe(2);
		result[1]!.Name.ShouldBe("test2");
		result[1]!.IsActive.ShouldBeFalse();
		result[1]!.Date.ShouldBe(new DateTime(DateOnly.MaxValue, TimeOnly.MinValue, DateTimeKind.Unspecified));
		result[1]!.DateOnly.ShouldBe(DateOnly.MaxValue);
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void ToList_WithConvertShortToBoolMixedStringDateTypes_ConvertsCorrectly(bool badFirstDate, bool badSecondDate)
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(short));
		dataTable.Columns.Add(nameof(TestClass.Date), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.DateOnly), typeof(string));

		dataTable.Rows.Add(1, "test1", (short)1, $"{DateOnly.MinValue:o}{((!badFirstDate) ? string.Empty : "badDate")}", $"{DateTime.MinValue:o}{((!badSecondDate) ? string.Empty : "badDate")}");
		dataTable.Rows.Add(2, "test2", (short)0, $"{DateOnly.MaxValue:o}{((!badFirstDate) ? string.Empty : "badDate")}", $"{DateTime.MaxValue:o}{((!badSecondDate) ? string.Empty : "badDate")}");

		if (!badFirstDate && !badSecondDate)
		{
			// Act

			List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true, cancellationToken: Current.CancellationToken);

			// Assert

			result.Count.ShouldBe(2);
			result[0]!.Id.ShouldBe(1);
			result[0]!.Name.ShouldBe("test1");
			result[0]!.IsActive.ShouldBeTrue();
			result[0]!.Date.ShouldBe(DateTime.MinValue);
			result[0]!.DateOnly.ShouldBe(DateOnly.MinValue);
			result[1]!.Id.ShouldBe(2);
			result[1]!.Name.ShouldBe("test2");
			result[1]!.IsActive.ShouldBeFalse();
			result[1]!.Date.ShouldBe(new DateTime(DateOnly.MaxValue, TimeOnly.MinValue, DateTimeKind.Unspecified));
			result[1]!.DateOnly.ShouldBe(DateOnly.MaxValue);
		}
		else
		{
			// Act & Assert

			Should.Throw<InvalidCastException>(() => dataTable.ToList<TestClass>(convertShortToBool: true));
		}
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void ToList_WithConvertShortToBoolStringDateTypes_ConvertsCorrectly(bool badFirstDate, bool badSecondDate)
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(short));
		dataTable.Columns.Add(nameof(TestClass.Date), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.DateOnly), typeof(string));

		dataTable.Rows.Add(1, "test1", (short)1, $"{DateTime.MinValue:o}{((!badFirstDate) ? string.Empty : "badDate")}", $"{DateOnly.MinValue:o}{((!badSecondDate) ? string.Empty : "badDate")}");
		dataTable.Rows.Add(2, "test2", (short)0, $"{DateTime.MaxValue:o}{((!badFirstDate) ? string.Empty : "badDate")}", $"{DateOnly.MaxValue:o}{((!badSecondDate) ? string.Empty : "badDate")}");

		if (!badFirstDate && !badSecondDate)
		{
			// Act

			List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true, cancellationToken: Current.CancellationToken);

			// Assert

			result.Count.ShouldBe(2);
			result[0]!.Id.ShouldBe(1);
			result[0]!.Name.ShouldBe("test1");
			result[0]!.IsActive.ShouldBeTrue();
			result[0]!.Date.ShouldBe(DateTime.MinValue);
			result[0]!.DateOnly.ShouldBe(DateOnly.MinValue);
			result[1]!.Id.ShouldBe(2);
			result[1]!.Name.ShouldBe("test2");
			result[1]!.IsActive.ShouldBeFalse();
			result[1]!.Date.ShouldBe(DateTime.MaxValue);
			result[1]!.DateOnly.ShouldBe(DateOnly.MaxValue);
		}
		else
		{
			// Act & Assert

			Should.Throw<InvalidCastException>(() => dataTable.ToList<TestClass>(convertShortToBool: true));
		}
	}

	[Fact]
	public void ToListParallel_ConvertsDataTableToList()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));
		dataTable.Columns.Add(nameof(TestClass.IsActive), typeof(bool));

		dataTable.Rows.Add(1, "test1", true);
		dataTable.Rows.Add(2, "test2", false);

		// Act

		List<TestClass> result = dataTable.ToListParallel<TestClass>(cancellationToken: Current.CancellationToken);

		// Assert

		result.Count.ShouldBe(2);
		result.ShouldContain(item => (item!.Id == 1) && (item.Name == "test1") && item.IsActive);
		result.ShouldContain(item => (item!.Id == 2) && (item.Name == "test2") && !item.IsActive);
	}

	[Fact]
	public void ToEnumerableParallel_ConvertsDataTableToEnumerable()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));

		dataTable.Rows.Add(1, "test1");
		dataTable.Rows.Add(2, "test2");

		// Act

		List<TestClass> result = dataTable.ToEnumerableParallel<TestClass>(cancellationToken: Current.CancellationToken).ToList();

		// Assert

		result.Count.ShouldBe(2);
		result.ShouldContain(item => (item!.Id == 1) && (item.Name == "test1"));
		result.ShouldContain(item => (item!.Id == 2) && (item.Name == "test2"));
	}

	[Fact]
	public void ToEnumerableStreaming_ConvertsDataTableToEnumerable()
	{
		// Arrange

		using DataTable dataTable = new();
		dataTable.Columns.Add(nameof(TestClass.Id), typeof(int));
		dataTable.Columns.Add(nameof(TestClass.Name), typeof(string));

		dataTable.Rows.Add(1, "test1");
		dataTable.Rows.Add(2, "test2");

		// Act

		List<TestClass> result = dataTable.ToEnumerableStreaming<TestClass>(cancellationToken: Current.CancellationToken).ToList();

		// Assert

		result.Count.ShouldBe(2);
		result.ShouldContain(item => (item!.Id == 1) && (item.Name == "test1"));
		result.ShouldContain(item => (item!.Id == 2) && (item.Name == "test2"));
	}

	#endregion

	#region ToDataTable Tests

	//	[Theory]
	//	[InlineData(true)]
	//	[InlineData(false)]
	//	public void ToDataTable_ConvertsCollectionToDataTable(bool useExpressionTrees)
	//	{
	//		// Arrange
	//		List<TestClass> collection = new()
	//				{
	//						new TestClass { Id = 1, Name = "test1", IsActive = true },
	//						new TestClass { Id = 2, Name = "test2", IsActive = false }
	//				};

	//		using DataTable result = new();

	//		// Act
	//		if (useExpressionTrees)
	//		{
	//			collection.ToDataTable(result);
	//		}
	//		else
	//		{
	//#pragma warning disable CS0618 // Type or member is obsolete
	//			collection.ToDataTableReflection(result);
	//#pragma warning restore CS0618 // Type or member is obsolete
	//		}

	//		// Assert
	//		result.ShouldNotBeNull();
	//		result.Columns.Count.ShouldBe(7); // Id, Name, IsActive
	//		result.Rows.Count.ShouldBe(2);

	//		result.Rows[0]["Id"].ShouldBe(1);
	//		result.Rows[0]["Name"].ShouldBe("test1");
	//		result.Rows[0]["IsActive"].ShouldBe(true);

	//		result.Rows[1]["Id"].ShouldBe(2);
	//		result.Rows[1]["Name"].ShouldBe("test2");
	//		result.Rows[1]["IsActive"].ShouldBe(false);

	//		result.Dispose();
	//	}

	//	[Theory]
	//	[InlineData(nameof(TestClass.Id), nameof(TestClass.Name), false)]
	//	[InlineData(nameof(TestClass.Id), nameof(TestClass.Name), true)]
	//	[InlineData($"{nameof(TestClass.Id)}NotInTestClass1", $"{nameof(TestClass.Name)}NotInTestClass2", false)]
	//	[InlineData($"{nameof(TestClass.Id)}NotInTestClass1", $"{nameof(TestClass.Name)}NotInTestClass2", true)]
	//	[InlineData(null, null, false)]
	//	[InlineData(null, null, true)]
	//	public void ToDataTable_WithExistingDataTable_AddsRowsToIt(string? column1Name, string? column2Name, bool useExpressionTrees)
	//	{
	//		// Arrange
	//		List<TestClass> collection = new()
	//				{
	//						new TestClass { Id = 1, Name = "test1" },
	//						new TestClass { Id = 2, Name = "test2" }
	//				};

	//		using DataTable dataTable = new();

	//		if (column1Name != null)
	//		{
	//			dataTable.Columns.Add(column1Name, typeof(int));
	//		}

	//		if (column2Name != null)
	//		{
	//			dataTable.Columns.Add(column2Name, typeof(string));
	//		}

	//		// Act
	//		DataTable result = new();

	//		// Act
	//		if (useExpressionTrees)
	//		{
	//			result = collection.ToDataTable(dataTable);
	//		}
	//		else
	//		{
	//#pragma warning disable CS0618 // Type or member is obsolete
	//			result = collection.ToDataTableReflection(dataTable);
	//#pragma warning restore CS0618 // Type or member is obsolete
	//		}

	//		// Assert
	//		result.ShouldBeSameAs(dataTable);
	//		result.Rows.Count.ShouldBe(2);
	//		result.Rows[0]["Id"].ShouldBe(1);
	//		result.Rows[0]["Name"].ShouldBe("test1");
	//		result.Rows[1]["Id"].ShouldBe(2);
	//		result.Rows[1]["Name"].ShouldBe("test2");
	//	}

	//	[Theory]
	//	[InlineData(true)]
	//	[InlineData(false)]
	//	public void ToDataTable_WithParallel_ConvertsCollectionToDataTable(bool useExpressionTrees)
	//	{
	//		// Arrange
	//		List<TestClass> collection = new()
	//				{
	//						new TestClass { Id = 1, Name = "test1" },
	//						new TestClass { Id = 2, Name = "test2" }
	//				};

	//		// Act
	//		using DataTable result = new();
	//		if (useExpressionTrees)
	//		{
	//			collection.ToDataTable(result, useParallel: true);
	//		}
	//		else
	//		{
	//#pragma warning disable CS0618 // Type or member is obsolete
	//			collection.ToDataTableReflection(result, useParallel: true);
	//#pragma warning restore CS0618 // Type or member is obsolete
	//		}
	//		// Assert
	//		result.ShouldNotBeNull();
	//		result.Rows.Count.ShouldBe(2);

	//		// Since parallel processing doesn't guarantee order, we need to check both rows exist
	//		bool foundRow1 = false;
	//		bool foundRow2 = false;

	//		foreach (DataRow row in result.Rows)
	//		{
	//			if (((int)row["Id"] == 1) && ((string)row["Name"] == "test1"))
	//			{
	//				foundRow1 = true;
	//			}
	//			else if (((int)row["Id"] == 2) && ((string)row["Name"] == "test2"))
	//			{
	//				foundRow2 = true;
	//			}
	//		}

	//		foundRow1.ShouldBeTrue();
	//		foundRow2.ShouldBeTrue();
	//	}

	//	[Theory]
	//	[InlineData(true)]
	//	[InlineData(false)]
	//	public void ToDataTable_WithNullCollection_ReturnsNull(bool useExpressionTrees)
	//	{
	//		// Act
	//		DataTable? result = new();
	//		if (useExpressionTrees)
	//		{
	//			result = Collections.ToDataTable<TestClass>(null, result);
	//		}
	//		else
	//		{
	//#pragma warning disable CS0618 // Type or member is obsolete
	//			result = Collections.ToDataTableReflection<TestClass>(null, result);
	//#pragma warning restore CS0618 // Type or member is obsolete
	//		}

	//		// Assert
	//		result.ShouldBeNull();
	//	}


	[Fact]
	public void ToDataTable_ConvertsCollectionToDataTable()
	{
		// Arrange

		List<TestClass> collection = new()
		{
			new TestClass { Id = 1, Name = "test1", IsActive = true },
			new TestClass { Id = 2, Name = "test2", IsActive = false }
		};

		// Act

		DataTable? result = collection.ToDataTable(cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Columns.Count.ShouldBe(7); // All properties of TestClass

		result.Rows.Count.ShouldBe(2);
		result.Rows[0]["Id"].ShouldBe(1);
		result.Rows[0]["Name"].ShouldBe("test1");
		result.Rows[0]["IsActive"].ShouldBe(true);
	}

	[Fact]
	public void ToDataTable_WithNullCollection_ReturnsNull()
	{
		// Act

		DataTable? result = Collections.ToDataTable<TestClass>(null, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldBeNull();
	}

	[Fact]
	public void ToDataTable_WithParallel_ConvertsCollectionToDataTable()
	{
		// Arrange

		List<TestClass> collection = new()
		{
			new TestClass { Id = 1, Name = "test1" },
			new TestClass { Id = 2, Name = "test2" },
			new TestClass { Id = 3, Name = "test3" }
		};

		// Act

		DataTable? result = collection.ToDataTable(useParallel: true, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(3);
		// Check that all rows were added (order may vary due to parallel processing)

		result.Rows.Cast<DataRow>().Count(r => (int)r["Id"] == 1).ShouldBe(1);
		result.Rows.Cast<DataRow>().Count(r => (int)r["Id"] == 2).ShouldBe(1);
		result.Rows.Cast<DataRow>().Count(r => (int)r["Id"] == 3).ShouldBe(1);
	}

	[Fact]
	public void ToDataTable_WithParallelAndCustomDegreeOfParallelism_ConvertsCorrectly()
	{
		// Arrange

		List<TestClass> collection = Enumerable.Range(1, 10)
			.Select(i => new TestClass { Id = i, Name = $"test{i}" })
			.ToList();

		// Act

		DataTable? result = collection.ToDataTable(useParallel: true, degreeOfParallelism: 2, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(10);
	}

	[Fact]
	public void ToDataTable_WithExistingDataTableHavingInvalidColumns_RemovesInvalidColumns()
	{
		// Arrange

		List<TestClass> collection = new()
		{
			new TestClass { Id = 1, Name = "test1" }
		};
		DataTable dataTable = new();
		dataTable.Columns.Add("InvalidColumn", typeof(string));
		dataTable.Columns.Add("AnotherInvalidColumn", typeof(int));

		// Act

		DataTable? result = collection.ToDataTable(dataTable, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Columns.Contains("InvalidColumn").ShouldBeFalse();
		result.Columns.Contains("AnotherInvalidColumn").ShouldBeFalse();
		result.Columns.Contains("Id").ShouldBeTrue();
		result.Columns.Contains("Name").ShouldBeTrue();
	}

	[Fact]
	public void ToDataTable_WithExistingDataTableHavingValidColumns_KeepsValidColumns()
	{
		// Arrange

		List<TestClass> collection = new()
		{
			new TestClass { Id = 1, Name = "test1" }
		};
		DataTable dataTable = new();
		dataTable.Columns.Add("Id", typeof(int));
		dataTable.Columns.Add("Name", typeof(string));

		// Act

		DataTable? result = collection.ToDataTable(dataTable, cancellationToken: Current.CancellationToken);

		// Assert

		result.ShouldNotBeNull();
		result.Columns.Contains("Id").ShouldBeTrue();
		result.Columns.Contains("Name").ShouldBeTrue();
		result.Rows.Count.ShouldBe(1);
	}

	#endregion

	#region StringAggProps Tests


	[Fact]
	public void StringAggProps_WithSingleProperty_AggregatesValues()
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test2" },
						new TestClass { Id = 2, Name = "test3" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps("Name").ToList();

		// Assert

		result.Count.ShouldBe(2);

		TestClass? group1 = result.FirstOrDefault(x => x.Id == 1);
		group1.ShouldNotBeNull();
		group1.Name.ShouldBe("test1;test2");

		TestClass? group2 = result.FirstOrDefault(x => x.Id == 2);
		group2.ShouldNotBeNull();
		group2.Name.ShouldBe("test3");
	}

	[Fact]
	public void StringAggProps_WithCustomSeparator_UsesCorrectSeparator()
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test2" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps("Name", separator: ",").ToList();

		// Assert

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("test1,test2");
	}

	public enum ArrayDimension
	{
		OneDimensional,
		TwoDimensional,
		ThreeDimensional
	}

	public enum ArrayTraverseEdgeCase
	{
		ArrayIsEmpty,
		AtEndOfArray
	}

	[Theory]
	[InlineData(ArrayDimension.OneDimensional)]
	[InlineData(ArrayDimension.TwoDimensional)]
	public void ArrayTraverse_Constructor_InitializesPositionCorrectly(ArrayDimension dimension)
	{
		// Arrange & Act & Assert
		switch (dimension)
		{
			case ArrayDimension.OneDimensional:
				{
					int[] array = new int[5];
					ArrayTraverse traverse = new(array);
					traverse.Position.ShouldNotBeNull();
					traverse.Position.Length.ShouldBe(1);
					traverse.Position[0].ShouldBe(0);
					break;
				}
			case ArrayDimension.TwoDimensional:
				{
					int[,] array = new int[3, 4];
					ArrayTraverse traverse = new(array);
					traverse.Position.ShouldNotBeNull();
					traverse.Position.Length.ShouldBe(2);
					traverse.Position[0].ShouldBe(0);
					traverse.Position[1].ShouldBe(0);
					break;
				}
		}
	}

	[Theory]
	[InlineData(ArrayDimension.OneDimensional)]
	[InlineData(ArrayDimension.TwoDimensional)]
	[InlineData(ArrayDimension.ThreeDimensional)]
	public void ArrayTraverse_Step_IteratesAllPositions(ArrayDimension dimension)
	{
		// Arrange & Act & Assert
		switch (dimension)
		{
			case ArrayDimension.OneDimensional:
				{
					const int length = 4;
					int[] array = new int[length];
					ArrayTraverse traverse = new(array);
					int[][] expectedPositions = new int[length][];
					for (int i = 0; i < length; i++)
					{
						expectedPositions[i] = new[] { i };
					}

					int stepCount = 0;
					do
					{
						traverse.Position[0].ShouldBe(expectedPositions[stepCount][0]);
						stepCount++;
					} while (traverse.Step());

					stepCount.ShouldBe(length);
					break;
				}
			case ArrayDimension.TwoDimensional:
				{
					const int dim0 = 2, dim1 = 3;
					int[,] array = new int[dim0, dim1];
					ArrayTraverse traverse = new(array);
					const int total = dim0 * dim1;
					int count = 0;
					int[,] visited = new int[dim0, dim1];

					do
					{
						int i = traverse.Position[0];
						int j = traverse.Position[1];
						visited[i, j]++;
						count++;
					} while (traverse.Step());

					count.ShouldBe(total);
					for (int i = 0; i < dim0; i++)
					{
						for (int j = 0; j < dim1; j++)
						{
							visited[i, j].ShouldBe(1);
						}
					}
					break;
				}
			case ArrayDimension.ThreeDimensional:
				{
					const int d0 = 2, d1 = 2, d2 = 2;
					int[,,] array = new int[d0, d1, d2];
					ArrayTraverse traverse = new(array);
					const int total = d0 * d1 * d2;
					int count = 0;
					int[,,] visited = new int[d0, d1, d2];

					do
					{
						int i = traverse.Position[0];
						int j = traverse.Position[1];
						int k = traverse.Position[2];
						visited[i, j, k]++;
						count++;
					} while (traverse.Step());

					count.ShouldBe(total);
					for (int i = 0; i < d0; i++)
					{
						for (int j = 0; j < d1; j++)
						{
							for (int k = 0; k < d2; k++)
							{
								visited[i, j, k].ShouldBe(1);
							}
						}
					}
					break;
				}
		}
	}

	[Theory]
	[InlineData(ArrayTraverseEdgeCase.ArrayIsEmpty)]
	[InlineData(ArrayTraverseEdgeCase.AtEndOfArray)]
	public void ArrayTraverse_Step_ReturnsFalse_ForEdgeCases(ArrayTraverseEdgeCase edgeCase)
	{
		// Arrange & Act & Assert
		switch (edgeCase)
		{
			case ArrayTraverseEdgeCase.ArrayIsEmpty:
				{
					int[] array = Array.Empty<int>();
					ArrayTraverse traverse = new(array);
					bool result = traverse.Step();
					result.ShouldBeFalse();
					break;
				}
			case ArrayTraverseEdgeCase.AtEndOfArray:
				{
					int[] array = new int[2];
					ArrayTraverse traverse = new(array);
					traverse.Step(); // Move to index 1
					bool result = traverse.Step(); // Should be at end
					result.ShouldBeFalse();
					break;
				}
		}
	}

	[Fact]
	public void StringAggProps_WithDistinctFalse_IncludesDuplicates()
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test2" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps("Name", distinct: false).ToList();

		// Assert

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("test1;test1;test2");
	}

	[Fact]
	public void StringAggProps_WithDistinctTrue_RemovesDuplicates()
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test2" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps("Name", distinct: true).ToList();

		// Assert

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("test1;test2");
	}

	[Fact]
	public void StringAggProps_WithParallelTrue_AggregatesValuesCorrectly()
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1" },
						new TestClass { Id = 1, Name = "test2" },
						new TestClass { Id = 2, Name = "test3" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps("Name", parallel: true).ToList();

		// Assert

		result.Count.ShouldBe(2);

		TestClass? group1 = result.FirstOrDefault(x => x.Id == 1);
		group1.ShouldNotBeNull();
		group1.Name.ShouldNotBeNull();
		group1.Name.ShouldContain("test1");
		group1.Name.ShouldContain("test2");

		TestClass? group2 = result.FirstOrDefault(x => x.Id == 2);
		group2.ShouldNotBeNull();
		group2.Name.ShouldBe("test3");
	}

	[Fact]
	public void StringAggProps_WithNullCollection_ReturnsEmptyList()
	{
		// Act

		IEnumerable<TestClass> result = Collections.StringAggProps<TestClass>(null, "Name");

		// Assert

		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void StringAggProps_WithMultipleProperties_AggregatesAllSpecifiedProperties(bool distinct)
	{
		// Arrange

		List<TestClass> collection = new()
				{
						new TestClass { Id = 1, Name = "test1", Description = "desc1" },
						new TestClass { Id = 1, Name = "test2", Description = "desc2" },
						new TestClass { Id = 1, Name = "test1", Description = "desc1" }
				};

		// Act

		List<TestClass> result = collection.StringAggProps(new HashSet<string> { "Name", "Description" }, distinct: distinct).ToList();

		// Assert

		if (distinct)
		{
			result.Count.ShouldBe(1);
			result[0].Name.ShouldBe("test1;test2");
			result[0].Description.ShouldBe("desc1;desc2");
		}
		else
		{
			result.Count.ShouldBe(1);
			result[0].Name.ShouldBe("test1;test2;test1");
			result[0].Description.ShouldBe("desc1;desc2;desc1");
		}
	}

	[Fact]
	public void StringAggProps_WithInvalidProperty_ThrowsArgumentException()
	{
		// Arrange

		List<TestClass> collection = new() { new TestClass { Id = 1, Name = "test1" } };

		// Act & Assert

		Should.Throw<ArgumentException>(() => collection.StringAggProps("InvalidProperty").ToList());
	}

	[Fact]
	public void StringAggProps_WithEmptyPropsToAgg_ThrowsArgumentException()
	{
		// Arrange

		List<TestClass> collection = new() { new TestClass { Id = 1, Name = "test1" } };

		// Act & Assert

		Should.Throw<ArgumentException>(() => collection.StringAggProps(new HashSet<string>()).ToList());
	}

	#endregion

	#region IndexOf Tests


	[Fact]
	public void IndexOf_FindsCorrectIndex()
	{
		// Arrange

		List<string> collection = new() { "test1", "test2", "test3" };

		// Act

		int result = Collections.IndexOf(collection, "test2");

		// Assert

		result.ShouldBe(1);
	}

	[Fact]
	public void IndexOf_WithNonExistentItem_ReturnsMinusOne()
	{
		// Arrange

		List<string> collection = new() { "test1", "test2", "test3" };

		// Act

		int result = Collections.IndexOf(collection, "test4");

		// Assert

		result.ShouldBe(-1);
	}

	[Fact]
	public void IndexOf_WithCustomComparer_FindsCorrectIndex()
	{
		// Arrange

		List<string> collection = new() { "TEST1", "TEST2", "TEST3" };
		StringComparer comparer = StringComparer.OrdinalIgnoreCase;

		// Act

		int result = collection.IndexOf("test2", comparer);

		// Assert

		result.ShouldBe(1);
	}

	#endregion

	#region IsIn Tests


	[Theory]
	[InlineData(DayOfWeek.Monday, true)]
	[InlineData(DayOfWeek.Saturday, false)]
	public void IsIn_ChecksEnumNumericMembership(DayOfWeek value, bool expected)
	{
		// Arrange

		DayOfWeek testEnum = value;

		// Act

		bool result = ((int)testEnum).IsIn<ETest>();

		// Assert

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(DayOfWeek.Monday, true)]
	[InlineData(DayOfWeek.Saturday, false)]
	public void IsIn_ChecksEnumNameMembership(DayOfWeek value, bool expected)
	{
		// Arrange

		DayOfWeek testEnum = value;

		// Act

		bool result = testEnum.ToString().IsIn<ETest>();

		// Assert

		result.ShouldBe(expected);
	}

	#endregion

	#region GetCombinations Tests


	public enum CombinationMethodType
	{
		GetCombinations,
		GetRandomCombinations,
		GetEnumeratedCombinations
	}

	public enum CombinationScenario
	{
		GeneratesAllPossibleCombinations,
		WithCustomSeparator_UsesCorrectSeparator,
		WithNullReplacement_HandlesNullValues,
		WithMaxCombinations_LimitsResults,
		WithEmptySource_ReturnsEmptySet,
		WithEmptyInnerList_HandlesCorrectly
	}

	[Theory]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.GeneratesAllPossibleCombinations)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.GeneratesAllPossibleCombinations)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.GeneratesAllPossibleCombinations)]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.WithCustomSeparator_UsesCorrectSeparator)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.WithCustomSeparator_UsesCorrectSeparator)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.WithCustomSeparator_UsesCorrectSeparator)]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.WithNullReplacement_HandlesNullValues)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.WithNullReplacement_HandlesNullValues)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.WithNullReplacement_HandlesNullValues)]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.WithMaxCombinations_LimitsResults)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.WithMaxCombinations_LimitsResults)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.WithMaxCombinations_LimitsResults)]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.WithEmptySource_ReturnsEmptySet)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.WithEmptySource_ReturnsEmptySet)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.WithEmptySource_ReturnsEmptySet)]
	[InlineData(CombinationMethodType.GetCombinations, CombinationScenario.WithEmptyInnerList_HandlesCorrectly)]
	[InlineData(CombinationMethodType.GetRandomCombinations, CombinationScenario.WithEmptyInnerList_HandlesCorrectly)]
	[InlineData(CombinationMethodType.GetEnumeratedCombinations, CombinationScenario.WithEmptyInnerList_HandlesCorrectly)]
	public void GetCombinations_VariousScenarios_WorkCorrectly(CombinationMethodType methodType, CombinationScenario scenario)
	{
		// Arrange & Act & Assert

		switch (scenario)
		{
			case CombinationScenario.GeneratesAllPossibleCombinations:
				{
					List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(4);
					result.ShouldContain("A|1");
					result.ShouldContain("A|2");
					result.ShouldContain("B|1");
					result.ShouldContain("B|2");
					break;
				}
			case CombinationScenario.WithCustomSeparator_UsesCorrectSeparator:
				{
					List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(separator: "-"),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(separator: "-"),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(separator: "-"),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(4);
					result.ShouldContain("A-1");
					result.ShouldContain("A-2");
					result.ShouldContain("B-1");
					result.ShouldContain("B-2");
					break;
				}
			case CombinationScenario.WithNullReplacement_HandlesNullValues:
				{
					List<List<string?>> sources = new() { new List<string?> { "A", null }, new List<string?> { "1", "2" } };
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(nullReplacement: "NULL"),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(nullReplacement: "NULL"),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(nullReplacement: "NULL"),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(4);
					result.ShouldContain("A|1");
					result.ShouldContain("A|2");
					result.ShouldContain("NULL|1");
					result.ShouldContain("NULL|2");
					break;
				}
			case CombinationScenario.WithMaxCombinations_LimitsResults:
				{
					List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(maxCombinations: 5),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(maxCombinations: 5),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(maxCombinations: 5),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(5);
					result.ShouldBeSubsetOf(["A|1", "A|2", "A|3", "B|1", "B|2", "B|3", "C|1", "C|2", "C|3"]);
					break;
				}
			case CombinationScenario.WithEmptySource_ReturnsEmptySet:
				{
					List<List<string>> sources = new();
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(0);
					break;
				}
			case CombinationScenario.WithEmptyInnerList_HandlesCorrectly:
				{
					List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string>() };
					IEnumerable<string> result = methodType switch
					{
						CombinationMethodType.GetCombinations => sources.GetCombinations(nullReplacement: "EMPTY"),
						CombinationMethodType.GetRandomCombinations => sources.GetRandomCombinations(nullReplacement: "EMPTY"),
						CombinationMethodType.GetEnumeratedCombinations => sources.GetEnumeratedCombinations(nullReplacement: "EMPTY"),
						_ => throw new ArgumentOutOfRangeException(nameof(methodType))
					};
					result.Count().ShouldBe(2);
					result.ShouldContain("A|EMPTY");
					result.ShouldContain("B|EMPTY");
					break;
				}
		}
	}

	[Theory]
	[InlineData(CombinationMethodType.GetCombinations)]
	[InlineData(CombinationMethodType.GetRandomCombinations)]
	public void GetCombinations_WithInvalidMaxCombinations_ThrowsArgumentException(CombinationMethodType methodType)
	{
		// Arrange

		List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };

		// Act & Assert

		switch (methodType)
		{
			case CombinationMethodType.GetCombinations:
				Should.Throw<ArgumentException>(() => sources.GetCombinations(maxCombinations: 0));
				Should.Throw<ArgumentException>(() => sources.GetCombinations(maxCombinations: -1));
				break;
			case CombinationMethodType.GetRandomCombinations:
				Should.Throw<ArgumentException>(() => sources.GetRandomCombinations(maxCombinations: 0));
				Should.Throw<ArgumentException>(() => sources.GetRandomCombinations(maxCombinations: -1));
				break;
		}
	}

	#endregion

	#region CombineExpressions Tests


	[Fact]
	public void CombineExpressions_CombinesMultipleExpressions()
	{
		// Arrange

		Expression<Func<TestClass, bool>> expr1 = x => x.Id > 0;
		Expression<Func<TestClass, bool>> expr2 = x => x.Name != null;

		List<Expression<Func<TestClass, bool>>> expressions = new() { expr1, expr2 };

		// Act

		Expression<Func<TestClass, bool>>? combinedExpr = Collections.CombineExpressions(expressions);
		Func<TestClass, bool>? func = combinedExpr?.CompileFast();

		// Assert

		combinedExpr.ShouldNotBeNull();

		// Test with valid object

		TestClass validObj = new() { Id = 1, Name = "test" };
		func!(validObj).ShouldBeTrue();

		// Test with invalid object (Id = 0)

		TestClass invalidObj1 = new() { Id = 0, Name = "test" };
		func(invalidObj1).ShouldBeFalse();

		// Test with invalid object (Name = null)

		TestClass invalidObj2 = new() { Id = 1, Name = null };
		func(invalidObj2).ShouldBeFalse();
	}

	[Fact]
	public void CombineExpressions_WithSingleExpression_ReturnsSameExpression()
	{
		// Arrange

		Expression<Func<TestClass, bool>> expr = x => x.Id > 0;
		List<Expression<Func<TestClass, bool>>> expressions = new() { expr };

		// Act

		Expression<Func<TestClass, bool>>? result = Collections.CombineExpressions(expressions);
		Func<TestClass, bool>? func = result?.CompileFast();

		// Assert

		result.ShouldNotBeNull();

		// Test with valid object

		TestClass validObj = new() { Id = 1 };
		func!(validObj).ShouldBeTrue();

		// Test with invalid object

		TestClass invalidObj = new() { Id = 0 };
		func(invalidObj).ShouldBeFalse();
	}

	[Fact]
	public void CombineExpressions_WithEmptyList_ReturnsNull()
	{
		// Arrange

		List<Expression<Func<TestClass, bool>>> expressions = new();

		// Act

		Expression<Func<TestClass, bool>>? result = Collections.CombineExpressions(expressions);

		// Assert

		result.ShouldBeNull();
	}

	#endregion

	#region ArrayTraverse Tests


	[Fact]
	public void ArrayTraverse_IteratesThroughMultidimensionalArray()
	{
		// Arrange

		int[,] array = new int[2, 3] { { 1, 2, 3 }, { 4, 5, 6 } };
		ArrayTraverse walker = new(array);
		List<int> visited = new();

		// Act

		do
		{
			visited.Add((int)array.GetValue(walker.Position)!);
		} while (walker.Step());

		// Assert

		visited.Count.ShouldBe(6);
		visited.ShouldBe(new List<int> { 1, 4, 2, 5, 3, 6 });
	}

	[Fact]
	public void ArrayTraverse_WithEmptyArray_DoesNotIterate()
	{
		// Arrange

		int[,] array = new int[0, 0];
		ArrayTraverse walker = new(array);
		bool visited = false;

		// Act

		if (walker.Step())
		{
			visited = true;
		}

		// Assert

		visited.ShouldBeFalse();
	}

	#endregion

	#region ReplaceParameterVisitor Tests


	[Fact]
	public void ReplaceParameterVisitor_ReplacesParameterInExpression()
	{
		// Arrange

		_ = Expression.Parameter(typeof(TestClass), "old");
		ParameterExpression newParam = Expression.Parameter(typeof(TestClass), "new");

		Expression<Func<TestClass, bool>> expr = x => x.Id > 0;

		// Create a visitor with replacement parameters

		ReplaceParameterVisitor visitor = new(expr.Parameters[0], newParam);

		// Act

		Expression result = visitor.Visit(expr.Body);

		// Assert

		result.ShouldNotBeNull();

		// Check that the parameter was replaced

		Expression<Func<TestClass, bool>> lambda = Expression.Lambda<Func<TestClass, bool>>(result, newParam);
		Func<TestClass, bool> func = lambda.CompileFast();

		TestClass testObj = new() { Id = 1 };
		func(testObj).ShouldBeTrue();
	}

	#endregion

	#region FIFO and LRU Dictionary Tests


	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	public void FixedFIFODictionary_BasicAddAndEviction(int capacity)
	{
		// Arrange

		FixedFifoDictionary<int, string> dict = new(capacity);

		// Act

		for (int i = 0; i < capacity; i++)
		{
			dict.Add(i, $"v{i}");
		}

		dict.Count.ShouldBe(capacity);

		// Add one more to trigger eviction

		dict.Add(capacity, $"v{capacity}");

		// Assert

		dict.Count.ShouldBe(capacity);

#pragma warning disable S1125 // Boolean literals should not be redundant
		dict.ContainsKey(0).ShouldBe(capacity != 1 && false); // 0 is always evicted

#pragma warning restore S1125 // Boolean literals should not be redundant
		dict.ContainsKey(capacity).ShouldBeTrue();
	}

	[Fact]
	public void FixedFIFODictionary_Constructor_ThrowsOnInvalidCapacity()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => new FixedFifoDictionary<int, string>(0));
		Should.Throw<ArgumentOutOfRangeException>(() => new FixedFifoDictionary<int, string>(-1));
	}

	[Fact]
	public void FixedFIFODictionary_Constructor_ThrowsOnOversizedSource()
	{
		Dictionary<int, string?> source = new() { [1] = "a", [2] = "b" };
		Should.Throw<ArgumentException>(() => new FixedFifoDictionary<int, string>(1, source));
	}

	[Fact]
	public void FixedFIFODictionary_Constructor_CopiesSource()
	{
		Dictionary<int, string?> source = new() { [1] = "a", [2] = "b" };
		FixedFifoDictionary<int, string> dict = new(2, source);
		dict.Count.ShouldBe(2);
		dict[1].ShouldBe("a");
		dict[2].ShouldBe("b");
	}

	[Fact]
	public void FixedFIFODictionary_Indexer_SetAndGet()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict[1] = "a";
		dict[1].ShouldBe("a");
		dict[2] = "b";
		dict[2].ShouldBe("b");
		dict[1] = "c";
		dict[1].ShouldBe("c");
	}

	[Fact]
	public void FixedFIFODictionary_Indexer_EvictsOldest()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict[1] = "a";
		dict[2] = "b";
		dict[3] = "c";
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}


#pragma warning disable S4143 // Collection elements should not be replaced unconditionally
	[Fact]
	public void FixedFIFODictionary_Add_UpdatesExisting()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(1, "b");
		dict[1].ShouldBe("b");
	}
#pragma warning restore S4143 // Collection elements should not be replaced unconditionally


	[Fact]
	public void FixedFIFODictionary_Add_KeyEviction()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.Add(3, "c");
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedFIFODictionary_Remove_Works()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Remove(1).ShouldBeTrue();
		dict.ContainsKey(1).ShouldBeFalse();
		dict.Remove(1).ShouldBeFalse();
	}

	[Fact]
	public void FixedFIFODictionary_Clear_Works()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.Clear();
		dict.Count.ShouldBe(0);
	}

	[Fact]
	public void FixedFIFODictionary_TryGetValue_Works()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.TryGetValue(1, out string? value).ShouldBeTrue();
		value.ShouldBe("a");
		dict.TryGetValue(2, out string? value2).ShouldBeFalse();
		value2.ShouldBeNull();
	}

	[Fact]
	public void FixedFIFODictionary_ContainsKey_And_Contains()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.ContainsKey(1).ShouldBeTrue();
		dict.Contains(new KeyValuePair<int, string?>(1, "a")).ShouldBeTrue();
		dict.Contains(new KeyValuePair<int, string?>(2, "b")).ShouldBeFalse();
	}

	[Fact]
	public void FixedFIFODictionary_CopyTo_And_Enumerator()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		KeyValuePair<int, string?>[] arr = new KeyValuePair<int, string?>[2];
		dict.CopyTo(arr, 0);
		arr.ShouldContain(new KeyValuePair<int, string?>(1, "a"));
		arr.ShouldContain(new KeyValuePair<int, string?>(2, "b"));
		List<KeyValuePair<int, string?>> list = dict.ToList();
		list.Count.ShouldBe(2);
	}

	[Fact]
	public void FixedFIFODictionary_Add_KeyValuePair()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(new KeyValuePair<int, string?>(1, "a"));
		dict[1].ShouldBe("a");
	}

	[Fact]
	public void FixedFIFODictionary_Remove_KeyValuePair()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Remove(new KeyValuePair<int, string?>(1, "a")).ShouldBeTrue();
		dict.Remove(new KeyValuePair<int, string?>(1, "a")).ShouldBeFalse();
	}

	[Fact]
	public void FixedFIFODictionary_Keys_Values()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.Keys.ShouldContain(1);
		dict.Keys.ShouldContain(2);
		dict.Values.ShouldContain("a");
		dict.Values.ShouldContain("b");
	}

	[Fact]
	public void FixedFIFODictionary_GetOrAdd_Works()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.GetOrAdd(1, _ => "a").ShouldBe("a");
		dict.GetOrAdd(1, _ => "b").ShouldBe("a");
		dict.GetOrAdd(2, _ => "b").ShouldBe("b");
		dict.GetOrAdd(3, _ => "c").ShouldBe("c");
		dict.ContainsKey(1).ShouldBeFalse();
	}

	[Fact]
	public void FixedFIFODictionary_IsReadOnly_IsFalse()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.IsReadOnly.ShouldBeFalse();
	}

	[Fact]
	public void FixedFIFODictionary_Enumerator_ImplementsIEnumerable()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		IEnumerator<KeyValuePair<int, string?>> enumerator = dict.GetEnumerator();
		enumerator.MoveNext().ShouldBeTrue();
	}

	// ----------------- FixedLRUDictionary Tests -----------------


	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	public void FixedLRUDictionary_BasicAddAndEviction(int capacity)
	{
		FixedLruDictionary<int, string> dict = new(capacity);

		for (int i = 0; i < capacity; i++)
		{
			dict.Add(i, $"v{i}");
		}

		dict.Count.ShouldBe(capacity);

		dict.Add(capacity, $"v{capacity}");

		dict.Count.ShouldBe(capacity);
		dict.ContainsKey(0).ShouldBeFalse();
		dict.ContainsKey(capacity).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_Constructor_ThrowsOnInvalidCapacity()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => new FixedLruDictionary<int, string>(0));
		Should.Throw<ArgumentOutOfRangeException>(() => new FixedLruDictionary<int, string>(-1));
	}

	[Fact]
	public void FixedLRUDictionary_Constructor_ThrowsOnOversizedSource()
	{
		Dictionary<int, string?> source = new() { [1] = "a", [2] = "b" };
		Should.Throw<ArgumentException>(() => new FixedLruDictionary<int, string>(1, source));
	}

	[Fact]
	public void FixedLRUDictionary_Constructor_CopiesSource()
	{
		Dictionary<int, string?> source = new() { [1] = "a", [2] = "b" };
		FixedLruDictionary<int, string> dict = new(2, source);
		dict.Count.ShouldBe(2);
		dict[1].ShouldBe("a");
		dict[2].ShouldBe("b");
	}

	[Fact]
	public void FixedLRUDictionary_Indexer_SetAndGet()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict[1] = "a";
		dict[1].ShouldBe("a");
		dict[2] = "b";
		dict[2].ShouldBe("b");
		dict[1] = "c";
		dict[1].ShouldBe("c");
	}

	[Fact]
	public void FixedLRUDictionary_Indexer_EvictsLeastRecentlyUsed()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict[1] = "a";
		dict[2] = "b";
		// Access 1 to make it most recently used

		_ = dict[1];
		dict[3] = "c";
		dict.ContainsKey(2).ShouldBeFalse();
		dict.ContainsKey(1).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_Add_ThrowsOnDuplicate()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		Should.Throw<ArgumentException>(() => dict.Add(1, "b"));
	}

	[Fact]
	public void FixedLRUDictionary_Remove_Works()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Remove(1).ShouldBeTrue();
		dict.ContainsKey(1).ShouldBeFalse();
		dict.Remove(1).ShouldBeFalse();
	}

	[Fact]
	public void FixedLRUDictionary_Clear_Works()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.Clear();
		dict.Count.ShouldBe(0);
	}

	[Fact]
	public void FixedLRUDictionary_TryGetValue_MovesToFront()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.TryGetValue(1, out string? value).ShouldBeTrue();
		value.ShouldBe("a");
		dict[1].ShouldBe("a");
		dict[2] = "b2";
		dict[3] = "c";
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_ContainsKey_And_Contains()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.ContainsKey(1).ShouldBeTrue();
		dict.Contains(new KeyValuePair<int, string?>(1, "a")).ShouldBeTrue();
		dict.Contains(new KeyValuePair<int, string?>(2, "b")).ShouldBeFalse();
	}

	[Fact]
	public void FixedLRUDictionary_CopyTo_And_Enumerator()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		KeyValuePair<int, string?>[] arr = new KeyValuePair<int, string?>[2];
		dict.CopyTo(arr, 0);
		arr.ShouldContain(new KeyValuePair<int, string?>(1, "a"));
		arr.ShouldContain(new KeyValuePair<int, string?>(2, "b"));
		List<KeyValuePair<int, string?>> list = dict.ToList();
		list.Count.ShouldBe(2);
	}

	[Fact]
	public void FixedLRUDictionary_Add_KeyValuePair()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(new KeyValuePair<int, string?>(1, "a"));
		dict[1].ShouldBe("a");
	}

	[Fact]
	public void FixedLRUDictionary_Remove_KeyValuePair()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Remove(new KeyValuePair<int, string?>(1, "a")).ShouldBeTrue();
		dict.Remove(new KeyValuePair<int, string?>(1, "a")).ShouldBeFalse();
	}

	[Fact]
	public void FixedLRUDictionary_Keys_Values()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		dict.Keys.ShouldContain(1);
		dict.Keys.ShouldContain(2);
		dict.Values.ShouldContain("a");
		dict.Values.ShouldContain("b");
	}

	[Fact]
	public void FixedLRUDictionary_GetOrAdd_Works()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.GetOrAdd(1, _ => "a").ShouldBe("a");
		dict.GetOrAdd(1, _ => "b").ShouldBe("a");
		dict.GetOrAdd(2, _ => "b").ShouldBe("b");
		dict.GetOrAdd(3, _ => "c").ShouldBe("c");
		dict.ContainsKey(1).ShouldBeFalse();
	}

	[Fact]
	public void FixedLRUDictionary_IsReadOnly_IsFalse()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.IsReadOnly.ShouldBeFalse();
	}

	[Fact]
	public void FixedLRUDictionary_Enumerator_ImplementsIEnumerable()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		IEnumerator<KeyValuePair<int, string?>> enumerator = dict.GetEnumerator();
		enumerator.MoveNext().ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_Indexer_ThrowsOnMissingKey()
	{
		FixedLruDictionary<int, string> dict = new(2);
		Should.Throw<KeyNotFoundException>(() => _ = dict[42]);
	}

	// Additional tests for FixedFIFODictionary coverage


	[Fact]
	public void FixedFIFODictionary_TrimExcess_Works()
	{
		FixedFifoDictionary<int, string> dict = new(10);
		dict.Add(1, "a");
		dict.Add(2, "b");
		Should.NotThrow(dict.TrimExcess);
		dict.Count.ShouldBe(2);
	}

	[Fact]
	public void FixedFIFODictionary_IEnumerable_GetEnumerator_Works()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		System.Collections.IEnumerable enumerable = dict;
		System.Collections.IEnumerator enumerator = enumerable.GetEnumerator();
		enumerator.MoveNext().ShouldBeTrue();
	}

	[Fact]
	public void FixedFIFODictionary_Indexer_Get_ReturnsValue()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict[1] = "a";
		dict[1].ShouldBe("a");
	}

	[Fact]
	public void FixedFIFODictionary_Indexer_Get_ThrowsOnMissingKey()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		Should.Throw<KeyNotFoundException>(() => _ = dict[42]);
	}

	[Fact]
	public void FixedFIFODictionary_TryGetValue_ReturnsFalseForMissingKey()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.TryGetValue(42, out string? value).ShouldBeFalse();
		value.ShouldBeNull();
	}

	[Fact]
	public void FixedFIFODictionary_TryAdd_ReturnsFalseForExistingKey()
	{
		FixedFifoDictionary<int, string> dict = new(5);
		dict.Add(1, "a");
		bool result = dict.TryAdd(1, "b");
		result.ShouldBeFalse();
		dict[1].ShouldBe("a");
	}

	[Fact]
	public void FixedFIFODictionary_TryAdd_EvictsOldestWhenFull()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.TryAdd(1, "a").ShouldBeTrue();
		dict.TryAdd(2, "b").ShouldBeTrue();
		dict.TryAdd(3, "c").ShouldBeTrue();
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedFIFODictionary_Add_KeyValuePair_EvictsOldestWhenFull()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(new KeyValuePair<int, string?>(1, "a"));
		dict.Add(new KeyValuePair<int, string?>(2, "b"));
		dict.Add(new KeyValuePair<int, string?>(3, "c"));
		dict.Count.ShouldBe(2);
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedFIFODictionary_Add_KeyValuePair_UpdatesExistingItem()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(new KeyValuePair<int, string?>(1, "a"));
		dict.Add(new KeyValuePair<int, string?>(1, "b"));
		dict[1].ShouldBe("b");
		dict.Count.ShouldBe(1);
	}

	[Fact]
	public void FixedFIFODictionary_GetOrAdd_AddsNewItemWhenMissing()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		string result = dict.GetOrAdd(1, k => $"value{k}");
		result.ShouldBe("value1");
		dict[1].ShouldBe("value1");
	}

	[Fact]
	public void FixedFIFODictionary_GetOrAdd_ReturnsExistingValue()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "existing");
		string result = dict.GetOrAdd(1, _ => "new");
		result.ShouldBe("existing");
		dict[1].ShouldBe("existing");
	}

	[Fact]
	public void FixedFIFODictionary_GetOrAdd_EvictsOldestWhenFull()
	{
		FixedFifoDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		string result = dict.GetOrAdd(3, _ => "c");
		result.ShouldBe("c");
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	// Additional tests for FixedLRUDictionary coverage


	[Fact]
	public void FixedLRUDictionary_TrimExcess_Works()
	{
		FixedLruDictionary<int, string> dict = new(10);
		dict.Add(1, "a");
		dict.Add(2, "b");
		Should.NotThrow(dict.TrimExcess);
		dict.Count.ShouldBe(2);
	}

	[Fact]
	public void FixedLRUDictionary_IEnumerable_GetEnumerator_Works()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		System.Collections.IEnumerable enumerable = dict;
		System.Collections.IEnumerator enumerator = enumerable.GetEnumerator();
		enumerator.MoveNext().ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_Indexer_Get_WhenKeyAlreadyAtFront_ReturnsValue()
	{
		FixedLruDictionary<int, string> dict = new(3);
		dict[3] = "c";
		dict[2] = "b";
		dict[1] = "a"; // 1 is now at front

		string result = dict[1]!; // Should return without moving since already at front

		result.ShouldBe("a");
		dict.Count.ShouldBe(3);
	}

	[Fact]
	public void FixedLRUDictionary_Indexer_Get_MovesItemToFront()
	{
		FixedLruDictionary<int, string> dict = new(3);
		dict[1] = "a";
		dict[2] = "b";
		dict[3] = "c";
		// Access 1 to move it to front

		string result = dict[1]!;
		result.ShouldBe("a");
		// Add another item, should evict 2 (not 1)

		dict[4] = "d";
		dict.ContainsKey(2).ShouldBeFalse();
		dict.ContainsKey(1).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_TryGetValue_WhenKeyAlreadyAtFront_ReturnsTrue()
	{
		FixedLruDictionary<int, string> dict = new(3);
		dict[3] = "c";
		dict[2] = "b";
		dict[1] = "a"; // 1 is now at front

		bool result = dict.TryGetValue(1, out string? value); // Should return without moving

		result.ShouldBeTrue();
		value.ShouldBe("a");
	}

	[Fact]
	public void FixedLRUDictionary_TryGetValue_ReturnsFalseForMissingKey()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.TryGetValue(42, out string? value).ShouldBeFalse();
		value.ShouldBeNull();
	}

	[Fact]
	public void FixedLRUDictionary_GetOrAdd_WhenKeyExistsAtFront_ReturnsExistingValue()
	{
		FixedLruDictionary<int, string> dict = new(3);
		dict[3] = "c";
		dict[2] = "b";
		dict[1] = "a"; // 1 is now at front

		string result = dict.GetOrAdd(1, _ => "new");
		result.ShouldBe("a");
	}

	[Fact]
	public void FixedLRUDictionary_GetOrAdd_WhenKeyExistsNotAtFront_MovesToFront()
	{
		FixedLruDictionary<int, string> dict = new(3);
		dict[1] = "a";
		dict[2] = "b";
		dict[3] = "c"; // 3 is at front, 1 is at back

		string result = dict.GetOrAdd(1, _ => "new");
		result.ShouldBe("a");
		// Add another item, should evict 2 (not 1 since it was moved to front)

		dict[4] = "d";
		dict.ContainsKey(2).ShouldBeFalse();
		dict.ContainsKey(1).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_GetOrAdd_AddsNewItemWhenMissing()
	{
		FixedLruDictionary<int, string> dict = new(2);
		string result = dict.GetOrAdd(1, k => $"value{k}");
		result.ShouldBe("value1");
		dict[1].ShouldBe("value1");
	}

	[Fact]
	public void FixedLRUDictionary_GetOrAdd_EvictsLeastRecentlyUsedWhenFull()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.Add(1, "a");
		dict.Add(2, "b");
		string result = dict.GetOrAdd(3, _ => "c");
		result.ShouldBe("c");
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	[Fact]
	public void FixedLRUDictionary_TryAdd_ReturnsFalseForExistingKey()
	{
		FixedLruDictionary<int, string> dict = new(5);
		dict.Add(1, "a");
		bool result = dict.TryAdd(1, "b");
		result.ShouldBeFalse();
		dict[1].ShouldBe("a");
	}

	[Fact]
	public void FixedLRUDictionary_TryAdd_EvictsLeastRecentlyUsedWhenFull()
	{
		FixedLruDictionary<int, string> dict = new(2);
		dict.TryAdd(1, "a").ShouldBeTrue();
		dict.TryAdd(2, "b").ShouldBeTrue();
		dict.TryAdd(3, "c").ShouldBeTrue();
		dict.ContainsKey(1).ShouldBeFalse();
		dict.ContainsKey(2).ShouldBeTrue();
		dict.ContainsKey(3).ShouldBeTrue();
	}

	#endregion


	public enum ClearTrimCollectionType
	{
		List,
		Dictionary,
		HashSet,
		Stack,
		Queue
	}

	[Theory]
	[InlineData(ClearTrimCollectionType.List)]
	[InlineData(ClearTrimCollectionType.Dictionary)]
	[InlineData(ClearTrimCollectionType.HashSet)]
	[InlineData(ClearTrimCollectionType.Stack)]
	[InlineData(ClearTrimCollectionType.Queue)]
	public void ClearTrim_Null_DoesNothing(ClearTrimCollectionType collectionType)
	{
		Should.NotThrow(() =>
		{
			switch (collectionType)
			{
				case ClearTrimCollectionType.List:
					List<int>? list = null;
					list.ClearTrim();
					list.ClearTrim(forceGc: true);
					break;
				case ClearTrimCollectionType.Dictionary:
					Dictionary<int, string>? dict = null;
					dict.ClearTrim();
					dict.ClearTrim(forceGc: true);
					break;
				case ClearTrimCollectionType.HashSet:
					HashSet<int>? set = null;
					set.ClearTrim();
					set.ClearTrim(forceGc: true);
					break;
				case ClearTrimCollectionType.Stack:
					Stack<int>? stack = null;
					stack.ClearTrim();
					stack.ClearTrim(forceGc: true);
					break;
				case ClearTrimCollectionType.Queue:
					Queue<int>? queue = null;
					queue.ClearTrim();
					queue.ClearTrim(forceGc: true);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(collectionType));
			}
		});
	}

	[Theory]
	[InlineData(ClearTrimCollectionType.List)]
	[InlineData(ClearTrimCollectionType.Dictionary)]
	[InlineData(ClearTrimCollectionType.HashSet)]
	[InlineData(ClearTrimCollectionType.Stack)]
	[InlineData(ClearTrimCollectionType.Queue)]
	public void ClearTrim_ClearsAndTrims(ClearTrimCollectionType collectionType)
	{
		switch (collectionType)
		{
			case ClearTrimCollectionType.List:
				List<int> list = new() { 1, 2, 3, 4 };
				list.ClearTrim();
				list.Count.ShouldBe(0);
				break;
			case ClearTrimCollectionType.Dictionary:
				Dictionary<int, string> dict = new() { [1] = "a", [2] = "b" };
				dict.ClearTrim();
				dict.Count.ShouldBe(0);
				break;
			case ClearTrimCollectionType.HashSet:
				HashSet<int> set = new() { 1, 2, 3 };
				set.ClearTrim();
				set.Count.ShouldBe(0);
				break;
			case ClearTrimCollectionType.Stack:
				Stack<int> stack = new();
				stack.Push(1);
				stack.Push(2);
				stack.ClearTrim();
				stack.Count.ShouldBe(0);
				break;
			case ClearTrimCollectionType.Queue:
				Queue<int> queue = new();
				queue.Enqueue(1);
				queue.Enqueue(2);
				queue.ClearTrim();
				queue.Count.ShouldBe(0);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(collectionType));
		}
	}

	[Theory]
	[InlineData(ClearTrimCollectionType.List)]
	[InlineData(ClearTrimCollectionType.Dictionary)]
	[InlineData(ClearTrimCollectionType.HashSet)]
	[InlineData(ClearTrimCollectionType.Stack)]
	[InlineData(ClearTrimCollectionType.Queue)]
	public void ClearTrim_ClearsAndTrims_WithForceGc(ClearTrimCollectionType collectionType)
	{
		Should.NotThrow(() =>
		{
			switch (collectionType)
			{
				case ClearTrimCollectionType.List:
					List<int> list = new() { 1, 2, 3, 4 };
					list.ClearTrim(forceGc: true);
					list.Count.ShouldBe(0);
					break;
				case ClearTrimCollectionType.Dictionary:
					Dictionary<int, string> dict = new() { [1] = "a", [2] = "b" };
					dict.ClearTrim(forceGc: true);
					dict.Count.ShouldBe(0);
					break;
				case ClearTrimCollectionType.HashSet:
					HashSet<int> set = new() { 1, 2, 3 };
					set.ClearTrim(forceGc: true);
					set.Count.ShouldBe(0);
					break;
				case ClearTrimCollectionType.Stack:
					Stack<int> stack = new();
					stack.Push(1);
					stack.Push(2);
					stack.ClearTrim(forceGc: true);
					stack.Count.ShouldBe(0);
					break;
				case ClearTrimCollectionType.Queue:
					Queue<int> queue = new();
					queue.Enqueue(1);
					queue.Enqueue(2);
					queue.ClearTrim(forceGc: true);
					queue.Count.ShouldBe(0);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(collectionType));
			}
		});
	}
}
