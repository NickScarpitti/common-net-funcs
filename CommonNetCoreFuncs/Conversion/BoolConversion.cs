namespace CommonNetCoreFuncs.Conversion;

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
            return EYesNo.Yes.ToString();
        }
        return EYesNo.No.ToString();
    }
}