using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Convert;
using static System.Web.HttpUtility;
using static CommonNetFuncs.Core.MathHelpers;

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
    MD5
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
    public const string DateOnlyUrlFormat = "yyyyMMdd";

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

    [GeneratedRegex(@"\D+")]
    private static partial Regex ExtractNumbersRegex();

    [GeneratedRegex(@"(\d{3})(\d{4})")]
    private static partial Regex SevenDigitPhoneNumberRegex();

    [GeneratedRegex(@"(\d{3})(\d{3})(\d{4})")]
    private static partial Regex TenDigitPhoneNumberRegex();

    [GeneratedRegex(@"(\d{1})(\d{3})(\d{3})(\d{4})")]
    private static partial Regex ElevenDigitPhoneNumberRegex();

    [GeneratedRegex(@"(\d{2})(\d{3})(\d{3})(\d{4})")]
    private static partial Regex TwelveDigitPhoneNumberRegex();

    [GeneratedRegex("[A-Za-z]")]
    private static partial Regex RemoveLettersRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex RemoveNumbersRegex();

    [GeneratedRegex("[A-Za-z ]")]
    private static partial Regex LettersOnlyRegex();

    [GeneratedRegex(@"[0-9]*\.?[0-9]+")]
    private static partial Regex NumbersOnlyRegex();

    [GeneratedRegex(@"[0-9]*\.?[0-9 ]+((\/|\\)[0-9 ]*\.?[0-9]+)?")]
    private static partial Regex NumbersWithFractionsOnlyRegex();

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
    /// Makes a string with the word "null" into a null value
    /// </summary>
    /// <param name="s">String to change to null if it contains the word "null"</param>
    /// <returns>Null if the string passed in is null or is the word null with no other text characters other than whitespace</returns>
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
        return !s.IsNullOrWhiteSpace() ? string.Concat(s.Select(x => char.IsUpper(x) ? $" {x}" : x.ToString())).TrimStart(' ') : s;
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
        return textToFind != null ? s?.IndexOf((char)textToFind, StringComparison.InvariantCultureIgnoreCase) ?? 0 : 0;
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
                if (s.ContainsInvariant(textToFind))
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
        return s?.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
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
    /// Compare two strings for string equality
    /// </summary>
    /// <param name="s1">First string to compare</param>
    /// <param name="s2">Second string to compare</param>
    /// <returns>True if the strings are equal</returns>
    public static bool StrComp(this string? s1, string? s2)
    {
        return string.Equals(s1 ?? string.Empty, s2 ?? string.Empty);
    }

    /// <summary>
    /// Compare two strings with optional stringComparison parameter
    /// </summary>
    /// <param name="s1">First string to compare</param>
    /// <param name="s2">Second string to compare</param>
    /// <returns>True if the strings are equal based on the stringComparison value</returns>
    public static bool StrComp(this string? s1, string? s2, StringComparison stringComparison)
    {
        return string.Equals(s1 ?? string.Empty, s2 ?? string.Empty, stringComparison);
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
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? TrimObjectStringsR<T>(this T? obj)
    {
        if (obj == null)
        {
            IEnumerable<PropertyInfo> props = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(string));
            if (props.Any())
            {
                foreach (PropertyInfo prop in props)
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

    private static readonly ConcurrentDictionary<(Type, bool), Delegate> trimObjectStringsCache = new();

    /// <summary>
    /// Removes excess spaces in string properties inside of an object
    /// </summary>
    /// <typeparam name="T">Type of object to trim strings in</typeparam>
    /// <param name="obj">Object containing string properties to be trimmed</param>
    /// <param name="recursive">If true, will recursively apply string trimming to nested object</param>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? TrimObjectStrings<T>(this T? obj, bool recursive = false)
    {
        if (obj == null) return obj;

        Type type = typeof(T);
        (Type type, bool recursive) key = (type, recursive);

        Action<T> action = (Action<T>)trimObjectStringsCache.GetOrAdd(key, _ => CreateTrimObjectStringsExpression<T>(recursive).Compile());
        action(obj);

        return obj;
    }

    private static Expression<Action<T>> CreateTrimObjectStringsExpression<T>(bool recursive)
    {
        ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
        List<Expression> expressions = [];
        List<ParameterExpression> variables = [];

        foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
        {
            if (prop.PropertyType == typeof(string))
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);

                MethodInfo makeTrimFull = typeof(Strings).GetMethod(nameof(TrimFull))!;
                MethodCallExpression callTrimFull = Expression.Call(makeTrimFull, propExpr);
                expressions.Add(Expression.Assign(propExpr, callTrimFull));
            }
            else if (recursive && prop.PropertyType.IsClass)
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);
                MethodInfo makeTrimObjectMethod = typeof(Strings).GetMethod(nameof(TrimObjectStrings))!;
                MethodInfo genericMethod = makeTrimObjectMethod.MakeGenericMethod(prop.PropertyType);
                MethodCallExpression callMakeTrimObject = Expression.Call(genericMethod, propExpr, Expression.Constant(true));
                expressions.Add(callMakeTrimObject);
            }
        }

        BlockExpression body = Expression.Block(variables, expressions);
        return Expression.Lambda<Action<T>>(body, objParam);
    }

    public static T? NormalizeObjectStringsR<T>(this T? obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD)
    {
        if (obj != null)
        {
            IEnumerable<PropertyInfo> props = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(string));
            if (props.Any())
            {
                foreach (PropertyInfo prop in props)
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

    private static readonly ConcurrentDictionary<(Type, bool, NormalizationForm, bool), Delegate> normalizeObjectStringsCache = new();

    /// <summary>
    /// Removes excess spaces in string properties inside of an object with the option to also trim them
    /// </summary>
    /// <typeparam name="T">Type of object to normalize strings in</typeparam>
    /// <param name="obj">Object containing string properties to be normalized</param>
    /// <param name="enableTrim">If true, will trim all object strings</param>
    /// <param name="normalizationForm">String normalization setting</param>
    /// <param name="recursive">If true, will recursively apply string normalization to nested object</param>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? NormalizeObjectStrings<T>(this T? obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD, bool recursive = false)
    {
        if (obj == null) return obj;

        Type type = typeof(T);
        (Type type, bool enableTrim, NormalizationForm normalizationForm, bool recursive) key = (type, enableTrim, normalizationForm, recursive);

        Action<T> action = (Action<T>)normalizeObjectStringsCache.GetOrAdd(key, _ => CreateNormalizeObjectStringsExpression<T>(enableTrim, normalizationForm, recursive).Compile());
        action(obj);

        return obj;
    }

    private static Expression<Action<T>> CreateNormalizeObjectStringsExpression<T>(bool enableTrim, NormalizationForm normalizationForm, bool recursive)
    {
        ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
        List<Expression> expressions = [];
        List<ParameterExpression> variables = [];

        foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
        {
            if (prop.PropertyType == typeof(string))
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);

                // Create a local variable to store the property value
                ParameterExpression localVar = Expression.Variable(typeof(string), prop.Name);
                variables.Add(localVar);
                expressions.Add(Expression.Assign(localVar, propExpr));

                // Create the null check
                Expression nullCheck = Expression.NotEqual(localVar, Expression.Constant(null, typeof(string)));

                Expression stringOperations;
                if (enableTrim)
                {
                    MethodInfo trimMethod = typeof(Strings).GetMethod(nameof(TrimFull))!;
                    MethodCallExpression callTrimMethod = Expression.Call(trimMethod, localVar);
                    MethodInfo makeNormalizeMethod = typeof(string).GetMethod(nameof(string.Normalize), [typeof(NormalizationForm)])!;
                    stringOperations = Expression.Call(callTrimMethod, makeNormalizeMethod, Expression.Constant(normalizationForm));
                }
                else
                {
                    MethodInfo makeNormalizeMethod = typeof(string).GetMethod(nameof(string.Normalize), [typeof(NormalizationForm)])!;
                    stringOperations = Expression.Call(localVar, makeNormalizeMethod, Expression.Constant(normalizationForm));
                }

                // Combine the null check with the string operations
                Expression conditionalOperation = Expression.Condition(nullCheck, stringOperations, localVar);

                // Assign the result back to the property
                expressions.Add(Expression.Assign(propExpr, conditionalOperation));
            }
            else if (recursive && prop.PropertyType.IsClass)
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);
                MethodInfo makeObjectNullNullMethod = typeof(Strings).GetMethod(nameof(NormalizeObjectStrings))!;
                MethodInfo genericMethod = makeObjectNullNullMethod.MakeGenericMethod(prop.PropertyType);
                MethodCallExpression callMakeObjectNullNull = Expression.Call(genericMethod, propExpr, Expression.Constant(enableTrim), Expression.Constant(normalizationForm), Expression.Constant(true));

                // Add null check for recursive call
                Expression nullCheck = Expression.NotEqual(propExpr, Expression.Constant(null));
                Expression conditionalCall = Expression.IfThen(nullCheck, callMakeObjectNullNull);
                expressions.Add(conditionalCall);
            }
        }

        BlockExpression body = Expression.Block(variables, expressions);
        return Expression.Lambda<Action<T>>(body, objParam);
    }

    /// <summary>
    /// Makes string properties in an object with the word "null" into a null value
    /// </summary>
    /// <param name="obj">Object containing string properties to be set to null if null</param>
    /// <returns>Objects with properties set to null if the string property is null or is the word "null" with no other text characters other than whitespace</returns>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? MakeObjectNullNullR<T>(this T? obj)
    {
        if (obj != null)
        {
            IEnumerable<PropertyInfo> props = typeof(T).GetProperties().Where(x => x.PropertyType == typeof(string));
            if (props.Any())
            {
                foreach (PropertyInfo prop in props)
                {
                    string? value = (string?)prop.GetValue(obj);
                    prop.SetValue(obj, value.MakeNullNull());
                }
            }
        }
        return obj;
    }

    private static readonly ConcurrentDictionary<(Type, bool), Delegate> makeObjectNullNullCache = new();

    /// <summary>
    /// Makes string properties in an object with the word "null" into a null value
    /// </summary>
    /// <param name="obj">Object containing string properties to be set to null if null</param>
    /// <param name="recursive">If true, will recursively apply nullification to nested objects</param>
    /// <returns>Objects with properties set to null if the string property is null or is the word "null" with no other text characters other than whitespace</returns>
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? MakeObjectNullNull<T>(this T? obj, bool recursive = false)
    {
        if (obj == null) return obj;

        Type type = typeof(T);
        (Type type, bool recursive) key = (type, recursive);

        Action<T> action = (Action<T>)makeObjectNullNullCache.GetOrAdd(key, _ => CreateMakeObjectNullNullExpression<T>(recursive).Compile());
        action(obj);

        return obj;
    }

    private static Expression<Action<T>> CreateMakeObjectNullNullExpression<T>(bool recursive)
    {
        ParameterExpression objParam = Expression.Parameter(typeof(T), "obj");
        List<Expression> expressions = [];

        foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.PropertyType == typeof(string) || (recursive && x.PropertyType.IsClass)))
        {
            if (prop.PropertyType == typeof(string))
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);
                MethodInfo makeNullNullMethod = typeof(Strings).GetMethod(nameof(MakeNullNull))!;
                MethodCallExpression callMakeNullNull = Expression.Call(makeNullNullMethod, propExpr);
                expressions.Add(Expression.Assign(propExpr, callMakeNullNull));
            }
            else //if (recursive && prop.PropertyType.IsClass) //Can use else here since property filter means only valid properties that are not strings will make it here
            {
                MemberExpression propExpr = Expression.Property(objParam, prop);
                MethodInfo makeObjectNullNullMethod = typeof(Strings).GetMethod(nameof(MakeObjectNullNull))!;
                MethodInfo genericMethod = makeObjectNullNullMethod.MakeGenericMethod(prop.PropertyType);
                MethodCallExpression callMakeObjectNullNull = Expression.Call(genericMethod, propExpr, Expression.Constant(true));
                expressions.Add(callMakeObjectNullNull);
            }
        }

        BlockExpression body = Expression.Block(expressions);
        return Expression.Lambda<Action<T>>(body, objParam);
    }

    /// <summary>
    /// Converts Nullable DateTime to string using the passed in formatting
    /// </summary>
    /// <param name="value">DateTime to convert to string</param>
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
    /// <param name="value">DateOnly to convert to string</param>
    /// <param name="format">Date format</param>
    /// <returns>Formatted string representation of the passed in nullable DateOnly</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this DateOnly? value, string? format = null)
    {
        string? output = null;
        if (value != null)
        {
            DateOnly dtActual = (DateOnly)value;
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
        return value?.ToString();
    }

    /// <summary>
    /// Converts nullable long to string
    /// </summary>
    /// <param name="value">Long to convert to string</param>
    /// <returns>String representation of the passed in nullable long</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this long? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Converts nullable double to string
    /// </summary>
    /// <param name="value">Double to convert to string</param>
    /// <returns>String representation of the passed in nullable double</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this double? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Converts nullable decimal to string
    /// </summary>
    /// <param name="value">Decimal to convert to string</param>
    /// <returns>String representation of the passed in nullable decimal</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this decimal? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Converts nullable object to string
    /// </summary>
    /// <param name="value">Boolean to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this bool? value)
    {
        return value?.ToString();
    }

    /// <summary>
    /// Converts nullable object to string
    /// </summary>
    /// <param name="value">Object to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this object? value)
    {
        return value?.ToString();
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
    /// Used to reduce boilerplate code for parsing strings into nullable DateOnlys
    /// </summary>
    /// <param name="value">String to parse into a DateOnly</param>
    /// <returns>Nullable DateOnly parsed from a string</returns>
    public static DateOnly? ToNDateOnly(this string? value)
    {
        DateOnly? dtn = null;
        if (DateOnly.TryParse(value, out DateOnly dt))
        {
            dtn = dt;
        }
        else if (double.TryParse(value, out double dbl))
        {
            dtn = DateOnly.FromDateTime(DateTime.FromOADate(dbl));
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
            if (days[..1].StrComp("0"))
            {
                days = days[1..];
            }
        }
        else
        {
            if (stringForm[..3].StrComp("00:"))
            {
                stringForm = stringForm[3..];  //Remove hours if there aren't any
                if (stringForm[..1].StrComp("0"))
                {
                    stringForm = stringForm[1..]; //Remove leading 0 in minutes
                }
            }
        }

        return string.IsNullOrWhiteSpace(days) ? stringForm : $"{days}:{stringForm}";
    }

    /// <summary>
    /// Takes in a string and returns the hashed value of it using the passed in hashing algorithm
    /// </summary>
    /// <param name="originalString">String to be hashed</param>
    /// <param name="algorithm">Which algorithm to use for the hash operation</param>
    /// <returns>Hash string</returns>
    public static string GetHash(this string originalString, EHashAlgorithm algorithm)
    {
        ReadOnlySpan<byte> bytes = algorithm switch
        {
            EHashAlgorithm.SHA1 => SHA1.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA256 => SHA256.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA384 => SHA384.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.MD5 => MD5.HashData(Encoding.UTF8.GetBytes(originalString)),
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
    /// <returns>Date formatted as a string following the output format</returns>
    [return: NotNullIfNotNull(nameof(dateString))]
    public static string? FormatDateString(this string? dateString, string sourceFormat, string outputFormat = "MM/dd/yyyy")
    {
        return dateString == null ? null : DateTime.ParseExact(dateString, sourceFormat, CultureInfo.InvariantCulture).ToString(string.IsNullOrWhiteSpace(outputFormat) ? "MM/dd/yyyy" : outputFormat);
    }

    /// <summary>
    /// Replaces any characters that don't match the provided regexPattern with specified replacement string.
    /// </summary>
    /// <param name="input">String to apply regex / replacement to</param>
    /// <param name="regexPattern">Regex pattern used to white list characters in input</param>
    /// <param name="replacement">String to replace any characters that aren't matched by the regex pattern</param>
    /// <param name="matchFirstOnly">If true, will only white list the first match of the regex pattern. If false, all matches with the regex pattern are white listed</param>
    /// <returns>String with any non-matching characters replaced by the replacement string</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? ReplaceInverse(this string? input, string regexPattern, string? replacement = "", bool matchFirstOnly = false)
    {
        if (input.IsNullOrEmpty()) return input;
        Regex regex = new(regexPattern);
        return regex.ReplaceInverse(input, replacement, matchFirstOnly);
    }

    /// <summary>
    /// Replaces any characters that don't match the provided regexPattern with specified replacement string.
    /// </summary>
    /// <param name="regex">Regex used to white list characters in input</param>
    /// <param name="input">String to apply regex / replacement to</param>
    /// <param name="replacement">String to replace any characters that aren't matched by the regex pattern</param>
    /// <param name="matchFirstOnly">If true, will only white list the first match of the regex pattern. If false, all matches with the regex pattern are white listed</param>
    /// <returns>String with any non-matching characters replaced by the replacement string</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? ReplaceInverse(this Regex regex, string? input, string? replacement = "", bool matchFirstOnly = false)
    {
        if (input.IsNullOrEmpty()) return input;
        replacement ??= string.Empty;

        // Use StringBuilder to build the result
        StringBuilder result = new();
        int lastMatchEnd = 0;

        foreach (Match match in regex.Matches(input))
        {
            // Append non-matching parts before the current match
            if (match.Index > lastMatchEnd)
            {
                // Append the replacement string only if it's not empty
                if (replacement.Length > 0)
                {
                    result.Append(replacement);
                }
            }
            // Append the matched value
            result.Append(match.Value);
            lastMatchEnd = match.Index + match.Length;

            if (matchFirstOnly)
            {
                break;
            }
        }

        // Append any remaining non-matching characters after the last match
        if (lastMatchEnd < input.Length && replacement.Length > 0)
        {
            result.Append(replacement);
        }

        return result.ToString();
    }

    /// <summary>
    /// URL Encodes a string but then replaces specific escape sequences with their decoded character. This method is mainly for logging user defined values in a safe manner.
    /// </summary>
    /// <param name="input">Input string to be URL encoded</param>
    /// <param name="replaceEscapeSequences">
    /// <para>List of key value pairs where the key is the escape sequence to replace and the value is the value to replace the escape sequence with.</para>
    /// <para>If null or empty, will use default escape sequence replacements "%20" -> " ", "%2F" -> "/", "%5C" -> @"\", "%7C" -> "|", "%28" -> "(", "%29" -> "(", and "%2A" -> "*"</para>
    /// </param>
    /// <param name="appendDefaultEscapeSequences">
    /// <para>If true, will append the default escape sequence replacements to any passed in through replaceEscapeSequences</para>
    /// <para>The default escape sequence replacements are "%20" -> " ", "%2F" -> "/", "%5C" -> @"\", "%7C" -> "|", "%28" -> "(", "%29" -> "(", and "%2A" -> "*"</para>
    /// </param>
    /// <returns>URL encoded string with the specified escape sequences replaced with their given values</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? UrlEncodeReadable(this string? input, List<KeyValuePair<string, string>>? replaceEscapeSequences = null, bool appendDefaultEscapeSequences = true)
    {
        if (input.IsNullOrWhiteSpace()) { return input; }
        List<KeyValuePair<string, string>> defaultEscapeSequences = [new("%20", " "), new("+", " "), new("%2F", "/"), new("%5C", @"\"), new("%7C", "|"), new("%28", "("), new("%29", "("), new("%2A", "*")];
        if (replaceEscapeSequences == null || replaceEscapeSequences.Count == 0)
        {
            replaceEscapeSequences = defaultEscapeSequences;
        }
        else if (appendDefaultEscapeSequences)
        {
            replaceEscapeSequences.AddRange(defaultEscapeSequences.Where(x => !replaceEscapeSequences.Any(y => y.Key.StrEq(x.Key))));
        }

        string output = UrlEncode(input);
        foreach (KeyValuePair<string, string> replaceEscapeSequence in replaceEscapeSequences)
        {
            output = output.ReplaceInvariant(replaceEscapeSequence.Key, replaceEscapeSequence.Value);
        }

        return output;
    }

    /// <summary>
    /// Formats a string as a phone number
    /// </summary>
    /// <param name="input">String to be formatted as phone number</param>
    /// <param name="separator">Character to be used to separate segments of the phone number (country code excluded)</param>
    /// <param name="addParenToAreaCode">If true, will add parentheses around the area code, eg. +1 (123)-456-7890 instead of +1 123-456-7890</param>
    /// <returns>String formatted as a phone number</returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? FormatPhoneNumber(this string? input, string separator = "-", bool addParenToAreaCode = false)
    {
        if (input.IsNullOrWhiteSpace())
        {
            return input;
        }

        string[] phoneParts = input.ToLowerInvariant().Split("x");
        string? extension = phoneParts.Length > 1 ? phoneParts[1] : null;

        input = string.Concat(ExtractNumbersRegex().Split(phoneParts[0]));

        Regex? phoneParser;
        string format;

        switch (input.Length)
        {
            case 7:
                phoneParser = SevenDigitPhoneNumberRegex();
                format = $"$1{separator}$2";
                break;

            case 10:
                phoneParser = TenDigitPhoneNumberRegex();
                format = !addParenToAreaCode ? $"$1{separator}$2{separator}$3" : $"($1){separator}$2{separator}$3";
                break;

            case 11:
                phoneParser = ElevenDigitPhoneNumberRegex();
                format = !addParenToAreaCode ? $"+$1 $2{separator}$3{separator}$4" : $"+$1 ($2){separator}$3{separator}$4";
                break;

            case 12:
                phoneParser = TwelveDigitPhoneNumberRegex();
                format = !addParenToAreaCode ? $"+$1 $2{separator}$3{separator}$4" : $"+$1 ($2){separator}$3{separator}$4";
                break;
            default:
                if (extension != null)
                {
                    input += $"x{extension}";
                }
                return input;

        }
        input = phoneParser.Replace(input, format);
        if (extension != null)
        {
            input += $"x{extension}";
        }
        return input;
    }

    [return: NotNullIfNotNull(nameof(input))]
    public static IEnumerable<string> SplitLines(this string? input)
    {
        if (input == null)
        {
            yield break;
        }
        string? line;
        using StringReader sr = new(input);
        while ((line = sr.ReadLine()) != null)
        {
            yield return line;
        }
    }

    [return: NotNullIfNotNull(nameof(number))]
    public static string? ToFractionString(this decimal? number, int maxNumberOfDecimalsToConsider)
    {
        if (number == null) return null;
        int wholeNumberPart = (int)number;
        decimal decimalNumberPart = (decimal)number - ToDecimal(wholeNumberPart);
        long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
        long numerator = (long)(decimalNumberPart * denominator);
        GreatestCommonDenominator(ref numerator, ref denominator, out long _);
        return $"{wholeNumberPart} {numerator}/{denominator}";
    }

    [return: NotNullIfNotNull(nameof(number))]
    public static string? ToFractionString(this decimal number, int maxNumberOfDecimalsToConsider)
    {
        int wholeNumberPart = (int)number;
        decimal decimalNumberPart = (decimal)number - ToDecimal(wholeNumberPart);
        long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
        long numerator = (long)(decimalNumberPart * denominator);
        GreatestCommonDenominator(ref numerator, ref denominator, out long _);
        return $"{wholeNumberPart} {numerator}/{denominator}";
    }

    [return: NotNullIfNotNull(nameof(number))]
    public static string? ToFractionString(this double? number, int maxNumberOfDecimalsToConsider)
    {
        if (number == null) return null;
        int wholeNumberPart = (int)number;
        double decimalNumberPart = (double)number - ToDouble(wholeNumberPart);
        long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
        long numerator = (long)(decimalNumberPart * denominator);
        GreatestCommonDenominator(ref numerator, ref denominator, out long _);
        return $"{wholeNumberPart} {numerator}/{denominator}";
    }

    [return: NotNullIfNotNull(nameof(number))]
    public static string? ToFractionString(this double number, int maxNumberOfDecimalsToConsider)
    {
        int wholeNumberPart = (int)number;
        double decimalNumberPart = (double)number - ToDouble(wholeNumberPart);
        long denominator = (long)Math.Pow(10, maxNumberOfDecimalsToConsider);
        long numerator = (long)(decimalNumberPart * denominator);
        GreatestCommonDenominator(ref numerator, ref denominator, out long _);
        return $"{wholeNumberPart} {numerator}/{denominator}";
    }

    [return: NotNullIfNotNull(nameof(fractionString))]
    public static decimal? FractionToDecimal(this string? fractionString)
    {
        if (fractionString == null) return null;
        if (decimal.TryParse(fractionString, out decimal result))
        {
            return result;
        }

        string[] split = fractionString.Split([' ', '/']);

        if (split.Length == 2 || split.Length == 3)
        {
            if (int.TryParse(split[0], out int a) && int.TryParse(split[1], out int b))
            {
                if (split.Length == 2)
                {
                    return (decimal)a / b;
                }

                if (int.TryParse(split[2], out int c))
                {
                    return a + (decimal)b / c;
                }
            }
        }

        throw new FormatException("Not a valid fraction.");
    }

    public static bool TryFractionToDecimal(this string? fractionString, [NotNullWhen(true)] out decimal? result)
    {
        result = null;
        bool success = true;
        try
        {
            result = fractionString.FractionToDecimal();
        }
        catch (Exception)
        {
            success = false;
        }
        return success;
    }

    [return: NotNullIfNotNull(nameof(fractionString))]
    public static double? FractionToDouble(this string? fractionString)
    {
        if (fractionString == null) return null;
        if (double.TryParse(fractionString, out double result))
        {
            return result;
        }

        string[] split = fractionString.Split([' ', '/']);

        if (split.Length == 2 || split.Length == 3)
        {
            if (int.TryParse(split[0], out int a) && int.TryParse(split[1], out int b))
            {
                if (split.Length == 2)
                {
                    return (double)a / b;
                }

                if (int.TryParse(split[2], out int c))
                {
                    return a + (double)b / c;
                }
            }
        }

        throw new FormatException("Not a valid fraction.");
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static string? RemoveLetters(this string? value)
    {
        if (value.IsNullOrWhiteSpace()) return null;
        return RemoveLettersRegex().Replace(value, string.Empty);
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static string? RemoveNumbers(this string? value)
    {
        if (value.IsNullOrWhiteSpace()) return null;
        return RemoveNumbersRegex().Replace(value, string.Empty);
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static string? GetOnlyLetters(this string? value)
    {
        if (value.IsNullOrWhiteSpace()) return null;
        return LettersOnlyRegex().Match(value).Value.Trim();
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static string? GetOnlyNumbers(this string? value, bool allowFractions = false)
    {
        if (value.IsNullOrWhiteSpace()) return null;
        return !allowFractions ? NumbersOnlyRegex().Match(value).Value.Trim() : NumbersWithFractionsOnlyRegex().Match(value).Value.Trim();
    }
}
