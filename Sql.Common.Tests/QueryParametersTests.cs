﻿using CommonNetFuncs.Sql.Common;

namespace Sql.Common.Tests;

public enum IListCleanScenario
{
	NullInput,
	EmptyList,
	ValidList,
	MultipleItems
}

public enum SanitizeSqlParameterScenario
{
	OnlyAlphanumericWithNonAlphanumeric,
	OnlyAlphaCharsWithNumbers,
	OnlyNumberCharsWithLetters,
	MaxLengthExactMatch,
	MinLengthExactMatch,
	WithCustomDefaultValue,
	NullParameterWithDefaultValue,
	OnlyAlphanumericOverridesFlags,
	OnlyAlphaCharsOverridesNumberChars
}

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

	[Theory]
	[InlineData(IListCleanScenario.NullInput)]
	[InlineData(IListCleanScenario.EmptyList)]
	[InlineData(IListCleanScenario.ValidList)]
	[InlineData(IListCleanScenario.MultipleItems)]
	public void CleanQueryParam_IList_ShouldHandleVariousScenarios(IListCleanScenario scenario)
	{
		// Arrange, Act & Assert
		switch (scenario)
		{
			case IListCleanScenario.NullInput:
				IList<string>? nullInput = null;
				List<string>? nullResult = nullInput.CleanQueryParam();
				nullResult.ShouldBeNull();
				break;

			case IListCleanScenario.EmptyList:
				IList<string> emptyInput = new List<string>();
				List<string>? emptyResult = emptyInput.CleanQueryParam();
				emptyResult.ShouldNotBeNull();
				emptyResult.ShouldBeEmpty();
				break;

			case IListCleanScenario.ValidList:
				IList<string> validInput = new List<string> { "test\n", " value ", "null", "  clean\nthis  " };
				List<string>? validResult = validInput.CleanQueryParam();
				validResult.ShouldNotBeNull();
				validResult.Count.ShouldBe(3);
				validResult.ShouldContain("test");
				validResult.ShouldContain("value");
				validResult.ShouldContain("cleanthis");
				break;

			case IListCleanScenario.MultipleItems:
				IList<string> multiInput = new List<string>
				{
					"item1\n",
					"  item2  ",
					"item3\n\n",
					" \nitem4 ",
					"null",
					"item5"
				};
				List<string>? multiResult = multiInput.CleanQueryParam();
				multiResult.ShouldNotBeNull();
				multiResult.Count.ShouldBe(5);
				multiResult.ShouldContain("item1");
				multiResult.ShouldContain("item2");
				multiResult.ShouldContain("item3");
				multiResult.ShouldContain("item4");
				multiResult.ShouldContain("item5");
				break;
		}
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

	[Theory]
	[InlineData(SanitizeSqlParameterScenario.OnlyAlphanumericWithNonAlphanumeric)]
	[InlineData(SanitizeSqlParameterScenario.OnlyAlphaCharsWithNumbers)]
	[InlineData(SanitizeSqlParameterScenario.OnlyNumberCharsWithLetters)]
	[InlineData(SanitizeSqlParameterScenario.MaxLengthExactMatch)]
	[InlineData(SanitizeSqlParameterScenario.MinLengthExactMatch)]
	[InlineData(SanitizeSqlParameterScenario.WithCustomDefaultValue)]
	[InlineData(SanitizeSqlParameterScenario.NullParameterWithDefaultValue)]
	[InlineData(SanitizeSqlParameterScenario.OnlyAlphanumericOverridesFlags)]
	[InlineData(SanitizeSqlParameterScenario.OnlyAlphaCharsOverridesNumberChars)]
	public void SanitizeSqlParameter_VariousScenarios_ShouldHandleCorrectly(SanitizeSqlParameterScenario scenario)
	{
		// Arrange, Act & Assert
		switch (scenario)
		{
			case SanitizeSqlParameterScenario.OnlyAlphanumericWithNonAlphanumeric:
				string result1 = "test@name".SanitizeSqlParameter(onlyAlphanumeric: true);
				result1.ShouldBe(string.Empty);
				break;

			case SanitizeSqlParameterScenario.OnlyAlphaCharsWithNumbers:
				string result2 = "test123".SanitizeSqlParameter(onlyAlphaChars: true);
				result2.ShouldBe(string.Empty);
				break;

			case SanitizeSqlParameterScenario.OnlyNumberCharsWithLetters:
				string result3 = "123abc".SanitizeSqlParameter(onlyNumberChars: true);
				result3.ShouldBe(string.Empty);
				break;

			case SanitizeSqlParameterScenario.MaxLengthExactMatch:
				string result4 = "test".SanitizeSqlParameter(maxLength: 4);
				result4.ShouldBe("test");
				break;

			case SanitizeSqlParameterScenario.MinLengthExactMatch:
				string result5 = "test".SanitizeSqlParameter(minLength: 4);
				result5.ShouldBe("test");
				break;

			case SanitizeSqlParameterScenario.WithCustomDefaultValue:
				string customDefault = "SAFE_DEFAULT";
				string result6 = "test;malicious".SanitizeSqlParameter(defaultValue: customDefault);
				result6.ShouldBe(customDefault);
				break;

			case SanitizeSqlParameterScenario.NullParameterWithDefaultValue:
				string? nullInput = null;
				string nullDefault = "NULL_DEFAULT";
				string result7 = nullInput.SanitizeSqlParameter(defaultValue: nullDefault);
				result7.ShouldBe(nullDefault);
				break;

			case SanitizeSqlParameterScenario.OnlyAlphanumericOverridesFlags:
				string result8 = "test123".SanitizeSqlParameter(onlyAlphanumeric: true, onlyAlphaChars: true);
				result8.ShouldBe("test123");
				break;

			case SanitizeSqlParameterScenario.OnlyAlphaCharsOverridesNumberChars:
				string result9 = "test".SanitizeSqlParameter(onlyAlphaChars: true, onlyNumberChars: true);
				result9.ShouldBe("test");
				break;
		}
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
