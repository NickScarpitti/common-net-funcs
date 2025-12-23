using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

using static CommonNetFuncs.Core.MathHelpers;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreMathHelpersBenchmarks
{
	private const double DoubleValue = 123.456;
	private const decimal DecimalValue = 123.456m;

	[Benchmark]
	public double Ceiling_Double()
	{
		return MathHelpers.Ceiling(DoubleValue, 0.5);
	}

	[Benchmark]
	public decimal Ceiling_Decimal()
	{
		return MathHelpers.Ceiling(DecimalValue, 0.5m);
	}

	[Benchmark]
	public double Floor_Double()
	{
		return MathHelpers.Floor(DoubleValue, 0.5);
	}

	[Benchmark]
	public decimal Floor_Decimal()
	{
		return MathHelpers.Floor(DecimalValue, 0.5m);
	}

	[Benchmark]
	public int GetPrecision_Decimal()
	{
		return DecimalValue.GetPrecision();
	}

	[Benchmark]
	public int GetPrecision_Double()
	{
		return DoubleValue.GetPrecision();
	}

	[Benchmark]
	public bool DoubleEquals()
	{
		return DoubleValue.Equals(123.457);
	}

	[Benchmark]
	public bool DoubleNotEquals()
	{
		return DoubleValue.NotEquals(123.457);
	}

	[Benchmark]
	public void GreatestCommonDenominator_Long()
	{
		long num = 48;
		long den = 18;
		GreatestCommonDenominator(ref num, ref den, out _);
	}

	[Benchmark]
	public void GreatestCommonDenominator_Int()
	{
		int num = 48;
		int den = 18;
		GreatestCommonDenominator(ref num, ref den, out _);
	}
}
