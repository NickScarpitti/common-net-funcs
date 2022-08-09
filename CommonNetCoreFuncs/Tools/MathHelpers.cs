namespace CommonNetCoreFuncs.Tools;

public static class MathHelpers
{
    public static double Ceiling(this double? value, double significance)
    {
        value ??= 0;

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return Convert.ToDouble(value);
    }

    public static decimal Ceiling(this decimal? value, decimal significance)
    {
        value ??= 0;

        if ((value % significance) != 0)
        {
            return ((int)(value / significance) * significance) + significance;
        }

        return Convert.ToDecimal(value);
    }
}
