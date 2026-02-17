using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListRegularExpressionAttributeTests : ValidationTestBase
{
	[Fact]
	public void Constructor_WithNullPattern_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new ListRegularExpressionAttribute(null!));
	}

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$");

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(new[] { "123", "456", "789" }, @"^\d+$", true)]
	[InlineData(new[] { "abc", "123", "def" }, @"^\d+$", false)]
	public void IsValid_WithStringList_ShouldValidateCorrectly(string[] values, string pattern, bool shouldBeValid)
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(pattern);
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
			result.ErrorMessage?.ShouldContain("does not match the required pattern");
		}
	}

	[Fact]
	public void IsValid_WithMatchTimeout_ShouldRespectTimeout()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new("^(a+)+$")
		{
			MatchTimeoutInMilliseconds = 100
		};
		List<string> list = new() { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!" }; // Catastrophic backtracking case

		// Act & Assert
		Should.Throw<RegexMatchTimeoutException>(() => attribute.GetValidationResult(list, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithNoTimeout_ShouldNotTimeout()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$")
		{
			MatchTimeoutInMilliseconds = -1 // No timeout
		};
		List<string> list = ["123", "456"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void MatchTimeout_Property_ShouldReturnTimeSpan()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$")
		{
			MatchTimeoutInMilliseconds = 500
		};

		// Act
		TimeSpan timeout = attribute.MatchTimeout;

		// Assert
		timeout.TotalMilliseconds.ShouldBe(500);
	}

	[Fact]
	public void IsValid_WithNonStringEnumerable_ShouldConvertAndValidate()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$");
		List<object> list = [123, 456, 789];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonEnumerable_ShouldThrow()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$");

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult("notAList", DummyValidationContext));
	}

	[Fact]
	public void FormatErrorMessage_ShouldReturnFormattedMessage()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$")
		{
			ErrorMessage = "The field {0} must match the pattern {1}."
		};

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("TestProperty");
		message.ShouldContain(@"^\d+$");
	}

	[Fact]
	public void SetupRegex_WithEmptyPattern_ShouldThrow()
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new("test");
		// Use reflection to set Pattern to empty (since it's readonly in constructor)
		typeof(ListRegularExpressionAttribute).GetField("<Pattern>k__BackingField",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(attribute, string.Empty);

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => attribute.GetValidationResult(new List<string> { "test" }, DummyValidationContext));
	}

	[Theory]
	[InlineData(new[] { "123", null, "456" }, true)]  // null is skipped, 123 and 456 match pattern
	[InlineData(new[] { "", "test" }, false)]        // empty is skipped, "test" doesn't match
	public void IsValid_WithNullOrEmptyItems_ShouldSkipThem(string?[] values, bool shouldBeValid)
	{
		// Arrange
		ListRegularExpressionAttribute attribute = new(@"^\d+$");
		List<string?> list = values.ToList();

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
		}
	}

	[Fact]
	public void IsValid_WithExactMatch_ShouldBreakEarlyOnMatch()
	{
		// Arrange - test to ensure the break statement in the match loop is executed
		ListRegularExpressionAttribute attribute = new(@"^test$");
		List<string> list = ["test", "test2"]; // First matches exactly, second doesn't

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert - should fail on second item
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("test2");
	}

	[Fact]
	public void IsValid_WithPartialMatch_ShouldReturnError()
	{
		// Arrange - test pattern that partially matches but not exact
		ListRegularExpressionAttribute attribute = new(@"^\d{3}$"); // Exactly 3 digits
		List<string> list = ["12345"]; // 5 digits - partial match but not exact

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage?.ShouldContain("does not match");
	}
}
