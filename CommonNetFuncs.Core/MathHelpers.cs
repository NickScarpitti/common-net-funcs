using System.Globalization;

namespace CommonNetFuncs.Core;

/// <summary>
/// Helper functions for complex math operations
/// </summary>
public static class MathHelpers
{
	/// <summary>
	/// Rounds value up to the next whole value specified by significance parameter.
	/// </summary>
	/// <param name="value">Value to round up</param>
	/// <param name="significance">Next step to round value parameter up to.</param>
	/// <returns>Double representation of the rounded value.</returns>
	public static double Ceiling(this double? value, double significance)
	{
		double val = value ?? 0;

		if (significance.Equals(0))
		{
			return Math.Ceiling(val);
		}

		if ((val % significance).NotEquals(0))
		{
			return Math.Ceiling(val / significance) * significance;
		}

		return val;
	}

	/// <summary>
	/// Rounds value up to the next whole value specified by significance parameter
	/// </summary>
	/// <param name="value">Value to round up</param>
	/// <param name="significance">Next step to round value parameter up to</param>
	/// <returns>Decimal representation of the rounded value</returns>
	public static decimal Ceiling(this decimal? value, decimal significance)
	{
		decimal val = value ?? 0;

		if (significance == 0)
		{
			return Math.Ceiling(val);
		}

		if (val % significance != 0)
		{
			return Math.Ceiling(val / significance) * significance;
		}

		return val;
	}

	/// <summary>
	/// Rounds value down to the next whole value specified by significance parameter
	/// </summary>
	/// <param name="value">Value to round up</param>
	/// <param name="significance">Next step to round value parameter down to</param>
	/// <returns>Double representation of the rounded value</returns>
	public static double Floor(this double? value, double significance)
	{
		double val = value ?? 0;

		if (significance.Equals(0))
		{
			return Math.Floor(val);
		}

		if ((val % significance).NotEquals(0))
		{
			return Math.Floor(val / significance) * significance;
		}

		return val;
	}

	/// <summary>
	/// Rounds value down to the next whole value specified by significance parameter
	/// </summary>
	/// <param name="value">Value to round up</param>
	/// <param name="significance">Next step to round value parameter down to</param>
	/// <returns>Decimal representation of the rounded value</returns>
	public static decimal Floor(this decimal? value, decimal significance)
	{
		decimal val = value ?? 0;

		if (significance == 0)
		{
			return Math.Floor(val);
		}

		if (val % significance != 0)
		{
			return Math.Floor(val / significance) * significance;
		}

		return val;
	}

	/// <summary>
	/// Get the number of decimal places of a decimal value
	/// </summary>
	/// <param name="value">Value to get the precision of</param>
	/// <returns>The number of decimal places of the given double value</returns>
	public static int GetPrecision(this decimal? value)
	{
		if (value == null)
		{
			return 0;
		}
		decimal val = value ?? 0;
		int[] bits = decimal.GetBits(val);
		return (bits[3] >> 16) & 0xFF;
	}

	/// <summary>
	/// Get the number of decimal places of a double value
	/// </summary>
	/// <param name="value">Value to get the precision of</param>
	/// <returns>The number of decimal places of the given double value</returns>
	public static int GetPrecision(this double? value, string? decimalSeparator = null)
	{
		if (value == null)
		{
			return 0;
		}

		Span<char> buffer = stackalloc char[64];
		if (!value.Value.TryFormat(buffer, out int charsWritten))
		{
			return 0;
		}

		decimalSeparator ??= NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
		ReadOnlySpan<char> valueString = buffer[..charsWritten];
		int position = valueString.IndexOf(decimalSeparator);
		return position == -1 ? 0 : valueString.Length - position - 1;
	}

	/// <summary>
	/// Get the number of decimal places of a decimal value
	/// </summary>
	/// <param name="value">Value to get the precision of</param>
	/// <returns>The number of decimal places of the given double value</returns>
	public static int GetPrecision(this decimal value)
	{
		int[] bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	/// <summary>
	/// Get the number of decimal places of a double value
	/// </summary>
	/// <param name="value">Value to get the precision of</param>
	/// <param name="decimalSeparator">The decimal separator to use.</param>
	/// <returns>The number of decimal places of the given double value</returns>
	public static int GetPrecision(this double value, string? decimalSeparator = null)
	{
		Span<char> buffer = stackalloc char[64];
		if (!value.TryFormat(buffer, out int charsWritten))
		{
			return 0;
		}

		decimalSeparator ??= NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
		ReadOnlySpan<char> valueString = buffer[..charsWritten];
		int position = valueString.IndexOf(decimalSeparator);
		return position == -1 ? 0 : valueString.Length - position - 1;
	}

#if NET7_0_OR_GREATER
	/// <summary>
	/// Generates a continuous range of numbers between start and end parameters (inclusive)
	/// </summary>
	/// <param name="start">Number to start range with (inclusive)</param>
	/// <param name="end">Number to end range with (inclusive)</param>
	/// <returns>An IEnumerable containing a continuous range of numbers between start and end parameters (inclusive)</returns>
	public static IEnumerable<TNumber> GenerateRange<TNumber>(TNumber start, TNumber end) where TNumber : struct, System.Numerics.INumber<TNumber>
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<TNumber> GenerateRangeInternal()
		{
			for (TNumber i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}
#endif

	/// <summary>
	/// Generates a continuous range of numbers between start and end parameters (inclusive)
	/// </summary>
	/// <param name="start">Number to start range with (inclusive)</param>
	/// <param name="end">Number to end range with (inclusive)</param>
	/// <returns>An IEnumerable containing a continuous range of numbers between start and end parameters (inclusive)</returns>
	public static IEnumerable<int> GenerateRange(int start, int end)
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<int> GenerateRangeInternal()
		{
			for (int i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}

	/// <inheritdoc cref="GenerateRange(int, int)"/>
	public static IEnumerable<long> GenerateRange(long start, long end)
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<long> GenerateRangeInternal()
		{
			for (long i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}

	/// <inheritdoc cref="GenerateRange(int, int)"/>
	public static IEnumerable<float> GenerateRange(float start, float end)
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<float> GenerateRangeInternal()
		{
			for (float i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}

	/// <inheritdoc cref="GenerateRange(int, int)"/>
	public static IEnumerable<double> GenerateRange(double start, double end)
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<double> GenerateRangeInternal()
		{
			for (double i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}

	/// <inheritdoc cref="GenerateRange(int, int)"/>
	public static IEnumerable<decimal> GenerateRange(decimal start, decimal end)
	{
		if (start > end)
		{
			throw new ArgumentException($"Parameter '{nameof(start)}' ({start}) cannot be greater than parameter '{nameof(end)}' ({end})");
		}

		return GenerateRangeInternal();

		IEnumerable<decimal> GenerateRangeInternal()
		{
			for (decimal i = start; i <= end; i++)
			{
				yield return i;
			}
		}
	}

#if NET7_0_OR_GREATER
	/// <summary>
	/// Calculates the greatest common denominator (GCD) of the specified numerator and denominator, and reduces the numerator and denominator to their lowest terms.
	/// </summary>
	/// <remarks>Both the numerator and denominator are reduced in place to their lowest terms by dividing them by the GCD.</remarks>
	/// <param name="numerator">The numerator of the fraction. This value will be updated to the reduced numerator after the GCD is calculated.</param>
	/// <param name="denominator">The denominator of the fraction. This value will be updated to the reduced denominator after the GCD is calculated.</param>
	/// <param name="greatestCommonDenominator">Contains the greatest common denominator of the original numerator and denominator.</param>
	public static void GreatestCommonDenominator<T>(ref T numerator, ref T denominator, out T greatestCommonDenominator) where T : System.Numerics.INumber<T>
	{
		// Fast Euclidean algorithm for GCD calculation
		T a = T.Abs(numerator);
		T b = T.Abs(denominator);
		while (b != T.Zero)
		{
			T temp = b;
			b = a % b;
			a = temp;
		}

		greatestCommonDenominator = a;
		if (greatestCommonDenominator != T.Zero)
		{
			numerator /= greatestCommonDenominator;
			denominator /= greatestCommonDenominator;
		}
	}
#endif

	/// <summary>
	/// Compares two <see cref="double"/> values for equality within a specified tolerance.
	/// </summary>
	/// <remarks>This method is useful for comparing floating-point numbers, which can have precision issues.</remarks>
	/// <param name="a">The first double value to compare.</param>
	/// <param name="b">The second double value to compare.</param>
	/// <param name="tolerance">The tolerance within which the two values are considered equal.</param>
	/// <returns>True if the values are equal within the specified tolerance, otherwise, false.</returns>
	public static bool Equals(this double? a, double? b, decimal tolerance = 0.0001m)
	{
		if (a == null && b == null)
		{
			return true;
		}
		if (a == null || b == null)
		{
			return false;
		}
		return Math.Abs(a.Value - b.Value) <= (double)tolerance;
	}

	/// <summary>
	/// Compares two <see cref="double"/> values for equality within a specified tolerance.
	/// </summary>
	/// <remarks>This method is useful for comparing floating-point numbers, which can have precision issues.</remarks>
	/// <param name="a">The first double value to compare.</param>
	/// <param name="b">The second double value to compare.</param>
	/// <param name="tolerance">The tolerance within which the two values are considered not equal.</param>
	/// <returns>True if the values are not equal within the specified tolerance, otherwise, false.</returns>
	public static bool Equals(this double a, double b, decimal tolerance = 0.0001m)
	{
		return Math.Abs(a - b) <= (double)tolerance;
	}

	/// <summary>
	/// Compares two <see cref="double"/> values for inequality within a specified tolerance.
	/// </summary>
	/// <remarks>This method is useful for comparing floating-point numbers, which can have precision issues.</remarks>
	/// <param name="a">The first double value to compare.</param>
	/// <param name="b">The second double value to compare.</param>
	/// <param name="tolerance">The tolerance within which the two values are considered not equal.</param>
	/// <returns>True if the values are not equal within the specified tolerance, otherwise, false.</returns>
	public static bool NotEquals(this double? a, double? b, decimal tolerance = 0.0001m)
	{
		if (a == null && b == null)
		{
			return false;
		}
		if (a == null || b == null)
		{
			return true;
		}
		return Math.Abs(a.Value - b.Value) > (double)tolerance;
	}

	/// <summary>
	/// Compares two <see cref="double"/> values for inequality within a specified tolerance.
	/// </summary>
	/// <remarks>This method is useful for comparing floating-point numbers, which can have precision issues.</remarks>
	/// <param name="a">The first double value to compare.</param>
	/// <param name="b">The second double value to compare.</param>
	/// <param name="tolerance">The tolerance within which the two values are considered not equal.</param>
	/// <returns>True if the values are not equal within the specified tolerance, otherwise, false.</returns>
	public static bool NotEquals(this double a, double b, decimal tolerance = 0.0001m)
	{
		return Math.Abs(a - b) > (double)tolerance;
	}

#if NET7_0_OR_GREATER
	public static TNumber GetMedian<TNumber>(this IEnumerable<TNumber> numbers) where TNumber : struct, System.Numerics.INumber<TNumber>
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		TNumber[] sorted = numbers.Order().ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0
				? sorted[mid]
				: (sorted[mid - 1] + sorted[mid]) / TNumber.CreateChecked(2);
	}
#endif

	/// <inheritdoc cref="GetMedian{TNumber}(IEnumerable{TNumber})"/>
	public static int GetMedian(this IEnumerable<int> numbers)
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		int[] sorted = numbers.OrderBy(static x => x).ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}

	/// <inheritdoc cref="GetMedian{TNumber}(IEnumerable{TNumber})"/>
	public static long GetMedian(this IEnumerable<long> numbers)
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		long[] sorted = numbers.OrderBy(static x => x).ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}

	/// <inheritdoc cref="GetMedian{TNumber}(IEnumerable{TNumber})"/>
	public static float GetMedian(this IEnumerable<float> numbers)
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		float[] sorted = numbers.OrderBy(static x => x).ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}

	/// <inheritdoc cref="GetMedian{TNumber}(IEnumerable{TNumber})"/>
	public static double GetMedian(this IEnumerable<double> numbers)
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		double[] sorted = numbers.OrderBy(static x => x).ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}

	/// <inheritdoc cref="GetMedian{TNumber}(IEnumerable{TNumber})"/>
	public static decimal GetMedian(this IEnumerable<decimal> numbers)
	{
		if (numbers?.Any() != true)
		{
			throw new ArgumentException("numbers parameter cannot be null or empty.");
		}

		decimal[] sorted = numbers.OrderBy(static x => x).ToArray();

		int mid = sorted.Length / 2;
		return sorted.Length % 2 != 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}
}
