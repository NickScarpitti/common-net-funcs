using CommonNetFuncs.ReinforcedTypings.Collections;
using CommonNetFuncs.ReinforcedTypings.Constants;
using CommonNetFuncs.ReinforcedTypings.Valibot;

namespace ReinforcedTypings.Tests;

public sealed class AttributeTests
{
	[Fact]
	public void TsConstAttribute_DefaultsToAllMode()
	{
		TsConstAttribute attr = new();

		attr.Mode.ShouldBe(TsConstExportMode.All);
	}

	[Fact]
	public void TsConstAttribute_CanBeConstructedWithSelectedMode()
	{
		TsConstAttribute attr = new(TsConstExportMode.Selected);

		attr.Mode.ShouldBe(TsConstExportMode.Selected);
	}

	[Fact]
	public void TsConstAttribute_IsRestrictedToClassesAndNotInherited()
	{
		AttributeUsageAttribute? usage = typeof(TsConstAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Class);
		usage.Inherited.ShouldBeFalse();
	}

	[Fact]
	public void TsCollectionAttribute_DefaultsToAllMode()
	{
		TsCollectionAttribute attr = new();

		attr.Mode.ShouldBe(TsCollectionExportMode.All);
	}

	[Fact]
	public void TsCollectionAttribute_CanBeConstructedWithSelectedMode()
	{
		TsCollectionAttribute attr = new(TsCollectionExportMode.Selected);

		attr.Mode.ShouldBe(TsCollectionExportMode.Selected);
	}

	[Fact]
	public void TsExportConstAttribute_TargetsFieldsOnly()
	{
		AttributeUsageAttribute? usage = typeof(TsExportConstAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Field);
	}

	[Fact]
	public void TsIgnoreConstAttribute_TargetsFieldsOnly()
	{
		AttributeUsageAttribute? usage = typeof(TsIgnoreConstAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Field);
	}

	[Fact]
	public void TsExportCollectionAttribute_TargetsFieldsOnly()
	{
		AttributeUsageAttribute? usage = typeof(TsExportCollectionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Field);
	}

	[Fact]
	public void TsIgnoreCollectionAttribute_TargetsFieldsOnly()
	{
		AttributeUsageAttribute? usage = typeof(TsIgnoreCollectionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Field);
	}

	[Fact]
	public void GenerateValibotSchemaAttribute_TargetsClassesAndStructsOnly_AndDisallowsMultiple()
	{
		AttributeUsageAttribute? usage = typeof(GenerateValibotSchemaAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
			.Cast<AttributeUsageAttribute>()
			.SingleOrDefault();

		usage.ShouldNotBeNull();
		usage.ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Struct);
		usage.AllowMultiple.ShouldBeFalse();
		usage.Inherited.ShouldBeFalse();
	}
}
