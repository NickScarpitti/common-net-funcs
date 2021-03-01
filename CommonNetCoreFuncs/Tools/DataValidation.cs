using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonNetCoreFuncs.Tools
{
    /// <summary>
    /// Methods for validating data
    /// </summary>
    public static class DataValidation
    {
        /// <summary>
        /// Compares two like objects against each other to check to see if they contain the same values
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns>Returns true if the two objects have the same value for all elements</returns>
        public static bool IsEqual(this object obj1, object obj2)
        {
            // They're both null.
            if (obj1 == null && obj2 == null) return true;
            // One is null, so they can't be the same.
            if (obj1 == null || obj2 == null) return false;
            // How can they be the same if they're different types?
            if (obj1.GetType() != obj1.GetType()) return false;
            var Props = obj1.GetType().GetProperties();
            foreach (var Prop in Props)
            {
                var aPropValue = Prop.GetValue(obj1) ?? string.Empty;
                var bPropValue = Prop.GetValue(obj2) ?? string.Empty;
                if (aPropValue.ToString() != bPropValue.ToString())
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidateFileExtention(this string fileName, string[] validExtensions)
        {
            string extension = Path.GetExtension(fileName);
            if (!validExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static bool StrEq(this string s1, string s2)
        {
            return string.Equals(s1 ?? "", s2 ?? "", StringComparison.InvariantCultureIgnoreCase);
        }

        public static void AddDictionaryItem(this Dictionary<string, string> dict, string key, string value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
        }
    }
}
