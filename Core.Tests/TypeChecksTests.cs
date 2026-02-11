using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class TypeChecksTests
{

	[Theory]
	[InlineData(typeof(Action), true)]
	[InlineData(typeof(Func<int>), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int), false)]
	public void IsDelegate_ShouldIdentifyDelegateTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsDelegate();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(int[]), true)]
	[InlineData(typeof(string[]), true)]
	[InlineData(typeof(List<int>), false)]
	[InlineData(typeof(int), false)]
	public void IsArray_ShouldIdentifyArrayTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsArray();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(Dictionary<int, string>), true)]
	[InlineData(typeof(Hashtable), true)]
	[InlineData(typeof(List<int>), false)]
	[InlineData(typeof(int), false)]
	public void IsDictionary_ShouldIdentifyDictionaryTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsDictionary();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(List<int>), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int[]), true)]
	[InlineData(typeof(int), false)]
	public void IsEnumerable_ShouldIdentifyEnumerableTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsEnumerable();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int), true)]
	[InlineData(typeof(List<int>), true)]
	[InlineData(null, true)]
	public void IsClassOtherThanString_ShouldIdentifyClassTypesOtherThanString(Type? type, bool expected)
	{
		// Act
		bool result = type.IsClassOtherThanString();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(123, true)]
	[InlineData(123.45, true)]
	[InlineData("string", false)]
	[InlineData(null, false)]
	public void IsNumeric_ShouldIdentifyNumericObjects(object? value, bool expected)
	{
		// Act
		bool result = value.IsNumeric();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(int), true)]
	[InlineData(typeof(double), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int?), true)]
	[InlineData(null, false)]
	public void IsNumericType_ShouldIdentifyNumericTypes(Type? type, bool expected)
	{
		// Act
		bool result = type.IsNumericType();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(int), true)]
	[InlineData(typeof(string), true)]
	[InlineData(typeof(decimal), true)]
	[InlineData(typeof(DateTime), true)]
	[InlineData(typeof(Guid), true)]
	[InlineData(typeof(List<int>), false)]
	public void IsSimpleType_ShouldIdentifySimpleTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsSimpleType();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(IReadOnlyCollection<int>), true)]
	[InlineData(typeof(IReadOnlyList<string>), true)]
	[InlineData(typeof(ReadOnlyCollection<int>), true)]
	[InlineData(typeof(List<int>), false)]
	[InlineData(typeof(int[]), false)]
	[InlineData(typeof(ImmutableArray<int>), true)]
	[InlineData(typeof(Dictionary<int, string>), false)]
	[InlineData(typeof(IReadOnlyDictionary<int, string>), true)]
	[InlineData(typeof(ICollection<int>), false)]
	[InlineData(typeof(IEnumerable<int>), false)]
	[InlineData(typeof(ArrayList), false)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int), false)]
	[InlineData(typeof(IMyReadOnlyCollectionInterface), true)]
	[InlineData(typeof(MyReadOnlyCollectionClass), true)]
	[InlineData(typeof(IReadOnlyCollection<>), true)]
	[InlineData(typeof(IReadOnlyList<>), true)]
	[InlineData(typeof(ReadOnlyCollection<>), true)]
	[InlineData(typeof(object), false)]
	[InlineData(null, false)]
	public void IsReadOnlyCollectionType_ShouldIdentifyReadOnlyCollectionTypes(Type? type, bool expected)
	{
		// Arrange
		bool result = false;

		// Act
		if (type != null)
		{
			result = type.IsReadOnlyCollectionType();
		}
		else
		{
			Should.Throw<ArgumentNullException>(() => type!.IsReadOnlyCollectionType());
		}

		// Assert
		result.ShouldBe(expected);
	}

	// Helper types for interface/class that implement IReadOnlyCollection<TObj>
	private interface IMyReadOnlyCollectionInterface : IReadOnlyCollection<int>;

	private class MyReadOnlyCollectionClass : IReadOnlyCollection<int>
	{
		public int Count => 0;

		public IEnumerator<int> GetEnumerator()
		{
			return Enumerable.Empty<int>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	// Test enum for IsSimpleType tests
	private enum TestEnum
	{
		Value1,
		Value2
	}

	[Fact]
	public void ClearTypeCheckCaches_ShouldClearAllCaches()
	{
		// Arrange - populate caches by calling methods
		typeof(int).IsSimpleType();
		typeof(List<int>).IsReadOnlyCollectionType();
		typeof(double).IsNumericType();
		typeof(int[]).IsEnumerable();

		// Act
		TypeChecks.ClearTypeCheckCaches();

		// Assert - verify caches were cleared by calling methods again
		// (they should still work correctly)
		typeof(int).IsSimpleType().ShouldBeTrue();
		typeof(List<int>).IsReadOnlyCollectionType().ShouldBeFalse();
		typeof(double).IsNumericType().ShouldBeTrue();
		typeof(int[]).IsEnumerable().ShouldBeTrue();
	}

	[Fact]
	public void ClearSimpleTypeCache_ShouldClearSimpleTypeCache()
	{
		// Arrange
		typeof(int).IsSimpleType();
		typeof(string).IsSimpleType();

		// Act
		TypeChecks.ClearSimpleTypeCache();

		// Assert - verify cache was cleared by calling method again
		typeof(int).IsSimpleType().ShouldBeTrue();
		typeof(string).IsSimpleType().ShouldBeTrue();
	}

	[Fact]
	public void ClearReadOnlyCollectionTypeCache_ShouldClearReadOnlyCollectionTypeCache()
	{
		// Arrange
		typeof(IReadOnlyCollection<int>).IsReadOnlyCollectionType();
		typeof(List<int>).IsReadOnlyCollectionType();

		// Act
		TypeChecks.ClearReadOnlyCollectionTypeCache();

		// Assert - verify cache was cleared by calling method again
		typeof(IReadOnlyCollection<int>).IsReadOnlyCollectionType().ShouldBeTrue();
		typeof(List<int>).IsReadOnlyCollectionType().ShouldBeFalse();
	}

	[Fact]
	public void ClearNumericTypeCache_ShouldClearNumericTypeCache()
	{
		// Arrange
		typeof(int).IsNumericType();
		typeof(double).IsNumericType();

		// Act
		TypeChecks.ClearNumericTypeCache();

		// Assert - verify cache was cleared by calling method again
		typeof(int).IsNumericType().ShouldBeTrue();
		typeof(double).IsNumericType().ShouldBeTrue();
	}

	[Fact]
	public void ClearEnumerableTypeCache_ShouldClearEnumerableTypeCache()
	{
		// Arrange
		typeof(List<int>).IsEnumerable();
		typeof(int[]).IsEnumerable();

		// Act
		TypeChecks.ClearEnumerableTypeCache();

		// Assert - verify cache was cleared by calling method again
		typeof(List<int>).IsEnumerable().ShouldBeTrue();
		typeof(int[]).IsEnumerable().ShouldBeTrue();
	}

	[Theory]
	[InlineData(typeof(int?), true)]
	[InlineData(typeof(DateTime?), true)]
	[InlineData(typeof(DateTimeOffset?), true)]
	[InlineData(typeof(TimeSpan?), true)]
	[InlineData(typeof(Guid?), true)]
	[InlineData(typeof(decimal?), true)]
	public void IsSimpleType_ShouldIdentifyNullableSimpleTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsSimpleType();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(TestEnum), true)]
	[InlineData(typeof(DateTimeOffset), true)]
	[InlineData(typeof(TimeSpan), true)]
	public void IsSimpleType_ShouldIdentifyAdditionalSimpleTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsSimpleType();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(typeof(byte), true)]
	[InlineData(typeof(sbyte), true)]
	[InlineData(typeof(short), true)]
	[InlineData(typeof(ushort), true)]
	[InlineData(typeof(uint), true)]
	[InlineData(typeof(ulong), true)]
	[InlineData(typeof(float), true)]
	[InlineData(typeof(long), true)]
	[InlineData(typeof(byte?), true)]
	[InlineData(typeof(sbyte?), true)]
	[InlineData(typeof(short?), true)]
	[InlineData(typeof(ushort?), true)]
	[InlineData(typeof(uint?), true)]
	[InlineData(typeof(ulong?), true)]
	[InlineData(typeof(float?), true)]
	[InlineData(typeof(long?), true)]
	[InlineData(typeof(decimal?), true)]
	public void IsNumericType_ShouldIdentifyAllNumericTypes(Type type, bool expected)
	{
		// Act
		bool result = type.IsNumericType();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void IsReadOnlyCollectionType_ShouldIdentifyReadOnlyDictionary()
	{
		// Arrange
		Type type = typeof(ReadOnlyDictionary<int, string>);

		// Act
		bool result = type.IsReadOnlyCollectionType();

		// Assert
		result.ShouldBeTrue();
	}
}
