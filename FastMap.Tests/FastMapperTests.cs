using System.Collections;
using System.Collections.ObjectModel;
using CommonNetFuncs.FastMap;

namespace FastMap.Tests;

public sealed class FastMapperTests : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                FastMapper.CacheManager.SetUseLimitedCache(true);
                FastMapper.CacheManager.SetLimitedCacheSize(100);
                FastMapper.CacheManager.ClearAllCaches();
            }
            disposed = true;
        }
    }

    ~FastMapperTests()
    {
        Dispose(false);
    }

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
        private readonly List<T> _items = [];

        public void Add(T item)
        {
            _items.Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
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

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
    [InlineData(new int[] { }, new int[] { })]
    [InlineData(new[] { 1 }, new[] { 1 })]
    public void FastMap_WithArrays_MapsCorrectly(int[] source, int[] expected)
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
    public void FastMap_WithSimpleProperties_MapsCorrectly(string stringProp, int intProp, string dateProp)
    {
        // Arrange
        SimpleSource source = new()
        {
            StringProp = stringProp,
            IntProp = intProp,
            DateProp = DateTime.Parse(dateProp)
        };

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
    public void FastMap_WithComplexProperties_MapsCorrectly(string name, string[] stringList, string dictKey, int dictValue,
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

        result.NestedObject?.StringProp.ShouldBe(source.NestedObject.StringProp);
        result.NestedObject?.IntProp.ShouldBe(source.NestedObject.IntProp);
        result.NestedObject?.DateProp.ShouldBe(source.NestedObject.DateProp);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { "a", "b" })]
    [InlineData(new int[] { }, new string[] { })]
    public void FastMap_WithReadOnlyCollections_MapsCorrectly(int[] numbers, string[] strings)
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
    public void FastMap_WithCustomCollections_MapsCorrectly(int[] sourceData)
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
    public void FastMap_BetweenDifferentCollectionTypes_MapsCorrectly(int[] sourceData)
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
    public void FastMap_WithMismatchedProperties_MapsMatchingPropertiesOnly(
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
    public void FastMap_WithNestedDictionary_MapsCorrectly(string key1Str, int key1Int, string? key2Str, int? key2Int)
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

    // Keep the Fact tests as they are since they test error conditions
    [Fact]
    public void FastMap_WhenSourceIsNull_ReturnsNull()
    {
        // Arrange
        SimpleSource? source = null;

        // Act
        SimpleDestination? result = source.FastMap<SimpleSource?, SimpleDestination>();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void FastMap_WithInvalidDictionaryMapping_ThrowsInvalidOperationException()
    {
        // Arrange
        Dictionary<int, string> source = new() { [1] = "test" };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => source.FastMap<Dictionary<int, string>, List<string>>())
            .Message.ShouldBe("Both source and destination must be a dictionary in order to be mapped");
    }

    [Fact]
    public void FastMap_WithMismatchedDictionaryKeyTypes_ThrowsInvalidOperationException()
    {
        // Arrange
        Dictionary<int, string> source = new() { [1] = "test" };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => source.FastMap<Dictionary<int, string>, Dictionary<string, string>>())
            .Message.ShouldBe("Source and destination dictionary key types must match.");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(1)]
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

    [Fact]
    public void FastMapper_CacheManager_TryAddCacheAndTryAddLimitedCache_Works()
    {
        // Arrange
        FastMapper.MapperCacheKey key = new(typeof(SimpleSource), typeof(SimpleDestination));
        Func<SimpleSource, SimpleDestination> del = (_ => new SimpleDestination { StringProp = "X", IntProp = 2, DateProp = DateTime.Now });

        // Act
        FastMapper.CacheManager.SetUseLimitedCache(false);
        FastMapper.CacheManager.ClearAllCaches();
        FastMapper.CacheManager.TryAddCache(key, del).ShouldBeTrue();
        FastMapper.CacheManager.GetCache().ContainsKey(key).ShouldBeTrue();

        FastMapper.CacheManager.SetUseLimitedCache(true);
        FastMapper.CacheManager.ClearAllCaches();
        FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
        FastMapper.CacheManager.GetLimitedCache().ContainsKey(key).ShouldBeTrue();
    }

    [Fact]
    public void FastMapper_CacheManager_DuplicateTryAddCache_ReturnsFalse()
    {
        // Arrange
        FastMapper.MapperCacheKey key = new(typeof(SimpleSource), typeof(SimpleDestination));
        Func<SimpleSource, SimpleDestination> del = _ => new SimpleDestination { StringProp = "X", IntProp = 2, DateProp = DateTime.Now };

        // Act
        FastMapper.CacheManager.SetUseLimitedCache(false);
        FastMapper.CacheManager.ClearAllCaches();
        FastMapper.CacheManager.TryAddCache(key, del).ShouldBeTrue();
        FastMapper.CacheManager.TryAddCache(key, del).ShouldBeFalse();

        FastMapper.CacheManager.SetUseLimitedCache(true);
        FastMapper.CacheManager.ClearAllCaches();
        FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
        FastMapper.CacheManager.TryAddLimitedCache(key, del).ShouldBeFalse();
    }
}
