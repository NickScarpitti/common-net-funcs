using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;

namespace Web.Common.Tests.ValidationAttributes;

public sealed class ListRangeAttributeTests : ValidationTestBase
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(10, 5)]
    public void Constructor_WithInvalidRange_ShouldThrow(int minimum, int maximum)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new ListRangeAttribute(minimum, maximum));
    }

    [Fact]
    public void IsValid_WithNull_ShouldReturnSuccess()
    {
        // Arrange
        ListRangeAttribute attribute = new(1, 10);

        // Act
        ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }

    [Theory]
    [InlineData(new[] { 1, 5, 10 }, 1, 10, true)]
    [InlineData(new[] { 0, 5, 11 }, 1, 10, false)]
    public void IsValid_WithIntegerList_ShouldValidateCorrectly(int[] values, int min, int max, bool shouldBeValid)
    {
        // Arrange
        ListRangeAttribute attribute = new(min, max);
        List<int> list = values.ToList();

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
            result.ErrorMessage?.ShouldContain("must be between");
        }
    }

    [Theory]
    [InlineData(new[] { 1.0, 5.5, 10.0 }, 1.0, 10.0, true)]
    [InlineData(new[] { 0.5, 5.5, 10.5 }, 1.0, 10.0, false)]
    public void IsValid_WithDoubleList_ShouldValidateCorrectly(double[] values, double min, double max, bool shouldBeValid)
    {
        // Arrange
        ListRangeAttribute attribute = new(min, max);
        List<double> list = values.ToList();

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
            result.ErrorMessage?.ShouldContain("must be between");
        }
    }
}
