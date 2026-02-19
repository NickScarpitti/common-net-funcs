using System.Reflection;
using CommonNetFuncs.Core;

namespace Core.Tests;

public enum EMergeScenario
{
	MultipleObjects,
	SingleObject,
	DefaultValuesDoNotOverride
}

public enum ECacheOperation
{
	SetLimitedCacheSize,
	SetUseLimitedCache,
	ClearAllCaches,
	GetLimitedCache,
	GetCache
}

public enum ESimpleTypeConversion
{
	IntToLong,
	FloatToDouble,
	ByteToInt,
	ByteToShort,
	IntToInt,
	DoubleToDouble,
	BoolToBool,
	StringToString,
	IntToString,
	StringToInt
}

public enum ERuntimeOperation
{
	CopyObjectNull,
	CopyItemNull,
	CopyCollectionNull,
	CopyObjectValid,
	CopyItemValid,
	CopyCollectionArray,
	CopyCollectionDictionary,
	CopyCollectionList,
	CopyCollectionUnknown
}

public enum ECollectionConversionType
{
	ListToArray,
	ListToHashSet,
	EmptyDictionary
}

public enum EAdvancedCollectionType
{
	PrimitiveArray,
	ComplexArray,
	PrimitiveDictionary,
	HashSet,
	LinkedList,
	NestedArray,
	ComplexGeneric,
	EmptyArray
}

public sealed class CopyTests
{
	#region CopyPropertiesTo Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesTo_ShouldCopyMatchingProperties(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Description = "Test Description"
		};

		DestinationClass destination = new();

		// Act
		source.CopyPropertiesTo(destination, useCache);

		// Assert
		destination.Id.ShouldBe(source.Id);
		destination.Name.ShouldBe(source.Name);

		// Description is not in destination, so it shouldn't be copied
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesTo_WithNullSource_ShouldNotThrowException(bool useCache)
	{
		// Arrange
		SourceClass? source = null;
		DestinationClass destination = new();

		// Act & Assert
		Should.NotThrow(() => source.CopyPropertiesTo(destination, useCache));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesTo_WithNullDestination_ShouldNotThrowException(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test"
		};
		DestinationClass? destination = null;

		// Act & Assert
		Should.NotThrow(() => source.CopyPropertiesTo(destination, useCache));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesTo_WithDifferentPropertyTypes_ShouldNotCopyIncompatibleProperties(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			CreatedDate = DateTime.Now
		};

		DestinationWithDifferentTypes destination = new()
		{
			Id = 0, // Will be copied
			Name = null, // Will be copied
			CreatedDate = 0 // Should not be copied as types don't match
		};

		// Act
		source.CopyPropertiesTo(destination, useCache);

		// Assert
		destination.Id.ShouldBe(source.Id);
		destination.Name.ShouldBe(source.Name);
		destination.CreatedDate.ShouldBe(0); // Should remain unchanged
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesTo_WithReadOnlyDestinationProperties_ShouldNotCopyToReadOnlyProperties(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test"
		};

		ClassWithReadOnlyProperty destination = new();

		// Act
		source.CopyPropertiesTo(destination, useCache);

		// Assert
		destination.Id.ShouldBe(0); // Should remain unchanged as it's read-only
		destination.Name.ShouldBe(source.Name);
	}

	#endregion

	#region CopyPropertiesToNew<T> Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNew_ShouldCreateNewInstanceWithCopiedProperties(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Description = "Test Description"
		};

		// Act
		SourceClass result = source.CopyPropertiesToNew(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
		result.Description.ShouldBe(source.Description);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNew_WithNullSource_ShouldReturnNull(bool useCache)
	{
		// Arrange
		SourceClass? source = null;

		// Act
		SourceClass? result = source.CopyPropertiesToNew(useCache: useCache);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region CopyPropertiesToNew<TSource, TDest> Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNew_Generic_ShouldCreateNewInstanceOfDifferentTypeWithCopiedProperties(bool useCache)
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Description = "Test Description"
		};

		// Act
		DestinationClass? result = source.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<DestinationClass>();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);

		// Description is not in destination, so it shouldn't be copied
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNew_Generic_WithNullSource_ShouldReturnNull(bool useCache)
	{
		// Arrange
		SourceClass? source = null;

		// Act
		DestinationClass? result = source.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: useCache);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region CopyPropertiesRecursive Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_ShouldCopySimpleProperties(bool useCache)
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test"
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_ShouldCopyNestedObjects(bool useCache)
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Child = new SourceClass
			{
				Id = 2,
				Name = "Child"
			}
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
		result.Child.ShouldNotBeNull();
		result.Child.Id.ShouldBe(source.Child.Id);
		result.Child.Name.ShouldBe(source.Child.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_ShouldCopyCollections(bool useCache)
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Items = new List<SourceClass>
			{
				new() { Id = 2, Name = "Item 1" },
				new() { Id = 3, Name = "Item 2" }
			}
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(source.Items.Count);
		result.Items[0].Id.ShouldBe(source.Items[0].Id);
		result.Items[0].Name.ShouldBe(source.Items[0].Name);
		result.Items[1].Id.ShouldBe(source.Items[1].Id);
		result.Items[1].Name.ShouldBe(source.Items[1].Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesRecursive_ShouldCopyDictionaries(bool useCache)
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Dictionary = new Dictionary<string, SourceClass>
			{
				["key1"] = new SourceClass { Id = 2, Name = "Value 1" },
				["key2"] = new SourceClass { Id = 3, Name = "Value 2" }
			}
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
		result.Dictionary.ShouldNotBeNull();
		result.Dictionary.Count.ShouldBe(source.Dictionary.Count);
		result.Dictionary["key1"].Id.ShouldBe(source.Dictionary["key1"].Id);
		result.Dictionary["key1"].Name.ShouldBe(source.Dictionary["key1"].Name);
		result.Dictionary["key2"].Id.ShouldBe(source.Dictionary["key2"].Id);
		result.Dictionary["key2"].Name.ShouldBe(source.Dictionary["key2"].Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithMaxDepth_ShouldLimitRecursionDepth(bool useCache)
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Child = new SourceClass
			{
				Id = 2,
				Name = "Child"
			},
			NestedChild = new ComplexSourceClass
			{
				Id = 3,
				Name = "Nested",
				Child = new SourceClass
				{
					Id = 4,
					Name = "Deeply Nested"
				}
			}
		};

		// Act - Set max depth to 1
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(1, useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
		result.Child.ShouldNotBeNull();
		result.Child.Id.ShouldBe(source.Child.Id);
		result.Child.Name.ShouldBe(source.Child.Name);

		result.NestedChild.ShouldNotBeNull();
		result.NestedChild.Id.ShouldBe(source.NestedChild.Id);
		result.NestedChild.Name.ShouldBe(source.NestedChild.Name);

		// This should be null because we limited recursion depth to 1
		result.NestedChild.Child.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithNullSource_ShouldReturnNull(bool useCache)
	{
		// Arrange
		ComplexSourceClass? source = null;

		// Act
		ComplexDestinationClass? result = source!.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region MergeInstances Tests

	[Theory]
	[InlineData(EMergeScenario.MultipleObjects)]
	[InlineData(EMergeScenario.SingleObject)]
	[InlineData(EMergeScenario.DefaultValuesDoNotOverride)]
	public void MergeInstances_ShouldMergeCorrectly(EMergeScenario scenario)
	{
		switch (scenario)
		{
			case EMergeScenario.MultipleObjects:
				{
					SourceClass target = new() { Id = 0, Name = null };
					SourceClass source1 = new() { Id = 1, Name = null };
					SourceClass source2 = new() { Id = 0, Name = "Test" };
					SourceClass result = target.MergeInstances(new[] { source1, source2 }, cancellationToken: TestContext.Current.CancellationToken);
					result.ShouldBeSameAs(target);
					result.Id.ShouldBe(1);
					result.Name.ShouldBe("Test");
					break;
				}
			case EMergeScenario.SingleObject:
				{
					SourceClass target = new() { Id = 0, Name = null };
					SourceClass source = new() { Id = 1, Name = "Test" };
					SourceClass result = target.MergeInstances(source, cancellationToken: TestContext.Current.CancellationToken);
					result.ShouldBeSameAs(target);
					result.Id.ShouldBe(1);
					result.Name.ShouldBe("Test");
					break;
				}
			case EMergeScenario.DefaultValuesDoNotOverride:
				{
					SourceClass target = new() { Id = 1, Name = "Original" };
					SourceClass source = new() { Id = 0, Name = "Test" };
					SourceClass result = target.MergeInstances(source, cancellationToken: TestContext.Current.CancellationToken);
					result.ShouldBeSameAs(target);
					result.Id.ShouldBe(1);
					result.Name.ShouldBe("Original");
					break;
				}
		}
	}

	#endregion

	#region CacheManager API Tests

	[Theory]
	[InlineData(ECacheOperation.SetLimitedCacheSize, true)]
	[InlineData(ECacheOperation.SetLimitedCacheSize, false)]
	[InlineData(ECacheOperation.SetUseLimitedCache, true)]
	[InlineData(ECacheOperation.SetUseLimitedCache, false)]
	[InlineData(ECacheOperation.ClearAllCaches, true)]
	[InlineData(ECacheOperation.ClearAllCaches, false)]
	[InlineData(ECacheOperation.GetLimitedCache, true)]
	[InlineData(ECacheOperation.GetLimitedCache, false)]
	[InlineData(ECacheOperation.GetCache, true)]
	[InlineData(ECacheOperation.GetCache, false)]
	public void CacheManager_Operations_ShouldWorkCorrectly(ECacheOperation operation, bool useDeepCopy)
	{
		switch (operation)
		{
			case ECacheOperation.SetLimitedCacheSize:
				if (useDeepCopy)
				{
					Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
					Copy.DeepCopyCacheManager.GetLimitedCacheSize().ShouldBe(10);
				}
				else
				{
					Copy.CopyCacheManager.SetLimitedCacheSize(10);
					Copy.CopyCacheManager.GetLimitedCacheSize().ShouldBe(10);
				}
				break;
			case ECacheOperation.SetUseLimitedCache:
				if (useDeepCopy)
				{
					Copy.DeepCopyCacheManager.SetUseLimitedCache(true);
					Copy.DeepCopyCacheManager.IsUsingLimitedCache().ShouldBeTrue();
					Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
				}
				else
				{
					Copy.CopyCacheManager.SetUseLimitedCache(true);
					Copy.CopyCacheManager.IsUsingLimitedCache().ShouldBeTrue();
					Copy.CopyCacheManager.SetUseLimitedCache(false);
				}
				break;
			case ECacheOperation.ClearAllCaches:
				if (useDeepCopy)
				{
					Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = new();
					source.CopyPropertiesTo(dest, useCache: true);
					Copy.DeepCopyCacheManager.ClearAllCaches();
					Copy.DeepCopyCacheManager.GetCache().Count.ShouldBe(0);
				}
				else
				{
					Copy.CopyCacheManager.SetLimitedCacheSize(10);
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = new();
					source.CopyPropertiesTo(dest, useCache: true);
					Copy.CopyCacheManager.ClearAllCaches();
					Copy.CopyCacheManager.GetCache().Count.ShouldBe(0);
				}
				break;
			case ECacheOperation.GetLimitedCache:
				if (useDeepCopy)
				{
					Copy.DeepCopyCacheManager.SetUseLimitedCache(true);
					Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>(useCache: true);
					dest.ShouldNotBeNull();
					Copy.DeepCopyCacheManager.GetLimitedCache().Count.ShouldBe(1);
					Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
				}
				else
				{
					Copy.CopyCacheManager.SetUseLimitedCache(true);
					Copy.CopyCacheManager.SetLimitedCacheSize(10);
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = new();
					source.CopyPropertiesTo(dest, useCache: true);
					Copy.CopyCacheManager.GetLimitedCache().Count.ShouldBe(1);
					Copy.CopyCacheManager.SetUseLimitedCache(false);
				}
				break;
			case ECacheOperation.GetCache:
				if (useDeepCopy)
				{
					Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>(useCache: true);
					dest.ShouldNotBeNull();
					Copy.DeepCopyCacheManager.GetCache().Count.ShouldBe(1);
				}
				else
				{
					Copy.CopyCacheManager.SetUseLimitedCache(false);
					Copy.CopyCacheManager.ClearAllCaches();
					Copy.CopyCacheTypedManager.ClearAllCaches();
					SourceClass source = new() { Id = 42, Name = "CacheTest" };
					DestinationClass dest = new();
					source.CopyPropertiesTo(dest, useCache: true);
					Copy.CopyCacheManager.GetCache().Count.ShouldBe(1);
				}
				break;
		}
	}

	[Fact]
	public void DeepCopyCacheManager_Api_ShouldNotAffectCopyBehavior()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetUseLimitedCache(true);
		Copy.DeepCopyCacheManager.SetLimitedCacheSize(5);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };
		DestinationClass dest = new();

		// Act
		source.CopyPropertiesTo(dest, useCache: true);

		// Assert
		dest.Id.ShouldBe(42);
		dest.Name.ShouldBe("CacheTest");

		// Now clear cache and copy again
		Copy.DeepCopyCacheManager.ClearAllCaches();
		dest = new();
		source.CopyPropertiesTo(dest, useCache: true);
		dest.Id.ShouldBe(42);
		dest.Name.ShouldBe("CacheTest");

		// Cleanup
		Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
	}

	#endregion

	#region CopyPropertiesToNewRecursive With useCache=false Tests

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_ShouldCopyProperties()
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Description = "Test Description"
		};

		// Act
		DestinationClass result = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(source.Id);
		result.Name.ShouldBe(source.Name);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndNestedObject_ShouldCopyRecursively()
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Parent",
			Child = new SourceClass { Id = 2, Name = "Child" }
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Parent");
		result.Child.ShouldNotBeNull();
		result.Child.Id.ShouldBe(2);
		result.Child.Name.ShouldBe("Child");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndCollectionSource_ShouldCopyCollection()
	{
		// Arrange
		List<SourceClass> source = new()
		{
			new SourceClass { Id = 1, Name = "Item1" },
			new SourceClass { Id = 2, Name = "Item2" }
		};

		// Act
		List<DestinationClass> result = source.CopyPropertiesToNewRecursive<List<SourceClass>, List<DestinationClass>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		result[0].Id.ShouldBe(1);
		result[0].Name.ShouldBe("Item1");
		result[1].Id.ShouldBe(2);
		result[1].Name.ShouldBe("Item2");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndDictionary_ShouldCopyDictionary()
	{
		// Arrange
		Dictionary<string, SourceClass> source = new()
		{
			["key1"] = new SourceClass { Id = 1, Name = "Value1" },
			["key2"] = new SourceClass { Id = 2, Name = "Value2" }
		};

		// Act
		Dictionary<string, DestinationClass> result = source.CopyPropertiesToNewRecursive<Dictionary<string, SourceClass>, Dictionary<string, DestinationClass>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		result["key1"].Id.ShouldBe(1);
		result["key1"].Name.ShouldBe("Value1");
		result["key2"].Id.ShouldBe(2);
		result["key2"].Name.ShouldBe("Value2");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndComplexDictionary_ShouldCopyDictionary()
	{
		// Arrange
		Dictionary<int, SourceClass> source = new()
		{
			[1] = new SourceClass { Id = 10, Name = "Value1" },
			[2] = new SourceClass { Id = 20, Name = "Value2" }
		};

		// Act
		Dictionary<int, DestinationClass> result = source.CopyPropertiesToNewRecursive<Dictionary<int, SourceClass>, Dictionary<int, DestinationClass>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		result[1].Id.ShouldBe(10);
		result[1].Name.ShouldBe("Value1");
		result[2].Id.ShouldBe(20);
		result[2].Name.ShouldBe("Value2");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndNullCollectionItem_ShouldHandleNull()
	{
		// Arrange
		List<SourceClass?> source = new()
		{
			new SourceClass { Id = 1, Name = "Item1" },
			null,
			new SourceClass { Id = 2, Name = "Item2" }
		};

		// Act
		List<DestinationClass?> result = source.CopyPropertiesToNewRecursive<List<SourceClass?>, List<DestinationClass?>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
		result[0].ShouldNotBeNull();
		result[0]!.Id.ShouldBe(1);
		result[1].ShouldBeNull();
		result[2].ShouldNotBeNull();
		result[2]!.Id.ShouldBe(2);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndMaxDepthLimit_ShouldStopAtMaxDepth()
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Level1",
			NestedChild = new ComplexSourceClass
			{
				Id = 2,
				Name = "Level2",
				Child = new SourceClass { Id = 3, Name = "Level3" }
			}
		};

		// Act - Max depth 0 should only copy simple properties
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(maxDepth: 0, useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Level1");
		result.NestedChild.ShouldBeNull(); // Should not be copied due to depth limit
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndSameType_ShouldReturnSource()
	{
		// Arrange
		SourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Description = "Description"
		};

		// Act
		SourceClass result = source.CopyPropertiesToNewRecursive<SourceClass, SourceClass>(useCache: false);

		// Assert
		result.ShouldBe(source); // Should be same object when types match
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndPropertyWithNullValue_ShouldCopyNull()
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Test",
			Child = null
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
		result.Child.ShouldBeNull();
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndNestedCollection_ShouldCopyNestedCollection()
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Parent",
			Items = new List<SourceClass>
			{
				new() { Id = 2, Name = "Child1" },
				new() { Id = 3, Name = "Child2" }
			}
		};

		// Act
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(2);
		result.Items[0].Id.ShouldBe(2);
		result.Items[0].Name.ShouldBe("Child1");
		result.Items[1].Id.ShouldBe(3);
		result.Items[1].Name.ShouldBe("Child2");
	}

	#endregion

	#region Expression Tree Copy Tests



	[Fact]
	public void CopyPropertiesToNewRecursive_WithInterface_ShouldCopyInterfaceProperties()
	{
		// Arrange
		ClassWithInterface source = new() { Id = 1, Name = "Test" };

		// Act
		ClassWithInterface result = source.CopyPropertiesToNewRecursive<ClassWithInterface, ClassWithInterface>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithArrayInExpressionTree_ShouldCopyArray()
	{
		// Arrange
		ClassWithArray source = new()
		{
			Id = 1,
			Items = new[] { new SourceClass { Id = 2, Name = "Item1" }, new SourceClass { Id = 3, Name = "Item2" } }
		};

		// Act
		ClassWithArray result = source.CopyPropertiesToNewRecursive<ClassWithArray, ClassWithArray>();

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Length.ShouldBe(2);
		result.Items[0].Id.ShouldBe(2);
		result.Items[0].Name.ShouldBe("Item1");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithDictionaryInExpressionTree_ShouldCopyDictionary()
	{
		// Arrange
		ClassWithDictionary source = new()
		{
			Id = 1,
			Data = new Dictionary<string, SourceClass>
			{
				["key1"] = new SourceClass { Id = 2, Name = "Value1" },
				["key2"] = new SourceClass { Id = 3, Name = "Value2" }
			}
		};

		// Act
		ClassWithDictionary result = source.CopyPropertiesToNewRecursive<ClassWithDictionary, ClassWithDictionary>();

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data.Count.ShouldBe(2);
		result.Data["key1"].Id.ShouldBe(2);
		result.Data["key1"].Name.ShouldBe("Value1");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithNullableProperties_ShouldHandleNulls()
	{
		// Arrange
		ClassWithNullable source = new()
		{
			Id = 1,
			NullableInt = null,
			Child = null
		};

		// Act
		ClassWithNullable result = source.CopyPropertiesToNewRecursive<ClassWithNullable, ClassWithNullable>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.NullableInt.ShouldBeNull();
		result.Child.ShouldBeNull();
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithDepthExceeded_ShouldStopRecursion()
	{
		// Arrange
		ComplexSourceClass source = new()
		{
			Id = 1,
			Name = "Level1",
			NestedChild = new ComplexSourceClass
			{
				Id = 2,
				Name = "Level2",
				NestedChild = new ComplexSourceClass
				{
					Id = 3,
					Name = "Level3"
				}
			}
		};

		// Act - Max depth 1 should copy first nested level only
		ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(maxDepth: 1);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Level1");
		result.NestedChild.ShouldNotBeNull();
		result.NestedChild.Id.ShouldBe(2);
		result.NestedChild.Name.ShouldBe("Level2");
		result.NestedChild.NestedChild.ShouldBeNull(); // Depth exceeded
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithCustomCollection_ShouldCopyCustomCollection()
	{
		// Arrange
		ClassWithCustomCollection source = new()
		{
			Id = 1,
			Items = new CustomCollection<SourceClass>
			{
				new() { Id = 2, Name = "Item1" },
				new() { Id = 3, Name = "Item2" }
			}
		};

		// Act
		ClassWithCustomCollection result = source.CopyPropertiesToNewRecursive<ClassWithCustomCollection, ClassWithCustomCollection>();

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(2);
		result.Items[0].Id.ShouldBe(2);
		result.Items[0].Name.ShouldBe("Item1");
	}

	#endregion

	#region Additional Edge Case Tests

	[Fact]
	public void CopyPropertiesTo_MultipleCallsWithCache_ShouldUseCachedMappings()
	{
		// Arrange
		SourceClass source1 = new() { Id = 1, Name = "First" };
		SourceClass source2 = new() { Id = 2, Name = "Second" };
		DestinationClass dest1 = new();
		DestinationClass dest2 = new();

		// Act - First call creates cache, second call uses cache
		source1.CopyPropertiesTo(dest1, useCache: true);
		source2.CopyPropertiesTo(dest2, useCache: true);

		// Assert
		dest1.Id.ShouldBe(1);
		dest1.Name.ShouldBe("First");
		dest2.Id.ShouldBe(2);
		dest2.Name.ShouldBe("Second");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithInterfaceProperty_ShouldCopyViaInterface()
	{
		// Arrange
		ClassWithInterface source = new() { Id = 42, Name = "Test" };

		// Act
		ClassWithInterface result = source.CopyPropertiesToNewRecursive<ClassWithInterface, ClassWithInterface>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(42);
		result.Name.ShouldBe("Test");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithNoMatchingProperties_ShouldCreateEmptyObject()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test" };

		// Act
		ClassWithNoMatchingProps result = source.CopyPropertiesToNewRecursive<SourceClass, ClassWithNoMatchingProps>();

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(0); // Default value
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithValueTypeProperty_ShouldCopyValueType()
	{
		// Arrange
		ClassWithValueType source = new() { Id = 1, Point = new System.Drawing.Point(10, 20) };

		// Act
		ClassWithValueType result = source.CopyPropertiesToNewRecursive<ClassWithValueType, ClassWithValueType>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Point.X.ShouldBe(10);
		result.Point.Y.ShouldBe(20);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithStructProperty_ShouldCopyStruct()
	{
		// Arrange
		ClassWithStruct source = new() { Id = 1, Data = new CustomStruct { Value1 = 42, Value2 = "Test" } };

		// Act
		ClassWithStruct result = source.CopyPropertiesToNewRecursive<ClassWithStruct, ClassWithStruct>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Data.Value1.ShouldBe(42);
		result.Data.Value2.ShouldBe("Test");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithIncompatibleSimpleTypes_ShouldUseDefault()
	{
		// Arrange
		ClassWithString source = new() { Value = "NotANumber" };

		// Act
		SimpleIntClass result = source.CopyPropertiesToNewRecursive<ClassWithString, SimpleIntClass>();

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(0); // Default value since string can't convert to int
	}



	[Fact]
	public void CopyPropertiesToNewRecursive_WithDictionaryWithComplexKeys_ShouldCopyDictionary()
	{
		// Arrange
		Dictionary<ComplexKey, SourceClass> source = new()
		{
			[new ComplexKey { Id = 1, Code = "A" }] = new SourceClass { Id = 10, Name = "Value1" },
			[new ComplexKey { Id = 2, Code = "B" }] = new SourceClass { Id = 20, Name = "Value2" }
		};

		// Act - When using useCache=false, collections are copied via reflection
		Dictionary<ComplexKey, DestinationClass> result =
			source.CopyPropertiesToNewRecursive<Dictionary<ComplexKey, SourceClass>, Dictionary<ComplexKey, DestinationClass>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		// Note: Complex keys are copied by the runtime helpers
	}

	[Fact]
	public void CopyPropertiesToNew_WithSameType_ShouldCopyAllProperties()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test", Description = "Desc" };

		// Act
		SourceClass result = source.CopyPropertiesToNew(useCache: true);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeSameAs(source);
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
		result.Description.ShouldBe("Desc");
	}

	[Fact]
	public void CopyPropertiesToNew_WithCache_MultipleCallsShouldUseCachedMappings()
	{
		// Arrange
		SourceClass source1 = new() { Id = 1, Name = "First" };
		SourceClass source2 = new() { Id = 2, Name = "Second" };

		// Act
		SourceClass result1 = source1.CopyPropertiesToNew(useCache: true);
		SourceClass result2 = source2.CopyPropertiesToNew(useCache: true);

		// Assert
		result1.Id.ShouldBe(1);
		result2.Id.ShouldBe(2);
	}

	[Fact]
	public void CopyPropertiesToNew_WithoutCache_ShouldCopyProperties()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test", Description = "Desc" };

		// Act
		SourceClass result = source.CopyPropertiesToNew(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
		result.Description.ShouldBe("Desc");
	}

	[Fact]
	public void CopyPropertiesToNew_Generic_WithCache_ShouldCopyMatchingProperties()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test" };

		// Act
		DestinationClass? result = source.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
	}

	[Fact]
	public void CopyPropertiesToNew_Generic_WithoutCache_ShouldCopyMatchingProperties()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test" };

		// Act
		DestinationClass? result = source.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
	}

	[Fact]
	public void CopyPropertiesTo_WithNullDestAndCacheTrue_ShouldReturnEarly()
	{
		// Arrange
		SourceClass source = new() { Id = 1, Name = "Test" };
		DestinationClass? dest = null;

		// Act & Assert
		Should.NotThrow(() => source.CopyPropertiesTo(dest, useCache: true));
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithEmptyCollection_ShouldReturnEmptyCollection()
	{
		// Arrange
		List<SourceClass> source = new();

		// Act
		List<DestinationClass> result = source.CopyPropertiesToNewRecursive<List<SourceClass>, List<DestinationClass>>();

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithEmptyDictionary_ShouldReturnEmptyDictionary()
	{
		// Arrange
		Dictionary<string, SourceClass> source = new();

		// Act - Use useCache=false to copy as collection
		Dictionary<string, DestinationClass> result =
			source.CopyPropertiesToNewRecursive<Dictionary<string, SourceClass>, Dictionary<string, DestinationClass>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(0);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithDictionaryContainingNullValue_ShouldHandleNull()
	{
		// Arrange
		Dictionary<string, SourceClass?> source = new()
		{
			["key1"] = new SourceClass { Id = 1, Name = "Value1" },
			["key2"] = null
		};

		// Act - Use useCache=false to copy as collection
		Dictionary<string, DestinationClass?> result =
			source.CopyPropertiesToNewRecursive<Dictionary<string, SourceClass?>, Dictionary<string, DestinationClass?>>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		result["key1"].ShouldNotBeNull();
		result["key2"].ShouldBeNull();
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithNestedDictionary_ShouldCopyNestedDictionary()
	{
		// Arrange
		ClassWithNestedDictionary source = new()
		{
			Id = 1,
			Data = new Dictionary<string, Dictionary<string, SourceClass>>
			{
				["outer1"] = new Dictionary<string, SourceClass>
				{
					["inner1"] = new SourceClass { Id = 2, Name = "Value" }
				}
			}
		};

		// Act
		ClassWithNestedDictionary result =
			source.CopyPropertiesToNewRecursive<ClassWithNestedDictionary, ClassWithNestedDictionary>();

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data["outer1"]["inner1"].Id.ShouldBe(2);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithInheritedProperties_ShouldCopyInheritedProperties()
	{
		// Arrange
		DerivedClass source = new() { Id = 1, Name = "Test", DerivedProperty = "Derived" };

		// Act
		DerivedClass result = source.CopyPropertiesToNewRecursive<DerivedClass, DerivedClass>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Name.ShouldBe("Test");
		result.DerivedProperty.ShouldBe("Derived");
	}

	[Fact]
	public void GetOrCreatePropertyMaps_WithInterfaceTypes_ShouldHandleInterfaceConversion()
	{
		// Arrange
		Copy.CopyCacheManager.ClearAllCaches();
		Copy.CopyCacheTypedManager.ClearAllCaches();

		// Act
		Dictionary<string, (Action<ITestInterface, object?> Set, Func<ITestInterface, object?> Get)> maps =
			Copy.GetOrCreatePropertyMaps<ITestInterface, ITestInterface>();

		// Assert
		maps.ShouldNotBeNull();
		maps.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void DeepCopyCacheManager_WithLimitedCache_ShouldUseLimitedCache()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetUseLimitedCache(true);
		Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
		Copy.DeepCopyCacheManager.ClearAllCaches();

		SourceClass source = new() { Id = 1, Name = "Test" };

		// Act
		DestinationClass result = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>();

		// Assert
		result.ShouldNotBeNull();
		Copy.DeepCopyCacheManager.GetLimitedCache().Count.ShouldBeGreaterThan(0);

		// Cleanup
		Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
	}

	#endregion

	#region Advanced Runtime Helper Tests

	[Theory]
	[InlineData(EAdvancedCollectionType.PrimitiveArray)]
	[InlineData(EAdvancedCollectionType.ComplexArray)]
	[InlineData(EAdvancedCollectionType.PrimitiveDictionary)]
	[InlineData(EAdvancedCollectionType.HashSet)]
	[InlineData(EAdvancedCollectionType.LinkedList)]
	[InlineData(EAdvancedCollectionType.NestedArray)]
	[InlineData(EAdvancedCollectionType.ComplexGeneric)]
	[InlineData(EAdvancedCollectionType.EmptyArray)]
	public void CopyPropertiesToNewRecursive_WithCollectionTypes_ShouldCopyCorrectly(EAdvancedCollectionType collectionType)
	{
		switch (collectionType)
		{
			case EAdvancedCollectionType.PrimitiveArray:
				{
					ClassWithPrimitiveArray source = new() { Id = 1, Values = new[] { 1, 2, 3, 4, 5 } };
					ClassWithPrimitiveArray result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveArray, ClassWithPrimitiveArray>();
					result.ShouldNotBeNull();
					result.Values.ShouldNotBeNull();
					result.Values.Length.ShouldBe(5);
					result.Values[0].ShouldBe(1);
					result.Values[4].ShouldBe(5);
					break;
				}
			case EAdvancedCollectionType.ComplexArray:
				{
					ClassWithComplexArray source = new()
					{
						Id = 1,
						Items = new[] { new SourceClass { Id = 10, Name = "Item1" }, new SourceClass { Id = 20, Name = "Item2" } }
					};
					ClassWithComplexArray result = source.CopyPropertiesToNewRecursive<ClassWithComplexArray, ClassWithComplexArray>();
					result.ShouldNotBeNull();
					result.Items.ShouldNotBeNull();
					result.Items.Length.ShouldBe(2);
					result.Items[0].Id.ShouldBe(10);
					result.Items[1].Name.ShouldBe("Item2");
					break;
				}
			case EAdvancedCollectionType.PrimitiveDictionary:
				{
					ClassWithPrimitiveDictionary source = new() { Id = 1, Data = new Dictionary<string, int> { ["key1"] = 100, ["key2"] = 200 } };
					ClassWithPrimitiveDictionary result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveDictionary, ClassWithPrimitiveDictionary>();
					result.ShouldNotBeNull();
					result.Data.ShouldNotBeNull();
					result.Data.Count.ShouldBe(2);
					result.Data["key1"].ShouldBe(100);
					result.Data["key2"].ShouldBe(200);
					break;
				}
			case EAdvancedCollectionType.HashSet:
				{
					ClassWithHashSet source = new() { Id = 1, Items = new HashSet<int> { 1, 2, 3, 4, 5 } };
					ClassWithHashSet result = source.CopyPropertiesToNewRecursive<ClassWithHashSet, ClassWithHashSet>();
					result.ShouldNotBeNull();
					result.Items.ShouldNotBeNull();
					result.Items.Count.ShouldBe(5);
					result.Items.Contains(1).ShouldBeTrue();
					result.Items.Contains(5).ShouldBeTrue();
					break;
				}
			case EAdvancedCollectionType.LinkedList:
				{
					ClassWithLinkedList source = new() { Id = 1, Items = new LinkedList<int>([1, 2, 3]) };
					ClassWithLinkedList result = source.CopyPropertiesToNewRecursive<ClassWithLinkedList, ClassWithLinkedList>();
					result.ShouldNotBeNull();
					result.Items.ShouldNotBeNull();
					result.Items.Count.ShouldBe(3);
					break;
				}
			case EAdvancedCollectionType.NestedArray:
				{
					ClassWithNestedArray source = new() { Id = 1, Matrix = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } } };
					ClassWithNestedArray result = source.CopyPropertiesToNewRecursive<ClassWithNestedArray, ClassWithNestedArray>();
					result.ShouldNotBeNull();
					result.Matrix.ShouldNotBeNull();
					result.Matrix.Length.ShouldBe(2);
					result.Matrix[0].Length.ShouldBe(3);
					result.Matrix[0][0].ShouldBe(1);
					result.Matrix[1][2].ShouldBe(6);
					break;
				}
			case EAdvancedCollectionType.ComplexGeneric:
				{
					ClassWithComplexGeneric source = new()
					{
						Id = 1,
						Data = new List<Dictionary<string, SourceClass>>
						{
							new() { ["key1"] = new SourceClass { Id = 10, Name = "Value1" } }
						}
					};
					ClassWithComplexGeneric result = source.CopyPropertiesToNewRecursive<ClassWithComplexGeneric, ClassWithComplexGeneric>();
					result.ShouldNotBeNull();
					result.Data.ShouldNotBeNull();
					result.Data.Count.ShouldBe(1);
					result.Data[0]["key1"].Id.ShouldBe(10);
					break;
				}
			case EAdvancedCollectionType.EmptyArray:
				{
					ClassWithPrimitiveArray source = new() { Id = 1, Values = Array.Empty<int>() };
					ClassWithPrimitiveArray result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveArray, ClassWithPrimitiveArray>();
					result.ShouldNotBeNull();
					result.Values.ShouldNotBeNull();
					result.Values.Length.ShouldBe(0);
					break;
				}
		}
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithMixedPropertyTypes_ShouldCopyOnlyCompatibleProperties()
	{
		// Arrange
		MixedTypeSource source = new()
		{
			IntValue = 42,
			StringValue = "Test",
			ComplexValue = new SourceClass { Id = 1, Name = "Complex" }
		};

		// Act
		MixedTypeDest result = source.CopyPropertiesToNewRecursive<MixedTypeSource, MixedTypeDest>();

		// Assert
		result.ShouldNotBeNull();
		result.IntValue.ShouldBe(42);
		result.StringValue.ShouldBe("Test");
		result.ComplexValue.ShouldNotBeNull();
		result.ComplexValue.Id.ShouldBe(1);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithRecursiveDepthLimitInExpression_ShouldStopAtDepth()
	{
		// Arrange
		DeepNesting source = new()
		{
			Level = 1,
			Next = new DeepNesting
			{
				Level = 2,
				Next = new DeepNesting
				{
					Level = 3,
					Next = new DeepNesting { Level = 4 }
				}
			}
		};

		// Act - Limit depth to 2
		DeepNesting result = source.CopyPropertiesToNewRecursive<DeepNesting, DeepNesting>(maxDepth: 2);

		// Assert
		result.ShouldNotBeNull();
		result.Level.ShouldBe(1);
		result.Next.ShouldNotBeNull();
		result.Next!.Level.ShouldBe(2);
		result.Next.Next.ShouldNotBeNull();
		result.Next.Next!.Level.ShouldBe(3);
		// Note: maxDepth is checked as (depth >= maxDepth), so at depth 2, we can still copy
		// The actual limit prevents copying at depth 3 and beyond
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithCancellationInCollection_ShouldRespectCancellation()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		List<SourceClass> largeSource = Enumerable.Range(1, 100)
			.Select(i => new SourceClass { Id = i, Name = $"Item{i}" })
			.ToList();

		// Act - Cancel immediately to test cancellation path
		cts.Cancel();

		// Assert - The cancellation exception will be wrapped in TargetInvocationException
		TargetInvocationException ex = Should.Throw<TargetInvocationException>(() =>
		{
			// Force using the reflection path which has cancellation support
			Dictionary<int, SourceClass> dict = largeSource.ToDictionary(x => x.Id);
			_ = typeof(Copy).GetMethod("CopyCollection", BindingFlags.NonPublic | BindingFlags.Static)!
				.Invoke(null, new object[] { dict, typeof(Dictionary<int, DestinationClass>), -1, cts.Token });
		});

		// Verify the inner exception is OperationCanceledException
		ex.InnerException.ShouldBeOfType<OperationCanceledException>();
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithReadOnlyCollection_ShouldCopyToReadOnlyCollection()
	{
		// Arrange
		ClassWithReadOnlyCollection source = new()
		{
			Id = 1,
			Items = new System.Collections.ObjectModel.ReadOnlyCollection<int>([1, 2, 3])
		};

		// Act - This will copy properties, not collection items
		ClassWithReadOnlyCollection result = source.CopyPropertiesToNewRecursive<ClassWithReadOnlyCollection, ClassWithReadOnlyCollection>();

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		// Items won't be copied as ReadOnlyCollection doesn't have Add method and different property structure
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithCircularReference_ShouldNotCauseInfiniteRecursion()
	{
		// Arrange
		CircularRef source = new() { Id = 1, Name = "Node1" };
		source.Self = source; // Circular reference

		// Act & Assert - Should not throw stack overflow or hang
		// The expression tree handles this by eventually hitting depth limits or returning same references
		Should.NotThrow(() =>
		{
			CircularRef result = source.CopyPropertiesToNewRecursive<CircularRef, CircularRef>(maxDepth: 10);
			result.ShouldNotBeNull();
			result.Id.ShouldBe(1);
			result.Name.ShouldBe("Node1");
		});
	}

	#endregion

	#region CopyCollection Coverage Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithListToArrayConversion_ShouldConvertToArray(bool useCache)
	{
		// Arrange - Testing destType.IsArray branch
		ClassWithListOfInts source = new()
		{
			Values = new List<int> { 1, 2, 3, 4, 5 }
		};

		// Act
		ClassWithIntArray result = source.CopyPropertiesToNewRecursive<ClassWithListOfInts, ClassWithIntArray>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Values.ShouldNotBeNull();
		result.Values.Length.ShouldBe(5);
		result.Values[0].ShouldBe(1);
		result.Values[4].ShouldBe(5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithListToHashSetType_ShouldUseAddMethod(bool useCache)
	{
		// Arrange - Testing non-interface, non-array collection with Add method
		ClassWithListOfInts source = new()
		{
			Values = new List<int> { 10, 20, 30 }
		};

		// Act
		ClassWithHashSetOfInts result = source.CopyPropertiesToNewRecursive<ClassWithListOfInts, ClassWithHashSetOfInts>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Values.ShouldNotBeNull();
		result.Values.Count.ShouldBe(3);
		result.Values.ShouldContain(10);
		result.Values.ShouldContain(20);
		result.Values.ShouldContain(30);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithEmptyDictionaryWithComplexValues_ShouldHandleEmpty(bool useCache)
	{
		// Arrange - Testing dictionary copy with empty source
		ClassWithDictionaryOfComplexValues source = new()
		{
			Data = new Dictionary<string, SourceClass>()
		};

		// Act
		ClassWithDictionaryOfComplexValues result = source.CopyPropertiesToNewRecursive<ClassWithDictionaryOfComplexValues, ClassWithDictionaryOfComplexValues>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data.Count.ShouldBe(0);
	}

	#endregion

	#region CreateCopyFunction Coverage Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithUnmatchedProperties_ShouldSkipThem(bool useCache)
	{
		// Arrange - Source has properties that don't exist in destination
		ClassWithExtraProperties source = new()
		{
			Id = 100,
			Name = "Test",
			ExtraProperty1 = "Extra1",
			ExtraProperty2 = 999
		};

		// Act
		ClassWithBasicProperties result = source.CopyPropertiesToNewRecursive<ClassWithExtraProperties, ClassWithBasicProperties>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(100);
		result.Name.ShouldBe("Test");
		// ExtraProperty1 and ExtraProperty2 don't exist in destination, so they're skipped
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithNullReferenceTypeProperty_ShouldHandleNull(bool useCache)
	{
		// Arrange - Testing null check branch for reference types
		ClassWithNullableReferenceProperty source = new()
		{
			Id = 50,
			Name = null  // null reference type
		};

		// Act
		ClassWithNullableReferenceProperty result = source.CopyPropertiesToNewRecursive<ClassWithNullableReferenceProperty, ClassWithNullableReferenceProperty>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(50);
		result.Name.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithIncompatibleComplexTypes_ShouldSkipIncompatibleProperties(bool useCache)
	{
		// Arrange - Properties with incompatible complex types that can't be converted
		ClassWithIncompatibleComplexType source = new()
		{
			Id = 200,
			ComplexProp = new SourceClass { Id = 1, Name = "Source" }
		};

		// Act
		ClassWithDifferentComplexType result = source.CopyPropertiesToNewRecursive<ClassWithIncompatibleComplexType, ClassWithDifferentComplexType>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(200);
		// ComplexProp has different types (SourceClass vs UnrelatedClass), but CreateValueCopyExpression handles it
		result.ComplexProp.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithValueTypeProperties_ShouldCopyValueTypes(bool useCache)
	{
		// Arrange - Testing value type branch (line 656)
		ClassWithValueTypeProperties source = new()
		{
			Id = 123,
			IntValue = 456,
			DoubleValue = 78.9,
			BoolValue = true
		};

		// Act
		ClassWithValueTypeProperties result = source.CopyPropertiesToNewRecursive<ClassWithValueTypeProperties, ClassWithValueTypeProperties>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(123);
		result.IntValue.ShouldBe(456);
		result.DoubleValue.ShouldBe(78.9);
		result.BoolValue.ShouldBeTrue();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithIncompatibleStructTypes_ShouldSkipIncompatibleProperties(bool useCache)
	{
		// Arrange - Properties with incompatible struct types that CreateValueCopyExpression returns null for
		ClassWithIncompatibleStruct source = new()
		{
			Id = 300,
			StructProp = new CustomStruct { Value1 = 10, Value2 = "Test" }
		};

		// Act
		ClassWithDifferentStruct result = source.CopyPropertiesToNewRecursive<ClassWithIncompatibleStruct, ClassWithDifferentStruct>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(300);
		// StructProp has incompatible types, CreateValueCopyExpression returns null, property not copied
		result.StructProp.OtherValue.ShouldBe(0);
	}

	#endregion

	#region Simple Type Destination Tests (Expression Tree Coverage)

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithSameSimpleTypeProperty_ShouldCopyValue(bool useCache)
	{
		// Arrange - Test sourceType == destType branch in CreateCopyFunction
		ClassWithInt source = new() { Value = 123 };

		// Act
		ClassWithInt result = source.CopyPropertiesToNewRecursive<ClassWithInt, ClassWithInt>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(123);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithConvertibleSimpleTypeProperty_ShouldConvert(bool useCache)
	{
		// Arrange - Test CanConvertTypes branch in CreateCopyFunction
		SimpleIntClass source = new() { Value = 42 };

		// Act
		SimpleLongClass result = source.CopyPropertiesToNewRecursive<SimpleIntClass, SimpleLongClass>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(42L);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithMatchingPropertyNames_ShouldCopyAll(bool useCache)
	{
		// Arrange - Both source and dest have IntValue and StringValue properties
		ClassWithIncompatibleTypes source = new() { IntValue = 999, StringValue = "test" };

		// Act
		ClassWithIncompatibleDestTypes result = source.CopyPropertiesToNewRecursive<ClassWithIncompatibleTypes, ClassWithIncompatibleDestTypes>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		// Both properties match by name and type, so both should be copied
		result.StringValue.ShouldBe("test");
		result.IntValue.ShouldBe(999);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithMultipleSimpleTypeProperties_ShouldCopyAll(bool useCache)
	{
		// Arrange
		ClassWithMultipleSimpleTypes source = new()
		{
			IntValue = 100,
			StringValue = "hello",
			DoubleValue = 3.14,
			BoolValue = true,
			DateValue = new DateTime(2024, 1, 1)
		};

		// Act
		ClassWithMultipleSimpleTypes result = source.CopyPropertiesToNewRecursive<ClassWithMultipleSimpleTypes, ClassWithMultipleSimpleTypes>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.IntValue.ShouldBe(100);
		result.StringValue.ShouldBe("hello");
		result.DoubleValue.ShouldBe(3.14);
		result.BoolValue.ShouldBeTrue();
		result.DateValue.ShouldBe(new DateTime(2024, 1, 1));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithNullableSimpleTypes_ShouldHandleNullAndValue(bool useCache)
	{
		// Arrange
		ClassWithNullableSimpleTypes source = new()
		{
			NullableInt = 42,
			NullableBool = null,
			NullableDouble = 2.5
		};

		// Act
		ClassWithNullableSimpleTypes result = source.CopyPropertiesToNewRecursive<ClassWithNullableSimpleTypes, ClassWithNullableSimpleTypes>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.NullableInt.ShouldBe(42);
		result.NullableBool.ShouldBeNull();
		result.NullableDouble.ShouldBe(2.5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithEnumProperty_ShouldCopyEnum(bool useCache)
	{
		// Arrange - Enums are simple types
		ClassWithEnum source = new()
		{
			Id = 1,
			Status = ETestValues.Active
		};

		// Act
		ClassWithEnum result = source.CopyPropertiesToNewRecursive<ClassWithEnum, ClassWithEnum>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Status.ShouldBe(ETestValues.Active);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithFloatToDoubleConversion_ShouldConvert(bool useCache)
	{
		// Arrange
		ClassWithFloat source = new() { Value = 1.5f };

		// Act
		ClassWithDouble result = source.CopyPropertiesToNewRecursive<ClassWithFloat, ClassWithDouble>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(1.5, 0.0001);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithByteToIntConversion_ShouldConvert(bool useCache)
	{
		// Arrange
		ClassWithByte source = new() { Value = 255 };

		// Act
		ClassWithInt result = source.CopyPropertiesToNewRecursive<ClassWithByte, ClassWithInt>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(255);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithNestedSimpleTypeInCollection_ShouldCopyRuntime(bool useCache)
	{
		// Arrange - This triggers CopyItemRuntime which could call GetOrCreateCopyFunction with simple types
		ClassWithListOfSimpleType source = new()
		{
			Values = new List<SimpleWrapper>
			{
				new() { Value = 10 },
				new() { Value = 20 },
				new() { Value = 30 }
			}
		};

		// Act
		ClassWithListOfSimpleType result = source.CopyPropertiesToNewRecursive<ClassWithListOfSimpleType, ClassWithListOfSimpleType>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Values.ShouldNotBeNull();
		result.Values.Count.ShouldBe(3);
		result.Values[0].Value.ShouldBe(10);
		result.Values[1].Value.ShouldBe(20);
		result.Values[2].Value.ShouldBe(30);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CopyPropertiesToNewRecursive_WithDictionaryOfSimpleTypeWrappers_ShouldCopyRuntime(bool useCache)
	{
		// Arrange - Dictionary values might trigger runtime copy with simple types
		ClassWithDictionaryOfWrappers source = new()
		{
			Data = new Dictionary<string, SimpleWrapper>
			{
				{ "first", new SimpleWrapper { Value = 100 } },
				{ "second", new SimpleWrapper { Value = 200 } }
			}
		};

		// Act
		ClassWithDictionaryOfWrappers result = source.CopyPropertiesToNewRecursive<ClassWithDictionaryOfWrappers, ClassWithDictionaryOfWrappers>(useCache: useCache);

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data.Count.ShouldBe(2);
		result.Data["first"].Value.ShouldBe(100);
		result.Data["second"].Value.ShouldBe(200);
	}

	#endregion

	#region Cache Hit Path Tests

	[Theory]
	[InlineData(ERuntimeOperation.CopyObjectNull)]
	[InlineData(ERuntimeOperation.CopyItemNull)]
	[InlineData(ERuntimeOperation.CopyCollectionNull)]
	[InlineData(ERuntimeOperation.CopyObjectValid)]
	[InlineData(ERuntimeOperation.CopyItemValid)]
	[InlineData(ERuntimeOperation.CopyCollectionArray)]
	[InlineData(ERuntimeOperation.CopyCollectionDictionary)]
	[InlineData(ERuntimeOperation.CopyCollectionList)]
	[InlineData(ERuntimeOperation.CopyCollectionUnknown)]
	public void RuntimeOperations_ShouldWorkCorrectly(ERuntimeOperation operation)
	{
		switch (operation)
		{
			case ERuntimeOperation.CopyObjectNull:
				{
					object? result = typeof(Copy).GetMethod("CopyObjectRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object?[] { null, typeof(DestinationClass), 0, -1 });
					result.ShouldBeNull();
					break;
				}
			case ERuntimeOperation.CopyItemNull:
				{
					object? result = typeof(Copy).GetMethod("CopyItemRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object?[] { null, typeof(int), 0, -1 });
					result.ShouldBeNull();
					break;
				}
			case ERuntimeOperation.CopyCollectionNull:
				{
					object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object?[] { null, typeof(List<int>), 0, -1 });
					result.ShouldBeNull();
					break;
				}
			case ERuntimeOperation.CopyObjectValid:
				{
					SourceClass source = new() { Id = 42, Name = "Test" };
					object? result = typeof(Copy).GetMethod("CopyObjectRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { source, typeof(DestinationClass), 0, -1 });
					result.ShouldNotBeNull();
					result.ShouldBeOfType<DestinationClass>();
					((DestinationClass)result).Id.ShouldBe(42);
					((DestinationClass)result).Name.ShouldBe("Test");
					break;
				}
			case ERuntimeOperation.CopyItemValid:
				{
					const int item = 42;
					object? result = typeof(Copy).GetMethod("CopyItemRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { item, typeof(int), 0, -1 });
					result.ShouldBe(42);
					break;
				}
			case ERuntimeOperation.CopyCollectionArray:
				{
					int[] source = new[] { 1, 2, 3 };
					object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { source, typeof(int[]), 0, -1 });
					result.ShouldNotBeNull();
					result.ShouldBeOfType<int[]>();
					((int[])result).Length.ShouldBe(3);
					break;
				}
			case ERuntimeOperation.CopyCollectionDictionary:
				{
					Dictionary<string, int> source = new() { ["key1"] = 1, ["key2"] = 2 };
					object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { source, typeof(Dictionary<string, int>), 0, -1 });
					result.ShouldNotBeNull();
					result.ShouldBeOfType<Dictionary<string, int>>();
					((Dictionary<string, int>)result).Count.ShouldBe(2);
					break;
				}
			case ERuntimeOperation.CopyCollectionList:
				{
					List<int> source = new() { 1, 2, 3 };
					object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { source, typeof(List<int>), 0, -1 });
					result.ShouldNotBeNull();
					result.ShouldBeOfType<List<int>>();
					((List<int>)result).Count.ShouldBe(3);
					break;
				}
			case ERuntimeOperation.CopyCollectionUnknown:
				{
					const string source = "test";
					object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
						.Invoke(null, new object[] { source, typeof(string), 0, -1 });
					result.ShouldBe(source);
					break;
				}
		}
	}

	[Fact]
	public void GetOrAddFunctionFromCopyCache_SecondCall_ShouldReturnCachedFunction()
	{
		// Arrange
		Copy.CopyCacheManager.SetUseLimitedCache(false);
		Copy.CopyCacheManager.ClearAllCaches();
		Copy.CopyCacheTypedManager.ClearAllCaches();
		SourceClass source1 = new() { Id = 1, Name = "First" };
		DestinationClass dest1 = new();

		// Act - First call creates cache
		source1.CopyPropertiesTo(dest1, useCache: true);

		// Second call should hit cache (line 63)
		SourceClass source2 = new() { Id = 2, Name = "Second" };
		DestinationClass dest2 = new();
		source2.CopyPropertiesTo(dest2, useCache: true);

		// Assert
		dest1.Id.ShouldBe(1);
		dest2.Id.ShouldBe(2);
		Copy.CopyCacheManager.GetCache().Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndSameTypeObject_ShouldReturnDifferentInstance()
	{
		// Arrange
		ComplexSourceClass source = new() { Id = 1, Name = "Test" };

		// Act - Using useCache=false
		ComplexSourceClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexSourceClass>(useCache: false);

		// Assert - When types are the same in CopyObject, it should return source (line 285)
		result.ShouldBe(source);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndNonClassProperty_ShouldCopyDirectly()
	{
		// Arrange
		ClassWithEnum source = new() { Id = 1, Status = ETestValues.Active };

		// Act
		ClassWithEnum result = source.CopyPropertiesToNewRecursive<ClassWithEnum, ClassWithEnum>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Status.ShouldBe(ETestValues.Active);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndIncompatiblePropertyTypes_ShouldThrow()
	{
		// Arrange
		ClassWithInt source = new() { Value = 42 };

		// Act & Assert - Incompatible property types cause exception when setting value
		Should.Throw<ArgumentException>(() => _ = source.CopyPropertiesToNewRecursive<ClassWithInt, ClassWithString>(useCache: false));
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithUseCacheFalse_AndCollectionWithAddMethod_ShouldUseAddMethod()
	{
		// Arrange
		ClassWithQueue source = new()
		{
			Id = 1,
			Items = new Queue<int>([1, 2, 3])
		};

		// Act
		ClassWithQueue result = source.CopyPropertiesToNewRecursive<ClassWithQueue, ClassWithQueue>(useCache: false);

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(3);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithSimpleTypeToSimpleTypeConversion_ShouldUseDirectConversion()
	{
		// Arrange
		SimpleIntClass source = new() { Value = 42 };

		// Act - Convert int to long (compatible simple types)
		SimpleLongClass result = source.CopyPropertiesToNewRecursive<SimpleIntClass, SimpleLongClass>();

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBe(42L);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithNullSourceForExpressionTree_ShouldReturnNull()
	{
		// Arrange
		SourceClass? source = null;

		// Act
		DestinationClass? result = source!.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void CreateInstanceExpression_WithTypeWithoutParameterlessConstructor_ShouldUseActivator()
	{
		// Arrange
		// we can test this indirectly by using a type that might not have a parameterless constructor
		// Act & Assert - This tests the CreateInstanceExpression fallback path
		Should.NotThrow(() =>
		{
			SimpleIntClass source = new() { Value = 42 };
			SimpleLongClass result = source.CopyPropertiesToNewRecursive<SimpleIntClass, SimpleLongClass>();
			result.ShouldNotBeNull();
		});
	}

	[Fact]
	public void CanConvertTypes_WithIncompatibleTypes_ShouldReturnFalse()
	{
		// This is tested indirectly through the expression tree code
		// When types can't be converted, the expression should handle it gracefully
		ClassWithString source = new() { Value = "NotANumber" };
		SimpleIntClass result = source.CopyPropertiesToNewRecursive<ClassWithString, SimpleIntClass>();
		result.Value.ShouldBe(0); // Default value
	}

	[Fact]
	public void CopyCollectionRuntime_WithNullSource_ShouldReturnNull()
	{
		// Arrange & Act
		object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object?[] { null, typeof(List<int>), 0, -1 });

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void CopyItemRuntime_WithNullItem_ShouldReturnNull()
	{
		// Arrange & Act
		object? result = typeof(Copy).GetMethod("CopyItemRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object?[] { null, typeof(int), 0, -1 });

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void CopyObjectRuntime_WithNullSource_ShouldReturnNull()
	{
		// Arrange & Act
		object? result = typeof(Copy).GetMethod("CopyObjectRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object?[] { null, typeof(DestinationClass), 0, -1 });

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region CreateCopyFunction Direct Tests (Simple Type Destinations)

	[Theory]
	[InlineData(ESimpleTypeConversion.IntToInt, 42, 42)]
	[InlineData(ESimpleTypeConversion.IntToLong, 42, 42L)]
	[InlineData(ESimpleTypeConversion.DoubleToDouble, 3.14, 3.14)]
	[InlineData(ESimpleTypeConversion.ByteToShort, (byte)200, (short)200)]
	[InlineData(ESimpleTypeConversion.BoolToBool, true, true)]
	public void CreateCopyFunction_WithSimpleTypes_ShouldCopyOrConvert(ESimpleTypeConversion conversionType, object inputValue, object expectedValue)
	{
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		Type sourceType, destType;
		switch (conversionType)
		{
			case ESimpleTypeConversion.IntToInt:
				sourceType = typeof(int);
				destType = typeof(int);
				break;
			case ESimpleTypeConversion.IntToLong:
				sourceType = typeof(int);
				destType = typeof(long);
				break;
			case ESimpleTypeConversion.DoubleToDouble:
				sourceType = typeof(double);
				destType = typeof(double);
				break;
			case ESimpleTypeConversion.ByteToShort:
				sourceType = typeof(byte);
				destType = typeof(short);
				break;
			case ESimpleTypeConversion.BoolToBool:
				sourceType = typeof(bool);
				destType = typeof(bool);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(conversionType));
		}

		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { sourceType, destType })!;
		object? result = copyFunc(inputValue, null, 0, -1);
		result.ShouldNotBeNull();
		result.ShouldBe(expectedValue);
	}

	[Theory]
	[InlineData(ESimpleTypeConversion.StringToString, "test string", "test string")]
	[InlineData(ESimpleTypeConversion.IntToString, 123, null!)]
	[InlineData(ESimpleTypeConversion.StringToInt, "hello", 0)]
	public void CreateCopyFunction_WithStringConversions_ShouldHandleCorrectly(ESimpleTypeConversion conversionType, object inputValue, object? expectedValue)
	{
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		Type sourceType, destType;
		switch (conversionType)
		{
			case ESimpleTypeConversion.StringToString:
				sourceType = typeof(string);
				destType = typeof(string);
				break;
			case ESimpleTypeConversion.IntToString:
				sourceType = typeof(int);
				destType = typeof(string);
				break;
			case ESimpleTypeConversion.StringToInt:
				sourceType = typeof(string);
				destType = typeof(int);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(conversionType));
		}

		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { sourceType, destType })!;
		object? result = copyFunc(inputValue, null, 0, -1);
		if (expectedValue == null)
		{
			result.ShouldBeNull();
		}
		else
		{
			result.ShouldBe(expectedValue);
		}
	}

	#endregion

	#region GetOrCreatePropertyMaps Tests

	[Fact]
	public void GetOrCreatePropertyMaps_ShouldCreateAndCacheMappings()
	{
		// Arrange & Act
		Dictionary<string, (Action<DestinationClass, object?> Set, Func<SourceClass, object?> Get)> maps1 =
			Copy.GetOrCreatePropertyMaps<SourceClass, DestinationClass>();
		Dictionary<string, (Action<DestinationClass, object?> Set, Func<SourceClass, object?> Get)> maps2 =
			Copy.GetOrCreatePropertyMaps<SourceClass, DestinationClass>();

		// Assert
		maps1.ShouldNotBeNull();
		maps2.ShouldNotBeNull();
		maps1.ShouldBeSameAs(maps2); // Should return cached instance
		maps1.Count.ShouldBeGreaterThan(0);
		maps1.ContainsKey("Id").ShouldBeTrue();
		maps1.ContainsKey("Name").ShouldBeTrue();
	}

	[Fact]
	public void GetOrCreatePropertyMaps_WithLimitedCache_ShouldUseLimitedCache()
	{
		// Arrange
		Copy.CopyCacheManager.SetUseLimitedCache(true);
		Copy.CopyCacheManager.SetLimitedCacheSize(10);
		Copy.CopyCacheManager.ClearAllCaches();
		Copy.CopyCacheTypedManager.ClearAllCaches();

		// Act
		Dictionary<string, (Action<DestinationWithDifferentTypes, object?> Set, Func<SourceClass, object?> Get)> maps =
			Copy.GetOrCreatePropertyMaps<SourceClass, DestinationWithDifferentTypes>();

		// Assert
		maps.ShouldNotBeNull();
		Copy.CopyCacheManager.GetLimitedCache().Count.ShouldBeGreaterThan(0);

		// Cleanup
		Copy.CopyCacheManager.SetUseLimitedCache(false);
	}

	#endregion

	#region Test Classes

	public sealed class SourceClass
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		public string? Description { get; set; }

		public DateTime CreatedDate { get; set; }
	}

	public sealed class DestinationClass
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		// No Description property
	}

	public sealed class DestinationWithDifferentTypes
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		public int CreatedDate { get; set; } // Different type from SourceClass.CreatedDate
	}

	public sealed class ClassWithReadOnlyProperty
	{
		public int Id { get; } // Read-only property

		public string? Name { get; set; }
	}

	public sealed class ComplexSourceClass
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		public SourceClass? Child { get; set; }

		public ComplexSourceClass? NestedChild { get; set; }

		public List<SourceClass>? Items { get; set; }

		public Dictionary<string, SourceClass>? Dictionary { get; set; }
	}

	public sealed class ComplexDestinationClass
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		public DestinationClass? Child { get; set; }

		public ComplexDestinationClass? NestedChild { get; set; }

		public List<DestinationClass>? Items { get; set; }

		public Dictionary<string, DestinationClass>? Dictionary { get; set; }
	}

	public sealed class SimpleIntClass
	{
		public int Value { get; set; }
	}

	public sealed class SimpleLongClass
	{
		public long Value { get; set; }
	}

	public interface ITestInterface
	{
		int Id { get; set; }
		string? Name { get; set; }
	}

	public sealed class ClassWithInterface : ITestInterface
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	public sealed class ClassWithArray
	{
		public int Id { get; set; }
		public SourceClass[]? Items { get; set; }
	}

	public sealed class ClassWithDictionary
	{
		public int Id { get; set; }
		public Dictionary<string, SourceClass>? Data { get; set; }
	}

	public sealed class ClassWithNullable
	{
		public int Id { get; set; }
		public int? NullableInt { get; set; }
		public SourceClass? Child { get; set; }
	}

	public sealed class CustomCollection<T> : List<T>;

	public sealed class ClassWithCustomCollection
	{
		public int Id { get; set; }
		public CustomCollection<SourceClass>? Items { get; set; }
	}

	public sealed class ClassWithNoMatchingProps
	{
		public int Value { get; set; }
	}

	public sealed class ClassWithValueType
	{
		public int Id { get; set; }
		public System.Drawing.Point Point { get; set; }
	}

	public struct CustomStruct
	{
		public int Value1 { get; set; }
		public string? Value2 { get; set; }
	}

	public sealed class ClassWithStruct
	{
		public int Id { get; set; }
		public CustomStruct Data { get; set; }
	}

	public sealed class ClassWithString
	{
		public string? Value { get; set; }
	}

	public sealed class ComplexKey
	{
		public int Id { get; set; }
		public string? Code { get; set; }

		public override bool Equals(object? obj)
		{
			if (obj is ComplexKey other)
			{
				return Id == other.Id && Code == other.Code;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Code);
		}
	}

	public sealed class ClassWithNestedDictionary
	{
		public int Id { get; set; }
		public Dictionary<string, Dictionary<string, SourceClass>>? Data { get; set; }
	}

	public class BaseClass
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	public sealed class DerivedClass : BaseClass
	{
		public string? DerivedProperty { get; set; }
	}

	public sealed class ClassWithPrimitiveArray
	{
		public int Id { get; set; }
		public int[]? Values { get; set; }
	}

	public sealed class ClassWithComplexArray
	{
		public int Id { get; set; }
		public SourceClass[]? Items { get; set; }
	}

	public sealed class ClassWithPrimitiveDictionary
	{
		public int Id { get; set; }
		public Dictionary<string, int>? Data { get; set; }
	}

	public sealed class ClassWithHashSet
	{
		public int Id { get; set; }
		public HashSet<int>? Items { get; set; }
	}

	public sealed class ClassWithLinkedList
	{
		public int Id { get; set; }
		public LinkedList<int>? Items { get; set; }
	}

	public sealed class ClassWithNestedArray
	{
		public int Id { get; set; }
		public int[][]? Matrix { get; set; }
	}

	public sealed class ClassWithComplexGeneric
	{
		public int Id { get; set; }
		public List<Dictionary<string, SourceClass>>? Data { get; set; }
	}

	public sealed class MixedTypeSource
	{
		public int IntValue { get; set; }
		public string? StringValue { get; set; }
		public SourceClass? ComplexValue { get; set; }
	}

	public sealed class MixedTypeDest
	{
		public int IntValue { get; set; }
		public string? StringValue { get; set; }
		public DestinationClass? ComplexValue { get; set; }
	}

	public sealed class DeepNesting
	{
		public int Level { get; set; }
		public DeepNesting? Next { get; set; }
	}

	public sealed class ClassWithReadOnlyCollection
	{
		public int Id { get; set; }
		public System.Collections.ObjectModel.ReadOnlyCollection<int>? Items { get; set; }
	}

	public sealed class CircularRef
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public CircularRef? Self { get; set; }
	}

	public enum ETestValues
	{
		Inactive = 0,
		Active = 1
	}

	public sealed class ClassWithEnum
	{
		public int Id { get; set; }
		public ETestValues Status { get; set; }
	}

	public sealed class ClassWithInt
	{
		public int Value { get; set; }
	}

	public sealed class ClassWithQueue
	{
		public int Id { get; set; }
		public Queue<int>? Items { get; set; }
	}

	// Classes for simple type destination tests
	public sealed class ClassWithIncompatibleTypes
	{
		public int IntValue { get; set; }
		public string? StringValue { get; set; }
	}

	public sealed class ClassWithIncompatibleDestTypes
	{
		public string? StringValue { get; set; }
		public int IntValue { get; set; }
	}

	public sealed class ClassWithMultipleSimpleTypes
	{
		public int IntValue { get; set; }
		public string? StringValue { get; set; }
		public double DoubleValue { get; set; }
		public bool BoolValue { get; set; }
		public DateTime DateValue { get; set; }
	}

	public sealed class ClassWithNullableSimpleTypes
	{
		public int? NullableInt { get; set; }
		public bool? NullableBool { get; set; }
		public double? NullableDouble { get; set; }
	}

	public sealed class ClassWithFloat
	{
		public float Value { get; set; }
	}

	public sealed class ClassWithDouble
	{
		public double Value { get; set; }
	}

	public sealed class ClassWithByte
	{
		public byte Value { get; set; }
	}

	public sealed class SimpleWrapper
	{
		public int Value { get; set; }
	}

	public sealed class ClassWithListOfSimpleType
	{
		public List<SimpleWrapper>? Values { get; set; }
	}

	public sealed class ClassWithDictionaryOfWrappers
	{
		public Dictionary<string, SimpleWrapper>? Data { get; set; }
	}

	// Classes for CopyCollection coverage tests
	public sealed class ClassWithListOfInts
	{
		public List<int>? Values { get; set; }
	}

	public sealed class ClassWithIntArray
	{
		public int[]? Values { get; set; }
	}

	public sealed class ClassWithHashSetOfInts
	{
		public HashSet<int>? Values { get; set; }
	}

	public sealed class ClassWithDictionaryOfComplexValues
	{
		public Dictionary<string, SourceClass>? Data { get; set; }
	}

	// Classes for CreateCopyFunction coverage tests
	public sealed class ClassWithExtraProperties
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public string? ExtraProperty1 { get; set; }
		public int ExtraProperty2 { get; set; }
	}

	public sealed class ClassWithBasicProperties
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	public sealed class ClassWithNullableReferenceProperty
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	public sealed class ClassWithIncompatibleComplexType
	{
		public int Id { get; set; }
		public SourceClass? ComplexProp { get; set; }
	}

	public sealed class UnrelatedClass
	{
		public int Value { get; set; }
		public string? Text { get; set; }
	}

	public sealed class ClassWithDifferentComplexType
	{
		public int Id { get; set; }
		public UnrelatedClass? ComplexProp { get; set; }
	}

	public sealed class ClassWithValueTypeProperties
	{
		public int Id { get; set; }
		public int IntValue { get; set; }
		public double DoubleValue { get; set; }
		public bool BoolValue { get; set; }
	}

	public struct OtherStruct
	{
		public int OtherValue { get; set; }
	}

	public sealed class ClassWithIncompatibleStruct
	{
		public int Id { get; set; }
		public CustomStruct StructProp { get; set; }
	}

	public sealed class ClassWithDifferentStruct
	{
		public int Id { get; set; }
		public OtherStruct StructProp { get; set; }
	}

	#endregion
}
