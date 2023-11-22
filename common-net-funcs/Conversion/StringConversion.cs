using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Common_Net_Funcs.Tools.DataValidation;
using static Common_Net_Funcs.Tools.ObjectHelpers;
using static Common_Net_Funcs.Tools.StringManipulation;

namespace Common_Net_Funcs.Conversion;

public enum EYesNo
{
    Yes,
    No
}

/// <summary>
/// Methods for converting various variable types to string and vice versa
/// </summary>
public static class StringConversion
{
    /// <summary>
    /// Converts Nullable DateTime to string using the passed in formatting
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format">Date time format</param>
    /// <returns>Formatted string representation of the passed in nullable DateTime</returns>
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
    /// <param name="value"></param>
    /// <param name="format">Timespan format</param>
    /// <returns>Formatted string representation of the passed in nullable Timespan</returns>
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
    /// <param name="value"></param>
    /// <returns>String representation of the passed in nullable int</returns>
    public static string? ToNString(this int? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable long to string
    /// </summary>
    /// <param name="value"></param>
    /// <returns>String representation of the passed in nullable long</returns>
    public static string? ToNString(this long? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable double to string
    /// </summary>
    /// <param name="value"></param>
    /// <returns>String representation of the passed in nullable double</returns>
    public static string? ToNString(this double? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable decimal to string
    /// </summary>
    /// <param name="value"></param>
    /// <returns>String representation of the passed in nullable decimal</returns>
    public static string? ToNString(this decimal? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts nullable object to string
    /// </summary>
    /// <param name="value"></param>
    /// <returns>String representation of the passed in nullable object</returns>
    public static string? ToNString(this object? value)
    {
        string? output = null;
        if (value != null)
        {
            output = value.ToString();
        }
        return output;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    public static SelectListItem? ToSelectListItem(this string? value, bool selected)
    {
        return value != null ? new() { Value= value , Text = value, Selected = selected} : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    public static SelectListItem? ToSelectListItem(this string? value)
    {
        return value != null ? new() { Value = value, Text = value } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    public static SelectListItem? ToSelectListItem(this string? value, string? text, bool selected)
    {
        return value != null && text != null ? new() { Value = value, Text = text, Selected = selected } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    public static SelectListItem? ToSelectListItem(this string? value, string? text)
    {
        return value != null && text != null ? new() { Value = value, Text = text } : null;
    }

    /// <summary>
    /// Converts list of string representations of integers into list of integers
    /// </summary>
    /// <param name="values"></param>
    /// <returns>List of integers where the strings could be parsed to integers and not null</returns>
    public static IEnumerable<int> ToListInt(this IEnumerable<string> values)
    {
        return values.Select(x => int.TryParse(x, out int i) ? i : (int?)null).Where(i => i.HasValue).Select(i => i!.Value);
    }

    /// <summary>
    /// Converts list of string representations of integers into list of integers
    /// </summary>
    /// <param name="values"></param>
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
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int i))
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
        if (!string.IsNullOrEmpty(value) && double.TryParse(value, out double i))
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
    public static decimal? ToNDecimal (this string? value)
    {
        if (!string.IsNullOrEmpty(value) && decimal.TryParse(value, out decimal i))
        {
            return i;
        }
        return null;
    }

    /// <summary>
    /// Used to reduce boilerplate code for parsing strings into nullable DateTimes
    /// </summary>
    /// <param name="value"></param>
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
    /// Convert string "Yes"/"No" value into bool
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YesNoToBool(this string? value)
    {
        return value.StrEq(nameof(EYesNo.Yes));
    }

    /// <summary>
    /// Convert string "Y"/"N" value into bool
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YNToBool(this string? value)
    {
        return value.StrEq("Y");
    }

    /// <summary>
    /// Cleans potential parsing issues out of a query parameter
    /// </summary>
    /// <param name="value"></param>
    /// <returns>String equivalent of value passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
    public static string? CleanQueryParam(this string? value)
    {
        return value.MakeNullNull()?.Replace("\n", "").Trim();
    }

    /// <summary>
    /// Cleans potential parsing issues out of a list of query parameters
    /// </summary>
    /// <param name="values"></param>
    /// <returns>List of string equivalents of the values passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
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
    /// <param name="values"></param>
    /// <returns>List of string equivalents of the values passed in replacing standalone text "null" with null value or removing any new line characters and extra spaces</returns>
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
    /// <typeparam name="T"></typeparam>
    /// <param name="parameters">List of a type that can be converted to string</param>
    /// <param name="queryParameterName">The name to be used in front of the equals sign for the query parameter string</param>
    /// <returns>String representation of the list passed in as query parameters with the name passed in as queryParameterName</returns>
    public static string ListToQueryParameters<T>(this IEnumerable<T>? parameters, string? queryParameterName)
    {
        string queryString = string.Empty;
        bool firstItem = true;
        if (parameters?.Any() == true && !string.IsNullOrWhiteSpace(queryParameterName))
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

    /// <summary>
    /// Get file name safe date in the chosen format
    /// </summary>
    /// <param name="dateFormat"></param>
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
            outputName = $"{fileName.Left(fileName.Length - extension.Length)} ({i}).{extension}";
            i++;
        }
        return outputName;
    }

    public static string TimespanToShortForm(this TimeSpan? t)
    {
        string shortForm = "";
        if (t != null)
        {
            shortForm = ((TimeSpan)t).TimespanToShortForm();
        }
        return shortForm;
    }

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
            if (days.Left(1) == "0")
            {
                days = days[1..];
            }
            //if (t.Milliseconds > 0)
            //{
            //    stringForm = stringForm.Replace($".{stringForm.Split(".").Last()}", string.Empty); //Remove milliseconds component
            //}
        }
        else
        {
            if (stringForm.Left(3) == "00:")
            {
                stringForm = stringForm[3..];  //Remove hours if there aren't any
                if (stringForm.Left(1) == "0")
                {
                    stringForm = stringForm[1..]; //Remove leading 0 in minutes
                }
            }
        }

        return string.IsNullOrWhiteSpace(days) ? stringForm : days + ":" + stringForm;
    }

    public enum EHashAlgorithm
    {
        SHA1,
        SHA256,
        SHA384,
        SHA512,
        MD5,
        RSA
    }

    /// <summary>
    /// Takes in a string and returns the hashed value of it using the passed in hashing algorithm
    /// </summary>
    /// <param name="originalString">String to be hashed</param>
    /// <param name="algorithm">Which algorithm to use for the hash operation</param>
    /// <returns>Hash string</returns>
    public static string GetHash(this string originalString, EHashAlgorithm algorithm)
    {
        var bytes = algorithm switch
        {
            EHashAlgorithm.SHA1 => SHA1.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA256 => SHA256.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.SHA384 => SHA384.HashData(Encoding.UTF8.GetBytes(originalString)),
            EHashAlgorithm.MD5 => MD5.HashData(Encoding.UTF8.GetBytes(originalString)),
            //case EHashAlgorithm.SHA512:
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
                case '\u0020':case '\u00A0':case '\u1680':case '\u2000':case '\u2001':case '\u2002':case '\u2003':case '\u2004':case '\u2005':case '\u2006':case '\u2007':case '\u2008':case '\u2009':
                case '\u200A':case '\u202F':case '\u205F':case '\u3000':case '\u2028':case '\u2029':case '\u0009':case '\u000A':case '\u000B':case '\u000C':case '\u000D':case '\u0085':
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
}
