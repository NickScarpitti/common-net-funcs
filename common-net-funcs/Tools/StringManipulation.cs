namespace Common_Net_Funcs.Tools;

/// <summary>
/// Methods for complex string manipulation
/// </summary>
public static class StringManipulation
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Clone of VBA Left() function
    /// </summary>
    /// <param name="s"></param>
    /// <param name="numChars"></param>
    /// <returns>String of the length indicated from the left side of the source string</returns>
    public static string? Left(this string? s, int numChars)
    {
        try
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            if (numChars <= s.Length)
            {
                return s[..numChars];
            }
            else
            {
                return s;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting left chars");
            return null;
        }
    }

    /// <summary>
    /// Clone of VBA Right() function
    /// </summary>
    /// <param name="s"></param>
    /// <param name="numChars"></param>
    /// <returns>String of the length indicated from the right side of the source string</returns>
    public static string? Right(this string? s, int numChars)
    {
        try
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            if (numChars <= s.Length)
            {
                return s.Substring(s.Length - numChars, numChars);
            }
            else
            {
                return s;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting right chars");
            return null;
        }
    }

    /// <summary>
    /// Makes a string with of the word "null" into a null value
    /// </summary>
    /// <param name="s"></param>
    /// <returns>Null is the string passed in is null or is the word null with no other text characters other than whitespace</returns>
    public static string? MakeNullNull(this string? s)
    {
        if (s?.StrEq("Null") != false || s.ToUpperInvariant().Replace("NULL", "")?.Length == 0 || s.Trim().StrEq("Null"))
        {
            return null;
        }
        return s;
    }

    /// <summary>
    /// Parses a string that is using camel casing so that each word is separated by a space
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <returns>Original string with spaces between all words starting with a capital letter</returns>
    public static string? ParseCamelCase(this string? s)
    {
        return !string.IsNullOrWhiteSpace(s) ? string.Concat(s.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ') : s;
    }
}
