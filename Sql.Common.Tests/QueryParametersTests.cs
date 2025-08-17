<<<<<<< HEAD
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
        result.ShouldBeNull();
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
}
=======
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
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
