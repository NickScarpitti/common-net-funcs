using System.Numerics;
using static System.Convert;
using static System.Math;

namespace CommonNetFuncs.Core;

/// <summary>
/// Helper methods for doing unit conversions
/// </summary>
public static class UnitConversion
{
    public const decimal KgToLbsConst = 2.20462m;
    public const decimal FtToInConst = 12;
    public const decimal MetersToMilesConst = 0.000621371m;

    /// <summary>
    /// Convert mass in lbs to kg
    /// </summary>
    /// <param name="massLbs">Mass in lbs to convert to kg</param>
    /// <returns>Decimal representation of the mass in lbs converted to kg</returns>
    public static decimal LbsToKg(this decimal massLbs)
    {
        return massLbs / KgToLbsConst;
    }

    /// <summary>
    /// Convert mass in lbs to kg
    /// </summary>
    /// <param name="massLbs">Mass in lbs to convert to kg</param>
    /// <returns>Decimal representation of the mass in lbs converted to kg</returns>
    public static decimal LbsToKg(this decimal? massLbs)
    {
        if (massLbs != null)
        {
            return ToDecimal(massLbs) / KgToLbsConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert mass in kg to lbs
    /// </summary>
    /// <param name="massKg">Mass in kg to convert to lbs</param>
    /// <returns>Decimal representation of the mass in kg converted to lbs</returns>
    public static decimal KgToLbs(this decimal massKg)
    {
        return massKg * KgToLbsConst;
    }

    /// <summary>
    /// Convert mass in kg to lbs
    /// </summary>
    /// <param name="massKg">Mass in kg to convert to lbs</param>
    /// <returns>Decimal representation of the mass in kg converted to lbs</returns>
    public static decimal KgToLbs(this decimal? massKg)
    {
        if (massKg != null)
        {
            return ToDecimal(massKg) * KgToLbsConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert length in inches to feet
    /// </summary>
    /// <param name="lenIns">Length in inches to convert to feet</param>
    /// <returns>Decimal representation of the length in inches converted to feet</returns>
    public static decimal InsToFt(this decimal lenIns)
    {
        return lenIns / FtToInConst;
    }

    /// <summary>
    /// Convert length in inches to feet
    /// </summary>
    /// <param name="lenIns">Length in inches to convert to feet</param>
    /// <returns>Decimal representation of the length in inches converted to feet</returns>
    public static decimal InsToFt(this decimal? lenIns)
    {
        if (lenIns != null)
        {
            return ToDecimal(lenIns) / FtToInConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert length in feet to inches
    /// </summary>
    /// <param name="lenFt">Length in feet to convert to inches</param>
    /// <returns>Decimal representation of the length in feet converted to inches</returns>
    public static decimal FtToIns(this decimal lenFt)
    {
        return lenFt * FtToInConst;
    }

    /// <summary>
    /// Convert length in feet to inches
    /// </summary>
    /// <param name="lenFt">Length in feet to convert to inches</param>
    /// <returns>Decimal representation of the length in feet converted to inches</returns>
    public static decimal FtToIns(this decimal? lenFt)
    {
        if (lenFt != null)
        {
            return ToDecimal(lenFt) * FtToInConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert bytes to Kb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Kb</param>
    /// <returns>Decimal representation of the number of bytes in Kb</returns>
    public static decimal BytesToKb(this int bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Kb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Kb</param>
    /// <returns>Decimal representation of the number of bytes in Kb</returns>
    public static decimal BytesToKb(this long bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to bytes
    /// </summary>
    /// <param name="kb">Number of Kb to convert to bytes</param>
    /// <returns>Decimal representation of the number of Kb in bytes</returns>
    public static long KbToBytes(this decimal kb)
    {
        return (long)(kb * 1024L);
    }

    /// <summary>
    /// Convert bytes to Mb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Mb</param>
    /// <returns>Decimal representation of the number of bytes in Mb</returns>
    public static decimal BytesToMb(this int bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Mb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Mb</param>
    /// <returns>Decimal representation of the number of bytes in Mb</returns>
    public static decimal BytesToMb(this long bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to bytes
    /// </summary>
    /// <param name="mb">Number of Mb to convert to bytes</param>
    /// <returns>Decimal representation of the number of Mb in bytes</returns>
    public static long MbToBytes(this decimal mb)
    {
        return (long)(mb * 1048576L);
    }

    /// <summary>
    /// Convert bytes to Gb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Gb</param>
    /// <returns>Decimal representation of the number of bytes in Gb</returns>
    public static decimal BytesToGb(this int bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1073741824m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Gb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Gb</param>
    /// <returns>Decimal representation of the number of bytes in Gb</returns>
    public static decimal BytesToGb(this long bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1073741824m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Gb to bytes
    /// </summary>
    /// <param name="gb">Number of Gb to convert to bytes</param>
    /// <returns>Decimal representation of the number of Gb in bytes</returns>
    public static long GbToBytes(this decimal gb)
    {
        return (long)(gb * 1073741824L);
    }

    /// <summary>
    /// Convert bytes to Tb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Tb</param>
    /// <returns>Decimal representation of the number of bytes in Tb</returns>
    public static decimal BytesToTb(this int bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1099511627776m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Tb
    /// </summary>
    /// <param name="bytes">Number of bytes to convert to Tb</param>
    /// <returns>Decimal representation of the number of bytes in Tb</returns>
    public static decimal BytesToTb(this long bytes, int decimalPlaces = 1)
    {
        return Round(bytes / 1099511627776m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert bytes to Tb
    /// </summary>
    /// <param name="tb">Number of Tb to convert to bytes</param>
    /// <returns>Decimal representation of the number of Tb in bytes</returns>
    public static long TbToBytes(this decimal tb)
    {
        return (long)(tb * 1099511627776L);
    }

    /// <summary>
    /// Convert Kb to Mb
    /// </summary>
    /// <param name="kb">Number of Kb to convert to Mb</param>
    /// <returns>Decimal representation of the number of Kb in Mb</returns>
    public static decimal KbToMb(this decimal kb, int decimalPlaces = 1)
    {
        return Round(kb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Kb
    /// </summary>
    /// <param name="mb">Number of Mb to convert to Kb</param>
    /// <returns>Decimal representation of the number of Mb in Kb</returns>
    public static decimal MbToKb(this decimal mb, int decimalPlaces = 1)
    {
        return Round(mb * 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to Gb
    /// </summary>
    /// <param name="kb">Number of Kb to convert to Gb</param>
    /// <returns>Decimal representation of the number of Kb in Gb</returns>
    public static decimal KbToGb(this decimal kb, int decimalPlaces = 1)
    {
        return Round(kb / 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Gb to Kb
    /// </summary>
    /// <param name="gb">Number of Gb to convert to Kb</param>
    /// <returns>Decimal representation of the number of Gb in Kb</returns>
    public static decimal GbToKb(this decimal gb, int decimalPlaces = 1)
    {
        return Round(gb / 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Kb to Tb
    /// </summary>
    /// <param name="kb">Number of Kb to convert to Tb</param>
    /// <returns>Decimal representation of the number of Kb in Tb</returns>
    public static decimal KbToTb(this decimal kb, int decimalPlaces = 1)
    {
        return Round(kb / 1073741824m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Tb to Kb
    /// </summary>
    /// <param name="tb">Number of Tb to convert to Kb</param>
    /// <returns>Decimal representation of the number of Tb in Kb</returns>
    public static decimal TbToKb(this decimal tb, int decimalPlaces = 1)
    {
        return Round(tb * 1073741824m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Gb
    /// </summary>
    /// <param name="gb">Number of Mb to convert to Gb</param>
    /// <returns>Decimal representation of the number of Mb in Gb</returns>
    public static decimal MbToGb(this decimal gb, int decimalPlaces = 1)
    {
        return Round(gb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Gb to Mb
    /// </summary>
    /// <param name="gb">Number of Gb to convert to Mb</param>
    /// <returns>Decimal representation of the number of Gb in Mb</returns>
    public static decimal GbToMb(this decimal gb, int decimalPlaces = 1)
    {
        return Round(gb * 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Mb to Tb
    /// </summary>
    /// <param name="mb">Number of Mb to convert to Tb</param>
    /// <returns>Decimal representation of the number of Mb in Tb</returns>
    public static decimal MbToTb(this decimal mb, int decimalPlaces = 1)
    {
        return Round(mb / 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Tb to Mb
    /// </summary>
    /// <param name="tb">Number of Tb to convert to Mb</param>
    /// <returns>Decimal representation of the number of Tb in Mb</returns>
    public static decimal TbToMb(this decimal tb, int decimalPlaces = 1)
    {
        return Round(tb * 1048576m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Gb to Tb
    /// </summary>
    /// <param name="gb">Number of Gb to convert to Tb</param>
    /// <returns>Decimal representation of the number of Gb in Tb</returns>
    public static decimal GbToTb(this decimal gb, int decimalPlaces = 1)
    {
        return Round(gb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convert Tb to Gb
    /// </summary>
    /// <param name="tb">Number of Tb to convert to Gb</param>
    /// <returns>Decimal representation of the number of Tb in Gb</returns>
    public static decimal TbToGb(this decimal tb, int decimalPlaces = 1)
    {
        return Round(tb * 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Returns a human readable string representation of the number of bytes
    /// </summary>
    /// <param name="inputBytes">Number of bytes to be converted</param>
    /// <returns>Human readable string representation of the number of bytes</returns>
    public static string GetFileSizeFromBytesWithUnits(this long inputBytes, int decimalPlaces = 1)
    {
        long bytes = Abs(inputBytes);
        long multiplier = 1;
        if (bytes > inputBytes)
        {
            multiplier = -1;
        }
        return bytes > 1025 ?
            bytes.BytesToKb(decimalPlaces) > 1025 ? +
                bytes.BytesToMb(decimalPlaces) > 1025 ?
                    bytes.BytesToGb(decimalPlaces) > 1025 ?
                        $"{bytes.BytesToTb(decimalPlaces) * multiplier} TB" :
                    $"{bytes.BytesToGb(decimalPlaces) * multiplier} GB" :
                $"{bytes.BytesToMb(decimalPlaces) * multiplier} MB" :
            $"{bytes.BytesToKb(decimalPlaces) * multiplier} KB" :
        $"{bytes * multiplier} B";
    }

    /// <summary>
    /// Returns a human readable string representation of the number of bytes
    /// </summary>
    /// <param name="nullBytes">Number of bytes to be converted</param>
    /// <returns>Human readable string representation of the number of bytes</returns>
    public static string GetFileSizeFromBytesWithUnits(this long? nullBytes, int decimalPlaces = 1)
    {
        if (nullBytes == null) { return "-0"; }
        long bytes = Abs((long)nullBytes);

        long multiplier = 1;
        if (bytes > nullBytes)
        {
            multiplier = -1;
        }
        return bytes > 1025 ?
            bytes.BytesToKb(decimalPlaces) > 1025 ?
                bytes.BytesToMb(decimalPlaces) > 1025 ?
                    bytes.BytesToGb(decimalPlaces) > 1025 ?
                        $"{bytes.BytesToTb(decimalPlaces) * multiplier} TB" :
                    $"{bytes.BytesToGb(decimalPlaces) * multiplier} GB" :
                $"{bytes.BytesToMb(decimalPlaces) * multiplier} MB" :
            $"{bytes.BytesToKb(decimalPlaces) * multiplier} KB" :
        $"{bytes * multiplier} B";
    }

    /// <summary>
    /// Returns a human readable string representation of the number of bytes
    /// </summary>
    /// <param name="inputBytes">Number of bytes to be converted</param>
    /// <returns>Human readable string representation of the number of bytes</returns>
    public static string GetFileSizeFromBytesWithUnits(this int inputBytes)
    {
        return ((long)inputBytes).GetFileSizeFromBytesWithUnits();
    }

    /// <summary>
    /// Returns a human readable string representation of the number of bytes
    /// </summary>
    /// <param name="nullBytes">Number of bytes to be converted</param>
    /// <returns>Human readable string representation of the number of bytes</returns>
    public static string GetFileSizeFromBytesWithUnits(this int? nullBytes)
    {
        return ((long?)nullBytes).GetFileSizeFromBytesWithUnits();
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this decimal meters)
    {
        return meters * MetersToMilesConst;
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this decimal? meters)
    {
        if (meters != null)
        {
            return ToDecimal(meters) * MetersToMilesConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this double meters)
    {
        return ToDecimal(meters) * MetersToMilesConst;
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this double? meters)
    {
        if (meters != null)
        {
            return ToDecimal(meters) * MetersToMilesConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this int meters)
    {
        return meters * MetersToMilesConst;
    }

    /// <summary>
    /// Convert meters to miles
    /// </summary>
    /// <param name="meters">Number of meters to be converted into miles</param>
    /// <returns>Decimal representation of number of miles that corresponds to the input meters</returns>
    public static decimal MetersToMiles(this int? meters)
    {
        if (meters != null)
        {
            return ToDecimal(meters) * MetersToMilesConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert miles to meters
    /// </summary>
    /// <param name="miles">Number of miles to be converted into meters</param>
    /// <returns>Decimal representation of number of meters that corresponds to the input miles</returns>
    public static decimal MilesToMeters(this decimal miles)
    {
        return miles / MetersToMilesConst;
    }

    /// <summary>
    /// Convert miles to meters
    /// </summary>
    /// <param name="miles">Number of miles to be converted into meters</param>
    /// <returns>Decimal representation of number of meters that corresponds to the input miles</returns>
    public static decimal MilesToMeters(this decimal? miles)
    {
        if (miles != null)
        {
            return ToDecimal(miles) / MetersToMilesConst;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Convert miles to meters
    /// </summary>
    /// <param name="miles">Number of miles to be converted into meters</param>
    /// <returns>Decimal representation of number of meters that corresponds to the input miles</returns>
    public static decimal MilesToMeters(this int miles)
    {
        return miles / MetersToMilesConst;
    }

    /// <summary>
    /// Convert miles to meters
    /// </summary>
    /// <param name="miles">Number of miles to be converted into meters</param>
    /// <returns>Decimal representation of number of meters that corresponds to the input miles</returns>
    public static decimal MilesToMeters(this int? miles)
    {
        if (miles != null)
        {
            return ToDecimal(miles) / MetersToMilesConst;
        }
        else
        {
            return 0;
        }
    }
}
