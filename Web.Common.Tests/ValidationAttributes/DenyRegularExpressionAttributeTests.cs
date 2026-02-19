using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using CommonNetFuncs.Web.Common.ValidationAttributes;


namespace Web.Common.Tests.ValidationAttributes;

public enum DenyRegexValidationCase
{
	Null,
	Empty
}

public sealed class DenyRegularExpressionAttributeTests : ValidationTestBase
{
	[Fact]
	public void Constructor_WithNullPattern_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new DenyRegularExpressionAttribute(null!));
	}

	[Fact]
	public void Constructor_WithValidPattern_ShouldSetProperties()
	{
		// Arrange
		const string pattern = "^admin.*";

		// Act
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Assert
		attribute.Pattern.ShouldBe(pattern);
		attribute.MatchTimeoutInMilliseconds.ShouldBe(2000); // Default timeout
	}

	[Theory]
	[InlineData(DenyRegexValidationCase.Null)]
	[InlineData(DenyRegexValidationCase.Empty)]
	public void IsValid_WithNullOrEmpty_ShouldReturnSuccess(DenyRegexValidationCase testCase)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^admin.*");
		string? value = testCase == DenyRegexValidationCase.Null ? null : string.Empty;

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData("user123", "^admin.*", true)]        // Does not match pattern - valid
	[InlineData("admin123", "^admin.*", false)]      // Matches pattern - invalid
	[InlineData("Admin123", "^admin.*", true)]       // Does not match (case sensitive) - valid
	[InlineData("test@example.com", @".*@test\.com$", true)]  // Does not match - valid
	[InlineData("user@test.com", @".*@test\.com$", false)]    // Matches - invalid
	public void IsValid_WithStringValue_ShouldValidateCorrectly(string value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Username"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must not match the pattern");
		}
	}

	[Theory]
	[InlineData("normaluser", "^(admin|root|superuser)$", true)]
	[InlineData("admin", "^(admin|root|superuser)$", false)]
	[InlineData("root", "^(admin|root|superuser)$", false)]
	[InlineData("superuser", "^(admin|root|superuser)$", false)]
	public void IsValid_WithMultiplePatterns_ShouldValidateCorrectly(string value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Username"));

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

	[Theory]
	[InlineData("<script>", "<.*>", false)]           // HTML tags - should fail
	[InlineData("hello world", "<.*>", true)]         // No HTML tags - should pass
	[InlineData("test<tag>", "<.*>", false)]          // Contains tag - should fail
	public void IsValid_WithHtmlPattern_ShouldValidateCorrectly(string value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Content"));

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
	public void IsValid_WithCustomErrorMessage_ShouldUseCustomMessage()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^admin.*")
		{
			ErrorMessage = "Custom error: {0} cannot start with admin"
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult("admin123", CreateValidationContext("Username"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("Custom error");
		result.ErrorMessage.ShouldContain("Username");
	}

	[Fact]
	public void IsValid_WithMatchTimeout_ShouldRespectTimeout()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^(a+)+$")
		{
			MatchTimeoutInMilliseconds = 100
		};
		const string value = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!"; // Catastrophic backtracking case

		// Act & Assert
		Should.Throw<RegexMatchTimeoutException>(() => attribute.GetValidationResult(value, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithNoTimeout_ShouldNotTimeout()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(@"^\d+$")
		{
			MatchTimeoutInMilliseconds = -1 // No timeout
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult("abc123", DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success); // Pattern doesn't match, so it's valid
	}

	[Theory]
	[InlineData("123", @"^\d+$", false)]              // All digits match pattern - invalid
	[InlineData("abc", @"^\d+$", true)]               // No digits match pattern - valid
	[InlineData("123abc", @"^\d+$", true)]            // Partial match only - valid (requires exact match)
	public void IsValid_WithNumericPattern_RequiresExactMatch(string value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Value"));

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
	public void FormatErrorMessage_WithDefaultMessage_ShouldReturnFormattedMessage()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^test.*");

		// Act
		string message = attribute.FormatErrorMessage("TestField");

		// Assert
		message.ShouldContain("TestField");
		message.ShouldContain("must not match the pattern");
		message.ShouldContain("^test.*");
	}

	[Fact]
	public void FormatErrorMessage_WithCustomMessage_ShouldUseCustomFormat()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^test.*")
		{
			ErrorMessage = "{0} is not allowed to match {1}"
		};

		// Act
		string message = attribute.FormatErrorMessage("MyField");

		// Assert
		message.ShouldContain("MyField");
		message.ShouldContain("is not allowed to match");
	}

	[Theory]
	[InlineData(123, @"^\d+$", false)]                // Integer converts to "123" - matches
	[InlineData(3.14, @"^\d+$", true)]                // Double converts to "3.14" - doesn't match exactly
	[InlineData(true, "^True$", false)]              // Boolean converts to "True" - matches
	public void IsValid_WithNonStringTypes_ShouldConvertAndValidate(object value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

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
		}
	}

	[Theory]
	[InlineData("test!", "[!@#$%^&*]", false)]       // Contains special char - invalid
	[InlineData("test", "[!@#$%^&*]", true)]         // No special chars - valid (no match)
	[InlineData("test@", "[!@#$%^&*]", false)]       // Contains @ - invalid
	public void IsValid_WithSpecialCharacterPattern_ShouldValidateCorrectly(string value, string pattern, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Input"));

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
	public void IsValid_ValidationContext_ShouldIncludeMemberName()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("^forbidden$");
		ValidationContext context = CreateValidationContext("TestProperty");

		// Act
		ValidationResult? result = attribute.GetValidationResult("forbidden", context);

		// Assert
		result.ShouldNotBeNull();
		result.MemberNames.ShouldContain("TestProperty");
	}

	[Fact]
	public void MatchTimeout_Property_ShouldReturnCorrectTimeSpan()
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new("test")
		{
			MatchTimeoutInMilliseconds = 5000
		};

		// Act & Assert
		attribute.MatchTimeout.ShouldBe(TimeSpan.FromMilliseconds(5000));
	}

	[Theory]
	[InlineData("test123", @"\d+", false, false)]          // Contains digits, deny any match - invalid
	[InlineData("test123", @"\d+", true, true)]           // Contains digits, deny full match only - valid (not full match)
	[InlineData("123", @"\d+", true, false)]              // All digits, deny full match - invalid
	[InlineData("123", @"\d+", false, false)]             // All digits, deny any match - invalid
	[InlineData("abc", @"\d+", false, true)]              // No digits, deny any match - valid
	[InlineData("abc", @"\d+", true, true)]               // No digits, deny full match - valid
	public void IsValid_WithDenyOnlyFullMatch_ShouldValidateCorrectly(string value, string pattern, bool denyOnlyFullMatch, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("TestField"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must not match the pattern");
		}
	}

	[Theory]
	[InlineData("test!value", "[!@#$%^&*()]", false, false)]  // Contains special char, deny any - invalid
	[InlineData("test!value", "[!@#$%^&*()]", true, true)]   // Contains special char, deny full only - valid
	[InlineData("!", "[!@#$%^&*()]", true, false)]          // Only special char, deny full - invalid
	[InlineData("!", "[!@#$%^&*()]", false, false)]         // Only special char, deny any - invalid
	public void IsValid_WithCharacterClass_AndDenyOnlyFullMatch_ShouldValidateCorrectly(string value, string pattern, bool denyOnlyFullMatch, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch);

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
		}
	}

	[Fact]
	public void DenyOnlyFullMatch_DefaultValue_ShouldBeFalse()
	{
		// Arrange & Act
		DenyRegularExpressionAttribute attribute = new("test");

		// Assert
		attribute.DenyOnlyFullMatch.ShouldBeFalse();
	}

	[Theory]
	[InlineData("<script>alert('xss')</script>", "<script.*?>", false, false)]  // XSS attempt, deny any - invalid
	[InlineData("hello <script> world", "<script.*?>", false, false)]         // Partial XSS, deny any - invalid
	[InlineData("hello <script> world", "<script.*?>", true, true)]          // Partial XSS, deny full only - valid
	[InlineData("<script></script>", "^<script.*?</script>$", true, false)]  // Full XSS, deny full - invalid
	public void IsValid_WithXSSPattern_ShouldValidateCorrectly(string value, string pattern, bool denyOnlyFullMatch, bool shouldBeValid)
	{
		// Arrange
		DenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("Input"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("must not match the pattern");
		}
	}

	[Fact]
	public void Constructor_WithEmptyPattern_ShouldThrowOnValidation()
	{
		// Arrange - empty pattern is allowed in constructor but should throw in SetupRegex
		DenyRegularExpressionAttribute attribute = new(string.Empty);

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => attribute.GetValidationResult("test", DummyValidationContext));
	}
}
