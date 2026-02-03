using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

using static CommonNetFuncs.Core.MathHelpers;

namespace BenchmarkSuite;

[RankColumn]
[MemoryDiagnoser]
#pragma warning disable RCS1102 // Make class static
#pragma warning disable S1118 // Utility classes should not have public constructors
public class CoreMathHelpersBenchmarks
#pragma warning restore S1118 // Utility classes should not have public constructors
#pragma warning restore RCS1102 // Make class static
{
	private const double DoubleValue = 123.456;
	private const decimal DecimalValue = 123.456m;

	[Benchmark]
	public static double Ceiling_Double()
	{
		return MathHelpers.Ceiling(DoubleValue, 0.5);
	}

	[Benchmark]
	public static decimal Ceiling_Decimal()
	{
		return MathHelpers.Ceiling(DecimalValue, 0.5m);
	}

	[Benchmark]
	public static double Floor_Double()
	{
		return MathHelpers.Floor(DoubleValue, 0.5);
	}

	[Benchmark]
	public static decimal Floor_Decimal()
	{
		return MathHelpers.Floor(DecimalValue, 0.5m);
	}

	[Benchmark]
	public static int GetPrecision_Decimal()
	{
		return DecimalValue.GetPrecision();
	}

	[Benchmark]
	public static int GetPrecision_Double()
	{
		return DoubleValue.GetPrecision();
	}

	[Benchmark]
	public static bool DoubleEquals()
	{
		return DoubleValue.Equals(123.457);
	}

	[Benchmark]
	public static bool DoubleNotEquals()
	{
		return DoubleValue.NotEquals(123.457);
	}

	[Benchmark]
	public static void GreatestCommonDenominator_Long()
	{
		long num = 48;
		long den = 18;
		GreatestCommonDenominator(ref num, ref den, out _);
	}

	[Benchmark]
	public static void GreatestCommonDenominator_Int()
	{
		int num = 48;
		int den = 18;
		GreatestCommonDenominator(ref num, ref den, out _);
	}
}
