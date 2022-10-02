namespace CommonNetCoreFuncs.Tools;

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

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return Convert.ToDouble(value);
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

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return Convert.ToDecimal(value);
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

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance);
        }

        return Convert.ToDouble(value);
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

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance);
        }

        return Convert.ToDecimal(value);
    }
}
