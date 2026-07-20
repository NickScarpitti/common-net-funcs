namespace CommonNetFuncs.ReinforcedTypings.Collections;

/// <summary>
/// Applied to a <c>public const</c> or <c>public static readonly</c> field inside a
/// <see cref="TsCollectionAttribute"/> class to exclude it from TypeScript generation.
///
/// Used with the default <see cref="TsCollectionExportMode.All"/> mode, where every field is
/// exported <em>except</em> those carrying this attribute.
///
/// Usage:
/// <code>
/// [TsCollection]
/// public static class RoleConstants
/// {
///     public const string NmPkgPlAdminName = "NM Pkg PL Admin";
///
///     /// Internal — do not export to TypeScript.
///     [TsIgnoreCollection]
///     public const string InternalOnlyName = "Internal";
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public sealed class TsIgnoreCollectionAttribute : Attribute { }
