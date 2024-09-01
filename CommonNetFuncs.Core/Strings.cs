using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonNetFuncs.Core;

public enum EComparisonType
{
    OR,
    AND
}

/// <summary>
/// Methods for complex string manipulation
/// </summary>
public static partial class Strings
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex("^[a-zA-Z0-9]*$")]
    private static partial Regex AlphanumericRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\s]*$")]
    private static partial Regex AlphanumericWithSpacesRegex();

    [GeneratedRegex("^[a-zA-Z]*$")]
    private static partial Regex AlphaOnlyRegex();

    [GeneratedRegex(@"^[a-zA-Z\s]*$")]
    private static partial Regex AlphaOnlyWithSpacesRegex();

    [GeneratedRegex("^[0-9]*$")]
    private static partial Regex NumericOnlyRegex();

    [GeneratedRegex(@"^[0-9\s]*$")]
    private static partial Regex NumericOnlyWithSpacesRegex();

    /// <summary>
    /// Clone of VBA Left() function that gets n characters from the left side of the string
    /// </summary>
    /// <param name="s">String to get left substring from</param>
    /// <param name="numChars">Number of characters to take from the right side of the string</param>
    /// <returns>String of the length indicated from the left side of the source string</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? Left(this string? s, int numChars)
    {
        if (s == null)
        {
            return null;
        }
        else if (s.Length == 0)
        {
            return string.Empty;
        }
        else if (numChars <= s.Length)
        {
            return s[..numChars];
        }
        else
        {
            return s;
        }
    }

    /// <summary>
    /// Clone of VBA Right() function that gets n characters from the right side of the string
    /// </summary>
    /// <param name="s">String to extract right substring from</param>
    /// <param name="numChars">Number of characters to take from the right side of the string</param>
    /// <returns>String of the length indicated from the right side of the source string</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? Right(this string? s, int numChars)
    {
        if (s == null)
        {
            return null;
        }
        else if (s.Length == 0)
        {
            return string.Empty;
        }
        else if (numChars <= s.Length)
        {
            return s.Substring(s.Length - numChars, numChars);
        }
        else
        {
            return s;
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
    /// Parses a string that is using pascal casing (works with camel case as well) so that each word is separated by a space
    /// </summary>
    /// <param name="s">String to parse</param>
    /// <returns>Original string with spaces between all words starting with a capital letter</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ParsePascalCase(this string? s)
    {
        return !s.IsNullOrWhiteSpace() ? string.Concat(s.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ') : s;
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
        if (!s.IsNullOrWhiteSpace())
        {
            TextInfo textinfo = new CultureInfo(cultureString, false).TextInfo;
            s = textinfo.ToTitleCase(s);
        }
        return s;
    }

    /// <summary>
    /// Trims a string removing all extra leading and trailing spaces as well as reducing multiple consecutive spaces to only 1 space
    /// </summary>
    /// <param name="s">String to remove extra spaces from</param>
    /// <returns>String without leading, trailing or multiple consecutive spaces</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? TrimFull(this string? s)
    {
        if (!s.IsNullOrWhiteSpace())
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
    /// Indicates whether a specified string is null or a zero length string
    /// </summary>
    /// <param name="enumerable">Collection to check if it's null or has no elements</param>
    /// <returns>True if s is null or a zero length string</returns>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable)
    {
        return enumerable?.Any() != true;
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static bool ContainsInvariant(this string? s, string? textToFind)
    {
        return textToFind != null && (s?.Contains(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
    }

    /// <summary>
    /// Checks if the any of the values in a collection of strings contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static bool ContainsInvariant(this IEnumerable<string?>? s, string? textToFind)
    {
        return s?.Contains(textToFind, StringComparer.InvariantCultureIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static bool StartsWithInvariant(this string? s, string? textToFind)
    {
        return textToFind != null && (s?.StartsWith(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static bool EndsWithInvariant(this string? s, string? textToFind)
    {
        return textToFind != null && (s?.EndsWith(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? false);
    }

    /// <summary>
    /// Checks if the any of the values in a collection of strings contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static int IndexOfInvariant(this string? s, string? textToFind)
    {
        return textToFind != null ? s?.IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) ?? 0 : 0;
    }

    /// <summary>
    /// Checks if the any of the values in a collection of strings contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textToFind">String to find in s</param>
    /// <returns>True if s contains the string textToFind in any form</returns>
    public static int IndexOfInvariant(this string? s, char? textToFind)
    {
        return textToFind != null && textToFind != null ? s?.IndexOf((char)textToFind, StringComparison.InvariantCultureIgnoreCase) ?? 0 : 0;
    }

    /// <summary>
    /// Checks if the given string contains a specific string regardless of culture or case
    /// </summary>
    /// <param name="s">String to search</param>
    /// <param name="textsToFind">Strings to find in s</param>
    /// <param name="useOrComparison">
    /// <para>If true, will check if any of the textsToFind values are in s. (OR configuration)</para>
    /// <para>If false, will check if all of the textsToFind values are in s. (AND configuration)</para>
    /// </param>
    /// <returns>
    /// <para>True if s contains any of the strings in textsToFind in any form when useOrComparison = True</para>
    /// <para>True if s contains all of the strings in textsToFind when useOrComparison = False</para>
    /// </returns>
    public static bool ContainsInvariant(this string? s, IEnumerable<string> textsToFind, bool useOrComparison = true)
    {
        if (s.IsNullOrWhiteSpace())
        {
            return false;
        }

        if (useOrComparison)
        {
            foreach (string textToFind in textsToFind)
            {
                if(s.ContainsInvariant(textToFind))
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            foreach (string textToFind in textsToFind)
            {
                if (!s.ContainsInvariant(textToFind))
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Replace a substring with another string, ignoring the case and culture when finding the substring to replace
    /// </summary>
    /// <param name="s">String to search for substring to replace</param>
    /// <param name="oldValue">Substring to search for in string s, ignoring culture and case</param>
    /// <param name="newValue">String to replace any substrings matching oldValue with</param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ReplaceInvariant(this string? s, string oldValue, string newValue)
    {
        return s?.Replace(oldValue, newValue);
    }

    /// <summary>
    /// Compare two strings ignoring culture and case
    /// </summary>
    /// <param name="s1">First string to compare</param>
    /// <param name="s2">Second string to compare</param>
    /// <returns>True if the strings are equal when ignoring culture and case</returns>
    public static bool StrEq(this string? s1, string? s2)
    {
        return string.Equals(s1?.Trim() ?? string.Empty, s2?.Trim() ?? string.Empty, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Check string to see if a string only contains letters and numbers (a-Z A-Z 0-9). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains alphanumeric characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns>True if testString contains only letters and numbers and optionally spaces</returns>
    public static bool IsAlphanumeric(this string? testString, bool allowSpaces = false)
    {
        return testString != null && (!allowSpaces ? AlphanumericRegex().IsMatch(testString) : AlphanumericWithSpacesRegex().IsMatch(testString));
    }

    /// <summary>
    /// Check string to see if a string only contains letters (a-z A-Z). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains alphabetical characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns>True if testString only contains letters and optionally spaces</returns>
    public static bool IsAlphaOnly(this string? testString, bool allowSpaces = false)
    {
        return testString != null && (!allowSpaces ? AlphaOnlyRegex().IsMatch(testString) : AlphaOnlyWithSpacesRegex().IsMatch(testString));
    }

    /// <summary>
    /// Check string to see if a string only contains numbers (0-9). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains numeric characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns>True if testString only contains numbers and optionally spaces</returns>
    public static bool IsNumericOnly(this string? testString, bool allowSpaces = false)
    {
        return testString != null && (!allowSpaces ? NumericOnlyRegex().IsMatch(testString) : NumericOnlyWithSpacesRegex().IsMatch(testString));
    }

    /// <summary>
    /// Gets string up until before the last instance of a character (exclusive)
    /// </summary>
    /// <param name="s">String to extract from</param>
    /// <param name="charToFind">Character to find last instance of</param>
    /// <returns>String up until the last instance of charToFind (exclusive)</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ExtractToLastInstance(this string? s, char charToFind)
    {
        if (s == null) { return null; }
        int lastIndex = s.LastIndexOf(charToFind);
        return lastIndex != -1 ? s[..lastIndex] : s;
    }

    /// <summary>
    /// Gets string remaining after the last instance of a character (exclusive)
    /// </summary>
    /// <param name="s">String to extract from</param>
    /// <param name="charToFind">Character to find last instance of</param>
    /// <returns>Remaining string after the last instance of charToFind (exclusive)</returns>
    [return: NotNullIfNotNull(nameof(s))]
    public static string? ExtractFromLastInstance(this string? s, char charToFind)
    {
        if (s == null) { return null; }
        int lastIndex = s.LastIndexOf(charToFind);
        return lastIndex != -1 ? s[(lastIndex + 1)..] : s;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object
    /// </summary>
    /// <typeparam name="T">Type of object to trim strings in</typeparam>
    /// <param name="obj">Object containing string properties to be trimmed</param>
    public static T TrimObjectStrings<T>(this T obj)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string?)prop.GetValue(obj);
                    if (!value.IsNullOrEmpty())
                    {
                        prop.SetValue(obj, value.TrimFull());
                    }
                }
            }
        }
        return obj;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object with the option to also trim them
    /// </summary>
    /// <typeparam name="T">Type of object to normalize strings in</typeparam>
    /// <param name="obj">Object containing string properties to be normalized</param>
    public static T NormalizeObjectStrings<T>(this T obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string?)prop.GetValue(obj);
                    if (!value.IsNullOrEmpty())
                    {
                        if (enableTrim)
                        {
                            prop.SetValue(obj, value.TrimFull().Normalize(normalizationForm));
                        }
                        else
                        {
                            prop.SetValue(obj, value.Normalize(normalizationForm));
                        }
                    }
                }
            }
        }
        return obj;
    }
}
