using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using static CommonNetFuncs.Core.Strings;
using static System.Web.HttpUtility;

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
        if (parameters?.Any() != true || queryParameterName.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = new();
        bool firstItem = true;
        foreach (T parameter in parameters)
        {
            if (!firstItem)
            {
                stringBuilder.Append('&');
            }
            stringBuilder.Append(queryParameterName);
            stringBuilder.Append('=');
            stringBuilder.Append(parameter);
            firstItem = false;
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts list of query parameters into a query parameter string.
    /// </summary>
    /// <typeparam name="T">The type of the value in the KeyValuePair that can be converted to string.</typeparam>
    /// <param name="parameters">Collection of key value pairs with a value that can be converted to string.</param>
    /// <returns>String representation of the list passed in as query parameters with the name passed in as queryParameterName</returns>
    public static string ListToQueryParameters<T>(this IEnumerable<KeyValuePair<string, T>>? parameters)
    {
        if (parameters?.Any() != true)
        {
            return string.Empty;
        }

        StringBuilder stringBuilder = new();
        bool firstItem = true;
        foreach (KeyValuePair<string, T> parameter in parameters)
        {
            if (!firstItem)
            {
                stringBuilder.Append('&');
            }
            stringBuilder.Append(parameter.Key);
            stringBuilder.Append('=');
            stringBuilder.Append(parameter.Value);
            firstItem = false;
        }
        return stringBuilder.ToString();
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

            //return $"{uri.GetLeftPart(UriPartial.Path)}?{queryParameters}";
            // Use Authority instead of GetLeftPart to avoid extra slash, then rebuild the path
            string basePart = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath.TrimEnd('/')}";
            return $"{basePart}?{queryParameters}";
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
