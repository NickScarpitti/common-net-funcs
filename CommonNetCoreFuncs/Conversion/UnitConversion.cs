namespace CommonNetCoreFuncs.Conversion;

/// <summary>
/// Helper methods for doing unit conversions
/// </summary>
public static class UnitConversion
{
    public const decimal KgToLbsConst = 2.20462m;
    public const decimal FtToInConst = 12;

    /// <summary>
    /// Convert mass in lbs to kg
    /// </summary>
    /// <param name="massLbs"></param>
    /// <returns>Decimal representation of the mass in lbs converted to kg</returns>
    public static decimal LbsToKg(this decimal massLbs)
    {
        return massLbs / KgToLbsConst;
    }

    /// <summary>
    /// Convert mass in lbs to kg
    /// </summary>
    /// <param name="massLbs"></param>
    /// <returns>Decimal representation of the mass in lbs converted to kg</returns>
    public static decimal LbsToKg(this decimal? massLbs)
    {
        if (massLbs != null)
        {
            return Convert.ToDecimal(massLbs) / KgToLbsConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert mass in kg to lbs
    /// </summary>
    /// <param name="massKg"></param>
    /// <returns>Decimal representation of the mass in kg converted to lbs</returns>
    public static decimal KgToLbs(this decimal massKg)
    {
        return massKg * KgToLbsConst;
    }

    /// <summary>
    /// Convert mass in kg to lbs
    /// </summary>
    /// <param name="massKg"></param>
    /// <returns>Decimal representation of the mass in kg converted to lbs</returns>
    public static decimal KgToLbs(this decimal? massKg)
    {
        if (massKg != null)
        {
            return Convert.ToDecimal(massKg) * KgToLbsConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert length in inches to feet
    /// </summary>
    /// <param name="lenIns"></param>
    /// <returns>Decimal representation of the length in inches converted to feet</returns>
    public static decimal InsToFt(this decimal lenIns)
    {
        return lenIns / FtToInConst;
    }

    /// <summary>
    /// Convert length in inches to feet
    /// </summary>
    /// <param name="lenIns"></param>
    /// <returns>Decimal representation of the length in inches converted to feet</returns>
    public static decimal InsToFt(this decimal? lenIns)
    {
        if (lenIns != null)
        {
            return Convert.ToDecimal(lenIns) / FtToInConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert length in feet to inches
    /// </summary>
    /// <param name="lenIns"></param>
    /// <returns>Decimal representation of the length in feet converted to inches</returns>
    public static decimal FtToIns(this decimal lenIns)
    {
        return lenIns * FtToInConst;
    }

    /// <summary>
    /// Convert length in feet to inches
    /// </summary>
    /// <param name="lenIns"></param>
    /// <returns>Decimal representation of the length in feet converted to inches</returns>
    public static decimal FtToIns(this decimal? lenIns)
    {
        if (lenIns != null)
        {
            return Convert.ToDecimal(lenIns) * FtToInConst;
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
    /// <returns>Decimal representation of the number of bits in Mb</returns>
    public static decimal BitsToMb(this int bits)
    {
        return Math.Round(bits / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bits to Mb
    /// </summary>
    /// <param name="bits"></param>
    /// <returns>Decimal representation of the number of bits in Mb</returns>
    public static decimal BitsToMb(this long bits)
    {
        return Math.Round(bits / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Gb
    /// </summary>
    /// <param name="mb"></param>
    /// <returns>Decimal representation of the number of Mb in Gb</returns>
    public static decimal MbToGb(this decimal mb)
    {
        return Math.Round(mb / 1024m, 1, MidpointRounding.AwayFromZero);
    }
}
