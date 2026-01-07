using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Core;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class DenyCharactersAttributeTests : ValidationTestBase
{
	[Fact]
	public void Constructor_WithNullCharacters_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new DenyCharactersAttribute((string?)null!));
	}

	[Fact]
	public void Constructor_WithEmptyCharacters_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new DenyCharactersAttribute(string.Empty));
	}

	[Fact]
	public void Constructor_WithValidCharacters_ShouldSetProperties()
	{
		// Arrange
		const string blacklist = "<>\"'/";

		// Act
		DenyCharactersAttribute attribute = new(blacklist);

		// Assert
		attribute.DenyCharacters.ShouldBe(blacklist);
	}

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		DenyCharactersAttribute attribute = new("<>");

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithEmptyString_ShouldReturnSuccess()
	{
		// Arrange
		DenyCharactersAttribute attribute = new("<>");

		// Act
		ValidationResult? result = attribute.GetValidationResult(string.Empty, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData("hello world", "<>\"", true)]          // No blacklisted chars - valid
	[InlineData("hello<world", "<>\"", false)]         // Contains < - invalid
	[InlineData("hello>world", "<>\"", false)]         // Contains > - invalid
	[InlineData("hello\"world", "<>\"", false)]        // Contains " - invalid
	[InlineData("helloworld", "<>\"", true)]           // No blacklisted chars - valid
	public void IsValid_WithStringValue_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

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
			result.ErrorMessage?.ShouldContain("must not contain any of the following characters");
		}
	}

	[Theory]
	[InlineData("user123", "!@#$%^&*()", true)]       // No special chars - valid
	[InlineData("user!", "!@#$%^&*()", false)]        // Contains ! - invalid
	[InlineData("user@domain", "!@#$%^&*()", false)]  // Contains @ - invalid
	[InlineData("test#tag", "!@#$%^&*()", false)]     // Contains # - invalid
	public void IsValid_WithSpecialCharacters_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

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

	[Theory]
	[InlineData("test123", "0123456789", false)]      // Contains digits - invalid
	[InlineData("testABC", "0123456789", true)]       // No digits - valid
	[InlineData("test", "0123456789", true)]          // No digits - valid
	public void IsValid_WithDigitBlacklist_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("NoNumbers"));

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
	[InlineData("test space", " ", false)]            // Contains space - invalid
	[InlineData("testspace", " ", true)]              // No space - valid
	[InlineData("test\ttab", "\t", false)]            // Contains tab - invalid
	[InlineData("test\nnewline", "\n", false)]        // Contains newline - invalid
	public void IsValid_WithWhitespaceBlacklist_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, CreateValidationContext("NoWhitespace"));

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
		DenyCharactersAttribute attribute = new("<>\"")
		{
			ErrorMessage = "Custom error: {0} has forbidden characters: {1}"
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult("test<value", CreateValidationContext("Input"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("Custom error");
		result.ErrorMessage.ShouldContain("Input");
	}

	[Theory]
	[InlineData("test", "<", true)]                   // Single blacklisted char, not present - valid
	[InlineData("te<st", "<", false)]                 // Single blacklisted char, present - invalid
	[InlineData("test", "<>\"'", true)]               // Multiple blacklisted chars, none present - valid
	[InlineData("te'st", "<>\"'", false)]             // Multiple blacklisted chars, one present - invalid
	public void IsValid_WithSingleAndMultipleBlacklist_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

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
	[InlineData(120, "0", false)]                     // Integer contains '0' when converted
	[InlineData(999, "0", true)]                      // Integer doesn't contain '0'
	[InlineData(3.14, ".", false)]                    // Double contains '.'
	[InlineData(314, ".", true)]                      // Integer doesn't contain '.'
	public void IsValid_WithNonStringTypes_ShouldConvertAndValidate(object value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

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
	public void FormatErrorMessage_WithDefaultMessage_ShouldReturnFormattedMessage()
	{
		// Arrange
		DenyCharactersAttribute attribute = new("<>\"");

		// Act
		string message = attribute.FormatErrorMessage("TestField");

		// Assert
		message.ShouldContain("TestField");
		message.ShouldContain("must not contain any of the following characters");
		message.ShouldContain("<>\"");
	}

	[Fact]
	public void FormatErrorMessage_WithCustomMessage_ShouldUseCustomFormat()
	{
		// Arrange
		DenyCharactersAttribute attribute = new("<>")
		{
			ErrorMessage = "{0} cannot have these: {1}"
		};

		// Act
		string message = attribute.FormatErrorMessage("MyField");

		// Assert
		message.ShouldContain("MyField");
		message.ShouldContain("cannot have these");
	}

	[Fact]
	public void IsValid_ValidationContext_ShouldIncludeMemberName()
	{
		// Arrange
		DenyCharactersAttribute attribute = new("@");
		ValidationContext context = CreateValidationContext("EmailInput");

		// Act
		ValidationResult? result = attribute.GetValidationResult("test@email", context);

		// Assert
		result.ShouldNotBeNull();
		result.MemberNames.ShouldContain("EmailInput");
	}

	[Theory]
	[InlineData("abc", "xyz", true)]                  // No overlap - valid
	[InlineData("abc", "cde", false)]                 // Contains 'c' - invalid
	[InlineData("abcdef", "xyz", true)]               // No overlap - valid
	[InlineData("abcdef", "fab", false)]              // Contains 'a', 'b', 'f' - invalid
	public void IsValid_WithDifferentCharacterSets_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);
		ValidationContext context = CreateValidationContext("CharSet");

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, context);

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.MemberNames.ShouldContain("CharSet");
		}
	}

	[Theory]
	[InlineData("café", "é", false)]                  // Unicode char present - invalid
	[InlineData("cafe", "é", true)]                   // Unicode char not present - valid
	[InlineData("test™", "™", false)]                 // Special unicode - invalid
	[InlineData("test", "™", true)]                   // Special unicode not present - valid
	public void IsValid_WithUnicodeCharacters_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);

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
			result.ErrorMessage.ShouldNotBeNull();
			result.ErrorMessage.ShouldContain(blacklist);
		}
	}

	[Fact]
	public void IsValid_WithAllPrintableSpecialChars_ShouldValidateCorrectly()
	{
		// Arrange
		const string allSpecialChars = "!@#$%^&*()_+-=[]{}\\|;:'\",.<>?/`~";
		DenyCharactersAttribute attribute = new(allSpecialChars);

		// Act
		ValidationResult? validResult = attribute.GetValidationResult("HelloWorld123", DummyValidationContext);
		ValidationResult? invalidResult = attribute.GetValidationResult("Hello@World", DummyValidationContext);

		// Assert
		validResult.ShouldBe(ValidationResult.Success);
		invalidResult.ShouldNotBeNull();
	}

	[Theory]
	[InlineData("test", "TEST", true)]                // Different case, no match - valid
	[InlineData("TEST", "test", true)]                // Different case, no match - valid
	[InlineData("TeSt", "eS", false)]                 // Exact case match - invalid
	public void IsValid_IsCaseSensitive_ShouldValidateCorrectly(string value, string blacklist, bool shouldBeValid)
	{
		// Arrange
		DenyCharactersAttribute attribute = new(blacklist);
		ReadOnlySpan<char> blacklistSpan = blacklist.AsSpan();

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, DummyValidationContext);

		// Assert - verify case-sensitive behavior
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
			// Also verify the underlying method respects case
			value.ContainsAnyCharacter(blacklistSpan).ShouldBe(false);
		}
		else
		{
			result.ShouldNotBeNull();
		}
	}
}
