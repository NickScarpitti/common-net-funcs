using System.Globalization;
using static System.Convert;

namespace CommonNetFuncs.Core;

/// <summary>
/// Helper functions for complex math operations
/// </summary>
public static class MathHelpers
{
    /// <summary>
    /// Rounds value up to the next whole value specified by significance parameter
    /// </summary>
    /// <param name="value">Value to round up</param>
    /// <param name="significance">Next step to round value parameter up to</param>
    /// <returns>Double representation of the rounded value</returns>
    public static double Ceiling(this double? value, double significance)
    {
        value ??= 0;

        if (significance == 0)
        {
            return System.Math.Ceiling((double)value);
        }

        if (value % significance != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return ToDouble(value);
    }

    /// <summary>
    /// Rounds value up to the next whole value specified by significance parameter
    /// </summary>
    /// <param name="value">Value to round up</param>
    /// <param name="significance">Next step to round value parameter up to</param>
    /// <returns>Decimal representation of the rounded value</returns>
    public static decimal Ceiling(this decimal? value, decimal significance)
    {
        value ??= 0;

        if (significance == 0)
        {
            return System.Math.Ceiling((decimal)value);
        }

        if (value % significance != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return ToDecimal(value);
    }

    /// <summary>
    /// Rounds value down to the next whole value specified by significance parameter
    /// </summary>
    /// <param name="value">Value to round up</param>
    /// <param name="significance">Next step to round value parameter down to</param>
    /// <returns>Double representation of the rounded value</returns>
    public static double Floor(this double? value, double significance)
    {
        value ??= 0;

        if (significance == 0)
        {
            return System.Math.Floor((double)value);
        }

        if (value % significance != 0)
        {
            return (int)(value / significance) * significance;
        }

        return ToDouble(value);
    }

    /// <summary>
    /// Rounds value down to the next whole value specified by significance parameter
    /// </summary>
    /// <param name="value">Value to round up</param>
    /// <param name="significance">Next step to round value parameter down to</param>
    /// <returns>Decimal representation of the rounded value</returns>
    public static decimal Floor(this decimal? value, decimal significance)
    {
        value ??= 0;

        if (significance == 0)
        {
            return System.Math.Floor((decimal)value);
        }

        if (value % significance != 0)
        {
            return (int)(value / significance) * significance;
        }

        return ToDecimal(value);
    }

    /// <summary>
    /// Get the number of decimal places of a decimal value
    /// </summary>
    /// <param name="value">Value to get the precision of</param>
    /// <returns>The number of decimal places of the given double value</returns>
    public static int GetPrecision(this decimal? value)
    {
        if (value == null) { return 0; }
        string decimalSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
        int position = value.ToString()!.IndexOf(decimalSeparator);
        return position == -1 ? 0 : value.ToString()!.Length - position - 1;
    }

    /// <summary>
    /// Get the number of decimal places of a double value
    /// </summary>
    /// <param name="value">Value to get the precision of</param>
    /// <returns>The number of decimal places of the given double value</returns>
    public static int GetPrecision(this double? value)
    {
        if (value == null) { return 0; }
        string decimalSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;
        int position = value.ToString()!.IndexOf(decimalSeparator);
        return position == -1 ? 0 : value.ToString()!.Length - position - 1;
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

    public static void GreatestCommonDenominator(ref long Numerator, ref long Denominator, out long greatestCommonDenominator)
    {
        greatestCommonDenominator = 0;
        for (int x = 1; x <= Denominator; x++)
        {
            if ((Numerator % x == 0) && (Denominator % x == 0))
            {
                greatestCommonDenominator = x;
            }
        }

        if (greatestCommonDenominator != 0)
        {
            Numerator /= greatestCommonDenominator;
            Denominator /= greatestCommonDenominator;
        }
    }
}
