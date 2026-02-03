using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

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

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithEmptyList_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string> emptyList = [];

		// Act
		ValidationResult? result = attribute.GetValidationResult(emptyList, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithAllNullItems_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string?> nullList = [null, null, null];

		// Act
		ValidationResult? result = attribute.GetValidationResult(nullList, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithAllEmptyStrings_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string> emptyStrings = [string.Empty, "", ""];

		// Act
		ValidationResult? result = attribute.GetValidationResult(emptyStrings, DummyValidationContext);

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

	[Fact]
	public void IsValid_WithAllValidItems_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^admin");
		List<string> items = ["user1", "customer", "guest"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithOneInvalidItem_ShouldFailAtCorrectIndex()
	{
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
	public void IsValid_WithFirstItemInvalid_ShouldFailAtIndex0()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("<script");
		List<string> items = ["<script>alert('xss')</script>", "safe", "clean"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Inputs"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 0");
	}

	[Fact]
	public void IsValid_WithLastItemInvalid_ShouldFailAtCorrectIndex()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("DROP|DELETE|INSERT");
		List<string> items = ["safe query", "another safe", "DROP TABLE users"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Queries"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
		result.ErrorMessage.ShouldContain("DROP TABLE users");
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

	[Fact]
	public void IsValid_WithNonEnumerableType_ShouldThrow()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");
		const int nonEnumerable = 42;

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(nonEnumerable, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithStringType_ShouldThrow()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("^test");
		const string singleString = "test";

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(singleString, DummyValidationContext));
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

	[Fact]
	public void IsValid_WithXSSPatterns_ShouldDetectMaliciousContent()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("<script|javascript:|onerror=|onclick=", denyOnlyFullMatch: false);
		List<string> items = [
			"clean content",
			"<script>alert('xss')</script>",
			"more clean content"
		];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Contents"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
	}

	[Fact]
	public void IsValid_WithSQLInjectionPatterns_ShouldDetectMaliciousContent()
	{
		// Arrange
		ListDenyRegularExpressionAttribute attribute = new("(DROP|DELETE|INSERT|UPDATE).*(TABLE|FROM|INTO)", denyOnlyFullMatch: false);
		List<string> items = [
			"SELECT * FROM users",
			"clean query",
			"DROP TABLE users"
		];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Queries"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
	}

	[Fact]
	public void IsValid_WithCaseInsensitivePattern_ShouldRespectRegexOptions()
	{
		// Arrange - Using default regex which is case-sensitive
		ListDenyRegularExpressionAttribute attribute = new("^ADMIN");

		// Act
		ValidationResult? resultLower = attribute.GetValidationResult(new List<string> { "admin" }, DummyValidationContext);
		ValidationResult? resultUpper = attribute.GetValidationResult(new List<string> { "ADMIN" }, CreateValidationContext("Items"));

		// Assert
		resultLower.ShouldBe(ValidationResult.Success); // Doesn't match (case-sensitive)
		resultUpper.ShouldNotBeNull(); // Matches
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
}
