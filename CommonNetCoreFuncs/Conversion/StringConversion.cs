using CommonNetCoreFuncs.Tools;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonNetCoreFuncs.Conversion
{
    public enum EYesNo
    {
        Yes,
        No
    }

    /// <summary>
    /// Methods for converting different nullable variable types to string
    /// </summary>
    public static class StringConversion
    {
        /// <summary>
        /// Converts Nullable DateTime to string using the passed in formatting
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns>Returns formatted string representation of the passed in nullable DateTime</returns>
        public static string ToNString(this DateTime? value, string format = null)
        {
            string output = null;
            if (value != null)
            {
                DateTime dtActual = (DateTime)value;
                output = dtActual.ToString(format);
            }
            return output;
        }

        /// <summary>
        /// Converts nullable int to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns string representation of the passed in nullable int</returns>
        public static string ToNString(this int? value)
        {
            string output = null;
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
        /// <returns>Returns string representation of the passed in nullable long</returns>
        public static string ToNString(this long? value)
        {
            string output = null;
            if (value != null)
            {
                output = value.ToNString();
            }
            return output;
        }

        /// <summary>
        /// Converts nullable double to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns string representation of the passed in nullable double</returns>
        public static string ToNString(this double? value)
        {
            string output = null;
            if (value != null)
            {
                output = value.ToNString();
            }
            return output;
        }

        public static SelectListItem ToSelectListItem(this string value)
        {
            if (value != null)
            {
                return new SelectListItem
                {
                    Text = value,
                    Value = value
                };
            }
            else
            {
                return null;
            }
        }

        public static List<int> ToListInt(this List<string> values)
        {
            return values.Select(x => { return int.TryParse(x, out int i) ? i : (int?)null; }).Where(i => i.HasValue).Select(i => i.Value).ToList();
        }

        /// <summary>
        /// Used to reduce boilerplate code for parsing strings into nullable integers
        /// </summary>
        /// <param name="value">String value to be converted to nullable int</param>
        /// <returns>Nullable int parsed from a string</returns>
        public static int? ToNInt(this string value)
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int i))
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
        public static DateTime? ToNDateTime(this string value)
        {
            if (DateTime.TryParse(value, out DateTime dt))
            {
                DateTime? dtn = dt;
                return dtn;
            }
            else
            {
                return null;
            }
        }

        public static bool YesNoToBool(this string value)
        {
            return value.StrEq(EYesNo.Yes.ToString());
        }
    }
}