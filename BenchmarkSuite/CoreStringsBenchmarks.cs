using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
#pragma warning disable RCS1102 // Make class static
#pragma warning disable S1118 // Utility classes should not have public constructors
public class CoreStringsBenchmarks
#pragma warning restore S1118 // Utility classes should not have public constructors
#pragma warning restore RCS1102 // Make class static
{
	private const string TestString = "The quick brown fox jumps over the lazy dog";
	private const string LongString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";

	[Benchmark]
	public static string Right_SmallString()
	{
		return TestString.Right(10);
	}

	[Benchmark]
	public static string Right_LongString()
	{
		return LongString.Right(50);
	}

	[Benchmark]
	public static string Left_SmallString()
	{
		return TestString.Left(10);
	}

	[Benchmark]
	public static string Left_LongString()
	{
		return LongString.Left(50);
	}

	[Benchmark]
	public static string ParsePascalCase()
	{
		return "ThisIsAPascalCaseString".ParsePascalCase();
	}

	[Benchmark]
	public static string TrimFull()
	{
		return "  This   has   multiple   spaces  ".TrimFull();
	}

	[Benchmark]
	public static bool ContainsInvariant_Single()
	{
		return TestString.ContainsInvariant("fox");
	}

	[Benchmark]
	public static bool ContainsInvariant_Multiple()
	{
		return TestString.ContainsInvariant(["fox", "dog", "cat"], useOrComparison: true);
	}

	[Benchmark]
	public static string ReplaceInvariant()
	{
		return TestString.ReplaceInvariant("fox", "cat");
	}

	[Benchmark]
	public static string ReplaceInvariant_Multiple()
	{
		return TestString.ReplaceInvariant(["fox", "dog"], "animal");
	}

	[Benchmark]
	public static bool IsNullOrWhiteSpace()
	{
		return TestString.IsNullOrWhiteSpace();
	}

	[Benchmark]
	public static string ExtractBetween()
	{
		return TestString.ExtractBetween("quick", "fox");
	}
}
