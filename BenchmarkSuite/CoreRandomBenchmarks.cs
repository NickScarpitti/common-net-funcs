using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
public class CoreRandomBenchmarks
{
	[Benchmark(Description = "GetRandomInt with min/max")]
	public int GetRandomInt_WithRange()
	{
		return Random.GetRandomInt(1, 100);
	}

	[Benchmark(Description = "GetRandomInt")]
	public int GetRandomInt()
	{
		return Random.GetRandomInt();
	}

	[Benchmark(Description = "GetRandomDouble")]
	public double GetRandomDouble()
	{
		return Random.GetRandomDouble();
	}

	[Benchmark(Description = "GetRandomDouble with decimal places")]
	public double GetRandomDouble_WithDecimalPlaces()
	{
		return Random.GetRandomDouble(5);
	}

	[Benchmark(Description = "GetRandomDecimal")]
	public decimal GetRandomDecimal()
	{
		return Random.GetRandomDecimal();
	}

	[Benchmark(Description = "GetRandomDecimal with decimal places")]
	public decimal GetRandomDecimal_WithDecimalPlaces()
	{
		return Random.GetRandomDecimal(10);
	}

	[Benchmark(Description = "GetRandomInts - 100 items")]
	public List<int> GetRandomInts()
	{
		return Random.GetRandomInts(100, 1, 1000).ToList();
	}
}
