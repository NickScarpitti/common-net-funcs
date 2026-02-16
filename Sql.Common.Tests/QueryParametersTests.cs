﻿using CommonNetFuncs.Sql.Common;

namespace Sql.Common.Tests;

public sealed class QueryParametersTests
{
	[Theory]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData("null", null)]
	[InlineData("test\n", "test")]
	[InlineData(" test ", "test")]
	[InlineData("\ntest\n ", "test")]
	public void CleanQueryParam_String_ShouldHandleVariousInputs(string? input, string? expected)
	{
		// Act
		string? result = input.CleanQueryParam();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void CleanQueryParam_EnumerableString_ShouldHandleNullInput()
	{
		// Arrange
		IEnumerable<string>? input = null;

		// Act
		IEnumerable<string>? result = input.CleanQueryParam();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void CleanQueryParam_EnumerableString_ShouldCleanValidCollection()
	{
		// Arrange
		IEnumerable<string> input = new[] { "test\n", " value ", "null", "  clean\nthis  " };

		// Act
		IEnumerable<string> result = input.CleanQueryParam()!;

		// Assert
		result.ShouldNotBeNull();
		result.Count().ShouldBe(3);
		result.ShouldContain("test");
		result.ShouldContain("value");
		result.ShouldContain("cleanthis");
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("normal text", true)]
	[InlineData("text;", false)]
	[InlineData("text'", false)]
	[InlineData("text[", false)]
	[InlineData("text]", false)]
	[InlineData("text\"", false)]
	[InlineData("text`", false)]
	[InlineData("text/*", false)]
	[InlineData("text*/", false)]
	[InlineData("textxp_", false)]
	[InlineData("text--", false)]
	public void IsClean_ShouldDetectMaliciousContent(string? input, bool expectedResult)
	{
		// Act
		bool result = input.IsClean();

		// Assert
		result.ShouldBe(expectedResult);
	}

	[Theory]
	[InlineData("test'name", false, false, false, null, null, "test''name")]
	[InlineData("test%name*", false, false, false, null, null, "testname")]
	[InlineData("test;name", false, false, false, null, null, "")]
	[InlineData("123abc", true, false, false, null, null, "123abc")]
	[InlineData("abc", false, true, false, null, null, "abc")]
	[InlineData("123", false, false, true, null, null, "123")]
	[InlineData("test", false, false, false, 3, null, "")]
	[InlineData("test", false, false, false, null, 5, "")]
	public void SanitizeSqlParameter_ShouldHandleVariousScenarios(string input, bool onlyAlphanumeric, bool onlyAlphaChars, bool onlyNumberChars, int? maxLength, int? minLength, string expected)
	{
		// Act
		string result = input.SanitizeSqlParameter(onlyAlphanumeric, onlyAlphaChars, onlyNumberChars, maxLength, minLength);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void CleanQueryParam_IList_ShouldHandleNullInput()
	{
		// Arrange
		IList<string>? input = null;

		// Act
		List<string>? result = input.CleanQueryParam();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void CleanQueryParam_IList_ShouldHandleEmptyList()
	{
		// Arrange
		IList<string> input = new List<string>();

		// Act
		List<string>? result = input.CleanQueryParam();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}

	[Fact]
	public void CleanQueryParam_IList_ShouldCleanValidList()
	{
		// Arrange
		IList<string> input = new List<string> { "test\n", " value ", "null", "  clean\nthis  " };

		// Act
		List<string>? result = input.CleanQueryParam();

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
		result.ShouldContain("test");
		result.ShouldContain("value");
		result.ShouldContain("cleanthis");
	}

	[Fact]
	public void CleanQueryParam_IList_ShouldHandleListWithMultipleItems()
	{
		// Arrange
		IList<string> input = new List<string>
				{
						"item1\n",
						"  item2  ",
						"item3\n\n",
						" \nitem4 ",
						"null",
						"item5"
				};

		// Act
		List<string>? result = input.CleanQueryParam();

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(5);
		result.ShouldContain("item1");
		result.ShouldContain("item2");
		result.ShouldContain("item3");
		result.ShouldContain("item4");
		result.ShouldContain("item5");
	}

	[Theory]
	[InlineData("test'name", true, true)]
	[InlineData("test;name", true, false)]
	[InlineData("normal text", true, true)]
	[InlineData("test[bracket", true, false)]
	public void IsClean_WithExcludeSingleQuote_ShouldHandleCorrectly(string input, bool excludeSingleQuote, bool expected)
	{
		// Act
		bool result = input.IsClean(excludeSingleQuote);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void SanitizeSqlParameter_OnlyAlphanumeric_WithNonAlphanumeric_ShouldReturnDefault()
	{
		// Arrange
		string input = "test@name";

		// Act
		string result = input.SanitizeSqlParameter(onlyAlphanumeric: true);

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public void SanitizeSqlParameter_OnlyAlphaChars_WithNumbers_ShouldReturnDefault()
	{
		// Arrange
		string input = "test123";

		// Act
		string result = input.SanitizeSqlParameter(onlyAlphaChars: true);

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public void SanitizeSqlParameter_OnlyNumberChars_WithLetters_ShouldReturnDefault()
	{
		// Arrange
		string input = "123abc";

		// Act
		string result = input.SanitizeSqlParameter(onlyNumberChars: true);

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public void SanitizeSqlParameter_MaxLength_ExactMatch_ShouldPass()
	{
		// Arrange
		string input = "test";

		// Act
		string result = input.SanitizeSqlParameter(maxLength: 4);

		// Assert
		result.ShouldBe("test");
	}

	[Fact]
	public void SanitizeSqlParameter_MinLength_ExactMatch_ShouldPass()
	{
		// Arrange
		string input = "test";

		// Act
		string result = input.SanitizeSqlParameter(minLength: 4);

		// Assert
		result.ShouldBe("test");
	}

	[Fact]
	public void SanitizeSqlParameter_WithCustomDefaultValue_ShouldReturnDefault()
	{
		// Arrange
		string input = "test;malicious";
		string customDefault = "SAFE_DEFAULT";

		// Act
		string result = input.SanitizeSqlParameter(defaultValue: customDefault);

		// Assert
		result.ShouldBe(customDefault);
	}

	[Fact]
	public void SanitizeSqlParameter_NullParameter_WithDefaultValue_ShouldReturnDefault()
	{
		// Arrange
		string? input = null;
		string customDefault = "NULL_DEFAULT";

		// Act
		string result = input.SanitizeSqlParameter(defaultValue: customDefault);

		// Assert
		result.ShouldBe(customDefault);
	}

	[Fact]
	public void SanitizeSqlParameter_OnlyAlphanumeric_OverridesOtherFlags()
	{
		// Arrange - alphanumeric string
		string input = "test123";

		// Act - onlyAlphanumeric should override onlyAlphaChars
		string result = input.SanitizeSqlParameter(onlyAlphanumeric: true, onlyAlphaChars: true);

		// Assert - should pass because onlyAlphanumeric allows both
		result.ShouldBe("test123");
	}

	[Fact]
	public void SanitizeSqlParameter_OnlyAlphaChars_OverridesOnlyNumberChars()
	{
		// Arrange - alpha only string
		string input = "test";

		// Act - onlyAlphaChars should override onlyNumberChars
		string result = input.SanitizeSqlParameter(onlyAlphaChars: true, onlyNumberChars: true);

		// Assert - should pass because onlyAlphaChars takes precedence
		result.ShouldBe("test");
	}

	[Theory]
	[InlineData("test", 3, "")]  // exceeds maxLength
	[InlineData("test", 5, "")]  // below minLength
	public void SanitizeSqlParameter_LengthViolations_ShouldReturnDefault(string input, int? length, string expected)
	{
		// Act
		string result = input.SanitizeSqlParameter(maxLength: length, minLength: length);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void SanitizeSqlParameter_EscapesAndRemovesWildcards()
	{
		// Arrange
		string input = "test'with%wildcards*here";

		// Act
		string result = input.SanitizeSqlParameter();

		// Assert
		result.ShouldBe("test''withwildcardshere");
	}

	[Fact]
	public void CleanQueryParam_EnumerableString_ShouldHandleEmptyStrings()
	{
		// Arrange
		IEnumerable<string> input = new[] { "", "  ", "\n" };

		// Act
		IEnumerable<string> result = input.CleanQueryParam()!;

		// Assert
		result.ShouldNotBeNull();
		result.Count().ShouldBe(3); // All become empty strings after cleaning
		result.All(x => x == string.Empty).ShouldBeTrue();
	}

	[Fact]
	public void SanitizeSqlParameter_NullParameter_NoDefaultValue_ShouldReturnEmptyString()
	{
		// Arrange
		string? input = null;

		// Act
		string? result = input.SanitizeSqlParameter();

		// Assert
		result.ShouldBe(string.Empty);
	}

	[Fact]
	public void SanitizeSqlParameter_WhitespaceParameter_ShouldPassThrough()
	{
		// Arrange
		string input = "   ";

		// Act
		string result = input.SanitizeSqlParameter();

		// Assert - whitespace is considered clean by IsClean, so it passes through
		result.ShouldBe("   ");
	}
}
