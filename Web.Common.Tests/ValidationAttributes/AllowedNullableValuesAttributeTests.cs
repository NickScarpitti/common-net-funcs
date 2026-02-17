using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class AllowedNullableValuesAttributeTests : ValidationTestBase
{
	private enum ETest
	{
		Value1 = 1,
		Value2 = 2,
		Value3 = 3
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithAllowedValues_ShouldSetProperties(bool allowNull)
	{
		// Arrange
		object[] values = { 1, 2, 3 };

		// Act
		AllowedNullableValuesAttribute attribute = new(allowNull, values);

		// Assert
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);
		if (allowNull)
		{
			result.ShouldBeNull();
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("cannot be null");
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithEnum_ShouldSetProperties(bool allowNull)
	{
		// Act
		AllowedNullableValuesAttribute attribute = new(allowNull, typeof(ETest));

		// Assert
		attribute.GetValidationResult(ETest.Value1, DummyValidationContext).ShouldBe(ValidationResult.Success);
		attribute.GetValidationResult(1, DummyValidationContext).ShouldBe(ValidationResult.Success);

		ValidationResult? nullResult = attribute.GetValidationResult(null, DummyValidationContext);
		if (allowNull)
		{
			nullResult.ShouldBe(ValidationResult.Success);
		}
		else
		{
			nullResult.ShouldNotBeNull();
		}
	}

	[Fact]
	public void Constructor_WithNonEnum_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AllowedNullableValuesAttribute(typeof(string)));
	}

	[Theory]
	[InlineData(4)]
	[InlineData(99)]
	public void IsValid_WithInvalidEnumValue_ShouldReturnError(int invalidValue)
	{
		// Arrange
		AllowedNullableValuesAttribute attribute = new(typeof(ETest));

		// Act
		ValidationResult? result = attribute.GetValidationResult(invalidValue, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("must be one of the following values");
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, true)]
	[InlineData(4, false)]
	public void IsValid_WithAllowedValues_ShouldValidateCorrectly(object value, bool shouldBeValid)
	{
		// Arrange
		AllowedNullableValuesAttribute attribute = new(1, 2, 3);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, DummyValidationContext);

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must be one of the following values");
		}
	}

	[Fact]
	public void Constructor_WithNullAllowedValues_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new AllowedNullableValuesAttribute(true, (object[])null!));
	}

	[Fact]
	public void Constructor_SimpleWithNullAllowedValues_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new AllowedNullableValuesAttribute((object[])null!));
	}

	[Fact]
	public void Constructor_SingleParamEnum_WithNonEnum_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AllowedNullableValuesAttribute(typeof(int)));
	}

	[Fact]
	public void IsValid_EnumWithUndefinedEnumValue_ShouldReturnError()
	{
		// Arrange
		AllowedNullableValuesAttribute attribute = new(typeof(ETest));

		// Act - cast 99 to the enum type which creates an undefined enum value
		ValidationResult? result = attribute.GetValidationResult((ETest)99, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("must be one of the following values");
		result.ErrorMessage?.ShouldContain("Value1");
	}

	[Fact]
	public void IsValid_EnumWithDefinedEnumValue_ShouldReturnSuccess()
	{
		// Arrange
		AllowedNullableValuesAttribute attribute = new(typeof(ETest));

		// Act - pass actual enum value
		ValidationResult? result = attribute.GetValidationResult(ETest.Value2, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonAllowedValue_WithoutEnum_ShouldIncludeNullInErrorMessage()
	{
		// Arrange - create attribute with null as one of the allowed values
		AllowedNullableValuesAttribute attribute = new(true, [1, 2, null!]);

		// Act
		ValidationResult? result = attribute.GetValidationResult(999, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("1, 2, null");
	}

	[Fact]
	public void IsValid_EnumAttribute_WithNonEnumNonNumericValue_ShouldReturnError()
	{
		// Arrange - create enum-based attribute
		AllowedNullableValuesAttribute attribute = new(typeof(ETest));

		// Act - pass a string value (not enum, not numeric)
		ValidationResult? result = attribute.GetValidationResult("invalid", CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("must be one of the following values");
	}
}
