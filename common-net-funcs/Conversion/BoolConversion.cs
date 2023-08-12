namespace Common_Net_Funcs.Conversion;

/// <summary>
/// Convert various values into a boolean value that would not work as a direct cast
/// </summary>
public static class BoolConversion
{
    /// <summary>
    /// Convert bool to "Yes" or "No"
    /// </summary>
    /// <param name="value"></param>
    /// <returns>"Yes" if true, "No" if false</returns>
    public static string BoolToYesNo(this bool value)
    {
        if (value)
        {
            return nameof(EYesNo.Yes);
        }
        return nameof(EYesNo.No);
    }

    /// <summary>
    /// Convert bool to 1 or 0
    /// </summary>
    /// <param name="value"></param>
    /// <returns>"Yes" if true, "No" if false</returns>
    public static int BoolToInt(this bool value)
    {
        return Convert.ToInt32(value);
    }
}
