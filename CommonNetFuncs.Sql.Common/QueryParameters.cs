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
}
