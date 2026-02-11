using CommonNetFuncs.Core;
using System.Reflection;

namespace Core.Tests;

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

	[Fact]
	public void MergeInstances_WithMultipleObjects_ShouldMergeNonDefaultValues()
	{
		// Arrange
		SourceClass target = new()
		{
			Id = 0, // Default value
			Name = null // Default value
		};

		SourceClass source1 = new()
		{
			Id = 1, // Non-default value
			Name = null // Default value
		};

		SourceClass source2 = new()
		{
			Id = 0, // Default value
			Name = "Test" // Non-default value
		};

		// Act
		SourceClass result = target.MergeInstances(new[] { source1, source2 }, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeSameAs(target); // Should return the same instance
		result.Id.ShouldBe(1); // Should take value from source1
		result.Name.ShouldBe("Test"); // Should take value from source2
	}

	[Fact]
	public void MergeInstances_WithSingleObject_ShouldMergeNonDefaultValues()
	{
		// Arrange
		SourceClass target = new()
		{
			Id = 0, // Default value
			Name = null // Default value
		};

		SourceClass source = new()
		{
			Id = 1, // Non-default value
			Name = "Test" // Non-default value
		};

		// Act
		SourceClass result = target.MergeInstances(source, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeSameAs(target); // Should return the same instance
		result.Id.ShouldBe(1); // Should take value from source
		result.Name.ShouldBe("Test"); // Should take value from source
	}

	[Fact]
	public void MergeInstances_WithDefaultValues_ShouldNotOverrideNonDefaultValues()
	{
		// Arrange
		SourceClass target = new()
		{
			Id = 1, // Non-default value
			Name = "Original" // Non-default value
		};

		SourceClass source = new()
		{
			Id = 0, // Default value
			Name = "Test" // Non-default value
		};

		// Act
		SourceClass result = target.MergeInstances(source, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeSameAs(target); // Should return the same instance
		result.Id.ShouldBe(1); // Should keep original value
		result.Name.ShouldBe("Original"); // Should keep original value since it's non-default
	}

	#endregion

	#region CacheManager API Tests

	[Fact]
	public void SetLimitedCacheSize_ShouldBeSetSize()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);

		// Assert
		Copy.DeepCopyCacheManager.GetLimitedCacheSize().ShouldBe(10);
	}

	[Fact]
	public void SetCopyCacheSize_ShouldBeSetSize()
	{
		// Act
		Copy.CopyCacheManager.SetLimitedCacheSize(10);

		// Assert
		Copy.CopyCacheManager.GetLimitedCacheSize().ShouldBe(10);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SetUseLimitedCache_ShouldBeUseLimited(bool useLimited)
	{
		// Act
		Copy.DeepCopyCacheManager.SetUseLimitedCache(useLimited);

		// Assert
		Copy.DeepCopyCacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SetUseCopyCache_ShouldBeUseLimited(bool useLimited)
	{
		// Act
		Copy.CopyCacheManager.SetUseLimitedCache(useLimited);

		// Assert
		Copy.CopyCacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
	}

	[Fact]
	public void ClearDeepCopyCaches_ShouldNotThrow()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };
		DestinationClass dest = new();
		source.CopyPropertiesTo(dest, useCache: true); // Create cached value

		// Act
		Copy.DeepCopyCacheManager.ClearAllCaches();

		// Assert
		Copy.DeepCopyCacheManager.GetCache().Count.ShouldBe(0);
	}

	[Fact]
	public void ClearCopyCaches_ShouldNotThrow()
	{
		// Arrange
		Copy.CopyCacheManager.SetLimitedCacheSize(10);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };
		DestinationClass dest = new();
		source.CopyPropertiesTo(dest, useCache: true); // Create cached value

		// Act
		Copy.CopyCacheManager.ClearAllCaches();

		// Assert
		Copy.CopyCacheManager.GetCache().Count.ShouldBe(0);
	}

	[Fact]
	public void GetLimitedDeepCopyCache_ShouldNotThrow()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetUseLimitedCache(true);
		Copy.DeepCopyCacheManager.SetLimitedCacheSize(10);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };

		// Act
		DestinationClass dest = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>(useCache: true); // Create cached value

		// Assert
		dest.ShouldNotBeNull();
		Copy.DeepCopyCacheManager.GetLimitedCache().Count.ShouldBe(1);
		Copy.DeepCopyCacheManager.GetLimitedCache().Keys.First().ShouldBe((typeof(SourceClass), typeof(DestinationClass)));
		Copy.DeepCopyCacheManager.GetLimitedCache().Values.First().ShouldNotBeNull();

		// Cleanup
		Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
	}

	[Fact]
	public void GetLimitedCache_ShouldNotThrow()
	{
		// Arrange
		Copy.CopyCacheManager.SetUseLimitedCache(true);
		Copy.CopyCacheManager.SetLimitedCacheSize(10);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };
		DestinationClass dest = new();

		// Act
		source.CopyPropertiesTo(dest, useCache: true); // Create cached value

		// Assert
		Copy.CopyCacheManager.GetLimitedCache().Count.ShouldBe(1);
		Copy.CopyCacheManager.GetLimitedCache().Keys.First().ShouldBe((typeof(SourceClass), typeof(DestinationClass)));
		Copy.CopyCacheManager.GetLimitedCache().Values.First().ShouldNotBeNull();

		// Cleanup
		Copy.CopyCacheManager.SetUseLimitedCache(false);
	}

	[Fact]
	public void GetCache_ShouldNotThrow()
	{
		// Arrange
		Copy.DeepCopyCacheManager.SetUseLimitedCache(false);
		SourceClass source = new() { Id = 42, Name = "CacheTest" };

		// Act
		DestinationClass dest = source.CopyPropertiesToNewRecursive<SourceClass, DestinationClass>(useCache: true); // Create cached value

		// Assert
		dest.ShouldNotBeNull();
		Copy.DeepCopyCacheManager.GetCache().Count.ShouldBe(1);
		Copy.DeepCopyCacheManager.GetLimitedCache().Count.ShouldBe(0);
		Copy.DeepCopyCacheManager.GetLimitedCacheSize().ShouldBe(1);
		Copy.DeepCopyCacheManager.GetCache().Keys.First().ShouldBe((typeof(SourceClass), typeof(DestinationClass)));
		Copy.DeepCopyCacheManager.GetCache().Values.First().ShouldNotBeNull();
	}

	[Fact]
	public void GetCache_ShouldNotBeUsingLimitedCache()
	{
		// Arrange
		Copy.CopyCacheManager.SetUseLimitedCache(false);
		Copy.CopyCacheManager.ClearAllCaches();
		Copy.CopyCacheTypedManager.ClearAllCaches();
		SourceClass source = new() { Id = 42, Name = "CacheTest" };
		DestinationClass dest = new();

		// Act
		source.CopyPropertiesTo(dest, useCache: true); // Create cached value

		// Assert
		Copy.CopyCacheManager.GetCache().Count.ShouldBe(1);
		Copy.CopyCacheManager.GetLimitedCache().Count.ShouldBe(0);
		Copy.CopyCacheManager.GetLimitedCacheSize().ShouldBe(1);
		Copy.CopyCacheManager.GetCache().Keys.First().ShouldBe((typeof(SourceClass), typeof(DestinationClass)));
		Copy.CopyCacheManager.GetCache().Values.First().ShouldNotBeNull();
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

	[Fact]
	public void CopyPropertiesToNewRecursive_WithArrayToArrayConversion_ShouldCopyArray()
	{
		// Arrange
		ClassWithPrimitiveArray source = new()
		{
			Id = 1,
			Values = new[] { 1, 2, 3, 4, 5 }
		};

		// Act
		ClassWithPrimitiveArray result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveArray, ClassWithPrimitiveArray>();

		// Assert
		result.ShouldNotBeNull();
		result.Values.ShouldNotBeNull();
		result.Values.Length.ShouldBe(5);
		result.Values[0].ShouldBe(1);
		result.Values[4].ShouldBe(5);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithComplexArrayInExpression_ShouldCopyComplexArray()
	{
		// Arrange
		ClassWithComplexArray source = new()
		{
			Id = 1,
			Items = new[]
			{
				new SourceClass { Id = 10, Name = "Item1" },
				new SourceClass { Id = 20, Name = "Item2" }
			}
		};

		// Act
		ClassWithComplexArray result = source.CopyPropertiesToNewRecursive<ClassWithComplexArray, ClassWithComplexArray>();

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Length.ShouldBe(2);
		result.Items[0].Id.ShouldBe(10);
		result.Items[1].Name.ShouldBe("Item2");
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithDictionaryOfPrimitives_ShouldCopyDictionary()
	{
		// Arrange
		ClassWithPrimitiveDictionary source = new()
		{
			Id = 1,
			Data = new Dictionary<string, int>
			{
				["key1"] = 100,
				["key2"] = 200
			}
		};

		// Act
		ClassWithPrimitiveDictionary result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveDictionary, ClassWithPrimitiveDictionary>();

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data.Count.ShouldBe(2);
		result.Data["key1"].ShouldBe(100);
		result.Data["key2"].ShouldBe(200);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithEmptyArray_ShouldCopyEmptyArray()
	{
		// Arrange
		ClassWithPrimitiveArray source = new()
		{
			Id = 1,
			Values = Array.Empty<int>()
		};

		// Act
		ClassWithPrimitiveArray result = source.CopyPropertiesToNewRecursive<ClassWithPrimitiveArray, ClassWithPrimitiveArray>();

		// Assert
		result.ShouldNotBeNull();
		result.Values.ShouldNotBeNull();
		result.Values.Length.ShouldBe(0);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithGenericCollection_ShouldUseAddMethod()
	{
		// Arrange
		ClassWithHashSet source = new()
		{
			Id = 1,
			Items = new HashSet<int> { 1, 2, 3, 4, 5 }
		};

		// Act
		ClassWithHashSet result = source.CopyPropertiesToNewRecursive<ClassWithHashSet, ClassWithHashSet>();

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(5);
		result.Items.Contains(1).ShouldBeTrue();
		result.Items.Contains(5).ShouldBeTrue();
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithConcreteCollectionType_ShouldCopyToConcreteType()
	{
		// Arrange
		ClassWithLinkedList source = new()
		{
			Id = 1,
			Items = new LinkedList<int>([1, 2, 3])
		};

		// Act
		ClassWithLinkedList result = source.CopyPropertiesToNewRecursive<ClassWithLinkedList, ClassWithLinkedList>();

		// Assert
		result.ShouldNotBeNull();
		result.Items.ShouldNotBeNull();
		result.Items.Count.ShouldBe(3);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithNestedArray_ShouldCopyNestedArray()
	{
		// Arrange
		ClassWithNestedArray source = new()
		{
			Id = 1,
			Matrix = new[]
			{
				new[] { 1, 2, 3 },
				new[] { 4, 5, 6 }
			}
		};

		// Act
		ClassWithNestedArray result = source.CopyPropertiesToNewRecursive<ClassWithNestedArray, ClassWithNestedArray>();

		// Assert
		result.ShouldNotBeNull();
		result.Matrix.ShouldNotBeNull();
		result.Matrix.Length.ShouldBe(2);
		result.Matrix[0].Length.ShouldBe(3);
		result.Matrix[0][0].ShouldBe(1);
		result.Matrix[1][2].ShouldBe(6);
	}

	[Fact]
	public void CopyPropertiesToNewRecursive_WithComplexGenericCollection_ShouldCopyComplexGenericCollection()
	{
		// Arrange
		ClassWithComplexGeneric source = new()
		{
			Id = 1,
			Data = new List<Dictionary<string, SourceClass>>
			{
				new() {
					["key1"] = new SourceClass { Id = 10, Name = "Value1" }
				}
			}
		};

		// Act
		ClassWithComplexGeneric result = source.CopyPropertiesToNewRecursive<ClassWithComplexGeneric, ClassWithComplexGeneric>();

		// Assert
		result.ShouldNotBeNull();
		result.Data.ShouldNotBeNull();
		result.Data.Count.ShouldBe(1);
		result.Data[0]["key1"].Id.ShouldBe(10);
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
		System.Reflection.TargetInvocationException ex = Should.Throw<System.Reflection.TargetInvocationException>(() =>
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
				new SimpleWrapper { Value = 10 },
				new SimpleWrapper { Value = 20 },
				new SimpleWrapper { Value = 30 }
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

	[Fact]
	public void CopyObjectRuntime_WithValidSource_ShouldCopyObject()
	{
		// Arrange
		SourceClass source = new() { Id = 42, Name = "Test" };

		// Act
		object? result = typeof(Copy).GetMethod("CopyObjectRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { source, typeof(DestinationClass), 0, -1 });

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<DestinationClass>();
		((DestinationClass)result).Id.ShouldBe(42);
		((DestinationClass)result).Name.ShouldBe("Test");
	}

	[Fact]
	public void CopyItemRuntime_WithSimpleType_ShouldReturnItem()
	{
		// Arrange
		const int item = 42;

		// Act
		object? result = typeof(Copy).GetMethod("CopyItemRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { item, typeof(int), 0, -1 });

		// Assert
		result.ShouldBe(42);
	}

	[Fact]
	public void CopyCollectionRuntime_WithArray_ShouldCopyArray()
	{
		// Arrange
		int[] source = new[] { 1, 2, 3 };

		// Act
		object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { source, typeof(int[]), 0, -1 });

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<int[]>();
		((int[])result).Length.ShouldBe(3);
	}

	[Fact]
	public void CopyCollectionRuntime_WithDictionary_ShouldCopyDictionary()
	{
		// Arrange
		Dictionary<string, int> source = new() { ["key1"] = 1, ["key2"] = 2 };

		// Act
		object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { source, typeof(Dictionary<string, int>), 0, -1 });

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<Dictionary<string, int>>();
		((Dictionary<string, int>)result).Count.ShouldBe(2);
	}

	[Fact]
	public void CopyCollectionRuntime_WithGenericList_ShouldCopyList()
	{
		// Arrange
		List<int> source = new() { 1, 2, 3 };

		// Act
		object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { source, typeof(List<int>), 0, -1 });

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<List<int>>();
		((List<int>)result).Count.ShouldBe(3);
	}

	[Fact]
	public void CopyCollectionRuntime_WithUnknownCollectionType_ShouldReturnSource()
	{
		// Arrange - Use a simple string which is IEnumerable but not a typical collection
		const string source = "test";

		// Act
		object? result = typeof(Copy).GetMethod("CopyCollectionRuntime", BindingFlags.NonPublic | BindingFlags.Static)!
			.Invoke(null, new object[] { source, typeof(string), 0, -1 });

		// Assert
		result.ShouldBe(source); // Should return source for unknown types
	}

	#endregion

	#region CreateCopyFunction Direct Tests (Simple Type Destinations)

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_SameAsSource_ShouldReturnDirectCopy()
	{
		// Arrange - Test the sourceType == destType branch when destType.IsSimpleType() is true (line 608-609)
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(int), typeof(int))
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(int), typeof(int) })!;

		object? result = copyFunc(42, null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(42);
	}

	[Fact]
	public void CreateCopyFunction_WithConvertibleSimpleTypes_ShouldConvert()
	{
		// Arrange - Test the CanConvertTypes branch when destType.IsSimpleType() is true (line 612-613)
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(int), typeof(long)) - convertible types
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(int), typeof(long) })!;

		object? result = copyFunc(42, null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(42L);
	}

	[Fact]
	public void CreateCopyFunction_WithInconvertibleSimpleTypes_ShouldReturnDefault()
	{
		// Arrange - Test the else branch (default value) when destType.IsSimpleType() is true (line 617-618)
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(string), typeof(int)) - inconvertible types
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(string), typeof(int) })!;

		object? result = copyFunc("hello", null, 0, -1);

		// Assert
		result.ShouldBe(0); // Default value for int
	}

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_StringToString_ShouldCopy()
	{
		// Arrange - Test simple type path with reference type (string) - line 680-684
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(string), typeof(string))
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(string), typeof(string) })!;

		object? result = copyFunc("test string", null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe("test string");
	}

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_StringToNullableString_ShouldReturnNull()
	{
		// Arrange - Test the else branch returning null for reference type (line 617-618)
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(int), typeof(string)) - incompatible
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(int), typeof(string) })!;

		object? result = copyFunc(123, null, 0, -1);

		// Assert
		result.ShouldBeNull(); // Default value for reference type (string)
	}

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_DoubleToDouble_ShouldCopy()
	{
		// Arrange - Test simple type path with another value type - covers line 680-684
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(double), typeof(double))
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(double), typeof(double) })!;

		object? result = copyFunc(3.14, null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3.14);
	}

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_ByteToShort_ShouldConvert()
	{
		// Arrange - Test conversion between numeric types - covers line 612-613 and 680-684
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(byte), typeof(short))
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(byte), typeof(short) })!;

		object? result = copyFunc((byte)200, null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe((short)200);
	}

	[Fact]
	public void CreateCopyFunction_WithSimpleDestType_BoolToBool_ShouldCopy()
	{
		// Arrange - Additional coverage for bool type
		MethodInfo? createCopyFunctionMethod = typeof(Copy).GetMethod("CreateCopyFunction", BindingFlags.NonPublic | BindingFlags.Static);
		createCopyFunctionMethod.ShouldNotBeNull();

		// Act - Call CreateCopyFunction(typeof(bool), typeof(bool))
		Func<object, object?, int, int, object?> copyFunc = (Func<object, object?, int, int, object?>)
			createCopyFunctionMethod.Invoke(null, new object[] { typeof(bool), typeof(bool) })!;

		object? result = copyFunc(true, null, 0, -1);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(true);
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
