﻿using CommonNetFuncs.Core;

namespace Core.Tests;

public enum NullabilityType
{
	NonNullable,
	Nullable
}

public enum IntegerSizeType
{
	Int32,
	Int64
}

public sealed class UnitConversionTests
{
	[Theory]
	[InlineData(NullabilityType.NonNullable, 10.0, 4.53592)]
	[InlineData(NullabilityType.NonNullable, 1.0, 0.453592)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 10.0, 4.53592)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void LbsToKg_ConvertsCorrectly(NullabilityType nullability, double? massLbs, decimal expectedKg)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)massLbs!.Value).LbsToKg()
			: ((decimal?)massLbs).LbsToKg();
		// Assert
		result.ShouldBe(expectedKg, 0.00001m);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, 10.0, 22.0462)]
	[InlineData(NullabilityType.NonNullable, 1.0, 2.20462)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 10.0, 22.0462)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void KgToLbs_ConvertsCorrectly(NullabilityType nullability, double? massKg, decimal expectedLbs)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)massKg!.Value).KgToLbs()
			: ((decimal?)massKg).KgToLbs();
		// Assert
		result.ShouldBe(expectedLbs, 0.000001m);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, 12.0, 1.0)]
	[InlineData(NullabilityType.NonNullable, 6.0, 0.5)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 12.0, 1.0)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void InsToFt_ConvertsCorrectly(NullabilityType nullability, double? inches, decimal expectedFeet)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)inches!.Value).InsToFt()
			: ((decimal?)inches).InsToFt();
		// Assert
		result.ShouldBe(expectedFeet);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, 1.0, 25.4)]
	[InlineData(NullabilityType.NonNullable, 0.5, 12.7)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 1.0, 25.4)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void InsToMm_ConvertsCorrectly(NullabilityType nullability, double? inches, decimal expectedMm)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)inches!.Value).InsToMm()
			: ((decimal?)inches).InsToMm(1);
		// Assert
		result.ShouldBe(expectedMm);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, 25.4, 1.0)]
	[InlineData(NullabilityType.NonNullable, 12.7, 0.5)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 25.4, 1.0)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void MmToIns_ConvertsCorrectly(NullabilityType nullability, double? mm, decimal expectedInches)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)mm!.Value).MmToIns(1)
			: ((decimal?)mm).MmToIns(1);
		// Assert
		result.ShouldBe(expectedInches);
	}

	[Theory]
	[InlineData(IntegerSizeType.Int32, 1024, 1.0)]
	[InlineData(IntegerSizeType.Int32, 2048, 2.0)]
	[InlineData(IntegerSizeType.Int32, 0, 0.0)]
	[InlineData(IntegerSizeType.Int64, 1024, 1.0)]
	[InlineData(IntegerSizeType.Int64, 2048, 2.0)]
	[InlineData(IntegerSizeType.Int64, 0, 0.0)]
	public void BytesToKb_ConvertsCorrectly(IntegerSizeType intSize, long bytes, decimal expectedKb)
	{
		// Act
		decimal result = intSize == IntegerSizeType.Int32
			? ((int)bytes).BytesToKb(1)
			: bytes.BytesToKb(1);
		// Assert
		result.ShouldBe(expectedKb);
	}

	[Theory]
	[InlineData(1.0, 1024L)]
	[InlineData(2.0, 2048L)]
	[InlineData(0.0, 0L)]
	public void KbToBytes_ConvertsCorrectly(decimal kb, long expectedBytes)
	{
		// Act
		long result = kb.KbToBytes();

		// Assert
		result.ShouldBe(expectedBytes);
	}

	[Theory]
	[InlineData(IntegerSizeType.Int32, 1048576, 1.0)]
	[InlineData(IntegerSizeType.Int32, 2097152, 2.0)]
	[InlineData(IntegerSizeType.Int32, 0, 0.0)]
	[InlineData(IntegerSizeType.Int32, -1048576, -1.0)]
	[InlineData(IntegerSizeType.Int64, 1048576, 1.0)]
	[InlineData(IntegerSizeType.Int64, 2097152, 2.0)]
	[InlineData(IntegerSizeType.Int64, 0, 0.0)]
	[InlineData(IntegerSizeType.Int64, -1048576, -1.0)]
	public void BytesToMb_ConvertsCorrectly(IntegerSizeType intSize, long bytes, decimal expectedMb)
	{
		// Act
		decimal result = intSize == IntegerSizeType.Int32
			? ((int)bytes).BytesToMb(1)
			: bytes.BytesToMb(1);
		// Assert
		result.ShouldBe(expectedMb);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, 1.0, 1609.34)]
	[InlineData(NullabilityType.NonNullable, 0.0, 0.0)]
	[InlineData(NullabilityType.Nullable, 1.0, 1609.34)]
	[InlineData(NullabilityType.Nullable, null, 0.0)]
	public void MilesToMeters_ConvertsCorrectly(NullabilityType nullability, double? miles, decimal expectedMeters)
	{
		// Act
		decimal result = nullability == NullabilityType.NonNullable
			? ((decimal)miles!.Value).MilesToMeters()
			: ((decimal?)miles).MilesToMeters();

		// Assert
		result.ShouldBe(expectedMeters, 0.01m);
	}
	[Theory]
	[InlineData(1609.34, 1)]
	[InlineData(0, 0)]
	public void MetersToMiles_NonNullable_ConvertsCorrectly(decimal meters, decimal expectedMiles)
	{
		// Act
		decimal result = meters.MetersToMiles();

		// Assert
		result.ShouldBe(expectedMiles, 0.00001m);
	}

	[Theory]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, 1024L, "1 KB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, 1048576L, "1 MB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, 1073741824L, "1 GB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, 1099511627776L, "1 TB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, -1024L, "-1 KB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int64, 0L, "0 B")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int32, 1024, "1 KB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int32, 1048576, "1 MB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int32, -1024, "-1 KB")]
	[InlineData(NullabilityType.NonNullable, IntegerSizeType.Int32, 0, "0 B")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int64, null, "-0")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int64, 1024L, "1 KB")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int64, 0L, "0 B")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int32, null, "-0")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int32, 1024, "1 KB")]
	[InlineData(NullabilityType.Nullable, IntegerSizeType.Int32, 0, "0 B")]
	public void GetFileSizeFromBytesWithUnits_FormatsCorrectly(NullabilityType nullability, IntegerSizeType intSize, object? bytes, string expected)
	{
		// Act
		string result;
		if (nullability == NullabilityType.NonNullable)
		{
			result = intSize == IntegerSizeType.Int32
				? Convert.ToInt32(bytes).GetFileSizeFromBytesWithUnits()
				: Convert.ToInt64(bytes).GetFileSizeFromBytesWithUnits();
		}
		else
		{
			result = intSize == IntegerSizeType.Int32
				? (bytes == null ? (int?)null : Convert.ToInt32(bytes)).GetFileSizeFromBytesWithUnits()
				: (bytes == null ? (long?)null : Convert.ToInt64(bytes)).GetFileSizeFromBytesWithUnits();
		}

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

	[Theory]
	[InlineData(1.0, 12.0)]
	[InlineData(0.0, 0.0)]
	[InlineData(-2.5, -30.0)]
	public void FtToIns_NonNullable_ConvertsCorrectly(decimal feet, decimal expectedInches)
	{
		decimal result = feet.FtToIns();
		result.ShouldBe(expectedInches);
	}

	[Theory]
	[InlineData(1.0, 12.0)]
	[InlineData(null, 0.0)]
	[InlineData(-2.5, -30.0)]
	public void FtToIns_Nullable_ConvertsCorrectly(double? feet, decimal expectedInches)
	{
		decimal result = ((decimal?)feet).FtToIns();
		result.ShouldBe(expectedInches);
	}

	[Theory]
	[InlineData(1048576L, 1, 1.0)]
	[InlineData(2097152L, 1, 2.0)]
	[InlineData(0L, 1, 0.0)]
	[InlineData(-1048576L, 1, -1.0)]
	public void BytesToMb_Long_ConvertsCorrectly(long bytes, int decimalPlaces, decimal expectedMb)
	{
		decimal result = bytes.BytesToMb(decimalPlaces);
		result.ShouldBe(expectedMb);
	}

	[Theory]
	[InlineData(1.0, 1048576L)]
	[InlineData(2.0, 2097152L)]
	[InlineData(0.0, 0L)]
	[InlineData(-1.0, -1048576L)]
	public void MbToBytes_ConvertsCorrectly(decimal mb, long expectedBytes)
	{
		long result = mb.MbToBytes();
		result.ShouldBe(expectedBytes);
	}

	[Theory]
	[InlineData(1073741824, 1, 1.0)]
	[InlineData(0, 1, 0.0)]
	[InlineData(-1073741824, 1, -1.0)]
	public void BytesToGb_Int_ConvertsCorrectly(int bytes, int decimalPlaces, decimal expectedGb)
	{
		// Act
		decimal result = bytes.BytesToGb(decimalPlaces);

		// Assert
		result.ShouldBe(expectedGb);
	}

	[Theory]
	[InlineData(1073741824L, 1, 1.0)]
	[InlineData(2147483648L, 1, 2.0)]
	[InlineData(0L, 1, 0.0)]
	[InlineData(-1073741824L, 1, -1.0)]
	public void BytesToGb_Long_ConvertsCorrectly(long bytes, int decimalPlaces, decimal expectedGb)
	{
		decimal result = bytes.BytesToGb(decimalPlaces);
		result.ShouldBe(expectedGb);
	}

	[Theory]
	[InlineData(1.0, 1073741824L)]
	[InlineData(2.0, 2147483648L)]
	[InlineData(0.0, 0L)]
	[InlineData(-1.0, -1073741824L)]
	public void GbToBytes_ConvertsCorrectly(decimal gb, long expectedBytes)
	{
		long result = gb.GbToBytes();
		result.ShouldBe(expectedBytes);
	}

	[Theory]
	[InlineData(600000000, 6, 0.000546)]
	[InlineData(666666666, 6, 0.000606)]
	[InlineData(0, 1, 0.0)]
	[InlineData(-666666666, 6, -0.000606)]
	public void BytesToTb_Int_ConvertsCorrectly(int bytes, int decimalPlaces, decimal expectedTb)
	{
		decimal result = bytes.BytesToTb(decimalPlaces);
		result.ShouldBe(expectedTb);
	}

	[Theory]
	[InlineData(1099511627776L, 1, 1.0)]
	[InlineData(2199023255552L, 1, 2.0)]
	[InlineData(0L, 1, 0.0)]
	[InlineData(-1099511627776L, 1, -1.0)]
	public void BytesToTb_Long_ConvertsCorrectly(long bytes, int decimalPlaces, decimal expectedTb)
	{
		decimal result = bytes.BytesToTb(decimalPlaces);

		result.ShouldBe(expectedTb);
	}

	[Theory]
	[InlineData(1.0, 1099511627776L)]
	[InlineData(2.0, 2199023255552L)]
	[InlineData(0.0, 0L)]
	[InlineData(-1.0, -1099511627776L)]
	public void TbToBytes_ConvertsCorrectly(decimal tb, long expectedBytes)
	{
		long result = tb.TbToBytes();
		result.ShouldBe(expectedBytes);
	}

	[Theory]
	[InlineData(1024.0, 1, 1.0)]
	[InlineData(2048.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1024.0, 1, -1.0)]
	public void KbToMb_ConvertsCorrectly(decimal kb, int decimalPlaces, decimal expectedMb)
	{
		decimal result = kb.KbToMb(decimalPlaces);
		result.ShouldBe(expectedMb);
	}

	[Theory]
	[InlineData(1.0, 1, 1024.0)]
	[InlineData(2.0, 1, 2048.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1.0, 1, -1024.0)]
	public void MbToKb_ConvertsCorrectly(decimal mb, int decimalPlaces, decimal expectedKb)
	{
		decimal result = mb.MbToKb(decimalPlaces);
		result.ShouldBe(expectedKb);
	}

	[Theory]
	[InlineData(1048576.0, 1, 1.0)]
	[InlineData(2097152.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1048576.0, 1, -1.0)]
	public void KbToGb_ConvertsCorrectly(decimal kb, int decimalPlaces, decimal expectedGb)
	{
		decimal result = kb.KbToGb(decimalPlaces);
		result.ShouldBe(expectedGb);
	}

	[Theory]
	[InlineData(1.0, 1, 0.0)]
	[InlineData(1048576.0, 1, 1.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1048576.0, 1, -1.0)]
	public void GbToKb_ConvertsCorrectly(decimal gb, int decimalPlaces, decimal expectedKb)
	{
		decimal result = gb.GbToKb(decimalPlaces);
		result.ShouldBe(expectedKb);
	}

	[Theory]
	[InlineData(1073741824.0, 1, 1.0)]
	[InlineData(2147483648.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1073741824.0, 1, -1.0)]
	public void KbToTb_ConvertsCorrectly(decimal kb, int decimalPlaces, decimal expectedTb)
	{
		decimal result = kb.KbToTb(decimalPlaces);
		result.ShouldBe(expectedTb);
	}

	[Theory]
	[InlineData(1.0, 1, 1073741824.0)]
	[InlineData(2.0, 1, 2147483648.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1.0, 1, -1073741824.0)]
	public void TbToKb_ConvertsCorrectly(decimal tb, int decimalPlaces, decimal expectedKb)
	{
		decimal result = tb.TbToKb(decimalPlaces);
		result.ShouldBe(expectedKb);
	}

	[Theory]
	[InlineData(1024.0, 1, 1.0)]
	[InlineData(2048.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1024.0, 1, -1.0)]
	public void MbToGb_ConvertsCorrectly(decimal mb, int decimalPlaces, decimal expectedGb)
	{
		decimal result = mb.MbToGb(decimalPlaces);
		result.ShouldBe(expectedGb);
	}

	[Theory]
	[InlineData(1.0, 1, 1024.0)]
	[InlineData(2.0, 1, 2048.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1.0, 1, -1024.0)]
	public void GbToMb_ConvertsCorrectly(decimal gb, int decimalPlaces, decimal expectedMb)
	{
		decimal result = gb.GbToMb(decimalPlaces);
		result.ShouldBe(expectedMb);
	}

	[Theory]
	[InlineData(1048576.0, 1, 1.0)]
	[InlineData(2097152.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1048576.0, 1, -1.0)]
	public void MbToTb_ConvertsCorrectly(decimal mb, int decimalPlaces, decimal expectedTb)
	{
		decimal result = mb.MbToTb(decimalPlaces);
		result.ShouldBe(expectedTb);
	}

	[Theory]
	[InlineData(1.0, 1, 1048576.0)]
	[InlineData(2.0, 1, 2097152.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1.0, 1, -1048576.0)]
	public void TbToMb_ConvertsCorrectly(decimal tb, int decimalPlaces, decimal expectedMb)
	{
		decimal result = tb.TbToMb(decimalPlaces);
		result.ShouldBe(expectedMb);
	}

	[Theory]
	[InlineData(1024.0, 1, 1.0)]
	[InlineData(2048.0, 1, 2.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1024.0, 1, -1.0)]
	public void GbToTb_ConvertsCorrectly(decimal gb, int decimalPlaces, decimal expectedTb)
	{
		decimal result = gb.GbToTb(decimalPlaces);
		result.ShouldBe(expectedTb);
	}

	[Theory]
	[InlineData(1.0, 1, 1024.0)]
	[InlineData(2.0, 1, 2048.0)]
	[InlineData(0.0, 1, 0.0)]
	[InlineData(-1.0, 1, -1024.0)]
	public void TbToGb_ConvertsCorrectly(decimal tb, int decimalPlaces, decimal expectedGb)
	{
		decimal result = tb.TbToGb(decimalPlaces);
		result.ShouldBe(expectedGb);
	}

	[Theory]
	[InlineData(1609.34, 1.0)]
	[InlineData(0.0, 0.0)]
	[InlineData(-1609.34, -1.0)]
	public void MetersToMiles_Decimal_ConvertsCorrectly(decimal meters, decimal expectedMiles)
	{
		decimal result = meters.MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1609.34, 1.0)]
	[InlineData(null, 0.0)]
	[InlineData(-1609.34, -1.0)]
	public void MetersToMiles_NullableDecimal_ConvertsCorrectly(double? meters, decimal expectedMiles)
	{
		decimal result = ((decimal?)meters).MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1609.34, 1.0)]
	[InlineData(0.0, 0.0)]
	[InlineData(-1609.34, -1.0)]
	public void MetersToMiles_Double_ConvertsCorrectly(double meters, decimal expectedMiles)
	{
		decimal result = meters.MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1609.34, 1.0)]
	[InlineData(null, 0.0)]
	[InlineData(-1609.34, -1.0)]
	public void MetersToMiles_NullableDouble_ConvertsCorrectly(double? meters, decimal expectedMiles)
	{
		decimal result = meters.MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1609, 1.0)]
	[InlineData(0, 0.0)]
	[InlineData(-1609, -1.0)]
	public void MetersToMiles_Int_ConvertsCorrectly(int meters, decimal expectedMiles)
	{
		decimal result = meters.MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1609, 1.0)]
	[InlineData(null, 0.0)]
	[InlineData(-1609, -1.0)]
	public void MetersToMiles_NullableInt_ConvertsCorrectly(int? meters, decimal expectedMiles)
	{
		decimal result = meters.MetersToMiles();
		result.ShouldBe(expectedMiles, 0.01m);
	}

	[Theory]
	[InlineData(1.0, 1609.34)]
	[InlineData(0.0, 0.0)]
	[InlineData(-1.0, -1609.34)]
	public void MilesToMeters_Decimal_ConvertsCorrectly(decimal miles, decimal expectedMeters)
	{
		decimal result = miles.MilesToMeters();
		result.ShouldBe(expectedMeters, 0.01m);
	}

	[Theory]
	[InlineData(1.0, 1609.34)]
	[InlineData(null, 0.0)]
	[InlineData(-1.0, -1609.34)]
	public void MilesToMeters_NullableDecimal_ConvertsCorrectly(double? miles, decimal expectedMeters)
	{
		decimal result = ((decimal?)miles).MilesToMeters();
		result.ShouldBe(expectedMeters, 0.01m);
	}

	[Theory]
	[InlineData(1, 1609.34)]
	[InlineData(0, 0.0)]
	[InlineData(-1, -1609.34)]
	public void MilesToMeters_Int_ConvertsCorrectly(int miles, decimal expectedMeters)
	{
		decimal result = miles.MilesToMeters();
		result.ShouldBe(expectedMeters, 0.01m);
	}

	[Theory]
	[InlineData(1, 1609.34)]
	[InlineData(null, 0.0)]
	[InlineData(-1, -1609.34)]
	public void MilesToMeters_NullableInt_ConvertsCorrectly(int? miles, decimal expectedMeters)
	{
		decimal result = miles.MilesToMeters();
		result.ShouldBe(expectedMeters, 0.01m);
	}
}
