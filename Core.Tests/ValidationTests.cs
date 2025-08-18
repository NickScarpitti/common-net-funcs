using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class ValidationTests
{
    public sealed class TestModel
    {
        [Required]
        public string? RequiredString { get; set; }

        [StringLength(5)]
        public string? MaxLengthString { get; set; }

        [Range(1, 10)]
        public int RangeNumber { get; set; }

        public string? UnvalidatedProperty { get; set; }
    }

    [Fact]
    public void SetInvalidPropertiesToDefault_NullObject_ReturnsNull()
    {
        // Arrange
        TestModel? model = null;

        // Act
        TestModel? result = model.SetInvalidPropertiesToDefault();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void SetInvalidPropertiesToDefault_ValidObject_ReturnsUnchangedObject()
    {
        // Arrange
        TestModel model = new()
        {
            RequiredString = "Test",
            MaxLengthString = "Test",
            RangeNumber = 5,
            UnvalidatedProperty = "Test"
        };

        // Act
        TestModel result = model.SetInvalidPropertiesToDefault();

        // Assert
        result.RequiredString.ShouldBe("Test");
        result.MaxLengthString.ShouldBe("Test");
        result.RangeNumber.ShouldBe(5);
        result.UnvalidatedProperty.ShouldBe("Test");
    }

    [Theory]
    [InlineData(null, "Valid", 5)] // Required string violation
    [InlineData("Valid", "TooLongString", 5)] // MaxLength violation
    [InlineData("Valid", "Valid", 20)] // Range violation
    public void SetInvalidPropertiesToDefault_InvalidProperties_ResetsInvalidProperties(
        string? requiredString,
        string? maxLengthString,
        int rangeNumber)
    {
        // Arrange
        TestModel model = new()
        {
            RequiredString = requiredString,
            MaxLengthString = maxLengthString,
            RangeNumber = rangeNumber,
            UnvalidatedProperty = "Test"
        };

        // Act
        TestModel result = model.SetInvalidPropertiesToDefault();

        // Assert
        if (requiredString is null)
        {
            result.RequiredString.ShouldBeNull();
        }

        if (maxLengthString == "TooLongString")
        {
            result.MaxLengthString.ShouldBeNull();
        }

        if (rangeNumber == 20)
        {
            result.RangeNumber.ShouldBe(0);
        }

        result.UnvalidatedProperty.ShouldBe("Test"); // Unvalidated property should remain unchanged
    }

    [Fact]
    public void SetInvalidPropertiesToDefault_MultipleValidationFailures_ResetsAllInvalidProperties()
    {
        // Arrange
        TestModel model = new()
        {
            RequiredString = null,
            MaxLengthString = "ThisStringIsTooLong",
            RangeNumber = 100,
            UnvalidatedProperty = "Test"
        };

        // Act
        TestModel result = model.SetInvalidPropertiesToDefault();

        // Assert
        result.RequiredString.ShouldBeNull();
        result.MaxLengthString.ShouldBeNull();
        result.RangeNumber.ShouldBe(0);
        result.UnvalidatedProperty.ShouldBe("Test");
    }

    [Fact]
    public void SetInvalidPropertiesToDefault_ValidateAllFalse_OnlyValidatesRequiredAttributes()
    {
        // Arrange
        TestModel model = new()
        {
            RequiredString = null, // Required validation will still occur
            MaxLengthString = "ThisStringIsTooLong", // This validation will be skipped
            RangeNumber = 100, // This validation will be skipped
            UnvalidatedProperty = "Test"
        };

        // Act
        TestModel result = model.SetInvalidPropertiesToDefault(validateAll: false);

        // Assert
        result.RequiredString.ShouldBeNull();
        result.MaxLengthString.ShouldBe("ThisStringIsTooLong"); // Should remain unchanged
        result.RangeNumber.ShouldBe(100); // Should remain unchanged
        result.UnvalidatedProperty.ShouldBe("Test");
    }
}
