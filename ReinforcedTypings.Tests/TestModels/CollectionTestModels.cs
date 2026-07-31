using CommonNetFuncs.ReinforcedTypings.Collections;
using Reinforced.Typings.Attributes;

namespace ReinforcedTypings.Tests.TestModels.Collections;

[TsEnum]
public enum ExportedEnum
{
	A,
	B,
	C
}

[TsInterface]
public sealed class AutoIInterfaceModel
{
	public string Name { get; set; } = string.Empty;
}

[TsInterface(AutoI = false)]
public sealed class PlainInterfaceModel
{
	public string Name { get; set; } = string.Empty;
}

[TsClass]
public sealed class ExportedClassModel
{
	public string Name { get; set; } = string.Empty;
}

public sealed class UnresolvedModel
{
	public string Name { get; set; } = string.Empty;
}

[TsCollection]
public static class BasicCollectionsAll
{
	public static readonly string[] Names = ["a", "b", "c"];

	[TsIgnoreCollection]
	public static readonly string[] Ignored = ["x"];

	public static readonly Dictionary<string, string> ADictionary = [];

	public const string NotACollection = "skip-scalar";
}

[TsCollection(TsCollectionExportMode.Selected)]
public static class BasicCollectionsSelected
{
	public static readonly int[] NotExported = [1, 2];

	[TsExportCollection]
	public static readonly int[] Exported = [3, 4];
}

[TsCollection]
public static class ReferenceCollections
{
	public static readonly List<ExportedEnum> Enums = [ExportedEnum.A, ExportedEnum.B];

	public static readonly List<AutoIInterfaceModel> AutoIModels = [new() { Name = "one" }];

	public static readonly List<PlainInterfaceModel> PlainModels = [new() { Name = "two" }];

	public static readonly List<ExportedClassModel> ClassModels = [new() { Name = "three" }];

	public static readonly List<UnresolvedModel> Unresolved = [new() { Name = "four" }];
}

[TsCollection]
public static class EmptyEligibleCollections
{
	public const string OnlyScalar = "nothing-to-export";
}

[TsCollection]
public static class CrossNamespaceCollections
{
	public static readonly List<Other.OtherNamespaceModel> Items = [new() { Name = "cross" }];
}
