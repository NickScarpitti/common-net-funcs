using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Convert;
using static Common_Net_Funcs.Tools.StringHelpers;

namespace Common_Net_Funcs.Conversion;

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
    MD5,
    RSA
}

/// <summary>
/// Methods for converting various variable types to string and vice versa
/// </summary>
public static class StringConversion
{
    public const string TimestampUrlFormat = "yyyyMMddHHmmssFFF";

    /// <summary>
    /// Converts Nullable DateTime to string using the passed in formatting
    /// </summary>
    /// <param name="value"></param>
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
    /// <param name="value">Long to convert to string</param>
    /// <returns>String representation of the passed in nullable long</returns>
    [return: NotNullIfNotNull(nameof(value))]
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
    /// <param name="value">Double to convert to string</param>
    /// <returns>String representation of the passed in nullable double</returns>
    [return: NotNullIfNotNull(nameof(value))]
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
    /// <param name="value">Decimal to convert to string</param>
    /// <returns>String representation of the passed in nullable decimal</returns>
    [return: NotNullIfNotNull(nameof(value))]
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
    /// <param name="value">Boolean to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? ToNString(this bool? value)
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
    /// <param name="value">Object to turn into a string</param>
    /// <returns>String representation of the passed in nullable object</returns>
    [return: NotNullIfNotNull(nameof(value))]
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
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this string? value, bool selected)
    {
        return value != null ? new() { Value= value , Text = value, Selected = selected} : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    [return: NotNullIfNotNull(nameof(value))]
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
    [return: NotNullIfNotNull(nameof(value)),  NotNullIfNotNull(nameof(text))]
    public static SelectListItem? ToSelectListItem(this string? value, string? text, bool selected)
    {
        return value != null ? new() { Value = value, Text = text ?? string.Empty, Selected = selected } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this string? value, string? text)
    {
        return value != null ? new() { Value = value, Text = text ?? string.Empty } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this int? value, bool selected)
    {
        return value != null ? new() { Value = value.ToString(), Text = value.ToString(), Selected = selected } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this int? value)
    {
        return value != null ? new() { Value = value.ToString(), Text = value.ToString() } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this int? value, string? text, bool selected)
    {
        return value != null ? new() { Value = value.ToString(), Text = text ?? string.Empty, Selected = selected } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static SelectListItem? ToSelectListItem(this int? value, string? text)
    {
        return value != null ? new() { Value = value.ToString(), Text = text ?? string.Empty } : null;
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    public static SelectListItem ToSelectListItem(this int value, bool selected)
    {
        return new() { Value = value.ToString(), Text = value.ToString(), Selected = selected };
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for both Value and Text properties</param>
    /// <returns>SelectListItem with text and value properties set to the passed in value</returns>
    public static SelectListItem ToSelectListItem(this int value)
    {
        return new() { Value = value.ToString(), Text = value.ToString() };
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    public static SelectListItem ToSelectListItem(this int value, string? text, bool selected)
    {
        return new() { Value = value.ToString(), Text = text ?? string.Empty, Selected = selected };
    }

    /// <summary>
    /// Converts value to select list item
    /// </summary>
    /// <param name="value">Value to be used for the Value property</param>
    /// <param name="text">Value to be used for the Text property</param>
    /// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
    public static SelectListItem ToSelectListItem(this int value, string? text)
    {
        return new() { Value = value.ToString(), Text = text ?? string.Empty };
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
        if (!value.IsNullOrEmpty() && int.TryParse(value, out int i))
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
        if (!value.IsNullOrEmpty() && double.TryParse(value, out double i))
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
        if (!value.IsNullOrEmpty() && decimal.TryParse(value, out decimal i))
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
    /// Convert string "Yes"/"No" value into bool
    /// </summary>
    /// <param name="value">"Yes"/"No" string to convert into a boolean</param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YesNoToBool(this string? value)
    {
        return value.StrEq(nameof(EYesNo.Yes));
    }

    /// <summary>
    /// Convert string "Y"/"N" value into bool
    /// </summary>
    /// <param name="value">"Y"/"N" string to convert into a boolean</param>
    /// <returns>Bool representation of string value passed in</returns>
    public static bool YNToBool(this string? value)
    {
        return value.StrEq("Y");
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
    /// <param name="value">Integer to conver to "Yes" or "No"</param>
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
            outputName = $"{fileName.Left(fileName.Length - extension.Length)} ({i}).{extension}";
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

        return days.IsNullOrWhiteSpace() ? stringForm : days + ":" + stringForm;
    }

    /// <summary>
    /// Takes in a string and returns the hashed value of it using the passed in hashing algorithm
    /// </summary>
    /// <param name="originalString">String to be hashed</param>
    /// <param name="algorithm">Which algorithm to use for the hash operation</param>
    /// <returns>Hash string</returns>
    public static string GetHash(this string originalString, EHashAlgorithm algorithm)
    {
        byte[] bytes = algorithm switch
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

    /// <summary>
    /// Take any format of a date time string and convert it to a different format
    /// </summary>
    /// <param name="dateString">Input date string to be converted</param>
    /// <param name="sourceFormat">Format of dateString string</param>
    /// <param name="outputFormat">Format to convert to. Defaults to MM/dd/yyyy</param>
    [return: NotNullIfNotNull(nameof(dateString))]
    public static string? FormatDateString(this string? dateString, string sourceFormat, string outputFormat = "MM/dd/yyyy")
    {
        return dateString == null ? null : DateTime.ParseExact(dateString, sourceFormat, CultureInfo.InvariantCulture).ToString(outputFormat.IsNullOrEmpty() ? "MM/dd/yyyy" : outputFormat);
    }
}
