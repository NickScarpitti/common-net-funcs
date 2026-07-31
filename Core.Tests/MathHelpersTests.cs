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

	// On net10.0+ these resolve to the same generic GenerateRange<TNumber>, but on netstandard2.1 they resolve to the separate long/float/double/decimal overloads, which otherwise go untested.
	[Theory]
	[InlineData(1L, 5L, new long[] { 1, 2, 3, 4, 5 })]
	[InlineData(-2L, 2L, new long[] { -2, -1, 0, 1, 2 })]
	[InlineData(10L, 12L, new long[] { 10, 11, 12 })]
	public void GenerateRange_Long_Works(long start, long end, long[] expected)
	{
		IEnumerable<long> range = GenerateRange(start, end);
		range.ShouldBe(expected);
	}

	[Fact]
	public void GenerateRange_Long_ThrowsOnInvalidRange()
	{
		Should.Throw<ArgumentException>(() => GenerateRange(5L, 1L));
	}

	[Theory]
	[InlineData(1f, 5f, new float[] { 1, 2, 3, 4, 5 })]
	[InlineData(-2f, 2f, new float[] { -2, -1, 0, 1, 2 })]
	public void GenerateRange_Float_Works(float start, float end, float[] expected)
	{
		IEnumerable<float> range = GenerateRange(start, end);
		range.ShouldBe(expected);
	}

	[Fact]
	public void GenerateRange_Float_ThrowsOnInvalidRange()
	{
		Should.Throw<ArgumentException>(() => GenerateRange(5f, 1f));
	}

	[Theory]
	[InlineData(1.0, 5.0, new[] { 1.0, 2.0, 3.0, 4.0, 5.0 })]
	[InlineData(-2.0, 2.0, new[] { -2.0, -1.0, 0.0, 1.0, 2.0 })]
	public void GenerateRange_Double_Works(double start, double end, double[] expected)
	{
		IEnumerable<double> range = GenerateRange(start, end);
		range.ShouldBe(expected);
	}

	[Fact]
	public void GenerateRange_Double_ThrowsOnInvalidRange()
	{
		Should.Throw<ArgumentException>(() => GenerateRange(5.0, 1.0));
	}

	[Fact]
	public void GenerateRange_Decimal_Works()
	{
		IEnumerable<decimal> range = GenerateRange(1.0m, 5.0m);
		range.ShouldBe([1.0m, 2.0m, 3.0m, 4.0m, 5.0m]);
	}

	[Fact]
	public void GenerateRange_Decimal_ThrowsOnInvalidRange()
	{
		Should.Throw<ArgumentException>(() => GenerateRange(5.0m, 1.0m));
	}

	// GreatestCommonDenominator<T> relies on generic math (INumber<T>) and is fully excluded from the netstandard2.1 build of CommonNetFuncs.Core.
#if CORE_NATIVE_BUILD
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
#endif

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

	[Theory]
	[InlineData(123.0, 0)]
	[InlineData(123.1, 1)]
	[InlineData(123.12, 2)]
	[InlineData(123.123, 3)]
	[InlineData(-123.12, 2)]
	[InlineData(0.0, 0)]
	public void GetPrecision_NonNullable_Decimal_Works(double value, int expected)
	{
		decimal decimalValue = (decimal)value;
		int precision = decimalValue.GetPrecision();
		precision.ShouldBe(expected);
	}

	[Fact]
	public void GetPrecision_Double_WithCustomSeparator_Works()
	{
		const double value = 123.45;
		int precision = value.GetPrecision(".");
		precision.ShouldBe(2);
	}

	[Theory]
	[InlineData(10.0, 10.0, true)]          // equal values
	[InlineData(10.0, 10.00001, true)]      // within tolerance
	[InlineData(10.0, 10.001, false)]       // outside tolerance
	[InlineData(10.0, 11.0, false)]         // different values
	[InlineData(0.0, 0.0, true)]            // zero values
	[InlineData(-10.0, -10.0, true)]        // negative equal values
	public void Equals_Double_Works(double a, double b, bool expected)
	{
		bool result = MathHelpers.Equals(a, b);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(10.0, 10.0, true)]          // equal values
	[InlineData(10.0, 10.00001, true)]      // within tolerance
	[InlineData(10.0, 10.001, false)]       // outside tolerance
	[InlineData(10.0, 11.0, false)]         // different values
	[InlineData(null, null, true)]          // both null
	[InlineData(10.0, null, false)]         // first not null
	[InlineData(null, 10.0, false)]         // second not null
	public void Equals_NullableDouble_Works(double? a, double? b, bool expected)
	{
		bool result = MathHelpers.Equals(a, b);
		result.ShouldBe(expected);
	}

	[Fact]
	public void Equals_Double_WithCustomTolerance_Works()
	{
		const double a = 10.0;
		const double b = 10.01;

		// Within custom tolerance
		a.Equals(b, 0.1m).ShouldBeTrue();

		// Outside custom tolerance
		a.Equals(b, 0.001m).ShouldBeFalse();
	}

	[Theory]
	[InlineData(10.0, 10.0, false)]         // equal values
	[InlineData(10.0, 10.00001, false)]     // within tolerance
	[InlineData(10.0, 10.001, true)]        // outside tolerance
	[InlineData(10.0, 11.0, true)]          // different values
	[InlineData(0.0, 0.0, false)]           // zero values
	[InlineData(-10.0, -10.0, false)]       // negative equal values
	public void NotEquals_Double_Works(double a, double b, bool expected)
	{
		bool result = a.NotEquals(b);
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(10.0, 10.0, false)]         // equal values
	[InlineData(10.0, 10.00001, false)]     // within tolerance
	[InlineData(10.0, 10.001, true)]        // outside tolerance
	[InlineData(10.0, 11.0, true)]          // different values
	[InlineData(null, null, false)]         // both null
	[InlineData(10.0, null, true)]          // first not null
	[InlineData(null, 10.0, true)]          // second not null
	public void NotEquals_NullableDouble_Works(double? a, double? b, bool expected)
	{
		bool result = a.NotEquals(b);
		result.ShouldBe(expected);
	}

	[Fact]
	public void NotEquals_Double_WithCustomTolerance_Works()
	{
		const double a = 10.0;
		const double b = 10.01;

		// Within custom tolerance
		a.NotEquals(b, 0.1m).ShouldBeFalse();

		// Outside custom tolerance
		a.NotEquals(b, 0.001m).ShouldBeTrue();
	}

	[Theory]
	[InlineData(123.0, 0)]                    // Whole number
	[InlineData(123.1, 1)]                    // One decimal place
	[InlineData(123.12, 2)]                   // Two decimal places
	[InlineData(123.123, 3)]                  // Three decimal places
	[InlineData(-123.12, 2)]                  // Negative number
	[InlineData(0.0, 0)]                      // Zero
	[InlineData(double.NaN, 0)]               // NaN value
	[InlineData(double.PositiveInfinity, 0)]  // Positive infinity
	[InlineData(double.NegativeInfinity, 0)]  // Negative infinity
	public void GetPrecision_NonNullable_Double_Works(double value, int expected)
	{
		int precision = value.GetPrecision();
		precision.ShouldBe(expected);
	}

	[Theory]
	[InlineData(double.NaN, 0)]
	[InlineData(double.PositiveInfinity, 0)]
	[InlineData(double.NegativeInfinity, 0)]
	public void GetPrecision_NullableDouble_SpecialValues_Works(double value, int expected)
	{
		double? nullableValue = value;
		int precision = nullableValue.GetPrecision();
		precision.ShouldBe(expected);
	}

	[Fact]
	public void GetMedian_NullCollection_ThrowsArgumentException()
	{
		IEnumerable<int>? numbers = null;
		Should.Throw<ArgumentException>(() => numbers!.GetMedian());
	}

	[Fact]
	public void GetMedian_EmptyCollection_ThrowsArgumentException()
	{
		IEnumerable<int> numbers = [];
		Should.Throw<ArgumentException>(() => numbers.GetMedian());
	}

	[Fact]
	public void GetMedian_SingleElement_ReturnsThatElement()
	{
		int[] numbers = [42];
		numbers.GetMedian().ShouldBe(42);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3 }, 2)]              // sorted odd count
	[InlineData(new[] { 3, 1, 2 }, 2)]              // unsorted odd count
	[InlineData(new[] { 1, 3, 5, 7, 9 }, 5)]        // five elements
	[InlineData(new[] { -5, -3, -1 }, -3)]           // negative values odd count
	[InlineData(new[] { 0, 0, 0 }, 0)]              // all zeros
	public void GetMedian_OddCount_Int_ReturnsMiddleElement(int[] numbers, int expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4 }, 2)]           // sorted even count — (2+3)/2 = 2 (integer division)
	[InlineData(new[] { 4, 3, 1, 2 }, 2)]           // unsorted even count
	[InlineData(new[] { 1, 3, 5, 7 }, 4)]           // four elements — (3+5)/2 = 4
	[InlineData(new[] { -4, -2, 0, 2 }, -1)]        // negative values even count — (-2+0)/2 = -1
	[InlineData(new[] { 0, 0, 0, 0 }, 0)]           // all zeros even count
	public void GetMedian_EvenCount_Int_ReturnsAverageOfMiddleTwo(int[] numbers, int expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	// On net10.0+ these resolve to the same generic GetMedian<TNumber>, but on netstandard2.1 they resolve to the separate long/float overloads, which otherwise go untested.
	[Theory]
	[InlineData(new long[] { 1, 2, 3 }, 2L)]                // sorted odd count
	[InlineData(new long[] { 3, 1, 2 }, 2L)]                // unsorted odd count
	[InlineData(new long[] { -5, -3, -1 }, -3L)]            // negative values odd count
	public void GetMedian_OddCount_Long_ReturnsMiddleElement(long[] numbers, long expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new long[] { 1, 2, 3, 4 }, 2L)]             // sorted even count — (2+3)/2 = 2 (integer division)
	[InlineData(new long[] { 4, 3, 1, 2 }, 2L)]             // unsorted even count
	public void GetMedian_EvenCount_Long_ReturnsAverageOfMiddleTwo(long[] numbers, long expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new float[] { 1, 2, 3 }, 2f)]               // sorted odd count
	[InlineData(new float[] { 3, 1, 2 }, 2f)]               // unsorted odd count
	[InlineData(new float[] { 1.5f, 2.5f, 3.5f }, 2.5f)]    // fractional values odd count
	public void GetMedian_OddCount_Float_ReturnsMiddleElement(float[] numbers, float expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new float[] { 1, 2, 3, 4 }, 2.5f)]          // sorted even count — (2+3)/2 = 2.5
	[InlineData(new float[] { 4, 1, 3, 2 }, 2.5f)]          // unsorted even count
	public void GetMedian_EvenCount_Float_ReturnsAverageOfMiddleTwo(float[] numbers, float expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new[] { 1.0, 2.0, 3.0 }, 2.0)]             // sorted odd count
	[InlineData(new[] { 3.0, 1.0, 2.0 }, 2.0)]             // unsorted odd count
	[InlineData(new[] { 1.5, 2.5, 3.5 }, 2.5)]             // fractional values odd count
	[InlineData(new[] { -3.0, -1.0, 1.0 }, -1.0)]          // negative odd count
	public void GetMedian_OddCount_Double_ReturnsMiddleElement(double[] numbers, double expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Theory]
	[InlineData(new[] { 1.0, 2.0, 3.0, 4.0 }, 2.5)]        // sorted even count — (2+3)/2 = 2.5
	[InlineData(new[] { 4.0, 1.0, 3.0, 2.0 }, 2.5)]        // unsorted even count
	[InlineData(new[] { 1.5, 2.5, 3.5, 4.5 }, 3.0)]        // fractional even count — (2.5+3.5)/2 = 3.0
	[InlineData(new[] { -4.0, -2.0, 0.0, 2.0 }, -1.0)]     // negative even count
	public void GetMedian_EvenCount_Double_ReturnsAverageOfMiddleTwo(double[] numbers, double expected)
	{
		numbers.GetMedian().ShouldBe(expected);
	}

	[Fact]
	public void GetMedian_OddCount_Decimal_ReturnsMiddleElement()
	{
		decimal[] numbers = [1.1m, 3.3m, 2.2m];
		numbers.GetMedian().ShouldBe(2.2m);
	}

	[Fact]
	public void GetMedian_EvenCount_Decimal_ReturnsAverageOfMiddleTwo()
	{
		decimal[] numbers = [1.0m, 2.0m, 3.0m, 4.0m];
		numbers.GetMedian().ShouldBe(2.5m);
	}

	[Fact]
	public void GetMedian_UnsortedInput_SortsBeforeCalculating()
	{
		int[] numbers = [5, 1, 3, 9, 7];
		// sorted: [1, 3, 5, 7, 9] — median is 5
		numbers.GetMedian().ShouldBe(5);
	}

	[Fact]
	public void GetMedian_AllSameValues_ReturnsThatValue()
	{
		int[] numbers = [7, 7, 7, 7];
		numbers.GetMedian().ShouldBe(7);
	}

	[Fact]
	public void GetMedian_LargeCollection_ReturnsCorrectMedian()
	{
		// 1..99 — odd count, median is 50
		IEnumerable<int> numbers = Enumerable.Range(1, 99);
		numbers.GetMedian().ShouldBe(50);
	}

	[Fact]
	public void GetMedian_LargeEvenCollection_ReturnsCorrectMedian()
	{
		// 1..100 — even count, median is (50+51)/2 = 50 (integer division)
		IEnumerable<int> numbers = Enumerable.Range(1, 100);
		numbers.GetMedian().ShouldBe(50);
	}

	[Fact]
	public void GetMedian_AcceptsIEnumerable_NotJustArrays()
	{
		IEnumerable<int> numbers = [3, 1, 2];
		numbers.GetMedian().ShouldBe(2);
	}
}
