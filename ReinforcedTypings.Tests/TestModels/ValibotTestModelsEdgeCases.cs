using System.ComponentModel.DataAnnotations;

namespace ReinforcedTypings.Tests.TestModels.Valibot.EdgeCases;

// Deliberately "broken" duplicates of the custom attributes in ValibotTestModels.cs: each is missing
// the property ValibotSchemaGenerator's Get*Pipe methods look up via reflection, so that the
// "property not found -> return null" fallback branch of every one of those methods gets exercised
// (the "happy path" versions in ValibotTestModels.cs always have the expected property populated).
[AttributeUsage(AttributeTargets.Property)]
public sealed class ListMaxLengthAttribute : ValidationAttribute; // no "Length" property

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListStringLengthAttribute : ValidationAttribute; // no "MaximumLength"/"MinimumLength" properties

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListRangeAttribute : ValidationAttribute; // no "Minimum"/"Maximum" properties

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListRegularExpressionAttribute : ValidationAttribute; // no "Pattern" property

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListDenyCharactersAttribute : ValidationAttribute; // no "Characters"/"DeniedCharacters" properties

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListDenyRegularExpressionAttribute : ValidationAttribute; // no "Pattern" property

[AttributeUsage(AttributeTargets.Property)]
public sealed class DenyCharactersAttribute : ValidationAttribute; // no "Characters"/"DeniedCharacters" properties

[AttributeUsage(AttributeTargets.Property)]
public sealed class DenyRegularExpressionAttribute : ValidationAttribute; // no "Pattern" property

[AttributeUsage(AttributeTargets.Property)]
public sealed class AllowedNullableValuesAttribute : ValidationAttribute; // no "Values"/"AllowedValues" property

/// <summary>Every property here is decorated with a "broken" attribute above, so each Get*Pipe method's
/// null-fallback path (missing expected reflected property) gets exercised.</summary>
public sealed class EdgeCaseAttributeModel
{
	[ListMaxLength]
	public string MissingLength { get; set; } = string.Empty;

	[ListStringLength]
	public string MissingLengths { get; set; } = string.Empty;

	[ListRange]
	public string MissingRange { get; set; } = string.Empty;

	[ListRegularExpression]
	public string MissingPattern { get; set; } = string.Empty;

	[ListDenyCharacters]
	public string MissingDenyChars { get; set; } = string.Empty;

	[ListDenyRegularExpression]
	public string MissingDenyPattern { get; set; } = string.Empty;

	[DenyCharacters]
	public string MissingDenyCharsOuter { get; set; } = string.Empty;

	[DenyRegularExpression]
	public string MissingDenyPatternOuter { get; set; } = string.Empty;

	[AllowedNullableValues]
	public string? MissingValues { get; set; }
}

// Deliberately missing the "SourceType" property that ValibotTestModels.SubsetOfAttribute has, forcing
// ValibotSchemaGenerator.ResolveSourceType to fall back to reading the constructor argument directly
// via CustomAttributeData.
[AttributeUsage(AttributeTargets.Class)]
public sealed class SubsetOfAttribute(Type sourceType) : Attribute
{
	public Type CtorSourceType { get; } = sourceType;
}

public sealed class EdgeSourceModel
{
	[Required]
	[MaxLength(7)]
	public string Code { get; set; } = string.Empty;
}

[SubsetOf(typeof(EdgeSourceModel))]
public sealed class EdgeBoundSourceModel
{
	public string Code { get; set; } = string.Empty;
}
