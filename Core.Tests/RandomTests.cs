<<<<<<< HEAD
﻿using static CommonNetFuncs.Core.MathHelpers;
using static CommonNetFuncs.Core.Random;
using static CommonNetFuncs.DeepClone.ExpressionTrees;

namespace Core.Tests;

public sealed class RandomTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 5)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void GetRandomInt_WithRange_ReturnsNumberInRange(int minValue, int maxValue)
    {
        // Act
        int result = GetRandomInt(minValue, maxValue);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(minValue);
        result.ShouldBeLessThan(maxValue);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetRandomInt_WithMaxValue_ReturnsNumberInRange(int maxValue)
    {
        // Act
        int result = GetRandomInt(maxValue);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(maxValue);
    }

    [Fact]
    public void GetRandomInt_WithoutParameters_ReturnsPositiveNumber()
    {
        int result = GetRandomInt();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(int.MaxValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetRandomInt_WithInvalidMaxValue_ThrowsException(int maxValue)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GetRandomInt(maxValue: maxValue));
    }

    [Theory]
    [InlineData(5, 0, 100)]
    [InlineData(10, -50, 50)]
    public void GetRandomInts_GeneratesCorrectNumberOfValuesInRange(int count, int min, int max)
    {
        // Act
        IEnumerable<int> results = GetRandomInts(count, min, max);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= min && x < max).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    public void GetRandomDouble_WithPrecision_ReturnsCorrectPrecision(int precision)
    {
        // Act
        double result = GetRandomDouble(precision);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThanOrEqualTo(1);
        result.GetPrecision().ShouldBeLessThanOrEqualTo(precision);
    }

    [Fact]
    public void GetRandomDouble_DefaultPrecision_ReturnsCorrectRange()
    {
        double result = GetRandomDouble();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(1);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    public void GetRandomDoubles_GeneratesCorrectNumberAndPrecision(int count, int precision)
    {
        // Act
        IEnumerable<double> results = GetRandomDoubles(count, precision);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= 0 && x < 1).ShouldBeTrue();
        results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(28)]
    public void GetRandomDecimal_WithPrecision_ReturnsCorrectPrecision(int precision)
    {
        // Act
        decimal result = GetRandomDecimal(precision);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThanOrEqualTo(1);
        result.GetPrecision().ShouldBeLessThanOrEqualTo(precision);
    }

    [Fact]
    public void GetRandomDecimal_DefaultPrecision_ReturnsCorrectRange()
    {
        decimal result = GetRandomDecimal();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(1);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    public void GetRandomDecimals_GeneratesCorrectNumberAndPrecision(int count, int precision)
    {
        // Act
        IEnumerable<decimal> results = GetRandomDecimals(count, precision);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= 0 && x < 1).ShouldBeTrue();
        results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
    }

    [Fact]
    public void ShuffleListInPlace_ModifiesOriginalList()
    {
        // Arrange
        List<int> original = Enumerable.Range(1, 100).ToList();
        List<int> copy = original.ToList();

        // Act
        original.ShuffleListInPlace();

        // Assert
        original.Count.ShouldBe(copy.Count);
        original.Order().SequenceEqual(copy.Order()).ShouldBeTrue();
        original.SequenceEqual(copy).ShouldBeFalse();
    }

    [Fact]
    public void Shuffle_ReturnsNewCollection()
    {
        // Arrange
        int[] original = Enumerable.Range(1, 1000).ToArray();

        // Act
        int[] shuffled = original.DeepClone();
        shuffled.Shuffle();

        // Assert
        shuffled.Length.ShouldBe(original.Length);
        shuffled.Order().SequenceEqual(original.Order()).ShouldBeTrue();
        shuffled.SequenceEqual(original).ShouldBeFalse();
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(20, 15)]
    [InlineData(5, 5)]
    public void GenerateRandomString_RespectsLengthBounds(int maxLength, int minLength)
    {
        // Act
        string result = GenerateRandomString(maxLength, minLength);

        // Assert
        result.Length.ShouldBeGreaterThanOrEqualTo(minLength);
        result.Length.ShouldBeLessThanOrEqualTo(maxLength);
    }

    [Theory]
    [InlineData(10, -1, 65, 90)] // uppercase letters
    [InlineData(10, -1, 97, 122)] // lowercase letters
    [InlineData(10, -1, 48, 57)]  // numbers
    public void GenerateRandomString_RespectsAsciiRange(int maxLength, int minLength, int lower, int upper)
    {
        // Act
        string result = GenerateRandomString(maxLength, minLength, lower, upper);

        // Assert
        result.All(c => c >= lower && c <= upper).ShouldBeTrue();
    }

    [Fact]
    public void GenerateRandomString_RespectsBlacklist()
    {
        // Arrange
        char[] blacklist = ['a', 'e', 'i', 'o', 'u'];

        // Act
        string result = GenerateRandomString(100, blacklistedCharacters: blacklist);

        // Assert
        result.Any(c => blacklist.Contains(c)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(10, 20)]
    public void GenerateRandomStrings_GeneratesCorrectNumber(int count, int length)
    {
        // Act
        IEnumerable<string> results = GenerateRandomStrings(count, length);

        // Assert
        results.Count().ShouldBe(count);
        results.All(s => s.Length == length).ShouldBeTrue();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void GenerateRandomStringByCharSet_UsesProvidedCharSet(int length)
    {
        // Arrange
        char[] charset = ['A', 'B', 'C', '1', '2', '3'];

        // Act
        string result = GenerateRandomStringByCharSet(length, charset);

        // Assert
        result.Length.ShouldBe(length);
        result.All(c => charset.Contains(c)).ShouldBeTrue();
    }

    [Fact]
    public void GetRandomElement_ReturnsValidElement()
    {
        // Arrange
        List<int> items = Enumerable.Range(1, 100).ToList();

        // Act
        int? result = items.GetRandomElement();

        // Assert
        result.ShouldNotBeNull();
        items.ShouldContain(result.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetRandomElements_ReturnsCorrectQuantity(int quantity)
    {
        // Arrange
        List<int> items = Enumerable.Range(1, 100).ToList();

        // Act
        IEnumerable<int> results = items.GetRandomElements(quantity);

        // Assert
        results.Count().ShouldBe(quantity);
        results.All(items.Contains).ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void GenerateRandomString_WithInvalidLength_ThrowsException(int maxLength)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(maxLength));
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(0, 0)]
    public void GenerateRandomString_WithInvalidLengthRange_ThrowsException(int minLength, int maxLength)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(maxLength, minLength));
    }
}
=======
﻿using FakeItEasy;
using static CommonNetFuncs.Core.MathHelpers;
using static CommonNetFuncs.Core.Random;
using static CommonNetFuncs.DeepClone.ExpressionTrees;

namespace Core.Tests;

public sealed class RandomTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 5)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void GetRandomInt_WithRange_ReturnsNumberInRange(int minValue, int maxValue)
    {
        // Act
        int result = GetRandomInt(minValue, maxValue);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(minValue);
        result.ShouldBeLessThan(maxValue);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetRandomInt_WithMaxValue_ReturnsNumberInRange(int maxValue)
    {
        // Act
        int result = GetRandomInt(maxValue);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(maxValue);
    }

    [Fact]
    public void GetRandomInt_WithoutParameters_ReturnsPositiveNumber()
    {
        int result = GetRandomInt();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(int.MaxValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetRandomInt_WithInvalidMaxValue_ThrowsException(int maxValue)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GetRandomInt(maxValue: maxValue));
    }

    [Theory]
    [InlineData(5, 0, 100)]
    [InlineData(10, -50, 50)]
    public void GetRandomInts_GeneratesCorrectNumberOfValuesInRange(int count, int min, int max)
    {
        // Act
        IEnumerable<int> results = GetRandomInts(count, min, max);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= min && x < max).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    public void GetRandomDouble_WithPrecision_ReturnsCorrectPrecision(int precision)
    {
        // Act
        double result = GetRandomDouble(precision);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThanOrEqualTo(1);
        result.GetPrecision().ShouldBeLessThanOrEqualTo(precision);
    }

    [Fact]
    public void GetRandomDouble_DefaultPrecision_ReturnsCorrectRange()
    {
        double result = GetRandomDouble();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(1);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    public void GetRandomDoubles_GeneratesCorrectNumberAndPrecision(int count, int precision)
    {
        // Act
        IEnumerable<double> results = GetRandomDoubles(count, precision);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= 0 && x < 1).ShouldBeTrue();
        results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(28)]
    public void GetRandomDecimal_WithPrecision_ReturnsCorrectPrecision(int precision)
    {
        // Act
        decimal result = GetRandomDecimal(precision);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThanOrEqualTo(1);
        result.GetPrecision().ShouldBeLessThanOrEqualTo(precision);
    }

    [Fact]
    public void GetRandomDecimal_DefaultPrecision_ReturnsCorrectRange()
    {
        decimal result = GetRandomDecimal();
        result.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeLessThan(1);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 10)]
    public void GetRandomDecimals_GeneratesCorrectNumberAndPrecision(int count, int precision)
    {
        // Act
        IEnumerable<decimal> results = GetRandomDecimals(count, precision);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x >= 0 && x < 1).ShouldBeTrue();
        results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
    }

    [Fact]
    public void ShuffleListInPlace_ModifiesOriginalList()
    {
        // Arrange
        List<int> original = Enumerable.Range(1, 100).ToList();
        List<int> copy = original.ToList();

        // Act
        original.ShuffleListInPlace();

        // Assert
        original.Count.ShouldBe(copy.Count);
        original.Order().SequenceEqual(copy.Order()).ShouldBeTrue();
        original.SequenceEqual(copy).ShouldBeFalse();
    }

    [Fact]
    public void Shuffle_ReturnsNewCollection()
    {
        // Arrange
        int[] original = Enumerable.Range(1, 1000).ToArray();

        // Act
        int[] shuffled = original.DeepClone();
        shuffled.Shuffle();

        // Assert
        shuffled.Length.ShouldBe(original.Length);
        shuffled.Order().SequenceEqual(original.Order()).ShouldBeTrue();
        shuffled.SequenceEqual(original).ShouldBeFalse();
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(20, 15)]
    [InlineData(5, 5)]
    public void GenerateRandomString_RespectsLengthBounds(int maxLength, int minLength)
    {
        // Act
        string result = GenerateRandomString(maxLength, minLength);

        // Assert
        result.Length.ShouldBeGreaterThanOrEqualTo(minLength);
        result.Length.ShouldBeLessThanOrEqualTo(maxLength);
    }

    [Theory]
    [InlineData(10, -1, 65, 90)] // uppercase letters
    [InlineData(10, -1, 97, 122)] // lowercase letters
    [InlineData(10, -1, 48, 57)]  // numbers
    public void GenerateRandomString_RespectsAsciiRange(int maxLength, int minLength, int lower, int upper)
    {
        // Act
        string result = GenerateRandomString(maxLength, minLength, lower, upper);

        // Assert
        result.All(c => c >= lower && c <= upper).ShouldBeTrue();
    }

    [Fact]
    public void GenerateRandomString_RespectsBlacklist()
    {
        // Arrange
        char[] blacklist = ['a', 'e', 'i', 'o', 'u'];

        // Act
        string result = GenerateRandomString(100, blacklistedCharacters: blacklist);

        // Assert
        result.Any(c => blacklist.Contains(c)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(10, 20)]
    public void GenerateRandomStrings_GeneratesCorrectNumber(int count, int length)
    {
        // Act
        IEnumerable<string> results = GenerateRandomStrings(count, length);

        // Assert
        results.Count().ShouldBe(count);
        results.All(x => x.Length == length).ShouldBeTrue();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void GenerateRandomStringByCharSet_UsesProvidedCharSet(int length)
    {
        // Arrange
        char[] charSet = ['A', 'B', 'C', '1', '2', '3'];

        // Act
        string result = GenerateRandomStringByCharSet(length, charSet);

        // Assert
        result.Length.ShouldBe(length);
        result.All(c => charSet.Contains(c)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void GenerateRandomStringByCharSet_UsesDefaultCharSet(int length)
    {
        // Act
        string result = GenerateRandomStringByCharSet(length);

        // Assert
        result.Length.ShouldBe(length);

        // Check if all characters are from the default char set
        result.All(c => DefaultCharSet.Contains(c)).ShouldBeTrue();
    }

    [Fact]
    public void GetRandomElement_ReturnsValidElement()
    {
        // Arrange
        List<int> items = Enumerable.Range(1, 100).ToList();

        // Act
        int? result = items.GetRandomElement();

        // Assert
        result.ShouldNotBeNull();
        items.ShouldContain(result.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetRandomElements_ReturnsCorrectQuantity(int quantity)
    {
        // Arrange
        List<int> items = Enumerable.Range(1, 100).ToList();

        // Act
        IEnumerable<int> results = items.GetRandomElements(quantity);

        // Assert
        results.Count().ShouldBe(quantity);
        results.All(items.Contains).ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void GenerateRandomString_WithInvalidLength_ThrowsException(int maxLength)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(maxLength));
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(0, 0)]
    public void GenerateRandomString_WithInvalidLengthRange_ThrowsException(int minLength, int maxLength)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(maxLength, minLength));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GetUniqueRandomElements_WhenSelectQuantityIsZeroOrNegative_ShouldThrowArgumentException(int selectQuantity)
    {
        // Arrange
        List<int> items = new() { 1, 2, 3 };

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => items.GetUniqueRandomElements(selectQuantity).ToList());

        exception.Message.ShouldContain("selectQuantity must be greater than 0");
        exception.ParamName.ShouldBe("selectQuantity");
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(null)]
    public void GetUniqueRandomElements_WhenItemsIsEmptyOrNull_ShouldReturnEmptyEnumerable(int[]? items)
    {
        // Arrange
        IEnumerable<int> inputItems = items ?? Enumerable.Empty<int>();

        // Act
        List<int> result = inputItems.GetUniqueRandomElements(1).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, 3)]
    [InlineData(new[] { 1, 2, 3 }, 4)]
    [InlineData(new[] { 1, 2, 3 }, 10)]
    [InlineData(new[] { 5 }, 1)]
    [InlineData(new[] { 5 }, 2)]
    public void GetUniqueRandomElements_WhenSelectQuantityIsGreaterThanOrEqualToUniqueItemCount_ShouldReturnAllUniqueItemsShuffled(int[] items, int selectQuantity)
    {
        // Arrange

        List<int> expectedShuffledItems = items.Distinct().ToList();

        // Act
        List<int> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBe(expectedShuffledItems.Count);
        result.ShouldBeSubsetOf(expectedShuffledItems);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 1, new[] { 0 })]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 2, new[] { 1, 3 })]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 3, new[] { 2, 0, 1 })]
    public void GetUniqueRandomElements_WhenSelectQuantityIsLessThanUniqueItemCount_ShouldUseReservoirSampling(int[] items, int selectQuantity, int[] randomIndices)
    {
        // Act
        List<int> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBe(selectQuantity);
        result.ShouldAllBe(item => items.Contains(item));
        result.Distinct().Count().ShouldBe(selectQuantity); // Ensure all results are unique
    }

    [Theory]
    [InlineData(new[] { 1, 1, 2, 2, 3, 3 }, 2, new[] { 0, 1 })]
    [InlineData(new[] { 5, 5, 5, 5 }, 1, new[] { 0 })]
    public void GetUniqueRandomElements_WhenItemsContainDuplicates_ShouldWorkWithUniqueItemsOnly(int[] items, int selectQuantity, int[] randomIndices)
    {
        // Arrange

        HashSet<int> uniqueItems = new(items);
        _ = uniqueItems.Count;

        // Act
        List<int> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBe(selectQuantity);
        result.ShouldAllBe(item => uniqueItems.Contains(item));
        result.Distinct().Count().ShouldBe(selectQuantity); // Ensure all results are unique
    }

    [Fact]
    public void GetUniqueRandomElements_WithStringItems_ShouldWorkCorrectly()
    {
        // Arrange

        string[] items = { "apple", "banana", "cherry", "date" };
        const int selectQuantity = 2;

        // Act
        List<string> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(item => items.Contains(item));
        result.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void GetUniqueRandomElements_WithDefaultSelectQuantity_ShouldReturnOneItem()
    {
        // Arrange

        int[] items = { 10, 20, 30 };

        // Act
        List<int> result = items.GetUniqueRandomElements().ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeOneOf(items); // Should return the item at index 1
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5 }, 4)]
    public void GetUniqueRandomElements_ShouldCorrectlySwapElementsInReservoirSampling(int[] items, int selectQuantity)
    {
        // Act
        List<int> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBe(selectQuantity);
        result.Distinct().Count().ShouldBe(selectQuantity); // All items should be unique
        result.ShouldAllBe(item => items.Contains(item)); // All items should be from original array
    }

    [Theory]
    [InlineData(new object?[] { null, 1, 2 }, 2)]
    [InlineData(new object?[] { "test", null, "other" }, 1)]
    public void GetUniqueRandomElements_WithNullableItems_ShouldHandleNullValues(object?[] items, int selectQuantity)
    {
        // Act
        List<object?> result = items.GetUniqueRandomElements(selectQuantity).ToList();

        // Assert
        result.Count.ShouldBeLessThanOrEqualTo(selectQuantity);
        result.ShouldAllBe(item => items.Contains(item));
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
