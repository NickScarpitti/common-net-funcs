using CommonNetFuncs.ReinforcedTypings.Constants;
using Reinforced.Typings.Attributes;

namespace ReinforcedTypings.Tests.TestModels.Consts;

public enum SimpleColor
{
	Red = 0,
	Green = 1,
	Blue = 2
}

[TsEnum(UseString = true)]
public enum StringEnum
{
	Alpha,
	Beta,
	Gamma
}

public sealed class NestedPoco
{
	public string Label { get; set; } = string.Empty;

	public int Value { get; set; }

	[TsIgnore]
	public string Secret { get; set; } = "hidden";
}

[TsConst]
public static class BasicConstsAll
{
	public const string Title = "Hello 'World'";

	public static readonly int Count = 42;

	[TsIgnoreConst]
	public const string Internal = "nope";
}

[TsConst(TsConstExportMode.Selected)]
public static class BasicConstsSelected
{
	public const string NotExported = "skip";

	[TsExportConst]
	public const string Exported = "keep";
}

[TsConst]
public static class RichTypeConsts
{
	public static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

	public static readonly DateTime CreatedAt = new(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

	public static readonly DateOnly Day = new(2024, 1, 2);

	public static readonly TimeOnly Time = new(3, 4, 5);

	public static readonly bool Flag = true;

	public static readonly double Ratio = 3.14;

	public static readonly SimpleColor Color = SimpleColor.Green;

	public static readonly StringEnum Mode = StringEnum.Beta;

	public static readonly string? Nothing = null;

	public static readonly int[] Numbers = [1, 2, 3];

	public static readonly List<string> Tags = ["a", "b"];

	public static readonly NestedPoco Poco = new() { Label = "x", Value = 7 };
}

[TsConst]
public static class TimeSpanConst
{
	public static readonly TimeSpan Duration = TimeSpan.FromMinutes(90);
}

[TsConst]
public static class EmptyConsts
{
	[TsIgnoreConst]
	public const string OnlyIgnored = "nope";
}

[TsConst(TsConstExportMode.Selected)]
public static class CamelCaseConsts
{
	[TsExportConst]
	public const string PascalCaseFieldName = "value";
}
