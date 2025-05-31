using System.Collections;
using System.Collections.ObjectModel;
using CommonNetFuncs.FastMap;
using Shouldly;

namespace FastMap.Tests;

public class FastMapperTests
{
    public class SimpleSource
    {
        public required string StringProp { get; set; }

        public int IntProp { get; set; }

        public DateTime DateProp { get; set; }
    }

    public class SimpleDestination
    {
        public required string StringProp { get; set; }

        public int IntProp { get; set; }

        public DateTime DateProp { get; set; }
    }

    public class ComplexSource
    {
        public required string Name { get; set; }

        public required List<string> StringList { get; set; }

        public required Dictionary<string, int> Dictionary { get; set; }

        public required SimpleSource NestedObject { get; set; }

        public required HashSet<int> NumberSet { get; set; }

        public required Queue<string> StringQueue { get; set; }

        public required Stack<double> DoubleStack { get; set; }
    }

    public class ComplexDestination
    {
        public required string Name { get; set; }

        public required List<string> StringList { get; set; }

        public required Dictionary<string, int> Dictionary { get; set; }

        public required SimpleDestination NestedObject { get; set; }

        public required HashSet<int> NumberSet { get; set; }

        public required Queue<string> StringQueue { get; set; }

        public required Stack<double> DoubleStack { get; set; }
    }

    public class ReadOnlyCollectionSource
    {
        public required IReadOnlyCollection<int> ReadOnlyNumbers { get; set; }

        public required IReadOnlyList<string> ReadOnlyStrings { get; set; }

        public required ReadOnlyCollection<DateTime> ReadOnlyDates { get; set; }
    }

    public class ReadOnlyCollectionDest
    {
        public required List<int> ReadOnlyNumbers { get; set; }

        public required IReadOnlyList<string> ReadOnlyStrings { get; set; }

        public required ReadOnlyCollection<DateTime> ReadOnlyDates { get; set; }
    }

    public class CustomCollection<T> : IEnumerable<T>
    {
        private readonly List<T> _items = [];

        public void Add(T item) { _items.Add(item); }

        public IEnumerator<T> GetEnumerator() { return _items.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    public class CustomCollectionSource
    {
        public required CustomCollection<int> Numbers { get; set; }
    }

    public class CustomCollectionDest
    {
        public required List<int> Numbers { get; set; }
    }

    //    // Add these test methods
    //    [Fact]
    //    public void FastMap_WithArrays_HandlesEmptyAndPopulatedArrays()
    //    {
    //        // Test empty array
    //        int[] emptySource = [];
    //        int[] emptyResult = emptySource.FastMap<int[], int[]>();
    //        emptyResult.ShouldNotBeNull();
    //        emptyResult.Length.ShouldBe(0);

    //        // Test populated array
    //        int[] populatedSource = [1, 2, 3];
    //        int[] populatedResult = populatedSource.FastMap<int[], int[]>();
    //        populatedResult.ShouldNotBeNull();
    //        populatedResult.Length.ShouldBe(populatedSource.Length);
    //        populatedResult.ShouldBe(populatedSource);

    //        // Test array of objects
    //        SimpleSource[] objectSource = [_fixture.Create<SimpleSource>(), _fixture.Create<SimpleSource>()];
    //        SimpleDestination[] objectResult = objectSource.FastMap<SimpleSource[], SimpleDestination[]>();
    //        objectResult.ShouldNotBeNull();
    //        objectResult.Length.ShouldBe(objectSource.Length);
    //        for (int i = 0; i < objectSource.Length; i++)
    //        {
    //            objectResult[i].StringProp.ShouldBe(objectSource[i].StringProp);
    //            objectResult[i].IntProp.ShouldBe(objectSource[i].IntProp);
    //            objectResult[i].DateProp.ShouldBe(objectSource[i].DateProp);
    //        }
    //    }

    //    [Fact]
    //    public void FastMap_WithReadOnlyCollections_MapsCorrectly()
    //    {
    //        // Arrange
    //        ReadOnlyCollectionSource source = new()
    //        {
    //            ReadOnlyNumbers = new List<int> { 1, 2, 3 }.AsReadOnly(),
    //            ReadOnlyStrings = new List<string> { "a", "b", "c" }.AsReadOnly(),
    //            ReadOnlyDates = new ReadOnlyCollection<DateTime>([DateTime.Now, DateTime.Now.AddDays(1)])
    //        };

    //        // Act
    //        ReadOnlyCollectionDest result = source.FastMap<ReadOnlyCollectionSource, ReadOnlyCollectionDest>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.ReadOnlyNumbers.ShouldBe(source.ReadOnlyNumbers);
    //        result.ReadOnlyStrings.ShouldBe(source.ReadOnlyStrings);
    //        result.ReadOnlyDates.ShouldBe(source.ReadOnlyDates);
    //    }

    //    [Fact]
    //    public void FastMap_WithCustomCollections_MapsCorrectly()
    //    {
    //        // Arrange
    //        CustomCollectionSource source = new()
    //        {
    //            Numbers = new CustomCollection<int>()
    //        };
    //        source.Numbers.Add(1);
    //        source.Numbers.Add(2);
    //        source.Numbers.Add(3);

    //        // Act
    //        CustomCollectionDest result = source.FastMap<CustomCollectionSource, CustomCollectionDest>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.Numbers.ShouldBe(source.Numbers);
    //    }

    //    [Fact]
    //    public void FastMap_BetweenDifferentCollectionTypes_MapsCorrectly()
    //    {
    //        // Array to List
    //        int[] arraySource = [1, 2, 3];
    //        List<int> listResult = arraySource.FastMap<int[], List<int>>();
    //        listResult.ShouldBe(arraySource);

    //        // List to Array
    //        List<int> listSource = [1, 2, 3];
    //        int[] arrayResult = listSource.FastMap<List<int>, int[]>();
    //        arrayResult.ShouldBe(listSource);

    //        // HashSet to List
    //        HashSet<int> hashSetSource = [1, 2, 3];
    //        List<int> listFromHashSet = hashSetSource.FastMap<HashSet<int>, List<int>>();
    //        listFromHashSet.ShouldBe(hashSetSource);

    //        // Array to HashSet
    //        int[] arrayToHashSetSource = [1, 2, 3, 3]; // Note duplicate
    //        HashSet<int> hashSetResult = arrayToHashSetSource.FastMap<int[], HashSet<int>>();
    //        hashSetResult.Count.ShouldBe(3); // Should eliminate duplicate
    //        hashSetResult.ShouldBe([1, 2, 3]);
    //    }

    //    [Fact]
    //    public void FastMap_WhenSourceIsNull_ReturnsNull()
    //    {
    //        // Arrange
    //        SimpleSource? source = null;

    //        // Act
    //        SimpleDestination? result = source.FastMap<SimpleSource?, SimpleDestination>();

    //        // Assert
    //        result.ShouldBeNull();
    //    }

    //    [Fact]
    //    public void FastMap_WithSimpleProperties_MapsCorrectly()
    //    {
    //        // Arrange
    //        SimpleSource source = _fixture.Create<SimpleSource>();

    //        // Act
    //        SimpleDestination result = source.FastMap<SimpleSource, SimpleDestination>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.StringProp.ShouldBe(source.StringProp);
    //        result.IntProp.ShouldBe(source.IntProp);
    //        result.DateProp.ShouldBe(source.DateProp);
    //    }

    //    [Fact]
    //    public void FastMap_WithCollections_MapsCorrectly()
    //    {
    //        // Arrange
    //        ComplexSource source = new()
    //        {
    //            Name = "Test",
    //            StringList = ["one", "two", "three"],
    //            Dictionary = new() { ["key1"] = 1, ["key2"] = 2 },
    //            NestedObject = _fixture.Create<SimpleSource>(),
    //            NumberSet = new() { 1, 2, 3 },
    //            StringQueue = new(["first", "second"]),
    //            DoubleStack = new([1.0, 2.0])
    //        };

    //        // Act
    //        ComplexDestination result = source.FastMap<ComplexSource, ComplexDestination>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.Name.ShouldBe(source.Name);
    //        result.StringList.ShouldBe(source.StringList);
    //        result.Dictionary.ShouldBe(source.Dictionary);
    //        result.NumberSet.ShouldBe(source.NumberSet);
    //        result.StringQueue.ShouldBe(source.StringQueue);
    //        result.DoubleStack.ShouldBe(source.DoubleStack);

    //        // Verify nested object
    //        result.NestedObject.StringProp.ShouldBe(source.NestedObject.StringProp);
    //        result.NestedObject.IntProp.ShouldBe(source.NestedObject.IntProp);
    //        result.NestedObject.DateProp.ShouldBe(source.NestedObject.DateProp);
    //    }

    //    [Theory]
    //    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
    //    [InlineData(new int[] { }, new int[] { })]
    //    public void FastMap_WithArrays_MapsCorrectly(int[] source, int[] expected)
    //    {
    //        // Act
    //        int[] result = source.FastMap<int[], int[]>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.ShouldBe(expected);
    //    }

    public class SourceWithMismatchedProperties
    {
        public required string Name { get; set; }

        public int Age { get; set; }

        public required string ExtraProperty { get; set; }
    }

    public class DestWithMismatchedProperties
    {
        public required string Name { get; set; }

        public int Age { get; set; }

        public required string DifferentProperty { get; set; }
    }

    //    [Fact]
    //    public void FastMap_WithMismatchedProperties_MapsMatchingPropertiesOnly()
    //    {
    //        // Arrange
    //        SourceWithMismatchedProperties source = new()
    //        {
    //            Name = "Test",
    //            Age = 30,
    //            ExtraProperty = "Extra"
    //        };

    //        // Act
    //        DestWithMismatchedProperties result = source.FastMap<SourceWithMismatchedProperties, DestWithMismatchedProperties>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.Name.ShouldBe(source.Name);
    //        result.Age.ShouldBe(source.Age);
    //        result.DifferentProperty.ShouldBeNull();
    //    }

    public class DictionarySource
    {
        public required Dictionary<string, SimpleSource> ComplexDict { get; set; }
    }

    public class DictionaryDest
    {
        public required Dictionary<string, SimpleDestination> ComplexDict { get; set; }
    }

    //    [Fact]
    //    public void FastMap_WithNestedDictionary_MapsCorrectly()
    //    {
    //        // Arrange
    //        DictionarySource source = new()
    //        {
    //            ComplexDict = new()
    //            {
    //                ["key1"] = _fixture.Create<SimpleSource>(),
    //                ["key2"] = _fixture.Create<SimpleSource>()
    //            }
    //        };

    //        // Act
    //        DictionaryDest result = source.FastMap<DictionarySource, DictionaryDest>();

    //        // Assert
    //        result.ShouldNotBeNull();
    //        result.ComplexDict.Count.ShouldBe(source.ComplexDict.Count);

    //        foreach ((string key, SimpleSource value) in source.ComplexDict)
    //        {
    //            result.ComplexDict.ContainsKey(key).ShouldBeTrue();
    //            result.ComplexDict[key].StringProp.ShouldBe(value.StringProp);
    //            result.ComplexDict[key].IntProp.ShouldBe(value.IntProp);
    //            result.ComplexDict[key].DateProp.ShouldBe(value.DateProp);
    //        }
    //    }

    //    [Fact]
    //    public void FastMap_WithInvalidDictionaryMapping_ThrowsInvalidOperationException()
    //    {
    //        // Arrange
    //        Dictionary<int, string> source = new() { [1] = "test" };

    //        // Act & Assert
    //        Should.Throw<InvalidOperationException>(source.FastMap<Dictionary<int, string>, List<string>>)
    //            .Message.ShouldBe("Both source and destination must be a dictionary in order to be mapped");
    //    }

    //    [Fact]
    //    public void FastMap_WithMismatchedDictionaryKeyTypes_ThrowsInvalidOperationException()
    //    {
    //        // Arrange
    //        Dictionary<int, string> source = new() { [1] = "test" };

    //        // Act & Assert
    //        Should.Throw<InvalidOperationException>(source.FastMap<Dictionary<int, string>, Dictionary<string, string>>)
    //            .Message.ShouldBe("Source and destination dictionary key types must match.");
    //    }

    //    [Fact]
    //    public void FastMap_WithHashSet_HandlesSpecialCases()
    //    {
    //        // HashSet to HashSet
    //        HashSet<int> hashSetSource = [1, 2, 3, 3]; // Note duplicate
    //        HashSet<int> hashSetResult = hashSetSource.FastMap<HashSet<int>, HashSet<int>>();
    //        hashSetResult.Count.ShouldBe(3); // Should maintain uniqueness
    //        hashSetResult.ShouldBe([1, 2, 3]);

    //        // HashSet with objects
    //        HashSet<SimpleSource> objectHashSetSource = [new() { StringProp = "Test1", IntProp = 1 }, new() { StringProp = "Test2", IntProp = 2 }];
    //        HashSet<SimpleDestination> objectHashSetResult = objectHashSetSource.FastMap<HashSet<SimpleSource>, HashSet<SimpleDestination>>();
    //        objectHashSetResult.Count.ShouldBe(2);
    //        objectHashSetResult.Select(x => x.StringProp).ShouldBe(["Test1", "Test2"]);
    //        objectHashSetResult.Select(x => x.IntProp).ShouldBe([1, 2]);
    //    }

    //    [Fact]
    //    public void FastMap_CollectionToReadOnlyCollection_MapsCorrectly()
    //    {
    //        // List to ReadOnlyCollection
    //        List<int> listSource = [1, 2, 3];
    //        ReadOnlyCollection<int> readOnlyResult = listSource.FastMap<List<int>, ReadOnlyCollection<int>>();
    //        readOnlyResult.ShouldBe(listSource);

    //        // Array to IReadOnlyList
    //        int[] arraySource = [1, 2, 3];
    //        IReadOnlyList<int> iReadOnlyResult = arraySource.FastMap<int[], IReadOnlyList<int>>();
    //        iReadOnlyResult.ShouldBe(arraySource);
    //    }

    //    [Fact]
    //    public void FastMap_WithCustomEnumerable_MapsToStandardCollections()
    //    {
    //        // CustomCollection to List
    //        CustomCollection<int> customSource = new();
    //        customSource.Add(1);
    //        customSource.Add(2);
    //        customSource.Add(3);

    //        List<int> listResult = customSource.FastMap<CustomCollection<int>, List<int>>();
    //        listResult.ShouldBe([1, 2, 3]);

    //        // CustomCollection to Array
    //        int[] arrayResult = customSource.FastMap<CustomCollection<int>, int[]>();
    //        arrayResult.ShouldBe([1, 2, 3]);

    //        // CustomCollection to HashSet
    //        HashSet<int> hashSetResult = customSource.FastMap<CustomCollection<int>, HashSet<int>>();
    //        hashSetResult.ShouldBe([1, 2, 3]);
    //    }

    //    [Fact]
    //    public void FastMap_WithNestedCollections_MapsCorrectly()
    //    {
    //        // List of HashSets
    //        List<HashSet<int>> nestedSource = [[1, 2], [3, 4]];
    //        List<HashSet<int>> nestedResult = nestedSource.FastMap<List<HashSet<int>>, List<HashSet<int>>>();
    //        nestedResult.Count.ShouldBe(2);
    //        nestedResult[0].ShouldBe([1, 2]);
    //        nestedResult[1].ShouldBe([3, 4]);

    //        // Dictionary with HashSet values
    //        Dictionary<string, HashSet<int>> dictSource = new()
    //        {
    //            ["key1"] = [1, 2],
    //            ["key2"] = [3, 4]
    //        };
    //        Dictionary<string, HashSet<int>> dictResult = dictSource.FastMap<Dictionary<string, HashSet<int>>, Dictionary<string, HashSet<int>>>();
    //        dictResult["key1"].ShouldBe([1, 2]);
    //        dictResult["key2"].ShouldBe([3, 4]);
    //    }

    //    [Fact]
    //    public void FastMap_WithReadOnlyCollections_HandlesAllVariants()
    //    {
    //        // Test ReadOnlyCollection direct mapping
    //        ReadOnlyCollection<int> source = new([1, 2, 3]);
    //        ReadOnlyCollection<int> result = source.FastMap<ReadOnlyCollection<int>, ReadOnlyCollection<int>>();
    //        result.ShouldBe(source);

    //        // Test IReadOnlyList to ReadOnlyCollection
    //        IReadOnlyList<int> sourceList = new List<int> { 1, 2, 3 }.AsReadOnly();
    //        ReadOnlyCollection<int> resultFromInterface = sourceList.FastMap<IReadOnlyList<int>, ReadOnlyCollection<int>>();
    //        resultFromInterface.ShouldBe(sourceList);

    //        // Test List to IReadOnlyList
    //        List<int> sourceRegularList = [1, 2, 3];
    //        IReadOnlyList<int> resultInterface = sourceRegularList.FastMap<List<int>, IReadOnlyList<int>>();
    //        resultInterface.ShouldBe(sourceRegularList);

    //        // Test with complex types
    //        ReadOnlyCollection<SimpleSource> complexSource = new([
    //            new() { StringProp = "Test1", IntProp = 1 },
    //        new() { StringProp = "Test2", IntProp = 2 }
    //        ]);
    //        ReadOnlyCollection<SimpleDestination> complexResult = complexSource.FastMap<ReadOnlyCollection<SimpleSource>, ReadOnlyCollection<SimpleDestination>>();
    //        complexResult.Count.ShouldBe(2);
    //        complexResult[0].StringProp.ShouldBe("Test1");
    //        complexResult[0].IntProp.ShouldBe(1);
    //        complexResult[1].StringProp.ShouldBe("Test2");
    //        complexResult[1].IntProp.ShouldBe(2);
    //    }

    //    [Fact]
    //    public void FastMap_WithNestedReadOnlyCollections_MapsCorrectly()
    //    {
    //        // Create a complex object with nested ReadOnlyCollections
    //        var source = new
    //        {
    //            Numbers = new ReadOnlyCollection<int>([1, 2, 3]),
    //            Nested = new ReadOnlyCollection<ReadOnlyCollection<int>>([
    //                new ReadOnlyCollection<int>([1, 2]),
    //                new ReadOnlyCollection<int>([3, 4])
    //            ])
    //        };

    //        dynamic result = source.FastMap<dynamic, dynamic>();

    //        result.Numbers.ShouldBeOfType<ReadOnlyCollection<int>>();
    //        result.Numbers.ShouldBe(new ReadOnlyCollection<int>[1, 2, 3]);

    //        result.Nested.ShouldBeOfType<ReadOnlyCollection<ReadOnlyCollection<int>>>();
    //        result.Nested[0].ShouldBe(new ReadOnlyCollection<int>[1, 2]);
    //        result.Nested[1].ShouldBe(new ReadOnlyCollection<int>[3, 4]);
    //    }

    //    [Fact]
    //    public void FastMap_BetweenDifferentReadOnlyTypes_MapsCorrectly()
    //    {
    //        // Array to ReadOnlyCollection
    //        int[] arraySource = [1, 2, 3];
    //        ReadOnlyCollection<int> readOnlyResult = arraySource.FastMap<int[], ReadOnlyCollection<int>>();
    //        readOnlyResult.ShouldBe(arraySource);

    //        // List to IReadOnlyList
    //        List<int> listSource = [1, 2, 3];
    //        IReadOnlyList<int> iReadOnlyResult = listSource.FastMap<List<int>, IReadOnlyList<int>>();
    //        iReadOnlyResult.ShouldBe(listSource);

    //        // HashSet to ReadOnlyCollection
    //        HashSet<int> hashSetSource = [1, 2, 3];
    //        ReadOnlyCollection<int> readOnlyFromHashSet = hashSetSource.FastMap<HashSet<int>, ReadOnlyCollection<int>>();
    //        readOnlyFromHashSet.ShouldBe(hashSetSource);
    //    }
    //}

    // Keep all the existing test classes (SimpleSource through CustomCollectionDest) as they are

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
        Should.Throw<InvalidOperationException>(source.FastMap<Dictionary<int, string>, List<string>>)
            .Message.ShouldBe("Both source and destination must be a dictionary in order to be mapped");
    }

    [Fact]
    public void FastMap_WithMismatchedDictionaryKeyTypes_ThrowsInvalidOperationException()
    {
        // Arrange
        Dictionary<int, string> source = new() { [1] = "test" };

        // Act & Assert
        Should.Throw<InvalidOperationException>(source.FastMap<Dictionary<int, string>, Dictionary<string, string>>)
            .Message.ShouldBe("Source and destination dictionary key types must match.");
    }

    #region Test Data Providers

    public static IEnumerable<object[]> GetSimpleSourceTestData()
    {
        yield return new object[]
        {
            new SimpleSource
            {
                StringProp = "Test1",
                IntProp = 1,
                DateProp = new DateTime(2025, 1, 1)
            }
        };
        yield return new object[]
        {
            new SimpleSource
            {
                StringProp = string.Empty,
                IntProp = 0,
                DateProp = DateTime.MinValue
            }
        };
    }

    public static IEnumerable<object[]> GetComplexSourceTestData()
    {
        yield return new object[]
        {
            new ComplexSource
            {
                Name = "Test1",
                StringList = ["one", "two"],
                Dictionary = new() { ["key1"] = 1 },
                NestedObject = new() { StringProp = "Nested", IntProp = 1, DateProp = DateTime.Now },
                NumberSet = [1, 2],
                StringQueue = new(["first"]),
                DoubleStack = new([1.0])
            } };
        yield return new object[]
        {
            new ComplexSource
            {
                Name = string.Empty,
                StringList = [],
                Dictionary = new(),
                NestedObject = new() { StringProp = string.Empty, IntProp = 0, DateProp = DateTime.MinValue },
                NumberSet = [],
                StringQueue = new(),
                DoubleStack = new()
            } };
    }

    public static IEnumerable<object[]> GetReadOnlyCollectionTestData()
    {
        DateTime[] dates = new[] { DateTime.Now, DateTime.Now.AddDays(1) };
        yield return new object[]
        {
            new List<int> { 1, 2, 3 }.AsReadOnly(),
            new List<string> { "a", "b" }.AsReadOnly(),
            new ReadOnlyCollection<DateTime>(dates)
        };
        yield return new object[]
        {
            new List<int>().AsReadOnly(),
            new List<string>().AsReadOnly(),
            new ReadOnlyCollection<DateTime>([])
        };
    }

    public static IEnumerable<object[]> GetCustomCollectionTestData()
    {
        yield return new object[] { new[] { 1, 2, 3 } };
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new[] { 1 } };
    }

    public static IEnumerable<object[]> GetCollectionConversionTestData()
    {
        int[] testData = [1, 2, 3];
        yield return new object[] { testData, typeof(List<int>), typeof(int[]) };
        yield return new object[] { testData, typeof(HashSet<int>), typeof(List<int>) };
        yield return new object[] { testData, typeof(int[]), typeof(HashSet<int>) };
    }

    public static IEnumerable<object?[]> GetMismatchedPropertiesTestData()
    {
        yield return new object[] { "Test", 30, "Extra" };
        yield return new object[] { string.Empty, 0, string.Empty };
        yield return new object?[] { "Name", 42, null };
    }

    public static IEnumerable<object[]> GetNestedDictionaryTestData()
    {
        yield return new object[]
        {
            new Dictionary<string, SimpleSource>
            {
                ["key1"] = new() { StringProp = "Test1", IntProp = 1, DateProp = DateTime.Now },
                ["key2"] = new() { StringProp = "Test2", IntProp = 2, DateProp = DateTime.Now.AddDays(1) }
            }
        };
        yield return new object[]
        {
            new Dictionary<string, SimpleSource>()
        };
    }

    #endregion
}
