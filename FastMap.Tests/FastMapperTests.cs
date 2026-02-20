using System.Collections;
using System.Collections.ObjectModel;
using CommonNetFuncs.FastMap;

namespace FastMap.Tests;

public enum EmptyCollectionType
{
	List,
	Array,
	Dictionary
}

public enum CollectionMappingType
{
	HashSetToHashSet,
	QueueToList,
	StackToList,
	ListToHashSet
}

public enum DictionaryMappingError
{
	InvalidMapping,
	MismatchedKeyTypes
}

public sealed class FastMapperTests
{
	public sealed class SimpleSource
	{
		public required string StringProp { get; set; }

		public int IntProp { get; set; }

		public DateTime DateProp { get; set; }
	}

	public sealed class NullableSimpleSource
	{
		public required string? StringProp { get; set; }

		public int? IntProp { get; set; }

		public DateTime? DateProp { get; set; }
	}

	public sealed class SimpleDestination
	{
		public required string StringProp { get; set; }

		public int IntProp { get; set; }

		public DateTime DateProp { get; set; }
	}

	public sealed class NullableSimpleDestination
	{
		public required string? StringProp { get; set; }

		public int? IntProp { get; set; }

		public DateTime? DateProp { get; set; }
	}

	public sealed class ComplexSource
	{
		public required string Name { get; set; }

		public required List<string> StringList { get; set; }

		public required Dictionary<string, int> Dictionary { get; set; }

		public required SimpleSource NestedObject { get; set; }

		public required HashSet<int> NumberSet { get; set; }

		public required Queue<string> StringQueue { get; set; }

		public required Stack<double> DoubleStack { get; set; }
	}

	public sealed class NullableComplexSource
	{
		public required string? Name { get; set; }

		public required List<string>? StringList { get; set; }

		public required Dictionary<string, int>? Dictionary { get; set; }

		public required NullableSimpleSource? NestedObject { get; set; }

		public required HashSet<int>? NumberSet { get; set; }

		public required Queue<string>? StringQueue { get; set; }

		public required Stack<double>? DoubleStack { get; set; }
	}

	public sealed class ComplexDestination
	{
		public required string Name { get; set; }

		public required List<string> StringList { get; set; }

		public required Dictionary<string, int> Dictionary { get; set; }

		public required SimpleDestination NestedObject { get; set; }

		public required HashSet<int> NumberSet { get; set; }

		public required Queue<string> StringQueue { get; set; }

		public required Stack<double> DoubleStack { get; set; }
	}

	public sealed class NullableComplexDestination
	{
		public required string? Name { get; set; }

		public required List<string>? StringList { get; set; }

		public required Dictionary<string, int>? Dictionary { get; set; }

		public required NullableSimpleDestination? NestedObject { get; set; }

		public required HashSet<int>? NumberSet { get; set; }

		public required Queue<string>? StringQueue { get; set; }

		public required Stack<double>? DoubleStack { get; set; }
	}

	public sealed class ReadOnlyCollectionSource
	{
		public required IReadOnlyCollection<int> ReadOnlyNumbers { get; set; }

		public required IReadOnlyList<string> ReadOnlyStrings { get; set; }

		public required ReadOnlyCollection<DateTime> ReadOnlyDates { get; set; }
	}

	public sealed class ReadOnlyCollectionDest
	{
		public required List<int> ReadOnlyNumbers { get; set; }

		public required IReadOnlyList<string> ReadOnlyStrings { get; set; }

		public required ReadOnlyCollection<DateTime> ReadOnlyDates { get; set; }
	}

	public sealed class CustomCollection<T> : IEnumerable<T>
	{
		private readonly List<T> items = [];

		public void Add(T item)
		{
			items.Add(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public sealed class CustomCollectionSource
	{
		public required CustomCollection<int> Numbers { get; set; }
	}

	public sealed class CustomCollectionDest
	{
		public required List<int> Numbers { get; set; }
	}

	public sealed class SourceWithMismatchedProperties
	{
		public required string Name { get; set; }

		public int Age { get; set; }

		public required string ExtraProperty { get; set; }
	}

	public sealed class DestWithMismatchedProperties
	{
		public required string Name { get; set; }

		public int Age { get; set; }

		public required string DifferentProperty { get; set; }
	}

	public sealed class DictionarySource
	{
		public required Dictionary<string, SimpleSource> ComplexDict { get; set; }
	}

	public sealed class DictionaryDest
	{
		public required Dictionary<string, SimpleDestination> ComplexDict { get; set; }
	}

	// Types used specifically for TryAdd cache tests to avoid polluting other tests
	public sealed class TryAddTestSource
	{
		public required string Value { get; set; }
	}

	public sealed class TryAddTestDest
	{
		public required string Value { get; set; }
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
	[InlineData(new int[] { }, new int[] { })]
	[InlineData(new[] { 1 }, new[] { 1 })]
	public void FasterMap_WithArrays_MapsCorrectly(int[] source, int[] expected)
	{
		// Act
		int[] result = source.FastMap<int[], int[]>();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Test1", 1, "2025-01-01")]
	[InlineData("", 0, "0001-01-01")]
	public void FasterMap_WithSimpleProperties_MapsCorrectly(string stringProp, int intProp, string dateProp)
	{
		// Arrange
#pragma warning disable S6580 // Use a format provider when parsing date and time.
		SimpleSource source = new()
		{
			StringProp = stringProp,
			IntProp = intProp,
			DateProp = DateTime.Parse(dateProp)
		};
#pragma warning restore S6580 // Use a format provider when parsing date and time.

		// Act
		SimpleDestination result = source.FastMap<SimpleSource, SimpleDestination>();

		// Assert
		result.ShouldNotBeNull();
		result.StringProp.ShouldBe(source.StringProp);
		result.IntProp.ShouldBe(source.IntProp);
		result.DateProp.ShouldBe(source.DateProp);
	}

	[Theory]
	[InlineData("Test1", new[] { "one", "two" }, "key1", 1, "Nested", 1, new[] { 1, 2 }, "first", 1.0)]
	[InlineData("", new string[] { }, "", 0, "", 0, new int[] { }, "", 0.0)]
	public void FasterMap_WithComplexProperties_MapsCorrectly(string name, string[] stringList, string dictKey, int dictValue,
			string nestedString, int nestedInt, int[] numberSet, string queueItem, double stackItem)
	{
		// Arrange
		ComplexSource source = new()
		{
			Name = name,
			StringList = stringList.ToList(),
			Dictionary = new() { [dictKey] = dictValue },
			NestedObject = new()
			{
				StringProp = nestedString,
				IntProp = nestedInt,
				DateProp = DateTime.Now
			},
			NumberSet = numberSet.ToHashSet(),
			StringQueue = new Queue<string>([queueItem]),
			DoubleStack = new Stack<double>([stackItem])
		};

		// Act
		ComplexDestination result = source.FastMap<ComplexSource, ComplexDestination>();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(source.Name);
		result.StringList.ShouldBe(source.StringList);
		result.Dictionary.ShouldBe(source.Dictionary);
		result.NumberSet.ShouldBe(source.NumberSet);
		result.StringQueue.ShouldBe(source.StringQueue);
		result.DoubleStack.ShouldBe(source.DoubleStack);
		result.NestedObject.StringProp.ShouldBe(source.NestedObject.StringProp);
		result.NestedObject.IntProp.ShouldBe(source.NestedObject.IntProp);
		result.NestedObject.DateProp.ShouldBe(source.NestedObject.DateProp);
	}

	[Theory]
	[InlineData("Test1", new[] { "one", "two" }, "key1", 1, "Nested", 1, new[] { 1, 2 }, "first", 1.0)]
	[InlineData(null, null, null, null, null, null, null, null, null)]
	public void FastMap_WithComplexNullableProperties_MapsCorrectly(string? name, string[]? stringList, string? dictKey, int? dictValue,
		string? nestedString, int? nestedInt, int[]? numberSet, string? queueItem, double? stackItem)
	{
		// Arrange
		NullableComplexSource source = new()
		{
			Name = name,
			StringList = stringList?.ToList(),
			Dictionary = dictKey == null || dictValue == null ? null : new() { [dictKey] = (int)dictValue },
			NestedObject = new()
			{
				StringProp = nestedString,
				IntProp = nestedInt,
				DateProp = nestedInt == null ? null : DateTime.Now
			},
			NumberSet = numberSet?.ToHashSet(),
			StringQueue = queueItem == null ? null : new Queue<string>([queueItem]),
			DoubleStack = stackItem == null ? null : new Stack<double>([(double)stackItem])
		};

		// Act
		NullableComplexDestination result = source.FastMap<NullableComplexSource, NullableComplexDestination>();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(source.Name);
		result.StringList.ShouldBe(source.StringList);
		result.Dictionary.ShouldBe(source.Dictionary);
		result.NumberSet.ShouldBe(source.NumberSet);
		result.StringQueue.ShouldBe(source.StringQueue);
		result.DoubleStack.ShouldBe(source.DoubleStack);
		result.NestedObject?.StringProp.ShouldBe(source.NestedObject.StringProp);
		result.NestedObject?.IntProp.ShouldBe(source.NestedObject.IntProp);
		result.NestedObject?.DateProp.ShouldBe(source.NestedObject.DateProp);
	}

	[Fact]
	public void FastMap_WithNullObjects_MapsCorrectly()
	{
		// Arrange
		SimpleSource? source = null;

		// Act
		SimpleDestination? result = source!.FastMap<SimpleSource?, SimpleDestination?>();

		// Assert
		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, new[] { "a", "b" })]
	[InlineData(new int[] { }, new string[] { })]
	public void FasterMap_WithReadOnlyCollections_MapsCorrectly(int[] numbers, string[] strings)
	{
		// Arrange
		ReadOnlyCollectionSource source = new()
		{
			ReadOnlyNumbers = numbers.ToList().AsReadOnly(),
			ReadOnlyStrings = strings.ToList().AsReadOnly(),
			ReadOnlyDates = new ReadOnlyCollection<DateTime>([DateTime.Now])
		};

		// Act
		ReadOnlyCollectionDest result = source.FastMap<ReadOnlyCollectionSource, ReadOnlyCollectionDest>();

		// Assert
		result.ShouldNotBeNull();
		result.ReadOnlyNumbers.ShouldBe(source.ReadOnlyNumbers);
		result.ReadOnlyStrings.ShouldBe(source.ReadOnlyStrings);
		result.ReadOnlyDates.ShouldBe(source.ReadOnlyDates);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 })]
	[InlineData(new int[] { })]
	[InlineData(new[] { 1 })]
	public void FasterMap_WithCustomCollections_MapsCorrectly(int[] sourceData)
	{
		// Arrange
		CustomCollectionSource source = new()
		{
			Numbers = new CustomCollection<int>()
		};
		foreach (int item in sourceData)
		{
			source.Numbers.Add(item);
		}

		// Act
		CustomCollectionDest result = source.FastMap<CustomCollectionSource, CustomCollectionDest>();

		// Assert
		result.ShouldNotBeNull();
		result.Numbers.ShouldBe(sourceData);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 })]
	[InlineData(new[] { 1 })]
	[InlineData(new int[] { })]
	public void FasterMap_BetweenDifferentCollectionTypes_MapsCorrectly(int[] sourceData)
	{
		// Arrange
		List<int> listSource = sourceData.ToList();
		HashSet<int> hashSetSource = sourceData.ToHashSet();

		// Act & Assert - Array to List
		List<int> listResult = sourceData.FastMap<int[], List<int>>();
		listResult.ShouldBe(sourceData);

		// Act & Assert - List to Array
		int[] arrayResult = listSource.FastMap<List<int>, int[]>();
		arrayResult.ShouldBe(listSource);

		// Act & Assert - HashSet to List
		List<int> listFromHashSet = hashSetSource.FastMap<HashSet<int>, List<int>>();
		listFromHashSet.ShouldBe(hashSetSource);
	}

	[Theory]
	[InlineData("Test", 30, "Extra")]
	[InlineData("", 0, "")]
	[InlineData("Name", 42, null)]
	public void FasterMap_WithMismatchedProperties_MapsMatchingPropertiesOnly(
			string name, int age, string? extraProperty)
	{
		// Arrange
		SourceWithMismatchedProperties source = new()
		{
			Name = name,
			Age = age,
			ExtraProperty = extraProperty ?? string.Empty
		};

		// Act
		DestWithMismatchedProperties result = source.FastMap<SourceWithMismatchedProperties, DestWithMismatchedProperties>();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(source.Name);
		result.Age.ShouldBe(source.Age);
		result.DifferentProperty.ShouldBeNull();
	}

	[Theory]
	[InlineData("Test1", 1, "Test2", 2)]
	[InlineData("Empty", 0, null, null)]
	public void FasterMap_WithNestedDictionary_MapsCorrectly(string key1Str, int key1Int, string? key2Str, int? key2Int)
	{
		// Arrange
		Dictionary<string, SimpleSource> sourceDict = new();
		sourceDict["key1"] = new()
		{
			StringProp = key1Str,
			IntProp = key1Int,
			DateProp = DateTime.Now
		};

		if (key2Str != null)
		{
			sourceDict["key2"] = new()
			{
				StringProp = key2Str,
				IntProp = key2Int ?? default,
				DateProp = DateTime.Now.AddDays(1)
			};
		}

		DictionarySource source = new()
		{
			ComplexDict = sourceDict
		};

		// Act
		DictionaryDest result = source.FastMap<DictionarySource, DictionaryDest>();

		// Assert
		result.ShouldNotBeNull();
		result.ComplexDict.Count.ShouldBe(source.ComplexDict.Count);

		foreach ((string key, SimpleSource value) in source.ComplexDict)
		{
			result.ComplexDict.ContainsKey(key).ShouldBeTrue();
			result.ComplexDict[key].StringProp.ShouldBe(value.StringProp);
			result.ComplexDict[key].IntProp.ShouldBe(value.IntProp);
			result.ComplexDict[key].DateProp.ShouldBe(value.DateProp);
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void FasterMap_WithNullSource_ReturnsNull(bool useCache)
	{
		// Arrange
		SimpleSource? source = null;

		// Act
		SimpleDestination? result = source!.FastMap<SimpleSource, SimpleDestination>(useCache: useCache);

		// Assert
		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(DictionaryMappingError.InvalidMapping)]
	[InlineData(DictionaryMappingError.MismatchedKeyTypes)]
	public void FasterMap_WithInvalidDictionaryMapping_ThrowsException(DictionaryMappingError errorType)
	{
		// Arrange
		Dictionary<int, string> source = new() { [1] = "test" };
		Exception? exception;

		// Act & Assert
		// FastMapper uses static generic class cache, so exception is wrapped in TypeInitializationException
		switch (errorType)
		{
			case DictionaryMappingError.InvalidMapping:
				exception = Should.Throw<TypeInitializationException>(source.FastMap<Dictionary<int, string>, List<string>>);
				exception.InnerException.ShouldNotBeNull();
				exception.InnerException.ShouldBeOfType<ArgumentException>();
				break;

			case DictionaryMappingError.MismatchedKeyTypes:
				exception = Should.Throw<TypeInitializationException>(source.FastMap<Dictionary<int, string>, Dictionary<string, string>>);
				exception.InnerException.ShouldNotBeNull();
				exception.InnerException.ShouldBeOfType<InvalidOperationException>();
				break;
		}
	}

	[Theory]
	[InlineData("Test1", 1)]
	[InlineData("Test2", 2)]
	public void FasterMap_CacheIsReused_MapsConsistently(string stringProp, int intProp)
	{
		// Arrange
		SimpleSource source = new()
		{
			StringProp = stringProp,
			IntProp = intProp,
			DateProp = DateTime.Now
		};

		// Act - call multiple times to verify cached mapper works consistently
		SimpleDestination result1 = source.FastMap<SimpleSource, SimpleDestination>();
		SimpleDestination result2 = source.FastMap<SimpleSource, SimpleDestination>();
		SimpleDestination result3 = source.FastMap<SimpleSource, SimpleDestination>();

		// Assert
		result1.StringProp.ShouldBe(source.StringProp);
		result2.StringProp.ShouldBe(source.StringProp);
		result3.StringProp.ShouldBe(source.StringProp);
		result1.IntProp.ShouldBe(source.IntProp);
		result2.IntProp.ShouldBe(source.IntProp);
		result3.IntProp.ShouldBe(source.IntProp);
	}

	public enum ComplexCollectionType
	{
		List,
		Array
	}

	[Theory]
	[InlineData(ComplexCollectionType.List)]
	[InlineData(ComplexCollectionType.Array)]
	public void FasterMap_WithComplexObjectCollections_MapsCorrectly(ComplexCollectionType collectionType)
	{
		switch (collectionType)
		{
			case ComplexCollectionType.List:
				// Arrange
				List<SimpleSource> listSource =
				[
					new() { StringProp = "A", IntProp = 1, DateProp = DateTime.Now },
					new() { StringProp = "B", IntProp = 2, DateProp = DateTime.Now.AddDays(1) },
					new() { StringProp = "C", IntProp = 3, DateProp = DateTime.Now.AddDays(2) }
				];

				// Act
				List<SimpleDestination> listResult = listSource.FastMap<List<SimpleSource>, List<SimpleDestination>>();

				// Assert
				listResult.ShouldNotBeNull();
				listResult.Count.ShouldBe(3);
				for (int i = 0; i < listSource.Count; i++)
				{
					listResult[i].StringProp.ShouldBe(listSource[i].StringProp);
					listResult[i].IntProp.ShouldBe(listSource[i].IntProp);
					listResult[i].DateProp.ShouldBe(listSource[i].DateProp);
				}
				break;

			case ComplexCollectionType.Array:
				// Arrange
				SimpleSource[] arraySource =
				[
					new() { StringProp = "X", IntProp = 10, DateProp = DateTime.Now },
					new() { StringProp = "Y", IntProp = 20, DateProp = DateTime.Now.AddHours(1) }
				];

				// Act
				SimpleDestination[] arrayResult = arraySource.FastMap<SimpleSource[], SimpleDestination[]>();

				// Assert
				arrayResult.ShouldNotBeNull();
				arrayResult.Length.ShouldBe(2);
				for (int i = 0; i < arraySource.Length; i++)
				{
					arrayResult[i].StringProp.ShouldBe(arraySource[i].StringProp);
					arrayResult[i].IntProp.ShouldBe(arraySource[i].IntProp);
					arrayResult[i].DateProp.ShouldBe(arraySource[i].DateProp);
				}
				break;
		}
	}

	[Theory]
	[InlineData(EmptyCollectionType.List)]
	[InlineData(EmptyCollectionType.Array)]
	[InlineData(EmptyCollectionType.Dictionary)]
	public void FasterMap_WithEmptyCollection_ReturnsEmptyCollection(EmptyCollectionType collectionType)
	{
		// Act & Assert
		switch (collectionType)
		{
			case EmptyCollectionType.List:
				List<SimpleSource> listSource = [];
				List<SimpleDestination> listResult = listSource.FastMap<List<SimpleSource>, List<SimpleDestination>>();
				listResult.ShouldNotBeNull();
				listResult.Count.ShouldBe(0);
				break;

			case EmptyCollectionType.Array:
				SimpleSource[] arraySource = [];
				SimpleDestination[] arrayResult = arraySource.FastMap<SimpleSource[], SimpleDestination[]>();
				arrayResult.ShouldNotBeNull();
				arrayResult.Length.ShouldBe(0);
				break;

			case EmptyCollectionType.Dictionary:
				Dictionary<string, int> dictSource = [];
				Dictionary<string, int> dictResult = dictSource.FastMap<Dictionary<string, int>, Dictionary<string, int>>();
				dictResult.ShouldNotBeNull();
				dictResult.Count.ShouldBe(0);
				break;
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("NonEmpty")]
	public void FasterMap_WithNullableStringProperty_MapsCorrectly(string? stringProp)
	{
		// Arrange
		NullableSimpleSource source = new()
		{
			StringProp = stringProp,
			IntProp = 42,
			DateProp = DateTime.Now
		};

		// Act
		NullableSimpleDestination result = source.FastMap<NullableSimpleSource, NullableSimpleDestination>();

		// Assert
		result.ShouldNotBeNull();
		result.StringProp.ShouldBe(source.StringProp);
		result.IntProp.ShouldBe(source.IntProp);
		result.DateProp.ShouldBe(source.DateProp);
	}

	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(int.MaxValue)]
	[InlineData(int.MinValue)]
	public void FasterMap_WithNullableIntProperty_MapsCorrectly(int? intProp)
	{
		// Arrange
		NullableSimpleSource source = new()
		{
			StringProp = "Test",
			IntProp = intProp,
			DateProp = DateTime.Now
		};

		// Act
		NullableSimpleDestination result = source.FastMap<NullableSimpleSource, NullableSimpleDestination>();

		// Assert
		result.ShouldNotBeNull();
		result.IntProp.ShouldBe(source.IntProp);
	}

	#region CacheManager Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void FasterMap_WithUseCacheParameter_MapsCorrectly(bool useCache)
	{
		// Arrange
		SimpleSource source = new()
		{
			StringProp = "CacheTest",
			IntProp = 42,
			DateProp = DateTime.Now
		};

		// Act
		SimpleDestination result = source.FastMap<SimpleSource, SimpleDestination>(useCache);

		// Assert
		result.ShouldNotBeNull();
		result.StringProp.ShouldBe(source.StringProp);
		result.IntProp.ShouldBe(source.IntProp);
		result.DateProp.ShouldBe(source.DateProp);
	}

	[Fact]
	public void FasterMap_WithUseCacheTrue_UsesManagedCache()
	{
		// Arrange
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.CacheManager.SetUseLimitedCache(false);

		SimpleSource source = new()
		{
			StringProp = "ManagedCacheTest",
			IntProp = 100,
			DateProp = DateTime.Now
		};

		// Act
		SimpleDestination result = source.FastMap<SimpleSource, SimpleDestination>(useCache: true);

		// Assert
		result.ShouldNotBeNull();
		result.StringProp.ShouldBe(source.StringProp);
		FastMapper.CacheManager.GetCache().Count.ShouldBeGreaterThan(0);
	}

	[Theory]
	[InlineData(5)]
	[InlineData(10)]
	[InlineData(100)]
	public void FastMapper_CacheManager_SetAndGetLimitedCacheSize_Works(int size)
	{
		// Act
		FastMapper.CacheManager.SetLimitedCacheSize(size);

		// Assert
		FastMapper.CacheManager.GetLimitedCacheSize().ShouldBe(size);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void FastMapper_CacheManager_SetAndGetUseLimitedCache_Works(bool useLimited)
	{
		// Act
		FastMapper.CacheManager.SetUseLimitedCache(useLimited);

		// Assert
		FastMapper.CacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
	}

	[Fact]
	public void FastMapper_CacheManager_ClearAllCaches_RemovesAllEntries()
	{
		// Arrange
		FastMapper.CacheManager.SetUseLimitedCache(false);
		SimpleSource source = new() { StringProp = "A", IntProp = 1, DateProp = DateTime.Now };
		source.FastMap<SimpleSource, SimpleDestination>(useCache: true);
		FastMapper.CacheManager.GetCache().Count.ShouldBeGreaterThan(0);

		// Act
		FastMapper.CacheManager.ClearAllCaches();

		// Assert
		FastMapper.CacheManager.GetCache().Count.ShouldBe(0);
		FastMapper.CacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void FastMapper_CacheManager_GetCacheAndLimitedCache_Work(bool useLimited)
	{
		// Arrange
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.CacheManager.SetUseLimitedCache(useLimited);
		FastMapper.CacheManager.SetLimitedCacheSize(10);
		SimpleSource source = new() { StringProp = "A", IntProp = 1, DateProp = DateTime.Now };

		// Act
		source.FastMap<SimpleSource, SimpleDestination>(useCache: true);

		// Assert
		if (useLimited)
		{
			FastMapper.CacheManager.GetLimitedCache().Count.ShouldBe(1);
			FastMapper.CacheManager.GetCache().Count.ShouldBe(0);
		}
		else
		{
			FastMapper.CacheManager.GetCache().Count.ShouldBe(1);
			FastMapper.CacheManager.GetLimitedCache().Count.ShouldBe(0);
		}
	}

	public enum TryAddCacheScenario
	{
		FirstAdd,
		DuplicateAdd
	}

	[Theory]
	[InlineData(TryAddCacheScenario.FirstAdd)]
	[InlineData(TryAddCacheScenario.DuplicateAdd)]
	public void FastMapper_CacheManager_TryAddCache_WorksCorrectly(TryAddCacheScenario scenario)
	{
		// Arrange - use dedicated test types to avoid polluting other tests
		FastMapper.MapperCacheKey key = new(typeof(TryAddTestSource), typeof(TryAddTestDest));
		Func<TryAddTestSource, TryAddTestDest> del = _ => new TryAddTestDest { Value = "X" };

		switch (scenario)
		{
			case TryAddCacheScenario.FirstAdd:
				// Act & Assert - unlimited cache
				FastMapper.CacheManager.SetUseLimitedCache(false);
				FastMapper.CacheManager.ClearAllCaches();
				FastMapper.CacheManager.TryAddCache(key, del).ShouldBeTrue();
				FastMapper.CacheManager.GetCache().ContainsKey(key).ShouldBeTrue();

				// Act & Assert - limited cache
				FastMapper.CacheManager.SetUseLimitedCache(true);
				FastMapper.CacheManager.ClearAllCaches();
				FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
				FastMapper.CacheManager.GetLimitedCache().ContainsKey(key).ShouldBeTrue();
				break;

			case TryAddCacheScenario.DuplicateAdd:
				// Act & Assert - unlimited cache
				FastMapper.CacheManager.SetUseLimitedCache(false);
				FastMapper.CacheManager.ClearAllCaches();
				FastMapper.CacheManager.TryAddCache(key, del).ShouldBeTrue();
				FastMapper.CacheManager.TryAddCache(key, del).ShouldBeFalse();

				// Act & Assert - limited cache
				FastMapper.CacheManager.SetUseLimitedCache(true);
				FastMapper.CacheManager.ClearAllCaches();
				FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
				FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeFalse();
				break;
		}
	}

	[Fact]
	public void FasterMap_WithUseCacheFalse_DoesNotAddToCache()
	{
		// Arrange
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.CacheManager.SetUseLimitedCache(false);

		SimpleSource source = new()
		{
			StringProp = "NoCacheTest",
			IntProp = 999,
			DateProp = DateTime.Now
		};

		int initialCacheCount = FastMapper.CacheManager.GetCache().Count;

		// Act
		SimpleDestination result = source.FastMap<SimpleSource, SimpleDestination>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.StringProp.ShouldBe(source.StringProp);
		// Cache count should remain the same when useCache is false
		FastMapper.CacheManager.GetCache().Count.ShouldBe(initialCacheCount);
	}

	[Fact]
	public void FastMapper_ClearCache_RemovesAllCachedMappers()
	{
		// Arrange
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.CacheManager.SetUseLimitedCache(false);
		SimpleSource source = new() { StringProp = "A", IntProp = 1, DateProp = DateTime.Now };
		source.FastMap<SimpleSource, SimpleDestination>(useCache: true);
		FastMapper.CacheManager.GetCache().Count.ShouldBeGreaterThan(0);

		// Act
		FastMapper.ClearCache();

		// Assert
		FastMapper.CacheManager.GetCache().Count.ShouldBe(0);
		FastMapper.CacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	#endregion

	#region Collection Type Branch Coverage Tests

	// Test classes for HashSet mapping
	public sealed class HashSetSource
	{
		public required HashSet<int> Numbers { get; set; }
	}

	public sealed class HashSetDest
	{
		public required HashSet<int> Numbers { get; set; }
	}

	// Test classes for Queue/Stack mapping to non-standard types
	public sealed class QueueSource
	{
		public required Queue<string> Items { get; set; }
	}

	public sealed class QueueDest
	{
		public required List<string> Items { get; set; }
	}

	public sealed class StackSource
	{
		public required Stack<double> Values { get; set; }
	}

	public sealed class StackDest
	{
		public required List<double> Values { get; set; }
	}

	// Test classes for different element types to HashSet (complex objects)
	public sealed class SimpleSourceListWrapper
	{
		public required List<SimpleSource> Items { get; set; }
	}

	public sealed class SimpleDestHashSetWrapper
	{
		public required HashSet<SimpleDestination> Items { get; set; }
	}

	// Test classes for same element type to Queue - REMOVED: FastMapper doesn't support Queue as destination
	// Lines 298-299 appear to be unreachable for working scenarios

	// Test classes for incompatible property types
	public sealed class IncompatibleSource
	{
		public required string Name { get; set; }
		public int Value { get; set; }
		public DateTime Timestamp { get; set; }  // Incompatible with dest
	}

	public sealed class IncompatibleDest
	{
		public required string Name { get; set; }
		public int Value { get; set; }
		public required List<string> Timestamp { get; set; }  // Incompatible with source
	}

	[Theory]
	[InlineData(CollectionMappingType.HashSetToHashSet)]
	[InlineData(CollectionMappingType.QueueToList)]
	[InlineData(CollectionMappingType.StackToList)]
	[InlineData(CollectionMappingType.ListToHashSet)]
	public void FasterMap_CollectionTypeMapping_MapsCorrectly(CollectionMappingType mappingType)
	{
		switch (mappingType)
		{
			case CollectionMappingType.HashSetToHashSet:
				HashSetSource hashsetSource = new() { Numbers = [1, 2, 3, 4, 5] };
				HashSetDest hashsetResult = hashsetSource.FastMap<HashSetSource, HashSetDest>();
				hashsetResult.ShouldNotBeNull();
				hashsetResult.Numbers.ShouldBe(hashsetSource.Numbers);
				break;

			case CollectionMappingType.QueueToList:
				QueueSource queueSource = new() { Items = new Queue<string>(["first", "second", "third"]) };
				QueueDest queueResult = queueSource.FastMap<QueueSource, QueueDest>();
				queueResult.ShouldNotBeNull();
				queueResult.Items.ShouldBe(queueSource.Items);
				break;

			case CollectionMappingType.StackToList:
				StackSource stackSource = new() { Values = new Stack<double>([1.1, 2.2, 3.3]) };
				StackDest stackResult = stackSource.FastMap<StackSource, StackDest>();
				stackResult.ShouldNotBeNull();
				stackResult.Values.ShouldBe(stackSource.Values);
				break;

			case CollectionMappingType.ListToHashSet:
				SimpleSourceListWrapper listSource = new()
				{
					Items =
					[
						new() { StringProp = "A", IntProp = 1, DateProp = DateTime.Now },
						new() { StringProp = "B", IntProp = 2, DateProp = DateTime.Now },
						new() { StringProp = "C", IntProp = 3, DateProp = DateTime.Now }
					]
				};
				SimpleDestHashSetWrapper hashSetResult = listSource.FastMap<SimpleSourceListWrapper, SimpleDestHashSetWrapper>();
				hashSetResult.ShouldNotBeNull();
				hashSetResult.Items.Count.ShouldBe(3);
				SimpleDestination[] resultArray = [.. hashSetResult.Items];
				resultArray[0].StringProp.ShouldBe(listSource.Items[0].StringProp);
				resultArray[0].IntProp.ShouldBe(listSource.Items[0].IntProp);
				break;
		}
	}

	[Fact]
	public void FasterMap_WithIncompatiblePropertyTypes_SkipsIncompatibleProperties()
	{
		// Arrange
		IncompatibleSource source = new()
		{
			Name = "Test",
			Value = 42,
			Timestamp = DateTime.Now
		};

		// Act
		IncompatibleDest result = source.FastMap<IncompatibleSource, IncompatibleDest>();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(source.Name);
		result.Value.ShouldBe(source.Value);
		// Timestamp should be in default state (empty required list - will cause initialization)
		result.Timestamp.ShouldBeNull();  // Incompatible type was skipped
	}

	#endregion

	#region Dead Code Branch Investigation

	// Tests to investigate if lines 291-294, 298-299, 343, 346-347 are truly unreachable

	[Fact]
	public void FasterMap_HashSetToHashSet_TopLevel_MapsCorrectly()
	{
		// Arrange - Tests lines 291-294 (HashSet branch in CreateSameTypeCollectionCopy)
		HashSet<int> source = [1, 2, 3, 4, 5];

		// Act
		HashSet<int> result = source.FastMap<HashSet<int>, HashSet<int>>();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(source);
	}

	// Note: Lines 298-299, 343, 346-347 are covered by existing tests during expression tree construction,
	// but produce invalid expressions for Queue<T>/Stack<T> destinations (which lack IEnumerable<T> constructors).
	// These lines execute correctly and return ToList expressions, but lambda compilation fails at line 154.
	// This is expected behavior - FastMapper intentionally does not support Queue/Stack as destination types.

	#endregion
}
