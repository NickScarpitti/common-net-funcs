using System.Text;
using CommonNetFuncs.Core;

namespace Core.Tests;

public class StringsTests
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
    public void MakeNullNull_HandlesNullStrings(string? input, string? expected)
    {
        // Act
        string? result = input.MakeNullNull();

        // Assert
        result.ShouldBe(expected);
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
    public void ContainsInvariant_MultipleStrings_HandlesLogicalOperations(string input, string[] searchTexts, bool useOrComparison, bool expected)
    {
        // Act
        bool result = input.ContainsInvariant(searchTexts, useOrComparison);

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
        string? result = input.ReplaceInvariant(oldValue, newValue);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("string1", "string1", true)]
    [InlineData("STRING1", "string1", true)]
    [InlineData("string1", "string2", false)]
    [InlineData(null, null, true)]
    [InlineData("", null, true)]
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
        input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertAllUppercase).ShouldBe("The Quick Brown Fox");

        input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.IgnoreUppercase).ShouldBe("THE QUICK BROWN FOX");

        input.ToTitleCase(uppercaseHandling: TitleCaseUppercaseWordHandling.ConvertByLength, minLengthToConvert: 4).ShouldBe("THE Quick Brown FOX");
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

    public class TestObject
    {
        public string? StringProp { get; set; }

        public NestedObject NestedObject { get; set; } = new();

        public string? StringPropWithSpaces { get; set; }

        public NestedObject NestedStringPropWithSpaces { get; set; } = new();
    }

    public class NestedObject
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
        IEnumerable<string> lines = input.SplitLines();

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

    [Theory]
    [InlineData("20230101", "yyyyMMdd", "MM/dd/yyyy", "01/01/2023")]
    [InlineData("2023-01-01", "yyyy-MM-dd", "yyyy.MM.dd", "2023.01.01")]
    public void FormatDateString_FormatsCorrectly(string input, string sourceFormat, string outputFormat, string expected)
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
        string? result = input.UrlEncodeReadable();

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
}
