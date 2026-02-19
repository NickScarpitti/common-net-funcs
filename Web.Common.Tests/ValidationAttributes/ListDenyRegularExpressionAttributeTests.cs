using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public enum ListDenyRegexTestCase
{
	NullValue,
	EmptyList,
	AllNullItems,
	AllEmptyStrings,
	AllValidItems
}

public sealed class ListDenyRegularExpressionAttributeTests : ValidationTestBase
{
	[Fact]
	public void Constructor_WithNullPattern_ShouldThrow()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new ListDenyRegularExpressionAttribute(null!));
	}

	[Fact]
	public void Constructor_WithValidPattern_ShouldSetProperties()
	{
		// Arrange
		const string pattern = "^admin.*";

		// Act
		ListDenyRegularExpressionAttribute attribute = new(pattern);

		// Assert
		attribute.Pattern.ShouldBe(pattern);
		attribute.MatchTimeoutInMilliseconds.ShouldBe(2000);
		attribute.DenyOnlyFullMatch.ShouldBe(false);
	}

	[Fact]
	public void Constructor_WithDenyOnlyFullMatch_ShouldSetProperty()
	{
		// Arrange
		const string pattern = @"^\d+$";

		// Act
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: true);

		// Assert
		attribute.DenyOnlyFullMatch.ShouldBe(true);
	}

	[Theory]
	[InlineData(ListDenyRegexTestCase.NullValue)]
	[InlineData(ListDenyRegexTestCase.EmptyList)]
	[InlineData(ListDenyRegexTestCase.AllNullItems)]
	[InlineData(ListDenyRegexTestCase.AllEmptyStrings)]
	[InlineData(ListDenyRegexTestCase.AllValidItems)]
	public void IsValid_WithValidScenarios_ShouldReturnSuccess(ListDenyRegexTestCase testCase)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		object? value = testCase switch
		{
			ListDenyRegexTestCase.NullValue => null,
			ListDenyRegexTestCase.EmptyList => new List<string>(),
			ListDenyRegexTestCase.AllNullItems => new List<string?> { null, null, null },
			ListDenyRegexTestCase.AllEmptyStrings => new List<string> { string.Empty, "", "" },
			ListDenyRegexTestCase.AllValidItems => new List<string> { "user1", "customer", "guest" },
			_ => throw new ArgumentOutOfRangeException(nameof(testCase))
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData("^admin.*", "user123", true)]
	[InlineData("^admin.*", "Admin123", true)]  // Case-sensitive, doesn't match
	[InlineData("^admin.*", "admin123", false)] // Matches - invalid
	[InlineData("^admin.*", "testadmin", true)] // Doesn't match start - valid
	public void IsValid_WithAnyMatchMode_ShouldValidateCorrectly(string pattern, string value, bool shouldBeValid)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: false);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage.ShouldNotBeNull();
			result.ErrorMessage.ShouldContain("index 0");
			result.ErrorMessage.ShouldContain(value);
			result.ErrorMessage.ShouldContain(pattern);
		}
	}

	[Theory]
	[InlineData(@"^\d+$", "123", false)]         // Full match - invalid
	[InlineData(@"^\d+$", "abc123def", true)]    // Not full match - valid
	[InlineData(@"^\d+$", "abc", true)]          // No match - valid
	[InlineData("^test$", "test", false)]       // Exact match - invalid
	[InlineData("^test$", "testing", true)]     // Not full match - valid
	public void IsValid_WithFullMatchMode_ShouldValidateCorrectly(string pattern, string value, bool shouldBeValid)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: true);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage.ShouldNotBeNull();
			result.ErrorMessage.ShouldContain("index 0");
		}
	}

	[Theory]
	[InlineData("Users", 1, "admin123")]
	[InlineData("Inputs", 0, "<script>alert('xss')</script>")]
	[InlineData("Queries", 2, "DROP TABLE users")]
	public void IsValid_WithInvalidItems_ShouldFailAtCorrectIndex(string memberName, int expectedIndex, string invalidValue)
	{
		// Arrange
		string pattern = memberName == "Queries" ? "DROP|DELETE|INSERT" :
						 memberName == "Inputs" ? "<script" : "^admin";
		ListDenyRegularExpressionAttribute attribute = new(pattern);
		List<string> items = expectedIndex switch
		{
			0 => [invalidValue, "safe", "clean"],
			1 => ["user1", invalidValue, "guest"],
			2 => ["safe query", "another safe", invalidValue],
			_ => throw new ArgumentOutOfRangeException(nameof(expectedIndex))
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext(memberName));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain($"index {expectedIndex}");
		result.MemberNames.ShouldContain(memberName);
	}

	[Fact]
	public void IsValid_WithOneInvalidItem_ShouldFailAtCorrectIndex()
	{
		// Legacy test - kept for backwards compatibility
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string> items = ["user1", "admin123", "guest"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Users"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
		result.ErrorMessage.ShouldContain("admin123");
		result.MemberNames.ShouldContain("Users");
	}

	[Fact]
	public void IsValid_WithMultipleInvalidItems_ShouldFailAtFirstInvalid()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(@"^\d+$");
		List<string> items = ["text", "123", "456"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
		result.ErrorMessage.ShouldContain("123");
	}

	[Fact]
	public void IsValid_WithMixedNullAndValidItems_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string?> items = ["user", null, "guest", "", "customer"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithMixedNullAndInvalidItems_ShouldFailAtInvalidIndex()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");
		List<string?> items = ["user", null, "testing123", "", "guest"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
		result.ErrorMessage.ShouldContain("testing123");
	}

	[Fact]
	public void IsValid_WithCustomErrorMessage_ShouldUseCustomMessage()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin")
		{
			ErrorMessage = "Custom error: Item {0} '{1}' matches forbidden pattern '{2}'"
		};
		List<string> items = ["user", "admin123"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("Custom error");
		result.ErrorMessage.ShouldContain("1"); // index
	}

	[Fact]
	public void IsValid_WithNonStringEnumerable_ShouldConvertAndValidate()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^0");
		List<int> numbers = [123, 456, 789];

		// Act
		ValidationResult? result = attribute.GetValidationResult(numbers, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonStringEnumerable_StartingWithZero_ShouldFail()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^0");
		List<double> numbers = [123.45, 0.456, 789.0]; // 0.456 starts with '0'

		// Act
		ValidationResult? result = attribute.GetValidationResult(numbers, CreateValidationContext("Numbers"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
	}

	[Theory]
	[InlineData(false, "notAList")]
	[InlineData(true, "singleString")]
	public void IsValid_WithNonEnumerableType_ShouldThrow(bool isString, string value)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(isString ? value : (object)42, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithArray_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		string[] items = ["user1", "guest", "admin"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Array"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
	}

	[Fact]
	public void IsValid_WithIEnumerableString_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");
		IEnumerable<string> items = new List<string> { "user", "guest" };

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(@"^\d{3}-\d{3}-\d{4}$", "555-1234", true)]      // Doesn't match phone pattern
	[InlineData(@"^\d{3}-\d{3}-\d{4}$", "555-123-4567", false)]  // Matches phone pattern
	[InlineData(@"^\w+@\w+\.\w+$", "test", true)]               // Doesn't match email pattern
	[InlineData(@"^\w+@\w+\.\w+$", "test@example.com", false)]  // Matches email pattern
	public void IsValid_WithCommonPatterns_ShouldValidateCorrectly(string pattern, string value, bool shouldBeValid)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

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
	[InlineData("<script|javascript:|onerror=|onclick=", 1, "<script>alert('xss')</script>")]
	[InlineData("(DROP|DELETE|INSERT).*(TABLE|FROM|INTO)", 2, "DROP TABLE users")]
	public void IsValid_WithSecurityPatterns_ShouldDetectMaliciousContent(string pattern, int expectedIndex, string maliciousValue)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: false);
		List<string> items = expectedIndex switch
		{
			1 => ["clean content", maliciousValue, "more clean content"],
			2 => ["SELECT * FROM users", "clean query", maliciousValue],
			_ => throw new ArgumentOutOfRangeException(nameof(expectedIndex))
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext(expectedIndex == 1 ? "Contents" : "Queries"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain($"index {expectedIndex}");
	}

	[Theory]
	[InlineData("^admin", "admin", "ADMIN")]
	[InlineData("^ADMIN", "ADMIN", "admin")]
	public void IsValid_WithCaseInsensitivePattern_ShouldRespectRegexOptions(string pattern, string invalidValue, string validValue)
	{
		// Arrange - Using default regex which is case-sensitive
		ListDenyRegularExpressionAttribute attribute = new(pattern);

		// Act
		ValidationResult? resultValid = attribute.GetValidationResult(new List<string> { validValue }, DummyValidationContext);
		ValidationResult? resultInvalid = attribute.GetValidationResult(new List<string> { invalidValue }, CreateValidationContext("Items"));

		// Assert
		resultValid.ShouldBe(ValidationResult.Success); // Doesn't match (case-sensitive)
		resultInvalid.ShouldNotBeNull(); // Matches
	}

	[Fact]
	public void IsValid_WithComplexPattern_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(@"^(?=.*[<>""']).*$");
		List<string> items = ["clean", "has<bracket"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
	}

	[Fact]
	public void IsValid_WithManyItems_ShouldValidateAll()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^X");
		List<string> items = new(100);
		for (int i = 0; i < 100; i++)
		{
			items.Add($"item{i}");
		}

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithManyItems_OneInvalid_ShouldFailAtCorrectIndex()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^X");
		List<string> items = new(100);
		for (int i = 0; i < 100; i++)
		{
			items.Add(i == 75 ? "Xitem75" : $"item{i}");
		}

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 75");
		result.ErrorMessage.ShouldContain("Xitem75");
	}

	[Fact]
	public void IsValid_WithUrlEncodableCharacters_ShouldEncodeInErrorMessage()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("<script");
		List<string> items = ["<script>alert('xss')</script>"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		// The UrlEncodeReadable should be applied to the value in the error message
		result.ErrorMessage.ShouldContain("script");
	}

	[Fact]
	public void FormatErrorMessage_ShouldContainPlaceholders()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");

		// Act
		string message = attribute.FormatErrorMessage("TestField");

		// Assert
		message.ShouldContain("{index}");
		message.ShouldContain("{value}");
		message.ShouldContain("^test");
	}

	[Fact]
	public void MatchTimeout_ShouldReflectMilliseconds()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test")
		{
			MatchTimeoutInMilliseconds = 5000
		};

		// Act & Assert
		attribute.MatchTimeout.TotalMilliseconds.ShouldBe(5000);
	}

	[Fact]
	public void IsValid_WithInvalidRegexPattern_ShouldThrowOnFirstUse()
	{
		// Arrange
#pragma warning disable RE0001 // Invalid regex pattern
		ListDenyRegularExpressionAttribute attribute = new("[invalid(regex");
#pragma warning restore RE0001 // Invalid regex pattern
		List<string> items = ["test"];

		// Act & Assert
		Should.Throw<ArgumentException>(() => attribute.GetValidationResult(items, DummyValidationContext));
	}

	[Theory]
	[InlineData(@"^\d+$", "123")]
	[InlineData("^[A-Z]+$", "ABC")]
	[InlineData("^test.*", "testing")]
	public void IsValid_DenyOnlyFullMatch_WithExactMatch_ShouldFail(string pattern, string value)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: true);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 0");
	}

	[Theory]
	[InlineData(@"^\d+$", "abc123")]
	[InlineData("^[A-Z]+$", "ABCdef")]
	[InlineData("^test$", "testing")]
	public void IsValid_DenyOnlyFullMatch_WithPartialMatch_ShouldSucceed(string pattern, string value)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: true);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_AnyMatchMode_WithPartialMatch_ShouldFail()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(@"\d+", denyOnlyFullMatch: false);
		List<string> items = ["abc123def"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 0");
		result.ErrorMessage.ShouldContain("abc123def");
	}

	[Fact]
	public void IsValid_AnyMatchMode_WithoutMatch_ShouldSucceed()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(@"\d+", denyOnlyFullMatch: false);
		List<string> items = ["abcdef"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void Constructor_WithEmptyPattern_ShouldThrowOnValidation()
	{
		// Arrange - empty pattern is allowed in constructor but should throw in SetupRegex
		ListDenyRegularExpressionAttribute attribute = new(string.Empty);

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => attribute.GetValidationResult(new List<string> { "test" }, DummyValidationContext));
	}

	[Theory]
	[InlineData(-1, true)]
	[InlineData(5000, true)]
	public void SetupRegex_WithTimeoutVariations_ShouldCompileRegex(int timeoutMs, bool shouldFail)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(@"^\d+$")
		{
			MatchTimeoutInMilliseconds = timeoutMs
		};
		List<string> items = ["123"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		if (shouldFail)
		{
			result.ShouldNotBeNull(); // Should fail because "123" matches the pattern
		}
		if (timeoutMs == 5000)
		{
			attribute.MatchTimeout.TotalMilliseconds.ShouldBe(5000);
		}
	}

	[Theory]
	[InlineData(@"\d+", "abc123def", 0, 3, false)] // Match at index 3, length 3, total length 9 - should succeed
	[InlineData(@"\d+", "123abc", 0, 3, false)]    // Match at index 0, length 3, total length 6 - should succeed
	public void IsValid_DenyOnlyFullMatch_WithMatchNotFullLength_ShouldSucceed(string pattern, string value, int matchIndex, int matchLength, bool shouldFail)
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new(pattern, denyOnlyFullMatch: true);
		List<string> items = [value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		if (shouldFail)
		{
			result.ShouldNotBeNull();
		}
		else
		{
			result.ShouldBe(ValidationResult.Success);
		}
	}
}
