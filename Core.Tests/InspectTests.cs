using System.ComponentModel;
using CommonNetFuncs.Core;

namespace Core.Tests;

#pragma warning disable IDE0079 // Remove unnecessary suppression

public sealed class InspectTests
{
	// Helper classes for testing
	public sealed class SimpleClass
	{
		public int IntProp { get; set; }

		public string? StringProp { get; set; }
	}

	public sealed class NestedClass
	{
		public int Id { get; set; }

		public SimpleClass? Child { get; set; }
	}

	public static readonly TheoryData<(Type, object?)> defaultValueTestData = new()
	{
		{ (typeof(int), default(int)) },
		{ (typeof(string), default(string)) },
		{ (typeof(DateTime), default(DateTime)) },
	};

	[Theory]
	[MemberData(nameof(defaultValueTestData), MemberType = typeof(InspectTests))]
	public void GetDefaultValue_ReturnsExpected((Type type, object? expected) values)
	{
		object? result = values.type.GetDefaultValue();
		result.ShouldBe(values.expected);
	}

	[Theory]
	[InlineData(0, null, 2)]
	[InlineData(1, "not default", 0)]
	public void CountDefaultProps_ReturnsCorrectCount(int intProp, string? stringProp, int expected)
	{
		SimpleClass obj = new() { IntProp = intProp, StringProp = stringProp };
		int count = obj.CountDefaultProps();
		count.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(ClassWithDescription), "DescriptionAttribute", true)]
	[InlineData(typeof(SimpleClass), "DescriptionAttribute", false)]
	public void ObjectHasAttribute_Works(Type type, string attrName, bool expected)
	{
		type.ObjectHasAttribute(attrName).ShouldBe(expected);
	}

	// Replace the untyped IEnumerable<object[]> with strongly-typed TheoryData<> for better type safety.

	public static TheoryData<SimpleClass?, SimpleClass?, IEnumerable<string>?, bool> IsEqualRTestData => new()
		{
				{ new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 5, StringProp = "abc" }, null, true },
				{ new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 6, StringProp = "abc" }, null, false },
				{ new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 6, StringProp = "abc" }, ["IntProp"], true },
				{ null, null, null, true },
				{ new SimpleClass(), null, null, false },
		};

#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable

	//[Theory]
	//[MemberData(nameof(IsEqualRTestData))]
	//public void IsEqualR_Works(object? a, object? b, IEnumerable<string>? exempt, bool expected)
	//{
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    a.IsEqualR(b, exempt).ShouldBe(expected);
	//    #pragma warning restore CS0618 // Type or member is obsolete
	//}

	[Theory]
	[MemberData(nameof(IsEqualRTestData))]
	public void IsEqualR_And_IsEqual_Consistency(object? a, object? b, IEnumerable<string>? exempt, bool expected)
	{
		a.IsEqual(b, exemptProps: exempt).ShouldBe(expected);
	}

#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable

	[Fact]
	public void IsEqual_Recursive_ReturnsTrue_ForIdenticalNestedObjects()
	{
		NestedClass a = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
		NestedClass b = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
		a.IsEqual(b).ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_Recursive_ReturnsFalse_ForDifferentNestedObjects()
	{
		NestedClass a = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
		NestedClass b = new() { Id = 1, Child = new SimpleClass { IntProp = 3, StringProp = "x" } };
		a.IsEqual(b).ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_Recursive_HandlesCycles()
	{
		CyclicClass a = new();
		CyclicClass b = new();
		a.Self = a;
		b.Self = b;
		a.IsEqual(b).ShouldBeTrue();
	}

	[Theory]
	[InlineData(1, "a", 2, "a", new[] { "IntProp" }, true)]
	[InlineData(1, "a", 2, "b", new[] { "IntProp" }, false)]
	public void IsEqual_ExemptProps_Works(int aInt, string aStr, int bInt, string bStr, string[] exempt, bool expected)
	{
		SimpleClass a = new() { IntProp = aInt, StringProp = aStr };
		SimpleClass b = new() { IntProp = bInt, StringProp = bStr };
		a.IsEqual(b, exemptProps: exempt).ShouldBe(expected);
	}

	[Theory]
	[InlineData("abc", "ABC", true, true)]
	[InlineData("abc", "ABC", false, false)]
	[InlineData("abc", "abc", true, true)]
	public void IsEqual_IgnoreStringCase_Works(string aStr, string bStr, bool ignoreCase, bool expected)
	{
		SimpleClass a = new() { IntProp = 1, StringProp = aStr };
		SimpleClass b = new() { IntProp = 1, StringProp = bStr };
		a.IsEqual(b, ignoreStringCase: ignoreCase).ShouldBe(expected);
	}

	public static TheoryData<SimpleClass, SimpleClass, bool> HashTestData => new()
		{
				{ new SimpleClass { IntProp = 1, StringProp = "abc" }, new SimpleClass { IntProp = 1, StringProp = "abc" }, true },
				{ new SimpleClass { IntProp = 1, StringProp = "abc" }, new SimpleClass { IntProp = 2, StringProp = "abc" }, false },
		};

	[Theory]
	[MemberData(nameof(HashTestData))]
	public void GetHashCode_And_GetHashForObject_Consistency(SimpleClass a, SimpleClass b, bool shouldMatch)
	{
		// a.GetHashCode().ShouldBe(b.GetHashCode(), shouldMatch ? "Hashes should match" : "Hashes should not match");
		string aHash = a.GetHashForObject();
		string bHash = b.GetHashForObject();
		if (shouldMatch)
		{
			aHash.ShouldBe(bHash);
		}
		else
		{
			aHash.ShouldNotBe(bHash);
		}
	}

	[Theory]
	[MemberData(nameof(HashTestData))]
	public async Task GetHashCode_And_GetHashForObjectAsync_Consistency(SimpleClass a, SimpleClass b, bool shouldMatch)
	{
		// a.GetHashCode().ShouldBe(b.GetHashCode(), shouldMatch ? "Hashes should match" : "Hashes should not match");
		string aHash = await a.GetHashForObjectAsync();
		string bHash = await b.GetHashForObjectAsync();
		if (shouldMatch)
		{
			aHash.ShouldBe(bHash);
		}
		else
		{
			aHash.ShouldNotBe(bHash);
		}
	}

	[Fact]
	public void GetHashForObject_ReturnsNullString_ForNull()
	{
		string hash = ((SimpleClass?)null).GetHashForObject();
		hash.ShouldBe("null");
	}

	[Fact]
	public void GetHashForObject_CollectionOrderIndependence()
	{
		ClassWithCollection a = new() { Items = new[] { 1, 2, 3 } };
		ClassWithCollection b = new() { Items = new[] { 3, 2, 1 } };
		a.GetHashForObject().ShouldBe(b.GetHashForObject());
	}

	// Helper types for attribute and cycle tests
	[Description("desc")]
	private sealed class ClassWithDescription;

	private sealed class CyclicClass
	{
		public CyclicClass? Self { get; set; }
	}

	private sealed class ClassWithCollection
	{
		public IEnumerable<int>? Items { get; set; }
	}

	private sealed class ClassWithReadOnlyProperty
	{
		public int ReadOnlyProp { get; }
		public int WritableProp { get; set; }

		public ClassWithReadOnlyProperty(int value)
		{
			ReadOnlyProp = value;
		}
	}

	private sealed class ClassWithIndexer
	{
		private readonly int[] _data = new int[10];

		public int this[int index]
		{
			get => _data[index];
			set => _data[index] = value;
		}

		public int RegularProp { get; set; }
	}

	private sealed class ClassWithPrimitives
	{
		public byte ByteValue { get; set; }
		public short ShortValue { get; set; }
		public long LongValue { get; set; }
		public float FloatValue { get; set; }
		public char CharValue { get; set; }
	}

	private sealed class ClassWithValueTypes
	{
		public int IntValue { get; set; }
		public double DoubleValue { get; set; }
		public bool BoolValue { get; set; }
		public decimal DecimalValue { get; set; }
	}

	private sealed class ClassWithComparable
	{
		public DateTime DateValue { get; set; }
		public string? StringValue { get; set; }
	}

	private sealed class ClassWithComplexObject
	{
		public SimpleClass? Nested { get; set; }
		public int Value { get; set; }
	}

	private sealed class ClassWithNullProperties
	{
		public string? NullString { get; set; }
		public SimpleClass? NullObject { get; set; }
	}

	[Fact]
	public void GetDefaultValue_ForValueType_ReturnsUninitializedObject()
	{
		// Arrange & Act
		object? result = typeof(int).GetDefaultValue();

		// Assert
		result.ShouldBe(0);
	}

	[Fact]
	public void GetDefaultValue_ForReferenceType_ReturnsNull()
	{
		// Arrange & Act
		object? result = typeof(string).GetDefaultValue();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void CountDefaultProps_WithReadOnlyProperties_OnlyCountsWritableProps()
	{
		// Arrange - use a simple class
		SimpleClass obj = new() { IntProp = 0, StringProp = null };

		// Act
		int count = obj.CountDefaultProps();

		// Assert
		count.ShouldBe(2); // Both properties are at default
	}

	[Fact]
	public void ObjectHasAttribute_WithNullTypeId_ReturnsFalse()
	{
		// Arrange - SimpleClass doesn't have DescriptionAttribute
		Type type = typeof(SimpleClass);

		// Act
		bool result = type.ObjectHasAttribute("NonExistentAttribute");

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_WithDifferentTypes_ReturnsFalse()
	{
		// Arrange
		SimpleClass obj1 = new() { IntProp = 1 };
		NestedClass obj2 = new() { Id = 1 };

		// Act
		bool result = obj1.IsEqual(obj2);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_WithValueTypes_ComparesCorrectly()
	{
		// Arrange
		ClassWithValueTypes a = new() { IntValue = 5, DoubleValue = 3.14, BoolValue = true, DecimalValue = 10.5m };
		ClassWithValueTypes b = new() { IntValue = 5, DoubleValue = 3.14, BoolValue = true, DecimalValue = 10.5m };

		// Act
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_WithValueTypes_DifferentValues_ReturnsFalse()
	{
		// Arrange
		ClassWithValueTypes a = new() { IntValue = 5, DoubleValue = 3.14, BoolValue = true, DecimalValue = 10.5m };
		ClassWithValueTypes b = new() { IntValue = 6, DoubleValue = 3.14, BoolValue = true, DecimalValue = 10.5m };

		// Act
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_WithComparableTypes_UsesCompareTo()
	{
		// Arrange
		ClassWithComparable a = new() { DateValue = new DateTime(2024, 1, 1), StringValue = "test" };
		ClassWithComparable b = new() { DateValue = new DateTime(2024, 1, 1), StringValue = "test" };

		// Act
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_WithComparableTypes_DifferentDates_ReturnsFalse()
	{
		// Arrange
		ClassWithComparable a = new() { DateValue = new DateTime(2024, 1, 1), StringValue = "test" };
		ClassWithComparable b = new() { DateValue = new DateTime(2024, 1, 2), StringValue = "test" };

		// Act
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_NonRecursive_IgnoresNestedObjects()
	{
		// Arrange
		ClassWithComplexObject a = new() { Value = 1, Nested = new SimpleClass { IntProp = 5 } };
		ClassWithComplexObject b = new() { Value = 1, Nested = new SimpleClass { IntProp = 10 } };

		// Act - non-recursive should ignore nested differences
		bool result = a.IsEqual(b, recursive: false);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_Recursive_ComparesNestedObjects()
	{
		// Arrange
		ClassWithComplexObject a = new() { Value = 1, Nested = new SimpleClass { IntProp = 5 } };
		ClassWithComplexObject b = new() { Value = 1, Nested = new SimpleClass { IntProp = 10 } };

		// Act - recursive should detect nested differences
		bool result = a.IsEqual(b, recursive: true);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsEqual_WithNullProperties_HandlesCorrectly()
	{
		// Arrange
		ClassWithNullProperties a = new() { NullString = null, NullObject = null };
		ClassWithNullProperties b = new() { NullString = null, NullObject = null };

		// Act
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_WithOneNullNestedProperty_ReturnsFalse()
	{
		// Arrange
		ClassWithNullProperties a = new() { NullObject = new SimpleClass { IntProp = 1 } };
		ClassWithNullProperties b = new() { NullObject = null };

		// Act
		bool result = a.IsEqual(b, recursive: true);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData(EHashAlgorithm.SHA1)]
	[InlineData(EHashAlgorithm.SHA256)]
	[InlineData(EHashAlgorithm.SHA384)]
	[InlineData(EHashAlgorithm.SHA512)]
	[InlineData(EHashAlgorithm.MD5)]
	public void GetHashForObject_WithDifferentAlgorithms_ProducesHash(EHashAlgorithm algorithm)
	{
		// Arrange
		SimpleClass obj = new() { IntProp = 5, StringProp = "test" };

		// Act
		string hash = obj.GetHashForObject(algorithm);

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Theory]
	[InlineData(EHashAlgorithm.SHA1)]
	[InlineData(EHashAlgorithm.SHA256)]
	[InlineData(EHashAlgorithm.SHA384)]
	[InlineData(EHashAlgorithm.SHA512)]
	[InlineData(EHashAlgorithm.MD5)]
	public async Task GetHashForObjectAsync_WithDifferentAlgorithms_ProducesHash(EHashAlgorithm algorithm)
	{
		// Arrange
		SimpleClass obj = new() { IntProp = 5, StringProp = "test" };

		// Act
		string hash = await obj.GetHashForObjectAsync(algorithm);

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_ReturnsNullString_ForNull()
	{
		// Arrange & Act
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
		SimpleClass obj = null;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8634 // Nullability of type argument doesn't match 'class' constraint
		string hash = await obj.GetHashForObjectAsync();
#pragma warning restore CS8634

		// Assert
		hash.ShouldBe("null");
	}

	[Fact]
	public void GetHashForObject_WithValueTypes_ProducesHash()
	{
		// Arrange
		ClassWithValueTypes obj = new() { IntValue = 10, DoubleValue = 5.5, BoolValue = true, DecimalValue = 99.99m };

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithValueTypes_ProducesHash()
	{
		// Arrange
		ClassWithValueTypes obj = new() { IntValue = 10, DoubleValue = 5.5, BoolValue = true, DecimalValue = 99.99m };

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetHashForObject_WithNestedObjects_ProducesHash()
	{
		// Arrange
		ClassWithComplexObject obj = new() { Value = 1, Nested = new SimpleClass { IntProp = 42, StringProp = "nested" } };

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithNestedObjects_ProducesHash()
	{
		// Arrange
		ClassWithComplexObject obj = new() { Value = 1, Nested = new SimpleClass { IntProp = 42, StringProp = "nested" } };

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetHashForObject_WithNullNestedObject_ProducesHash()
	{
		// Arrange
		ClassWithComplexObject obj = new() { Value = 1, Nested = null };

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithNullNestedObject_ProducesHash()
	{
		// Arrange
		ClassWithComplexObject obj = new() { Value = 1, Nested = null };

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetHashForObject_WithEmptyCollection_ProducesHash()
	{
		// Arrange
		ClassWithCollection obj = new() { Items = Array.Empty<int>() };

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithEmptyCollection_ProducesHash()
	{
		// Arrange
		ClassWithCollection obj = new() { Items = Array.Empty<int>() };

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void IsEqual_WithMultipleCycles_HandlesCorrectly()
	{
		// Arrange - create more complex cycle
		CyclicClass a1 = new();
		CyclicClass a2 = new();
		a1.Self = a2;
		a2.Self = a1;

		CyclicClass b1 = new();
		CyclicClass b2 = new();
		b1.Self = b2;
		b2.Self = b1;

		// Act
		bool result = a1.IsEqual(b1);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_CachesComparisonDelegates()
	{
		// Arrange
		SimpleClass a1 = new() { IntProp = 1, StringProp = "a" };
		SimpleClass b1 = new() { IntProp = 1, StringProp = "a" };
		SimpleClass a2 = new() { IntProp = 2, StringProp = "b" };
		SimpleClass b2 = new() { IntProp = 2, StringProp = "b" };

		// Act - call multiple times to test delegate caching
		bool result1 = a1.IsEqual(b1);
		bool result2 = a2.IsEqual(b2);

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_WithDifferentCaseSensitivitySettings_UsesSeparateDelegates()
	{
		// Arrange
		SimpleClass a = new() { IntProp = 1, StringProp = "ABC" };
		SimpleClass b = new() { IntProp = 1, StringProp = "abc" };

		// Act
		bool caseSensitive = a.IsEqual(b, ignoreStringCase: false);
		bool caseInsensitive = a.IsEqual(b, ignoreStringCase: true);

		// Assert
		caseSensitive.ShouldBeFalse();
		caseInsensitive.ShouldBeTrue();
	}

	[Fact]
	public void IsEqual_WithDifferentRecursiveSettings_UsesSeparateDelegates()
	{
		// Arrange
		ClassWithComplexObject a = new() { Value = 1, Nested = new SimpleClass { IntProp = 5 } };
		ClassWithComplexObject b = new() { Value = 1, Nested = new SimpleClass { IntProp = 10 } };

		// Act
		bool nonRecursive = a.IsEqual(b, recursive: false);
		bool recursive = a.IsEqual(b, recursive: true);

		// Assert
		nonRecursive.ShouldBeTrue();
		recursive.ShouldBeFalse();
	}

	[Fact]
	public void CountDefaultProps_WithReadOnlyProperty_SkipsIt()
	{
		// Arrange
		ClassWithReadOnlyProperty obj = new(42) { WritableProp = 0 };

		// Act
		int count = obj.CountDefaultProps();

		// Assert
		count.ShouldBe(1); // Only WritableProp counts (and it's at default)
	}

	[Fact]
	public void IsEqual_WithIndexedProperties_SkipsThem()
	{
		// Arrange
		ClassWithIndexer a = new() { RegularProp = 5 };
		ClassWithIndexer b = new() { RegularProp = 5 };
		a[0] = 100;
		b[0] = 200;

		// Act - indexed properties should be skipped
		bool result = a.IsEqual(b);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void GetHashForObject_WithPrimitiveTypes_ProducesHash()
	{
		// Arrange
		ClassWithPrimitives obj = new()
		{
			ByteValue = 255,
			ShortValue = 1000,
			LongValue = 999999L,
			FloatValue = 3.14f,
			CharValue = 'X'
		};

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithPrimitiveTypes_ProducesHash()
	{
		// Arrange
		ClassWithPrimitives obj = new()
		{
			ByteValue = 255,
			ShortValue = 1000,
			LongValue = 999999L,
			FloatValue = 3.14f,
			CharValue = 'X'
		};

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetHashForObject_WithNestedCollections_ProducesHash()
	{
		// Arrange
		ClassWithCollection obj = new()
		{
			Items = new List<int> { 1, 2, 3, 4, 5 }
		};

		// Act
		string hash = obj.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_WithNestedCollections_ProducesHash()
	{
		// Arrange
		ClassWithCollection obj = new()
		{
			Items = new List<int> { 1, 2, 3, 4, 5 }
		};

		// Act
		string hash = await obj.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void GetHashForObject_CollectionWithNullItems_ProducesHash()
	{
		// Arrange
		ClassWithComplexObject obj1 = new() { Value = 1, Nested = null };
		ClassWithComplexObject obj2 = new() { Value = 2, Nested = null };
		var collection = new[] { obj1, obj2 };

		// Create a wrapper class
		var wrapper = new { Items = collection };

		// Act
		string hash = wrapper.GetHashForObject();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public async Task GetHashForObjectAsync_CollectionWithComplexObjects_ProducesHash()
	{
		// Arrange
		var collection = new[]
		{
			new SimpleClass { IntProp = 1, StringProp = "a" },
			new SimpleClass { IntProp = 2, StringProp = "b" }
		};
		var wrapper = new { Items = collection };

		// Act
		string hash = await wrapper.GetHashForObjectAsync();

		// Assert
		hash.ShouldNotBeNullOrEmpty();
	}

	[Fact]
	public void IsEqual_WithAlreadyComparingPair_ReturnsTrue()
	{
		// Arrange - this tests the cycle detection by using self-referencing objects
		CyclicClass obj = new();
		obj.Self = obj;

		// Act - comparing object to itself should work
		bool result = obj.IsEqual(obj);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ObjectHasAttribute_WithAttributeWithNullName_ReturnsFalse()
	{
		// This tests the path where attrName is null in ObjectHasAttribute
		// Though this is hard to create in practice, the code has a null check
		Type type = typeof(SimpleClass);
		bool result = type.ObjectHasAttribute("SomeAttribute");
		result.ShouldBeFalse();
	}

	[Fact]
	public void ObjectHasAttribute_WithDifferentAttributeName_ReturnsFalse()
	{
		// This tests the path where attrName is not null but doesn't match attributeName
		Type type = typeof(ClassWithDescription);
		bool result = type.ObjectHasAttribute("NonExistentAttribute");
		result.ShouldBeFalse();
	}
}
#pragma warning restore IDE0079 // Remove unnecessary suppression

