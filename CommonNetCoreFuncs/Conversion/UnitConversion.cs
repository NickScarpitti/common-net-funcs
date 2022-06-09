namespace CommonNetCoreFuncs.Conversion;

public static class UnitConversion
{
    public const double KgToLbsConst = 2.20462;
    public const double FtToInConst = 12;

    /// <summary>
    /// Convert mass in lbs to kg
    /// </summary>
    /// <param name="massLbs"></param>
    /// <returns>Double representation of the mass in lbs converted to kg</returns>
    public static double LbsToKg(this double massLbs)
    {
        return massLbs / KgToLbsConst;
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
            return Convert.ToDouble(massLbs) / KgToLbsConst;
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
        return massKg * KgToLbsConst;
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
            return Convert.ToDouble(massKg) * KgToLbsConst;
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
        return lenIns / FtToInConst;
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
            return Convert.ToDouble(lenIns) / FtToInConst;
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
        return lenIns * FtToInConst;
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
            return Convert.ToDouble(lenIns) * FtToInConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert bits to Mb
    /// </summary>
    /// <param name="bits"></param>
    /// <returns>Double representation of the number of bits in Mb</returns>
    public static double BitsToMb(this int bits)
    {
        return Math.Round(bits / 1048576.0, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bits to Mb
    /// </summary>
    /// <param name="bits"></param>
    /// <returns>Double representation of the number of bits in Mb</returns>
    public static double BitsToMb(this long bits)
    {
        return Math.Round(bits / 1048576.0, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Gb
    /// </summary>
    /// <param name="mb"></param>
    /// <returns>Double representation of the number of Mb in Gb</returns>
    public static double MbToGb(this double mb)
    {
        return Math.Round(mb / 1024.0, 1, MidpointRounding.AwayFromZero);
    }
}
