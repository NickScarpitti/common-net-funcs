using System;

namespace CommonNetCoreFuncs.Conversion
{
    public static class UnitConversion
    {
        public const double kgToLbsConst = 2.20462;
        public const double ftToInConst = 12;

        /// <summary>
        /// Convert mass in lbs to kg
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the mass in lbs converted to kg</returns>
        public static double LbsToKg(this double massLbs)
        {
            return massLbs / kgToLbsConst;
        }

        /// <summary>
        /// Convert mass in lbs to kg
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the mass in lbs converted to kg</returns>
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

        /// <summary>
        /// Convert mass in kg to lbs
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the mass in kg converted to lbs</returns>
        public static double KgToLbs(this double massKg)
        {
            return massKg * kgToLbsConst;
        }

        /// <summary>
        /// Convert mass in kg to lbs
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the mass in kg converted to lbs</returns>
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

        /// <summary>
        /// Convert length in inches to feet
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the length in inches converted to feet</returns>
        public static double InsToFt(this double lenIns)
        {
            return lenIns / ftToInConst;
        }

        /// <summary>
        /// Convert length in inches to feet
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the length in inches converted to feet</returns>
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

        /// <summary>
        /// Convert length in feet to inches
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the length in feet converted to inches</returns>
        public static double FtToIns(this double lenIns)
        {
            return lenIns * ftToInConst;
        }

        /// <summary>
        /// Convert length in feet to inches
        /// </summary>
        /// <param name="massLbs"></param>
        /// <returns>Double representation of the length in feet converted to inches</returns>
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

        public static double BitsToMb(this int bits)
        {
            return Math.Round(bits / 1048576.0, 1, MidpointRounding.AwayFromZero);
        }

        public static double BitsToMb(this long bits)
        {
            return Math.Round(bits / 1048576.0, 1, MidpointRounding.AwayFromZero);
        }

        public static double MbToGb(this double mb)
        {
            return Math.Round(mb / 1024.0, 1, MidpointRounding.AwayFromZero);
        }
    }
}