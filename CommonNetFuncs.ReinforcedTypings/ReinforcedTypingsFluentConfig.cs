using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using CommonNetFuncs.ReinforcedTypings.Collections;
using CommonNetFuncs.ReinforcedTypings.Constants;
using CommonNetFuncs.ReinforcedTypings.Valibot;
using Reinforced.Typings;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using ZLinq;

namespace CommonNetFuncs.ReinforcedTypings;

/// <summary>
/// Fluent configuration that augments the attribute-based RT config.
/// <list type="bullet">
/// <item>Applies <see cref="ValibotSchemaGenerator"/> to types decorated with both
/// [TsInterface] and [GenerateValibotSchema].</item>
/// <item>Generates <c>as const</c> TypeScript files for static classes decorated
/// with [TsConst], respecting <see cref="TsConstExportMode"/>,
/// <see cref="TsIgnoreConstAttribute"/>, and <see cref="TsExportConstAttribute"/>.
/// Any field type is supported (scalars, enums, `Guid`/date types, nested collections,
/// and class/record instances), not just strings.</item>
/// <item>Generates TypeScript array literal files for static classes decorated with
/// [TsCollections], respecting <see cref="TsCollectionExportMode"/>,
/// <see cref="TsIgnoreCollectionAttribute"/>, and <see cref="TsExportCollectionAttribute"/>.
/// Collection element types that are themselves exported via [TsInterface]/[TsClass]/[TsEnum]
/// use their real generated type (imported from the sibling generated file); everything else
/// falls back to <c>any</c>.</item>
/// </list>
/// Referenced via RtConfigurationMethod in Reinforced.Typings.settings.xml. Types are discovered
/// from <see cref="ExportContext.SourceAssemblies"/> and output is written to whichever directory
/// the consuming project's own <c>RtTargetDirectory</c>/<c>RtTargetFile</c> settings resolve to, so
/// this class behaves correctly when shared as a library across multiple projects with their own
/// Reinforced.Typings.settings.xml.
/// The hand-written TsConst/TsCollections output also honors the resolved <see cref="ExportContext.Global"/>
/// parameters (as configured via <c>[TsGlobal]</c> or fluent <c>builder.Global(...)</c>), namely
/// <c>WriteWarningComment</c>, <c>TabSymbol</c>, <c>NewLine</c>, <c>CamelCaseForProperties</c>,
/// <c>UnresolvedToUnknown</c>, <c>UseModules</c>, <c>DiscardNamespacesWhenUsingModules</c>,
/// <c>RootNamespace</c>, and <c>ExportPureTypings</c>:
/// <list type="bullet">
/// <item>When <c>UseModules</c> is <c>false</c>, no top-level <c>export</c>/<c>import</c> is emitted
/// (which would otherwise turn the file into an ES module and can break global-namespace-style
/// consumers). Output is instead wrapped in a TS <c>namespace</c> block mirroring the C# type's
/// namespace (with <c>export</c> only appearing *inside* that block, as required for member
/// visibility), or left as a bare global <c>const</c> when the type has no namespace.</item>
/// <item><c>DiscardNamespacesWhenUsingModules</c> controls whether generated files are written flat
/// into the output directory or nested into namespace-mirroring subdirectories, matching RT's own
/// rule that this setting only applies when <c>UseModules</c> is <c>true</c> (namespaces always
/// factor into file arrangement when not using modules).</item>
/// <item><c>ExportPureTypings</c> emits an ambient, implementation-free shape declaration (<c>declare
/// const</c>) instead of the real <c>as const</c> literal, matching RT's ".d.ts only" convention.</item>
/// </list>
/// </summary>
public static class ReinforcedTypingsFluentConfig
{
	public static void Configure(ConfigurationBuilder builder)
	{
		// Scan the assemblies the consuming project's own RT settings are configured to export
		// from (RtAssemblies / entry assembly), rather than only the assembly this class lives in.
		// This is what allows the class to be shared as a library across multiple projects.
		Assembly[] sourceAssemblies = builder.Context.SourceAssemblies is { Length: > 0 }
			? builder.Context.SourceAssemblies
			: [typeof(ReinforcedTypingsFluentConfig).Assembly];

		Type[] allTypes = sourceAssemblies.AsValueEnumerable().SelectMany(a => a.GetTypes()).ToArray();

		// ── Valibot schema generation ──────────────────────────────────────────
		Type[] schemaTypes = allTypes
			.Where(t => t.GetCustomAttribute<TsInterfaceAttribute>() != null && t.GetCustomAttribute<GenerateValibotSchemaAttribute>() != null)
			.ToArray();

		if (schemaTypes.Length > 0)
		{
			builder.ExportAsInterfaces(schemaTypes, config => config.WithCodeGenerator<ValibotSchemaGenerator>());
		}

		// Resolved once from the consuming project's own RT settings (RtTargetDirectory when
		// RtDivideTypesAmongFiles/Hierarchical, otherwise the directory portion of RtTargetFile).
		string? outputDir = ResolveOutputDirectory(builder.Context);

		// Resolved [TsGlobal] / fluent builder.Global(...) parameters for the consuming project, so the
		// hand-written const/collection output matches the same conventions RT itself would use.
		GlobalParameters global = builder.Context.Global;

		// ── TsConst generation ───────────────────────────────────────────
		Type[] constTypes = allTypes
			.Where(t => t.GetCustomAttribute<TsConstAttribute>() != null)
			.ToArray();

		if (constTypes.Length > 0 && outputDir != null)
		{
			foreach (Type constType in constTypes)
			{
				TsConstAttribute attr = constType.GetCustomAttribute<TsConstAttribute>()!;
				string tsContent = GenerateTsConst(constType, attr.Mode, global);
				string filePath = ResolveTypeFilePath(constType, outputDir, global);
				File.WriteAllText(filePath, tsContent);
			}
		}

		// ── TsCollection generation ───────────────────────────────────────────
		Type[] collectionTypes = allTypes
			.Where(t => t.GetCustomAttribute<TsCollectionAttribute>() != null)
			.ToArray();

		if (collectionTypes.Length > 0 && outputDir != null)
		{
			foreach (Type collectionType in collectionTypes)
			{
				TsCollectionAttribute attr = collectionType.GetCustomAttribute<TsCollectionAttribute>()!;
				string tsContent = GenerateTsCollection(collectionType, attr.Mode, global);
				if (string.IsNullOrEmpty(tsContent))
				{
					continue; // No eligible collection fields found on this type.
				}

				string filePath = ResolveTypeFilePath(collectionType, outputDir, global);
				File.WriteAllText(filePath, tsContent);
			}
		}
	}

	/// <summary>
	/// Generates an <c>as const</c> TypeScript object literal for every eligible field on
	/// <paramref name="type"/>, regardless of field type. Values are serialized via
	/// <see cref="SerializeTsValue"/>, which supports scalars, enums, <c>Guid</c>/date types,
	/// nested arrays/collections, and class/record instances (as nested object literals).
	/// Honors <paramref name="global"/>'s <c>WriteWarningComment</c>, <c>TabSymbol</c>, <c>NewLine</c>,
	/// <c>CamelCaseForProperties</c>, <c>UseModules</c>/<c>RootNamespace</c> (see
	/// <see cref="ResolveTsNamespace"/>), and <c>ExportPureTypings</c> settings.
	/// </summary>
	private static string GenerateTsConst(Type type, TsConstExportMode mode, GlobalParameters global)
	{
		StringBuilder sb = new();
		void Line(string text = "") => sb.Append(text).Append(global.NewLine);

		if (global.WriteWarningComment)
		{
			Line("//     This code was generated by a Reinforced.Typings tool.");
			Line("//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.");
			Line();
		}

		// When UseModules is false, a top-level `export`/`import` would turn this file into an ES
		// module, which can break global-namespace-style consumers. Mirror RT's own "internal module"
		// (TS namespace) convention instead: `export` then only appears *inside* the namespace block
		// (required for member visibility), never at the top level; types with no namespace fall back
		// to a bare global `const` (no `export` at all).
		string? tsNamespace = global.UseModules ? null : ResolveTsNamespace(type, global);
		bool wrapInNamespace = tsNamespace != null;
		string indent = wrapInNamespace ? global.TabSymbol : string.Empty;
		bool needsExport = global.UseModules || wrapInNamespace;

		List<(string PropName, object? Value, Type FieldType)> fields = [];
		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			bool shouldExport = mode switch
			{
				TsConstExportMode.Selected => field.GetCustomAttribute<TsExportConstAttribute>() != null,
				_ => field.GetCustomAttribute<TsIgnoreConstAttribute>() == null  // All: export unless ignored
			};

			if (!shouldExport)
			{
				continue;
			}

			fields.Add((ToPropertyName(field.Name, global), field.GetValue(null), field.FieldType));
		}

		if (wrapInNamespace)
		{
			Line($"{(global.ExportPureTypings ? "declare namespace" : "namespace")} {tsNamespace} {{");
		}

		if (global.ExportPureTypings)
		{
			// Pure-typings mode emits an ambient shape declaration only (no runtime implementation),
			// matching RT's own ".d.ts only" export convention. Literal value syntax doubles as
			// TypeScript literal-type syntax, so the same serialization is reused for the typing.
			string declarePrefix = wrapInNamespace ? "export const" : needsExport ? "export declare const" : "declare const";
			Line($"{indent}{declarePrefix} {type.Name}: {{");
			foreach ((string propName, object? value, Type fieldType) in fields)
			{
				Line($"{indent}{global.TabSymbol}readonly {propName}: {SerializeTsValue(value, fieldType, global)};");
			}
			Line($"{indent}}};");
		}
		else
		{
			string declKeyword = needsExport ? "export const" : "const";
			string typeKeyword = needsExport ? "export type" : "type";

			Line($"{indent}{declKeyword} {type.Name} = {{");
			foreach ((string propName, object? value, Type fieldType) in fields)
			{
				Line($"{indent}{global.TabSymbol}{propName}: {SerializeTsValue(value, fieldType, global)},");
			}
			Line($"{indent}}} as const;");
			Line();
			Line($"{indent}{typeKeyword} {type.Name}Key = keyof typeof {type.Name};");
			Line($"{indent}{typeKeyword} {type.Name}Value = (typeof {type.Name})[{type.Name}Key];");
		}

		if (wrapInNamespace)
		{
			Line("}");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Maps built-in .NET primitive/scalar types to their TypeScript equivalents.
	/// Anything not listed here is either an enum, a class/interface, or unsupported ("any").
	/// </summary>
	private static readonly Dictionary<Type, string> BasicTsTypeMap = new()
	{
		[typeof(string)] = "string",
		[typeof(char)] = "string",
		[typeof(Guid)] = "string",
		[typeof(DateTime)] = "string",
		[typeof(DateTimeOffset)] = "string",
		[typeof(DateOnly)] = "string",
		[typeof(TimeOnly)] = "string",
		[typeof(TimeSpan)] = "string",
		[typeof(bool)] = "boolean",
		[typeof(byte)] = "number",
		[typeof(sbyte)] = "number",
		[typeof(short)] = "number",
		[typeof(ushort)] = "number",
		[typeof(int)] = "number",
		[typeof(uint)] = "number",
		[typeof(long)] = "number",
		[typeof(ulong)] = "number",
		[typeof(float)] = "number",
		[typeof(double)] = "number",
		[typeof(decimal)] = "number",
	};

	/// <summary>
	/// Generates an <c>as const</c> TypeScript array-literal collections object for every eligible
	/// field on <paramref name="type"/>. Honors the same <paramref name="global"/> settings as
	/// <see cref="GenerateTsConst"/> (<c>WriteWarningComment</c>, <c>TabSymbol</c>, <c>NewLine</c>,
	/// <c>CamelCaseForProperties</c>, <c>UseModules</c>, <c>ExportPureTypings</c>), plus how
	/// cross-references to <c>[TsInterface]</c>/<c>[TsClass]</c>/<c>[TsEnum]</c> element types are
	/// addressed: an <c>import type</c> (with a namespace-aware relative path, see
	/// <see cref="GetImportSpecifier"/>) when <c>UseModules</c> is <c>true</c>, or a fully
	/// namespace-qualified reference (see <see cref="QualifyTsName"/>) when it is <c>false</c>
	/// (since there is no module system to import from in that case).
	/// </summary>
	private static string GenerateTsCollection(Type type, TsCollectionExportMode mode, GlobalParameters global)
	{
		// ── Discover eligible collection fields ────────────────────────────────
		List<(FieldInfo Field, Type ElementType)> collectionFields = [];

		foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			bool shouldExport = mode switch
			{
				TsCollectionExportMode.Selected => field.GetCustomAttribute<TsExportCollectionAttribute>() != null,
				_ => field.GetCustomAttribute<TsIgnoreCollectionAttribute>() == null  // All: export unless ignored
			};

			if (!shouldExport)
			{
				continue;
			}

			Type? elementType = GetCollectionElementType(field.FieldType);
			if (elementType == null)
			{
				continue; // Not a recognized collection type (excluding string/dictionaries).
			}

			collectionFields.Add((field, elementType));
		}

		if (collectionFields.Count == 0)
		{
			return string.Empty;
		}

		// ── Resolve the TS type + optional cross-reference for every field's element type ──
		Dictionary<FieldInfo, string> fieldTsTypes = [];
		Dictionary<FieldInfo, Type> fieldReferencedTypes = [];

		foreach ((FieldInfo field, Type elementType) in collectionFields)
		{
			string tsType = ResolveElementTsType(elementType, global, out Type? referencedType);
			fieldTsTypes[field] = tsType;
			if (referencedType != null)
			{
				fieldReferencedTypes[field] = referencedType;
			}
		}

		string? tsNamespace = global.UseModules ? null : ResolveTsNamespace(type, global);
		bool wrapInNamespace = tsNamespace != null;
		string indent = wrapInNamespace ? global.TabSymbol : string.Empty;
		bool needsExport = global.UseModules || wrapInNamespace;

		// Under ES modules, referenced types are brought into scope via `import type` (using a
		// namespace-aware relative path so it still resolves when files are nested into
		// namespace-mirroring subdirectories). Without a module system there is nothing to import,
		// so referenced types are addressed via their fully namespace-qualified name instead.
		Dictionary<FieldInfo, string> fieldCastTypes = [];
		SortedDictionary<string, string> imports = new(StringComparer.Ordinal); // import specifier -> imported type name

		foreach ((FieldInfo field, Type elementType) in collectionFields)
		{
			string tsType = fieldTsTypes[field];
			if (!fieldReferencedTypes.TryGetValue(field, out Type? referencedType))
			{
				fieldCastTypes[field] = tsType;
				continue;
			}

			if (global.UseModules)
			{
				imports[GetImportSpecifier(type, referencedType, global)] = tsType;
				fieldCastTypes[field] = tsType;
			}
			else
			{
				fieldCastTypes[field] = QualifyTsName(referencedType, tsType, global);
			}
		}

		// ── Emit the file ───────────────────────────────────────────────────────
		StringBuilder sb = new();
		void Line(string text = "") => sb.Append(text).Append(global.NewLine);

		if (global.WriteWarningComment)
		{
			Line("//     This code was generated by a Reinforced.Typings tool.");
			Line("//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.");
			Line();
		}

		foreach (KeyValuePair<string, string> import in imports)
		{
			Line($"import type {{ {import.Value} }} from '{import.Key}';");
		}

		if (imports.Count > 0)
		{
			Line();
		}

		if (wrapInNamespace)
		{
			Line($"{(global.ExportPureTypings ? "declare namespace" : "namespace")} {tsNamespace} {{");
		}

		if (global.ExportPureTypings)
		{
			string declarePrefix = wrapInNamespace ? "export const" : needsExport ? "export declare const" : "declare const";
			Line($"{indent}{declarePrefix} {type.Name}Collections: {{");
			foreach ((FieldInfo field, _) in collectionFields)
			{
				string propName = ToPropertyName(field.Name, global);
				Line($"{indent}{global.TabSymbol}readonly {propName}: readonly {fieldCastTypes[field]}[];");
			}
			Line($"{indent}}};");
		}
		else
		{
			string declKeyword = needsExport ? "export const" : "const";
			string typeKeyword = needsExport ? "export type" : "type";

			Line($"{indent}{declKeyword} {type.Name}Collections = {{");
			foreach ((FieldInfo field, Type elementType) in collectionFields)
			{
				object? rawValue = field.GetValue(null);
				string arrayLiteral = SerializeCollectionLiteral(rawValue, elementType, global);
				string propName = ToPropertyName(field.Name, global);
				Line($"{indent}{global.TabSymbol}{propName}: {arrayLiteral} as {fieldCastTypes[field]}[],");
			}
			Line($"{indent}}} as const;");
			Line();
			Line($"{indent}{typeKeyword} {type.Name}CollectionsKey = keyof typeof {type.Name}Collections;");
			Line($"{indent}{typeKeyword} {type.Name}CollectionsValue = (typeof {type.Name}Collections)[{type.Name}CollectionsKey];");
		}

		if (wrapInNamespace)
		{
			Line("}");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Returns the element type of <paramref name="fieldType"/> if it is an array or a generic
	/// collection (<c>List&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>, etc.),
	/// excluding <see cref="string"/> (itself an <c>IEnumerable&lt;char&gt;</c>) and dictionaries.
	/// Returns <c>null</c> when the type is not a recognized single-generic-argument collection.
	/// </summary>
	private static Type? GetCollectionElementType(Type fieldType)
	{
		if (fieldType == typeof(string) || typeof(IDictionary).IsAssignableFrom(fieldType))
		{
			return null;
		}

		if (fieldType.IsArray)
		{
			return fieldType.GetElementType();
		}

		if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
		{
			return null;
		}

		if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			return fieldType.GetGenericArguments()[0];
		}

		Type? enumerableInterface = fieldType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		return enumerableInterface?.GetGenericArguments()[0];
	}

	/// <summary>
	/// Resolves the TypeScript type name to use for <paramref name="elementType"/>, honoring the
	/// relevant Reinforced.Typings export attributes:
	/// <list type="bullet">
	/// <item>Basic/scalar .NET types map directly to <c>string</c> / <c>number</c> / <c>boolean</c>.</item>
	/// <item>Enums decorated with <c>[TsEnum]</c> use the actual generated enum name/import.</item>
	/// <item>Classes/records decorated with <c>[TsInterface]</c> use the actual generated interface
	/// name (honoring that attribute's <c>AutoI</c> "I" prefix) and are imported from their
	/// generated sibling file.</item>
	/// <item>Classes decorated with <c>[TsClass]</c> use the actual generated class name.</item>
	/// <item>Anything else (not exported by Reinforced.Typings) falls back to <c>any</c>, or <c>unknown</c>
	/// when <paramref name="global"/>.<c>UnresolvedToUnknown</c> is set.</item>
	/// </list>
	/// When a cross-reference is required, <paramref name="referencedType"/> is set to the underlying
	/// exported .NET type (used to resolve its generated sibling file/namespace); otherwise <c>null</c>.
	/// </summary>
	private static string ResolveElementTsType(Type elementType, GlobalParameters global, out Type? referencedType)
	{
		referencedType = null;
		string unresolved = global.UnresolvedToUnknown ? "unknown" : "any";

		Type underlying = Nullable.GetUnderlyingType(elementType) ?? elementType;

		if (BasicTsTypeMap.TryGetValue(underlying, out string? basicTsType))
		{
			return basicTsType;
		}

		if (underlying.IsEnum)
		{
			if (underlying.GetCustomAttribute<TsEnumAttribute>() != null)
			{
				referencedType = underlying;
				return underlying.Name;
			}

			return unresolved;
		}

		TsInterfaceAttribute? tsInterface = underlying.GetCustomAttribute<TsInterfaceAttribute>();
		if (tsInterface != null)
		{
			referencedType = underlying;
			return tsInterface.AutoI ? $"I{underlying.Name}" : underlying.Name;
		}

		TsClassAttribute? tsClass = underlying.GetCustomAttribute<TsClassAttribute>();
		if (tsClass != null)
		{
			referencedType = underlying;
			return underlying.Name;
		}

		return unresolved;
	}

	/// <summary>
	/// Serializes the actual runtime collection <paramref name="rawValue"/> (e.g. the value of a
	/// <c>public static readonly List&lt;T&gt;</c> field) into a TypeScript array literal, recursively
	/// serializing class-typed elements as object literals.
	/// </summary>
	private static string SerializeCollectionLiteral(object? rawValue, Type elementType, GlobalParameters global)
	{
		if (rawValue is not IEnumerable enumerable || rawValue is string)
		{
			return "[]";
		}

		List<string> items = [];
		foreach (object? item in enumerable)
		{
			items.Add(SerializeTsValue(item, elementType, global));
		}

		return $"[{string.Join(", ", items)}]";
	}

	/// <summary>
	/// Serializes a single runtime value to its TypeScript literal representation. Handles scalars,
	/// enums (respecting <c>[TsEnum(UseString = true)]</c>), nested collections, and class/record
	/// instances (recursively emitted as object literals, honoring <c>[TsIgnore]</c> on properties).
	/// /// </summary>
	private static string SerializeTsValue(object? value, Type declaredType, GlobalParameters global)
	{
		if (value is null)
		{
			return "null";
		}

		Type valueType = value.GetType();

		if (valueType.IsEnum)
		{
			TsEnumAttribute? tsEnum = valueType.GetCustomAttribute<TsEnumAttribute>();
			return tsEnum is { UseString: true }
				? $"'{EscapeTs(value.ToString()!)}'"
				: Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture);
		}

		switch (value)
		{
			case string s: return $"'{EscapeTs(s)}'";
			case bool b: return b ? "true" : "false";
			case Guid g: return $"'{g}'";
			case DateTime dt: return $"'{dt.ToString("O", CultureInfo.InvariantCulture)}'";
			case DateTimeOffset dto: return $"'{dto.ToString("O", CultureInfo.InvariantCulture)}'";
			case DateOnly d: return $"'{d.ToString("O", CultureInfo.InvariantCulture)}'";
			case TimeOnly t: return $"'{t.ToString("O", CultureInfo.InvariantCulture)}'";
			case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
				return Convert.ToString(value, CultureInfo.InvariantCulture)!;
		}

		Type? nestedElementType = GetCollectionElementType(valueType);
		if (nestedElementType != null && value is IEnumerable nestedEnumerable)
		{
			return SerializeCollectionLiteral(nestedEnumerable, nestedElementType, global);
		}

		return SerializeTsObjectLiteral(value, valueType, global);
	}

	/// <summary>
	/// Serializes a class/record instance as a TypeScript object literal, using its public instance
	/// properties (skipping those marked <c>[TsIgnore]</c> or indexers) so the emitted data matches
	/// the interface Reinforced.Typings would generate for that type. Property names honor
	/// <paramref name="global"/>.<c>CamelCaseForProperties</c>.
	/// </summary>
	private static string SerializeTsObjectLiteral(object value, Type type, GlobalParameters global)
	{
		List<string> propEntries = [];

		foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetIndexParameters().Length > 0 || !prop.CanRead || prop.GetCustomAttribute<TsIgnoreAttribute>() != null)
			{
				continue;
			}

			object? propValue = prop.GetValue(value);
			string propName = ToPropertyName(prop.Name, global);
			propEntries.Add($"{propName}: {SerializeTsValue(propValue, prop.PropertyType, global)}");
		}

		return $"{{ {string.Join(", ", propEntries)} }}";
	}

	private static string EscapeTs(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");

	/// <summary>
	/// Applies <paramref name="global"/>.<c>CamelCaseForProperties</c> to a field/property name,
	/// matching the naming convention Reinforced.Typings itself would use for the same setting.
	/// </summary>
	private static string ToPropertyName(string name, GlobalParameters global) =>
		global.CamelCaseForProperties && name.Length > 0
			? char.ToLowerInvariant(name[0]) + name[1..]
			: name;

	/// <summary>
	/// Resolves the TypeScript namespace path to use for <paramref name="type"/>, honoring
	/// <paramref name="global"/>.<c>RootNamespace</c> by stripping it as a leading prefix (mirroring
	/// how RT avoids redundant nesting for hierarchical export). Returns <c>null</c> when the type
	/// has no namespace, or its namespace equals <c>RootNamespace</c> exactly.
	/// </summary>
	private static string? ResolveTsNamespace(Type type, GlobalParameters global)
	{
		string? ns = type.Namespace;
		if (string.IsNullOrEmpty(ns))
		{
			return null;
		}

		string? root = global.RootNamespace;
		if (!string.IsNullOrEmpty(root))
		{
			if (ns.Equals(root, StringComparison.Ordinal))
			{
				return null;
			}

			if (ns.StartsWith(root + ".", StringComparison.Ordinal))
			{
				ns = ns[(root.Length + 1)..];
			}
		}

		return string.IsNullOrEmpty(ns) ? null : ns;
	}

	/// <summary>
	/// Whether generated const/collection files should be written flat into the root output directory
	/// rather than nested into a namespace-mirroring subdirectory. Mirrors RT's own rule that
	/// <c>DiscardNamespacesWhenUsingModules</c> only takes effect when <c>UseModules</c> is <c>true</c>;
	/// when not using ES modules, namespaces always factor into file arrangement.
	/// </summary>
	private static bool ShouldFlattenNamespaceFolders(GlobalParameters global) => global.UseModules && global.DiscardNamespacesWhenUsingModules;

	/// <summary>
	/// Returns the namespace-derived relative directory (forward-slash separated; empty when flat)
	/// used for both physical file placement and cross-file import path calculation.
	/// </summary>
	private static string GetRelativeDir(Type type, GlobalParameters global)
	{
		if (ShouldFlattenNamespaceFolders(global))
		{
			return string.Empty;
		}

		string? ns = ResolveTsNamespace(type, global);
		return ns == null ? string.Empty : ns.Replace('.', '/');
	}

	/// <summary>
	/// Resolves the absolute file path for <paramref name="type"/>'s generated const/collection file,
	/// creating any namespace-mirroring subdirectory (see <see cref="GetRelativeDir"/>) as needed.
	/// </summary>
	private static string ResolveTypeFilePath(Type type, string outputDir, GlobalParameters global)
	{
		string relativeDir = GetRelativeDir(type, global);
		string dir = string.IsNullOrEmpty(relativeDir)
			? outputDir
			: Path.Combine(outputDir, relativeDir.Replace('/', Path.DirectorySeparatorChar));

		Directory.CreateDirectory(dir);
		return Path.Combine(dir, $"{type.Name}.ts");
	}

	/// <summary>
	/// Computes the relative ES module import specifier (e.g. <c>./Foo</c> or <c>../Bar/Foo</c>)
	/// to reach <paramref name="to"/>'s generated sibling file from <paramref name="from"/>'s
	/// generated file, honoring namespace-mirroring folder placement (see <see cref="GetRelativeDir"/>).
	/// </summary>
	private static string GetImportSpecifier(Type from, Type to, GlobalParameters global)
	{
		string fromDir = GetRelativeDir(from, global);
		string toDir = GetRelativeDir(to, global);

		if (fromDir == toDir)
		{
			return $"./{to.Name}";
		}

		string rel = Path.GetRelativePath(
			string.IsNullOrEmpty(fromDir) ? "." : fromDir,
			string.IsNullOrEmpty(toDir) ? "." : toDir).Replace('\\', '/');

		string prefix = rel == "." ? string.Empty : rel.StartsWith("..", StringComparison.Ordinal) ? $"{rel}/" : $"./{rel}/";
		return $"{prefix}{to.Name}";
	}

	/// <summary>
	/// Builds the fully namespace-qualified TypeScript reference for <paramref name="type"/>
	/// (e.g. <c>Foo.Bar.IBaz</c>), for use when <c>UseModules</c> is <c>false</c> and there is
	/// therefore no <c>import</c> statement available to bring <paramref name="bareName"/> into scope.
	/// </summary>
	private static string QualifyTsName(Type type, string bareName, GlobalParameters global)
	{
		string? ns = ResolveTsNamespace(type, global);
		return ns == null ? bareName : $"{ns}.{bareName}";
	}

	/// <summary>
	/// Resolves the output directory for generated const/collection files directly from the
	/// consuming project's own Reinforced.Typings settings, so this class behaves correctly
	/// when reused as a library across projects with different <c>Reinforced.Typings.settings.xml</c>
	/// configurations rather than assuming a fixed "TypeScriptModels" folder convention.
	/// <list type="bullet">
	/// <item>When <c>RtDivideTypesAmongFiles</c> is true (<see cref="ExportContext.Hierarchical"/>),
	/// uses <see cref="ExportContext.TargetDirectory"/> (<c>RtTargetDirectory</c>).</item>
	/// <item>Otherwise uses the directory portion of <see cref="ExportContext.TargetFile"/>
	/// (<c>RtTargetFile</c>).</item>
	/// </list>
	/// </summary>
	private static string? ResolveOutputDirectory(ExportContext context)
	{
		string? outputDir = context.Hierarchical
			? context.TargetDirectory
			: Path.GetDirectoryName(context.TargetFile);

		if (string.IsNullOrEmpty(outputDir))
		{
			return null;
		}

		Directory.CreateDirectory(outputDir);
		return outputDir;
	}
}


