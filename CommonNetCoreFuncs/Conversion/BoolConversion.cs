using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Conversion
{
    public static class BoolConversion
    {
        public static string BoolToYesNo(this bool value)
        {
            if (value)
            {
                return EYesNo.Yes.ToString();
            }
            return EYesNo.No.ToString();
        }
    }
}
