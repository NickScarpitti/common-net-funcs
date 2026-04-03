using System.Reflection;
using CommonNetFuncs.SubsetModelBinder;

namespace SubsetModelBinder.Attribute.Tests;

public class SubsetOfAttributeTests
{
	[Fact]
	public void Constructor_WithSourceTypeOnly_ShouldSetSourceTypeAndUseDefaults()
	{
		// Arrange
		Type sourceType = typeof(string);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBeFalse();
		attribute.AllowInheritedProperties.ShouldBeTrue();
		attribute.IgnoreType.ShouldBeFalse();
	}

	[Fact]
	public void Constructor_WithAllParameters_ShouldSetAllProperties()
	{
		// Arrange
		Type sourceType = typeof(int);
		const bool isMvcApp = true;
		const bool allowInheritedProperties = false;
		const bool ignoreType = true;

		// Act
		SubsetOfAttribute attribute = new(sourceType, isMvcApp, allowInheritedProperties, ignoreType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBe(isMvcApp);
		attribute.AllowInheritedProperties.ShouldBe(allowInheritedProperties);
		attribute.IgnoreType.ShouldBe(ignoreType);
	}

	[Fact]
	public void Constructor_WithIsMvcAppTrue_ShouldSetIsMvcAppProperty()
	{
		// Arrange
		Type sourceType = typeof(DateTime);

		// Act
		SubsetOfAttribute attribute = new(sourceType, isMvcApp: true);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBeTrue();
		attribute.AllowInheritedProperties.ShouldBeTrue();
		attribute.IgnoreType.ShouldBeFalse();
	}

	[Fact]
	public void Constructor_WithAllowInheritedPropertiesFalse_ShouldSetProperty()
	{
		// Arrange
		Type sourceType = typeof(decimal);

		// Act
		SubsetOfAttribute attribute = new(sourceType, allowInheritedProperties: false);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBeFalse();
		attribute.AllowInheritedProperties.ShouldBeFalse();
		attribute.IgnoreType.ShouldBeFalse();
	}

	[Fact]
	public void Constructor_WithIgnoreTypeTrue_ShouldSetProperty()
	{
		// Arrange
		Type sourceType = typeof(bool);

		// Act
		SubsetOfAttribute attribute = new(sourceType, ignoreType: true);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBeFalse();
		attribute.AllowInheritedProperties.ShouldBeTrue();
		attribute.IgnoreType.ShouldBeTrue();
	}

	[Fact]
	public void Constructor_WithMixedParameters_ShouldSetCorrectly()
	{
		// Arrange
		Type sourceType = typeof(List<string>);

		// Act
		SubsetOfAttribute attribute = new(sourceType, isMvcApp: true, allowInheritedProperties: false);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBeTrue();
		attribute.AllowInheritedProperties.ShouldBeFalse();
		attribute.IgnoreType.ShouldBeFalse();
	}

	[Fact]
	public void Constructor_WithGenericType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(Dictionary<string, int>);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Constructor_WithCustomClassType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(SubsetOfAttributeTests);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Properties_ShouldBeReadOnly()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act
		PropertyInfo sourceTypeProperty = attributeType.GetProperty(nameof(SubsetOfAttribute.SourceType))!;
		PropertyInfo isMvcAppProperty = attributeType.GetProperty(nameof(SubsetOfAttribute.IsMvcApp))!;
		PropertyInfo allowInheritedPropertiesProperty = attributeType.GetProperty(nameof(SubsetOfAttribute.AllowInheritedProperties))!;
		PropertyInfo ignoreTypeProperty = attributeType.GetProperty(nameof(SubsetOfAttribute.IgnoreType))!;

		// Assert
		sourceTypeProperty.CanWrite.ShouldBeFalse();
		isMvcAppProperty.CanWrite.ShouldBeFalse();
		allowInheritedPropertiesProperty.CanWrite.ShouldBeFalse();
		ignoreTypeProperty.CanWrite.ShouldBeFalse();
	}

	[Fact]
	public void AttributeUsage_ShouldTargetClassOnly()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act
		AttributeUsageAttribute? usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

		// Assert
		usageAttribute.ShouldNotBeNull();
		usageAttribute!.ValidOn.ShouldBe(AttributeTargets.Class);
	}

	[Fact]
	public void AttributeUsage_ShouldNotBeInherited()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act
		AttributeUsageAttribute? usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

		// Assert
		usageAttribute.ShouldNotBeNull();
		usageAttribute!.Inherited.ShouldBeFalse();
	}

	[Fact]
	public void AttributeUsage_ShouldNotAllowMultiple()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act
		AttributeUsageAttribute? usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

		// Assert
		usageAttribute.ShouldNotBeNull();
		usageAttribute!.AllowMultiple.ShouldBeFalse();
	}

	[Fact]
	public void Attribute_ShouldBeSealed()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act & Assert
		attributeType.IsSealed.ShouldBeTrue();
	}

	[Fact]
	public void Attribute_ShouldInheritFromAttribute()
	{
		// Arrange
		Type attributeType = typeof(SubsetOfAttribute);

		// Act & Assert
		attributeType.BaseType.ShouldBe(typeof(System.Attribute));
	}

	[Theory]
	[InlineData(true, true, true)]
	[InlineData(true, false, false)]
	[InlineData(false, true, false)]
	[InlineData(false, false, true)]
	public void Constructor_WithVariousBooleanCombinations_ShouldSetCorrectly(bool isMvcApp, bool allowInheritedProperties, bool ignoreType)
	{
		// Arrange
		Type sourceType = typeof(object);

		// Act
		SubsetOfAttribute attribute = new(sourceType, isMvcApp, allowInheritedProperties, ignoreType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
		attribute.IsMvcApp.ShouldBe(isMvcApp);
		attribute.AllowInheritedProperties.ShouldBe(allowInheritedProperties);
		attribute.IgnoreType.ShouldBe(ignoreType);
	}

	[Fact]
	public void Constructor_WithInterfaceType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(IDisposable);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Constructor_WithAbstractClassType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(Stream);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Constructor_WithNestedType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(Environment.SpecialFolder);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Constructor_WithArrayType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(string[]);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void Constructor_WithValueType_ShouldAcceptType()
	{
		// Arrange
		Type sourceType = typeof(Guid);

		// Act
		SubsetOfAttribute attribute = new(sourceType);

		// Assert
		attribute.SourceType.ShouldBe(sourceType);
	}

	[Fact]
	public void AttributeOnClass_ShouldBeRetrievable()
	{
		// Arrange & Act
		SubsetOfAttribute? attribute = typeof(TestClassWithAttribute).GetCustomAttribute<SubsetOfAttribute>();

		// Assert
		attribute.ShouldNotBeNull();
		attribute!.SourceType.ShouldBe(typeof(string));
		attribute.IsMvcApp.ShouldBeTrue();
		attribute.AllowInheritedProperties.ShouldBeFalse();
		attribute.IgnoreType.ShouldBeTrue();
	}

	[Fact]
	public void MultipleDefaultParametersCombinations_ShouldWorkCorrectly()
	{
		// Test various combinations of optional parameters being omitted

		// Only sourceType
		SubsetOfAttribute attr1 = new(typeof(int));
		attr1.IsMvcApp.ShouldBeFalse();
		attr1.AllowInheritedProperties.ShouldBeTrue();
		attr1.IgnoreType.ShouldBeFalse();

		// sourceType + isMvcApp
		SubsetOfAttribute attr2 = new(typeof(int), true);
		attr2.IsMvcApp.ShouldBeTrue();
		attr2.AllowInheritedProperties.ShouldBeTrue();
		attr2.IgnoreType.ShouldBeFalse();

		// sourceType + isMvcApp + allowInheritedProperties
		SubsetOfAttribute attr3 = new(typeof(int), true, false);
		attr3.IsMvcApp.ShouldBeTrue();
		attr3.AllowInheritedProperties.ShouldBeFalse();
		attr3.IgnoreType.ShouldBeFalse();

		// All parameters
		SubsetOfAttribute attr4 = new(typeof(int), true, false, true);
		attr4.IsMvcApp.ShouldBeTrue();
		attr4.AllowInheritedProperties.ShouldBeFalse();
		attr4.IgnoreType.ShouldBeTrue();
	}

	// Test class with attribute applied
	[SubsetOf(typeof(string), isMvcApp: true, allowInheritedProperties: false, ignoreType: true)]
	private class TestClassWithAttribute;
}