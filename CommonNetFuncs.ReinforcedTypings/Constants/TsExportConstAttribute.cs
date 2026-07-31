namespace CommonNetFuncs.ReinforcedTypings.Constants;

/// <summary>
/// Applied to a <c>public const</c> or <c>public static readonly</c> field inside a
/// <see cref="TsConstAttribute"/> class that uses
/// <see cref="TsConstExportMode.Selected"/> to explicitly opt that field in to TypeScript generation.
///
/// Only fields carrying this attribute are exported when the containing class is tagged
/// <c>[TsConst(TsConstExportMode.Selected)]</c>.
///
/// Usage:
/// <code>
/// [TsConst(TsConstExportMode.Selected)]
/// public static class PolicyNames
/// {
///     // Not exported — no [TsExportConst]
///     public const string InternalPolicy = "InternalPolicy";
///
///     // Exported
///     [TsExportConst]
///     public const string EditUsersPolicy = "EditUsersPolicy";
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public sealed class TsExportConstAttribute : Attribute { }
