using System.Globalization;
using CommonNetFuncs.Core;
using static CommonNetFuncs.Core.MathHelpers;

namespace Core.Tests;

public sealed class MathHelpersTests
{
    [Theory]
    [InlineData(null, 5.0, 0.0)]        // null value
    [InlineData(10.5, 0.0, 11.0)]       // zero significance
    [InlineData(10.0, 5.0, 10.0)]       // exact multiple
    [InlineData(12.0, 5.0, 15.0)]       // needs rounding up
    [InlineData(-12.0, 5.0, -10.0)]     // negative value
    [InlineData(0.0, 5.0, 0.0)]         // zero value
    [InlineData(4.1, 2.0, 6.0)]         // decimal value
    public void Ceiling_Double_Works(double? value, double significance, double expected)
    {
        double result = value.Ceiling(significance);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 5.0, 0.0)]
    [InlineData(10.5, 0.0, 11.0)]
    [InlineData(10.0, 5.0, 10.0)]
    [InlineData(12.0, 5.0, 15.0)]
    [InlineData(-12.0, 5.0, -10.0)]
    [InlineData(0.0, 5.0, 0.0)]
    [InlineData(4.1, 2.0, 6.0)]
    public void Ceiling_Decimal_Works(double? value, decimal significance, decimal expected)
    {
        decimal result = ((decimal?)value).Ceiling(significance);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 5.0, 0.0)]        // null value
    [InlineData(10.5, 0.0, 10.0)]       // zero significance
    [InlineData(10.0, 5.0, 10.0)]       // exact multiple
    [InlineData(12.0, 5.0, 10.0)]       // needs rounding down
    [InlineData(-12.0, 5.0, -15.0)]     // negative value
    [InlineData(0.0, 5.0, 0.0)]         // zero value
    [InlineData(4.1, 2.0, 4.0)]         // decimal value
    public void Floor_Double_Works(double? value, double significance, double expected)
    {
        double result = value.Floor(significance);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 5.0, 0.0)]
    [InlineData(10.5, 0.0, 10.0)]
    [InlineData(10.0, 5.0, 10.0)]
    [InlineData(12.0, 5.0, 10.0)]
    [InlineData(-12.0, 5.0, -15.0)]
    [InlineData(0.0, 5.0, 0.0)]
    [InlineData(4.1, 2.0, 4.0)]
    public void Floor_Decimal_Works(double? value, decimal significance, decimal expected)
    {
        decimal result = ((decimal?)value).Floor(significance);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 0)]               // null value
    [InlineData(123.0, 0)]              // whole number
    [InlineData(123.1, 1)]              // one decimal place
    [InlineData(123.12, 2)]             // two decimal places
    [InlineData(123.123, 3)]            // three decimal places
    [InlineData(-123.12, 2)]            // negative number
    [InlineData(0.0, 0)]                // zero
    public void GetPrecision_Double_Works(double? value, int expected)
    {
        int precision = value.GetPrecision();
        precision.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(123.0, 0)]
    [InlineData(123.1, 1)]
    [InlineData(123.12, 2)]
    [InlineData(123.123, 3)]
    [InlineData(-123.12, 2)]
    [InlineData(0.0, 0)]
    public void GetPrecision_Decimal_Works(double? value, int expected)
    {
        int precision = ((decimal?)value).GetPrecision();
        precision.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 5, new[] { 1, 2, 3, 4, 5 })]             // simple range
    [InlineData(-2, 2, new[] { -2, -1, 0, 1, 2 })]          // negative to positive
    [InlineData(0, 0, new[] { 0 })]                         // single number
    [InlineData(10, 12, new[] { 10, 11, 12 })]              // short range
    public void GenerateRange_Works(int start, int end, int[] expected)
    {
        IEnumerable<int> range = GenerateRange(start, end);
        range.ShouldBe(expected);
    }

    [Fact]
    public void GenerateRange_ThrowsOnInvalidRange()
    {
        Should.Throw<ArgumentException>(() => GenerateRange(5, 1));
    }

    [Theory]
    [InlineData(12L, 8L, 4L, 3L, 2L)]       // basic reduction
    [InlineData(25L, 15L, 5L, 5L, 3L)]      // larger numbers
    [InlineData(7L, 13L, 1L, 7L, 13L)]      // coprime numbers
    [InlineData(0L, 5L, 5L, 0L, 1L)]        // zero numerator
    [InlineData(100L, 100L, 100L, 1L, 1L)]  // equal numbers
    [InlineData(123456789L, 9123456789L, 9L, 13717421L, 1013717421L)]  // large numbers
    public void GreatestCommonDenominatorLong_Works(long initialNum, long initialDen, long expectedGcd, long expectedNum, long expectedDen)
    {
        long numerator = initialNum;
        long denominator = initialDen;

        GreatestCommonDenominator(ref numerator, ref denominator, out long gcd);

        gcd.ShouldBe(expectedGcd);
        numerator.ShouldBe(expectedNum);
        denominator.ShouldBe(expectedDen);
    }

    [Theory]
    [InlineData(12, 8, 4, 3, 2)]       // basic reduction
    [InlineData(25, 15, 5, 5, 3)]      // larger numbers
    [InlineData(7, 13, 1, 7, 13)]      // coprime numbers
    [InlineData(0, 5, 5, 0, 1)]        // zero numerator
    [InlineData(100, 100, 100, 1, 1)]  // equal numbers
    [InlineData(123456, 9123456, 192, 643, 47518)]  // large numbers
    public void GreatestCommonDenominatorInt_Works(int initialNum, int initialDen, int expectedGcd, int expectedNum, int expectedDen)
    {
        int numerator = initialNum;
        int denominator = initialDen;

        GreatestCommonDenominator(ref numerator, ref denominator, out int gcd);

        gcd.ShouldBe(expectedGcd);
        numerator.ShouldBe(expectedNum);
        denominator.ShouldBe(expectedDen);
    }

    [Theory]
    [InlineData(12, 8, 4, 3, 2)]       // basic reduction
    [InlineData(25, 15, 5, 5, 3)]      // larger numbers
    [InlineData(7, 13, 1, 7, 13)]      // coprime numbers
    [InlineData(0, 5, 5, 0, 1)]        // zero numerator
    [InlineData(100, 100, 100, 1, 1)]  // equal numbers
    [InlineData(123456, 9123456, 192, 643, 47518)]  // large numbers
    [InlineData(123456.6, 9123456.6, 1.8, 68587, 5068587)]  // large numbers with decimals
    public void GreatestCommonDenominatorDecimal_Works(double initialNum, double initialDen, double expectedGcd, double expectedNum, double expectedDen)
    {
        decimal numerator = (decimal)initialNum;
        decimal denominator = (decimal)initialDen;

        GreatestCommonDenominator(ref numerator, ref denominator, out decimal gcd);

        gcd.ShouldBe((decimal)expectedGcd);
        numerator.ShouldBe((decimal)expectedNum);
        denominator.ShouldBe((decimal)expectedDen);
    }

    [Fact]
    public void GetPrecision_RespectsCurrentCulture()
    {
        // Arrange
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // Test with different decimal separators
            CultureInfo.CurrentCulture = new CultureInfo("en-US"); // Uses "."
            const double valueUS = 123.45;
            const decimal decimalUS = 123.45m;

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); // Uses ","
            const double valueFR = 123.45;
            const decimal decimalFR = 123.45m;

            // Act & Assert
            valueUS.GetPrecision().ShouldBe(2);
            decimalUS.GetPrecision().ShouldBe(2);
            valueFR.GetPrecision().ShouldBe(2);
            decimalFR.GetPrecision().ShouldBe(2);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
