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
    /// Convert bytes to Kb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Kb</returns>
    public static decimal BytesToKb(this int bytes)
    {
        return Math.Round(bytes / 1024m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Kb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Kb</returns>
    public static decimal BytesToKb(this long bytes)
    {
        return Math.Round(bytes / 1024m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Mb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Mb</returns>
    public static decimal BytesToMb(this int bytes)
    {
        return Math.Round(bytes / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Mb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Mb</returns>
    public static decimal BytesToMb(this long bytes)
    {
        return Math.Round(bytes / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Gb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Gb</returns>
    public static decimal BytesToGb(this int bytes)
    {
        return Math.Round(bytes / 1073741824m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Gb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Gb</returns>
    public static decimal BytesToGb(this long bytes)
    {
        return Math.Round(bytes / 1073741824m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Tb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Tb</returns>
    public static decimal BytesToTb(this int bytes)
    {
        return Math.Round(bytes / 1099511627776m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Tb
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>Decimal representation of the number of bytes in Tb</returns>
    public static decimal BytesToTb(this long bytes)
    {
        return Math.Round(bytes / 1099511627776m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to Mb
    /// </summary>
    /// <param name="kb"></param>
    /// <returns>Decimal representation of the number of Kb in Mb</returns>
    public static decimal KbToMb(this decimal kb)
    {
        return Math.Round(kb / 1024m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to Gb
    /// </summary>
    /// <param name="kb"></param>
    /// <returns>Decimal representation of the number of Kb in Gb</returns>
    public static decimal KbToGb(this decimal kb)
    {
        return Math.Round(kb / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to Tb
    /// </summary>
    /// <param name="kb"></param>
    /// <returns>Decimal representation of the number of Kb in Tb</returns>
    public static decimal KbToTb(this decimal kb)
    {
        return Math.Round(kb / 1073741824m, 1, MidpointRounding.AwayFromZero);
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

    /// <summary>
    /// Convert Mb to Tb
    /// </summary>
    /// <param name="mb"></param>
    /// <returns>Decimal representation of the number of Mb in Tb</returns>
    public static decimal MbToTb(this decimal mb)
    {
        return Math.Round(mb / 1048576m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Tb
    /// </summary>
    /// <param name="Gb"></param>
    /// <returns>Decimal representation of the number of Gb in Tb</returns>
    public static decimal GbToTb(this decimal Gb)
    {
        return Math.Round(Gb / 1024m, 1, MidpointRounding.AwayFromZero);
    }

    public static string GetFileSizeFromBytesWithUnits(this long bytes)
    {
        bytes = Math.Abs(bytes);
        return bytes > 1025 ? bytes.BytesToKb() > 1025 ? bytes.BytesToMb() > 1025 ? bytes.BytesToGb() > 1025 ? $"{bytes.BytesToTb()} TB" : $"{bytes.BytesToGb()} GB" : $"{bytes.BytesToMb()} MB" : $"{bytes.BytesToTb()} KB" :  $"{bytes} B";
    }

    public static string GetFileSizeFromBytesWithUnits(this long? nullBytes)
    {
        if (nullBytes == null) { return "-0"; }
        long bytes = Math.Abs((long)nullBytes);
        return bytes > 1025 ? bytes.BytesToKb() > 1025 ? bytes.BytesToMb() > 1025 ? bytes.BytesToGb() > 1025 ? $"{bytes.BytesToTb()} TB" : $"{bytes.BytesToGb()} GB" : $"{bytes.BytesToMb()} MB" : $"{bytes.BytesToTb()} KB" : $"{bytes} B";
    }

    public static string GetFileSizeFromBytesWithUnits(this int bytes)
    {
        bytes = Math.Abs(bytes);
        return bytes > 1025 ? bytes.BytesToKb() > 1025 ? bytes.BytesToMb() > 1025 ? bytes.BytesToGb() > 1025 ? $"{bytes.BytesToTb()} TB" : $"{bytes.BytesToGb()} GB" : $"{bytes.BytesToMb()} MB" : $"{bytes.BytesToTb()} KB" : $"{bytes} B";
    }

    public static string GetFileSizeFromBytesWithUnits(this int? nullBytes)
    {
        if (nullBytes == null) { return "-0"; }
        int bytes = Math.Abs((int)nullBytes);
        return bytes > 1025 ? bytes.BytesToKb() > 1025 ? bytes.BytesToMb() > 1025 ? bytes.BytesToGb() > 1025 ? $"{bytes.BytesToTb()} TB" : $"{bytes.BytesToGb()} GB" : $"{bytes.BytesToMb()} MB" : $"{bytes.BytesToTb()} KB" : $"{bytes} B";
    }
}
