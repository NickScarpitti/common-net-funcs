using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
#pragma warning disable RCS1102 // Make class static
#pragma warning disable S1118 // Utility classes should not have public constructors
public class CoreRandomBenchmarks
#pragma warning restore S1118 // Utility classes should not have public constructors
#pragma warning restore RCS1102 // Make class static
{
	[Benchmark(Description = "GetRandomInt with min/max")]
	public static int GetRandomInt_WithRange()
	{
		return Random.GetRandomInt(1, 100);
	}

	[Benchmark(Description = "GetRandomInt")]
	public static int GetRandomInt()
	{
		return Random.GetRandomInt();
	}

	[Benchmark(Description = "GetRandomDouble")]
	public static double GetRandomDouble()
	{
		return Random.GetRandomDouble();
	}

	[Benchmark(Description = "GetRandomDouble with decimal places")]
	public static double GetRandomDouble_WithDecimalPlaces()
	{
		return Random.GetRandomDouble(5);
	}

	[Benchmark(Description = "GetRandomDecimal")]
	public static decimal GetRandomDecimal()
	{
		return Random.GetRandomDecimal();
	}

	[Benchmark(Description = "GetRandomDecimal with decimal places")]
	public static decimal GetRandomDecimal_WithDecimalPlaces()
	{
		return Random.GetRandomDecimal(10);
	}

	[Benchmark(Description = "GetRandomInts - 100 items")]
	public static List<int> GetRandomInts()
	{
		return Random.GetRandomInts(100, 1, 1000).ToList();
	}
}
