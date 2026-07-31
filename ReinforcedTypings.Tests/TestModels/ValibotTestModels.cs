using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.ReinforcedTypings.Valibot;
using Reinforced.Typings.Attributes;

namespace ReinforcedTypings.Tests.TestModels.Valibot;

// Fakes for CommonNetFuncs.Web.Common.ValidationAttributes, matched by type name only
// (ValibotSchemaGenerator.GetCustomAttributePipe switches on attr.GetType().Name). These derive from
// ValidationAttribute (like the real ones presumably do) so that GetAttrErrorMessage's "attr is
// ValidationAttribute" branch and the DataAnnotations attr-switch's default arm both get exercised,
// and so the inherited ErrorMessage property can be used directly.
[AttributeUsage(AttributeTargets.Property)]
public sealed class ListMaxLengthAttribute(int length) : ValidationAttribute
{
	public int Length { get; } = length;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListStringLengthAttribute(int maximumLength) : ValidationAttribute
{
	public int MaximumLength { get; } = maximumLength;

	public int MinimumLength { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListRangeAttribute : ValidationAttribute
{
	public object? Minimum { get; set; }

	public object? Maximum { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListRegularExpressionAttribute(string pattern) : ValidationAttribute
{
	public string Pattern { get; } = pattern;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListDenyCharactersAttribute(string characters) : ValidationAttribute
{
	public string Characters { get; } = characters;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ListDenyRegularExpressionAttribute(string pattern) : ValidationAttribute
{
	public string Pattern { get; } = pattern;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DenyCharactersAttribute(string characters) : ValidationAttribute
{
	public string Characters { get; } = characters;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DenyRegularExpressionAttribute(string pattern) : ValidationAttribute
{
	public string Pattern { get; } = pattern;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class AllowedNullableValuesAttribute(params object[] values) : ValidationAttribute
{
	public object[] Values { get; } = values;
}

// Fake for a [SubsetOf(typeof(X))] attribute (matched by type name in ValibotSchemaGenerator.ResolveSourceType).
[AttributeUsage(AttributeTargets.Class)]
public sealed class SubsetOfAttribute(Type sourceType) : Attribute
{
	public Type SourceType { get; } = sourceType;
}

public enum ValibotColor
{
	Red,
	Green,
	Blue
}

public sealed class NestedValibotModel
{
	public string Label { get; set; } = string.Empty;

	[Required]
	public string RequiredChild { get; set; } = string.Empty;

	[TsIgnore]
	public string HiddenChild { get; set; } = string.Empty;
}

public sealed class CyclicModel
{
	public string Name { get; set; } = string.Empty;

	public CyclicModel? Self { get; set; }
}

[TsInterface]
[GenerateValibotSchema]
public sealed class FullValidationModel
{
	[Required]
	[MaxLength(50)]
	[DisplayName("Full Name")]
	public string Name { get; set; } = string.Empty;

	[StringLength(20, MinimumLength = 3)]
	public string? Nickname { get; set; }

	[Range(1, 100)]
	public int Age { get; set; }

	[RegularExpression("^[A-Z]+$")]
	public string Code { get; set; } = string.Empty;

	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Url]
	public string? Website { get; set; }

	[Phone]
	public string? PhoneNumber { get; set; }

	[CreditCard]
	public string? CardNumber { get; set; }

	[DenyCharacters("<>")]
	public string SafeText { get; set; } = string.Empty;

	[DenyRegularExpression("bad")]
	public string NoBadWord { get; set; } = string.Empty;

	[AllowedNullableValues("A", "B", "C")]
	public string? Choice { get; set; }

	[ListMaxLength(10, ErrorMessage = "Too many tags")]
	public List<string> Tags { get; set; } = [];

	[ListStringLength(15, MinimumLength = 2)]
	public List<string> Codes { get; set; } = [];

	[ListRange(Minimum = 0, Maximum = 10)]
	public List<int> Scores { get; set; } = [];

	[ListRegularExpression("^[0-9]+$")]
	public List<string> Numeric { get; set; } = [];

	[ListDenyCharacters("!@#")]
	public List<string> Clean { get; set; } = [];

	[ListDenyRegularExpression("bad")]
	public List<string> GoodWords { get; set; } = [];

	public Guid ExternalId { get; set; }

	public DateTime CreatedOn { get; set; }

	public DateOnly BirthDate { get; set; }

	public ValibotColor Status { get; set; }

	public NestedValibotModel Nested { get; set; } = new();

	[TsIgnore]
	public string Ignored { get; set; } = string.Empty;

	public int? OptionalNumber { get; set; }

	public int RequiredNumber { get; set; }

	[MinLength(3)]
	public string MinLenText { get; set; } = string.Empty;

	[Required]
	public string? RequiredNullableString { get; set; }

	[DenyCharacters("]^-")]
	public string StrictText { get; set; } = string.Empty;

	[AllowedNullableValues]
	public string? EmptyChoice { get; set; }

	// No item-level attribute: exercises GetBaseSchema/GetArrayItemType's array branch directly.
	public int[] NumbersArray { get; set; } = [];

	// No item-level attribute: exercises the IList<>/IEnumerable<>/ICollection<> switch arms
	// (as opposed to the concrete List<> used by Tags/Codes/etc. above) in both GetBaseSchema and GetArrayItemType.
	public IList<string> IListProp { get; set; } = [];

	public IEnumerable<int> IEnumerableProp { get; set; } = [];

	public ICollection<string> ICollectionProp { get; set; } = [];

	public double Price { get; set; }

	public bool Flag { get; set; }

	// System-namespaced class type: falls through every specific case in GetBaseSchema to the
	// final "v.any()" fallback (and exercises the false side of the complex-domain-type check).
	public object? Misc { get; set; }

	// Non-string values: exercises the "x is string" ternary's false branch in GetAllowedValuesPipe
	// (Choice above only ever supplies string values).
	[AllowedNullableValues(1, 2, 3)]
	public int? NumericChoice { get; set; }
}

public sealed class SourceModel
{
	[Required]
	[MaxLength(5)]
	public string Code { get; set; } = string.Empty;
}

[SubsetOf(typeof(SourceModel))]
public sealed class BoundSourceModel
{
	public string Code { get; set; } = string.Empty;
}
