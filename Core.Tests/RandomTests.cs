using FakeItEasy;
using static CommonNetFuncs.Core.MathHelpers;
using static CommonNetFuncs.Core.Random;

namespace Core.Tests;

// Enums for test consolidation (must be public for xUnit)
public enum NumericType { Int, Double, Decimal }
public enum ShuffleMethodType { ListInPlace, IEnumerable, IList, Array, Span }
public enum StringLengthValidation { InvalidMaxLength, InvalidLengthRange }

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
	[InlineData(NumericType.Int, 0)]
	[InlineData(NumericType.Int, -1)]
	[InlineData(NumericType.Int, -10)]
	[InlineData(NumericType.Double, 0)]
	[InlineData(NumericType.Double, -1)]
	[InlineData(NumericType.Decimal, 0)]
	[InlineData(NumericType.Decimal, -1)]
	public void GetRandomMultiple_WhenNumberToGenerateIsZeroOrNegative_ThrowsException(NumericType type, int numberToGenerate)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = type switch
		{
			NumericType.Int => Should.Throw<ArgumentOutOfRangeException>(() => GetRandomInts(numberToGenerate).ToList()),
			NumericType.Double => Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDoubles(numberToGenerate).ToList()),
			NumericType.Decimal => Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDecimals(numberToGenerate).ToList()),
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
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
	[InlineData(NumericType.Double, 0)]
	[InlineData(NumericType.Double, -1)]
	[InlineData(NumericType.Double, -5)]
	[InlineData(NumericType.Decimal, 0)]
	[InlineData(NumericType.Decimal, -1)]
	[InlineData(NumericType.Decimal, -5)]
	public void GetRandomFloatingPoint_WithInvalidPrecision_ThrowsException(NumericType type, int precision)
	{
		// Act & Assert
		ArgumentOutOfRangeException exception = type switch
		{
			NumericType.Double => Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDouble(precision)),
			NumericType.Decimal => Should.Throw<ArgumentOutOfRangeException>(() => GetRandomDecimal(precision)),
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
		exception.Message.ShouldContain("decimalPlaces must be greater than 0");
		exception.ParamName.ShouldBe("decimalPlaces");
	}

	[Theory]
	[InlineData(NumericType.Double, 20, 15)]
	[InlineData(NumericType.Decimal, 35, 28)]
	public void GetRandomFloatingPoint_WithPrecisionGreaterThanMax_CapsAtMax(NumericType type, int requestedPrecision, int maxPrecision)
	{
		// Act & Assert
		switch (type)
		{
			case NumericType.Double:
				double doubleResult = GetRandomDouble(requestedPrecision);
				doubleResult.ShouldBeGreaterThanOrEqualTo(0);
				doubleResult.ShouldBeLessThan(1);
				doubleResult.GetPrecision().ShouldBeLessThanOrEqualTo(maxPrecision);
				break;
			case NumericType.Decimal:
				decimal decimalResult = GetRandomDecimal(requestedPrecision);
				decimalResult.ShouldBeGreaterThanOrEqualTo(0);
				decimalResult.ShouldBeLessThan(1);
				decimalResult.GetPrecision().ShouldBeLessThanOrEqualTo(maxPrecision);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(type));
		}
	}

	[Theory]
	[InlineData(NumericType.Double)]
	[InlineData(NumericType.Decimal)]
	public void GetRandomFloatingPoint_DefaultPrecision_ReturnsCorrectRange(NumericType type)
	{
		switch (type)
		{
			case NumericType.Double:
				double doubleResult = GetRandomDouble();
				doubleResult.ShouldBeGreaterThanOrEqualTo(0);
				doubleResult.ShouldBeLessThan(1);
				break;
			case NumericType.Decimal:
				decimal decimalResult = GetRandomDecimal();
				decimalResult.ShouldBeGreaterThanOrEqualTo(0);
				decimalResult.ShouldBeLessThan(1);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(type));
		}
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

	[Theory]
	[InlineData(ShuffleMethodType.ListInPlace)]
	[InlineData(ShuffleMethodType.IEnumerable)]
	[InlineData(ShuffleMethodType.IList)]
	[InlineData(ShuffleMethodType.Array)]
	public void Shuffle_WithEmptyCollection_ReturnsEmpty(ShuffleMethodType methodType)
	{
		// Arrange & Act & Assert
		switch (methodType)
		{
			case ShuffleMethodType.ListInPlace:
				List<int> emptyList = new();
				IList<int> result = emptyList.ShuffleListInPlace(cancellationToken: TestContext.Current.CancellationToken);
				result.ShouldBeEmpty();
				break;
			case ShuffleMethodType.IEnumerable:
				List<int> emptyEnumerable = new();
				IEnumerable<int> enumerableResult = emptyEnumerable.Shuffle();
				enumerableResult.ShouldBeEmpty();
				break;
			case ShuffleMethodType.IList:
				List<int> emptyIList = new();
				List<int> iListResult = emptyIList.Shuffle();
				iListResult.ShouldBeEmpty();
				break;
			case ShuffleMethodType.Array:
				int[] emptyArray = Array.Empty<int>();
				emptyArray.Shuffle();
				emptyArray.ShouldBeEmpty();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(methodType));
		}
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
	public void GenerateRandomStringByCharSet_UsesProvidedOrDefaultCharSet(int length)
	{
		// Arrange
		HashSet<char> charSet = ['A', 'B', 'C', '1', '2', '3'];

		// Act - with provided charset
		string resultWithCharSet = GenerateRandomStringByCharSet(length, charSet, TestContext.Current.CancellationToken);

		// Assert
		resultWithCharSet.Length.ShouldBe(length);
		resultWithCharSet.All(charSet.Contains).ShouldBeTrue();

		// Act - with default charset
		string resultDefault = GenerateRandomStringByCharSet(length, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		resultDefault.Length.ShouldBe(length);
		resultDefault.All(DefaultCharSet.Contains).ShouldBeTrue();
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
	[InlineData(StringLengthValidation.InvalidMaxLength, -1)]
	[InlineData(StringLengthValidation.InvalidMaxLength, 0)]
	[InlineData(StringLengthValidation.InvalidLengthRange, 10)]
	[InlineData(StringLengthValidation.InvalidLengthRange, 0)]
	public void GenerateRandomString_WithInvalidLength_ThrowsException(StringLengthValidation validationType, int value)
	{
		switch (validationType)
		{
			case StringLengthValidation.InvalidMaxLength:
				Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(value));
				break;
			case StringLengthValidation.InvalidLengthRange:
				// minLength > maxLength
				Should.Throw<ArgumentOutOfRangeException>(() => GenerateRandomString(maxLength: value, minLength: value > 0 ? value + 5 : 0));
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(validationType));
		}
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
