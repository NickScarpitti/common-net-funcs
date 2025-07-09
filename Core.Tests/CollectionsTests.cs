using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using CommonNetFuncs.Core;
using FastExpressionCompiler;

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

public enum TestEnum
{
    Monday = DayOfWeek.Monday,
    Tuesday = DayOfWeek.Tuesday,
    Wednesday = DayOfWeek.Wednesday,
    Thursday = DayOfWeek.Thursday,
    Friday = DayOfWeek.Friday
}

public sealed class CollectionsTests
{
    private readonly Fixture _fixture;

    public CollectionsTests()
    {
        _fixture = new Fixture();
    }

    #region AnyFast Tests

    [Fact]
    public void AnyFast_WithNullICollection_ReturnsFalse()
    {
        // Act
        bool result = ((ICollection<string>?)null).AnyFast();

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
            collection.Add(_fixture.Create<string>());
        }

        // Act
        bool result = collection.AnyFast();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void AnyFast_WithNullIList_ReturnsFalse()
    {
        // Act
        bool result = ((IList<string>?)null).AnyFast();

        // Assert
        result.ShouldBeFalse();
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
            list.Add(_fixture.Create<string>());
        }

        // Act
        bool result = list.AnyFast();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void AnyFast_WithNullConcurrentBag_ReturnsFalse()
    {
        // Act
        bool result = ((ConcurrentBag<string>?)null).AnyFast();

        // Assert
        result.ShouldBeFalse();
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
            bag.Add(_fixture.Create<string>());
        }

        // Act
        bool result = bag.AnyFast();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void AnyFast_WithNullArray_ReturnsFalse()
    {
        // Act
        bool result = ((string[]?)null).AnyFast();

        // Assert
        result.ShouldBeFalse();
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
            array[i] = _fixture.Create<string>();
        }

        // Act
        bool result = array.AnyFast();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void AnyFast_WithNullDictionary_ReturnsFalse()
    {
        // Act
        bool result = ((IDictionary<string, string>?)null).AnyFast();

        // Assert
        result.ShouldBeFalse();
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
            dict.Add(_fixture.Create<string>(), _fixture.Create<string>());
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
            dict.TryAdd(_fixture.Create<string>(), _fixture.Create<string>());
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
        dict.AddDictionaryItems(pairs);

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
        dict.AddDictionaryItems(pairs);

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
        bag.AddRange(items);

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
        bag.AddRangeParallel(items);

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
        bag.AddRangeParallel(items, options);

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
        hashSet.AddRange(items);

        // Assert
        hashSet.Count.ShouldBe(2);
        hashSet.ShouldContain("test1");
        hashSet.ShouldContain("test2");
    }

    #endregion

    #region SetValue Tests

    [Fact]
    public void SetValue_AppliesActionToAllItems()
    {
        // Arrange
        List<TestClass> items = new() { new TestClass { Name = "test1" }, new TestClass { Name = "test2" } };

        // Act
        IEnumerable<TestClass> result = items.SetValue(item => item.Name = item.Name?.ToUpper());

        // Assert
        result.ShouldBeSameAs(items);
        items[0].Name.ShouldBe("TEST1");
        items[1].Name.ShouldBe("TEST2");
    }

    [Fact]
    public void SetValue_ForStrings_AppliesFunctionToAllItems()
    {
        // Arrange
        List<string> items = new() { "test1", "test2" };

        // Act
        List<string?> result = items.SetValue(s => s?.ToUpper());

        // Assert
        result.Count.ShouldBe(2);
        result[0].ShouldBe("TEST1");
        result[1].ShouldBe("TEST2");
    }

    [Fact]
    public void SetValueParallel_AppliesActionToAllItems()
    {
        // Arrange
        List<TestClass> items = new() { new TestClass { Name = "test1" }, new TestClass { Name = "test2" } };

        // Act
        IEnumerable<TestClass> result = items.SetValueParallel(item => item.Name = item.Name?.ToUpper());

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(item => item.Name == "TEST1");
        result.ShouldContain(item => item.Name == "TEST2");
    }

    [Fact]
    public void SetValueParallel_WithCustomMaxDegreeOfParallelism()
    {
        // Arrange
        List<TestClass> items = new() { new TestClass { Name = "test1" }, new TestClass { Name = "test2" } };

        // Act
        IEnumerable<TestClass> result = items.SetValueParallel(item => item.Name = item.Name?.ToUpper(), 2);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(item => item.Name == "TEST1");
        result.ShouldContain(item => item.Name == "TEST2");
    }

    #endregion

    #region SetValue for Array Tests

    [Fact]
    public void SetValue_ForArray_AppliesActionToAllElements()
    {
        // Arrange
        int[,] array = new int[,] { { 1, 2 }, { 3, 4 } };

        // Act
        array.SetValue((arr, indices) => arr.SetValue(((int)arr.GetValue(indices)!) * 2, indices));

        // Assert
        array[0, 0].ShouldBe(2);
        array[0, 1].ShouldBe(4);
        array[1, 0].ShouldBe(6);
        array[1, 1].ShouldBe(8);
    }

    [Fact]
    public void SetValue_ForEmptyArray_DoesNothing()
    {
        // Arrange
        int[] array = Array.Empty<int>();

        // Act & Assert (should not throw)
        Should.NotThrow(() => array.SetValue((arr, indices) => arr = indices));
    }

    #endregion

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

    [Fact]
    public void SelectNonEmpty_WithNullCollection_ReturnsNull()
    {
        // Act
        IEnumerable<string>? result = Collections.SelectNonEmpty(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void SelectNonNull_ReturnsNonNullObjects()
    {
        // Arrange
        List<TestClass?> items = new() { new TestClass { Name = "test1" }, null, new TestClass { Name = "test2" } };

        // Act
        IEnumerable<TestClass> result = items.SelectNonNull();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.ShouldContain(item => item.Name == "test1");
        result.ShouldContain(item => item.Name == "test2");
    }

    [Fact]
    public void SelectNonNull_WithNullCollection_ReturnsNull()
    {
        // Act
        IEnumerable<TestClass>? result = Collections.SelectNonNull<TestClass>(null);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SingleToList Tests

    [Fact]
    public void SingleToList_WithNonNullObject_ReturnsListWithObject()
    {
        // Arrange
        TestClass obj = new() { Name = "test" };

        // Act
        List<TestClass> result = obj.SingleToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(obj);
    }

    [Fact]
    public void SingleToList_WithNullObject_ReturnsEmptyList()
    {
        // Act
        List<TestClass> result = Collections.SingleToList<TestClass>(null);

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void SingleToList_WithNonEmptyString_ReturnsListWithString()
    {
        // Arrange
        const string str = "test";

        // Act
        List<string> result = str.SingleToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(str);
    }

    [Fact]
    public void SingleToList_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        List<string> result = string.Empty.SingleToList(allowEmptyStrings: false);

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void SingleToList_WithEmptyStringAndAllowEmptyTrue_ReturnsListWithEmptyString()
    {
        // Act
        List<string> result = string.Empty.SingleToList(allowEmptyStrings: true);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(string.Empty);
    }

    [Fact]
    public void SingleToList_WithNullString_ReturnsEmptyList()
    {
        // Act
        List<string> result = Collections.SingleToList(null, allowEmptyStrings: true);

        // Assert
        result.Count.ShouldBe(0);
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
        TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues);

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
        TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues);

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
        TestClass? result = list.AsQueryable().GetObjectByPartial(partial, ignoreDefaultValues);

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
        List<TestClass> result = dataTable.ToList<TestClass>();

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
        List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true);

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
        List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true);

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
        result[1]!.Date.ShouldBe(new DateTime(DateOnly.MaxValue, TimeOnly.MinValue));
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
            List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true);

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
            result[1]!.Date.ShouldBe(new DateTime(DateOnly.MaxValue, TimeOnly.MinValue));
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
            List<TestClass> result = dataTable.ToList<TestClass>(convertShortToBool: true);

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
        List<TestClass> result = dataTable.ToListParallel<TestClass>();

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
        List<TestClass> result = dataTable.ToEnumerableParallel<TestClass>().ToList();

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
        List<TestClass> result = dataTable.ToEnumerableStreaming<TestClass>().ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(item => (item!.Id == 1) && (item.Name == "test1"));
        result.ShouldContain(item => (item!.Id == 2) && (item.Name == "test2"));
    }

    #endregion

    #region ToDataTable Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDataTable_ConvertsCollectionToDataTable(bool useExpressionTrees)
    {
        // Arrange
        List<TestClass> collection = new()
        {
            new TestClass { Id = 1, Name = "test1", IsActive = true },
            new TestClass { Id = 2, Name = "test2", IsActive = false }
        };

        using DataTable result = new();

        // Act
        if (useExpressionTrees)
        {
            collection.ToDataTable(result);
        }
        else
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            collection.ToDataTableReflection(result);
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        // Assert
        result.ShouldNotBeNull();
        result.Columns.Count.ShouldBe(7); // Id, Name, IsActive
        result.Rows.Count.ShouldBe(2);

        result.Rows[0]["Id"].ShouldBe(1);
        result.Rows[0]["Name"].ShouldBe("test1");
        result.Rows[0]["IsActive"].ShouldBe(true);

        result.Rows[1]["Id"].ShouldBe(2);
        result.Rows[1]["Name"].ShouldBe("test2");
        result.Rows[1]["IsActive"].ShouldBe(false);

        result.Dispose();
    }

    [Theory]
    [InlineData(nameof(TestClass.Id), nameof(TestClass.Name), false)]
    [InlineData(nameof(TestClass.Id), nameof(TestClass.Name), true)]
    [InlineData($"{nameof(TestClass.Id)}NotInTestClass1", $"{nameof(TestClass.Name)}NotInTestClass2", false)]
    [InlineData($"{nameof(TestClass.Id)}NotInTestClass1", $"{nameof(TestClass.Name)}NotInTestClass2", true)]
    [InlineData(null, null, false)]
    [InlineData(null, null, true)]
    public void ToDataTable_WithExistingDataTable_AddsRowsToIt(string? column1Name, string? column2Name, bool useExpressionTrees)
    {
        // Arrange
        List<TestClass> collection = new()
        {
            new TestClass { Id = 1, Name = "test1" },
            new TestClass { Id = 2, Name = "test2" }
        };

        using DataTable dataTable = new();

        if (column1Name != null)
        {
            dataTable.Columns.Add(column1Name, typeof(int));
        }

        if (column2Name != null)
        {
            dataTable.Columns.Add(column2Name, typeof(string));
        }

        // Act
        DataTable result = new();

        // Act
        if (useExpressionTrees)
        {
            result = collection.ToDataTable(dataTable);
        }
        else
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            result = collection.ToDataTableReflection(dataTable);
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        // Assert
        result.ShouldBeSameAs(dataTable);
        result.Rows.Count.ShouldBe(2);
        result.Rows[0]["Id"].ShouldBe(1);
        result.Rows[0]["Name"].ShouldBe("test1");
        result.Rows[1]["Id"].ShouldBe(2);
        result.Rows[1]["Name"].ShouldBe("test2");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDataTable_WithParallel_ConvertsCollectionToDataTable(bool useExpressionTrees)
    {
        // Arrange
        List<TestClass> collection = new()
        {
            new TestClass { Id = 1, Name = "test1" },
            new TestClass { Id = 2, Name = "test2" }
        };

        // Act
        using DataTable result = new();
        if (useExpressionTrees)
        {
            collection.ToDataTable(result, useParallel: true);
        }
        else
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            collection.ToDataTableReflection(result, useParallel: true);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
        // Assert
        result.ShouldNotBeNull();
        result.Rows.Count.ShouldBe(2);

        // Since parallel processing doesn't guarantee order, we need to check both rows exist
        bool foundRow1 = false;
        bool foundRow2 = false;

        foreach (DataRow row in result.Rows)
        {
            if (((int)row["Id"] == 1) && ((string)row["Name"] == "test1"))
            {
                foundRow1 = true;
            }
            else if (((int)row["Id"] == 2) && ((string)row["Name"] == "test2"))
            {
                foundRow2 = true;
            }
        }

        foundRow1.ShouldBeTrue();
        foundRow2.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDataTable_WithNullCollection_ReturnsNull(bool useExpressionTrees)
    {
        // Act
        DataTable? result = new();
        if (useExpressionTrees)
        {
            result = Collections.ToDataTable<TestClass>(null, result);
        }
        else
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            result = Collections.ToDataTableReflection<TestClass>(null, result);
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        // Assert
        result.ShouldBeNull();
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

    public sealed class ArrayTraverseTests
    {
        [Fact]
        public void Constructor_ShouldInitializePositionAndMaxLengths_For1DArray()
        {
            // Arrange
            int[] array = new int[5];

            // Act
            ArrayTraverse traverse = new(array);

            // Assert
            traverse.Position.ShouldNotBeNull();
            traverse.Position.Length.ShouldBe(1);
            traverse.Position[0].ShouldBe(0);
        }

        [Fact]
        public void Constructor_ShouldInitializePositionAndMaxLengths_For2DArray()
        {
            // Arrange
            int[,] array = new int[3, 4];

            // Act
            ArrayTraverse traverse = new(array);

            // Assert
            traverse.Position.ShouldNotBeNull();
            traverse.Position.Length.ShouldBe(2);
            traverse.Position[0].ShouldBe(0);
            traverse.Position[1].ShouldBe(0);
        }

        [Fact]
        public void Step_ShouldIterateAllPositions_For1DArray()
        {
            // Arrange
            const int length = 4;
            int[] array = new int[length];
            ArrayTraverse traverse = new(array);
            int[][] expectedPositions = new int[length][];
            for (int i = 0; i < length; i++)
            {
                expectedPositions[i] = new[] { i };
            }

            // Act & Assert
            int stepCount = 0;
            do
            {
                traverse.Position[0].ShouldBe(expectedPositions[stepCount][0]);
                stepCount++;
            } while (traverse.Step());

            // After last step, should have iterated all positions
            stepCount.ShouldBe(length);
        }

        [Fact]
        public void Step_ShouldIterateAllPositions_For2DArray()
        {
            // Arrange
            const int dim0 = 2, dim1 = 3;
            int[,] array = new int[dim0, dim1];
            ArrayTraverse traverse = new(array);
            const int total = dim0 * dim1;
            int count = 0;
            int[,] visited = new int[dim0, dim1];

            // Act
            do
            {
                int i = traverse.Position[0];
                int j = traverse.Position[1];
                visited[i, j]++;
                count++;
            } while (traverse.Step());

            // Assert
            count.ShouldBe(total);
            for (int i = 0; i < dim0; i++)
            {
                for (int j = 0; j < dim1; j++)
                {
                    visited[i, j].ShouldBe(1);
                }
            }
        }

        [Fact]
        public void Step_ShouldIterateAllPositions_For3DArray()
        {
            // Arrange
            const int d0 = 2, d1 = 2, d2 = 2;
            int[,,] array = new int[d0, d1, d2];
            ArrayTraverse traverse = new(array);
            const int total = d0 * d1 * d2;
            int count = 0;
            int[,,] visited = new int[d0, d1, d2];

            // Act
            do
            {
                int i = traverse.Position[0];
                int j = traverse.Position[1];
                int k = traverse.Position[2];
                visited[i, j, k]++;
                count++;
            } while (traverse.Step());

            // Assert
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
        }

        [Fact]
        public void Step_ShouldReturnFalse_WhenArrayIsEmpty()
        {
            // Arrange
            int[] array = Array.Empty<int>();
            ArrayTraverse traverse = new(array);

            // Act
            bool result = traverse.Step();

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void Step_ShouldReturnFalse_WhenAtEndOfArray()
        {
            // Arrange
            int[] array = new int[2];
            ArrayTraverse traverse = new(array);

            // Act
            traverse.Step(); // Move to index 1
            bool result = traverse.Step(); // Should be at end

            // Assert
            result.ShouldBeFalse();
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
        List<TestClass> result = collection.StringAggProps(["Name", "Description"], distinct: distinct).ToList();

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
        Should.Throw<ArgumentException>(() => collection.StringAggProps(Array.Empty<string>()).ToList());
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
        bool result = ((int)testEnum).IsIn<TestEnum>();

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
        bool result = testEnum.ToString().IsIn<TestEnum>();

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region GetCombinations Tests

    [Fact]
    public void GetCombinations_GeneratesAllPossibleCombinations()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };

        // Act
        HashSet<string> result = sources.GetCombinations();

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain("A|1");
        result.ShouldContain("A|2");
        result.ShouldContain("B|1");
        result.ShouldContain("B|2");
    }

    [Fact]
    public void GetEnumeratedCombinations_GeneratesAllPossibleCombinations()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };

        // Act
        IEnumerable<string> result = sources.GetEnumeratedCombinations();

        // Assert
        result.Count().ShouldBe(4);
        result.ShouldContain("A|1");
        result.ShouldContain("A|2");
        result.ShouldContain("B|1");
        result.ShouldContain("B|2");
    }

    [Fact]
    public void GetCombinations_WithCustomSeparator_UsesCorrectSeparator()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };

        // Act
        HashSet<string> result = sources.GetCombinations(separator: "-");

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain("A-1");
        result.ShouldContain("A-2");
        result.ShouldContain("B-1");
        result.ShouldContain("B-2");
    }

    [Fact]
    public void GetEnumeratedCombinations_WithCustomSeparator_UsesCorrectSeparator()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string> { "1", "2" } };

        // Act
        IEnumerable<string> result = sources.GetEnumeratedCombinations(separator: "-");

        // Assert
        result.Count().ShouldBe(4);
        result.ShouldContain("A-1");
        result.ShouldContain("A-2");
        result.ShouldContain("B-1");
        result.ShouldContain("B-2");
    }

    [Fact]
    public void GetCombinations_WithNullReplacement_HandlesNullValues()
    {
        // Arrange
        List<List<string?>> sources = new() { new List<string?> { "A", null }, new List<string?> { "1", "2" } };

        // Act
        HashSet<string> result = sources.GetCombinations(nullReplacement: "NULL");

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldContain("A|1");
        result.ShouldContain("A|2");
        result.ShouldContain("NULL|1");
        result.ShouldContain("NULL|2");
    }

    [Fact]
    public void GetEnumeratedCombinations_WithNullReplacement_HandlesNullValues()
    {
        // Arrange
        List<List<string?>> sources = new() { new List<string?> { "A", null }, new List<string?> { "1", "2" } };

        // Act
        IEnumerable<string> result = sources.GetEnumeratedCombinations(nullReplacement: "NULL");

        // Assert
        result.Count().ShouldBe(4);
        result.ShouldContain("A|1");
        result.ShouldContain("A|2");
        result.ShouldContain("NULL|1");
        result.ShouldContain("NULL|2");
    }

    [Fact]
    public void GetCombinations_WithMaxCombinations_LimitsResults()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };

        // Act & Assert
        Should.Throw<ArgumentException>(() => sources.GetCombinations(maxCombinations: 5));
    }

    [Fact]
    public void GetEnumeratedCombinations_WithMaxCombinations_LimitsResults()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B", "C" }, new List<string> { "1", "2", "3" } };

        // Act & Assert
        Should.Throw<ArgumentException>(() => sources.GetEnumeratedCombinations(maxCombinations: 5).ToList());
    }

    [Fact]
    public void GetCombinations_WithEmptySource_ReturnsEmptySet()
    {
        // Arrange
        List<List<string>> sources = new();

        // Act
        HashSet<string> result = sources.GetCombinations();

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void GetEnumeratedCombinations_WithEmptySource_ReturnsEmptySet()
    {
        // Arrange
        List<List<string>> sources = new();

        // Act
        IEnumerable<string> result = sources.GetEnumeratedCombinations();

        // Assert
        result.Count().ShouldBe(0);
    }

    [Fact]
    public void GetCombinations_WithEmptyInnerList_HandlesCorrectly()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string>() };

        // Act
        HashSet<string> result = sources.GetCombinations(nullReplacement: "EMPTY");

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("A|EMPTY");
        result.ShouldContain("B|EMPTY");
    }

    [Fact]
    public void GetEnumeratedCombinations_WithEmptyInnerList_HandlesCorrectly()
    {
        // Arrange
        List<List<string>> sources = new() { new List<string> { "A", "B" }, new List<string>() };

        // Act
        IEnumerable<string> result = sources.GetEnumeratedCombinations(nullReplacement: "EMPTY");

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain("A|EMPTY");
        result.ShouldContain("B|EMPTY");
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
        Expression<Func<TestClass, bool>>? combinedExpr = Collections.CombineExpressions<TestClass>(expressions);
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
        Expression<Func<TestClass, bool>>? result = Collections.CombineExpressions<TestClass>(expressions);

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
}
