using System;
using System.Collections.Generic;
using System.Text;

namespace CommonNetCoreFuncs.Conversion
{
    public static class StringConversion
    {
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
        public static string ToNString(this int? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToString();
            }
            return output;
        }
        public static string ToNString(this long? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToNString();
            }
            return output;
        }
        public static string ToNString(this double? x)
        {
            string output = null;
            if (x != null)
            {
                output = x.ToNString();
            }
            return output;
        }
    }
}
