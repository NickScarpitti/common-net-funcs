using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListMaxLengthAttributeTests : ValidationTestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Constructor_WithInvalidLength_ShouldThrowOnValidation(int length)
    {
        // Arrange
        ListMaxLengthAttribute attribute = new(length);
        List<string> list = new() { "test" };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => attribute.GetValidationResult(list, DummyValidationContext));
    }

    [Fact]
    public void IsValid_WithNull_ShouldReturnSuccess()
    {
        // Arrange
        ListMaxLengthAttribute attribute = new(5);

        // Act
        ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void IsValid_WithNonCollection_ShouldThrow()
    {
        // Arrange
        ListMaxLengthAttribute attribute = new(5);
        const string nonCollection = "not a collection";

        // Act & Assert
        Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(nonCollection, DummyValidationContext));
    }

    [Theory]
    [InlineData(5, new[] { "1234", "123", "12345" }, true)]
    [InlineData(3, new[] { "1234", "12345" }, false)]
    public void IsValid_WithStringList_ShouldValidateCorrectly(int maxLength, string[] items, bool shouldBeValid)
    {
        // Arrange
        ListMaxLengthAttribute attribute = new(maxLength);
        List<string> list = new(items);

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
            result.ErrorMessage?.ShouldContain("exceeds the maximum length");
        }
    }

    [Fact]
    public void FormatErrorMessage_ShouldReturnFormattedMessage()
    {
        // Arrange
        ListMaxLengthAttribute attribute = new(5);

        // Act
        string message = attribute.FormatErrorMessage("TestProperty");

        // Assert
        message.ShouldContain("TestProperty");
        message.ShouldContain("5");
    }
}
