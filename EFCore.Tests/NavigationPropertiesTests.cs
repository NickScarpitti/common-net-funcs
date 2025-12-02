using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;

public sealed class NavigationPropertiesTests : IDisposable
{
	private readonly Fixture _fixture;
	private readonly TestDbContext _context;

	public NavigationPropertiesTests()
	{
		_fixture = new Fixture();
		DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(databaseName: _fixture.Create<string>()).Options;
		_context = new TestDbContext(options);
	}

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
				_context?.Dispose();
				NavigationProperties.NavigationCacheManager.SetUseLimitedCache(true);
				NavigationProperties.NavigationCacheManager.SetLimitedCacheSize(100);
				NavigationProperties.NavigationCacheManager.ClearAllCaches();

				NavigationProperties.TopLevelNavigationCacheManager.SetUseLimitedCache(true);
				NavigationProperties.TopLevelNavigationCacheManager.SetLimitedCacheSize(100);
				NavigationProperties.TopLevelNavigationCacheManager.ClearAllCaches();
			}
			disposed = true;
		}
	}

	~NavigationPropertiesTests()
	{
		Dispose(false);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(5)]
	public void GetNavigations_WithDifferentDepths_ReturnsCorrectNavigationPaths(int maxDepth)
	{
		// Arrange
		NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth);

		// Act
		HashSet<string> navigationPaths = NavigationProperties.GetNavigations<TestEntity>(_context, options);

		// Assert
		navigationPaths.ShouldNotBeEmpty();
		navigationPaths.All(path => path.Split('.').Length <= maxDepth + 1).ShouldBeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void GetNavigations_WithDifferentDepthsOneToMany_ReturnsCorrectNavigationPaths(int maxDepth)
	{
		// Arrange
		NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth);

		// Act
		HashSet<string> navigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, options);

		// Assert
		navigationPaths.ShouldNotBeEmpty();
		navigationPaths.All(path => path.Split('.').Length <= maxDepth + 1).ShouldBeTrue();
	}

	[Fact]
	public void GetNavigations_WithDifferentDepthsUsingCache_ReturnsCorrectNavigationPaths()
	{
		//Get depth of 1, then 3, then 1 again , and ensure the cache is used correctly and extra navigations are left out when depth is less than max cached depth
		// Arrange
		HashSet<string> originalNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 1));
		HashSet<string> deeperNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 3));

		// Act
		HashSet<string> testNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 1));

		// Assert
		testNavigationPaths.ShouldNotBeEmpty();
		testNavigationPaths.ShouldBeEquivalentTo(originalNavigationPaths);
		testNavigationPaths.All(path => path.Split('.').Length <= 2).ShouldBeTrue(); // Depth of 1 means only direct navigations
		deeperNavigationPaths.Any(path => path.Split('.').Length > 2).ShouldBeTrue(); // Should have longer navigations than testNavigationPaths
		deeperNavigationPaths.ShouldNotBeSameAs(testNavigationPaths.Count); // Deeper paths should have more navigations
		deeperNavigationPaths.ShouldNotBeSameAs(originalNavigationPaths.Count); // Deeper paths should have more navigations
		deeperNavigationPaths.Count.ShouldBeGreaterThan(testNavigationPaths.Count); // Deeper paths should have more navigations
		deeperNavigationPaths.Count.ShouldBeGreaterThan(originalNavigationPaths.Count); // Deeper paths should have more navigations
	}

	[Fact]
	public void GetNavigations_WithCaching_ReturnsSameResult()
	{
		// Arrange
		NavigationPropertiesOptions options = new(useCaching: true);

		// Act
		HashSet<string> firstCall = NavigationProperties.GetNavigations<TestEntity>(_context, options);
		HashSet<string> secondCall = NavigationProperties.GetNavigations<TestEntity>(_context, options);

		// Assert
		firstCall.ShouldBe(secondCall);
	}

	[Fact]
	public void GetTopLevelNavigations_ReturnsOnlyDirectNavigations()
	{
		// Act
		List<string> topLevelNavigations = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context);

		// Assert
		topLevelNavigations.ShouldNotBeEmpty();
		topLevelNavigations.All(nav => !nav.Contains('.')).ShouldBeTrue();
	}

	[Theory]
	[InlineData(typeof(JsonIgnoreAttribute))]
	[InlineData(typeof(XmlIgnoreAttribute))]
	public void GetTopLevelNavigations_WithIgnoredAttributes_ExcludesIgnoredProperties(Type attributeType)
	{
		// Arrange
		List<Type> ignoreList = [attributeType];

		// Act
		List<string> topLevelNavigations = NavigationProperties.GetTopLevelNavigations<TestEntityWithIgnores>(_context, ignoreList);

		// Assert
		topLevelNavigations.ShouldNotContain("IgnoredNavigation");
	}

	[Fact]
	public void IncludeNavigationProperties_AddsIncludeStatementsToQuery()
	{
		// Arrange
		IQueryable<TestEntity> query = _context.TestEntities;

		// Act
		IQueryable<TestEntity> result = query.IncludeNavigationProperties(_context);

		// Assert
		result.Expression.ToString().ShouldContain("Include");
	}

	[Fact]
	public void RemoveNavigationProperties_SetsAllNavigationsToNull()
	{
		// Arrange
		TestEntity entity = new()
		{
			Id = 1,
			RelatedEntity = new TestRelatedEntity { Id = 2 }
		};

		// Act
		entity.RemoveNavigationProperties(_context);

		// Assert
		entity.RelatedEntity.ShouldBeNull();
	}

	// Test entities
	private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
	{
		public DbSet<TestEntity> TestEntities => Set<TestEntity>();

#pragma warning disable S1144 // Unused private types or members should be removed
		public DbSet<TestRelatedEntity> TestRelatedEntities => Set<TestRelatedEntity>();

		public DbSet<DeepTestEntity> DeepTestEntities => Set<DeepTestEntity>();

		public DbSet<DeepTestRelatedEntity> DeepTestRelatedEntities => Set<DeepTestRelatedEntity>();

		public DbSet<DeepTestRelatedEntity2> DeepTestRelatedEntity2s => Set<DeepTestRelatedEntity2>();
#pragma warning restore S1144 // Unused private types or members should be removed

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TestEntity>()
					.HasOne(x => x.RelatedEntity)
					.WithMany();

			modelBuilder.Entity<TestRelatedEntity>()
					.HasOne(x => x.Parent)
					.WithMany();

			modelBuilder.Entity<DeepTestEntity>()
					.HasOne(x => x.DeepTestRelatedEntity)
					.WithMany(x => x.TestEntities);

			modelBuilder.Entity<DeepTestRelatedEntity>()
					.HasOne(x => x.GetDeepTestRelatedEntity2)
					.WithMany(x => x.DeepTestRelatedEntities);

			modelBuilder.Entity<DeepTestRelatedEntity2>()
					.HasOne(x => x.DeepTestRelatedEntity3)
					.WithMany(x => x.DeepTestRelatedEntity2s);
		}
	}

	private sealed class TestEntity
	{
		public int Id { get; set; }

		public TestRelatedEntity? RelatedEntity { get; set; }
	}

	private sealed class TestRelatedEntity
	{
		public int Id { get; set; }

#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
		public TestEntity? Parent { get; set; }
	}

	private sealed class TestEntityWithIgnores
	{
		public int Id { get; set; }

		[JsonIgnore]
		public TestRelatedEntity? IgnoredNavigation { get; set; }

		public TestRelatedEntity? NormalNavigation { get; set; }
	}

	private sealed class DeepTestEntity
	{
		public int Id { get; set; }

		public DeepTestRelatedEntity? DeepTestRelatedEntity { get; set; }
	}

	private class DeepTestRelatedEntity
	{
		public int Id { get; set; }

		public DeepTestRelatedEntity2? GetDeepTestRelatedEntity2 { get; set; }

		[JsonIgnore]
		public virtual ICollection<DeepTestEntity>? TestEntities { get; set; }
	}

	private class DeepTestRelatedEntity2
	{
		public int Id { get; set; }

		public DeepTestRelatedEntity3? DeepTestRelatedEntity3 { get; set; }

		[JsonIgnore]
		public virtual ICollection<DeepTestRelatedEntity>? DeepTestRelatedEntities { get; set; }
	}

	private class DeepTestRelatedEntity3
	{
		public int Id { get; set; }

		[JsonIgnore]
		public virtual ICollection<DeepTestRelatedEntity2>? DeepTestRelatedEntity2s { get; set; }
	}
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed

	[Theory]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(false, 5)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	[InlineData(true, 5)]
	public void GetNavigations_CacheModes_ReturnsCorrectNavigationPaths(bool useLimitedCache, int maxDepth)
	{
		// Arrange
		NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth, useCaching: true);

		// Switch cache mode
		if (useLimitedCache)
		{
			NavigationProperties.NavigationCacheManager.SetLimitedCacheSize(10);
		}
		else
		{
			NavigationProperties.NavigationCacheManager.SetUseLimitedCache(false);
		}

		// Act
		HashSet<string> navigationPaths = NavigationProperties.GetNavigations<TestEntity>(_context, options);

		// Assert
		navigationPaths.ShouldNotBeEmpty();
		navigationPaths.All(path => path.Split('.').Length <= maxDepth + 1).ShouldBeTrue();

		// Check cache is populated
		IReadOnlyDictionary<NavigationProperties.NavigationProperiesCacheKey, NavigationProperties.NavigationProperiesCacheValue> cache = useLimitedCache
				? NavigationProperties.NavigationCacheManager.GetLimitedCache()
				: NavigationProperties.NavigationCacheManager.GetCache();
		cache.Count.ShouldBeGreaterThan(0);

		// Clear cache and verify
		if (useLimitedCache)
		{
			NavigationProperties.NavigationCacheManager.ClearLimitedCache();
			NavigationProperties.NavigationCacheManager.GetLimitedCache().Count.ShouldBe(0);
		}
		else
		{
			NavigationProperties.NavigationCacheManager.ClearCache();
			NavigationProperties.NavigationCacheManager.GetCache().Count.ShouldBe(0);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void GetTopLevelNavigations_CacheModes_ReturnsOnlyDirectNavigations(bool useLimitedCache)
	{
		// Arrange
		if (useLimitedCache)
		{
			NavigationProperties.TopLevelNavigationCacheManager.SetLimitedCacheSize(10);
		}
		else
		{
			NavigationProperties.TopLevelNavigationCacheManager.SetUseLimitedCache(false);
		}

		// Act
		List<string> topLevelNavigations = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context);

		// Assert
		topLevelNavigations.ShouldNotBeEmpty();
		topLevelNavigations.All(nav => !nav.Contains('.')).ShouldBeTrue();

		// Check cache is populated
		IReadOnlyDictionary<Type, List<string>> cache = useLimitedCache
				? NavigationProperties.TopLevelNavigationCacheManager.GetLimitedCache()!
				: NavigationProperties.TopLevelNavigationCacheManager.GetCache();
		cache.Count.ShouldBeGreaterThan(0);

		// Clear cache and verify
		if (useLimitedCache)
		{
			NavigationProperties.TopLevelNavigationCacheManager.ClearLimitedCache();
			NavigationProperties.TopLevelNavigationCacheManager.GetLimitedCache().Count.ShouldBe(0);
		}
		else
		{
			NavigationProperties.TopLevelNavigationCacheManager.ClearCache();
			NavigationProperties.TopLevelNavigationCacheManager.GetCache().Count.ShouldBe(0);
		}
	}

	[Fact]
	public void NavigationCacheManager_UseLimitedCache_RespectsSizeLimit()
	{
		// Arrange
		NavigationProperties.NavigationCacheManager.SetLimitedCacheSize(1);

		// Act
		NavigationPropertiesOptions options1 = new(maxNavigationDepth: 1, useCaching: true);
		NavigationPropertiesOptions options2 = new(maxNavigationDepth: 2, useCaching: true);

		_ = NavigationProperties.GetNavigations<TestEntity>(_context, options1);
		_ = NavigationProperties.GetNavigations<TestEntity>(_context, options2);

		// Assert
		IReadOnlyDictionary<NavigationProperties.NavigationProperiesCacheKey, NavigationProperties.NavigationProperiesCacheValue> limitedCache = NavigationProperties.NavigationCacheManager.GetLimitedCache();
		limitedCache.Count.ShouldBe(1); // Should not exceed limit

		// Add another and check eviction
		NavigationPropertiesOptions options3 = new(maxNavigationDepth: 3, useCaching: true);
		_ = NavigationProperties.GetNavigations<TestEntity>(_context, options3);
		limitedCache.Count.ShouldBe(1); // Still 1 due to limit
	}

	[Fact]
	public void TopLevelNavigationCacheManager_UseLimitedCache_RespectsSizeLimit()
	{
		// Arrange
		NavigationProperties.TopLevelNavigationCacheManager.SetLimitedCacheSize(1);

		// Act
		_ = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context);
		_ = NavigationProperties.GetTopLevelNavigations<DeepTestEntity>(_context);

		// Assert
		IReadOnlyDictionary<Type, List<string>?> limitedCache = NavigationProperties.TopLevelNavigationCacheManager.GetLimitedCache();
		limitedCache.Count.ShouldBe(1); // Should not exceed limit

		// Add another and check eviction
		_ = NavigationProperties.GetTopLevelNavigations<TestEntityWithIgnores>(_context);
		limitedCache.Count.ShouldBe(1); // Still 1 due to limit
	}

	[Fact]
	public void Constructor_DefaultValues_SetsCorrectDefaults()
	{
		// Act
		NavigationPropertiesOptions options = new();

		// Assert
		options.MaxNavigationDepth.ShouldBe(100);
		options.NavPropAttributesToIgnore.ShouldBeNull();
		options.UseCaching.ShouldBe(true);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(50)]
	[InlineData(int.MaxValue)]
	public void Constructor_WithMaxDepth_SetsCorrectValue(int maxDepth)
	{
		// Act
		NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth);

		// Assert
		options.MaxNavigationDepth.ShouldBe(maxDepth);
	}

	[Fact]
	public void Constructor_WithAttributesList_SetsCorrectValue()
	{
		// Arrange
		List<Type> attributes = [typeof(System.Text.Json.Serialization.JsonIgnoreAttribute)];

		// Act
		NavigationPropertiesOptions options = new(navPropAttributesToIgnore: attributes);

		// Assert
		options.NavPropAttributesToIgnore.ShouldBe(attributes);
		options.NavPropAttributesToIgnore!.Count.ShouldBe(1);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithUseCaching_SetsCorrectValue(bool useCaching)
	{
		// Act
		NavigationPropertiesOptions options = new(useCaching: useCaching);

		// Assert
		options.UseCaching.ShouldBe(useCaching);
	}

	[Fact]
	public void Constructor_WithAllParameters_SetsCorrectValues()
	{
		// Arrange
		const int maxDepth = 5;
		List<Type> attributes = [typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), typeof(Newtonsoft.Json.JsonIgnoreAttribute)];
		const bool useCaching = false;

		// Act
		NavigationPropertiesOptions options = new(maxDepth, attributes, useCaching);

		// Assert
		options.MaxNavigationDepth.ShouldBe(maxDepth);
		options.NavPropAttributesToIgnore.ShouldBe(attributes);
		options.NavPropAttributesToIgnore!.Count.ShouldBe(2);
		options.UseCaching.ShouldBe(useCaching);
	}

	[Fact]
	public void MaxNavigationDepth_SetAndGet_WorksCorrectly()
	{
		// Arrange
		NavigationPropertiesOptions options = new()
		{
			// Act
			MaxNavigationDepth = 10
		};

		// Assert
		options.MaxNavigationDepth.ShouldBe(10);
	}

	[Fact]
	public void NavPropAttributesToIgnore_SetAndGet_WorksCorrectly()
	{
		// Arrange
		NavigationPropertiesOptions options = new();
		List<Type> attributes = [typeof(System.Xml.Serialization.XmlIgnoreAttribute)];

		// Act
		options.NavPropAttributesToIgnore = attributes;

		// Assert
		options.NavPropAttributesToIgnore.ShouldBe(attributes);
	}

	[Fact]
	public void UseCaching_SetAndGet_WorksCorrectly()
	{
		// Arrange
		NavigationPropertiesOptions options = new()
		{
			// Act
			UseCaching = false
		};

		// Assert
		options.UseCaching.ShouldBe(false);
	}

	[Fact]
	public void Constructor_WithNegativeMaxDepth_AllowsNegativeValue()
	{
		// Act
		NavigationPropertiesOptions options = new(maxNavigationDepth: -1);

		// Assert
		options.MaxNavigationDepth.ShouldBe(-1);
	}

	[Fact]
	public void NavPropAttributesToIgnore_WithEmptyList_SetsEmptyList()
	{
		// Arrange
		List<Type> emptyList = [];

		// Act
		NavigationPropertiesOptions options = new(navPropAttributesToIgnore: emptyList);

		// Assert
		options.NavPropAttributesToIgnore.ShouldNotBeNull();
		options.NavPropAttributesToIgnore.ShouldBeEmpty();
	}

	#region NavigationProperiesCacheKey and CacheValue Tests

	[Fact]
	public void NavigationProperiesCacheKey_Equality_WorksCorrectly()
	{
		// Arrange
		NavigationProperties.NavigationProperiesCacheKey key1 = new(typeof(TestEntity), "attr1");
		NavigationProperties.NavigationProperiesCacheKey key2 = new(typeof(TestEntity), "attr1");
		NavigationProperties.NavigationProperiesCacheKey key3 = new(typeof(TestRelatedEntity), "attr1");
		NavigationProperties.NavigationProperiesCacheKey key4 = new(typeof(TestEntity), "attr2");

		// Act & Assert
		key1.Equals(key2).ShouldBeTrue();
		key1.Equals(key3).ShouldBeFalse();
		key1.Equals(key4).ShouldBeFalse();
		(key1 == key2).ShouldBeTrue();
		(key1 != key3).ShouldBeTrue();
	}

	[Fact]
	public void NavigationProperiesCacheKey_GetHashCode_ReturnsConsistentValue()
	{
		// Arrange
		NavigationProperties.NavigationProperiesCacheKey key1 = new(typeof(TestEntity), "attr1");
		NavigationProperties.NavigationProperiesCacheKey key2 = new(typeof(TestEntity), "attr1");

		// Act
		int hash1 = key1.GetHashCode();
		int hash2 = key2.GetHashCode();

		// Assert
		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void NavigationProperiesCacheKey_EqualsObject_WorksCorrectly()
	{
		// Arrange
		NavigationProperties.NavigationProperiesCacheKey key1 = new(typeof(TestEntity), "attr1");
		object key2 = new NavigationProperties.NavigationProperiesCacheKey(typeof(TestEntity), "attr1");
		object notKey = "not a key";

		// Act & Assert
		key1.Equals(key2).ShouldBeTrue();
		key1.Equals(notKey).ShouldBeFalse();
	}

	[Fact]
	public void NavigationProperiesCacheValue_Equality_WorksCorrectly()
	{
		// Arrange
		HashSet<string> navs1 = ["prop1", "prop2"];
		HashSet<string> navs2 = ["prop1", "prop2"];
		HashSet<string> navs3 = ["prop1", "prop3"];

		NavigationProperties.NavigationProperiesCacheValue value1 = new(navs1, 5);
		NavigationProperties.NavigationProperiesCacheValue value2 = new(navs2, 5);
		NavigationProperties.NavigationProperiesCacheValue value3 = new(navs3, 5);
		NavigationProperties.NavigationProperiesCacheValue value4 = new(navs1, 10);

		// Act & Assert
		value1.Equals(value2).ShouldBeTrue();
		value1.Equals(value3).ShouldBeFalse();
		value1.Equals(value4).ShouldBeFalse();
		(value1 == value2).ShouldBeTrue();
		(value1 != value3).ShouldBeTrue();
	}

	[Fact]
	public void NavigationProperiesCacheValue_GetHashCode_ReturnsConsistentValue()
	{
		// Arrange
		HashSet<string> navs1 = ["prop1", "prop2"];
		HashSet<string> navs2 = ["prop1", "prop2"];

		NavigationProperties.NavigationProperiesCacheValue value1 = new(navs1, 5);
		NavigationProperties.NavigationProperiesCacheValue value2 = new(navs2, 5);

		// Act
		int hash1 = value1.GetHashCode();
		int hash2 = value2.GetHashCode();

		// Assert
		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void NavigationProperiesCacheValue_EqualsObject_WorksCorrectly()
	{
		// Arrange
		HashSet<string> navs = ["prop1", "prop2"];
		NavigationProperties.NavigationProperiesCacheValue value1 = new(navs, 5);
		object value2 = new NavigationProperties.NavigationProperiesCacheValue(new HashSet<string>(navs), 5);
		object notValue = "not a value";

		// Act & Assert
		value1.Equals(value2).ShouldBeTrue();
		value1.Equals(notValue).ShouldBeFalse();
	}

	[Fact]
	public void NavigationProperiesCacheValue_GetNavigationsToDepth_ReturnsCorrectSubset()
	{
		// Arrange
		HashSet<string> navs = ["prop1", "prop1.prop2", "prop1.prop2.prop3", "prop4"];
		NavigationProperties.NavigationProperiesCacheValue value = new(navs, 10);

		// Act
		HashSet<string> depth0 = value.GetNavigationsToDepth(0);
		HashSet<string> depth1 = value.GetNavigationsToDepth(1);
		HashSet<string> depth2 = value.GetNavigationsToDepth(2);
		HashSet<string> depthNegative = value.GetNavigationsToDepth(-1);

		// Assert
		depth0.Count.ShouldBe(2); // prop1, prop4
		depth1.Count.ShouldBe(3); // prop1, prop1.prop2, prop4
		depth2.Count.ShouldBe(4); // All
		depthNegative.Count.ShouldBe(4); // All when depth is negative
	}

	#endregion

	#region RemoveNavigationProperties Tests

	[Fact]
	public void RemoveNavigationProperties_WithNullObject_DoesNotThrow()
	{
		// Arrange
		TestEntity? entity = null;

		// Act & Assert
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
		Should.NotThrow(() => entity.RemoveNavigationProperties(_context));
#pragma warning restore CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
	}

	[Fact]
	public void RemoveNavigationProperties_WithNoWritableNavigations_HandlesCorrectly()
	{
		// Arrange
		TestEntityWithReadOnlyNav entity = new() { Id = 1 };

		// Act
		entity.RemoveNavigationProperties(_context);

		// Assert - should not throw
		entity.Id.ShouldBe(1);
	}

	#endregion

	#region GetNavigations Non-Caching Tests

	[Fact]
	public void GetNavigations_WithoutCaching_ReturnsNavigations()
	{
		// Arrange
		NavigationPropertiesOptions options = new(useCaching: false);

		// Act
		HashSet<string> result1 = NavigationProperties.GetNavigations<TestEntity>(_context, options);
		HashSet<string> result2 = NavigationProperties.GetNavigations<TestEntity>(_context, options);

		// Assert
		result1.ShouldNotBeEmpty();
		result2.ShouldNotBeEmpty();
		// Results should be equivalent but not the same instance
		result1.ShouldBeEquivalentTo(result2);
	}

	[Fact]
	public void GetTopLevelNavigations_WithoutCaching_ReturnsNavigations()
	{
		// Arrange & Act
		List<string> result1 = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context, useCaching: false);
		List<string> result2 = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context, useCaching: false);

		// Assert
		result1.ShouldNotBeEmpty();
		result2.ShouldNotBeEmpty();
		result1.ShouldBeEquivalentTo(result2);
	}

	#endregion

	#region IncludeNavigationProperties Tests

	[Fact]
	public void IncludeNavigationProperties_WithCustomOptions_AddsIncludes()
	{
		// Arrange
		IQueryable<TestEntity> query = _context.TestEntities;
		NavigationPropertiesOptions options = new(maxNavigationDepth: 1, useCaching: false);

		// Act
		IQueryable<TestEntity> result = query.IncludeNavigationProperties(_context, options);

		// Assert
		result.Expression.ToString().ShouldContain("Include");
	}

	#endregion

	#region Entity with ReadOnly Navigation for Testing

#pragma warning disable S1144 // Unused private types or members should be removeds
	private sealed class TestEntityWithReadOnlyNav
	{
		public int Id { get; set; }
		public TestRelatedEntity? ReadOnlyNav { get; init; } // Init-only property
#pragma warning restore S1144 // Unused private types or members should be removed
	}

	#endregion
}
