using System.Reflection;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

#pragma warning disable S3011 // Make sure that this accessibility bypass is safe here
#pragma warning disable S1075 // Refactor your code not to use hardcoded absolute paths or URIs.
[MemoryDiagnoser]
public class CoreFileHelpersBenchmarks
{
	private readonly string _testFileName1 = @"test/file:with<invalid>chars|?.txt";
	private readonly string _testFileName2 = @"C:\Users\test\file\with\path:chars.doc";
	private readonly string _testFileName3 = "NormalFile.txt";
	private readonly string _testFileName4 = @"complex|file<name>with:many/invalid\chars?.pdf";

	private const string CleanFileNameMethodName = nameof(FileHelpers.CleanFileName);

	[Benchmark(Description = "CleanFileName - many invalid chars")]
	public string CleanFileName_ManyInvalid()
	{
		// Access private method via reflection for benchmarking
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { _testFileName1 })!;
	}

	[Benchmark(Description = "CleanFileName - path with invalid chars")]
	public string CleanFileName_PathWithInvalid()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { _testFileName2 })!;
	}

	[Benchmark(Description = "CleanFileName - no invalid chars")]
	public string CleanFileName_NoInvalid()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { _testFileName3 })!;
	}

	[Benchmark(Description = "CleanFileName - complex name")]
	public string CleanFileName_Complex()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { _testFileName4 })!;
	}
}

#pragma warning restore S3011 // Make sure that this accessibility bypass is safe here
#pragma warning restore S1075 // Refactor your code not to use hardcoded absolute paths or URIs.
