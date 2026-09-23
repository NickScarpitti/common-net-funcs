using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;
using static System.Convert;
using static System.Math;

namespace BenchmarkSuite;

/// <summary>
/// Benchmarks for CoreUnitConversion methods, specifically comparing the new and old
/// implementations of GetFileSizeFromBytesWithUnits
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class CoreUnitConversionBenchmarks
{
	private static readonly long[] TestValues = new long[]
	{
		0,
		1024,                    // 1 KiB
		1048576,                 // 1 MiB
		1073741824,              // 1 GiB
		1099511627776,           // 1 TiB
		1125899906842624,        // 1 PiB
		-1024,                   // Negative test
		-1073741824,             // Negative test
		512,
		2097152,
		10737418240,
	};

	/// <summary>
	/// Benchmarks the new implementation (uses logarithmic approach)
	/// </summary>
	[Benchmark(Description = "New Implementation (Logarithmic)")]
	public string[] BenchmarkNewImplementation()
	{
		var results = new string[TestValues.Length];
		for (int i = 0; i < TestValues.Length; i++)
		{
			results[i] = TestValues[i].GetFileSizeFromBytesWithUnits();
		}
		return results;
	}

	/// <summary>
	/// Benchmarks the old implementation (nested ternary operators)
	/// </summary>
	[Benchmark(Description = "Old Implementation (Nested Ternary)")]
	public string[] BenchmarkOldImplementation()
	{
		var results = new string[TestValues.Length];
		for (int i = 0; i < TestValues.Length; i++)
		{
			results[i] = GetFileSizeFromBytesWithUnitsOldWay(TestValues[i]);
		}
		return results;
	}

	/// <summary>
	/// Old implementation of GetFileSizeFromBytesWithUnits using nested ternary operators
	/// </summary>
	private static string GetFileSizeFromBytesWithUnitsOldWay(long inputBytes, int decimalPlaces = 1)
	{
		long bytes = Abs(inputBytes);
		long longInput = inputBytes;

		if (bytes == 0)
		{
			return "0 B";
		}

		int dm = decimalPlaces < 0 ? 0 : decimalPlaces;

		long multiplier = bytes > longInput ? -1L : 1L;
		return bytes >= 1024 ?
			bytes.BytesToKb(dm) >= 1024 ? bytes.BytesToMb(dm) >= 1024 ?
				bytes.BytesToGb(dm) >= 1024 ?
					$"{bytes.BytesToTb(dm) * multiplier} TB" :
				$"{bytes.BytesToGb(dm) * multiplier} GB" :
			$"{bytes.BytesToMb(dm) * multiplier} MB" :
		$"{bytes.BytesToKb(dm) * multiplier} KB" :
		$"{bytes * multiplier} B";
	}
}
