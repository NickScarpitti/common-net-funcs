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

	// -------------------------------------------------------------------------
	// GetRepeatableRandomInt
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(0, 10, "seed1")]
	[InlineData(-5, 5, "seed2")]
	[InlineData(1, 100, "seed3")]
	public void GetRepeatableRandomInt_WithRangeAndSeed_ReturnsNumberInRange(int minValue, int maxValue, string seed)
	{
		int result = GetRepeatableRandomInt(minValue, maxValue, seed);
		result.ShouldBeGreaterThanOrEqualTo(minValue);
		result.ShouldBeLessThan(maxValue);
	}

	[Theory]
	[InlineData(0, 10, "seed1")]
	[InlineData(-5, 5, "seed2")]
	[InlineData(1, 100, "seed3")]
	public void GetRepeatableRandomInt_WithRangeAndSeed_IsRepeatable(int minValue, int maxValue, string seed)
	{
		int result1 = GetRepeatableRandomInt(minValue, maxValue, seed);
		int result2 = GetRepeatableRandomInt(minValue, maxValue, seed);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(0, 10)]
	[InlineData(-5, 5)]
	[InlineData(1, 100)]
	public void GetRepeatableRandomInt_WithRangeAndRnd_ReturnsNumberInRange(int minValue, int maxValue)
	{
		System.Random rnd = new(42);
		int result = GetRepeatableRandomInt(minValue, maxValue, rnd);
		result.ShouldBeGreaterThanOrEqualTo(minValue);
		result.ShouldBeLessThan(maxValue);
	}

	[Theory]
	[InlineData(0, 10)]
	[InlineData(-5, 5)]
	[InlineData(1, 100)]
	public void GetRepeatableRandomInt_WithRangeAndRnd_IsRepeatable(int minValue, int maxValue)
	{
		System.Random rnd1 = new(42);
		System.Random rnd2 = new(42);
		int result1 = GetRepeatableRandomInt(minValue, maxValue, rnd1);
		int result2 = GetRepeatableRandomInt(minValue, maxValue, rnd2);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(5, 5)]
	[InlineData(100, 100)]
	public void GetRepeatableRandomInt_WithRangeAndRnd_WhenMinEqualsMax_ReturnsMinValue(int minValue, int maxValue)
	{
		int result = GetRepeatableRandomInt(minValue, maxValue, new System.Random(1));
		result.ShouldBe(minValue);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomInt_WithRangeAndRnd_WhenMaxValueInvalid_ThrowsException(int maxValue)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomInt(0, maxValue, new System.Random(1)));
		exception.ParamName.ShouldBe("maxValue");
	}

	[Fact]
	public void GetRepeatableRandomInt_WithRangeAndRnd_WhenMinValueGreaterThanMaxValue_ThrowsException()
	{
		ArgumentException exception = Should.Throw<ArgumentException>(() => GetRepeatableRandomInt(10, 5, new System.Random(1)));
		exception.Message.ShouldContain("minValue must be less than or equal to maxValue");
		exception.ParamName.ShouldBe("minValue");
	}

	[Theory]
	[InlineData(10, "seed1")]
	[InlineData(100, "seed2")]
	public void GetRepeatableRandomInt_WithMaxValueAndSeed_ReturnsInRange(int maxValue, string seed)
	{
		int result = GetRepeatableRandomInt(maxValue, seed);
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(maxValue);
	}

	[Theory]
	[InlineData(10, "seed1")]
	[InlineData(100, "seed2")]
	public void GetRepeatableRandomInt_WithMaxValueAndSeed_IsRepeatable(int maxValue, string seed)
	{
		int result1 = GetRepeatableRandomInt(maxValue, seed);
		int result2 = GetRepeatableRandomInt(maxValue, seed);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(10)]
	[InlineData(100)]
	public void GetRepeatableRandomInt_WithMaxValueAndRnd_ReturnsInRange(int maxValue)
	{
		int result = GetRepeatableRandomInt(maxValue, new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(maxValue);
	}

	[Theory]
	[InlineData(10)]
	[InlineData(100)]
	public void GetRepeatableRandomInt_WithMaxValueAndRnd_IsRepeatable(int maxValue)
	{
		int result1 = GetRepeatableRandomInt(maxValue, new System.Random(42));
		int result2 = GetRepeatableRandomInt(maxValue, new System.Random(42));
		result1.ShouldBe(result2);
	}

	[Fact]
	public void GetRepeatableRandomInt_WithSeedOnly_ReturnsNonNegativeNumber()
	{
		int result = GetRepeatableRandomInt("anySeed");
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(int.MaxValue);
	}

	[Fact]
	public void GetRepeatableRandomInt_WithSeedOnly_IsRepeatable()
	{
		int result1 = GetRepeatableRandomInt("anySeed");
		int result2 = GetRepeatableRandomInt("anySeed");
		result1.ShouldBe(result2);
	}

	[Fact]
	public void GetRepeatableRandomInt_WithRndOnly_ReturnsNonNegativeNumber()
	{
		int result = GetRepeatableRandomInt(new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(int.MaxValue);
	}

	[Fact]
	public void GetRepeatableRandomInt_WithRndOnly_IsRepeatable()
	{
		int result1 = GetRepeatableRandomInt(new System.Random(42));
		int result2 = GetRepeatableRandomInt(new System.Random(42));
		result1.ShouldBe(result2);
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomInts
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(5, 0, 100, "seed1")]
	[InlineData(10, -50, 50, "seed2")]
	public void GetRepeatableRandomInts_WithSeed_GeneratesCorrectCountAndRange(int count, int min, int max, string seed)
	{
		List<int> results = GetRepeatableRandomInts(count, seed, min, max, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= min && x < max);
	}

	[Theory]
	[InlineData(5, 0, 100, "seed1")]
	[InlineData(10, -50, 50, "seed2")]
	public void GetRepeatableRandomInts_WithSeed_IsRepeatable(int count, int min, int max, string seed)
	{
		List<int> results1 = GetRepeatableRandomInts(count, seed, min, max, TestContext.Current.CancellationToken).ToList();
		List<int> results2 = GetRepeatableRandomInts(count, seed, min, max, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(5, 0, 100)]
	[InlineData(10, -50, 50)]
	public void GetRepeatableRandomInts_WithRnd_GeneratesCorrectCountAndRange(int count, int min, int max)
	{
		List<int> results = GetRepeatableRandomInts(count, new System.Random(42), min, max, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= min && x < max);
	}

	[Theory]
	[InlineData(5, 0, 100)]
	[InlineData(10, -50, 50)]
	public void GetRepeatableRandomInts_WithRnd_IsRepeatable(int count, int min, int max)
	{
		List<int> results1 = GetRepeatableRandomInts(count, new System.Random(42), min, max, TestContext.Current.CancellationToken).ToList();
		List<int> results2 = GetRepeatableRandomInts(count, new System.Random(42), min, max, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomInts_WithSeed_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomInts(numberToGenerate, "seed").ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomInts_WithRnd_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomInts(numberToGenerate, new System.Random(1)).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomDouble
	// -------------------------------------------------------------------------

	[Fact]
	public void GetRepeatableRandomDouble_WithSeed_ReturnsValueInRange()
	{
		double result = GetRepeatableRandomDouble("testSeed");
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
	}

	[Fact]
	public void GetRepeatableRandomDouble_WithSeed_IsRepeatable()
	{
		double result1 = GetRepeatableRandomDouble("testSeed");
		double result2 = GetRepeatableRandomDouble("testSeed");
		result1.ShouldBe(result2);
	}

	[Fact]
	public void GetRepeatableRandomDouble_WithRnd_ReturnsValueInRange()
	{
		double result = GetRepeatableRandomDouble(new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
	}

	[Fact]
	public void GetRepeatableRandomDouble_WithRnd_IsRepeatable()
	{
		double result1 = GetRepeatableRandomDouble(new System.Random(42));
		double result2 = GetRepeatableRandomDouble(new System.Random(42));
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(15, "seed3")]
	public void GetRepeatableRandomDouble_WithDecimalPlacesAndSeed_ReturnsCorrectPrecision(int decimalPlaces, string seed)
	{
		double result = GetRepeatableRandomDouble(decimalPlaces, seed);
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(decimalPlaces);
	}

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(15, "seed3")]
	public void GetRepeatableRandomDouble_WithDecimalPlacesAndSeed_IsRepeatable(int decimalPlaces, string seed)
	{
		double result1 = GetRepeatableRandomDouble(decimalPlaces, seed);
		double result2 = GetRepeatableRandomDouble(decimalPlaces, seed);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(15)]
	public void GetRepeatableRandomDouble_WithDecimalPlacesAndRnd_ReturnsCorrectPrecision(int decimalPlaces)
	{
		double result = GetRepeatableRandomDouble(decimalPlaces, new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(decimalPlaces);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(15)]
	public void GetRepeatableRandomDouble_WithDecimalPlacesAndRnd_IsRepeatable(int decimalPlaces)
	{
		double result1 = GetRepeatableRandomDouble(decimalPlaces, new System.Random(42));
		double result2 = GetRepeatableRandomDouble(decimalPlaces, new System.Random(42));
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(16)]
	[InlineData(20)]
	public void GetRepeatableRandomDouble_WithDecimalPlacesGreaterThan15AndSeed_ThrowsException(int decimalPlaces)
	{
		// Unlike GetRandomDouble which caps at 15, GetRepeatableRandomDouble does not clamp and will throw
		Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDouble(decimalPlaces, "testSeed"));
	}

	[Theory]
	[InlineData(16)]
	[InlineData(20)]
	public void GetRepeatableRandomDouble_WithDecimalPlacesGreaterThan15AndRnd_ThrowsException(int decimalPlaces)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDouble(decimalPlaces, new System.Random(42)));
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomDoubles
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(5, 3, "seed1")]
	[InlineData(10, 10, "seed2")]
	public void GetRepeatableRandomDoubles_WithSeed_GeneratesCorrectCountAndPrecision(int count, int decimalPlaces, string seed)
	{
		List<double> results = GetRepeatableRandomDoubles(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= 0 && x < 1);
		results.ShouldAllBe(x => x.GetPrecision() <= decimalPlaces);
	}

	[Theory]
	[InlineData(5, 3, "seed1")]
	[InlineData(10, 10, "seed2")]
	public void GetRepeatableRandomDoubles_WithSeed_IsRepeatable(int count, int decimalPlaces, string seed)
	{
		List<double> results1 = GetRepeatableRandomDoubles(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		List<double> results2 = GetRepeatableRandomDoubles(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(5, 3)]
	[InlineData(10, 10)]
	public void GetRepeatableRandomDoubles_WithRnd_GeneratesCorrectCountAndPrecision(int count, int decimalPlaces)
	{
		List<double> results = GetRepeatableRandomDoubles(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= 0 && x < 1);
		results.ShouldAllBe(x => x.GetPrecision() <= decimalPlaces);
	}

	[Theory]
	[InlineData(5, 3)]
	[InlineData(10, 10)]
	public void GetRepeatableRandomDoubles_WithRnd_IsRepeatable(int count, int decimalPlaces)
	{
		List<double> results1 = GetRepeatableRandomDoubles(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		List<double> results2 = GetRepeatableRandomDoubles(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDoubles_WithSeed_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDoubles(numberToGenerate, "seed").ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDoubles_WithRnd_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDoubles(numberToGenerate, new System.Random(1)).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomDecimal
	// -------------------------------------------------------------------------

	[Fact]
	public void GetRepeatableRandomDecimal_WithSeed_ReturnsValueInRange()
	{
		decimal result = GetRepeatableRandomDecimal("testSeed");
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
	}

	[Fact]
	public void GetRepeatableRandomDecimal_WithSeed_IsRepeatable()
	{
		decimal result1 = GetRepeatableRandomDecimal("testSeed");
		decimal result2 = GetRepeatableRandomDecimal("testSeed");
		result1.ShouldBe(result2);
	}

	[Fact]
	public void GetRepeatableRandomDecimal_WithRnd_ReturnsValueInRange()
	{
		decimal result = GetRepeatableRandomDecimal(new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
	}

	[Fact]
	public void GetRepeatableRandomDecimal_WithRnd_IsRepeatable()
	{
		decimal result1 = GetRepeatableRandomDecimal(new System.Random(42));
		decimal result2 = GetRepeatableRandomDecimal(new System.Random(42));
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(28, "seed3")]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndSeed_ReturnsCorrectPrecision(int decimalPlaces, string seed)
	{
		decimal result = GetRepeatableRandomDecimal(decimalPlaces, seed);
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(decimalPlaces);
	}

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(28, "seed3")]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndSeed_IsRepeatable(int decimalPlaces, string seed)
	{
		decimal result1 = GetRepeatableRandomDecimal(decimalPlaces, seed);
		decimal result2 = GetRepeatableRandomDecimal(decimalPlaces, seed);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(28)]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndRnd_ReturnsCorrectPrecision(int decimalPlaces)
	{
		decimal result = GetRepeatableRandomDecimal(decimalPlaces, new System.Random(42));
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(decimalPlaces);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(28)]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndRnd_IsRepeatable(int decimalPlaces)
	{
		decimal result1 = GetRepeatableRandomDecimal(decimalPlaces, new System.Random(42));
		decimal result2 = GetRepeatableRandomDecimal(decimalPlaces, new System.Random(42));
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndSeed_WhenDecimalPlacesInvalid_ThrowsException(int decimalPlaces)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDecimal(decimalPlaces, "seed"));
		exception.Message.ShouldContain("decimalPlaces must be greater than 0");
		exception.ParamName.ShouldBe("decimalPlaces");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesAndRnd_WhenDecimalPlacesInvalid_ThrowsException(int decimalPlaces)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDecimal(decimalPlaces, new System.Random(1)));
		exception.Message.ShouldContain("decimalPlaces must be greater than 0");
		exception.ParamName.ShouldBe("decimalPlaces");
	}

	[Fact]
	public void GetRepeatableRandomDecimal_WithDecimalPlacesGreaterThan28AndSeed_CapsAt28()
	{
		decimal result = GetRepeatableRandomDecimal(35, "testSeed");
		result.ShouldBeGreaterThanOrEqualTo(0);
		result.ShouldBeLessThan(1);
		result.GetPrecision().ShouldBeLessThanOrEqualTo(28);
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomDecimals
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(5, 3, "seed1")]
	[InlineData(10, 10, "seed2")]
	public void GetRepeatableRandomDecimals_WithSeed_GeneratesCorrectCountAndPrecision(int count, int decimalPlaces, string seed)
	{
		List<decimal> results = GetRepeatableRandomDecimals(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= 0 && x < 1);
		results.ShouldAllBe(x => x.GetPrecision() <= decimalPlaces);
	}

	[Theory]
	[InlineData(5, 3, "seed1")]
	[InlineData(10, 10, "seed2")]
	public void GetRepeatableRandomDecimals_WithSeed_IsRepeatable(int count, int decimalPlaces, string seed)
	{
		List<decimal> results1 = GetRepeatableRandomDecimals(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		List<decimal> results2 = GetRepeatableRandomDecimals(count, seed, decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(5, 3)]
	[InlineData(10, 10)]
	public void GetRepeatableRandomDecimals_WithRnd_GeneratesCorrectCountAndPrecision(int count, int decimalPlaces)
	{
		List<decimal> results = GetRepeatableRandomDecimals(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x >= 0 && x < 1);
		results.ShouldAllBe(x => x.GetPrecision() <= decimalPlaces);
	}

	[Theory]
	[InlineData(5, 3)]
	[InlineData(10, 10)]
	public void GetRepeatableRandomDecimals_WithRnd_IsRepeatable(int count, int decimalPlaces)
	{
		List<decimal> results1 = GetRepeatableRandomDecimals(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		List<decimal> results2 = GetRepeatableRandomDecimals(count, new System.Random(42), decimalPlaces, TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDecimals_WithSeed_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDecimals(numberToGenerate, "seed").ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableRandomDecimals_WithRnd_WhenNumberToGenerateInvalid_ThrowsException(int numberToGenerate)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => GetRepeatableRandomDecimals(numberToGenerate, new System.Random(1)).ToList());
		exception.Message.ShouldContain("Number to generate must be greater than 0");
		exception.ParamName.ShouldBe("numberToGenerate");
	}

	// -------------------------------------------------------------------------
	// RepeatableShuffleListInPlace
	// -------------------------------------------------------------------------

	[Fact]
	public void RepeatableShuffleListInPlace_WithSeed_ContainsSameElements()
	{
		List<int> original = Enumerable.Range(1, 100).ToList();
		List<int> copy = original.ToList();
		copy.RepeatableShuffleListInPlace("testSeed", TestContext.Current.CancellationToken);
		copy.Count.ShouldBe(original.Count);
		copy.Order().SequenceEqual(original.Order()).ShouldBeTrue();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithSeed_IsRepeatable()
	{
		List<int> list1 = Enumerable.Range(1, 100).ToList();
		List<int> list2 = Enumerable.Range(1, 100).ToList();
		list1.RepeatableShuffleListInPlace("testSeed", TestContext.Current.CancellationToken);
		list2.RepeatableShuffleListInPlace("testSeed", TestContext.Current.CancellationToken);
		list1.SequenceEqual(list2).ShouldBeTrue();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithRnd_ContainsSameElements()
	{
		List<int> original = Enumerable.Range(1, 100).ToList();
		List<int> copy = original.ToList();
		copy.RepeatableShuffleListInPlace(new System.Random(42), TestContext.Current.CancellationToken);
		copy.Count.ShouldBe(original.Count);
		copy.Order().SequenceEqual(original.Order()).ShouldBeTrue();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithRnd_IsRepeatable()
	{
		List<int> list1 = Enumerable.Range(1, 100).ToList();
		List<int> list2 = Enumerable.Range(1, 100).ToList();
		list1.RepeatableShuffleListInPlace(new System.Random(42), TestContext.Current.CancellationToken);
		list2.RepeatableShuffleListInPlace(new System.Random(42), TestContext.Current.CancellationToken);
		list1.SequenceEqual(list2).ShouldBeTrue();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithSeed_WithEmptyList_ReturnsEmptyList()
	{
		List<int> emptyList = new();
		IList<int> result = emptyList.RepeatableShuffleListInPlace("testSeed", TestContext.Current.CancellationToken);
		result.ShouldBeEmpty();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithRnd_WithEmptyList_ReturnsEmptyList()
	{
		List<int> emptyList = new();
		IList<int> result = emptyList.RepeatableShuffleListInPlace(new System.Random(42), TestContext.Current.CancellationToken);
		result.ShouldBeEmpty();
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithSeed_WithSingleElement_ReturnsSameElement()
	{
		List<int> singleItem = new() { 42 };
		IList<int> result = singleItem.RepeatableShuffleListInPlace("testSeed", TestContext.Current.CancellationToken);
		result.Count.ShouldBe(1);
		result[0].ShouldBe(42);
	}

	[Fact]
	public void RepeatableShuffleListInPlace_WithRnd_WithSingleElement_ReturnsSameElement()
	{
		List<int> singleItem = new() { 42 };
		IList<int> result = singleItem.RepeatableShuffleListInPlace(new System.Random(1), TestContext.Current.CancellationToken);
		result.Count.ShouldBe(1);
		result[0].ShouldBe(42);
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomElement
	// -------------------------------------------------------------------------

	[Fact]
	public void GetRepeatableRandomElement_WithSeed_ReturnsValidElement()
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		int? result = items.GetRepeatableRandomElement("testSeed");
		result.ShouldNotBeNull();
		items.ShouldContain(result.Value);
	}

	[Fact]
	public void GetRepeatableRandomElement_WithSeed_IsRepeatable()
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		int? result1 = items.GetRepeatableRandomElement("testSeed");
		int? result2 = items.GetRepeatableRandomElement("testSeed");
		result1.ShouldBe(result2);
	}

	[Fact]
	public void GetRepeatableRandomElement_WithRnd_ReturnsValidElement()
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		int? result = items.GetRepeatableRandomElement(new System.Random(42));
		result.ShouldNotBeNull();
		items.ShouldContain(result.Value);
	}

	[Fact]
	public void GetRepeatableRandomElement_WithRnd_IsRepeatable()
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		int? result1 = items.GetRepeatableRandomElement(new System.Random(42));
		int? result2 = items.GetRepeatableRandomElement(new System.Random(42));
		result1.ShouldBe(result2);
	}

	// -------------------------------------------------------------------------
	// GetRepeatableRandomElements
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(10, "seed3")]
	public void GetRepeatableRandomElements_WithSeed_ReturnsCorrectQuantityAndValidElements(int quantity, string seed)
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		List<int> results = items.GetRepeatableRandomElements(seed, quantity).ToList();
		results.Count.ShouldBe(quantity);
		results.ShouldAllBe(x => items.Contains(x));
	}

	[Theory]
	[InlineData(1, "seed1")]
	[InlineData(5, "seed2")]
	[InlineData(10, "seed3")]
	public void GetRepeatableRandomElements_WithSeed_IsRepeatable(int quantity, string seed)
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		List<int> results1 = items.GetRepeatableRandomElements(seed, quantity).ToList();
		List<int> results2 = items.GetRepeatableRandomElements(seed, quantity).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	public void GetRepeatableRandomElements_WithRnd_ReturnsCorrectQuantityAndValidElements(int quantity)
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		List<int> results = items.GetRepeatableRandomElements(new System.Random(42), quantity).ToList();
		results.Count.ShouldBe(quantity);
		results.ShouldAllBe(x => items.Contains(x));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	public void GetRepeatableRandomElements_WithRnd_IsRepeatable(int quantity)
	{
		List<int> items = Enumerable.Range(1, 100).ToList();
		List<int> results1 = items.GetRepeatableRandomElements(new System.Random(42), quantity).ToList();
		List<int> results2 = items.GetRepeatableRandomElements(new System.Random(42), quantity).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	// -------------------------------------------------------------------------
	// GetRepeatableUniqueRandomElements
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableUniqueRandomElements_WithSeed_WhenSelectQuantityInvalid_ThrowsException(int selectQuantity)
	{
		List<int> items = new() { 1, 2, 3 };
		ArgumentException exception = Should.Throw<ArgumentException>(() => items.GetRepeatableUniqueRandomElements("seed", selectQuantity).ToList());
		exception.Message.ShouldContain("selectQuantity must be greater than 0");
		exception.ParamName.ShouldBe("selectQuantity");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void GetRepeatableUniqueRandomElements_WithRnd_WhenSelectQuantityInvalid_ThrowsException(int selectQuantity)
	{
		List<int> items = new() { 1, 2, 3 };
		ArgumentException exception = Should.Throw<ArgumentException>(() => items.GetRepeatableUniqueRandomElements(new System.Random(1), selectQuantity).ToList());
		exception.Message.ShouldContain("selectQuantity must be greater than 0");
		exception.ParamName.ShouldBe("selectQuantity");
	}

	[Theory]
	[InlineData(new int[] { })]
	[InlineData(null)]
	public void GetRepeatableUniqueRandomElements_WithSeed_WhenItemsIsEmpty_ReturnsEmpty(int[]? items)
	{
		IEnumerable<int> inputItems = items ?? Enumerable.Empty<int>();
		List<int> result = inputItems.GetRepeatableUniqueRandomElements("seed", 1).ToList();
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(new int[] { })]
	[InlineData(null)]
	public void GetRepeatableUniqueRandomElements_WithRnd_WhenItemsIsEmpty_ReturnsEmpty(int[]? items)
	{
		IEnumerable<int> inputItems = items ?? Enumerable.Empty<int>();
		List<int> result = inputItems.GetRepeatableUniqueRandomElements(new System.Random(1), 1).ToList();
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 3, "seed1")]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 1, "seed2")]
	public void GetRepeatableUniqueRandomElements_WithSeed_ReturnsUniqueElementsFromSource(int[] items, int selectQuantity, string seed)
	{
		List<int> result = items.GetRepeatableUniqueRandomElements(seed, selectQuantity).ToList();
		result.Count.ShouldBe(selectQuantity);
		result.ShouldAllBe(x => items.Contains(x));
		result.Distinct().Count().ShouldBe(selectQuantity);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 3, "seed1")]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 1, "seed2")]
	public void GetRepeatableUniqueRandomElements_WithSeed_IsRepeatable(int[] items, int selectQuantity, string seed)
	{
		List<int> result1 = items.GetRepeatableUniqueRandomElements(seed, selectQuantity).ToList();
		List<int> result2 = items.GetRepeatableUniqueRandomElements(seed, selectQuantity).ToList();
		result1.SequenceEqual(result2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 3)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 1)]
	public void GetRepeatableUniqueRandomElements_WithRnd_ReturnsUniqueElementsFromSource(int[] items, int selectQuantity)
	{
		List<int> result = items.GetRepeatableUniqueRandomElements(new System.Random(42), selectQuantity).ToList();
		result.Count.ShouldBe(selectQuantity);
		result.ShouldAllBe(x => items.Contains(x));
		result.Distinct().Count().ShouldBe(selectQuantity);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 3)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 1)]
	public void GetRepeatableUniqueRandomElements_WithRnd_IsRepeatable(int[] items, int selectQuantity)
	{
		List<int> result1 = items.GetRepeatableUniqueRandomElements(new System.Random(42), selectQuantity).ToList();
		List<int> result2 = items.GetRepeatableUniqueRandomElements(new System.Random(42), selectQuantity).ToList();
		result1.SequenceEqual(result2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, 3, "seed1")]
	[InlineData(new[] { 1, 2, 3 }, 5, "seed2")]
	public void GetRepeatableUniqueRandomElements_WithSeed_WhenSelectQuantityGeUniqueCount_ReturnsAllUniqueItemsShuffled(int[] items, int selectQuantity, string seed)
	{
		List<int> uniqueItems = items.Distinct().ToList();
		List<int> result = items.GetRepeatableUniqueRandomElements(seed, selectQuantity).ToList();
		result.Count.ShouldBe(uniqueItems.Count);
		result.ShouldBeSubsetOf(uniqueItems);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, 3)]
	[InlineData(new[] { 1, 2, 3 }, 5)]
	public void GetRepeatableUniqueRandomElements_WithRnd_WhenSelectQuantityGeUniqueCount_ReturnsAllUniqueItemsShuffled(int[] items, int selectQuantity)
	{
		List<int> uniqueItems = items.Distinct().ToList();
		List<int> result = items.GetRepeatableUniqueRandomElements(new System.Random(42), selectQuantity).ToList();
		result.Count.ShouldBe(uniqueItems.Count);
		result.ShouldBeSubsetOf(uniqueItems);
	}

	// -------------------------------------------------------------------------
	// GenerateRepeatableRandomString
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(10, 5, "seed1")]
	[InlineData(20, 15, "seed2")]
	[InlineData(10, -1, "seed3")]
	public void GenerateRepeatableRandomString_WithSeed_RespectsLengthBounds(int maxLength, int minLength, string seed)
	{
		string result = GenerateRepeatableRandomString(maxLength, seed, minLength, cancellationToken: TestContext.Current.CancellationToken);
		result.Length.ShouldBeGreaterThanOrEqualTo(minLength == -1 ? maxLength : minLength);
		result.Length.ShouldBeLessThanOrEqualTo(maxLength);
	}

	[Theory]
	[InlineData(10, 5, "seed1")]
	[InlineData(20, 15, "seed2")]
	public void GenerateRepeatableRandomString_WithSeed_IsRepeatable(int maxLength, int minLength, string seed)
	{
		string result1 = GenerateRepeatableRandomString(maxLength, seed, minLength, cancellationToken: TestContext.Current.CancellationToken);
		string result2 = GenerateRepeatableRandomString(maxLength, seed, minLength, cancellationToken: TestContext.Current.CancellationToken);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(10, -1, 65, 90, "seed1")] // uppercase letters
	[InlineData(10, -1, 97, 122, "seed2")] // lowercase letters
	[InlineData(10, -1, 48, 57, "seed3")]  // numbers
	public void GenerateRepeatableRandomString_WithSeed_RespectsAsciiRange(int maxLength, int minLength, int lower, int upper, string seed)
	{
		string result = GenerateRepeatableRandomString(maxLength, seed, minLength, lower, upper, cancellationToken: TestContext.Current.CancellationToken);
		result.All(c => c >= lower && c <= upper).ShouldBeTrue();
	}

	[Fact]
	public void GenerateRepeatableRandomString_WithSeed_RespectsBlacklist()
	{
		HashSet<char> blacklist = ['a', 'e', 'i', 'o', 'u'];
		string result = GenerateRepeatableRandomString(100, "testSeed", blacklistedCharacters: blacklist, cancellationToken: TestContext.Current.CancellationToken);
		result.Any(blacklist.Contains).ShouldBeFalse();
	}

	[Theory]
	[InlineData(10, 5, "seed1")]
	[InlineData(20, 15, "seed2")]
	[InlineData(10, -1, "seed3")]
	public void GenerateRepeatableRandomString_WithRnd_RespectsLengthBounds(int maxLength, int minLength, string seed)
	{
		string result = GenerateRepeatableRandomString(maxLength, new System.Random(seed.GetHashCode()), minLength, cancellationToken: TestContext.Current.CancellationToken);
		result.Length.ShouldBeGreaterThanOrEqualTo(minLength == -1 ? maxLength : minLength);
		result.Length.ShouldBeLessThanOrEqualTo(maxLength);
	}

	[Theory]
	[InlineData(10, 5)]
	[InlineData(20, 15)]
	public void GenerateRepeatableRandomString_WithRnd_IsRepeatable(int maxLength, int minLength)
	{
		string result1 = GenerateRepeatableRandomString(maxLength, new System.Random(42), minLength, cancellationToken: TestContext.Current.CancellationToken);
		string result2 = GenerateRepeatableRandomString(maxLength, new System.Random(42), minLength, cancellationToken: TestContext.Current.CancellationToken);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(10, -1, 65, 90)] // uppercase letters
	[InlineData(10, -1, 97, 122)] // lowercase letters
	[InlineData(10, -1, 48, 57)]  // numbers
	public void GenerateRepeatableRandomString_WithRnd_RespectsAsciiRange(int maxLength, int minLength, int lower, int upper)
	{
		string result = GenerateRepeatableRandomString(maxLength, new System.Random(42), minLength, lower, upper, cancellationToken: TestContext.Current.CancellationToken);
		result.All(c => c >= lower && c <= upper).ShouldBeTrue();
	}

	[Fact]
	public void GenerateRepeatableRandomString_WithRnd_RespectsBlacklist()
	{
		HashSet<char> blacklist = ['a', 'e', 'i', 'o', 'u'];
		string result = GenerateRepeatableRandomString(100, new System.Random(42), blacklistedCharacters: blacklist, cancellationToken: TestContext.Current.CancellationToken);
		result.Any(blacklist.Contains).ShouldBeFalse();
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	public void GenerateRepeatableRandomString_WithSeed_WhenMaxLengthInvalid_ThrowsException(int maxLength)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => GenerateRepeatableRandomString(maxLength, "seed"));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	public void GenerateRepeatableRandomString_WithRnd_WhenMaxLengthInvalid_ThrowsException(int maxLength)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => GenerateRepeatableRandomString(maxLength, new System.Random(1)));
	}

	[Theory]
	[InlineData(-1, 126)]
	[InlineData(0, 128)]
	[InlineData(100, 50)]
	public void GenerateRepeatableRandomString_WithSeed_WhenAsciiBoundsInvalid_ThrowsException(int lower, int upper)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() =>
			GenerateRepeatableRandomString(10, "seed", lowerAsciiBound: lower, upperAsciiBound: upper));
		exception.Message.ShouldContain("Bounds must be between 0 and 127, and lowerBound must be less than upperBound");
		exception.ParamName.ShouldBe("upperAsciiBound");
	}

	[Theory]
	[InlineData(-1, 126)]
	[InlineData(0, 128)]
	[InlineData(100, 50)]
	public void GenerateRepeatableRandomString_WithRnd_WhenAsciiBoundsInvalid_ThrowsException(int lower, int upper)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() =>
			GenerateRepeatableRandomString(10, new System.Random(1), lowerAsciiBound: lower, upperAsciiBound: upper));
		exception.Message.ShouldContain("Bounds must be between 0 and 127, and lowerBound must be less than upperBound");
		exception.ParamName.ShouldBe("upperAsciiBound");
	}

	[Fact]
	public void GenerateRepeatableRandomString_WithSeed_WhenBlacklistContainsAllChars_ThrowsException()
	{
		HashSet<char> blacklist = new();
		for (int i = 65; i <= 90; i++) { blacklist.Add((char)i); }
		ArgumentException exception = Should.Throw<ArgumentException>(() =>
			GenerateRepeatableRandomString(10, "seed", lowerAsciiBound: 65, upperAsciiBound: 90, blacklistedCharacters: blacklist));
		exception.Message.ShouldContain("Black list contains all available values");
		exception.ParamName.ShouldBe("blacklistedCharacters");
	}

	[Fact]
	public void GenerateRepeatableRandomString_WithRnd_WhenBlacklistContainsAllChars_ThrowsException()
	{
		HashSet<char> blacklist = new();
		for (int i = 65; i <= 90; i++) { blacklist.Add((char)i); }
		ArgumentException exception = Should.Throw<ArgumentException>(() =>
			GenerateRepeatableRandomString(10, new System.Random(1), lowerAsciiBound: 65, upperAsciiBound: 90, blacklistedCharacters: blacklist));
		exception.Message.ShouldContain("Black list contains all available values");
		exception.ParamName.ShouldBe("blacklistedCharacters");
	}

	// -------------------------------------------------------------------------
	// GenerateRepeatableRandomStrings
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(5, 10, "seed1")]
	[InlineData(10, 20, "seed2")]
	public void GenerateRepeatableRandomStrings_WithSeed_GeneratesCorrectCount(int count, int length, string seed)
	{
		List<string> results = GenerateRepeatableRandomStrings(count, length, seed, cancellationToken: TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x.Length == length);
	}

	[Theory]
	[InlineData(5, 10, "seed1")]
	[InlineData(10, 20, "seed2")]
	public void GenerateRepeatableRandomStrings_WithSeed_IsRepeatable(int count, int length, string seed)
	{
		List<string> results1 = GenerateRepeatableRandomStrings(count, length, seed, cancellationToken: TestContext.Current.CancellationToken).ToList();
		List<string> results2 = GenerateRepeatableRandomStrings(count, length, seed, cancellationToken: TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	[Theory]
	[InlineData(5, 10)]
	[InlineData(10, 20)]
	public void GenerateRepeatableRandomStrings_WithRnd_GeneratesCorrectCount(int count, int length)
	{
		List<string> results = GenerateRepeatableRandomStrings(count, length, new System.Random(42), cancellationToken: TestContext.Current.CancellationToken).ToList();
		results.Count.ShouldBe(count);
		results.ShouldAllBe(x => x.Length == length);
	}

	[Theory]
	[InlineData(5, 10)]
	[InlineData(10, 20)]
	public void GenerateRepeatableRandomStrings_WithRnd_IsRepeatable(int count, int length)
	{
		List<string> results1 = GenerateRepeatableRandomStrings(count, length, new System.Random(42), cancellationToken: TestContext.Current.CancellationToken).ToList();
		List<string> results2 = GenerateRepeatableRandomStrings(count, length, new System.Random(42), cancellationToken: TestContext.Current.CancellationToken).ToList();
		results1.SequenceEqual(results2).ShouldBeTrue();
	}

	// -------------------------------------------------------------------------
	// GenerateRepeatableRandomStringByCharSet
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(10, "seed1")]
	[InlineData(20, "seed2")]
	public void GenerateRepeatableRandomStringByCharSet_WithSeed_UsesProvidedCharSet(int length, string seed)
	{
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];
		string result = GenerateRepeatableRandomStringByCharSet(length, seed, charSet, TestContext.Current.CancellationToken);
		result.Length.ShouldBe(length);
		result.All(charSet.Contains).ShouldBeTrue();
	}

	[Theory]
	[InlineData(10, "seed1")]
	[InlineData(20, "seed2")]
	public void GenerateRepeatableRandomStringByCharSet_WithSeed_IsRepeatable(int length, string seed)
	{
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];
		string result1 = GenerateRepeatableRandomStringByCharSet(length, seed, charSet, TestContext.Current.CancellationToken);
		string result2 = GenerateRepeatableRandomStringByCharSet(length, seed, charSet, TestContext.Current.CancellationToken);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(10, "seed1")]
	[InlineData(20, "seed2")]
	public void GenerateRepeatableRandomStringByCharSet_WithSeed_UsesDefaultCharSetWhenNullProvided(int length, string seed)
	{
		string result = GenerateRepeatableRandomStringByCharSet(length, seed, cancellationToken: TestContext.Current.CancellationToken);
		result.Length.ShouldBe(length);
		result.All(DefaultCharSet.Contains).ShouldBeTrue();
	}

	[Theory]
	[InlineData(10)]
	[InlineData(20)]
	public void GenerateRepeatableRandomStringByCharSet_WithRnd_UsesProvidedCharSet(int length)
	{
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];
		string result = GenerateRepeatableRandomStringByCharSet(length, new System.Random(42), charSet, TestContext.Current.CancellationToken);
		result.Length.ShouldBe(length);
		result.All(charSet.Contains).ShouldBeTrue();
	}

	[Theory]
	[InlineData(10)]
	[InlineData(20)]
	public void GenerateRepeatableRandomStringByCharSet_WithRnd_IsRepeatable(int length)
	{
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];
		string result1 = GenerateRepeatableRandomStringByCharSet(length, new System.Random(42), charSet, TestContext.Current.CancellationToken);
		string result2 = GenerateRepeatableRandomStringByCharSet(length, new System.Random(42), charSet, TestContext.Current.CancellationToken);
		result1.ShouldBe(result2);
	}

	[Theory]
	[InlineData(10)]
	[InlineData(20)]
	public void GenerateRepeatableRandomStringByCharSet_WithRnd_UsesDefaultCharSetWhenNullProvided(int length)
	{
		string result = GenerateRepeatableRandomStringByCharSet(length, new System.Random(42), cancellationToken: TestContext.Current.CancellationToken);
		result.Length.ShouldBe(length);
		result.All(DefaultCharSet.Contains).ShouldBeTrue();
	}
}
