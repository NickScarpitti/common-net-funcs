using System.Text;
using System.Text.RegularExpressions;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class StringsTests
{
	[Theory]
	[InlineData("Hello", 3, "Hel")]
	[InlineData("Test", 5, "Test")]
	[InlineData("ABC", 0, "")]
	[InlineData(null, 3, null)]
	[InlineData("", 1, "")]
	public void Left_ReturnsCorrectSubstring(string? input, int numChars, string? expected)
	{
		// Act
		string? result = input.Left(numChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", 3, "llo")]
	[InlineData("Test", 5, "Test")]
	[InlineData("ABC", 0, "")]
	[InlineData(null, 3, null)]
	[InlineData("", 1, "")]
	public void Right_ReturnsCorrectSubstring(string? input, int numChars, string? expected)
	{
		// Act
		string? result = input.Right(numChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello World", "Hello", "World", " ")]
	[InlineData("Start[Middle]End", "[", "]", "Middle")]
	[InlineData("NoDelimiters", "[", "]", null)]
	[InlineData(null, "[", "]", null)]
	[InlineData("", "[", "]", null)]
	[InlineData("[test]", "[", "]", "test")]
	[InlineData("test]start[end", "[", "]", null)]
	[InlineData("test[start]middle]end", "[", "]", "start]middle")]
	public void ExtractBetween_ReturnsCorrectSubstring(string? input, string start, string end, string? expected)
	{
		// Act
		string? result = input.ExtractBetween(start, end);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("null", null)]
	[InlineData("NULL", null)]
	[InlineData(" null ", null)]
	[InlineData("not null", "not null")]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData("Null", null)]
	[InlineData("NuLL", null)]
	[InlineData("   ", "   ")]
	[InlineData("nullnull", null)]
	[InlineData("NULLNULL", null)]
	[InlineData("NullNull", null)]
	[InlineData("  Null  ", null)] // Trim branch
	[InlineData("  null  ", null)] // Trim branch with lowercase
	[InlineData("   null", null)] // Leading spaces
	[InlineData("null   ", null)] // Trailing spaces
	[InlineData("some text", "some text")] // Non-null string
	[InlineData("Null123", "Null123")] // Null with suffix
	[InlineData("123Null", "123Null")] // Null with prefix
	[InlineData("NULL NULL NULL", "NULL NULL NULL")] // Multiple NULLs with spaces
	[InlineData("nUlL", null)] // Mixed case variant
	[InlineData("\tNull\t", null)] // Tab characters instead of spaces
	[InlineData("\nNull\n", null)] // Newline characters
	[InlineData("NULLNULLNULL", null)] // Three NULLs concatenated
	[InlineData("nullNULLnull", null)] // Mixed case multiple nulls
	[InlineData("  nullnull  ", "  nullnull  ")] // Multiple nulls with surrounding spaces - has spaces so not just NULL
	[InlineData("text with null", "text with null")] // Contains null but not only null
	[InlineData("NULL_NULL", "NULL_NULL")] // NULLs separated by underscore
	[InlineData("NULL.NULL", "NULL.NULL")] // NULLs separated by dot
	[InlineData("xNULL", "xNULL")] // Prefix before NULL
	[InlineData("NULLx", "NULLx")] // Suffix after NULL
	public void MakeNullNull_HandlesNullStrings(string? input, string? expected)
	{
		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void MakeNullNull_FirstCondition_StrEqReturnsTrue()
	{
		// Tests first part of OR: s?.StrEq("Null") != false
		// Arrange
		const string input = "NULL";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MakeNullNull_SecondCondition_OnlyNullPatternsRemain()
	{
		// Tests second part of OR: s.ToUpperInvariant().Replace("NULL", string.Empty)?.Length == 0
		// First condition should be false for this to be the deciding factor
		// Arrange
		const string input = "nullnull"; // Doesn't equal "Null" alone, but is composed only of "null"

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MakeNullNull_ThirdCondition_TrimmedEqualsNull()
	{
		// Tests third part of OR: s.Trim().StrEq("Null")
		// This should be redundant with first condition, but testing explicitly
		// Arrange
		const string input = "   Null   ";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MakeNullNull_AllConditionsFalse_ReturnsOriginal()
	{
		// Tests when all three OR conditions are false
		// Arrange
		const string input = "some value";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBe("some value");
	}

	[Fact]
	public void MakeNullNull_EmptyAfterReplaceButNotNull_ReturnsNull()
	{
		// Specifically test the second condition where Replace leaves empty string
		// Arrange
		const string input = "NullNullNull";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MakeNullNull_ComplexWhitespace_VariousBehaviors()
	{
		// Test with various whitespace combinations
		// Arrange & Act & Assert
		"\r\nNull\r\n".MakeNullNull().ShouldBeNull(); // Leading/trailing newlines are trimmed
		" \t Null \t ".MakeNullNull().ShouldBeNull(); // Leading/trailing tabs are trimmed
		"NULL\nNULL".MakeNullNull().ShouldBe("NULL\nNULL"); // Newline between NULLs means it's not just NULL
		"null\r\nnull".MakeNullNull().ShouldBe("null\r\nnull"); // CRLF between nulls means it's not just null
	}

	[Fact]
	public void MakeNullNull_OnlyWhitespace_ReturnsOriginal()
	{
		// Tests the outer if condition - whitespace only should not become null
		// Arrange
		const string input = "     ";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBe("     ");
	}

	[Theory]
	[InlineData("HelloWorld", "Hello World")]
	[InlineData("camelCase", "camel Case")]
	[InlineData("ABC", "A B C")]
	[InlineData(null, null)]
	[InlineData("", "")]
	public void ParsePascalCase_CorrectlySeparatesWords(string? input, string? expected)
	{
		// Act
		string? result = input.ParsePascalCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello  world   test", "hello world test")]
	[InlineData("   extra   spaces   ", "extra spaces")]
	[InlineData("\t\nwhitespace\r\n", "whitespace")]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData("already trimmed", "already trimmed")]
	[InlineData("  ", "")]
	public void TrimFull_RemovesExcessWhitespace(string? input, string? expected)
	{
		// Act
		string? result = input.TrimFull();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Test String", false)]
	[InlineData("", true)]
	[InlineData("   ", true)]
	[InlineData(null, true)]
	[InlineData("NotEmpty", false)]
	public void IsNullOrWhiteSpace_DetectsEmptyStrings(string? input, bool expected)
	{
		// Act
		bool result = input.IsNullOrWhiteSpace();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello WORLD", "world", true)]
	[InlineData("Test", "no match", false)]
	[InlineData(null, "test", false)]
	[InlineData("test", null, false)]
	public void ContainsInvariant_ChecksStringContains(string? input, string? searchText, bool expected)
	{
		// Act
		bool result = input.ContainsInvariant(searchText);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("The quick brown fox", new[] { "quick", "fox" }, true, true)]
	[InlineData("The quick brown fox", new[] { "quick", "missing" }, true, true)]
	[InlineData("The quick brown fox", new[] { "quick", "fox" }, false, true)]
	[InlineData("The quick brown fox", new[] { "quick", "missing" }, false, false)]
	[InlineData("", new[] { "quick", "missing" }, false, false)]
	public void ContainsInvariant_MultipleStrings_HandlesLogicalOperations(string input, string[] searchTexts, bool useOrComparison, bool expected)
	{
		// Act
		bool result = input.ContainsInvariant(searchTexts, useOrComparison);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("The quick brown fox", new[] { "quick", "fox" }, true, true)]
	[InlineData("The quick brown fox", new[] { "quick", "missing" }, true, true)]
	[InlineData("The quick brown fox", new[] { "quick", "fox" }, false, true)]
	[InlineData("The quick brown fox", new[] { "quick", "missing" }, false, false)]
	[InlineData("", new[] { "quick", "missing" }, false, false)]
	public void SpanContainsInvariant_MultipleStrings_HandlesLogicalOperations(string input, string[] searchTexts, bool useOrComparison, bool expected)
	{
		// Act
		bool result = input.AsSpan().ContainsInvariant(searchTexts, useOrComparison);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello WORLD", "world", true)]
	[InlineData("Test", "no match", false)]
	[InlineData(null, "test", false)]
	[InlineData("test", null, false)]
	public void ContainsInvariant_ChecksSpanContains(string? input, string? searchText, bool expected)
	{
		// Act
		bool result = input.AsSpan().ContainsInvariant(searchText.AsSpan());

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello WORLD", "HELLO", "Test", "Test WORLD")]
	[InlineData("Test String", "missing", "replace", "Test String")]
	[InlineData(null, "test", "replace", null)]
	public void ReplaceInvariant_ReplacesText(string? input, string oldValue, string newValue, string? expected)
	{
		// Act
		string? result = input.ReplaceInvariant(oldValue, newValue, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("string1", "string1", true)]
	[InlineData("STRING1", "string1", true)]
	[InlineData("string1", "string2", false)]
	[InlineData(null, null, true)]
	[InlineData("", null, true)]
	[InlineData(null, "", true)]
	[InlineData("test", null, false)]
	[InlineData(null, "test", false)]
	[InlineData("  test  ", "test", true)]
	[InlineData("test", "  test  ", true)]
	[InlineData("", "", true)]
	public void StrEq_ComparesStrings(string? input1, string? input2, bool expected)
	{
		// Act
		bool result = input1.StrEq(input2);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToTitleCase_HandlesUppercaseWords()
	{
		// Arrange
		const string input = "THE QUICK BROWN FOX";

		// Act & Assert
		input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase, cancellationToken: TestContext.Current.CancellationToken).ShouldBe("The Quick Brown Fox");

		input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.IgnoreUppercase, cancellationToken: TestContext.Current.CancellationToken).ShouldBe("THE QUICK BROWN FOX");

		input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertByLength, minLengthToConvert: 4, cancellationToken: TestContext.Current.CancellationToken).ShouldBe("THE Quick Brown FOX");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ToTitleCase_ReturnsInputForNullOrWhitespace(string? input)
	{
		// Act
		string? result = input.ToTitleCase(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(input);
	}

	[Fact]
	public void ToTitleCase_RespectsCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		cts.Cancel();
		const string input = "word1 word2 word3 word4 word5";

		// Act & Assert
		Should.Throw<OperationCanceledException>(() =>
			input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase, cancellationToken: cts.Token));
	}

	[Theory]
	[InlineData("Hello World", new[] { "Hello", "World" }, true, true)]
	[InlineData("Hello World", new[] { "Hello" }, true, true)]
	[InlineData("Hello World", new[] { "Hello", "Missing" }, true, true)]
	[InlineData("Hello World", new[] { "Missing" }, true, false)]
	[InlineData("Hello World", new[] { "Hello", "World" }, false, true)]
	[InlineData("Hello World", new[] { "Hello", "Missing" }, false, false)]
	[InlineData(null, new[] { "test" }, true, false)]
	[InlineData("", new[] { "test" }, true, false)]
	[InlineData("   ", new[] { "test" }, true, false)]
	public void Contains_WithCollection_Works(string? s, string[] stringsToFind, bool useOrComparison, bool expected)
	{
		// Act
		bool result = s.Contains(stringsToFind, useOrComparison);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello World", new[] { "Hello", "World" }, true, true)]
	[InlineData("Hello World", new[] { "Hello" }, true, true)]
	[InlineData("Hello World", new[] { "Hello", "Missing" }, true, true)]
	[InlineData("Hello World", new[] { "Missing" }, true, false)]
	[InlineData("Hello World", new[] { "Hello", "World" }, false, true)]
	[InlineData("Hello World", new[] { "Hello", "Missing" }, false, false)]
	[InlineData("", new[] { "test" }, true, false)]
	public void Contains_Span_WithCollection_Works(string s, string[] stringsToFind, bool useOrComparison, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = s.AsSpan();

		// Act
		bool result = span.Contains(stringsToFind, useOrComparison);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc123", true)]
	[InlineData("abc 123", true, true)]
	[InlineData("abc@123", false)]
	[InlineData(null, false)]
	public void IsAlphanumeric_ValidatesInput(string? input, bool expected, bool allowSpaces = false)
	{
		// Act
		bool result = input.IsAlphanumeric(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abcDEF", true)]
	[InlineData("abc DEF", true, true)]
	[InlineData("abc123", false)]
	[InlineData(null, false)]
	public void IsAlphaOnly_ValidatesInput(string? input, bool expected, bool allowSpaces = false)
	{
		// Act
		bool result = input.IsAlphaOnly(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void TrimObjectStrings_HandlesComplexObjects()
	{
		// Arrange
		TestObject testObject = new()
		{
			StringProp = "  test  ",
			NestedObject = new() { InnerString = "  inner1  " }
		};

		// Act
		TestObject result = testObject.TrimObjectStrings(recursive: true);

		// Assert
		result.StringProp.ShouldBe("test");
		result.NestedObject.InnerString.ShouldBe("inner1");
	}

	[Fact]
	public void TrimObjectStrings_HandlesNull()
	{
		// Arrange
		TestObject? testObject = null;

		// Act
		TestObject? result = testObject?.TrimObjectStrings(recursive: true);

		// Assert
		result.ShouldBeNull();
	}

	public sealed class TestObject
	{
		public string? StringProp { get; set; }

		public NestedObject NestedObject { get; set; } = new();

		public string? StringPropWithSpaces { get; set; }

		public NestedObject NestedStringPropWithSpaces { get; set; } = new();
	}

	public sealed class NestedObject
	{
		public string? InnerString { get; set; }
	}

	[Fact]
	public void NormalizeObjectStrings_HandlesComplexObjects()
	{
		// Arrange
		TestObject testObject = new()
		{
			StringProp = "test\u0300", // Combining grave accent
			NestedObject = new() { InnerString = "e\u0301" }, // Combining acute accent
			StringPropWithSpaces = "  test  ",
			NestedStringPropWithSpaces = new() { InnerString = "  test  " }
		};

		// Act
		TestObject result = testObject.NormalizeObjectStrings(true, NormalizationForm.FormD, true);

		// Assert
		result.StringProp.ShouldBe(testObject.StringProp);
		result.NestedObject.InnerString.ShouldBe(testObject.NestedObject.InnerString);
		result.StringPropWithSpaces.ShouldBe(testObject.StringPropWithSpaces.Trim());
		result.NestedStringPropWithSpaces.InnerString.ShouldBe(testObject.NestedStringPropWithSpaces.InnerString.Trim());
	}

	[Theory]
	[InlineData("123-456-7890", "-", false, "123-456-7890")]
	[InlineData("1234567890", "-", false, "123-456-7890")]
	[InlineData("11234567890", "-", false, "+1 123-456-7890")]
	[InlineData("111234567890", "-", false, "+11 123-456-7890")]
	[InlineData("1234567", "-", false, "123-4567")]
	[InlineData("1234567890x123", "-", false, "123-456-7890x123")]
	[InlineData("1234567890", "-", true, "(123)-456-7890")]
	[InlineData("11234567890", "-", true, "+1 (123)-456-7890")]
	public void FormatPhoneNumber_FormatsCorrectly(string input, string separator, bool addParenToAreaCode, string expected)
	{
		// Act
		string? result = input.FormatPhoneNumber(separator, addParenToAreaCode);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello\nworld\ntest", 3)]
	[InlineData("single line", 1)]
	[InlineData("", 0)]
	[InlineData(null, 0)]
	public void SplitLines_CountsCorrectly(string? input, int expectedLineCount)
	{
		// Act
		IEnumerable<string> lines = input.SplitLines(TestContext.Current.CancellationToken);

		// Assert
		lines.Count().ShouldBe(expectedLineCount);
	}

	[Theory]
	[InlineData(2.5, 3, "2 1/2")]
	[InlineData(3.25, 2, "3 1/4")]
	[InlineData(1.125, 3, "1 1/8")]
	public void ToFractionString_ConvertsFractionCorrectly(decimal input, int maxDecimals, string expected)
	{
		// Act
		string result = input.ToFractionString(maxDecimals);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("2 1/2", 2.5)]
	[InlineData("3 1/4", 3.25)]
	[InlineData("1/2", 0.5)]
	[InlineData("2.5", 2.5)]
	public void FractionToDecimal_ParsesCorrectly(string input, decimal expected)
	{
		// Act
		decimal? result = input.FractionToDecimal();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello123", "hello")]
	[InlineData("123abc456", "abc")]
	[InlineData("123456", "")]
	[InlineData(null, null)]
	public void RemoveNumbers_RemovesCorrectly(string? input, string? expected)
	{
		// Act
		string? result = input.RemoveNumbers();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello123", "123")]
	[InlineData("abc456def", "456")]
	[InlineData("abc", "")]
	[InlineData(null, null)]
	public void RemoveLetters_RemovesCorrectly(string? input, string? expected)
	{
		// Act
		string? result = input.RemoveLetters();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello123", "hello")]
	[InlineData("A1BC456DEF", "ABCDEF")]
	[InlineData("123", "")]
	[InlineData(null, null)]
	public void GetOnlyLetters_ExtractsCorrectly(string? input, string? expected)
	{
		// Act
		string? result = input.GetOnlyLetters();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello123", "123")]
	[InlineData("ABC45X6DEF", "456")]
	[InlineData("abc", "")]
	[InlineData("123 1/2", "123 1/2", true)]
	[InlineData(null, null)]
	public void GetOnlyNumbers_ExtractsCorrectly(string? input, string? expected, bool allowFractions = false)
	{
		// Act
		string? result = input.GetOnlyNumbers(allowFractions);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("!@#abc123", "abc123")]
	[InlineData("---test", "test")]
	[InlineData("abc123", "abc123")]
	[InlineData(null, null)]
	public void RemoveLeadingNonAlphanumeric_RemovesCorrectly(string? input, string? expected)
	{
		// Act
		string? result = input.RemoveLeadingNonAlphanumeric();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc123!@#", "abc123")]
	[InlineData("test---", "test")]
	[InlineData("abc123", "abc123")]
	[InlineData(null, null)]
	public void RemoveTrailingNonAlphanumeric_RemovesCorrectly(string? input, string? expected)
	{
		// Act
		string? result = input.RemoveTrailingNonAlphanumeric();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2)]
	[InlineData("test", 't', 2)]
	[InlineData("", 'x', 0)]
	[InlineData(null, 'x', 0)]
	public void CountChars_CountsCorrectly(string? input, char charToFind, int expected)
	{
		// Act
		int result = input.CountChars(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(3.14, "3.14")]
	[InlineData(123, "123")]
	public void ToNString_HandlesNullableTypes(object? value, string? expected)
	{
		// Act & Assert
		if (value is int intVal)
		{
			intVal.ToNString().ShouldBe(expected);
		}
		else if (value is double doubleVal)
		{
			doubleVal.ToNString().ShouldBe(expected);
		}
		else
		{
			value.ToNString().ShouldBe(expected);
		}
	}

	[Theory]
	[InlineData("Hello World", "World", "Hello", true)]
	[InlineData("Hello World", "missing", "value", false)]
	public void ContainsInvariant_MultipleTexts(string input, string text1, string text2, bool expected)
	{
		// Arrange
		string[] textsToFind = new[] { text1, text2 };

		// Act
		bool result = input.ContainsInvariant(textsToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello World", "World", "Hello", true)]
	[InlineData("Hello World", "missing", "value", false)]
	public void SpanContainsInvariant_MultipleTexts(string input, string text1, string text2, bool expected)
	{
		// Arrange
		string[] textsToFind = new[] { text1, text2 };

		// Act
		bool result = input.AsSpan().ContainsInvariant(textsToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void GetHash_GeneratesCorrectHash()
	{
		// Arrange
		const string input = "test string";

		// Act
		string sha256Result = input.GetHash(EHashAlgorithm.SHA256);
		string sha512Result = input.GetHash(EHashAlgorithm.SHA512);

		// Assert
		sha256Result.Length.ShouldBe(64); // SHA256 produces 32 bytes = 64 hex chars
		sha512Result.Length.ShouldBe(128); // SHA512 produces 64 bytes = 128 hex chars
	}

	[Fact]
	public void NormalizeWhiteSpace_HandlesVariousWhitespace()
	{
		// Arrange
		const string input = "Hello   World\t\nTest";

		// Act
		string result = input.NormalizeWhiteSpace();

		// Assert
		result.ShouldBe("Hello World\nTest");
	}

	[Fact]
	public void NormalizeWhiteSpace_HandlesNullInput()
	{
		// Arrange
		const string? input = null;

		// Act
		string result = input.NormalizeWhiteSpace();

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("20230101", "yyyyMMdd", "MM/dd/yyyy", "01/01/2023")]
	[InlineData("2023-01-01", "yyyy-MM-dd", "yyyy.MM.dd", "2023.01.01")]
	[InlineData(null, "yyyy-MM-dd", "yyyy.MM.dd", null)]
	public void FormatDateString_FormatsCorrectly(string? input, string sourceFormat, string outputFormat, string? expected)
	{
		// Act
		string? result = input.FormatDateString(sourceFormat, outputFormat);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void UrlEncodeReadable_HandlesSpecialCharacters()
	{
		// Arrange
		const string input = "Hello World (test) * /";

		// Act
		string? result = input.UrlEncodeReadable(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("Hello World (test) * /");
	}

	[Fact]
	public void TrimObjectStrings_HandlesNestedObjects()
	{
		// Arrange
		TestObject testObj = new()
		{
			StringProp = "  Test  ",
			NestedObject = new() { InnerString = "  Nested  " }
		};

		// Act
		TestObject result = testObj.TrimObjectStrings(recursive: true);

		// Assert
		result.StringProp.ShouldBe("Test");
		result.NestedObject.InnerString.ShouldBe("Nested");
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void BoolToInt_ConvertsCorrectly(bool input, int expected)
	{
		// Act
		int result = input.BoolToInt();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("1", 1)]
	[InlineData("", null)]
	[InlineData(null, null)]
	[InlineData("invalid", null)]
	public void ToNInt_ParsesCorrectly(string? input, int? expected)
	{
		// Act
		int? result = input.ToNInt();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]  // String has 2 'l's, max is 2
	[InlineData("hello", 'l', 3, true)]  // String has 2 'l's, max is 3
	[InlineData("hello", 'l', 1, false)] // String has 2 'l's, max is 1
	[InlineData("test", 'x', 0, true)]   // String has 0 'x's, max is 0
	[InlineData("", 'a', 5, true)]       // Empty string always returns true
	[InlineData(null, 'a', 5, true)]     // Null string always returns true
	public void HasNoMoreThanNumberOfChars_CountsCorrectly(string? input, char charToFind, int maxNumberOfChars, bool expected)
	{
		// Act
		bool result = input.HasNoMoreThanNumberOfChars(charToFind, maxNumberOfChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", "l", 2, true)]  // String version test
	[InlineData("hello", "l", 3, true)]
	[InlineData("hello", "l", 1, false)]
	public void HasNoMoreThanNumberOfChars_StringOverload_CountsCorrectly(string input, string charToFind, int maxNumberOfChars, bool expected)
	{
		// Act
		bool result = input.HasNoMoreThanNumberOfChars(charToFind, maxNumberOfChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void HasNoMoreThanNumberOfChars_ThrowsOnInvalidArgs()
	{
		// Arrange
		const string input = "test";

		// Act & Assert
		Should.Throw<ArgumentException>(() => input.HasNoMoreThanNumberOfChars('t', -1));
		Should.Throw<InvalidDataException>(() => input.HasNoMoreThanNumberOfChars("tt", 1));
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]  // String has 2 'l's, min is 2
	[InlineData("hello", 'l', 3, false)] // String has 2 'l's, min is 3
	[InlineData("hello", 'l', 1, true)]  // String has 2 'l's, min is 1
	[InlineData("test", 'x', 0, true)]   // String has 0 'x's, min is 0
	[InlineData("test", 'x', 1, false)]  // String has 0 'x's, min is 1
	[InlineData("", 'a', 0, true)]       // Empty string returns true only if min is 0
	[InlineData("", 'a', 1, false)]      // Empty string returns false if min > 0
	[InlineData(null, 'a', 0, true)]     // Null string behaves like empty string
	[InlineData(null, 'a', 1, false)]    // Null string returns false if min > 0
	public void HasNoLessThanNumberOfChars_CountsCorrectly(string? input, char charToFind, int minNumberOfChars, bool expected)
	{
		// Act
		bool result = input.HasNoLessThanNumberOfChars(charToFind, minNumberOfChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", "l", 2, true)]  // String version test
	[InlineData("hello", "l", 3, false)]
	[InlineData("hello", "l", 1, true)]
	public void HasNoLessThanNumberOfChars_StringOverload_CountsCorrectly(string input, string charToFind, int minNumberOfChars, bool expected)
	{
		// Act
		bool result = input.HasNoLessThanNumberOfChars(charToFind, minNumberOfChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void HasNoLessThanNumberOfChars_ThrowsOnInvalidArgs()
	{
		// Arrange
		const string input = "test";

		// Act & Assert
		Should.Throw<ArgumentException>(() => input.HasNoLessThanNumberOfChars('t', -1));
		Should.Throw<ArgumentException>(() => input.HasNoLessThanNumberOfChars("t", -1));
		Should.Throw<InvalidDataException>(() => input.HasNoLessThanNumberOfChars("tt", 1));
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData(" ", false)]
	[InlineData("abc", false)]
	public void IsNullOrEmpty_String_Works(string? input, bool expected)
	{
		// Act
		bool result = input.IsNullOrEmpty();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData(new int[0], true)]
	[InlineData(new[] { 1 }, false)]
	public void IsNullOrEmpty_Enumerable_Works(int[]? input, bool expected)
	{
		// Act
		bool result = input.IsNullOrEmpty();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "he", true)]
	[InlineData("Hello", "LO", true)]
	[InlineData("Hello", "x", false)]
	[InlineData(null, "he", false)]
	[InlineData("Hello", null, false)]
	public void ContainsInvariant_String_Works(string? s, string? textToFind, bool expected)
	{
		// Act
		bool result = s.ContainsInvariant(textToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "he", true)]
	[InlineData("Hello", "He", true)]
	[InlineData("Hello", "lo", false)]
	[InlineData(null, "he", false)]
	[InlineData("Hello", null, false)]
	public void StartsWithInvariant_Works(string? s, string? textToFind, bool expected)
	{
		// Act
		bool result = s.StartsWithInvariant(textToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "LO", true)]
	[InlineData("Hello", "lo", true)]
	[InlineData("Hello", "he", false)]
	[InlineData(null, "lo", false)]
	[InlineData("Hello", null, false)]
	public void EndsWithInvariant_Works(string? s, string? textToFind, bool expected)
	{
		// Act
		bool result = s.EndsWithInvariant(textToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "l", 2)]
	[InlineData("Hello", "LO", 3)]
	[InlineData("Hello", "x", -1)]
	[InlineData(null, "l", -1)]
	[InlineData("Hello", null, -1)]
	public void IndexOfInvariant_String_Works(string? s, string? textToFind, int expected)
	{
		// Act
		int result = s.IndexOfInvariant(textToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", 'l', 2)]
	[InlineData("Hello", 'x', -1)]
	[InlineData(null, 'l', -1)]
	[InlineData("Hello", null, -1)]
	public void IndexOfInvariant_Char_Works(string? s, char? charToFind, int expected)
	{
		// Act
		int result = s.IndexOfInvariant(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc def", new[] { "abc", "def" }, true, true)]
	[InlineData("abc def", new[] { "abc", "xyz" }, true, true)]
	[InlineData("abc def", new[] { "abc", "xyz" }, false, false)]
	[InlineData("abc def", new[] { "abc", "def" }, false, true)]
	[InlineData(null, new[] { "abc" }, true, false)]
	public void Contains_MultipleStrings_Works(string? s, string[] search, bool useOr, bool expected)
	{
		// Act
		bool result = s.Contains(search, useOr);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc def", new[] { "abc", "def" }, true, true)]
	[InlineData("abc def", new[] { "abc", "xyz" }, true, true)]
	[InlineData("abc def", new[] { "abc", "xyz" }, false, false)]
	[InlineData("abc def", new[] { "abc", "def" }, false, true)]
	[InlineData(null, new[] { "abc" }, true, false)]
	public void ContainsSpan_MultipleStrings_Works(string? s, string[] search, bool useOr, bool expected)
	{
		// Act
		bool result = s.AsSpan().Contains(search, useOr);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello world", "aeiou", true)]        // Contains vowels
	[InlineData("hello world", "xyz", false)]         // No xyz characters
	[InlineData("test123", "0123456789", true)]       // Contains digits
	[InlineData("test", "0123456789", false)]         // No digits
	[InlineData("", "abc", false)]                    // Empty string
	[InlineData(null, "abc", false)]                  // Null string
	[InlineData("test", "", false)]                   // Empty character set
	public void ContainsAnyCharacter_String_Works(string? s, string characters, bool expected)
	{
		// Act
		bool result = s.ContainsAnyCharacter(characters.AsSpan());

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello world", "aeiou", true)]        // Contains vowels
	[InlineData("hello world", "xyz", false)]         // No xyz characters
	[InlineData("test123", "0123456789", true)]       // Contains digits
	[InlineData("test", "0123456789", false)]         // No digits
	[InlineData("", "abc", false)]                    // Empty span
	[InlineData("test", "", false)]                   // Empty character set
	public void ContainsAnyCharacter_Span_Works(string s, string characters, bool expected)
	{
		// Act
		bool result = s.AsSpan().ContainsAnyCharacter(characters.AsSpan());

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("user@domain.com", "@", true)]        // Contains @
	[InlineData("username", "@", false)]              // No @
	[InlineData("test<tag>", "<>", true)]             // Contains angle brackets
	[InlineData("normal text", "<>", false)]          // No angle brackets
	public void ContainsAnyCharacter_SingleChar_Works(string s, string characters, bool expected)
	{
		// Act
		bool result = s.ContainsAnyCharacter(characters.AsSpan());

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("test!@#", "!@#$%^&*()", true)]       // Contains special chars
	[InlineData("test", "!@#$%^&*()", false)]         // No special chars
	[InlineData("hello world", " \t\n\r", true)]      // Contains whitespace
	[InlineData("helloworld", " \t\n\r", false)]      // No whitespace
	public void ContainsAnyCharacter_MultipleChars_Works(string s, string characters, bool expected)
	{
		// Act - using span overload to differentiate from single char test
		bool result = s.AsSpan().ContainsAnyCharacter(characters.AsSpan());

		// Assert
		result.ShouldBe(expected);
		// Additional check: verify string overload gives same result
		s.ContainsAnyCharacter(characters.AsSpan()).ShouldBe(expected);
	}

	[Theory]
	[InlineData("café", "é", true)]                   // Unicode char present
	[InlineData("cafe", "é", false)]                  // Unicode char not present
	[InlineData("test™", "™®©", true)]                // Special unicode symbols
	[InlineData("test", "™®©", false)]                // No special symbols
	public void ContainsAnyCharacter_UnicodeChars_Works(string s, string characters, bool expected)
	{
		// Arrange - test both string and span overloads with unicode
		ReadOnlySpan<char> span = s.AsSpan();
		ReadOnlySpan<char> charSpan = characters.AsSpan();

		// Act
		bool result = span.ContainsAnyCharacter(charSpan);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Test", "test", true)]                // Case sensitive - matches on e, s, and t
	[InlineData("Test", "E", false)]                  // Case sensitive - no match
	[InlineData("Test", "T", true)]                   // Exact match
	[InlineData("hello", "HELLO", false)]             // Case sensitive - no match
	[InlineData("ABC", "abc", false)]                 // Case sensitive - no match
	public void ContainsAnyCharacter_CaseSensitive_Works(string s, string characters, bool expected)
	{
		// Act - verify case sensitivity using string extension method
		bool stringResult = s.ContainsAnyCharacter(characters.AsSpan());
		bool spanResult = s.AsSpan().ContainsAnyCharacter(characters.AsSpan());

		// Assert - both should behave the same
		stringResult.ShouldBe(expected);
		spanResult.ShouldBe(expected);
	}

	[Theory]
	[InlineData("a", "a", true)]                      // Single char, match
	[InlineData("a", "b", false)]                     // Single char, no match
	[InlineData("abc", "xyz", false)]                 // Multiple chars, no match
	[InlineData("abc", "cde", true)]                  // Multiple chars, partial match (c)
	public void ContainsAnyCharacter_EdgeCases_Works(string s, string characters, bool expected)
	{
		// Arrange - test minimum and boundary cases
		string testString = s;
		ReadOnlySpan<char> testChars = characters.AsSpan();

		// Act
		bool result = testString.ContainsAnyCharacter(testChars);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello World", "world", "Universe", true, "Hello Universe")]
	[InlineData("Hello World World", "world", "Universe", false, "Hello Universe World")]
	[InlineData(null, "world", "Universe", true, null)]
	public void ReplaceInvariant_Single_Works(string? s, string oldValue, string newValue, bool replaceAll, string? expected)
	{
		// Act
		string? result = s.ReplaceInvariant(oldValue, newValue, replaceAll, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello World", new[] { "Hello", "World" }, "X", true, "X X")]
	[InlineData("Hello World", new[] { "Hello" }, "X", true, "X World")]
	[InlineData(null, new[] { "Hello" }, "X", true, null)]
	[InlineData("", new[] { "test" }, "X", true, "")]
	[InlineData("test test test", new[] { "test" }, "X", false, "X test test")]
	[InlineData("Hello World", new[] { "", "Hello" }, "X", true, "X World")]
	[InlineData("Hello World", new[] { "hello", "world" }, "X", true, "X X")]
	public void ReplaceInvariant_Multiple_Works(string? s, string[] oldValues, string newValue, bool replaceAll, string? expected)
	{
		// Act
		string? result = s.ReplaceInvariant(oldValues, newValue, replaceAll, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ReplaceInvariant_RespectsCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		cts.Cancel();
		const string input = "word1 word2 word3 word4";
		string[] oldValues = ["word1", "word2", "word3", "word4"];

		// Act & Assert
		Should.Throw<OperationCanceledException>(() =>
			input.ReplaceInvariant(oldValues, "X", cancellationToken: cts.Token));
	}

	// StrComp (string?, string?)
	[Theory]
	[InlineData("abc", "abc", true)]
	[InlineData("abc", "ABC", false)]
	[InlineData(null, null, true)]
	[InlineData("", null, true)]
	[InlineData("abc", null, false)]
	[InlineData(null, "abc", false)]
	[InlineData(null, "", true)]
	public void StrComp_Default_Works(string? s1, string? s2, bool expected)
	{
		// Act
		bool result = s1.StrComp(s2);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc", "ABC", StringComparison.InvariantCultureIgnoreCase, true)]
	[InlineData("abc", "ABC", StringComparison.InvariantCulture, false)]
	[InlineData(null, null, StringComparison.InvariantCulture, true)]
	[InlineData("abc", null, StringComparison.InvariantCulture, false)]
	[InlineData(null, "abc", StringComparison.InvariantCulture, false)]
	[InlineData("", null, StringComparison.InvariantCulture, true)]
	[InlineData(null, "", StringComparison.InvariantCulture, true)]
	public void StrComp_Comparison_Works(string? s1, string? s2, StringComparison comparison, bool expected)
	{
		// Act
		bool result = s1.StrComp(s2, comparison);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("12345", false, true)]
	[InlineData("123 45", false, false)]
	[InlineData("123 45", true, true)]
	[InlineData("abc", false, false)]
	[InlineData(null, false, false)]
	public void IsNumericOnly_Works(string? input, bool allowSpaces, bool expected)
	{
		// Act
		bool result = input.IsNumericOnly(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc.def.ghi", '.', "abc.def")]
	[InlineData("abc", '.', "abc")]
	[InlineData(null, '.', null)]
	public void ExtractToLastInstance_Works(string? s, char c, string? expected)
	{
		// Act
		string? result = s.ExtractToLastInstance(c);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc.def.ghi", '.', "ghi")]
	[InlineData("abc", '.', "abc")]
	[InlineData(null, '.', null)]
	public void ExtractFromLastInstance_Works(string? s, char c, string? expected)
	{
		// Act
		string? result = s.ExtractFromLastInstance(c);

		// Assert
		result.ShouldBe(expected);
	}

	//private class TestTrimObj
	//{
	//	public string? Name { get; set; }

	//	public string? Desc { get; set; }
	//}

	//	[Fact]
	//	public void TrimObjectStringsR_TrimsStrings()
	//	{
	//		// Arrange
	//		TestTrimObj obj = new() { Name = "  test  ", Desc = "  desc  " };

	//		// Act
	//#pragma warning disable CS0618 // Type or member is obsolete
	//		TestTrimObj result = obj.TrimObjectStringsR();
	//#pragma warning restore CS0618 // Type or member is obsolete

	//		// Assert
	//		result!.Name.ShouldBe("test");
	//		result.Desc.ShouldBe("desc");
	//	}

	private class TestNormObj
	{
		public string? Name { get; set; }
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void NormalizeObjectStringsR_NormalizesStrings(bool enableTrim)
	{
		// Arrange
		TestNormObj obj = new() { Name = "  café  " };

		// Act
		TestNormObj? result = obj.NormalizeObjectStringsR(enableTrim, NormalizationForm.FormD);

		// Assert
		if (enableTrim)
		{
			result!.Name.ShouldBe("café");
		}
		else
		{
			result!.Name.ShouldBe("  café  ");
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void NormalizeObjectStrings_HandlesNull(bool inlineData)
	{
		// Arrange
		TestObject? testObject = null;

		// Act
		TestObject? result = testObject.NormalizeObjectStrings(inlineData, NormalizationForm.FormD, true);

		// Assert
		result.ShouldBeNull();
	}

	private class TestNullObj
	{
		public string? Name { get; set; }
	}

	[Fact]
	public void MakeObjectNullNullR_SetsNull()
	{
		// Arrange
		TestNullObj obj = new() { Name = " null " };

		// Act
		TestNullObj result = obj.MakeObjectNullNullR();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBeNull();
	}

	[Fact]
	public void MakeObjectNullNull_SetsNull()
	{
		// Arrange
		TestNullObj obj = new() { Name = " null " };

		// Act
		TestNullObj result = obj.MakeObjectNullNull();

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBeNull();
	}

	[Fact]
	public void MakeNullObjectNull_SetsNull()
	{
		// Arrange
		TestNullObj? obj = null;

		// Act
		TestNullObj? result = obj.MakeObjectNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ToNString_Overloads_Work()
	{
		// Arrange
		DateTime? dt = new DateTime(2024, 1, 2, 3, 4, 5);
		DateOnly? d = new DateOnly(2024, 1, 2);
		TimeSpan? ts = new TimeSpan(1, 2, 3);
		int? i = 42;
		long? l = 123456789L;
		double? dbl = 3.14;
		decimal? dec = 2.71m;
		bool? b = true;
		object? o = "test";
		int? ni = null;
		long? nl = null;
		double? ndbl = null;
		decimal? ndec = null;
		bool? nb = null;
		object? no = null;

		// Act & Assert
		dt.ToNString().ShouldBe(dt.ToString());
		d.ToNString().ShouldBe(d.ToString());
		ts.ToNString().ShouldBe(ts.ToString());
		i.ToNString().ShouldBe("42");
		l.ToNString().ShouldBe("123456789");
		dbl.ToNString().ShouldBe("3.14");
		dec.ToNString().ShouldBe("2.71");
		b.ToNString().ShouldBe("True");
		o.ToNString().ShouldBe("test");

		ni.ToNString().ShouldBe(null);
		nl.ToNString().ShouldBe(null);
		ndbl.ToNString().ShouldBe(null);
		ndec.ToNString().ShouldBe(null);
		nb.ToNString().ShouldBe(null);
		no.ToNString().ShouldBe(null);
		((object?)null).ToNString().ShouldBeNull();
	}

	[Fact]
	public void ToListInt_Overloads_Work()
	{
		// Arrange
		IEnumerable<string> enumerable = new[] { "1", "2", "3" };
		IEnumerable<string> enumerableStrings = new[] { "four", "five", "six" };
		IList<string> list = new List<string> { "4", "5", "6" };
		IList<string> listStrings = new List<string> { "four", "five", "six" };

		// Act
		IEnumerable<int> result1 = enumerable.ToListInt();
		List<int> result2 = list.ToListInt();
		List<int> result3 = listStrings.ToListInt();
		IEnumerable<int> result4 = enumerableStrings.ToListInt();

		// Assert
		result1.ShouldBe([1, 2, 3]);
		result2.ShouldBe(new List<int> { 4, 5, 6 });
		result3.ShouldBeEmpty();
		result4.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("3.14", 3.14)]
	[InlineData("not a number", null)]
	[InlineData(null, null)]
	public void ToNDouble_Works(string? input, double? expected)
	{
		// Act
		double? result = input.ToNDouble();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("2.71", 2.71)]
	[InlineData("not a number", null)]
	[InlineData(null, null)]
	public void ToNDecimal_Works(string? input, double? expected)
	{
		// Act
		decimal? result = input.ToNDecimal();

		// Assert
		result.ShouldBe((decimal?)expected);
	}

	[Theory]
	[InlineData("2024-01-02", 2024, 1, 2)]
	[InlineData(null, null, null, null)]
	public void ToNDateTime_Works(string? input, int? year, int? month, int? day)
	{
		// Act
		DateTime? result = input.ToNDateTime();

		// Assert
		if (year is null)
		{
			result.ShouldBeNull();
		}
		else
		{
			result.ShouldBe(new DateTime(year.Value, month!.Value, day!.Value));
		}
	}

	[Theory]
	[InlineData("2024-01-02", 2024, 1, 2)]
	[InlineData(null, null, null, null)]
	public void ToNDateOnly_Works(string? input, int? year, int? month, int? day)
	{
		// Act
		DateOnly? result = input.ToNDateOnly();

		// Assert
		if (year is null)
		{
			result.ShouldBeNull();
		}
		else
		{
			result.ShouldBe(new DateOnly(year.Value, month!.Value, day!.Value));
		}
	}

	[Theory]
	[InlineData("Yes", true)]
	[InlineData("No", false)]
	[InlineData("yes", true)]
	[InlineData("no", false)]
	[InlineData("  Yes  ", true)]
	[InlineData(null, false)]
	public void YesNoToBool_Works(string? input, bool expected)
	{
		// Act
		bool result = input.YesNoToBool();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Y", true)]
	[InlineData("N", false)]
	[InlineData("y", true)]
	[InlineData("n", false)]
	[InlineData("  Y  ", true)]
	[InlineData(null, false)]
	public void YNToBool_Works(string? input, bool expected)
	{
		// Act
		bool result = input.YNToBool();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(true, "Yes")]
	[InlineData(false, "No")]
	public void BoolToYesNo_Works(bool input, string expected)
	{
		// Act
		string result = input.BoolToYesNo();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(true, "Y")]
	[InlineData(false, "N")]
	public void BoolToYN_Works(bool input, string expected)
	{
		// Act
		string result = input.BoolToYN();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void GetSafeDate_Overloads_Work()
	{
		// Arrange
		const string format = "yyyyMMdd";
		DateTime? dt = DateTime.Now;
		DateOnly? d = DateOnly.FromDateTime(DateTime.Now);

		// Act
		string result1 = Strings.GetSafeDate(format);
		string result2 = dt.GetSafeDate(format);
		string result3 = d.GetSafeDate(format);
		string result4 = ((DateOnly?)null).GetSafeDate(format);
		string result5 = ((DateTime?)null).GetSafeDate(format);

		// Assert
		string.IsNullOrWhiteSpace(result1).ShouldBeFalse();
		string.IsNullOrWhiteSpace(result2).ShouldBeFalse();
		string.IsNullOrWhiteSpace(result3).ShouldBeFalse();
		string.IsNullOrWhiteSpace(result4).ShouldBeFalse();
		string.IsNullOrWhiteSpace(result5).ShouldBeFalse();
	}

	[Theory]
	[InlineData("png")]
	[InlineData(null)]
	public void MakeExportNameUnique_Works(string? extension)
	{
		// Arrange
		const string tempDir = "TestData";
		const string fileName = "test.png";
		const string ext = "png";

		// Act
		string unique = Strings.MakeExportNameUnique(tempDir, fileName, extension);

		// Assert
		unique.ShouldStartWith(fileName.Replace($".{ext}", string.Empty));
		unique.ShouldEndWith(ext);
	}

	[Theory]
	[InlineData(1, 2, 3, "02:03:00")]
	[InlineData(0, 0, 0, "00:00:00")]
	public void TimespanToShortForm_Nullable_Works(int days, int hours, int minutes, string expected)
	{
		// Arrange
		TimeSpan? ts = new TimeSpan(days, hours, minutes, 0);

		// Act
		string result = ts.TimespanToShortForm();

		// Assert
		result.ShouldContain(expected[..2]);
	}

	[Theory]
	[InlineData(1, 2, 3, "02:03:00")]
	[InlineData(0, 0, 0, "00:00:00")]
	public void TimespanToShortForm_Value_Works(int days, int hours, int minutes, string expected)
	{
		// Arrange
		TimeSpan ts = new(days, hours, minutes, 0);

		// Act
		string result = ts.TimespanToShortForm();

		// Assert
		result.ShouldContain(expected[..2]);
	}

	[Theory]
	[InlineData("abc123", @"\d+", "*", false, "*123")]
	[InlineData("abc123", "[a-z]+", "#", false, "abc#")]
	[InlineData(null, @"\d+", "*", false, null)]
	public void ReplaceInverse_String_Works(string? input, string pattern, string? replacement, bool matchFirstOnly, string? expected)
	{
		// Act
		string? result = input.ReplaceInverse(pattern, replacement, matchFirstOnly);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc123", @"\d+", "*", false, "*123")]
	[InlineData("abc123", "[a-z]+", "#", false, "abc#")]
	[InlineData(null, @"\d+", "*", false, null)]
	[InlineData("abc123", "[a-z]+", null, false, "abc")]
	[InlineData("abc123", @"\d+", "*", true, "*123")]
	[InlineData("abc123", "[a-z]+", "#", true, "abc#")]
	[InlineData(null, @"\d+", "*", true, null)]
	[InlineData("abc123", "[a-z]+", null, true, "abc")]
	public void ReplaceInverse_Regex_Works(string? input, string pattern, string? replacement, bool matchFirstOnly, string? expected)
	{
		// Arrange
		Regex regex = new(pattern);

		// Act
		string? result = regex.ReplaceInverse(input, replacement, matchFirstOnly);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(2.5, 3, "2 1/2")]
	[InlineData(3.25, 2, "3 1/4")]
	public void ToFractionString_Decimal_Works(decimal input, int maxDecimals, string expected)
	{
		// Act
		string? result = input.ToFractionString(maxDecimals);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(2.5, 3, "2 1/2")]
	[InlineData(3.25, 2, "3 1/4")]
	public void ToFractionString_Double_Works(double input, int maxDecimals, string expected)
	{
		// Act
		string? result = input.ToFractionString(maxDecimals);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToFractionString_NullableDecimal_Works()
	{
		// Arrange
		decimal? input = null;

		// Act
		string? result = input.ToFractionString(2);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ToFractionString_NullableDouble_Works()
	{
		// Arrange
		double? input = null;

		// Act
		string? result = input.ToFractionString(2);

		// Assert
		result.ShouldBeNull();
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, null, false)]
	[InlineData("bad", null, false)]
	public void TryFractionToDecimal_Nullable_Works(string? input, double? expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryFractionToDecimal(out decimal? result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe((decimal?)expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, 0.0, false)]
	[InlineData("bad", 0.0, false)]
	public void TryFractionToDecimal_Value_Works(string? input, double expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryFractionToDecimal(out decimal result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe((decimal)expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, null, false)]
	[InlineData("bad", null, false)]
	public void TryStringToDecimal_Nullable_Works(string? input, double? expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryStringToDecimal(out decimal? result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe((decimal?)expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, 0.0, false)]
	[InlineData("bad", 0.0, false)]
	public void TryStringToDecimal_Value_Works(string? input, double expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryStringToDecimal(out decimal result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe((decimal)expected);
	}

	[Theory]
	[InlineData("1/2", 0.5)]
	[InlineData("2", 2.0)]
	[InlineData("1 1/2", 1.5)]
	[InlineData(null, null)]
	public void FractionToDouble_Works(string? input, double? expected)
	{
		// Act
		double? result = input.FractionToDouble();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, null, false)]
	[InlineData("bad", null, false)]
	public void TryFractionToDouble_Nullable_Works(string? input, double? expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryFractionToDouble(out double? result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, 0.0, false)]
	[InlineData("bad", 0.0, false)]
	public void TryFractionToDouble_Value_Works(string? input, double expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryFractionToDouble(out double result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, null, false)]
	[InlineData("bad", null, false)]
	public void TryStringToDouble_Nullable_Works(string? input, double? expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryStringToDouble(out double? result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("1/2", 0.5, true)]
	[InlineData("2", 2.0, true)]
	[InlineData(null, 0.0, false)]
	[InlineData("bad", 0.0, false)]
	public void TryStringToDouble_Value_Works(string? input, double expected, bool expectedSuccess)
	{
		// Act
		bool success = input.TryStringToDouble(out double result);

		// Assert
		success.ShouldBe(expectedSuccess);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("!@#abc123", "abc123")]
	[InlineData("---test---", "test")]
	[InlineData("abc123", "abc123")]
	[InlineData(null, null)]
	public void TrimOuterNonAlphanumeric_Works(string? input, string? expected)
	{
		// Act
		string? result = input.TrimOuterNonAlphanumeric();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2)]
	[InlineData("test", 't', 2)]
	[InlineData("", 'x', 0)]
	public void CountChars_Span_Works(string input, char charToFind, int expected)
	{
		// Act
		int result = input.AsSpan().CountChars(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", "l", 2)]
	[InlineData("test", "t", 2)]
	[InlineData("", "x", 0)]
	public void CountChars_String_Works(string? input, string charToFind, int expected)
	{
		// Act
		int result = input.CountChars(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void CountChars_StringTooLong_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => "input".CountChars("XYZ"));
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]
	[InlineData("hello", 'l', 1, false)]
	[InlineData("", 'x', 0, true)]
	public void HasNoMoreThanNumberOfChars_Works(string input, char charToFind, int max, bool expected)
	{
		// Act
		bool result = input.AsSpan().HasNoMoreThanNumberOfChars(charToFind, max);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]
	[InlineData("hello", 'l', 3, false)]
	[InlineData("", 'x', 0, true)]
	public void HasNoLessThanNumberOfChars_Works(string input, char charToFind, int min, bool expected)
	{
		// Act
		bool result = input.AsSpan().HasNoLessThanNumberOfChars(charToFind, min);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void CacheManager_Property_ReturnsExpectedInstance()
	{
		// Act
		ICacheManagerApi<(Type, bool, NormalizationForm, bool), Delegate> cache = Strings.CacheManager;

		// Assert
		cache.ShouldNotBeNull();
		cache.ShouldBeAssignableTo<ICacheManagerApi<(Type, bool, NormalizationForm, bool), Delegate>>();
	}

	[Fact]
	public void NormalizeObjectStrings_UsesCacheAndBypassesCache()
	{
		// Arrange
		TestNormObj obj = new() { Name = "  café  " };

		// Act

		// First call: should populate the cache
		TestNormObj result1 = obj.NormalizeObjectStrings(true, NormalizationForm.FormD, recursive: false, useCache: true);

		// Second call: should hit the cache
		TestNormObj result2 = obj.NormalizeObjectStrings(true, NormalizationForm.FormD, recursive: false, useCache: true);

		// Third call: bypass cache
		TestNormObj result3 = obj.NormalizeObjectStrings(true, NormalizationForm.FormD, recursive: false, useCache: false);

		// Assert
		result1!.Name.ShouldBe("café");
		result2!.Name.ShouldBe("café");
		result3!.Name.ShouldBe("café");
	}

	[Fact]
	public void NormalizeObjectStrings_CacheManager_LimitedAndUnlimitedModes()
	{
		// Arrange
		ICacheManagerApi<(Type, bool, NormalizationForm, bool), Delegate> cacheManager = Strings.CacheManager;
		TestNormObj obj = new() { Name = "  café  " };
		(Type, bool, NormalizationForm FormD, bool) key = (typeof(TestNormObj), true, NormalizationForm.FormD, false);

		bool wasLimited = cacheManager.IsUsingLimitedCache(); // Save original mode

		//Act & Assert
		try
		{
			// Force unlimited mode
			if (wasLimited)
			{
				cacheManager.SetUseLimitedCache(false);
			}

			// Should add to unlimited cache
			obj.NormalizeObjectStrings(true, NormalizationForm.FormD, false, true);

			cacheManager.GetCache().ContainsKey(key).ShouldBeTrue();

			// Switch to limited mode
			cacheManager.SetUseLimitedCache(true);

			// Should add to limited cache
			obj.NormalizeObjectStrings(true, NormalizationForm.FormD, false, true);

			cacheManager.GetLimitedCache().ContainsKey(key).ShouldBeTrue();
		}
		finally
		{
			// Restore original mode
			cacheManager.SetUseLimitedCache(wasLimited);
		}
	}

	#region ReadOnlySpan<char> Overloads

	[Theory]
	[InlineData("Hello", 3, "Hel")]
	[InlineData("Test", 5, "Test")]
	[InlineData("ABC", 0, "")]
	[InlineData("", 1, "")]
	public void Left_Span_ReturnsCorrectSubstring(string input, int numChars, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.Left(numChars);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void Left_Span_Empty_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.Left(3);

		// Assert
		result.ToString().ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("Hello", 3, "llo")]
	[InlineData("Test", 5, "Test")]
	[InlineData("ABC", 0, "")]
	[InlineData("", 1, "")]
	public void Right_Span_ReturnsCorrectSubstring(string input, int numChars, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.Right(numChars);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void Right_Span_Empty_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.Right(3);

		// Assert
		result.ToString().ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("Start[Middle]End", "[", "]", "Middle")]
	[InlineData("NoDelimiters", "[", "]", "")]
	[InlineData("", "[", "]", "")]
	public void ExtractBetween_Span_ReturnsCorrectSubstring(string input, string start, string end, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.ExtractBetween(start, end);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void ExtractBetween_Span_Empty_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.ExtractBetween("[", "]");

		// Assert
		result.ToString().ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("PascalCaseTest", "Pascal Case Test")]
	[InlineData("camelCase", "camel Case")]
	[InlineData("ABC", "A B C")]
	[InlineData("", "")]
	public void ParsePascalCase_Span_CorrectlySeparatesWords(string input, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.ParsePascalCase();

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void ParsePascalCase_Span_Empty_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.ParsePascalCase();

		// Assert
		result.ToString().ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("abc123", true)]
	[InlineData("abc 123", true, true)]
	[InlineData("abc@123", false)]
	[InlineData("", false)]
	public void IsAlphanumeric_Span_ValidatesInput(string input, bool expected, bool allowSpaces = false)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		bool result = span.IsAlphanumeric(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abcDEF", true)]
	[InlineData("abc DEF", true, true)]
	[InlineData("abc123", false)]
	[InlineData("", false)]
	public void IsAlphaOnly_Span_ValidatesInput(string input, bool expected, bool allowSpaces = false)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		bool result = span.IsAlphaOnly(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("12345", false, true)]
	[InlineData("123 45", false, false)]
	[InlineData("123 45", true, true)]
	[InlineData("abc", false, false)]
	[InlineData("", false, false)]
	public void IsNumericOnly_Span_Works(string input, bool allowSpaces, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		bool result = span.IsNumericOnly(allowSpaces);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc.def.ghi", '.', "abc.def")]
	[InlineData("abc", '.', "abc")]
	[InlineData("", '.', "")]
	public void ExtractToLastInstance_Span_Works(string input, char c, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.ExtractToLastInstance(c);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc.def.ghi", '.', "ghi")]
	[InlineData("abc", '.', "abc")]
	[InlineData("", '.', "")]
	public void ExtractFromLastInstance_Span_Works(string input, char c, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.ExtractFromLastInstance(c);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]
	[InlineData("hello", 'l', 1, false)]
	[InlineData("", 'x', 0, true)]
	public void HasNoMoreThanNumberOfChars_Span_Works(string input, char charToFind, int max, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		bool result = span.HasNoMoreThanNumberOfChars(charToFind, max);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2, true)]
	[InlineData("hello", 'l', 3, false)]
	[InlineData("", 'x', 0, true)]
	public void HasNoLessThanNumberOfChars_Span_Works(string input, char charToFind, int min, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		bool result = span.HasNoLessThanNumberOfChars(charToFind, min);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "he", true)]
	[InlineData("Hello", "LO", true)]
	[InlineData("Hello", "x", false)]
	[InlineData("", "he", false)]
	[InlineData("Hello", "", false)]
	public void ContainsInvariant_Span_Works(string input, string textToFind, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;
		ReadOnlySpan<char> find = textToFind;

		// Act
		bool result = span.ContainsInvariant(find);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "he", true)]
	[InlineData("Hello", "He", true)]
	[InlineData("Hello", "lo", false)]
	[InlineData("", "he", false)]
	[InlineData("Hello", "", false)]
	public void StartsWithInvariant_Span_Works(string input, string textToFind, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;
		ReadOnlySpan<char> find = textToFind;

		// Act
		bool result = span.StartsWithInvariant(find);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "LO", true)]
	[InlineData("Hello", "lo", true)]
	[InlineData("Hello", "he", false)]
	[InlineData("", "lo", false)]
	[InlineData("Hello", "", false)]
	public void EndsWithInvariant_Span_Works(string input, string textToFind, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;
		ReadOnlySpan<char> find = textToFind;

		// Act
		bool result = span.EndsWithInvariant(find);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", "l", 2)]
	[InlineData("Hello", "LO", 3)]
	[InlineData("Hello", "x", -1)]
	[InlineData("", "l", -1)]
	[InlineData("Hello", "", -1)]
	public void IndexOfInvariant_Span_Works(string input, string textToFind, int expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;
		ReadOnlySpan<char> find = textToFind;

		// Act
		int result = span.IndexOfInvariant(find);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("Hello", 'l', 2)]
	[InlineData("Hello", 'x', -1)]
	[InlineData("", 'l', -1)]
	public void IndexOfInvariant_Char_Span_Works(string input, char charToFind, int expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		int result = span.IndexOfInvariant(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello", 'l', 2)]
	[InlineData("test", 't', 2)]
	[InlineData("", 'x', 0)]
	public void CountChars_Span_Char_Works(string input, char charToFind, int expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		int result = span.CountChars(charToFind);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("20230101", "yyyyMMdd", "MM/dd/yyyy", "01/01/2023")]
	[InlineData("2023-01-01", "yyyy-MM-dd", "yyyy.MM.dd", "2023.01.01")]
	public void FormatDateString_Span_Works(string input, string sourceFormat, string outputFormat, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.FormatDateString(sourceFormat, outputFormat);

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void FormatDateString_Span_Empty_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.FormatDateString("yyyyMMdd", "MM/dd/yyyy");

		// Assert
		result.ToString().ShouldBe(string.Empty);
	}

	[Theory]
	[InlineData("123", 123)]
	[InlineData("notanint", null)]
	[InlineData("", null)]
	public void ToNInt_Span_Works(string input, int? expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		int? result = span.ToNInt();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("3.14", 3.14)]
	[InlineData("notanumber", null)]
	[InlineData("", null)]
	public void ToNDouble_Span_Works(string input, double? expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		double? result = span.ToNDouble();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("2.71", 2.71)]
	[InlineData("notanumber", null)]
	[InlineData("", null)]
	public void ToNDecimal_Span_Works(string input, double? expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		decimal? result = span.ToNDecimal();

		// Assert
		result.ShouldBe((decimal?)expected);
	}

	[Theory]
	[InlineData("2024-01-02", 2024, 1, 2)]
	[InlineData("", null, null, null)]
	public void ToNDateTime_Span_Works(string input, int? year, int? month, int? day)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		DateTime? result = span.ToNDateTime();

		// Assert
		if (year is null)
		{
			result.ShouldBeNull();
		}
		else
		{
			result.ShouldBe(new DateTime(year.Value, month!.Value, day!.Value, 0, 0, 0, DateTimeKind.Unspecified));
		}
	}

	[Theory]
	[InlineData("2024-01-02", 2024, 1, 2)]
	[InlineData("", null, null, null)]
	public void ToNDateOnly_Span_Works(string input, int? year, int? month, int? day)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		DateOnly? result = span.ToNDateOnly();

		// Assert
		if (year is null)
		{
			result.ShouldBeNull();
		}
		else
		{
			result.ShouldBe(new DateOnly(year.Value, month!.Value, day!.Value));
		}
	}

	[Theory]
	[InlineData("!@#abc123", "abc123")]
	[InlineData("---test", "test")]
	[InlineData("abc123", "abc123")]
	[InlineData("", "")]
	public void RemoveLeadingNonAlphanumeric_Span_Works(string input, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.RemoveLeadingNonAlphanumeric();

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc123!@#", "abc123")]
	[InlineData("test---", "test")]
	[InlineData("abc123", "abc123")]
	[InlineData("", "")]
	public void RemoveTrailingNonAlphanumeric_Span_Works(string input, string expected)
	{
		// Arrange
		ReadOnlySpan<char> span = input;

		// Act
		ReadOnlySpan<char> result = span.RemoveTrailingNonAlphanumeric();

		// Assert
		result.ToString().ShouldBe(expected);
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_NullCollection_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?>? collection = null;

		// Act
		bool result = collection.ContainsInvariant("test");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_NullCollection_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?>? collection = null;

		// Act
		bool result = collection.ContainsInvariant("test".AsSpan());

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_EmptyCollection_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = [];

		// Act
		bool result = collection.ContainsInvariant("test");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_EmptyCollection_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = [];

		// Act
		bool result = collection.ContainsInvariant("test".AsSpan());

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_ContainsExactMatch_ReturnsTrue()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana", "cherry"];

		// Act
		bool result = collection.ContainsInvariant("banana");

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_ContainsExactMatch_ReturnsTrue()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana", "cherry"];

		// Act
		bool result = collection.ContainsInvariant("banana".AsSpan());

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_ContainsCaseInsensitiveMatch_ReturnsTrue()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "BANANA", "cherry"];

		// Act
		bool result = collection.ContainsInvariant("banana");

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_DoesNotContain_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana", "cherry"];

		// Act
		bool result = collection.ContainsInvariant("date");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_DoesNotContain_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana", "cherry"];

		// Act
		bool result = collection.ContainsInvariant("date".AsSpan());

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_ContainsNulls_ReturnsTrueIfMatchExists()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", null, "banana"];

		// Act
		bool result = collection.ContainsInvariant("banana");

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_ContainsNulls_ReturnsTrueIfMatchExists()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", null, "banana"];

		// Act
		bool result = collection.ContainsInvariant("banana".AsSpan());

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_ContainsNulls_ReturnsFalseIfNoMatch()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", null, "banana"];

		// Act
		bool result = collection.ContainsInvariant("date");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void SpanContainsInvariant_IEnumerableString_ContainsNulls_ReturnsFalseIfNoMatch()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", null, "banana"];

		// Act
		bool result = collection.ContainsInvariant("date".AsSpan());

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_SearchTextIsNull_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana"];

		// Act
		bool result = collection.ContainsInvariant(null);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariantSpan_IEnumerableString_SearchSpanIsEmpty_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = ["apple", "banana"];

		// Act
		bool result = collection.ContainsInvariant(new Span<char>());

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_AllNulls_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = [null, null];

		// Act
		bool result = collection.ContainsInvariant("banana");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariantSpan_IEnumerableString_AllNulls_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = [null, null];

		// Act
		bool result = collection.ContainsInvariant("banana".AsSpan());

		// Assert
		result.ShouldBeFalse();
	}

	private class Nested
	{
		public string? Value { get; set; }
	}

	private class Container
	{
		public string? Name { get; set; }

		public Nested? NestedObj { get; set; }
	}

	[Fact]
	public void MakeObjectNullNull_Recursive_SetsNestedStringPropertyToNull()
	{
		Container obj = new()
		{
			Name = "null",
			NestedObj = new Nested { Value = "null" }
		};

		// Act: recursive = true triggers the non-string property path
		Container result = obj.MakeObjectNullNull(recursive: true);

		result.ShouldNotBeNull();
		result.Name.ShouldBeNull(); // Top-level string property
		result.NestedObj.ShouldNotBeNull();
		result.NestedObj!.Value.ShouldBeNull(); // Nested string property
	}

	[Fact]
	public void MakeObjectNullNull_Recursive_DoesNotChangeNonNullNestedString()
	{
		Container obj = new()
		{
			Name = "null",
			NestedObj = new Nested { Value = "not null" }
		};

		Container result = obj.MakeObjectNullNull(recursive: true);

		result.ShouldNotBeNull();
		result.Name.ShouldBeNull();
		result.NestedObj.ShouldNotBeNull();
		result.NestedObj!.Value.ShouldBe("not null");
	}

	[Fact]
	public void MakeObjectNullNull_Recursive_NullNestedObject_DoesNotThrow()
	{
		Container obj = new()
		{
			Name = "null",
			NestedObj = null
		};

		Container result = obj.MakeObjectNullNull(recursive: true);

		result.ShouldNotBeNull();
		result.Name.ShouldBeNull();
		result.NestedObj.ShouldBeNull();
	}

	[Fact]
	public void ParsePascalCase_Span_HandlesEmptySpan()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.ParsePascalCase();

		// Assert
		result.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_WithString_ExactMatch_ReturnsTrue()
	{
		// Arrange
		IEnumerable<string?> collection = new[] { "hello", "world", "test" };

		// Act
		bool result = collection.ContainsInvariant("TEST");

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_WithSpan_EmptySpan_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = new[] { "hello", "world" };
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		bool result = collection.ContainsInvariant(span);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_WithSpan_Match_ReturnsTrue()
	{
		// Arrange
		IEnumerable<string?> collection = new[] { "hello", "world", "test" };
		ReadOnlySpan<char> span = "TEST".AsSpan();

		// Act
		bool result = collection.ContainsInvariant(span);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsInvariant_IEnumerableString_WithSpan_NoMatch_ReturnsFalse()
	{
		// Arrange
		IEnumerable<string?> collection = new[] { "hello", "world" };
		ReadOnlySpan<char> span = "missing".AsSpan();

		// Act
		bool result = collection.ContainsInvariant(span);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData("hello world", new[] { "hello", "world" }, true, true)]
	[InlineData("hello world", new[] { "hello", "missing" }, true, true)]
	[InlineData("hello world", new[] { "missing" }, true, false)]
	[InlineData("hello world", new[] { "hello", "world" }, false, true)]
	[InlineData("hello world", new[] { "hello", "missing" }, false, false)]
	[InlineData("", new[] { "test" }, true, false)]
	public void ContainsInvariant_String_WithCollection_Works(string s, string[] textsToFind, bool useOr, bool expected)
	{
		// Act
		bool result = s.ContainsInvariant(textsToFind, useOr);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("hello world", new[] { "hello", "world" }, true, true)]
	[InlineData("hello world", new[] { "hello", "missing" }, true, true)]
	[InlineData("hello world", new[] { "missing" }, true, false)]
	[InlineData("hello world", new[] { "hello", "world" }, false, true)]
	[InlineData("hello world", new[] { "hello", "missing" }, false, false)]
	public void ContainsInvariant_Span_WithCollection_Works(string s, string[] textsToFind, bool useOr, bool expected)
	{
		// Arrange
		ReadOnlySpan<char> span = s.AsSpan();

		// Act
		bool result = span.ContainsInvariant(textsToFind, useOr);

		// Assert
		result.ShouldBe(expected);
	}





	[Fact]
	public void ContainsAnyCharacter_String_EmptyCharacters_ReturnsFalse()
	{
		// Act
		bool result = "hello".ContainsAnyCharacter(ReadOnlySpan<char>.Empty);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsAnyCharacter_Span_EmptyCharacters_ReturnsFalse()
	{
		// Arrange
		ReadOnlySpan<char> span = "hello".AsSpan();

		// Act
		bool result = span.ContainsAnyCharacter(ReadOnlySpan<char>.Empty);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ToTitleCase_HandlesNonWordCharacters()
	{
		// Arrange
		const string input = "hello-world...test";

		// Act
		string? result = input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("Hello-World...Test");
	}

	[Fact]
	public void ToTitleCase_HandlesEmptyWords()
	{
		// Arrange
		const string input = "  hello   world  ";

		// Act
		string? result = input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldContain("Hello");
		result.ShouldContain("World");
	}

	[Fact]
	public void ToTitleCase_ConvertByLength_KeepsShortUppercaseWords()
	{
		// Arrange
		const string input = "THE quick BROWN fox";

		// Act
		string? result = input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertByLength, minLengthToConvert: 5, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("THE Quick Brown Fox");
	}

	[Fact]
	public void ToTitleCase_HandlesMixedCase()
	{
		// Arrange
		const string input = "hELLo WoRLD";

		// Act
		string? result = input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("Hello World");
	}

	[Fact]
	public void ReplaceInvariant_MultipleOldValues_NoMatch_ReturnsOriginal()
	{
		// Arrange
		const string input = "hello world";
		string[] oldValues = ["missing", "notfound"];

		// Act
		string? result = input.ReplaceInvariant(oldValues, "X", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(input);
	}

	[Fact]
	public void ReplaceInvariant_MultipleOldValues_SingleMatch_ReplacesOnce()
	{
		// Arrange
		const string input = "hello world hello";
		string[] oldValues = ["hello"];

		// Act
		string? result = input.ReplaceInvariant(oldValues, "X", replaceAllInstances: false, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("X world hello");
	}

	[Fact]
	public void ExtractBetween_OverlappingDelimiters_ExtractsCorrectly()
	{
		// Arrange
		const string input = "[[inner]]";

		// Act
		string? result = input.ExtractBetween("[", "]");

		// Assert
		result.ShouldBe("[inner]");
	}

	[Fact]
	public void ExtractBetween_Span_OverlappingDelimiters_ExtractsCorrectly()
	{
		// Arrange
		ReadOnlySpan<char> span = "[[inner]]".AsSpan();

		// Act
		ReadOnlySpan<char> result = span.ExtractBetween("[", "]");

		// Assert
		result.ToString().ShouldBe("[inner]");
	}

	[Fact]
	public void ExtractFromLastInstance_NoMatch_ReturnsOriginal()
	{
		// Arrange
		const string input = "hello";

		// Act
		string? result = input.ExtractFromLastInstance('x');

		// Assert
		result.ShouldBe(input);
	}



	[Fact]
	public void ExtractFromLastInstance_Span_NoMatch_ReturnsOriginal()
	{
		// Arrange
		ReadOnlySpan<char> span = "hello".AsSpan();

		// Act
		ReadOnlySpan<char> result = span.ExtractFromLastInstance('x');

		// Assert
		result.ToString().ShouldBe("hello");
	}



	[Fact]
	public void ExtractToLastInstance_Span_EmptySpan_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.ExtractToLastInstance('.');

		// Assert
		result.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void ExtractFromLastInstance_Span_EmptySpan_ReturnsEmpty()
	{
		// Arrange
		ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;

		// Act
		ReadOnlySpan<char> result = span.ExtractFromLastInstance('.');

		// Assert
		result.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void MakeNullNull_MultipleNullsWithSpaces_ReturnsOriginal()
	{
		// Arrange
		const string input = "NULL  NULL";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBe("NULL  NULL");
	}

	[Fact]
	public void MakeNullNull_MixedCaseNullRepeated_ReturnsNull()
	{
		// Arrange
		const string input = "NuLlNuLl";

		// Act
		string? result = input.MakeNullNull();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ParsePascalCase_Span_SingleUppercaseChar()
	{
		// Arrange
		ReadOnlySpan<char> input = "A".AsSpan();

		// Act
		ReadOnlySpan<char> result = input.ParsePascalCase();

		// Assert
		result.ToString().ShouldBe("A");
	}

	[Fact]
	public void ToTitleCase_ConvertByLength_SingleCharUppercase()
	{
		// Arrange
		const string input = "THE A FOX";

		// Act
		string? result = input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertByLength, minLengthToConvert: 3, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("The A Fox");
	}

	[Fact]
	public void ToTitleCase_NonWordCharacterPreservation()
	{
		// Arrange
		const string input = "hello-world_test";

		// Act
		string? result = input.ToTitleCase(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("Hello-World_Test");
	}

	[Fact]
	public void ContainsInvariant_AndComparison_NotAllTextsFound()
	{
		// Arrange
		const string input = "Hello World";
		string[] texts = ["Hello", "Missing"];

		// Act
		bool result = input.ContainsInvariant(texts, useOrComparison: false);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ContainsInvariant_Span_AndComparison_FirstMatchSecondNot()
	{
		// Arrange
		ReadOnlySpan<char> input = "Hello World".AsSpan();
		string[] texts = ["Hello", "Missing"];

		// Act
		bool result = input.ContainsInvariant(texts, useOrComparison: false);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void Contains_Span_WithOrdinalIgnoreCase()
	{
		// Arrange
		ReadOnlySpan<char> input = "Hello World".AsSpan();
		string[] texts = ["HELLO", "WORLD"];

		// Act
		bool result = input.Contains(texts, stringComparison: StringComparison.OrdinalIgnoreCase);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ContainsAnyCharacter_String_EmptyCharacters()
	{
		// Arrange
		const string input = "hello";

		// Act
		bool result = input.ContainsAnyCharacter(ReadOnlySpan<char>.Empty);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ReplaceInvariant_SingleInstance_OnlyReplacesFirst()
	{
		// Arrange
		const string input = "cat cat cat";

		// Act
		string? result = input.ReplaceInvariant("cat", "dog", replaceAllInstances: false, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("dog cat cat");
	}

	[Fact]
	public void ReplaceInvariant_MultipleOldValues_WithNullOrEmpty()
	{
		// Arrange
		const string input = "hello world test";
		string[] oldValues = ["", "world"];

		// Act
		string? result = input.ReplaceInvariant(oldValues, "REPLACED", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe("hello REPLACED test");
	}

	[Fact]
	public void ExtractBetween_Span_ToExactEnd()
	{
		// Arrange
		ReadOnlySpan<char> input = "start[content]".AsSpan();

		// Act
		ReadOnlySpan<char> result = input.ExtractBetween("[", "]");

		// Assert
		result.ToString().ShouldBe("content");
	}

	[Fact]
	public void ExtractFromLastInstance_Span_CharNotFound()
	{
		// Arrange
		ReadOnlySpan<char> input = "hello world".AsSpan();

		// Act
		ReadOnlySpan<char> result = input.ExtractFromLastInstance('x');

		// Assert
		result.ToString().ShouldBe("hello world");
	}

	[Fact]
	public void ExtractToLastInstance_Span_CharNotFound()
	{
		// Arrange
		ReadOnlySpan<char> input = "hello world".AsSpan();

		// Act
		ReadOnlySpan<char> result = input.ExtractToLastInstance('x');

		// Assert
		result.ToString().ShouldBe("hello world");
	}

	[Fact]
	public void StrEq_FirstIsNotNull_SecondIsNull()
	{
		// Arrange
		const string s1 = "test";

		// Act
		bool result = s1.StrEq(null);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void StrEq_FirstIsNull_SecondIsNotNull()
	{
		// Arrange
		const string? s1 = null;

		// Act
		bool result = s1.StrEq("test");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ToNDateTime_WithOADateValue()
	{
		// Arrange
		const string oaDate = "44927"; // Represents 2023-01-01

		// Act
		DateTime? result = oaDate.ToNDateTime();

		// Assert
		result.ShouldNotBeNull();
		result?.Year.ShouldBe(2023);
	}

	[Fact]
	public void ToNDateTime_Span_WithOADateValue()
	{
		// Arrange
		ReadOnlySpan<char> oaDate = "44927".AsSpan(); // Represents 2023-01-01

		// Act
		DateTime? result = oaDate.ToNDateTime();

		// Assert
		result.ShouldNotBeNull();
		result?.Year.ShouldBe(2023);
	}

	[Fact]
	public void ToNDateOnly_WithOADateValue()
	{
		// Arrange
		const string oaDate = "44927"; // Represents 2023-01-01

		// Act
		DateOnly? result = oaDate.ToNDateOnly();

		// Assert
		result.ShouldNotBeNull();
		result?.Year.ShouldBe(2023);
	}

	[Fact]
	public void ToNDateOnly_Span_WithOADateValue()
	{
		// Arrange
		ReadOnlySpan<char> oaDate = "44927".AsSpan(); // Represents 2023-01-01

		// Act
		DateOnly? result = oaDate.ToNDateOnly();

		// Assert
		result.ShouldNotBeNull();
		result?.Year.ShouldBe(2023);
	}

	[Fact]
	public void TimespanToShortForm_WithMilliseconds()
	{
		// Arrange
		TimeSpan t = TimeSpan.FromMilliseconds(1234.567);

		// Act
		string result = t.TimespanToShortForm();

		// Assert
		result.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void TimespanToShortForm_WithDaysAndLeadingZero()
	{
		// Arrange
		TimeSpan t = new(1, 0, 5, 30); // 1 day, 5 minutes, 30 seconds

		// Act
		string result = t.TimespanToShortForm();

		// Assert
		result.ShouldContain("1:");
	}

	[Fact]
	public void TimespanToShortForm_NoHours()
	{
		// Arrange
		TimeSpan t = TimeSpan.FromMinutes(5.5); // 0 hours, 5 minutes, 30 seconds

		// Act
		string result = t.TimespanToShortForm();

		// Assert
		result.ShouldNotStartWith("00:");
	}

	[Fact]
	public void GetHash_AllAlgorithms()
	{
		// Arrange
		const string input = "test string";

		// Act & Assert
		input.GetHash(EHashAlgorithm.SHA1).ShouldNotBeNullOrEmpty();
		input.GetHash(EHashAlgorithm.SHA256).ShouldNotBeNullOrEmpty();
		input.GetHash(EHashAlgorithm.SHA384).ShouldNotBeNullOrEmpty();
		input.GetHash(EHashAlgorithm.MD5).ShouldNotBeNullOrEmpty();
		input.GetHash(EHashAlgorithm.SHA512).ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void UrlEncodeReadable_WithCustomEscapeSequences()
	{
		// Arrange
		const string input = "hello world/test";
		List<KeyValuePair<string, string>> customSequences = [new("%20", "_")];

		// Act
		string? result = input.UrlEncodeReadable(customSequences, appendDefaultEscapeSequences: false, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void UrlEncodeReadable_AppendDefaultEscapeSequences()
	{
		// Arrange
		const string input = "hello world/test";
		List<KeyValuePair<string, string>> customSequences = [new("%21", "!")];

		// Act
		string? result = input.UrlEncodeReadable(customSequences, appendDefaultEscapeSequences: true, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void FormatPhoneNumber_SevenDigits()
	{
		// Arrange
		const string input = "1234567";

		// Act
		string? result = input.FormatPhoneNumber();

		// Assert
		result.ShouldBe("123-4567");
	}

	[Fact]
	public void FormatPhoneNumber_TenDigitsWithParens()
	{
		// Arrange
		const string input = "1234567890";

		// Act
		string? result = input.FormatPhoneNumber(addParenToAreaCode: true);

		// Assert
		result.ShouldBe("(123)-456-7890");
	}

	[Fact]
	public void FormatPhoneNumber_ElevenDigits()
	{
		// Arrange
		const string input = "11234567890";

		// Act
		string? result = input.FormatPhoneNumber();

		// Assert
		result.ShouldBe("+1 123-456-7890");
	}

	[Fact]
	public void FormatPhoneNumber_TwelveDigits()
	{
		// Arrange
		const string input = "121234567890";

		// Act
		string? result = input.FormatPhoneNumber();

		// Assert
		result.ShouldBe("+12 123-456-7890");
	}

	[Fact]
	public void FormatPhoneNumber_WithExtension()
	{
		// Arrange
		const string input = "1234567x123";

		// Act
		string? result = input.FormatPhoneNumber();

		// Assert
		result.ShouldBe("123-4567x123");
	}

	[Fact]
	public void FormatPhoneNumber_InvalidLength()
	{
		// Arrange
		const string input = "12345";

		// Act
		string? result = input.FormatPhoneNumber();

		// Assert
		result.ShouldBe("12345");
	}

	[Theory]
	[InlineData("abcd", true, "ab cd")]
	[InlineData("ab\ncd", true, "ab  cd")]
	[InlineData("ab\rcd", true, "ab  cd")]
	[InlineData("ab\n\rcd", true, "ab   cd")]
	[InlineData("abcd", false, "abcd")]
	[InlineData("ab\ncd", false, "ab cd")]
	[InlineData("ab\rcd", false, "ab cd")]
	[InlineData("ab\n\rcd", false, "ab  cd")]
	public void SanitizeForLog_ShouldWork(string s, bool insertNewLine, string expected)
	{
		// Arrange
		if (insertNewLine)
		{
			s = $"{s[..2]}{Environment.NewLine}{s[2..]}";
		}

		// Act
		string? result = s.SanitizeForLog();

		// Assert
		result.ShouldBe(expected);
	}
}

	#endregion
