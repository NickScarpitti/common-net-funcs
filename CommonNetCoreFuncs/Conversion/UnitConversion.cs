using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Conversion
{
    public static class UnitConversion
    {
        public const double lbsToKgConst = 0.453592;
        public const double ftToInConst = 12;

        public static double LbsToKg(this double massLbs)
        {
            return massLbs * lbsToKgConst;
        }
        public static double LbsToKg(this double? massLbs)
        {
            if (massLbs != null)
            {
                return Convert.ToDouble(massLbs) * lbsToKgConst;
            }
            else
            {
                return 0;
            }
        }
        public static double KgToLbs(this double massLbs)
        {
            return massLbs / lbsToKgConst;
        }
        public static double KgToLbs(this double? massLbs)
        {
            if (massLbs != null)
            {
                return Convert.ToDouble(massLbs) / lbsToKgConst;
            }
            else
            {
                return 0;
            }
        }
        public static double InsToFt(this double lenIns)
        {
            return lenIns / ftToInConst;
        }
        public static double InsToFt(this double? lenIns)
        {
            if (lenIns != null)
            {
                return Convert.ToDouble(lenIns) / ftToInConst;
            }
            else
            {
                return 0;
            }
        }
        public static double FtToIns(this double lenIns)
        {
            return lenIns * ftToInConst;
        }
        public static double FtToIns(this double? lenIns)
        {
            if (lenIns != null)
            {
                return Convert.ToDouble(lenIns) * ftToInConst;
            }
            else
            {
                return 0;
            }
        }
    }
}
