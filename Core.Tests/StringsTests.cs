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
    [InlineData("", new[] { "quick", "missing" }, false, false)]
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello", "l", 2)]
    [InlineData("Hello", "LO", 3)]
    [InlineData("Hello", "x", -1)]
    [InlineData(null, "l", 0)]
    [InlineData("Hello", null, 0)]
    public void IndexOfInvariant_String_Works(string? s, string? textToFind, int expected)
    {
        // Act
        int result = s.IndexOfInvariant(textToFind);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello", 'l', 2)]
    [InlineData("Hello", 'x', -1)]
    [InlineData(null, 'l', 0)]
    public void IndexOfInvariant_Char_Works(string? s, char? charToFind, int expected)
    {
        // Act
        int result = s.IndexOfInvariant(charToFind);

        // Assert
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", "world", "Universe", true, "Hello Universe")]
    [InlineData("Hello World World", "world", "Universe", false, "Hello Universe World")]
    [InlineData(null, "world", "Universe", true, null)]
    public void ReplaceInvariant_Single_Works(string? s, string oldValue, string newValue, bool replaceAll, string? expected)
    {
        // Act
        string? result = s.ReplaceInvariant(oldValue, newValue, replaceAll);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", new[] { "Hello", "World" }, "X", true, "X X")]
    [InlineData("Hello World", new[] { "Hello" }, "X", true, "X World")]
    [InlineData(null, new[] { "Hello" }, "X", true, null)]
    public void ReplaceInvariant_Multiple_Works(string? s, string[] oldValues, string newValue, bool replaceAll, string? expected)
    {
        // Act
        string? result = s.ReplaceInvariant(oldValues, newValue, replaceAll);

        // Assert
        Assert.Equal(expected, result);
    }

    // StrComp (string?, string?)
    [Theory]
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "ABC", false)]
    [InlineData(null, null, true)]
    [InlineData("", null, true)]
    [InlineData("abc", null, false)]
    public void StrComp_Default_Works(string? s1, string? s2, bool expected)
    {
        // Act
        bool result = s1.StrComp(s2);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc", "ABC", StringComparison.InvariantCultureIgnoreCase, true)]
    [InlineData("abc", "ABC", StringComparison.InvariantCulture, false)]
    [InlineData(null, null, StringComparison.InvariantCulture, true)]
    public void StrComp_Comparison_Works(string? s1, string? s2, StringComparison comparison, bool expected)
    {
        // Act
        bool result = s1.StrComp(s2, comparison);

        // Assert
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }

    private class TestTrimObj
    {
        public string? Name { get; set; }

        public string? Desc { get; set; }
    }

    [Fact]
    public void TrimObjectStringsR_TrimsStrings()
    {
        // Arrange
        TestTrimObj obj = new() { Name = "  test  ", Desc = "  desc  " };

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        TestTrimObj result = obj.TrimObjectStringsR();
        #pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        Assert.Equal("test", result!.Name);
        Assert.Equal("desc", result.Desc);
    }

    private class TestNormObj
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void NormalizeObjectStringsR_NormalizesStrings()
    {
        // Arrange
        TestNormObj obj = new() { Name = "  café  " };

        // Act
        TestNormObj? result = obj.NormalizeObjectStringsR(true, NormalizationForm.FormD);

        // Assert
        Assert.Equal("café", result!.Name); // decomposed form
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
        Assert.Null(result!.Name);
    }

    [Fact]
    public void MakeObjectNullNull_SetsNull()
    {
        // Arrange
        TestNullObj obj = new() { Name = " null " };

        // Act
        TestNullObj result = obj.MakeObjectNullNull();

        // Assert
        Assert.Null(result!.Name);
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

        // Act & Assert
        Assert.Equal(dt.Value.ToString(), dt.ToNString());
        Assert.Equal(d.Value.ToString(), d.ToNString());
        Assert.Equal(ts.Value.ToString(), ts.ToNString());
        Assert.Equal("42", i.ToNString());
        Assert.Equal("123456789", l.ToNString());
        Assert.Equal("3.14", dbl.ToNString());
        Assert.Equal("2.71", dec.ToNString());
        Assert.Equal("True", b.ToNString());
        Assert.Equal("test", o.ToNString());
        Assert.Null(((object?)null).ToNString());
    }

    [Fact]
    public void ToListInt_Overloads_Work()
    {
        // Arrange
        IEnumerable<string> enumerable = new[] { "1", "2", "3" };
        IList<string> list = new List<string> { "4", "5", "6" };

        // Act
        IEnumerable<int> result1 = enumerable.ToListInt();
        List<int> result2 = list.ToListInt();

        // Assert
        Assert.Equal([1, 2, 3], result1);
        Assert.Equal(new List<int> { 4, 5, 6 }, result2);
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
        Assert.Equal(expected, result);
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
        Assert.Equal((decimal?)expected, result);
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
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(new DateTime(year.Value, month!.Value, day!.Value), result);
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
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(new DateOnly(year.Value, month!.Value, day!.Value), result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    public void BoolToYesNo_Works(bool input, string expected)
    {
        // Act
        string result = input.BoolToYesNo();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "Y")]
    [InlineData(false, "N")]
    public void BoolToYN_Works(bool input, string expected)
    {
        // Act
        string result = input.BoolToYN();

        // Assert
        Assert.Equal(expected, result);
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

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result1));
        Assert.False(string.IsNullOrWhiteSpace(result2));
        Assert.False(string.IsNullOrWhiteSpace(result3));
    }

    [Fact]
    public void MakeExportNameUnique_Works()
    {
        // Arrange
        const string tempDir = "TestData";
        const string fileName = "test.png";
        const string ext = "png";

        // Act
        string unique = Strings.MakeExportNameUnique(tempDir, fileName, ext);

        // Assert
        Assert.StartsWith(fileName.Replace($".{ext}", string.Empty), unique);
        Assert.EndsWith(ext, unique);
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
        Assert.Contains(expected[..2], result);
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
        Assert.Contains(expected[..2], result);
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
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc123", @"\d+", "*", false, "*123")]
    [InlineData("abc123", "[a-z]+", "#", false, "abc#")]
    [InlineData(null, @"\d+", "*", false, null)]
    public void ReplaceInverse_Regex_Works(string? input, string pattern, string? replacement, bool matchFirstOnly, string? expected)
    {
        // Arrange
        Regex regex = new(pattern);

        // Act
        string? result = regex.ReplaceInverse(input, replacement, matchFirstOnly);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2.5, 3, "2 1/2")]
    [InlineData(3.25, 2, "3 1/4")]
    public void ToFractionString_Decimal_Works(decimal input, int maxDecimals, string expected)
    {
        // Act
        string? result = input.ToFractionString(maxDecimals);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2.5, 3, "2 1/2")]
    [InlineData(3.25, 2, "3 1/4")]
    public void ToFractionString_Double_Works(double input, int maxDecimals, string expected)
    {
        // Act
        string? result = input.ToFractionString(maxDecimals);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToFractionString_NullableDecimal_Works()
    {
        // Arrange
        decimal? input = null;

        // Act
        string? result = input.ToFractionString(2);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToFractionString_NullableDouble_Works()
    {
        // Arrange
        double? input = null;

        // Act
        string? result = input.ToFractionString(2);

        // Assert
        Assert.Null(result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal((decimal?)expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal((decimal?)expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal((decimal?)expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal((decimal?)expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
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
        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
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
        Assert.Equal(expected, result);
    }
}
