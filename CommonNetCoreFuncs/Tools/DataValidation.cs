using System;
using System.Collections.Generic;
using System.Text;

namespace CommonNetCoreFuncs.Tools
{
    public static class DataValidation
    {
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
    }
}
