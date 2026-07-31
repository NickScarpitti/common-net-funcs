namespace CommonNetFuncs.ReinforcedTypings.Collections;

/// <summary>
/// Applied to a <c>public const</c> or <c>public static readonly</c> field inside a
/// <see cref="TsCollectionAttribute"/> class that uses
/// <see cref="TsCollectionExportMode.Selected"/> to explicitly opt that field in to TypeScript generation.
///
/// Only fields carrying this attribute are exported when the containing class is tagged
/// <c>[TsCollection(TsCollectionExportMode.Selected)]</c>.
///
/// Usage:
/// <code>
/// [TsCollection(TsCollectionExportMode.Selected)]
/// public static class PolicyNames
/// {
///     // Not exported — no [TsExportCollection]
///     public const string InternalPolicy = "InternalPolicy";
///
///     // Exported
///     [TsExportCollection]
///     public const string EditUsersPolicy = "EditUsersPolicy";
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public sealed class TsExportCollectionAttribute : Attribute { }
