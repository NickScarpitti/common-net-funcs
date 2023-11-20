using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;

namespace Common_Net_Funcs.Tools;

/// <summary>
/// Methods for validating data
/// </summary>
public static class DataValidation
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Compares two like objects against each other to check to see if they contain the same values
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <returns>True if the two objects have the same value for all elements</returns>
    public static bool IsEqual(this object? obj1, object? obj2)
    {
       return IsEqual(obj1, obj2, null);
    }

    /// <summary>
    /// Compare two class objects for value equality
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <param name="exemptProps">Names of properties to not include in the matching check</param>
    /// <returns>True if both objects contain identical values for all properties except for the ones identified by exemptProps</returns>
    public static bool IsEqual(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null)
    {
        // They're both null.
        if (obj1 == null && obj2 == null) return true;
        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null) return false;
        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType()) return false;

        IEnumerable<PropertyInfo> props = obj1.GetType().GetProperties();
        if (exemptProps?.Any() == true)
        {
            props = props.Where(x => exemptProps?.Contains(x.Name) != true);
        }
        foreach (PropertyInfo prop in props)
        {
            var aPropValue = prop.GetValue(obj1) ?? string.Empty;
            var bPropValue = prop.GetValue(obj2) ?? string.Empty;

            bool aIsNumeric = aPropValue.IsNumeric();
            bool bIsNumeric = bPropValue.IsNumeric();

            try
            {
                //This will prevent issues with numbers with varying decimal places from being counted as a difference
                if ((aIsNumeric && bIsNumeric && decimal.Parse(aPropValue.ToString()!) != decimal.Parse(bPropValue.ToString()!)) ||
                    (!(aIsNumeric && bIsNumeric) && aPropValue.ToString() != bPropValue.ToString()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates file extension based on list of valid extensions
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="validExtensions">Array of valid file extensions</param>
    /// <returns>True if the file has a valid extension</returns>
    public static bool ValidateFileExtention(this string fileName, string[] validExtensions)
    {
        string extension = Path.GetExtension(fileName);
        return validExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compare two strings ignoring culture and case
    /// </summary>
    /// <param name="s1"></param>
    /// <param name="s2"></param>
    /// <returns>True if the strings are equal when ignoring culture and case</returns>
    public static bool StrEq(this string? s1, string? s2)
    {
        return string.Equals(s1?.Trim() ?? "", s2?.Trim() ?? "", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="key">Key of new item to add to dict</param>
    /// <param name="value">Value of new item to add to dict</param>
    public static void AddDictionaryItem<K, V>(this Dictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        dict.TryAdd(key, value);
    }

    /// <summary>
    /// Provides a safe way to add a new ConcurrentDictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">ConcurrentDictionary to add item to</param>
    /// <param name="key">Key of new item to add to dict</param>
    /// <param name="value">Value of new item to add to dict</param>
    public static void AddDictionaryItem<K, V>(this ConcurrentDictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        if (!dict.ContainsKey(key))
        {
            dict.TryAdd(key, value);
        }
    }

    /// <summary>
    /// Returns whether or not the provided double value is a valid OADate
    /// </summary>
    /// <param name="oaDate">Double to check as OADate</param>
    /// <returns></returns>
    public static bool IsValidOaDate(this double oaDate)
    {
        return oaDate >= 657435.0 && oaDate <= 2958465.99999999;
    }

    /// <summary>
    /// Returns whether or not the provided double value is a valid OADate
    /// </summary>
    /// <param name="oaDate">Double to check as OADate</param>
    /// <returns></returns>
    public static bool IsValidOaDate(this double? oaDate)
    {
        return oaDate != null && oaDate >= 657435.0 && oaDate <= 2958465.99999999;
    }

    /// <summary>
    /// Check if an object is a numeric type
    /// </summary>
    /// <param name="testObject"></param>
    /// <returns></returns>
    public static bool IsNumeric(this object? testObject)
    {
        bool isNumeric = false;
        if (testObject != null && !string.IsNullOrWhiteSpace(testObject.ToString()))
        {
            isNumeric = decimal.TryParse(testObject.ToString(), NumberStyles.Number, NumberFormatInfo.InvariantInfo, out _);
        }
        return isNumeric;
    }

    /// <summary>
    /// Check string to see if a string only contains letters and numbers (a-Z A-Z 0-9). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains alphanumeric characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns></returns>
    public static bool IsAlphanumeric(this string? testString, bool allowSpaces = false)
    {
        return testString != null && Regex.IsMatch(testString, $"^[a-zA-Z0-9{(allowSpaces ? @"\s" : string.Empty)}]*$");
    }

    /// <summary>
    /// Check string to see if a string only contains letters (a-z A-Z). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains alphabetical characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns></returns>
    public static bool IsAlphaOnly(this string? testString, bool allowSpaces = false)
    {
        return testString != null && Regex.IsMatch(testString, $"^[a-zA-Z{(allowSpaces ? @"\s" : string.Empty)}]*$");
    }

    /// <summary>
    /// Check string to see if a string only contains numbers (0-9). Null returns false.
    /// </summary>
    /// <param name="testString">String to check if it only contains numeric characters</param>
    /// <param name="allowSpaces">Will count spaces as a valid character when testing the string</param>
    /// <returns></returns>
    public static bool IsNumericOnly(this string? testString, bool allowSpaces = false)
    {
        return testString != null && Regex.IsMatch(testString, $"^[0-9{(allowSpaces ? @"\s" : string.Empty)}]*$");
    }
}
