using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common_Net_Funcs.Tools;

public enum EComparisonType
{
    OR,
    AND
}

/// <summary>
/// Methods for complex string manipulation
/// </summary>
public static partial class StringHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    /// <summary>
    /// Clone of VBA Left() function that gets n characters from the left side of the string
    /// </summary>
    /// <param name="s">String to get left substring from</param>
    /// <param name="numChars">Number of characters to take from the right side of the string</param>
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
    /// Clone of VBA Right() function that gets n characters from the right side of the string
    /// </summary>
    /// <param name="s">String to extract right substring from</param>
    /// <param name="numChars">Number of characters to take from the right side of the string</param>
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
    /// Extract the string between two string values
    /// </summary>
    /// <param name="s">String value to extract value from</param>
    /// <param name="sStart">Text that ends immediately before the end of the string you wish to extract</param>
    /// <param name="sEnd">Text that starts immediately after the end of the string you wish to extract</param>
    /// <returns>Extracted string found between the two given string values</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ExtractBetween(this string? s, string sStart, string sEnd)
    {
        string? result = null;
        if (s != null)
        {
            int sStartStartIndex = s.IndexOf(sStart);//Find the beginning index of the word1
            int sStartEndIndex = sStartStartIndex + sStart.Length;//Add the length of the word1 to starting index to find the end of the word1
            int sEndStartIndex = s.LastIndexOf(sEnd);//Find the beginning index of word2
            int length = sEndStartIndex - sStartEndIndex;//Length of the sub string by subtracting index beginning of word2 from the end of word1
            if (sStartStartIndex != -1 && sEndStartIndex != -1 && length > 0 && sStartEndIndex + length <= s.Length -1)
            {
                ReadOnlySpan<char> textToSlice = s.ToCharArray();
                result = textToSlice.Slice(sStartEndIndex, length).ToString();//Get the substring based on the end of word1 and length
            }
        }
        return result;
    }

    /// <summary>
    /// Makes a string with of the word "null" into a null value
    /// </summary>
    /// <param name="s">String to change to null if it contains the word "null"</param>
    /// <returns>Null is the string passed in is null or is the word null with no other text characters other than whitespace</returns>
    public static string? MakeNullNull(this string? s)
    {
        return s?.StrEq("Null") != false || s.ToUpperInvariant().Replace("NULL", "")?.Length == 0 || s.Trim().StrEq("Null") ? null : s;
    }

    /// <summary>
    /// Parses a string that is using camel casing so that each word is separated by a space
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <returns>Original string with spaces between all words starting with a capital letter</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ParseCamelCase(this string? s)
    {
        return !string.IsNullOrWhiteSpace(s) ? string.Concat(s.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ') : s;
    }

    /// <summary>
    /// Converts string to title case using the specified culture
    /// </summary>
    /// <param name="s">String to convert to title case</param>
    /// <param name="cultureString">String representation of the culture to use when converting string to title case</param>
    /// <returns>String converted to title case</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ToTitleCase(this string? s, string cultureString = "en-US")
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            TextInfo textinfo = new CultureInfo(cultureString, false).TextInfo;
            s = textinfo.ToTitleCase(s);
        }
        return s;
    }

    [return: NotNullIfNotNull(nameof(s))]
    public static string? TrimFull(this string? s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            s = MultiSpaceRegex().Replace(s.Trim(), " ");
        }
        return s?.Trim();
    }

    /// <summary>
    /// Indicates whether a specified string is null, a zero length string, or consists only of white-space characters
    /// </summary>
    /// <param name="s">The string to test</param>
    /// <returns>True if s is null, a zero length string, or consists only of white-space characters</returns>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    /// <summary>
    /// Indicates whether a specified string is null or a zero length string
    /// </summary>
    /// <param name="s">The string to test</param>
    /// <returns>True if s is null or a zero length string</returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s)
    {
        return string.IsNullOrEmpty(s);
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static bool InvariantContains(this string? s, string textToFind)
    {
        return s?.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textsToFind">Strings to find in s</param>
    /// <param name="comparisonType">Value to specify whether to do an AND or and OR check for all values of textsToFind</param>
    /// <returns>
    /// <para>True if s contains any of the strings in textsToFind in any form when comparisonType = OR</para>
    /// <para>True if s contains all of the strings in textsToFind when comparisonType = AND</para>
    /// </returns>
    public static bool InvariantContains(this string? s, IEnumerable<string> textsToFind, EComparisonType comparisonType)
    {
        if (s.IsNullOrWhiteSpace())
        {
            return false;
        }

        switch (comparisonType)
        {
            case EComparisonType.OR:
                foreach (string textToFind in textsToFind)
                {
                    if(s.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            case EComparisonType.AND:
                foreach (string textToFind in textsToFind)
                {
                    if (!s.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            default:
                return false;
        }
    }
}
