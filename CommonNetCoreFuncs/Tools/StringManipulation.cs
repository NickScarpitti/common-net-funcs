using System;
using System.Collections.Generic;
using System.Text;

namespace CommonNetCoreFuncs.Tools
{
    public static class StringManipulation
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string Left(this string st, int numChars)
        {
            try
            {
                if (numChars <= st.Length)
                {
                    return st.Substring(0, numChars);
                }
                else
                {
                    return st;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting left chars");
                return null;
            }
        }
        public static string Right(this string st, int numChars)
        {
            try
            {
                if (numChars <= st.Length)
                {
                    return st.Substring(st.Length - numChars, numChars);
                }
                else
                {
                    return st;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting right chars");
                return null;
            }
        }
        public static DateTime? ParseNullableDateTime(this string s)
        {
            if (DateTime.TryParse(s, out DateTime dt))
            {
                DateTime? dtn = dt;
                return dtn;
            }
            else
            {
                return null;
            }
        }
        public static int? ParseNullableInt(this string s)
        {
            if (int.TryParse(s, out int i))
            {
                int? iNull = i;
                return iNull;
            }
            else
            {
                return null;
            }
        }
    }
}
