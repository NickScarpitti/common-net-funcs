using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreStringsBenchmarks
{
	private const string TestString = "The quick brown fox jumps over the lazy dog";
	private const string LongString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

	[Benchmark]
	public string Right_SmallString()
	{
		return TestString.Right(10);
	}

	[Benchmark]
	public string Right_LongString()
	{
		return LongString.Right(50);
	}

	[Benchmark]
	public string Left_SmallString()
	{
		return TestString.Left(10);
	}

	[Benchmark]
	public string Left_LongString()
	{
		return LongString.Left(50);
	}

	[Benchmark]
	public string ParsePascalCase()
	{
		return "ThisIsAPascalCaseString".ParsePascalCase();
	}

	[Benchmark]
	public string TrimFull()
	{
		return "  This   has   multiple   spaces  ".TrimFull();
	}

	[Benchmark]
	public bool ContainsInvariant_Single()
	{
		return TestString.ContainsInvariant("fox");
	}

	[Benchmark]
	public bool ContainsInvariant_Multiple()
	{
		return TestString.ContainsInvariant(["fox", "dog", "cat"], useOrComparison: true);
	}

	[Benchmark]
	public string ReplaceInvariant()
	{
		return TestString.ReplaceInvariant("fox", "cat");
	}

	[Benchmark]
	public string ReplaceInvariant_Multiple()
	{
		return TestString.ReplaceInvariant(["fox", "dog"], "animal");
	}

	[Benchmark]
	public bool IsNullOrWhiteSpace()
	{
		return TestString.IsNullOrWhiteSpace();
	}

	[Benchmark]
	public string ExtractBetween()
	{
		return TestString.ExtractBetween("quick", "fox");
	}
}
