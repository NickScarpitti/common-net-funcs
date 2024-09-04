using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Convert;

namespace CommonNetFuncs.Core;

public enum EYesNo
{
    Yes,
    No
}

public enum EHashAlgorithm
{
    SHA1,
    SHA256,
    SHA384,
    SHA512,
    MD5,
    RSA
}

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
    public const string TimestampUrlFormat = "yyyyMMddHHmmssFFF";

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

    /// <summary>
    /// Converts Nullable DateTime to string using the passed in formatting
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format">Date time format</param>
    /// <returns>Formatted string representation of the passed in nullable DateTime</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this DateTime? value, string? format = null)
    {
        string? output = null;
        if (value != null)
        {
            DateTime dtActual = (DateTime)value;
            output = dtActual.ToString(format);
        }
        return output;
    }

    /// <summary>
    /// Converts Nullable DateTime to string using the passed in formatting
    /// </summary>
    /// <param name="value">Timespan to convert to string</param>
    /// <param name="format">Timespan format</param>
    /// <returns>Formatted string representation of the passed in nullable Timespan</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this TimeSpan? value, string? format = null)
    {
        string? output = null;
        if (value != null)
        {
            TimeSpan tsActual = (TimeSpan)value;
            output = tsActual.ToString(format);
        }
        return output;
    }

    /// <summary>
    /// Converts nullable int to string
    /// </summary>
    /// <param name="value">Integer to convert to string</param>
    /// <returns>String representation of the passed in nullable int</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this int? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable long to string
    /// </summary>
    /// <param name="value">Long to convert to string</param>
    /// <returns>String representation of the passed in nullable long</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this long? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable double to string
    /// </summary>
    /// <param name="value">Double to convert to string</param>
    /// <returns>String representation of the passed in nullable double</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this double? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable decimal to string
    /// </summary>
    /// <param name="value">Decimal to convert to string</param>
    /// <returns>String representation of the passed in nullable decimal</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this decimal? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable object to string
    /// </summary>
    /// <param name="value">Boolean to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this bool? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable object to string
    /// </summary>
    /// <param name="value">Object to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this object? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts list of string representations of integers into list of integers
    /// </summary>
    /// <param name="values">Collection of strings to be converted to integers</param>
    /// <returns>List of integers where the strings could be parsed to integers and not null</returns>
    public static IEnumerable<int> ToListInt(this IEnumerable<string> values)
    {
        return values.Select(x => int.TryParse(x, out int i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i!.Value);
    }

    /// <summary>
    /// Converts list of string representations of integers into list of integers
    /// </summary>
    /// <param name="values">Collection of strings to be converted to integers</param>
    /// <returns>List of integers where the strings could be parsed to integers and not null</returns>
    public static List<int> ToListInt(this IList<string> values)
    {
        return values.Select(x => int.TryParse(x, out int i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i!.Value).ToList();
    }

    /// <summary>
    /// Used to reduce boilerplate code for parsing strings into nullable integers
    /// </summary>
    /// <param name="value">String value to be converted to nullable int</param>
    /// <returns>Nullable int parsed from a string</returns>
    public static int? ToNInt(this string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int i))
        {
            return i;
        }
        return null;
    }

    /// <summary>
    /// Used to reduce boilerplate code for parsing strings into nullable doubles
    /// </summary>
    /// <param name="value">String value to be converted to nullable double</param>
    /// <returns>Nullable double parsed from a string</returns>
    public static double? ToNDouble(this string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, out double i))
        {
            return i;
        }
        return null;
    }

    /// <summary>
    /// Used to reduce boilerplate code for parsing strings into nullable decimals
    /// </summary>
    /// <param name="value">String value to be converted to nullable decimal</param>
    /// <returns>Nullable decimal parsed from a string</returns>
    public static decimal? ToNDecimal(this string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && decimal.TryParse(value, out decimal i))
        {
            return i;
        }
        return null;
    }

    /// <summary>
    /// Used to reduce boilerplate code for parsing strings into nullable DateTimes
    /// </summary>
    /// <param name="value">String to parse into a DateTime</param>
    /// <returns>Nullable DateTime parsed from a string</returns>
    public static DateTime? ToNDateTime(this string? value)
    {
        DateTime? dtn = null;
        if (DateTime.TryParse(value, out DateTime dt))
        {
            dtn = dt;
        }
        else if (double.TryParse(value, out double dbl))
        {
            dtn = DateTime.FromOADate(dbl);
        }
        return dtn;
    }

    /// <summary>
    /// Convert string "Yes"/"No" value into bool
    /// </summary>
    /// <param name="value">"Yes"/"No" string to convert into a boolean</param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YesNoToBool(this string? value)
    {
        return string.Equals(value?.Trim() ?? string.Empty, nameof(EYesNo.Yes), StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Convert string "Y"/"N" value into bool
    /// </summary>
    /// <param name="value">"Y"/"N" string to convert into a boolean</param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YNToBool(this string? value)
    {
        return string.Equals(value?.Trim() ?? string.Empty, "Y", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Convert bool to "Yes" or "No"
    /// </summary>
    /// <param name="value">Boolean to convert to "Yes" or "No"</param>
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
    /// Convert bool to "Y" or "N"
    /// </summary>
    /// <param name="value">Boolean to convert to "Yes" or "No"</param>
    /// <returns>"Y" if true, "N" if false</returns>
    public static string BoolToYN(this bool value)
    {
        if (value)
        {
            return "Y";
        }
        return "N";
    }

    /// <summary>
    /// Convert bool to 1 or 0
    /// </summary>
    /// <param name="value">Integer to convert to "Yes" or "No"</param>
    /// <returns>"Yes" if true, "No" if false</returns>
    public static int BoolToInt(this bool value)
    {
        return ToInt32(value);
    }

    /// <summary>
    /// Get file name safe date in the chosen format
    /// </summary>
    /// <param name="dateFormat">Base format to get date in before doing text replacement</param>
    /// <returns>File name safe formatted date</returns>
    public static string GetSafeDate(string dateFormat)
    {
        return DateTime.Today.ToString(dateFormat).Replace("/", "-");
    }

    /// <summary>
    /// Adds number in () at the end of a file name if it would create a duplicate in the savePath
    /// </summary>
    /// <param name="savePath">Path to get unique name for</param>
    /// <param name="fileName">File name to make unique</param>
    /// <param name="extension">File extension</param>
    /// <returns>Unique file name string</returns>
    public static string MakeExportNameUnique(string savePath, string fileName, string extension)
    {
        int i = 0;
        string outputName = fileName;
        while (File.Exists(Path.Combine(savePath, outputName)))
        {
            outputName = $"{fileName[..(fileName.Length - extension.Length)]} ({i}).{extension}";
            i++;
        }
        return outputName;
    }

    /// <summary>
    /// Remove unnecessary characters and components of a timespan to make it more readable
    /// </summary>
    /// <param name="t">Timespan to convert to shortened string</param>
    /// <returns>Shortened string representation of the timespan</returns>
    public static string TimespanToShortForm(this TimeSpan? t)
    {
        string shortForm = "";
        if (t != null)
        {
            shortForm = ((TimeSpan)t).TimespanToShortForm();
        }
        return shortForm;
    }

    /// <summary>
    /// Remove unnecessary characters and components of a timespan to make it more readable
    /// </summary>
    /// <param name="t">Timespan to convert to shortened string</param>
    /// <returns>Shortened string representation of the timespan</returns>
    public static string TimespanToShortForm(this in TimeSpan t)
    {
        string stringForm = t.ToString();

        if (t.Milliseconds > 0)
        {
            stringForm = stringForm.Replace($".{stringForm.Split(".").Last()}", string.Empty); //Remove milliseconds component
        }

        stringForm = stringForm.Split(".").Last();
        string days = string.Empty;

        if (t.Days > 0)
        {
            days = t.Days.ToString();
            if (days[..1] == "0")
            {
                days = days[1..];
            }
        }
        else
        {
            if (stringForm[..3] == "00:")
            {
                stringForm = stringForm[3..];  //Remove hours if there aren't any
                if (stringForm[..1] == "0")
                {
                    stringForm = stringForm[1..]; //Remove leading 0 in minutes
                }
            }
        }

        return string.IsNullOrWhiteSpace(days) ? stringForm : days + ":" + stringForm;
    }

    /// <summary>
    /// Takes in a string and returns the hashed value of it using the passed in hashing algorithm
    /// </summary>
    /// <param name="originalString">String to be hashed</param>
    /// <param name="algorithm">Which algorithm to use for the hash operation</param>
    /// <returns>Hash string</returns>
    public static string GetHash(this string originalString, EHashAlgorithm algorithm)
    {
        byte[] bytes = algorithm switch
        {
            EHashAlgorithm.SHA1 => SHA1.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA256 => SHA256.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA384 => SHA384.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.MD5 => MD5.HashData(Encoding.UTF8.GetBytes(originalString)),
            //case EHashAlgorithm.SHA512:
            _ => SHA512.HashData(Encoding.UTF8.GetBytes(originalString)),
        };
        StringBuilder builder = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }

    /// <summary>
    /// Remove extra whitespace from a string preserving inner whitespace as a single space
    /// </summary>
    /// <param name="input">String to have whitespace normalized for</param>
    /// <returns>String without excess whitespace</returns>
    public static string NormalizeWhiteSpace(this string? input)
    {
        if (input == null) { return string.Empty; }

        input = input.Trim();

        int len = input.Length;
        int index = 0;
        int i = 0;

        char[] src = input.ToCharArray();
        bool skip = false;
        char ch;

        for (; i < len; i++)
        {
            ch = src[i];
            switch (ch)
            {
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\u2028':
                case '\u2029':
                case '\u0009':
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085':
                    if (skip)
                    {
                        continue;
                    }

                    src[index++] = ch;
                    skip = true;
                    continue;

                default:
                    skip = false;
                    src[index++] = ch;
                    continue;
            }
        }
        return new(src, 0, index);
    }

    /// <summary>
    /// Take any format of a date time string and convert it to a different format
    /// </summary>
    /// <param name="dateString">Input date string to be converted</param>
    /// <param name="sourceFormat">Format of dateString string</param>
    /// <param name="outputFormat">Format to convert to. Defaults to MM/dd/yyyy</param>
    [return: NotNullIfNotNull(nameof(dateString))]
    public static string? FormatDateString(this string? dateString, string sourceFormat, string outputFormat = "MM/dd/yyyy")
    {
        return dateString == null ? null : DateTime.ParseExact(dateString, sourceFormat, CultureInfo.InvariantCulture).ToString(string.IsNullOrWhiteSpace(outputFormat) ? "MM/dd/yyyy" : outputFormat);
    }
}
