using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListRangeAttributeTests : ValidationTestBase
{
	[Theory]
	[InlineData(1, 0)]
	[InlineData(10, 5)]
	public void Constructor_WithInvalidRange_ShouldThrow(int minimum, int maximum)
	{
		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => new ListRangeAttribute(minimum, maximum));
	}

	[Theory]
	[InlineData(1.0, 0.0)]
	[InlineData(10.5, 5.2)]
	public void Constructor_DoubleOverload_WithInvalidRange_ShouldThrow(double minimum, double maximum)
	{
		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => new ListRangeAttribute(minimum, maximum));
	}

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10);

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(new[] { 1, 5, 10 }, 1, 10, true)]
	[InlineData(new[] { 0, 5, 11 }, 1, 10, false)]
	public void IsValid_WithIntegerList_ShouldValidateCorrectly(int[] values, int min, int max, bool shouldBeValid)
	{
		// Arrange
		ListRangeAttribute attribute = new(min, max);
		List<int> list = values.ToList();

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must be between");
		}
	}

	[Theory]
	[InlineData(new[] { 1.0, 5.5, 10.0 }, 1.0, 10.0, true)]
	[InlineData(new[] { 0.5, 5.5, 10.5 }, 1.0, 10.0, false)]
	public void IsValid_WithDoubleList_ShouldValidateCorrectly(double[] values, double min, double max, bool shouldBeValid)
	{
		// Arrange
		ListRangeAttribute attribute = new(min, max);
		List<double> list = values.ToList();

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must be between");
		}
	}

	[Fact]
	public void Constructor_WithTypeAndStringRange_ShouldSetProperties()
	{
		// Arrange & Act
		ListRangeAttribute attribute = new(typeof(int), "1", "10");

		// Assert
		attribute.Minimum.ShouldBe("1");
		attribute.Maximum.ShouldBe("10");
		attribute.OperandType.ShouldBe(typeof(int));
	}

	[Fact]
	public void IsValid_WithTypeConstructor_ShouldConvertAndValidate()
	{
		// Arrange
		ListRangeAttribute attribute = new(typeof(int), "1", "10");
		List<object> list = [5, 10];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonEnumerable_ShouldThrow()
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10);

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult("notAList", DummyValidationContext));
	}

	[Theory]
	[InlineData(new[] { 2, 5, 9 }, 1, 10, true, false, true)]   // Min exclusive: 2 > 1, max inclusive: 9 <= 10
	[InlineData(new[] { 1, 5, 9 }, 1, 10, false, true, true)]   // Min inclusive: 1 >= 1, max exclusive: 9 < 10
	[InlineData(new[] { 1 }, 1, 10, true, false, false)]        // Minimum is exclusive, value equals minimum (1 is NOT > 1)
	[InlineData(new[] { 10 }, 1, 10, false, true, false)]       // Maximum is exclusive, value equals maximum (10 is NOT < 10)
	public void IsValid_WithExclusiveBounds_ShouldValidateCorrectly(int[] values, int min, int max, bool minExclusive, bool maxExclusive, bool shouldBeValid)
	{
		// Arrange
		ListRangeAttribute attribute = new(min, max)
		{
			MinimumIsExclusive = minExclusive,
			MaximumIsExclusive = maxExclusive
		};
		List<int> list = values.ToList();

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must be between");
		}
	}

	[Fact]
	public void Constructor_TypeBased_WithInvalidMinMax_ShouldThrow()
	{
		// Arrange & Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() =>
		{
			ListRangeAttribute attribute = new(typeof(int), "10", "1");
			// Trigger SetupConversion
			attribute.GetValidationResult(new List<int> { 5 }, DummyValidationContext);
		});
	}

	[Theory]
	[InlineData(true, false)]  // minExclusive, equalBounds
	[InlineData(false, true)]  // maxExclusive, equalBounds
	public void Constructor_TypeBased_WithEqualBoundsAndExclusive_ShouldThrow(bool minExclusive, bool maxExclusive)
	{
		// Arrange & Act & Assert
		Should.Throw<InvalidOperationException>(() =>
		{
			ListRangeAttribute attribute = new(typeof(int), "5", "5")
			{
				MinimumIsExclusive = minExclusive,
				MaximumIsExclusive = maxExclusive
			};
			attribute.GetValidationResult(new List<int> { 5 }, DummyValidationContext);
		});
	}

	[Fact]
	public void IsValid_WithInvalidConversionFormat_ShouldThrow()
	{
		// Arrange
		ListRangeAttribute attribute = new(typeof(int), "1", "10");
		List<object> list = ["invalid"];

		// Act & Assert
		// TypeConverter.ConvertFrom wraps FormatException in ArgumentException
		Should.Throw<ArgumentException>(() => attribute.GetValidationResult(list, CreateValidationContext("TestProperty")));
	}

	[Fact]
	public void IsValid_WithInvalidCast_ShouldReturnError()
	{
		// Arrange
		ListRangeAttribute attribute = new(typeof(DateTime), DateTime.Now.ToString(), DateTime.Now.AddDays(1).ToString());
		List<object> list = [new object()]; // Cannot be cast to DateTime

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldBe("Not supported");
	}

	[Fact]
	public void Constructor_TypeBased_WithNonIComparableType_ShouldThrow()
	{
		// Arrange & Act & Assert
		Should.Throw<InvalidOperationException>(() =>
		{
			ListRangeAttribute attribute = new(typeof(object), "a", "b");
			attribute.GetValidationResult(new List<object> { new() }, DummyValidationContext);
		});
	}

	[Theory]
	[InlineData(true, true)]   // parse invariant, convert invariant
	[InlineData(false, false)] // parse current, convert current
	public void CultureProperties_ShouldAffectParsing(bool parseInvariant, bool convertInvariant)
	{
		// Arrange
		ListRangeAttribute attribute = new(typeof(decimal), "1.5", "10.5")
		{
			ParseLimitsInInvariantCulture = parseInvariant,
			ConvertValueInInvariantCulture = convertInvariant
		};
		List<decimal> list = [5.5m];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void FormatErrorMessage_ShouldReturnFormattedMessage()
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10)
		{
			ErrorMessage = "The field {0} must be between {1} and {2}."
		};

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("TestProperty");
		message.ShouldContain("1");
		message.ShouldContain("10");
	}

	[Fact]
	public void IsValid_WithFormatException_ShouldThrowArgumentException()
	{
		// Arrange - FormatException is wrapped in ArgumentException by TypeConverter
		ListRangeAttribute attribute = new(typeof(int), "1", "10");
		List<string> list = ["not_a_number"]; // This will cause ArgumentException (wrapping FormatException)

		// Act & Assert - ArgumentException is not caught, so it propagates
		Should.Throw<ArgumentException>(() => attribute.GetValidationResult(list, CreateValidationContext("TestProperty")));
	}

	[Fact]
	public void IsValid_WithNotSupportedException_ShouldReturnValidationError()
	{
		// Arrange - Double to int TypeConverter throws NotSupportedException for certain conversions
		ListRangeAttribute attribute = new(typeof(int), "1", "10");
		// Create a list with a value that will cause NotSupportedException during int conversion
		List<object> list = [3.14159]; // Double to int conversion triggers NotSupportedException

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldBe("Not supported");
	}

	[Fact]
	public void IsValid_WithNullItemInCollection_ShouldHandleGracefully()
	{
		// Arrange - test null handling in foreach loop
		ListRangeAttribute attribute = new(1, 10);
		List<int?> list = [1, null, 5]; // Nullable int list

		// Act - null conversion will be attempted
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert - Either succeeds or returns error depending on null handling
		// Just ensure it doesn't crash
		result.ShouldNotBeNull();
	}

	[Fact]
	public void IsValid_WithEmptyCollection_ShouldReturnSuccess()
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10);
		List<int> list = [];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithCollectionHavingNullMemberName_ShouldUseEmptyString()
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10);
		List<int> list = [0]; // Out of range to trigger error
		ValidationContext context = new(new object()) { MemberName = null };

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, context);

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("must be between");
	}

	[Theory]
	[InlineData(true, "Invalid format")]   // int constructor, format exception
	[InlineData(false, "Invalid cast")]    // int constructor, cast exception
	public void IsValid_IntConstructor_WithExceptions_ShouldReturnValidationError(bool isFormatException, string expectedError)
	{
		// Arrange
		ListRangeAttribute attribute = new(1, 10);
		List<object> list = isFormatException ? ["not_a_number"] : [new object()];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldBe(expectedError);
	}

	[Theory]
	[InlineData(true, "Invalid format")]   // double constructor, format exception
	[InlineData(false, "Invalid cast")]    // double constructor, cast exception
	public void IsValid_DoubleConstructor_WithExceptions_ShouldReturnValidationError(bool isFormatException, string expectedError)
	{
		// Arrange
		ListRangeAttribute attribute = new(1.0, 10.0);
		List<object> list = isFormatException ? ["not_a_number"] : [new object()];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldBe(expectedError);
	}

	[Theory]
	[InlineData(true, true)]   // invariant, type match
	[InlineData(true, false)]  // invariant, type mismatch
	[InlineData(false, true)]  // current culture, type match
	[InlineData(false, false)] // current culture, type mismatch
	public void ConvertValueInInvariantCulture_WithTypeVariations_ShouldWork(bool invariant, bool typeMatch)
	{
		// Arrange
		ListRangeAttribute attribute = typeMatch
			? new(typeof(decimal), "1.5", "10.5") { ConvertValueInInvariantCulture = invariant }
			: new(typeof(int), "1", "10") { ConvertValueInInvariantCulture = invariant };
		List<object> list = typeMatch ? [2.5m, 7.5m] : ["5", "7"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(5, 5)]      // int equal bounds
	[InlineData(5.5, 5.5)]  // double equal bounds
	public void Constructor_WithEqualBounds_ShouldWork(object min, object max)
	{
		// Arrange & Act
		ListRangeAttribute attribute = min is int intMin
			? new ListRangeAttribute(intMin, (int)max)
			: new ListRangeAttribute((double)min, (double)max);
		List<object> list = [min];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}
}
