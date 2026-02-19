using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListStringLengthAttributeTests : ValidationTestBase
{
	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	public void Constructor_WithNegativeMaxLength_ShouldThrow(int maxLength)
	{
		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => new ListStringLengthAttribute(maxLength));
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	public void MinimumLength_SetToNegative_ShouldThrow(int minLength)
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10);

		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => attribute.MinimumLength = minLength);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-10)]
	public void MaximumLength_SetToNegative_ShouldThrow(int maxLength)
	{
		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => new ListStringLengthAttribute(maxLength));
	}

	[Theory]
	[InlineData(5, 10)]
	[InlineData(10, 5)]
	public void Constructor_WithInvalidMinMaxLength_ShouldThrow(int minLength, int maxLength)
	{
		// Arrange
		ListStringLengthAttribute attribute = new(maxLength)
		{
			MinimumLength = minLength
		};

		// Act & Assert
		if (minLength > maxLength)
		{
			Should.Throw<ArgumentOutOfRangeException>(() => attribute.GetValidationResult(new List<string> { "test" }, DummyValidationContext));
		}
		else
		{
			ValidationResult? result = attribute.GetValidationResult(new List<string> { "test" }, DummyValidationContext);
			result?.ErrorMessage?.ShouldContain("must be between");
		}
	}

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10);

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(new[] { "12345", "123", "12345" }, 3, 5, true)]
	[InlineData(new[] { "1", "123456", "12" }, 2, 5, false)]
	public void IsValid_WithStringList_ShouldValidateCorrectly(string[] values, int minLength, int maxLength, bool shouldBeValid)
	{
		// Arrange
		ListStringLengthAttribute attribute = new(maxLength)
		{
			MinimumLength = minLength
		};
		List<string> list = values.ToList();

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
	public void IsValid_WithNonCollection_ShouldThrow()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10);
		const string nonCollection = "not a collection";

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.IsValid(nonCollection));
	}

	[Fact]
	public void FormatErrorMessage_ShouldIncludeMinMaxLengths()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10)
		{
			MinimumLength = 5
		};

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("5");
		message.ShouldContain("10");
	}

	[Fact]
	public void IsValid_WithNullItemsInList_ShouldSkipNulls()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10)
		{
			MinimumLength = 3
		};
		List<string?> list = ["test", null, "valid"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void FormatErrorMessage_WithZeroMinLength_ShouldUseMaxLengthMessage()
	{
		// Arrange - MinimumLength == 0 should trigger the else branch
		ListStringLengthAttribute attribute = new(10)
		{
			MinimumLength = 0
		};

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("Maximum length");
		message.ShouldContain("10");
	}

	[Fact]
	public void FormatErrorMessage_WithCustomErrorMessage_ShouldUseCustomMessage()
	{
		// Arrange - When ErrorMessage is set, should trigger else branch
		ListStringLengthAttribute attribute = new(10)
		{
			MinimumLength = 5,
			ErrorMessage = "Custom error for {0}"
		};

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("Maximum length");
		message.ShouldContain("10");
	}

	[Fact]
	public void IsValid_WithEmptyCollection_ShouldReturnSuccess()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(10)
		{
			MinimumLength = 3
		};
		List<string> list = [];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNullMemberName_ShouldUseEmptyString()
	{
		// Arrange
		ListStringLengthAttribute attribute = new(5)
		{
			MinimumLength = 3
		};
		List<string> list = ["toolong123"]; // Exceeds max length
		ValidationContext context = new(new object()) { MemberName = null };

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, context);

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("must be between");
	}
}
