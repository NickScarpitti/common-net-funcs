using CommonNetFuncs.Web.Common;
using FakeItEasy;
using Microsoft.AspNetCore.Http;

namespace Web.Common.Tests;

public class UriHelpersTests
{
    [Theory]
    [InlineData(null, null, "")]
    [InlineData(new int[0], "param", "")]
    [InlineData(null, "param", "")]
    [InlineData(new[] { 1 }, null, "")]
    [InlineData(new[] { 1 }, "", "")]
    [InlineData(new[] { 1 }, "param", "param=1")]
    [InlineData(new[] { 1, 2 }, "param", "param=1&param=2")]
    [InlineData(new[] { 1, 2, 3 }, "param", "param=1&param=2&param=3")]
    public void ListToQueryParameters_ShouldHandleVariousInputs(IEnumerable<int>? values, string? paramName, string expected)
    {
        // Act
        string result = values.ListToQueryParameters(paramName);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(new int[0], "")]
    [InlineData(new[] { 1 }, "param=1")]
    [InlineData(new[] { 1, 2 }, "param=1&param=2")]
    [InlineData(new[] { 1, 2, 3 }, "param=1&param=2&param=3")]
    public void ListToQueryParameters_KeyValuePairOverload_ShouldHandleVariousInputs(IEnumerable<int>? values, string expected)
    {
        // Arrange
        IEnumerable<KeyValuePair<string, int>>? parameters = values?.Select(v => new KeyValuePair<string, int>("param", v));

        // Act
        string result = parameters.ListToQueryParameters();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("2025-06-22", null, "20250622000000")]
    [InlineData("2025-06-22", "yyyy-MM-dd", "2025-06-22")]
    [InlineData("2025-06-22", "MM/dd/yyyy", "06/22/2025")]
    public void ToUriSafeString_DateTime_ShouldFormatCorrectly(string input, string? format, string expected)
    {
        // Arrange
        DateTime date = DateTime.Parse(input);

        // Act
        string result = date.ToUriSafeString(format);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("2025-06-22", null, "20250622000000")]
    [InlineData("2025-06-22", "yyyy-MM-dd", "2025-06-22")]
    [InlineData("2025-06-22", "MM/dd/yyyy", "06/22/2025")]
    public void ToUriSafeString_NullableDateTime_ShouldFormatCorrectly(string input, string? format, string expected)
    {
        // Arrange
        DateTime? date = DateTime.Parse(input);

        // Act
        string result = date.ToUriSafeString(format);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToUriSafeString_NullableDateTime_ShouldHandleNull()
    {
        // Arrange
        DateTime? date = null;

        // Act
        string? result = date.ToUriSafeString();

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("2025-06-22", null, "20250622")]
    [InlineData("2025-06-22", "yyyy-MM-dd", "2025-06-22")]
    [InlineData("2025-06-22", "MM/dd/yyyy", "06/22/2025")]
    public void ToUriSafeString_DateOnly_ShouldFormatCorrectly(string input, string? format, string expected)
    {
        // Arrange
        DateOnly date = DateOnly.Parse(input);

        // Act
        string result = date.ToUriSafeString(format);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("2025-06-22", null, "20250622")]
    [InlineData("2025-06-22", "yyyy-MM-dd", "2025-06-22")]
    [InlineData("2025-06-22", "MM/dd/yyyy", "06/22/2025")]
    public void ToUriSafeString_NullableDateOnly_ShouldFormatCorrectly(string input, string? format, string expected)
    {
        // Arrange
        DateOnly? date = DateOnly.Parse(input);

        // Act
        string result = date.ToUriSafeString(format);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ToUriSafeString_NullableDateOnly_ShouldHandleNull()
    {
        // Arrange
        DateOnly? date = null;

        // Act
        string? result = date.ToUriSafeString();

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("20250622000000", null, true)]
    [InlineData("2025-06-22", "yyyy-MM-dd", true)]
    [InlineData("invalid", null, false)]
    [InlineData(null, null, false)]
    public void ParseUriSafeDateTime_ShouldParseCorrectly(string? input, string? format, bool shouldParse)
    {
        // Act
        DateTime? result = input.ParseUriSafeDateTime(format);

        // Assert
        if (shouldParse)
        {
            result.ShouldNotBeNull();
            result.Value.ToString("yyyy-MM-dd").ShouldBe("2025-06-22");
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("20250622", null, true)]
    [InlineData("2025-06-22", "yyyy-MM-dd", true)]
    [InlineData("invalid", null, false)]
    [InlineData(null, null, false)]
    public void ParseUriSafeDateOnly_ShouldParseCorrectly(string? input, string? format, bool shouldParse)
    {
        // Act
        DateOnly? result = input.ParseUriSafeDateOnly(format);

        // Assert
        if (shouldParse)
        {
            result.ShouldNotBeNull();
            result.Value.ToString("yyyy-MM-dd").ShouldBe("2025-06-22");
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("https://example.com", "<REDACTED>", "https://example.com")]
    [InlineData("https://example.com?key=value", "<REDACTED>", "https://example.com?key=%3cREDACTED%3e")]
    [InlineData("https://example.com?key1=value1&key2=value2", "XXX", "https://example.com?key1=XXX&key2=XXX")]
    [InlineData(null, "<REDACTED>", null)]
    [InlineData("", "<REDACTED>", null)]
    [InlineData(" ", "<REDACTED>", null)]
    public void GetRedactedUri_ShouldRedactCorrectly(string? input, string redactedString, string? expected)
    {
        // Act
        string? result = input.GetRedactedUri(redactedString);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GetUriFromRequest_ShouldConstructUriCorrectly()
    {
        // Arrange
        HttpRequest request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Scheme).Returns("https");
        A.CallTo(() => request.Host).Returns(new HostString("example.com"));
        A.CallTo(() => request.PathBase).Returns("/api");
        A.CallTo(() => request.Path).Returns("/v1/resource");
        A.CallTo(() => request.QueryString).Returns(new QueryString("?key=value"));

        // Act
        string result = UriHelpers.GetUriFromRequest(request);

        // Assert
        result.ShouldBe("https://example.com/api/v1/resource?key=value");
    }
}
