using System;

namespace CommonNetCoreFuncs.Tools
{
    /// <summary>
    /// Methods for complex string manipulation
    /// </summary>
    public static class StringManipulation
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
                if (string.IsNullOrEmpty(st))
                {
                    return null;
                }

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
                logger.Error(ex, "Error getting left chars");
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
                if (string.IsNullOrEmpty(st))
                {
                    return null;
                }

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
                logger.Error(ex, "Error getting right chars");
                return null;
            }
        }


        public static string MakeNullNull(this string s)
        {
            if (s == null || s.StrEq("Null") || s.ToUpperInvariant().Replace("NULL", "") == "")
            {
                return null;
            }
            return s;
        }
    }
}