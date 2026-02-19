using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommonNetFuncs.Core;

namespace Core.Tests;

public enum CacheType
{
	All,
	SimpleType,
	ReadOnlyCollection,
	NumericType,
	Enumerable
}

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
	[InlineData(typeof(int?), true)]
	[InlineData(typeof(DateTime?), true)]
	[InlineData(typeof(DateTimeOffset?), true)]
	[InlineData(typeof(TimeSpan?), true)]
	[InlineData(typeof(Guid?), true)]
	[InlineData(typeof(decimal?), true)]
	[InlineData(typeof(ETestEnum), true)]
	[InlineData(typeof(DateTimeOffset), true)]
	[InlineData(typeof(TimeSpan), true)]
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
	private enum ETestEnum
	{
		Value1,
		Value2
	}

	[Theory]
	[InlineData(CacheType.All)]
	[InlineData(CacheType.SimpleType)]
	[InlineData(CacheType.ReadOnlyCollection)]
	[InlineData(CacheType.NumericType)]
	[InlineData(CacheType.Enumerable)]
	public void ClearTypeCheckCache_ShouldClearSpecifiedCache(CacheType cacheType)
	{
		// Arrange - populate caches by calling methods
		typeof(int).IsSimpleType();
		typeof(List<int>).IsReadOnlyCollectionType();
		typeof(double).IsNumericType();
		typeof(int[]).IsEnumerable();

		// Act
		switch (cacheType)
		{
			case CacheType.All:
				TypeChecks.ClearTypeCheckCaches();
				break;
			case CacheType.SimpleType:
				TypeChecks.ClearSimpleTypeCache();
				break;
			case CacheType.ReadOnlyCollection:
				TypeChecks.ClearReadOnlyCollectionTypeCache();
				break;
			case CacheType.NumericType:
				TypeChecks.ClearNumericTypeCache();
				break;
			case CacheType.Enumerable:
				TypeChecks.ClearEnumerableTypeCache();
				break;
		}

		// Assert - verify caches were cleared by calling methods again (they should still work correctly)
		typeof(int).IsSimpleType().ShouldBeTrue();
		typeof(List<int>).IsReadOnlyCollectionType().ShouldBeFalse();
		typeof(double).IsNumericType().ShouldBeTrue();
		typeof(int[]).IsEnumerable().ShouldBeTrue();
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
