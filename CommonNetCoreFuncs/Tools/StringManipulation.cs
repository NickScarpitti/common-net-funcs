using System;
using System.Collections.Generic;
using System.Text;

namespace CommonNetCoreFuncs.Tools
{
    /// <summary>
    /// Methods for complex string manipulation
    /// </summary>
    public static class StringManipulation
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Clone of VBA Left() function 
        /// </summary>
        /// <param name="st"></param>
        /// <param name="numChars"></param>
        /// <returns>Returns a string of the length indicated from the left side of the source string</returns>
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

        /// <summary>
        /// Clone of VBA Right() function
        /// </summary>
        /// <param name="st"></param>
        /// <param name="numChars"></param>
        /// <returns>Returns a string of the length indicated from the right side of the source string</returns>
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

        /// <summary>
        /// Used to reduce boilerplate code for parsing strings into nullable DateTimes
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Nullable DateTime parsed from a string</returns>
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

        /// <summary>
        /// Used to reduce boilerplate code for parsing strings into nullable integers
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Nullable int parsed from a string</returns>
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

        public static string MakeNullNull(this string s)
        {
            if (s == null)
            {
                return null;
            }
            else if (s.ToUpper() == "NULL")
            {
                return null;
            }
            else if (s.ToUpper().Replace("NULL", "") == "")
            {
                return null;
            }
            return s;
        }
    }
}
