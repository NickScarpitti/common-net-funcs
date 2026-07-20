namespace CommonNetFuncs.ReinforcedTypings.Collections;

/// <summary>
/// Controls which fields are exported when <see cref="TsCollectionAttribute"/> is applied.
/// </summary>
public enum TsCollectionExportMode
{
	/// <summary>
	/// Export every eligible field <em>except</em> those decorated with
	/// <see cref="TsIgnoreCollectionAttribute"/>. This is the default.
	/// </summary>
	All = 0,

	/// <summary>
	/// Export <em>only</em> fields that are explicitly decorated with
	/// <see cref="TsExportCollectionAttribute"/>.
	/// </summary>
	Selected = 1
}

/// <summary>
/// Marks a static C# class whose <c>public const string</c> and
/// <c>public static readonly string[]</c> members should be exported to TypeScript
/// as an <c>as const</c> object literal by <see cref="ReinforcedTypingsFluentConfig"/>.
///
/// <para>By default (<see cref="TsCollectionExportMode.All"/>) every eligible field is exported
/// unless it carries <see cref="TsIgnoreCollectionAttribute"/>.</para>
/// <para>Use <see cref="TsCollectionExportMode.Selected"/> to opt-in individual fields via
/// <see cref="TsExportCollectionAttribute"/> instead.</para>
///
/// Usage:
/// <code>
/// // Export all, suppress one:
/// [TsCollections]
/// public static class RoleConstants
/// {
///     public const string NmPkgPlAdminName = "NM Pkg PL Admin";  // exported
///
///     [TsIgnoreCollection]
///     public const string InternalOnly = "internal";              // skipped
/// }
///
/// // Export only selected:
/// [TsCollections(TsCollectionExportMode.Selected)]
/// public static class PolicyNames
/// {
///     public const string InternalPolicy = "InternalPolicy";      // skipped
///
///     [TsExportCollection]
///     public const string EditUsersPolicy = "EditUsersPolicy";    // exported
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TsCollectionAttribute(TsCollectionExportMode mode = TsCollectionExportMode.All) : Attribute
{
	/// <summary>Controls which fields are included in the generated TypeScript file.</summary>
	public TsCollectionExportMode Mode { get; } = mode;
}
