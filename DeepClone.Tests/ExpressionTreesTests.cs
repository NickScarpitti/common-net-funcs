using System.Reflection;
using CommonNetFuncs.Core;
using static CommonNetFuncs.DeepClone.ExpressionTrees;

namespace DeepClone.Tests;

public sealed class ExpressionTreesTests : IDisposable
{
	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				CacheManager.SetUseLimitedCache(true);
				CacheManager.SetLimitedCacheSize(100);
				CacheManager.ClearAllCaches();
			}
			disposed = true;
		}
	}

	~ExpressionTreesTests()
	{
		Dispose(false);
	}

	public sealed class TestClass
	{
		public int Number { get; set; }

		public string? Text { get; set; }

		public List<int>? Numbers { get; set; }

		public int[]? NumberArray { get; set; }

		public TestClass? Child { get; set; }

		public readonly string ReadOnlyField = "test";

#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable CS0414 // Remove unused member declaration
#pragma warning disable S1144 // Unused private types or members should be removed
		private readonly int privateReadOnlyField = 42;
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore CS0414 // Remove unused member declaration
#pragma warning restore RCS1213 // Remove unused member declaration
	}

	public struct TestStruct
	{
		public int Number { get; set; }

		public string? Text { get; set; }

		public TestClass? RefType { get; set; }
	}

	public delegate void TestDelegate();

	[Fact]
	public void DeepClone_WhenInputIsNull_ShouldReturnNull()
	{
		// Arrange
		TestClass? source = null;

		// Act
		TestClass? result = source.DeepClone();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void DeepClone_WhenInputIsPrimitiveType_ShouldReturnSameValue()
	{
		// Arrange
		const int source = 42;

		// Act
		int result = source.DeepClone();

		// Assert
		result.ShouldBe(source);
	}

	[Fact]
	public void DeepClone_WhenInputIsString_ShouldReturnSameInstance()
	{
		// Arrange
		const string source = "test";

		// Act
		string result = source.DeepClone();

		// Assert
		result.ShouldBe(source);
		ReferenceEquals(result, source).ShouldBeTrue();
	}

	[Fact]
	public void DeepClone_WhenInputIsDelegate_ShouldThrowArgumentException()
	{
		// Arrange
		TestDelegate source = () => { };

		// Act & Assert
		Should.Throw<ArgumentException>(() => source.DeepClone());
	}

	[Fact]
	public void DeepClone_WhenInputIsSimpleClass_ShouldCreateDeepCopy()
	{
		// Arrange
		TestClass source = new()
		{
			Number = 42,
			Text = "test"
		};

		// Act
		TestClass result = source.DeepClone();

		// Assert
		result.ShouldNotBeSameAs(source);
		result.Number.ShouldBe(source.Number);
		result.Text.ShouldBe(source.Text);
	}

	[Fact]
	public void DeepClone_WhenInputHasCircularReference_ShouldHandleCorrectly()
	{
		// Arrange
		TestClass source = new()
		{
			Number = 42,
			Text = "parent"
		};

		source.Child = new()
		{
			Number = 24,
			Text = "child",
			Child = source
		};

		// Act
		TestClass result = source.DeepClone();

		// Assert
		result.ShouldNotBeSameAs(source);
		result.Child.ShouldNotBeSameAs(source.Child);
		result.Child?.Child.ShouldBeSameAs(result);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void DeepClone_WhenInputIsArray_ShouldCreateDeepCopy(int dimensions)
	{
		// Arrange
		Array source = dimensions switch
		{
			1 => new int[] { 1, 2, 3 },
			2 => new int[,] { { 1, 2 }, { 3, 4 } },
			3 => new int[,,] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } },
			_ => throw new ArgumentException("Unsupported dimension")
		};

		// Act
		Array result = source.DeepClone();

		// Assert
		result.ShouldNotBeSameAs(source);
		result.Length.ShouldBe(source.Length);
		result.Rank.ShouldBe(dimensions);
	}

	[Fact]
	public void DeepClone_WhenInputIsStruct_WithClassMembers_ShouldCreateDeepCopy()
	{
		// Arrange
		TestStruct source = new()
		{
			Number = 42,
			Text = "test",
			RefType = new TestClass { Number = 24, Text = "inner" }
		};

		// Act
		TestStruct result = source.DeepClone();

		// Assert
		result.Number.ShouldBe(source.Number);
		result.Text.ShouldBe(source.Text);
		result.RefType.ShouldNotBeSameAs(source.RefType);
		result.RefType!.Number.ShouldBe(source.RefType.Number);
		result.RefType.Text.ShouldBe(source.RefType.Text);
	}

	[Fact]
	public void DeepClone_WhenInputHasReadOnlyFields_ShouldCreateDeepCopy()
	{
		// Arrange
		TestClass source = new();

		// Act
		TestClass result = source.DeepClone();

		// Assert
		result.ShouldNotBeSameAs(source);
		result.ReadOnlyField.ShouldBe(source.ReadOnlyField);

		// Use reflection to check private readonly field
		FieldInfo? fieldInfo = typeof(TestClass).GetField("privateReadOnlyField", BindingFlags.NonPublic | BindingFlags.Instance);
		fieldInfo.ShouldNotBeNull();
		fieldInfo.GetValue(result).ShouldBe(fieldInfo.GetValue(source));
	}

	[Fact]
	public void DeepClone_WhenInputHasCollection_ShouldCreateDeepCopy()
	{
		// Arrange
		TestClass source = new()
		{
			Numbers = new List<int> { 1, 2, 3 }
		};

		// Act
		TestClass result = source.DeepClone();

		// Assert
		result.Numbers.ShouldNotBeSameAs(source.Numbers);
		result.Numbers!.Count.ShouldBe(source.Numbers.Count);
		result.Numbers.SequenceEqual(source.Numbers!).ShouldBeTrue();
	}

	[Fact]
	public void DeepClone_WhenInputHasArray_ShouldCreateDeepCopy()
	{
		// Arrange
		TestClass[] source =
		[
			new TestClass
				{
					Numbers = [0, 1, 2],
					NumberArray = [1, 2, 3]
				},
				new TestClass
				{
					Numbers = [3, 4, 5],
					NumberArray = [4, 5, 6]
				},
				new TestClass
				{
					Numbers = [6, 7, 8],
					NumberArray = [7, 8, 9]
				},
		];

		// Act
		TestClass[] result = source.DeepClone();

		// Assert
		result.Length.ShouldBe(source.Length);

		for (int i = 0; i < result.Length; i++)
		{
			result[i].NumberArray.ShouldNotBeSameAs(source[i].NumberArray);
			result[i].Numbers.ShouldNotBeSameAs(source[i].Numbers);
			result[i].NumberArray!.Length.ShouldBe(source[i].NumberArray!.Length);
			result[i].Numbers!.Count.ShouldBe(source[i].Numbers!.Count);
			result[i].NumberArray!.SequenceEqual(source[i].NumberArray!).ShouldBeTrue();
			result[i].Numbers!.SequenceEqual(source[i].Numbers!).ShouldBeTrue();
		}
	}

	[Fact]
	public void DeepClone_WithCustomDictionary_ShouldUseProvidedDictionary()
	{
		// Arrange
		TestClass source = new() { Number = 42 };
		Dictionary<object, object> customDict = new(ReferenceEqualityComparer.Instance);

		// Act
		TestClass result = source.DeepClone(customDict);

		// Assert
		result.ShouldNotBeSameAs(source);
		customDict.Count.ShouldBe(1);
		customDict.ContainsKey(source).ShouldBeTrue();
		customDict[source].ShouldBeSameAs(result);
	}

	[Fact]
	public void CacheManager_Property_ShouldExposeApiAndAffectCache()
	{
		// Arrange
		ICacheManagerApi<Type, Func<object, Dictionary<object, object>, object>> cacheApi = CacheManager;
		Type type = typeof(TestClass);

		// Act
		cacheApi.ClearCache();
		cacheApi.ClearLimitedCache();
		cacheApi.SetLimitedCacheSize(1);

		// Should not have anything cached yet
		cacheApi.GetCache().ContainsKey(type).ShouldBeFalse();
		cacheApi.GetLimitedCache().ContainsKey(type).ShouldBeFalse();

		// Add to cache
		Func<object, Dictionary<object, object>, object> func = CreateCompiledLambdaCopyFunctionForType(type, true).Compile();
		cacheApi.TryAddCache(type, func).ShouldBeTrue();
		cacheApi.GetCache().ContainsKey(type).ShouldBeTrue();

		// Add to limited cache
		cacheApi.TryAddLimitedCache(type, func).ShouldBeTrue();
		cacheApi.GetLimitedCache().ContainsKey(type).ShouldBeTrue();

		// IsUsingLimitedCache should reflect state
		cacheApi.SetLimitedCacheSize(2);
		cacheApi.IsUsingLimitedCache().ShouldBeTrue();
		cacheApi.SetLimitedCacheSize(0);
		cacheApi.IsUsingLimitedCache().ShouldBeFalse();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void DeepClone_ShouldRespectCacheUsage(bool useCache)
	{
		// Arrange
		Type type = typeof(TestClass);
		CacheManager.ClearCache();
		CacheManager.ClearLimitedCache();
		CacheManager.SetLimitedCacheSize(0);

		TestClass source = new() { Number = 1, Text = "abc" };

		// Act
		TestClass clone1 = source.DeepClone(useCache: useCache);
		TestClass clone2 = source.DeepClone(useCache: useCache);

		// Assert
		clone1.ShouldNotBeSameAs(source);
		clone2.ShouldNotBeSameAs(source);

		// If using cache, the compiled function should be cached
		if (useCache)
		{
			CacheManager.GetCache().ContainsKey(type).ShouldBeTrue();
		}
		else
		{
			CacheManager.GetCache().ContainsKey(type).ShouldBeFalse();
		}
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(2, true)]
	public void DeepClone_ShouldUseLimitedCache_WhenConfigured(int limitedCacheSize, bool expectLimited)
	{
		// Arrange
		Type type = typeof(TestClass);
		CacheManager.ClearCache();
		CacheManager.ClearLimitedCache();
		CacheManager.SetLimitedCacheSize(limitedCacheSize);

		TestClass source = new() { Number = 2, Text = "xyz" };

		// Act
		_ = source.DeepClone();

		// Assert
		if (expectLimited)
		{
			CacheManager.GetLimitedCache().ContainsKey(type).ShouldBeTrue();
			CacheManager.GetCache().ContainsKey(type).ShouldBeFalse();
		}
		else
		{
			CacheManager.GetCache().ContainsKey(type).ShouldBeTrue();
			CacheManager.GetLimitedCache().ContainsKey(type).ShouldBeFalse();
		}
	}

	[Fact]
	public void DeepClone_WhenInputIsPlainObjectType_ShouldCreateNewObject()
	{
		// Arrange
		object source = new();

		// Act
		object result = source.DeepClone();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.GetType().ShouldBe(typeof(object));
	}

	// Test helper classes for readonly reference fields
	public class ClassWithReadonlyReferenceField
	{
		public readonly TestClass ReadonlyChild;

		public ClassWithReadonlyReferenceField(TestClass child)
		{
			ReadonlyChild = child;
		}
	}

	// Test helper classes for delegate fields
	public class ClassWithDelegateFields
	{
		public readonly TestDelegate? ReadonlyDelegateField;
		public TestDelegate? WritableDelegateField;

		public ClassWithDelegateFields(TestDelegate? readonlyDelegate, TestDelegate? writableDelegate)
		{
			ReadonlyDelegateField = readonlyDelegate;
			WritableDelegateField = writableDelegate;
		}
	}

	// Test helper structs for complex nested scenarios
	public struct InnerStructWithClass
	{
		public int Value { get; set; }
		public TestClass? ClassField { get; set; }
	}

	public struct OuterStructWithInnerStruct
	{
		public string Name { get; set; }
		public InnerStructWithClass InnerStruct { get; set; }
	}

	[Fact]
	public void DeepClone_WhenInputHasReadonlyReferenceField_ShouldCreateDeepCopyWithBoxing()
	{
		// Arrange
		TestClass child = new() { Number = 100, Text = "child" };
		ClassWithReadonlyReferenceField source = new(child);

		// Act
		ClassWithReadonlyReferenceField result = source.DeepClone();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.ReadonlyChild.ShouldNotBeNull();
		result.ReadonlyChild.ShouldNotBeSameAs(source.ReadonlyChild);
		result.ReadonlyChild.Number.ShouldBe(100);
		result.ReadonlyChild.Text.ShouldBe("child");
	}

	[Fact]
	public void DeepClone_WhenInputHasReadonlyDelegateField_ShouldSetToNull()
	{
		// Arrange
		TestDelegate readonlyDelegate = () => { };
		TestDelegate writableDelegate = () => { };
		ClassWithDelegateFields source = new(readonlyDelegate, writableDelegate);

		// Act
		ClassWithDelegateFields result = source.DeepClone();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.ReadonlyDelegateField.ShouldBeNull();
		result.WritableDelegateField.ShouldBeNull();
	}

	[Fact]
	public void DeepClone_WhenInputHasWritableDelegateField_ShouldSetToNull()
	{
		// Arrange
		ClassWithDelegateFields source = new(null, () => { });

		// Act
		ClassWithDelegateFields result = source.DeepClone();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.ReadonlyDelegateField.ShouldBeNull();
		result.WritableDelegateField.ShouldBeNull();
	}

	[Fact]
	public void DeepClone_WhenInputIsNestedStructWithClass_ShouldCreateDeepCopy()
	{
		// Arrange
		TestClass innerClass = new() { Number = 50, Text = "nested" };
		InnerStructWithClass innerStruct = new() { Value = 10, ClassField = innerClass };
		OuterStructWithInnerStruct source = new() { Name = "outer", InnerStruct = innerStruct };

		// Act
		OuterStructWithInnerStruct result = source.DeepClone();

		// Assert
		result.Name.ShouldBe("outer");
		result.InnerStruct.Value.ShouldBe(10);
		result.InnerStruct.ClassField.ShouldNotBeNull();
		result.InnerStruct.ClassField.ShouldNotBeSameAs(source.InnerStruct.ClassField);
		result.InnerStruct.ClassField!.Number.ShouldBe(50);
		result.InnerStruct.ClassField.Text.ShouldBe("nested");
	}
}
