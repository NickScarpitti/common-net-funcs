using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkSuite;

internal static class Program
{
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable IDE0060 // Remove unused parameter
	static void Main(string[] args)
	{
		Summary[] _ = BenchmarkRunner.Run(typeof(Program).Assembly);
	}
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter
}
