using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public enum CharConstructorTestCase
{
	NullString,
	EmptyString,
	NullArray,
	EmptyArray
}

public enum ListDenyCharTestCase
{
	NullValue,
	EmptyList,
	AllNullItems,
	AllEmptyStrings,
	AllValidItems
}

public sealed class ListDenyCharactersAttributeTests : ValidationTestBase
{
	[Theory]
	[InlineData(CharConstructorTestCase.NullString)]
	[InlineData(CharConstructorTestCase.EmptyString)]
	[InlineData(CharConstructorTestCase.NullArray)]
	[InlineData(CharConstructorTestCase.EmptyArray)]
	public void Constructor_WithInvalidInput_ShouldThrow(CharConstructorTestCase testCase)
	{
		// Act & Assert
		switch (testCase)
		{
			case CharConstructorTestCase.NullString:
				Should.Throw<ArgumentNullException>(() => new ListDenyCharactersAttribute((string?)null!));
				break;
			case CharConstructorTestCase.EmptyString:
				Should.Throw<ArgumentException>(() => new ListDenyCharactersAttribute(string.Empty));
				break;
			case CharConstructorTestCase.NullArray:
				Should.Throw<ArgumentNullException>(() => new ListDenyCharactersAttribute((char[])null!));
				break;
			case CharConstructorTestCase.EmptyArray:
				Should.Throw<ArgumentException>(() => new ListDenyCharactersAttribute(Array.Empty<char>()));
				break;
		}
	}

	[Theory]
	[InlineData("<>", "'")]
	[InlineData("abc", "def")]
	public void Constructor_WithValidInput_ShouldSetProperties(string chars, string altChars)
	{
		// Arrange & Act
		ListDenyCharactersAttribute attribute1 = new(chars);
		ListDenyCharactersAttribute attribute2 = new(altChars.ToCharArray());

		// Assert
		attribute1.DenyCharacters.ShouldBe(chars);
		attribute2.DenyCharacters.ShouldBe(altChars);
	}

	[Theory]
	[InlineData(ListDenyCharTestCase.NullValue)]
	[InlineData(ListDenyCharTestCase.EmptyList)]
	[InlineData(ListDenyCharTestCase.AllNullItems)]
	[InlineData(ListDenyCharTestCase.AllEmptyStrings)]
	[InlineData(ListDenyCharTestCase.AllValidItems)]
	public void IsValid_WithValidScenarios_ShouldReturnSuccess(ListDenyCharTestCase testCase)
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");
		object? value = testCase switch
		{
			ListDenyCharTestCase.NullValue => null,
			ListDenyCharTestCase.EmptyList => new List<string>(),
			ListDenyCharTestCase.AllNullItems => new List<string?> { null, null, null },
			ListDenyCharTestCase.AllEmptyStrings => new List<string> { string.Empty, "", "" },
			ListDenyCharTestCase.AllValidItems => new List<string> { "hello", "world", "test" },
			_ => throw new ArgumentOutOfRangeException(nameof(testCase))
		};

		// Act
		ValidationResult? result = attribute.GetValidationResult(value, testCase == ListDenyCharTestCase.AllValidItems ? CreateValidationContext("Items") : DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData("<>\"", 1, "world<test", "Items")]
	[InlineData("@#$", 0, "test@email", "Emails")]
	[InlineData("!", 2, "invalid!", "Items")]
	public void IsValid_WithInvalidItems_ShouldFailAtCorrectIndex(string blacklist, int expectedIndex, string invalidValue, string memberName)
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new(blacklist);
		List<string> items = expectedIndex switch
		{
			0 => [invalidValue, "valid", "alsoValid"],
			1 => ["hello", invalidValue, "valid"],
			2 => ["valid1", "valid2", invalidValue],
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
	public void IsValid_WithMixedNullAndValidItems_ShouldReturnSuccess()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");
		List<string?> items = ["valid", null, "alsoValid", "", "another"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithMixedNullAndInvalidItems_ShouldFailAtInvalidIndex()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");
		List<string?> items = ["valid", null, "invalid<", "", "another"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
	}

	[Theory]
	[InlineData("!@#$%^&*()", "test123")]
	[InlineData("<>\"'", "clean text")]
	[InlineData("0123456789", "NoNumbers")]
	public void IsValid_WithValidList_AllItemsClean_ShouldReturnSuccess(string blacklist, string value)
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new(blacklist);
		List<string> items = [value, value, value];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData("!@#$", "test!", 0)]
	[InlineData("<>", "clean", -1)]  // -1 means should succeed
	[InlineData("123", "abc", -1)]
	[InlineData("123", "test2test", 0)]
	public void IsValid_WithTheoryData_ShouldValidateCorrectly(string blacklist, string testValue, int expectedFailIndex)
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new(blacklist);
		List<string> items = [testValue];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		if (expectedFailIndex == -1)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage.ShouldNotBeNull();
			result.ErrorMessage.ShouldContain($"index {expectedFailIndex}");
		}
	}

	[Fact]
	public void IsValid_WithCustomErrorMessage_ShouldUseCustomMessage()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>")
		{
			ErrorMessage = "Custom: Item {0} value '{1}' has forbidden chars '{2}'"
		};
		List<string> items = ["valid", "test<invalid"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("Custom");
		result.ErrorMessage.ShouldContain("1"); // index
	}

	[Fact]
	public void IsValid_WithNonStringEnumerable_ShouldConvertAndValidate()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("0");
		List<int> numbers = [123, 456, 789];

		// Act
		ValidationResult? result = attribute.GetValidationResult(numbers, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonStringEnumerable_ContainingBlacklisted_ShouldFail()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("0");
		List<int> numbers = [123, 405, 789]; // 405 contains '0'

		// Act
		ValidationResult? result = attribute.GetValidationResult(numbers, CreateValidationContext("Numbers"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
		result.ErrorMessage.ShouldContain("405");
	}

	[Fact]
	public void IsValid_WithNonEnumerableType_ShouldThrow()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");
		const int nonEnumerable = 42;

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(nonEnumerable, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithStringType_ShouldThrow()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");
		const string singleString = "test";

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(singleString, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithArray_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("!");
		string[] items = ["valid1", "valid2", "invalid!"];

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
		ListDenyCharactersAttribute attribute = new("@");
		IEnumerable<string> items = new List<string> { "test", "another" };

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithSpecialCharacters_ShouldDetectCorrectly()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("\t\n\r");
		List<string> items = ["clean", "has\ttab", "alsoClean"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
	}

	[Fact]
	public void IsValid_WithUnicodeCharacters_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("é™");
		List<string> items = ["cafe", "test", "café"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 2");
		// UrlEncodeReadable will encode é, so just check for 'caf'
		result.ErrorMessage.ShouldContain("caf");
	}

	[Fact]
	public void IsValid_WithManyItems_ShouldValidateAll()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("X");
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
		ListDenyCharactersAttribute attribute = new("X");
		List<string> items = new(100);
		for (int i = 0; i < 100; i++)
		{
			items.Add(i == 50 ? "itemX50" : $"item{i}");
		}

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 50");
		result.ErrorMessage.ShouldContain("itemX50");
	}

	[Fact]
	public void FormatErrorMessage_ShouldContainPlaceholders()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>");

		// Act
		string message = attribute.FormatErrorMessage("TestField");

		// Assert
		message.ShouldContain("{index}");
		message.ShouldContain("{value}");
		message.ShouldContain("<>");
	}

	[Fact]
	public void IsValid_WithUrlEncodableCharacters_ShouldEncodeInErrorMessage()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<");
		List<string> items = ["test<script>alert('xss')</script>"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		// The UrlEncodeReadable should be applied to the value in the error message
		result.ErrorMessage.ShouldContain("test");
	}

	[Fact]
	public void IsValid_CaseSensitive_ShouldDetectExactCase()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("A");
		List<string> items = ["test", "another"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_CaseSensitive_ShouldFailOnExactMatch()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("A");
		List<string> items = ["test", "Another"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
		result.ErrorMessage.ShouldContain("Another");
	}

	[Fact]
	public void IsValid_WithMultipleBlacklistedCharsInOneItem_ShouldFailOnFirst()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("<>\"");
		List<string> items = ["valid", "has<and>both"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, CreateValidationContext("Items"));

		// Assert
		result.ShouldNotBeNull();
		result.ErrorMessage.ShouldNotBeNull();
		result.ErrorMessage.ShouldContain("index 1");
		// UrlEncodeReadable will encode < and >, so just check for 'has' and 'both'
		result.ErrorMessage.ShouldContain("has");
		result.ErrorMessage.ShouldContain("both");
	}

	[Fact]
	public void IsValid_WithSingleCharBlacklist_ShouldValidateCorrectly()
	{
		// Arrange
		ListDenyCharactersAttribute attribute = new("X");
		List<string> items = ["test", "another", "nox"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(items, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}
}
