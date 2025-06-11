using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class UnitConversionTests
{
    [Theory]
    [InlineData(10.0, 4.53592)]    // 10 lbs = 4.53592 kg
    [InlineData(1.0, 0.453592)]    // 1 lb = 0.453592 kg
    [InlineData(0.0, 0.0)]         // 0 lbs = 0 kg
    public void LbsToKg_NonNullable_ConvertsCorrectly(double massLbs, decimal expectedKg)
    {
        // Act
        decimal result = ((decimal)massLbs).LbsToKg();

        // Assert
        result.ShouldBe(expectedKg, 0.00001m);
    }

    [Theory]
    [InlineData(10.0, 4.53592)]    // 10 lbs = 4.53592 kg
    [InlineData(null, 0.0)]        // null lbs = 0 kg
    public void LbsToKg_Nullable_ConvertsCorrectly(double? massLbs, decimal expectedKg)
    {
        // Act
        decimal result = ((decimal?)massLbs).LbsToKg();

        // Assert
        result.ShouldBe(expectedKg, 0.00001m);
    }

    [Theory]
    [InlineData(10.0, 22.0462)]    // 10 kg = 22.0462 lbs
    [InlineData(1.0, 2.20462)]     // 1 kg = 2.20462 lbs
    [InlineData(0.0, 0.0)]         // 0 kg = 0 lbs
    public void KgToLbs_NonNullable_ConvertsCorrectly(decimal massKg, decimal expectedLbs)
    {
        // Act
        decimal result = massKg.KgToLbs();

        // Assert
        result.ShouldBe(expectedLbs, 0.000001m);
    }

    [Theory]
    [InlineData(10.0, 22.0462)]    // 10 kg = 22.0462 lbs
    [InlineData(null, 0.0)]        // null kg = 0 lbs
    public void KgToLbs_Nullable_ConvertsCorrectly(double? massKg, decimal expectedLbs)
    {
        // Act
        decimal result = ((decimal?)massKg).KgToLbs();

        // Assert
        result.ShouldBe(expectedLbs, 0.000001m);
    }

    [Theory]
    [InlineData(12.0, 1.0)]        // 12 inches = 1 foot
    [InlineData(6.0, 0.5)]         // 6 inches = 0.5 feet
    [InlineData(0.0, 0.0)]         // 0 inches = 0 feet
    public void InsToFt_NonNullable_ConvertsCorrectly(double inches, decimal expectedFeet)
    {
        // Act
        decimal result = ((decimal)inches).InsToFt();

        // Assert
        result.ShouldBe(expectedFeet);
    }

    [Theory]
    [InlineData(12.0, 1.0)]        // 12 inches = 1 foot
    [InlineData(null, 0.0)]        // null inches = 0 feet
    public void InsToFt_Nullable_ConvertsCorrectly(double? inches, decimal expectedFeet)
    {
        // Act
        decimal result = ((decimal?)inches).InsToFt();

        // Assert
        result.ShouldBe(expectedFeet);
    }

    [Theory]
    [InlineData(1.0, 25.4)]        // 1 inch = 25.4 mm
    [InlineData(0.5, 12.7)]        // 0.5 inches = 12.7 mm
    [InlineData(0.0, 0.0)]         // 0 inches = 0 mm
    public void InsToMm_NonNullable_ConvertsCorrectly(double inches, decimal expectedMm)
    {
        // Act
        decimal result = ((decimal)inches).InsToMm();

        // Assert
        result.ShouldBe(expectedMm);
    }

    [Theory]
    [InlineData(1.0, 1, 25.4)]     // 1 inch = 25.4 mm (1 decimal place)
    [InlineData(null, 1, 0.0)]     // null inches = 0 mm
    public void InsToMm_Nullable_ConvertsCorrectly(double? inches, int decimalPlaces, decimal expectedMm)
    {
        // Act
        decimal result = ((decimal?)inches).InsToMm(decimalPlaces);

        // Assert
        result.ShouldBe(expectedMm);
    }

    [Theory]
    [InlineData(25.4, 1, 1.0)]     // 25.4 mm = 1 inch
    [InlineData(12.7, 1, 0.5)]     // 12.7 mm = 0.5 inches
    [InlineData(0.0, 1, 0.0)]      // 0 mm = 0 inches
    public void MmToIns_NonNullable_ConvertsCorrectly(double mm, int decimalPlaces, decimal expectedInches)
    {
        // Act
        decimal result = ((decimal)mm).MmToIns(decimalPlaces);

        // Assert
        result.ShouldBe(expectedInches);
    }

    [Theory]
    [InlineData(25.4, 1, 1.0)]     // 25.4 mm = 1 inch
    [InlineData(null, 1, 0.0)]     // null mm = 0 inches
    public void MmToIns_Nullable_ConvertsCorrectly(double? mm, int decimalPlaces, decimal expectedInches)
    {
        // Act
        decimal result = ((decimal?)mm).MmToIns(decimalPlaces);

        // Assert
        result.ShouldBe(expectedInches);
    }

    [Theory]
    [InlineData(1024, 1, 1.0)]     // 1024 bytes = 1 KB
    [InlineData(2048, 1, 2.0)]     // 2048 bytes = 2 KB
    [InlineData(0, 1, 0.0)]        // 0 bytes = 0 KB
    public void BytesToKb_Int_ConvertsCorrectly(int bytes, int decimalPlaces, decimal expectedKb)
    {
        // Act
        decimal result = bytes.BytesToKb(decimalPlaces);

        // Assert
        result.ShouldBe(expectedKb);
    }

    [Theory]
    [InlineData(1024L, 1, 1.0)]    // 1024 bytes = 1 KB
    [InlineData(2048L, 1, 2.0)]    // 2048 bytes = 2 KB
    [InlineData(0L, 1, 0.0)]       // 0 bytes = 0 KB
    public void BytesToKb_Long_ConvertsCorrectly(long bytes, int decimalPlaces, decimal expectedKb)
    {
        // Act
        decimal result = bytes.BytesToKb(decimalPlaces);

        // Assert
        result.ShouldBe(expectedKb);
    }

    [Theory]
    [InlineData(1.0, 1024L)]       // 1 KB = 1024 bytes
    [InlineData(2.0, 2048L)]       // 2 KB = 2048 bytes
    [InlineData(0.0, 0L)]          // 0 KB = 0 bytes
    public void KbToBytes_ConvertsCorrectly(decimal kb, long expectedBytes)
    {
        // Act
        long result = kb.KbToBytes();

        // Assert
        result.ShouldBe(expectedBytes);
    }

    [Theory]
    [InlineData(1048576, 1, 1.0)]  // 1MB in bytes = 1.0
    [InlineData(2097152, 1, 2.0)]  // 2MB in bytes = 2.0
    [InlineData(0, 1, 0.0)]        // 0 bytes = 0 MB
    public void BytesToMb_Int_ConvertsCorrectly(int bytes, int decimalPlaces, decimal expectedMb)
    {
        // Act
        decimal result = bytes.BytesToMb(decimalPlaces);

        // Assert
        result.ShouldBe(expectedMb);
    }

    [Theory]
    [InlineData(1, 1609.34)]       // 1 mile = 1609.34 meters
    [InlineData(0, 0)]             // 0 miles = 0 meters
    public void MilesToMeters_NonNullable_ConvertsCorrectly(double miles, decimal expectedMeters)
    {
        // Act
        decimal result = ((decimal)miles).MilesToMeters();

        // Assert
        result.ShouldBe(expectedMeters, 0.01m);
    }

    [Theory]
    [InlineData(1609.34, 1)]       // 1609.34 meters = 1 mile
    [InlineData(0, 0)]             // 0 meters = 0 miles
    public void MetersToMiles_NonNullable_ConvertsCorrectly(decimal meters, decimal expectedMiles)
    {
        // Act
        decimal result = meters.MetersToMiles();

        // Assert
        result.ShouldBe(expectedMiles, 0.00001m);
    }

    [Theory]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    [InlineData(1099511627776L, "1 TB")]
    [InlineData(-1024L, "-1 KB")]
    [InlineData(0L, "0 B")]
    public void GetFileSizeFromBytesWithUnits_Long_FormatsCorrectly(long bytes, string expected)
    {
        // Act
        string result = bytes.GetFileSizeFromBytesWithUnits();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "-0")]
    [InlineData(1024L, "1 KB")]
    [InlineData(0L, "0 B")]
    public void GetFileSizeFromBytesWithUnits_NullableLong_FormatsCorrectly(long? bytes, string expected)
    {
        // Act
        string result = bytes.GetFileSizeFromBytesWithUnits();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(-1024, "-1 KB")]
    [InlineData(0, "0 B")]
    public void GetFileSizeFromBytesWithUnits_Int_FormatsCorrectly(int bytes, string expected)
    {
        // Act
        string result = bytes.GetFileSizeFromBytesWithUnits();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "-0")]
    [InlineData(1024, "1 KB")]
    [InlineData(0, "0 B")]
    public void GetFileSizeFromBytesWithUnits_NullableInt_FormatsCorrectly(int? bytes, string expected)
    {
        // Act
        string result = bytes.GetFileSizeFromBytesWithUnits();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void All_ConversionConstants_HaveCorrectValues()
    {
        // Assert
        UnitConversion.KgToLbsConst.ShouldBe(2.20462m);
        UnitConversion.FtToInConst.ShouldBe(12m);
        UnitConversion.InToMmConst.ShouldBe(25.4m);
        UnitConversion.MetersToMilesConst.ShouldBe(0.000621371m);
    }
}
