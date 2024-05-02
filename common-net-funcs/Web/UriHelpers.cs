using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Common_Net_Funcs.Conversion.StringConversion;
using static Common_Net_Funcs.Tools.ObjectHelpers;
using static Common_Net_Funcs.Tools.StringHelpers;
using static System.Web.HttpUtility;

namespace Common_Net_Funcs.Web;

public static class UriHelpers
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
        if (values.AnyFast())
        {
            Parallel.ForEach(values, value => cleanValues.Add(value.MakeNullNull()?.Replace("\n", "").Trim()));
        }

        return (cleanValues ?? []).Where(x => x != null).ToList()!;
    }

    /// <summary>
    /// Converts list of query parameters into a query parameter string
    /// </summary>
    /// <param name="parameters">List of a type that can be converted to string</param>
    /// <param name="queryParameterName">The name to be used in front of the equals sign for the query parameter string</param>
    /// <returns>String representation of the list passed in as query parameters with the name passed in as queryParameterName</returns>
    public static string ListToQueryParameters<T>(this IEnumerable<T>? parameters, string? queryParameterName)
    {
        string queryString = string.Empty;
        bool firstItem = true;
        if (parameters?.Any() == true && !queryParameterName.IsNullOrWhiteSpace())
        {
            foreach (T parameter in parameters)
            {
                if (!firstItem)
                {
                    queryString += $"&{queryParameterName}={parameter}";
                }
                else
                {
                    queryString = $"{queryParameterName}={parameter}";
                    firstItem = false;
                }
            }
        }
        return queryString;
    }

    [return: NotNullIfNotNull(nameof(dateTime))]
    public static string? ToUriSafeString(this DateTime? dateTime, string? dateFormat = null)
    {
        return dateTime.ToNString(dateFormat ?? TimestampUrlFormat);
    }

    public static string ToUriSafeString(this DateTime dateTime, string? dateFormat = null)
    {
        return dateTime.ToString(dateFormat ?? TimestampUrlFormat);
    }

    public static DateTime? ParseUriSafeDateTime(this string? urlSafeDateTime, string? dateFormat = null)
    {
        if (DateTime.TryParseExact(urlSafeDateTime, dateFormat ?? TimestampUrlFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
        {
            return dateTime;
        }
        return null;
    }

    [return: NotNullIfNotNull(nameof(url))]
    public static string? GetRedactedUri(this string? url, string redactedString = "<REDACTED>")
    {
        if (url.IsNullOrWhiteSpace())
        {
            return null;
        }

        // Parse the URL to get the base part and the query string
        Uri uri = new(url);
        NameValueCollection queryParameters = ParseQueryString(uri.Query);

        if (queryParameters.Count > 0)
        {
            for (int i = 0; i < queryParameters.Count; i++)
            {
                queryParameters.Set(queryParameters.GetKey(i), redactedString); //Replace values with redactedString value
            }

            return uri.GetLeftPart(UriPartial.Path) + "?" + queryParameters;
        }
        else
        {
            return url;
        }
    }

    public static string GetUriFromRequest(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
