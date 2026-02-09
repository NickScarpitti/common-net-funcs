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

	// Edge case: zero or negative max dimensions for 3D int
	[Theory]
	[InlineData(100, 100, 100, 0, 100, 100)]
	[InlineData(100, 100, 100, 100, 0, 100)]
	[InlineData(100, 100, 100, 100, 100, 0)]
	[InlineData(100, 100, 100, -100, 100, 100)]
	[InlineData(100, 100, 100, 100, -100, 100)]
	[InlineData(100, 100, 100, 100, 100, -100)]
	public void ScaleDimensionsToConstraint_Int3D_ThrowsOnInvalidMaxInput(int originalWidth, int originalHeight, int originalDepth, int maxWidth, int maxHeight, int maxDepth)
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth));
	}

	// Edge case: zero or negative max dimensions for 3D decimal
	[Theory]
	[InlineData(100.0, 100.0, 100.0, 0.0, 100.0, 100.0)]
	[InlineData(100.0, 100.0, 100.0, 100.0, 0.0, 100.0)]
	[InlineData(100.0, 100.0, 100.0, 100.0, 100.0, 0.0)]
	[InlineData(100.0, 100.0, 100.0, -100.0, 100.0, 100.0)]
	[InlineData(100.0, 100.0, 100.0, 100.0, -100.0, 100.0)]
	[InlineData(100.0, 100.0, 100.0, 100.0, 100.0, -100.0)]
	public void ScaleDimensionsToConstraint_Decimal3D_ThrowsOnInvalidMaxInput(decimal originalWidth, decimal originalHeight, decimal originalDepth, decimal maxWidth, decimal maxHeight, decimal maxDepth)
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth));
	}

	// Test when scaleUpToFit is false and dimensions need scaling
	[Fact]
	public void ScaleDimensionsToConstraint_Int2D_NoScaleUp_WhenLargerThanMax_ScalesDown()
	{
		// Arrange
		int originalWidth = 200;
		int originalHeight = 200;
		int maxWidth = 100;
		int maxHeight = 100;

		// Act
		(int newWidth, int newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit: false);

		// Assert
		newWidth.ShouldBe(100);
		newHeight.ShouldBe(100);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Decimal2D_NoScaleUp_WhenLargerThanMax_ScalesDown()
	{
		// Arrange
		decimal originalWidth = 200.0m;
		decimal originalHeight = 200.0m;
		decimal maxWidth = 100.0m;
		decimal maxHeight = 100.0m;

		// Act
		(decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit: false);

		// Assert
		newWidth.ShouldBe(100.0m);
		newHeight.ShouldBe(100.0m);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Int3D_NoScaleUp_WhenLargerThanMax_ScalesDown()
	{
		// Arrange
		int originalWidth = 200;
		int originalHeight = 200;
		int originalDepth = 200;
		int maxWidth = 100;
		int maxHeight = 100;
		int maxDepth = 100;

		// Act
		(int newWidth, int newHeight, int newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit: false);

		// Assert
		newWidth.ShouldBe(100);
		newHeight.ShouldBe(100);
		newDepth.ShouldBe(100);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Decimal3D_NoScaleUp_WhenLargerThanMax_ScalesDown()
	{
		// Arrange
		decimal originalWidth = 200.0m;
		decimal originalHeight = 200.0m;
		decimal originalDepth = 200.0m;
		decimal maxWidth = 100.0m;
		decimal maxHeight = 100.0m;
		decimal maxDepth = 100.0m;

		// Act
		(decimal newWidth, decimal newHeight, decimal newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit: false);

		// Assert
		newWidth.ShouldBe(100.0m);
		newHeight.ShouldBe(100.0m);
		newDepth.ShouldBe(100.0m);
	}

	// Test aspect ratio scenarios
	[Theory]
	[InlineData(400, 200, 200, 200, true, 200, 100)] // Width is limiting factor
	[InlineData(200, 400, 200, 200, true, 100, 200)] // Height is limiting factor
	public void ScaleDimensionsToConstraint_Int2D_MaintainsAspectRatio(int originalWidth, int originalHeight, int maxWidth, int maxHeight, bool scaleUpToFit, int expectedWidth, int expectedHeight)
	{
		// Act
		(int newWidth, int newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit);

		// Assert
		newWidth.ShouldBe(expectedWidth);
		newHeight.ShouldBe(expectedHeight);

		// Verify aspect ratio is maintained (with tolerance for rounding)
		decimal originalRatio = (decimal)originalWidth / originalHeight;
		decimal newRatio = (decimal)newWidth / newHeight;
		Math.Abs(originalRatio - newRatio).ShouldBeLessThan(0.1m);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Int2D_WithOddRatio_RoundsToZero()
	{
		// Arrange - 300x150 scaled to 100x100 will have rounding
		int originalWidth = 300;
		int originalHeight = 150;
		int maxWidth = 100;
		int maxHeight = 100;

		// Act
		(int newWidth, int newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit: true);

		// Assert - ToZero rounding means 99 and 49 (not 100 and 50)
		newWidth.ShouldBe(99);
		newHeight.ShouldBe(49);
		newWidth.ShouldBeLessThanOrEqualTo(maxWidth);
		newHeight.ShouldBeLessThanOrEqualTo(maxHeight);

		// Verify aspect ratio is maintained
		decimal originalRatio = (decimal)originalWidth / originalHeight;
		decimal newRatio = (decimal)newWidth / newHeight;
		Math.Abs(originalRatio - newRatio).ShouldBeLessThan(0.1m);
	}

	[Theory]
	[InlineData(400.0, 200.0, 200.0, 200.0, true, null, 200.0, 100.0)] // Width is limiting factor
	[InlineData(200.0, 400.0, 200.0, 200.0, true, null, 100.0, 200.0)] // Height is limiting factor
	public void ScaleDimensionsToConstraint_Decimal2D_MaintainsAspectRatio(decimal originalWidth, decimal originalHeight, decimal maxWidth, decimal maxHeight, bool scaleUpToFit, int? decimalPlaces,
			decimal expectedWidth, decimal expectedHeight)
	{
		// Act
		(decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit, decimalPlaces);

		// Assert
		newWidth.ShouldBe(expectedWidth);
		newHeight.ShouldBe(expectedHeight);

		// Verify aspect ratio is maintained
		decimal originalRatio = originalWidth / originalHeight;
		decimal newRatio = newWidth / newHeight;
		Math.Abs(originalRatio - newRatio).ShouldBeLessThan(0.01m);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Decimal2D_WithOddRatio_MaintainsAspectRatio()
	{
		// Arrange - 300x150 scaled to 100x100
		decimal originalWidth = 300.0m;
		decimal originalHeight = 150.0m;
		decimal maxWidth = 100.0m;
		decimal maxHeight = 100.0m;

		// Act
		(decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit: true);

		// Assert
		newWidth.ShouldBeLessThanOrEqualTo(maxWidth);
		newHeight.ShouldBeLessThanOrEqualTo(maxHeight);

		// Verify aspect ratio is maintained perfectly with decimals
		decimal originalRatio = originalWidth / originalHeight;
		decimal newRatio = newWidth / newHeight;
		originalRatio.ShouldBe(newRatio);
	}

	[Theory]
	[InlineData(400, 200, 200, 200, 200, 200, true, 200, 100, 100)] // Width is limiting factor
	[InlineData(200, 400, 200, 200, 200, 200, true, 100, 200, 100)] // Height is limiting factor
	[InlineData(200, 200, 400, 200, 200, 200, true, 100, 100, 200)] // Depth is limiting factor
	public void ScaleDimensionsToConstraint_Int3D_MaintainsAspectRatio(int originalWidth, int originalHeight, int originalDepth, int maxWidth, int maxHeight, int maxDepth, bool scaleUpToFit,
			int expectedWidth, int expectedHeight, int expectedDepth)
	{
		// Act
		(int newWidth, int newHeight, int newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit);

		// Assert
		newWidth.ShouldBe(expectedWidth);
		newHeight.ShouldBe(expectedHeight);
		newDepth.ShouldBe(expectedDepth);

		// Verify aspect ratios are maintained (with tolerance for rounding)
		decimal originalRatioWH = (decimal)originalWidth / originalHeight;
		decimal newRatioWH = (decimal)newWidth / newHeight;
		Math.Abs(originalRatioWH - newRatioWH).ShouldBeLessThan(0.1m);

		decimal originalRatioWD = (decimal)originalWidth / originalDepth;
		decimal newRatioWD = (decimal)newWidth / newDepth;
		Math.Abs(originalRatioWD - newRatioWD).ShouldBeLessThan(0.1m);
	}

	[Theory]
	[InlineData(400.0, 200.0, 200.0, 200.0, 200.0, 200.0, true, null, 200.0, 100.0, 100.0)] // Width is limiting factor
	[InlineData(200.0, 400.0, 200.0, 200.0, 200.0, 200.0, true, null, 100.0, 200.0, 100.0)] // Height is limiting factor
	[InlineData(200.0, 200.0, 400.0, 200.0, 200.0, 200.0, true, null, 100.0, 100.0, 200.0)] // Depth is limiting factor
	public void ScaleDimensionsToConstraint_Decimal3D_MaintainsAspectRatio(decimal originalWidth, decimal originalHeight, decimal originalDepth, decimal maxWidth, decimal maxHeight, decimal maxDepth, bool scaleUpToFit,
			int? decimalPlaces, decimal expectedWidth, decimal expectedHeight, decimal expectedDepth)
	{
		// Act
		(decimal newWidth, decimal newHeight, decimal newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit, decimalPlaces);

		// Assert
		newWidth.ShouldBe(expectedWidth);
		newHeight.ShouldBe(expectedHeight);
		newDepth.ShouldBe(expectedDepth);

		// Verify aspect ratios are maintained
		decimal originalRatioWH = originalWidth / originalHeight;
		decimal newRatioWH = newWidth / newHeight;
		Math.Abs(originalRatioWH - newRatioWH).ShouldBeLessThan(0.01m);

		decimal originalRatioWD = originalWidth / originalDepth;
		decimal newRatioWD = newWidth / newDepth;
		Math.Abs(originalRatioWD - newRatioWD).ShouldBeLessThan(0.01m);
	}

	// Test decimal places rounding
	[Fact]
	public void ScaleDimensionsToConstraint_Decimal2D_WithDecimalPlaces_RoundsCorrectly()
	{
		// Arrange
		decimal originalWidth = 333.333m;
		decimal originalHeight = 222.222m;
		decimal maxWidth = 100.0m;
		decimal maxHeight = 100.0m;

		// Act
		(decimal newWidth, decimal newHeight) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, maxWidth, maxHeight, scaleUpToFit: true, resultDecimalPlaces: 2);

		// Assert
		// Verify decimal places
		(newWidth * 100).ShouldBe(Math.Floor(newWidth * 100));
		(newHeight * 100).ShouldBe(Math.Floor(newHeight * 100));

		// Should maintain aspect ratio
		newWidth.ShouldBeGreaterThan(newHeight);
	}

	[Fact]
	public void ScaleDimensionsToConstraint_Decimal3D_WithDecimalPlaces_RoundsCorrectly()
	{
		// Arrange
		decimal originalWidth = 333.333m;
		decimal originalHeight = 222.222m;
		decimal originalDepth = 111.111m;
		decimal maxWidth = 100.0m;
		decimal maxHeight = 100.0m;
		decimal maxDepth = 100.0m;

		// Act
		(decimal newWidth, decimal newHeight, decimal newDepth) = DimensionScale.ScaleDimensionsToConstraint(originalWidth, originalHeight, originalDepth, maxWidth, maxHeight, maxDepth, scaleUpToFit: true, resultDecimalPlaces: 2);

		// Assert
		// Verify decimal places
		(newWidth * 100).ShouldBe(Math.Floor(newWidth * 100));
		(newHeight * 100).ShouldBe(Math.Floor(newHeight * 100));
		(newDepth * 100).ShouldBe(Math.Floor(newDepth * 100));

		// Should maintain relative sizes
		newWidth.ShouldBeGreaterThan(newHeight);
		newHeight.ShouldBeGreaterThan(newDepth);
	}
}
