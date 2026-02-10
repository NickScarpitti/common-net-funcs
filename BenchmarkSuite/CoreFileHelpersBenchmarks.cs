using System.Reflection;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
public class CoreFileHelpersBenchmarks
{
#pragma warning disable S1075 // URIs should not be hardcoded
	private readonly string testFileName2 = @"C:\Users\test\file\with\path:chars.doc";
#pragma warning restore S1075 // URIs should not be hardcoded

	private readonly string testFileName1 = "test/file:with<invalid>chars|?.txt";
	private readonly string testFileName3 = "NormalFile.txt";
	private readonly string testFileName4 = @"complex|file<name>with:many/invalid\chars?.pdf";

	private const string CleanFileNameMethodName = nameof(FileHelpers.CleanFileName);

	[Benchmark(Description = "CleanFileName - many invalid chars")]
	public string CleanFileName_ManyInvalid()
	{
		// Access private method via reflection for benchmarking
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { testFileName1 })!;
	}

	[Benchmark(Description = "CleanFileName - path with invalid chars")]
	public string CleanFileName_PathWithInvalid()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { testFileName2 })!;
	}

	[Benchmark(Description = "CleanFileName - no invalid chars")]
	public string CleanFileName_NoInvalid()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { testFileName3 })!;
	}

	[Benchmark(Description = "CleanFileName - complex name")]
	public string CleanFileName_Complex()
	{
		MethodInfo? method = typeof(FileHelpers).GetMethod(CleanFileNameMethodName,
			BindingFlags.NonPublic | BindingFlags.Static);
		return (string)method!.Invoke(null, new object[] { testFileName4 })!;
	}
}
