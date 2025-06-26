using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListRegularExpressionAttributeTests : ValidationTestBase
{
    [Fact]
    public void Constructor_WithNullPattern_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ListRegularExpressionAttribute(null!));
    }

    [Fact]
    public void IsValid_WithNull_ShouldReturnSuccess()
    {
        // Arrange
        ListRegularExpressionAttribute attribute = new(@"^\d+$");

        // Act
        ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Theory]
    [InlineData(new[] { "123", "456", "789" }, @"^\d+$", true)]
    [InlineData(new[] { "abc", "123", "def" }, @"^\d+$", false)]
    public void IsValid_WithStringList_ShouldValidateCorrectly(string[] values, string pattern, bool shouldBeValid)
    {
        // Arrange
        ListRegularExpressionAttribute attribute = new(pattern);
        List<string> list = values.ToList();

        // Act
        ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

        // Assert
        if (shouldBeValid)
        {
            result.ShouldBe(ValidationResult.Success);
        }
        else
        {
            result.ShouldNotBeNull();
            result.ErrorMessage?.ShouldContain("does not match the required pattern");
        }
    }

    [Fact]
    public void IsValid_WithMatchTimeout_ShouldRespectTimeout()
    {
        // Arrange
        ListRegularExpressionAttribute attribute = new("^(a+)+$")
        {
            MatchTimeoutInMilliseconds = 100
        };
        List<string> list = new() { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!" }; // Catastrophic backtracking case

        // Act & Assert
        Should.Throw<RegexMatchTimeoutException>(() => attribute.GetValidationResult(list, DummyValidationContext));
    }
}
