using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using static System.Web.HttpUtility;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Common;

public static class UriHelpers
{
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

    [return: NotNullIfNotNull(nameof(date))]
    public static string? ToUriSafeString(this DateOnly? date, string? dateFormat = null)
    {
        return date.ToNString(dateFormat ?? DateOnlyUrlFormat);
    }

    public static string ToUriSafeString(this DateOnly date, string? dateFormat = null)
    {
        return date.ToString(dateFormat ?? DateOnlyUrlFormat);
    }

    public static DateTime? ParseUriSafeDateTime(this string? urlSafeDateTime, string? dateFormat = null)
    {
        if (DateTime.TryParseExact(urlSafeDateTime, dateFormat ?? TimestampUrlFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
        {
            return dateTime;
        }
        return null;
    }

    public static DateOnly? ParseUriSafeDateOnly(this string? urlSafeDateTime, string? dateFormat = null)
    {
        if (DateOnly.TryParseExact(urlSafeDateTime, dateFormat ?? DateOnlyUrlFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateTime))
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
