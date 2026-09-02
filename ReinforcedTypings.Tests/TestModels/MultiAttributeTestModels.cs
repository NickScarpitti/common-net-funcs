using CommonNetFuncs.ReinforcedTypings.Collections;
using CommonNetFuncs.ReinforcedTypings.Constants;
using Reinforced.Typings.Attributes;

namespace ReinforcedTypings.Tests.TestModels.MultiAttribute;

// AutoI defaults to true, so RT's own attribute-driven export writes "IAutoIInterfaceWithConsts.ts" -
// no actual collision here, but the safe "*Constants.ts" naming is still used unconditionally.
[TsConst]
[TsInterface]
public sealed class AutoIInterfaceWithConsts
{
	public string Name { get; set; } = string.Empty;

	public const string Greeting = "hello";
}

// AutoI = false makes RT's own attribute-driven export write "PlainInterfaceWithCollections.ts" -
// the exact same path our TsCollection generator would otherwise use, a genuine collision.
[TsCollection]
[TsInterface(AutoI = false)]
public sealed class PlainInterfaceWithCollections
{
	public string Name { get; set; } = string.Empty;

	public static readonly string[] Tags = ["a", "b"];
}

[TsClass]
[TsConst]
[TsCollection]
public sealed class ClassWithConstsAndCollections
{
	public string Name { get; set; } = string.Empty;

	public const string Greeting = "hi";

	public static readonly int[] Numbers = [1, 2, 3];
}
