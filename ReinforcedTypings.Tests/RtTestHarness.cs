using CommonNetFuncs.ReinforcedTypings;
using Reinforced.Typings;
using Reinforced.Typings.Fluent;

namespace ReinforcedTypings.Tests;

/// <summary>
/// Shared helper that drives <see cref="ReinforcedTypingsFluentConfig.Configure"/> through the real
/// Reinforced.Typings pipeline (<see cref="TsExporter.Initialize"/>), which is the only public entry
/// point capable of constructing a <see cref="ConfigurationBuilder"/> (its constructor is internal to
/// the Reinforced.Typings package). Each call gets its own temp output directory so generated files
/// never collide between tests and can be safely inspected/asserted on afterwards.
/// </summary>
internal static class RtTestHarness
{
	/// <summary>
	/// Runs <see cref="ReinforcedTypingsFluentConfig.Configure"/> against a fresh <see cref="ExportContext"/>
	/// scanning only this test assembly, writing hand-written TsConst/TsCollection output to a new temp
	/// directory. <paramref name="configureContext"/> can further tweak <see cref="ExportContext.Global"/>
	/// (or any other context setting) before generation runs.
	/// </summary>
	public static string RunConfigure(Action<ExportContext>? configureContext = null)
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "rt-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		ExportContext context = new([typeof(RtTestHarness).Assembly], new FilesOperations())
		{
			Hierarchical = true,
			TargetDirectory = tempDir
		};

		configureContext?.Invoke(context);

		context.ConfigurationMethod = ReinforcedTypingsFluentConfig.Configure;
		new TsExporter(context).Initialize();

		return tempDir;
	}

	/// <summary>Reads a generated file relative to <paramref name="outputDir"/>, using '/' as the path separator.</summary>
	public static string ReadGenerated(string outputDir, string relativePath)
	{
		return File.ReadAllText(Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));
	}

	/// <summary>Whether a generated file exists relative to <paramref name="outputDir"/>, using '/' as the path separator.</summary>
	public static bool GeneratedFileExists(string outputDir, string relativePath)
	{
		return File.Exists(Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));
	}
}
