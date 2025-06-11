using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class DimensionScaleTests
{
    // 2D int tests
    [Theory]
    [InlineData(100, 100, 200, 200, true, 200, 200)] // scale up
    [InlineData(200, 100, 100, 100, true, 100, 50)]  // scale down
    [InlineData(100, 200, 100, 100, true, 50, 100)]  // scale down
    [InlineData(100, 100, 100, 100, true, 100, 100)] // exact fit
    [InlineData(50, 50, 100, 100, false, 50, 50)]    // no scale up
    [InlineData(200, 200, 100, 100, false, 100, 100)]// scale down, no up
    public void ScaleDimensionsToConstraint_Int2D_Works(int originalWidth, int originalHeight, int maxWidth, int maxHeight, bool scaleUpToFit, int expectedWidth, int expectedHeight)
    {
        // Act
        (int newWidth, int newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit);

        // Assert
        newWidth.ShouldBe(expectedWidth);
        newHeight.ShouldBe(expectedHeight);
        newWidth.ShouldBeLessThanOrEqualTo(maxWidth);
        newHeight.ShouldBeLessThanOrEqualTo(maxHeight);
    }

    // 2D decimal tests
    [Theory]
    [InlineData(100.0, 100.0, 200.0, 200.0, true, null, 200.0, 200.0)]
    [InlineData(200.0, 100.0, 100.0, 100.0, true, null, 100.0, 50.0)]
    [InlineData(100.0, 200.0, 100.0, 100.0, true, null, 50.0, 100.0)]
    [InlineData(100.0, 100.0, 100.0, 100.0, true, null, 100.0, 100.0)]
    [InlineData(50.0, 50.0, 100.0, 100.0, false, null, 50.0, 50.0)]
    [InlineData(200.0, 200.0, 100.0, 100.0, false, null, 100.0, 100.0)]
    [InlineData(100.0, 100.0, 200.0, 200.0, true, 1, 200.0, 200.0)]
    [InlineData(200.0, 100.0, 100.0, 100.0, true, 2, 100.00, 50.00)]
    public void ScaleDimensionsToConstraint_Decimal2D_Works(decimal originalWidth, decimal originalHeight, decimal maxWidth, decimal maxHeight, bool scaleUpToFit, int? decimalPlaces,
        decimal expectedWidth, decimal expectedHeight)
    {
        // Act
        (decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit, decimalPlaces);

        // Assert
        newWidth.ShouldBe(expectedWidth);
        newHeight.ShouldBe(expectedHeight);
        newWidth.ShouldBeLessThanOrEqualTo(maxWidth + 0.0001m);
        newHeight.ShouldBeLessThanOrEqualTo(maxHeight + 0.0001m);
    }

    // 3D int tests
    [Theory]
    [InlineData(100, 100, 100, 200, 200, 200, true, 200, 200, 200)] // scale up
    [InlineData(200, 100, 100, 100, 100, 100, true, 100, 50, 50)]   // scale down
    [InlineData(100, 200, 100, 100, 100, 100, true, 50, 100, 50)]   // scale down
    [InlineData(100, 100, 100, 100, 100, 100, true, 100, 100, 100)] // exact fit
    [InlineData(50, 50, 50, 100, 100, 100, false, 50, 50, 50)]      // no scale up
    [InlineData(200, 200, 200, 100, 100, 100, false, 100, 100, 100)]// scale down, no up
    public void ScaleDimensionsToConstraint_Int3D_Works(int originalWidth, int originalHeight, int originalDepth, int maxWidth, int maxHeight, int maxDepth, bool scaleUpToFit,
        int expectedWidth, int expectedHeight, int expectedDepth)
    {
        // Act
        (int newWidth, int newHeight, int newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit);

        // Assert
        newWidth.ShouldBe(expectedWidth);
        newHeight.ShouldBe(expectedHeight);
        newDepth.ShouldBe(expectedDepth);
        newWidth.ShouldBeLessThanOrEqualTo(maxWidth);
        newHeight.ShouldBeLessThanOrEqualTo(maxHeight);
        newDepth.ShouldBeLessThanOrEqualTo(maxDepth);
    }

    // 3D decimal tests
    [Theory]
    [InlineData(100.0, 100.0, 100.0, 200.0, 200.0, 200.0, true, null, 200.0, 200.0, 200.0)]
    [InlineData(200.0, 100.0, 100.0, 100.0, 100.0, 100.0, true, null, 100.0, 50.0, 50.0)]
    [InlineData(100.0, 200.0, 100.0, 100.0, 100.0, 100.0, true, null, 50.0, 100.0, 50.0)]
    [InlineData(100.0, 100.0, 100.0, 100.0, 100.0, 100.0, true, null, 100.0, 100.0, 100.0)]
    [InlineData(50.0, 50.0, 50.0, 100.0, 100.0, 100.0, false, null, 50.0, 50.0, 50.0)]
    [InlineData(200.0, 200.0, 200.0, 100.0, 100.0, 100.0, false, null, 100.0, 100.0, 100.0)]
    [InlineData(100.0, 100.0, 100.0, 200.0, 200.0, 200.0, true, 1, 200.0, 200.0, 200.0)]
    [InlineData(200.0, 100.0, 100.0, 100.0, 100.0, 100.0, true, 2, 100.00, 50.00, 50.00)]
    public void ScaleDimensionsToConstraint_Decimal3D_Works(decimal originalWidth, decimal originalHeight, decimal originalDepth, decimal maxWidth, decimal maxHeight, decimal maxDepth, bool scaleUpToFit,
        int? decimalPlaces, decimal expectedWidth, decimal expectedHeight, decimal expectedDepth)
    {
        // Act
        (decimal newWidth, decimal newHeight, decimal newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit, decimalPlaces);

        // Assert
        newWidth.ShouldBe(expectedWidth);
        newHeight.ShouldBe(expectedHeight);
        newDepth.ShouldBe(expectedDepth);
        newWidth.ShouldBeLessThanOrEqualTo(maxWidth + 0.0001m);
        newHeight.ShouldBeLessThanOrEqualTo(maxHeight + 0.0001m);
        newDepth.ShouldBeLessThanOrEqualTo(maxDepth + 0.0001m);
    }

    // Edge case: zero or negative dimensions
    [Theory]
    [InlineData(0, 100, 100, 100)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(100, 100, 0, 100)]
    [InlineData(100, 100, 100, 0)]
    [InlineData(-100, 100, 100, 100)]
    [InlineData(100, -100, 100, 100)]
    [InlineData(100, 100, -100, 100)]
    [InlineData(100, 100, 100, -100)]
    public void ScaleDimensionsToConstraint_Int2D_ThrowsOnInvalidInput(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight));
    }

    [Theory]
    [InlineData(0.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, 0.0, 100.0, 100.0)]
    [InlineData(100.0, 100.0, 0.0, 100.0)]
    [InlineData(100.0, 100.0, 100.0, 0.0)]
    [InlineData(-100.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, -100.0, 100.0, 100.0)]
    [InlineData(100.0, 100.0, -100.0, 100.0)]
    [InlineData(100.0, 100.0, 100.0, -100.0)]
    public void ScaleDimensionsToConstraint_Decimal2D_ThrowsOnInvalidInput(decimal originalWidth, decimal originalHeight, decimal maxWidth, decimal maxHeight)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight));
    }

    [Theory]
    [InlineData(0, 100, 100, 100, 100, 100)]
    [InlineData(100, 0, 100, 100, 100, 100)]
    [InlineData(100, 100, 0, 100, 100, 100)]
    [InlineData(-100, 100, 100, 100, 100, 100)]
    [InlineData(100, -100, 100, 100, 100, 100)]
    [InlineData(100, 100, -100, 100, 100, 100)]
    public void ScaleDimensionsToConstraint_Int3D_ThrowsOnInvalidInput(int originalWidth, int originalHeight, int originalDepth, int maxWidth, int maxHeight, int maxDepth)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth));
    }

    [Theory]
    [InlineData(0.0, 100.0, 100.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, 0.0, 100.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, 100.0, 0.0, 100.0, 100.0, 100.0)]
    [InlineData(-100.0, 100.0, 100.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, -100.0, 100.0, 100.0, 100.0, 100.0)]
    [InlineData(100.0, 100.0, -100.0, 100.0, 100.0, 100.0)]
    public void ScaleDimensionsToConstraint_Decimal3D_ThrowsOnInvalidInput(decimal originalWidth, decimal originalHeight, decimal originalDepth, decimal maxWidth, decimal maxHeight, decimal maxDepth)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth));
    }
}
