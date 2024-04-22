using System.Diagnostics.CodeAnalysis;
using static Common_Net_Funcs.Tools.StringHelpers;

namespace Common_Net_Funcs.SQL;

public static class ParameterFilter
{
    /// <summary>
    /// Check if a parameter being used for a query contains any potentially malicious values
    /// </summary>
    /// <param name="parameter">Parameter to check</param>
    /// <returns>True if there are no suspect characters or strings in the parameter</returns>
    public static bool IsClean(this string? parameter)
    {
        return parameter.IsNullOrWhiteSpace() || (!parameter.Contains(';') && !parameter.Contains('\'') && !parameter.Contains('[') && !parameter.Contains(']') &&
            !parameter.Contains('"') && !parameter.Contains('`') && !parameter.Contains("/*") && !parameter.Contains("*/") && !parameter.Contains("xp_") && !parameter.Contains("--"));
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
