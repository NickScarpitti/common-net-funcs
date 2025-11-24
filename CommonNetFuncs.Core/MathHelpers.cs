using System.Globalization;
using static System.Convert;

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

		if (significance == 0)
		{
			return Math.Ceiling(val);
		}

		if (val % significance != 0)
		{
			return Math.Ceiling(val / significance) * significance;
			//return ((int)(value / significance) * significance) + (value > 0 ? significance : 0);
		}

		return ToDouble(val);
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
			//return ((int)(value / significance) * significance) + (value > 0 ? significance : 0);
		}

		return ToDecimal(val);
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

		if (significance == 0)
		{
			return Math.Floor(val);
		}

		if (val % significance != 0)
		{
			return Math.Floor(val / significance) * significance;
			//return (int)(value / significance) * significance - (value > 0 ? 0 : significance);
		}

		return ToDouble(val);
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
			//return (int)(value / significance) * significance - (value > 0 ? 0 : significance);
		}

		return ToDecimal(val);
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

		string valueString = value.ToString() ?? string.Empty;
		decimalSeparator ??= NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
		int position = valueString!.IndexOf(decimalSeparator);
		return position == -1 ? 0 : valueString!.Length - position - 1;
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
		string valueString = value.ToString() ?? string.Empty;
		decimalSeparator ??= NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
		int position = valueString!.IndexOf(decimalSeparator);
		return position == -1 ? 0 : valueString!.Length - position - 1;
	}

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
		return Enumerable.Range(start, end - start + 1);
	}

	/// <summary>
	/// Calculates the greatest common denominator (GCD) of the specified numerator and denominator, and reduces the numerator and denominator to their lowest terms.
	/// </summary>
	/// <remarks>Both the numerator and denominator are reduced in place to their lowest terms by dividing them by the GCD.</remarks>
	/// <param name="numerator">The numerator of the fraction. This value will be updated to the reduced numerator after the GCD is calculated.</param>
	/// <param name="denominator">The denominator of the fraction. This value will be updated to the reduced denominator after the GCD is calculated.</param>
	/// <param name="greatestCommonDenominator">Contains the greatest common denominator of the original numerator and denominator.</param>
	public static void GreatestCommonDenominator(ref long numerator, ref long denominator, out long greatestCommonDenominator)
	{
		// Fast Euclidean algorithm for GCD calculation
		long a = Math.Abs(numerator);
		long b = Math.Abs(denominator);
		while (b != 0)
		{
			long temp = b;
			b = a % b;
			a = temp;
		}

		greatestCommonDenominator = a;
		if (greatestCommonDenominator != 0)
		{
			numerator /= greatestCommonDenominator;
			denominator /= greatestCommonDenominator;
		}
	}

	/// <summary>
	/// Calculates the greatest common denominator (GCD) of the specified numerator and denominator, and reduces the numerator and denominator to their lowest terms.
	/// </summary>
	/// <remarks>Both the numerator and denominator are reduced in place to their lowest terms by dividing them by the GCD.</remarks>
	/// <param name="numerator">The numerator of the fraction. This value will be updated to the reduced numerator after the GCD is calculated.</param>
	/// <param name="denominator">The denominator of the fraction. This value will be updated to the reduced denominator after the GCD is calculated.</param>
	/// <param name="greatestCommonDenominator">Contains the greatest common denominator of the original numerator and denominator.</param>
	public static void GreatestCommonDenominator(ref int numerator, ref int denominator, out int greatestCommonDenominator)
	{
		// Fast Euclidean algorithm for GCD calculation
		int a = Math.Abs(numerator);
		int b = Math.Abs(denominator);
		while (b != 0)
		{
			int temp = b;
			b = a % b;
			a = temp;
		}

		greatestCommonDenominator = a;
		if (greatestCommonDenominator != 0)
		{
			numerator /= greatestCommonDenominator;
			denominator /= greatestCommonDenominator;
		}
	}

	/// <summary>
	/// Calculates the greatest common denominator (GCD) of the specified numerator and denominator, and reduces the numerator and denominator to their lowest terms.
	/// </summary>
	/// <remarks>Both the numerator and denominator are reduced in place to their lowest terms by dividing them by the GCD.</remarks>
	/// <param name="numerator">The numerator of the fraction. This value will be updated to the reduced numerator after the GCD is calculated.</param>
	/// <param name="denominator">The denominator of the fraction. This value will be updated to the reduced denominator after the GCD is calculated.</param>
	/// <param name="greatestCommonDenominator">Contains the greatest common denominator of the original numerator and denominator.</param>
	public static void GreatestCommonDenominator(ref decimal numerator, ref decimal denominator, out decimal greatestCommonDenominator)
	{
		// Fast Euclidean algorithm for GCD calculation
		decimal a = Math.Abs(numerator);
		decimal b = Math.Abs(denominator);
		while (b != 0)
		{
			decimal temp = b;
			b = a % b;
			a = temp;
		}

		greatestCommonDenominator = a;
		if (greatestCommonDenominator != 0)
		{
			numerator /= greatestCommonDenominator;
			denominator /= greatestCommonDenominator;
		}
	}

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

	public static bool Equals(this double a, double b, decimal tolerance = 0.0001m)
	{
		return Math.Abs(a - b) <= (double)tolerance;
	}

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

	public static bool NotEquals(this double a, double b, decimal tolerance = 0.0001m)
	{
		return Math.Abs(a - b) > (double)tolerance;
	}
}
