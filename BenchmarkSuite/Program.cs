using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkSuite;

internal class Program
{
	// static void Main(string[] args)
	// {
	// 	// Summary[] _ = BenchmarkRunner.Run(typeof(Program).Assembly); // Run All Benchmarks in Assembly

	// 	BenchmarkRunner.Run<CompressionStreamsBenchmarks>();
	// }

	// This setup allow running specific benchmarks from command line like this:
	// dotnet run -c Release -- --job short --runtimes Net10_0 --filter *BenchmarkClass1*
	static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
