namespace CommonNetFuncs.ReinforcedTypings.Constants;

/// <summary>
/// Controls which fields are exported when <see cref="TsConstAttribute"/> is applied.
/// </summary>
public enum TsConstExportMode
{
	/// <summary>
	/// Export every eligible field <em>except</em> those decorated with
	/// <see cref="TsIgnoreConstAttribute"/>. This is the default.
	/// </summary>
	All = 0,

	/// <summary>
	/// Export <em>only</em> fields that are explicitly decorated with
	/// <see cref="TsExportConstAttribute"/>.
	/// </summary>
	Selected = 1
}

/// <summary>
/// Marks a static C# class whose <c>public const</c> and <c>public static readonly</c>
/// members — of <em>any</em> field type (strings, numbers, booleans, <c>Guid</c>/date types,
/// enums, arrays/collections, or class/record instances) — should be exported to TypeScript
/// as an <c>as const</c> object literal by <see cref="ReinforcedTypingsFluentConfig"/>.
///
/// <para>By default (<see cref="TsConstExportMode.All"/>) every eligible field is exported
/// unless it carries <see cref="TsIgnoreConstAttribute"/>.</para>
/// <para>Use <see cref="TsConstExportMode.Selected"/> to opt-in individual fields via
/// <see cref="TsExportConstAttribute"/> instead.</para>
///
/// Usage:
/// <code>
/// // Export all, suppress one:
/// [TsConst]
/// public static class RoleConstants
/// {
///     public const string NmPkgPlAdminName = "NM Pkg PL Admin";  // exported
///
///     [TsIgnoreConst]
///     public const string InternalOnly = "internal";              // skipped
/// }
///
/// // Export only selected:
/// [TsConst(TsConstExportMode.Selected)]
/// public static class PolicyNames
/// {
///     public const string InternalPolicy = "InternalPolicy";      // skipped
///
///     [TsExportConst]
///     public const string EditUsersPolicy = "EditUsersPolicy";    // exported
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TsConstAttribute(TsConstExportMode mode = TsConstExportMode.All) : Attribute
{
	/// <summary>Controls which fields are included in the generated TypeScript file.</summary>
	public TsConstExportMode Mode { get; } = mode;
}
