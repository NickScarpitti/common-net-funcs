using System;

namespace CommonNetCoreFuncs.Conversion
{
    public static class UnitConversion
    {
        public const double kgToLbsConst = 2.20462;
        public const double ftToInConst = 12;

        public static double LbsToKg(this double massLbs)
        {
            return massLbs / kgToLbsConst;
        }

        public static double LbsToKg(this double? massLbs)
        {
            if (massLbs != null)
            {
                return Convert.ToDouble(massLbs) / kgToLbsConst;
            }
            else
            {
                return 0;
            }
        }

        public static double KgToLbs(this double massKg)
        {
            return massKg * kgToLbsConst;
        }

        public static double KgToLbs(this double? massKg)
        {
            if (massKg != null)
            {
                return Convert.ToDouble(massKg) * kgToLbsConst;
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