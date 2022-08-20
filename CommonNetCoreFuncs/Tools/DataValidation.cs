using System.Collections.Concurrent;

namespace CommonNetCoreFuncs.Tools;

/// <summary>
/// Methods for validating data
/// </summary>
public static class DataValidation
{
    /// <summary>
    /// Compares two like objects against each other to check to see if they contain the same values
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <returns>True if the two objects have the same value for all elements</returns>
    public static bool IsEqual(this object? obj1, object? obj2)
    {
        // They're both null.
        if (obj1 == null && obj2 == null) return true;
        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null) return false;
        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType()) return false;
        var Props = obj1.GetType().GetProperties();
        foreach (var Prop in Props)
        {
            var aPropValue = Prop.GetValue(obj1) ?? string.Empty;
            var bPropValue = Prop.GetValue(obj2) ?? string.Empty;

            if (aPropValue.ToString() != bPropValue.ToString())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates file extension based on list of valid extensions
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="validExtensions"></param>
    /// <returns>True if the file has a valid extension</returns>
    public static bool ValidateFileExtention(this string fileName, string[] validExtensions)
    {
        string extension = Path.GetExtension(fileName);
        if (!validExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        {
            return false;
        }
        return true;
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
    /// Provides a safe way to add a new dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void AddDictionaryItem<K, V>(this Dictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, value);
        }
    }

    public static void AddDictionaryItem<K, V>(this ConcurrentDictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        if (!dict.ContainsKey(key))
        {
            dict.TryAdd(key, value);
        }
    }
}
