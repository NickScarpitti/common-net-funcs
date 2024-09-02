using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Sql.Common;

public static class QueryParameters
{
    /// <summary>
    /// Cleans potential parsing issues out of a query parameter
    /// </summary>
    /// <param name="value">String to clean for use in a SQL query</param>
    /// <returns>String equivalent of value passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
    public static string? CleanQueryParam(this string? value)
    {
        return value.MakeNullNull()?.Replace("\n", "").Trim();
    }

    /// <summary>
    /// Cleans potential parsing issues out of a list of query parameters
    /// </summary>
    /// <param name="values">Collection of strings to clean for use in a SQL query</param>
    /// <returns>List of string equivalents of the values passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
    [return: NotNullIfNotNull(nameof(values))]
    public static IEnumerable<string>? CleanQueryParam(this IEnumerable<string>? values)
    {
        if (values == null)
        {
            return null;
        }

        ConcurrentBag<string?> cleanValues = [];
        if (values.Any())
        {
            Parallel.ForEach(values, value => cleanValues.Add(value.MakeNullNull()?.Replace("\n", "").Trim()));
        }

        return (cleanValues ?? []).Where(x => x != null)!;
    }

    /// <summary>
    /// Cleans potential parsing issues out of a list of query parameters
    /// </summary>
    /// <param name="values">List of strings to clean for use in a SQL query</param>
    /// <returns>List of string equivalents of the values passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
    [return: NotNullIfNotNull(nameof(values))]
    public static List<string>? CleanQueryParam(this IList<string>? values)
    {
        if (values == null)
        {
            return null;
        }

        ConcurrentBag<string?> cleanValues = [];
        if (values.Count > 0)
        {
            Parallel.ForEach(values, value => cleanValues.Add(value.MakeNullNull()?.Replace("\n", "").Trim()));
        }

        return (cleanValues ?? []).Where(x => x != null).ToList()!;
    }

    /// <summary>
    /// Check if a parameter being used for a query contains any potentially malicious values
    /// </summary>
    /// <param name="parameter">Parameter to check</param>
    /// <returns>True if there are no suspect characters or strings in the parameter</returns>
    public static bool IsClean(this string? parameter)
    {
        return parameter.IsNullOrWhiteSpace() || !parameter.Contains(';') && !parameter.Contains('\'') && !parameter.Contains('[') && !parameter.Contains(']') &&
            !parameter.Contains('"') && !parameter.Contains('`') && !parameter.Contains("/*") && !parameter.Contains("*/") && !parameter.Contains("xp_") && !parameter.Contains("--");
    }

    /// <summary>
    /// Checks if value has any potentially malicious or invalid values, then escapes single quotes and removes % and * wild card characters.
    /// </summary>
    /// <param name="parameter">Parameter to check</param>
    /// <param name="onlyAlphanumeric">Only allow values with numbers or letters (a-z A-Z). Overrides onlyAlphaChars and onlyNumberChars</param>
    /// <param name="onlyAlphaChars">Only allow values with letters (a-z A-Z). Overrides onlyNumberChars</param>
    /// <param name="onlyNumberChars">Only allow values with numbers (0-9).</param>
    /// <returns>A string that is safe to use as a parameter in a SQL query</returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? SanitizeSqlParameter(this string? parameter, bool onlyAlphanumeric = false, bool onlyAlphaChars = false, bool onlyNumberChars = false, int? maxLength = null, int? minLength = null, string? defaultValue = "")
    {
        string? result = null;
        if (parameter.IsClean() && (maxLength == null || (parameter?.Length ?? 0) <= maxLength) && (minLength == null || (parameter?.Length ?? 0) >= minLength))
        {
            if (onlyAlphanumeric || onlyAlphaChars || onlyNumberChars)
            {
                result = onlyAlphanumeric && parameter.IsAlphanumeric() ? parameter : onlyAlphaChars && parameter.IsAlphaOnly() ? parameter : onlyNumberChars && parameter.IsNumericOnly() ? parameter : null;
            }
            else
            {
                result = parameter;
            }
        }

        return result?.Replace("'", "''").Replace("%", "").Replace("*", "") ?? defaultValue;
    }
}
