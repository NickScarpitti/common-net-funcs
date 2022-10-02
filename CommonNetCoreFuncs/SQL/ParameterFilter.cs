namespace CommonNetCoreFuncs.SQL;

public static class ParameterFilter
{
    /// <summary>
    /// Check if a parameter being used for a query contains any potentially malicious values
    /// </summary>
    /// <param name="parameter">Parameter to check</param>
    /// <returns>True if there are no suspect characters or strings in the parameter</returns>
    public static bool IsClean(this string? parameter)
    {
        return string.IsNullOrWhiteSpace(parameter) || (!parameter.Contains(';') && !parameter.Contains('\'') && !parameter.Contains('[') && !parameter.Contains(']') && !parameter.Contains('"') && !parameter.Contains('`') &&
            !parameter.Contains("select ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("from ", StringComparison.InvariantCultureIgnoreCase) &&
            !parameter.Contains("where ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("having ", StringComparison.InvariantCultureIgnoreCase) &&
            !parameter.Contains("select ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("group by ", StringComparison.InvariantCultureIgnoreCase) &&
            !parameter.Contains("order by ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("join ", StringComparison.InvariantCultureIgnoreCase) &&
            !parameter.Contains("union ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("max(", StringComparison.InvariantCultureIgnoreCase));
    }
}
