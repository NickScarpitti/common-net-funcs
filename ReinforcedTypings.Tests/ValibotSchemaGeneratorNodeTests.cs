using System.Reflection;
using CommonNetFuncs.ReinforcedTypings.Valibot;
using Reinforced.Typings;
using Reinforced.Typings.Fluent;
using ReinforcedTypings.Tests.TestModels.Valibot;

namespace ReinforcedTypings.Tests;

/// <summary>
/// Exercises <see cref="ValibotSchemaGenerator.GenerateNode"/> (and its private
/// <c>FindProjectDirectory</c> helper) end-to-end through the real Reinforced.Typings pipeline
/// (<see cref="TsExporter.Export"/>), since <see cref="ValibotSchemaGenerator"/>'s base class
/// (<c>InterfaceCodeGenerator</c>) requires a fully RT-constructed <c>TypeResolver</c>/<c>RtInterface</c>
/// pair - internals not safely reproducible by hand (attempts to build them manually via reflection
/// throw <see cref="NullReferenceException"/> deep inside RT's own base implementation).
/// <see cref="ValibotSchemaGenerator"/> caches its resolved output directory in a
/// <c>private static string? outputDirectory</c> field for the lifetime of the process, so every test
/// here pre-seeds that field via reflection to a fresh temp directory before exporting, guaranteeing
/// each test writes only into its own sandboxed folder rather than resolving (and polluting) this test
/// project's real directory.
/// </summary>
public sealed class ValibotSchemaGeneratorNodeTests
{
	private static readonly FieldInfo OutputDirectoryField = typeof(ValibotSchemaGenerator).GetField("outputDirectory", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("outputDirectory field not found via reflection.");

	private static readonly MethodInfo FindProjectDirectoryMethod = typeof(ValibotSchemaGenerator).GetMethod("FindProjectDirectory", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("FindProjectDirectory method not found via reflection.");

	private static string CreateTempDir()
	{
		string dir = Path.Combine(Path.GetTempPath(), "rt-valibot-node-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static string? FindProjectDirectory(string startDir) => (string?)FindProjectDirectoryMethod.Invoke(null, [startDir]);

	/// <summary>Runs the real RT export pipeline against a single type, configured to use <see cref="ValibotSchemaGenerator"/>.</summary>
	private static void ExportAsInterface(Type type, string rtOutputDir)
	{
		ExportContext context = new([type.Assembly], new FilesOperations())
		{
			Hierarchical = true,
			TargetDirectory = rtOutputDir
		};
		context.ConfigurationMethod = builder => builder.ExportAsInterfaces([type], config => config.WithCodeGenerator<ValibotSchemaGenerator>());

		new TsExporter(context).Export();
	}

	[Fact]
	public void FindProjectDirectory_TypeScriptModelsFolderPresentInStartDir_ReturnsStartDir()
	{
		string startDir = CreateTempDir();
		Directory.CreateDirectory(Path.Combine(startDir, "TypeScriptModels"));

		string? result = FindProjectDirectory(startDir);

		result.ShouldBe(startDir);
	}

	[Fact]
	public void FindProjectDirectory_CsprojPresentInAncestor_ReturnsThatAncestor()
	{
		string root = CreateTempDir();
		string nested = Path.Combine(root, "bin", "Debug", "net10.0");
		Directory.CreateDirectory(nested);
		File.WriteAllText(Path.Combine(root, "SomeProject.csproj"), "<Project />");

		string? result = FindProjectDirectory(nested);

		result.ShouldBe(root);
	}

	[Fact]
	public void FindProjectDirectory_NeitherFound_ReturnsNull()
	{
		// A freshly created, uniquely-named temp directory with no ancestor csproj/TypeScriptModels
		// folder (walking up through the OS temp directory tree) should exhaust the search and return null.
		string startDir = CreateTempDir();

		string? result = FindProjectDirectory(startDir);

		result.ShouldBeNull();
	}

	[Fact]
	public void GenerateNode_TypeWithoutGenerateValibotSchemaAttribute_SkipsSchemaGeneration()
	{
		string schemaDir = CreateTempDir();
		OutputDirectoryField.SetValue(null, schemaDir);

		// SourceModel has no [GenerateValibotSchema]; the generator's defense-in-depth guard should
		// return the base-generated interface node without ever resolving/writing a companion schema.
		ExportAsInterface(typeof(SourceModel), CreateTempDir());

		Directory.GetFiles(schemaDir).ShouldBeEmpty();
	}

	[Fact]
	public void GenerateNode_TypeWithGenerateValibotSchemaAttribute_WritesSchemaFile()
	{
		string schemaDir = CreateTempDir();
		OutputDirectoryField.SetValue(null, schemaDir);

		ExportAsInterface(typeof(FullValidationModel), CreateTempDir());

		string schemaPath = Path.Combine(schemaDir, "FullValidationModel.schema.ts");
		File.Exists(schemaPath).ShouldBeTrue();
		File.ReadAllText(schemaPath).ShouldContain("export const FullValidationModelSchema = v.object({");
	}

	[Fact]
	public void GenerateNode_OutputDirectoryNotYetCached_ResolvesItFromTheProjectContainingTypeScriptModels()
	{
		// Force the real (uncached) resolution path: outputDirectory starts null, so GenerateNode must
		// call FindProjectDirectory itself, walking up from this test assembly's own bin/ output to the
		// ReinforcedTypings.Tests project directory (which has a .csproj) and creating "TypeScriptModels"
		// there - a real, if transient, side effect in this project's own directory, cleaned up below.
		OutputDirectoryField.SetValue(null, null);
		string? resolvedDir = null;
		try
		{
			ExportAsInterface(typeof(FullValidationModel), CreateTempDir());

			resolvedDir = (string?)OutputDirectoryField.GetValue(null);
			resolvedDir.ShouldNotBeNull();
			Path.GetFileName(resolvedDir).ShouldBe("TypeScriptModels");
			File.Exists(Path.Combine(resolvedDir!, "FullValidationModel.schema.ts")).ShouldBeTrue();
		}
		finally
		{
			if (resolvedDir != null && Directory.Exists(resolvedDir))
			{
				Directory.Delete(resolvedDir, recursive: true);
			}

			// Reset the process-lifetime cache so it doesn't leak this real path into other tests in this class.
			OutputDirectoryField.SetValue(null, null);
		}
	}
}
