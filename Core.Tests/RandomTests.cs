using FakeItEasy;
using static CommonNetFuncs.Core.MathHelpers;
using static CommonNetFuncs.Core.Random;

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

	[Fact]
	public void GetRandomInt_WhenMinValueGreaterThanMaxValue_ThrowsException()
	{
		// Arrange & Act & Assert
		ArgumentException exception = Should.Throw<ArgumentException>(() => GetRandomInt(10, 5));
		exception.Message.ShouldContain("minValue must be less than or equal to maxValue");
		exception.ParamName.ShouldBe("minValue");
	}

	[Theory]
	[InlineData(5, 5)]
	[InlineData(100, 100)]
	[InlineData(1, 1)]
	public void GetRandomInt_WhenMinValueEqualsMaxValue_ReturnsTheValue(int minValue, int maxValue)
	{
		// Act
		int result = GetRandomInt(minValue, maxValue);

		// Assert
		result.ShouldBe(minValue);
	}

	[Theory]
	[InlineData(5, 0, 100)]
	[InlineData(10, -50, 50)]
	public void GetRandomInts_GeneratesCorrectNumberOfValuesInRange(int count, int min, int max)
	{
		// Act
		IEnumerable<int> results = GetRandomInts(count, min, max, TestContext.Current.CancellationToken);

		// Assert
		results.Count().ShouldBe(count);
		results.All(x => x >= min && x < max).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-10)]
	public void GetRandomInts_WhenNumberToGenerateIsZeroOrNegative_ThrowsException(int numberToGenerate)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRandomInts(numberToGenerate).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
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

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-5)]
	public void GetRandomDouble_WithInvalidPrecision_ThrowsException(int precision)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDouble(precision));
		exception.Message.ShouldContain("decimalPlaces must be greater than 0");
		exception.ParamName.ShouldBe("decimalPlaces");
	}

	[Fact]
	public void GetRandomDouble_WithPrecisionGreaterThan15_CapsAt15()
	{
		// Act
		double result = GetRandomDouble(20);

		// Assert
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(15);
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
		IEnumerable<double> results = GetRandomDoubles(count, precision, TestContext.Current.CancellationToken);

		// Assert
		results.Count().ShouldBe(count);
		results.All(x => x is >= 0 and < 1).ShouldBeTrue();
		results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-10)]
	public void GetRandomDoubles_WhenNumberToGenerateIsZeroOrNegative_ThrowsException(int numberToGenerate)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDoubles(numberToGenerate).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
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

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-5)]
	public void GetRandomDecimal_WithInvalidPrecision_ThrowsException(int precision)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDecimal(precision));
		exception.Message.ShouldContain("decimalPlaces must be greater than 0");
		exception.ParamName.ShouldBe("decimalPlaces");
	}

	[Fact]
	public void GetRandomDecimal_WithPrecisionGreaterThan28_CapsAt28()
	{
		// Act
		decimal result = GetRandomDecimal(35);

		// Assert
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(28);
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
		IEnumerable<decimal> results = GetRandomDecimals(count, precision, TestContext.Current.CancellationToken);

		// Assert
		results.Count().ShouldBe(count);
		results.All(x => x is >= 0 and < 1).ShouldBeTrue();
		results.All(x => x.GetPrecision() <= precision).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-10)]
	public void GetRandomDecimals_WhenNumberToGenerateIsZeroOrNegative_ThrowsException(int numberToGenerate)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDecimals(numberToGenerate).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	[Fact]
	public void ShuffleListInPlace_ModifiesOriginalList()
	{
		// Arrange
		List<int> original = Enumerable.Range(1, 100).ToList();
		List<int> copy = original.ToList();

		// Act
		original.ShuffleListInPlace(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		original.Count.ShouldBe(copy.Count);
		original.Order().SequenceEqual(copy.Order()).ShouldBeTrue();
		original.SequenceEqual(copy).ShouldBeFalse();
	}

	[Fact]
	public void ShuffleListInPlace_WithEmptyList_ReturnsEmptyList()
	{
		// Arrange
		List<int> emptyList = new();

		// Act
		IList<int> result = emptyList.ShuffleListInPlace(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void ShuffleListInPlace_WithSingleElement_ReturnsSameElement()
	{
		// Arrange
		List<int> singleItem = new() { 42 };

		// Act
		IList<int> result = singleItem.ShuffleListInPlace(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Count.ShouldBe(1);
		result[0].ShouldBe(42);
	}

	[Fact]
	public void Shuffle_ReturnsNewCollection()
	{
		// Arrange
		int[] original = Enumerable.Range(1, 1000).ToArray();

		// Act
		int[] shuffled = new int[1000];
		original.CopyTo(shuffled, 0);
		shuffled.Shuffle();

		// Assert
		shuffled.Length.ShouldBe(original.Length);
		shuffled.Order().SequenceEqual(original.Order()).ShouldBeTrue();
		shuffled.SequenceEqual(original).ShouldBeFalse();
	}

	[Fact]
	public void ShuffleLinq_ShufflesCollection()
	{
		// Arrange
		int[] original = Enumerable.Range(1, 100).ToArray();

		// Act
		int[] shuffled = original.ShuffleLinq().ToArray();

		// Assert
		shuffled.Length.ShouldBe(original.Length);
		shuffled.Order().SequenceEqual(original.Order()).ShouldBeTrue();
		shuffled.SequenceEqual(original).ShouldBeFalse();
	}

	[Fact]
	public void Shuffle_WithEmptyEnumerable_ReturnsEmpty()
	{
		// Arrange
		List<int> empty = new();

		// Act
		IEnumerable<int> result = empty.Shuffle();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void Shuffle_IList_WithEmptyList_ReturnsEmpty()
	{
		// Arrange
		List<int> empty = new();

		// Act
		List<int> result = empty.Shuffle();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void Shuffle_Array_WithEmptyArray_RemainsEmpty()
	{
		// Arrange
		int[] empty = Array.Empty<int>();

		// Act
		empty.Shuffle();

		// Assert
		empty.ShouldBeEmpty();
	}

	[Fact]
	public void Shuffle_Array_ShufflesInPlace()
	{
		// Arrange
		int[] original = Enumerable.Range(1, 50).ToArray();
		int[] copy = original.ToArray();

		// Act
		original.Shuffle();

		// Assert
		original.Length.ShouldBe(copy.Length);
		original.Order().SequenceEqual(copy.Order()).ShouldBeTrue();
		original.SequenceEqual(copy).ShouldBeFalse();
	}

	[Fact]
	public void Shuffle_Span_ShufflesInPlace()
	{
		// Arrange
		int[] original = Enumerable.Range(1, 50).ToArray();
		int[] copy = original.ToArray();
		Span<int> span = original.AsSpan();

		// Act
		span.Shuffle();

		// Assert
		original.Length.ShouldBe(copy.Length);
		original.Order().SequenceEqual(copy.Order()).ShouldBeTrue();
		original.SequenceEqual(copy).ShouldBeFalse();
	}

	[Theory]
	[InlineData(10, 5)]
	[InlineData(20, 15)]
	[InlineData(5, 5)]
	public void GenerateRandomString_RespectsLengthBounds(int maxLength, int minLength)
	{
		// Act
		string result = GenerateRandomString(maxLength, minLength, cancellationToken: TestContext.Current.CancellationToken);

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
		string result = GenerateRandomString(maxLength, minLength, lower, upper, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.All(c => c >= lower && c <= upper).ShouldBeTrue();
	}

	[Fact]
	public void GenerateRandomString_RespectsBlacklist()
	{
		// Arrange
		HashSet<char> blacklist = ['a', 'e', 'i', 'o', 'u'];

		// Act
		string result = GenerateRandomString(100, blacklistedCharacters: blacklist, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Any(blacklist.Contains).ShouldBeFalse();
	}

	[Theory]
	[InlineData(5, 10)]
	[InlineData(10, 20)]
	public void GenerateRandomStrings_GeneratesCorrectNumber(int count, int length)
	{
		// Act
		IEnumerable<string> results = GenerateRandomStrings(count, length, cancellationToken: TestContext.Current.CancellationToken);

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
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];

		// Act
		string result = GenerateRandomStringByCharSet(length, charSet, TestContext.Current.CancellationToken);

		// Assert
		result.Length.ShouldBe(length);
		result.All(charSet.Contains).ShouldBeTrue();
	}

	[Theory]
	[InlineData(10)]
	[InlineData(20)]
	public void GenerateRandomStringByCharSet_UsesDefaultCharSet(int length)
	{
		// Act
		string result = GenerateRandomStringByCharSet(length, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Length.ShouldBe(length);

		// Check if all characters are from the default char set
		result.All(DefaultCharSet.Contains).ShouldBeTrue();
	}

	[Fact]
	public void GenerateRandomStringByCharSet_WithEmptyCharSet_UsesDefaultCharSet()
	{
		// Arrange
		HashSet<char> emptyCharSet = new();

		// Act
		string result = GenerateRandomStringByCharSet(10, emptyCharSet, TestContext.Current.CancellationToken);

		// Assert
		result.Length.ShouldBe(10);
		result.All(DefaultCharSet.Contains).ShouldBeTrue();
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

	[Fact]
	public void GetRandomElement_WithSingleElement_ReturnsThatElement()
	{
		// Arrange
		List<int> items = new() { 42 };

		// Act
		int? result = items.GetRandomElement();

		// Assert
		result.ShouldBe(42);
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

	[Fact]
	public void GetRandomElements_WithEmptyCollection_ThrowsException()
	{
		// Arrange
		List<int> empty = new();

		// Act & Assert
		Should.Throw<ArgumentException>(() => empty.GetRandomElements(5));
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

	[Fact]
	public void GenerateRandomString_WhenBlacklistContainsAllAvailableCharacters_ThrowsException()
	{
		// Arrange
		HashSet<char> blacklist = new();
		for (int i = 65; i <= 90; i++) // A-Z
		{
			blacklist.Add((char)i);
		}

		// Act & Assert
		ArgumentException exception = Should.Throw<ArgumentException>(() =>
			GenerateRandomString(10, lowerAsciiBound: 65, upperAsciiBound: 90, blacklistedCharacters: blacklist));
		exception.Message.ShouldContain("Black list contains all available values");
		exception.ParamName.ShouldBe("blacklistedCharacters");
	}

	// [Fact]
	// public void GenerateRandomString_WhenBlacklistHasCharsOutsideRange_ThrowsNoAvailableCharsException()
	// {
	// 	// Arrange - Blacklist contains all chars IN the range plus extras OUTSIDE
	// 	// This tests the defensive check that counts only blacklisted chars within the actual range
	// 	HashSet<char> blacklist = new();
	// 	blacklist.Add((char)65); // 'A' - in range
	// 	blacklist.Add((char)66); // 'B' - in range
	// 	blacklist.Add((char)97); // 'a' - outside range
	// 	blacklist.Add((char)98); // 'b' - outside range

	// 	// Act & Assert - With range 65-66 (2 chars 'A', 'B') and blacklist {65, 66, 97, 98}:
	// 	// availableCharCount = 2 - 2 = 0 (only counts blacklist chars within range)
	// 	// This hits the defensive check!
	// 	ArgumentException exception = Should.Throw<ArgumentException>(() =>
	// 		GenerateRandomString(10, lowerAsciiBound: 65, upperAsciiBound: 66, blacklistedCharacters: blacklist));
	// 	exception.Message.ShouldContain("No available characters to use after applying blacklist");
	// 	exception.ParamName.ShouldBe("blacklistedCharacters");
	// }

	[Theory]
	[InlineData(-1, 126)]
	[InlineData(0, 128)]
	[InlineData(100, 50)]
	public void GenerateRandomString_WithInvalidAsciiBounds_ThrowsException(int lower, int upper)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() =>
			GenerateRandomString(10, lowerAsciiBound: lower, upperAsciiBound: upper));
		exception.Message.ShouldContain("Bounds must be between 0 and 127, and lowerBound must be less than upperBound");
		exception.ParamName.ShouldBe("upperAsciiBound");
	}

	[Fact]
	public void GenerateRandomString_WhenMinLengthEqualsMaxLength_ReturnsExactLength()
	{
		// Act
		string result = GenerateRandomString(10, 10, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Length.ShouldBe(10);
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
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 1)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 2)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 3)]
	public void GetUniqueRandomElements_WhenSelectQuantityIsLessThanUniqueItemCount_ShouldUseReservoirSampling(int[] items, int selectQuantity)
	{
		// Act
		List<int> result = items.GetUniqueRandomElements(selectQuantity).ToList();

		// Assert
		result.Count.ShouldBe(selectQuantity);
		result.ShouldAllBe(item => items.Contains(item));
		result.Distinct().Count().ShouldBe(selectQuantity); // Ensure all results are unique
	}

	[Theory]
	[InlineData(new[] { 1, 1, 2, 2, 3, 3 }, 2)]
	[InlineData(new[] { 5, 5, 5, 5 }, 1)]
	public void GetUniqueRandomElements_WhenItemsContainDuplicates_ShouldWorkWithUniqueItemsOnly(int[] items, int selectQuantity)
	{
		// Arrange
		HashSet<int> uniqueItems = new(items);

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
