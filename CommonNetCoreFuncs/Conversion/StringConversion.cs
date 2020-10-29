using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonNetCoreFuncs.Conversion
{
    /// <summary>
    /// Methods for converting different nullable variable types to string
    /// </summary>
    public static class StringConversion
    {
        /// <summary>
        /// Converts Nullable DateTime to string using the passed in formatting
        /// </summary>
        /// <param name="x"></param>
        /// <param name="formatString"></param>
        /// <returns>Returns formatted string representation of the passed in nullable DateTime</returns>
        public static string ToNString(this DateTime? x, string formatString = "MM/dd/yyyy")
        {
            string output = null;
            if (x != null)
            {
                DateTime dt = DateTime.Parse(x.ToString());
                output = dt.ToString(formatString);
            }
            return output;
        }

        /// <summary>
        /// Converts nullable int to string 
        /// </summary>
        /// <param name="x"></param>
        /// <returns>Returns string representation of the passed in nullable int</returns>
        public static string ToNString(this int? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToString();
            }
            return output;
        }
        /// <summary>
        /// Converts nullable long to string 
        /// </summary>
        /// <param name="x"></param>
        /// <returns>Returns string representation of the passed in nullable long</returns>
        public static string ToNString(this long? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToNString();
            }
            return output;
        }
        /// <summary>
        /// Converts nullable double to string 
        /// </summary>
        /// <param name="x"></param>
        /// <returns>Returns string representation of the passed in nullable double</returns>
        public static string ToNString(this double? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToNString();
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
    }
}
