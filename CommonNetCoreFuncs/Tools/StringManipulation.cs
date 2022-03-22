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
        /// <param name="s"></param>
        /// <param name="numChars"></param>
        /// <returns>String of the length indicated from the left side of the source string</returns>
        public static string? Left(this string s, int numChars)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return null;
                }

                if (numChars <= s.Length)
                {
                    return s[..numChars];
                }
                else
                {
                    return s;
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
        /// <param name="s"></param>
        /// <param name="numChars"></param>
        /// <returns>String of the length indicated from the right side of the source string</returns>
        public static string? Right(this string? s, int numChars)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return null;
                }

                if (numChars <= s.Length)
                {
                    return s.Substring(s.Length - numChars, numChars);
                }
                else
                {
                    return s;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting right chars");
                return null;
            }
        }

        /// <summary>
        /// Makes a string with of the word "null" into a null value
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Null is the string passed in is null or is the word null with no other text characters other than whitespace</returns>
        public static string? MakeNullNull(this string? s)
        {
            if (s == null || s.StrEq("Null") || s.ToUpperInvariant().Replace("NULL", "") == "" || s.Trim().StrEq("Null"))
            {
                return null;
            }
            return s;
        }
    }
}