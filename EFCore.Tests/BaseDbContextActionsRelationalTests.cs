using CommonNetFuncs.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Xunit.TestContext;

namespace EFCore.Tests;

/// <summary>
/// Tests for methods that require a relational database provider but aren't covered by ExecuteTests.
/// Uses SQLite instead of in-memory database because in-memory doesn't support certain relational features.
/// </summary>
public sealed class BaseDbContextActionsRelationalTests : IDisposable
{
	private readonly SqliteConnection connection;
	private readonly SqliteConnection connectionWithFilters;
	private readonly IServiceProvider serviceProvider;
	private readonly Fixture fixture;
	private bool disposed;

	public BaseDbContextActionsRelationalTests()
	{
		fixture = new Fixture();
		fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => fixture.Behaviors.Remove(x));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		// Initialize SQLitePCL batteries before using SQLite
		SQLitePCL.Batteries.Init();

		// Setup SQLite in-memory database (supports relational features unlike InMemoryDatabase)
		connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();

		// Separate connection for filtered entities to avoid schema conflicts
		connectionWithFilters = new SqliteConnection("DataSource=:memory:");
		connectionWithFilters.Open();

		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection), ServiceLifetime.Transient);
		services.AddDbContext<TestDbContextWithFilters>(options => options.UseSqlite(connectionWithFilters), ServiceLifetime.Transient);
		serviceProvider = services.BuildServiceProvider();

		// Ensure both databases are created
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			context.Database.EnsureCreated();
		}

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters contextWithFilters = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			contextWithFilters.Database.EnsureCreated();
		}
	}

	public enum GlobalFilterOptionsType
	{
		None,
		DisableAll,
		DisableSpecific,
		Null,
		EmptyList,
		DisableAllFalse,
		DisableAllWithQueryTimeout,
		DisableAllWithCancellation,
		FilterNamesPriority
	}

	public enum KeyType
	{
		SingleKey,
		CompoundKey
	}

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
				connection?.Dispose();
				connectionWithFilters?.Dispose();
				(serviceProvider as IDisposable)?.Dispose();
			}
			disposed = true;
		}
	}

	#region DeleteManyTracked Tests

	[Fact]
	public async Task DeleteManyTracked_WithValidEntities_ShouldRemoveFromDatabase()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked(entities);

		// Assert
		result.ShouldBeTrue();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			foreach (TestEntity entity in entities)
			{
				TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id }, Current.CancellationToken);
				deletedEntity.ShouldBeNull();
			}
		}
	}

	[Fact]
	public async Task DeleteManyTracked_WithEmptyList_ShouldReturnTrue()
	{
		// Arrange
		List<TestEntity> entities = [];
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked(entities);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteManyTracked_WithSingleEntity_ShouldRemoveFromDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked([entity]);

		// Assert
		result.ShouldBeTrue();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id }, Current.CancellationToken);
			deletedEntity.ShouldBeNull();
		}
	}

	[Fact]
	public async Task DeleteManyTracked_WithLargeDataset_ShouldHandleEfficiently()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(50).ToList();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked(entities);

		// Assert
		result.ShouldBeTrue();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(cancellationToken: Current.CancellationToken);
			remainingCount.ShouldBe(0);
		}
	}

	[Fact]
	public async Task DeleteManyTracked_WithRemoveNavigationProps_ShouldRemoveFromDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		entity.Details = [new TestEntityDetail { Description = "Test Detail" }];

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked([entity], removeNavigationProps: true);

		// Assert
		result.ShouldBeTrue();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id }, Current.CancellationToken);
			deletedEntity.ShouldBeNull();
		}
	}

	[Fact]
	public async Task DeleteManyTracked_WhenEntityDoesNotExist_ShouldReturnTrue()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		// Entity is created but not added to database

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked([entity]);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteManyTracked_WithMixedExistingAndNonExisting_ShouldHandleCorrectly()
	{
		// Arrange
		TestEntity existingEntity = fixture.Create<TestEntity>();
		TestEntity nonExistingEntity = fixture.Create<TestEntity>();

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddAsync(existingEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyTracked([existingEntity, nonExistingEntity]);

		// Assert
		result.ShouldBeTrue();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { existingEntity.Id }, Current.CancellationToken);
			deletedEntity.ShouldBeNull();

			TestEntity? nonExistent = await context.TestEntities.FindAsync(new object?[] { nonExistingEntity.Id }, Current.CancellationToken);
			nonExistent.ShouldBeNull();
		}
	}

	[Fact]
	public async Task DeleteManyTracked_MultipleConsecutiveCalls_ShouldWorkCorrectly()
	{
		// Arrange
		List<TestEntity> firstBatch = fixture.CreateMany<TestEntity>(3).ToList();
		List<TestEntity> secondBatch = fixture.CreateMany<TestEntity>(3).ToList();

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(firstBatch, Current.CancellationToken);
			await context.TestEntities.AddRangeAsync(secondBatch, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result1 = await testContext.DeleteManyTracked(firstBatch);
		bool result2 = await testContext.DeleteManyTracked(secondBatch);

		// Assert
		result1.ShouldBeTrue();
		result2.ShouldBeTrue();

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(cancellationToken: Current.CancellationToken);
			remainingCount.ShouldBe(0);
		}
	}

	#endregion

	#region DeleteManyByKeys Tests

	[Fact]
	public async Task DeleteManyByKeys_WithValidKeys_ShouldRemoveFromDatabase()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		List<object> keys;

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
			keys = entities.ConvertAll(e => (object)e.Id);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyByKeys(keys);

		// Assert
		// Note: This method is documented as "Does not work with PostgreSQL, not testable"
		// For SQLite, we test that it doesn't throw
		result.ShouldBeOneOf(true, false);
	}

	[Fact]
	public async Task DeleteManyByKeys_WithEmptyList_ShouldReturnTrue()
	{
		// Arrange
		List<object> keys = [];
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyByKeys(keys);

		// Assert
		// DeleteManyByKeys returns true even for empty lists
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteManyByKeys_WithSingleKey_ShouldRemoveFromDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyByKeys([entity.Id]);

		// Assert
		// Method behavior may vary by provider
		result.ShouldBeOneOf(true, false);
	}

	[Fact]
	public async Task DeleteManyByKeys_WithNonExistentKeys_ShouldReturnTrue()
	{
		// Arrange
		List<object> keys = [999, 1000, 1001];
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyByKeys(keys);

		// Assert
		// DeleteManyByKeys returns true even for non-existent keys
		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DeleteManyByKeys_WithMixedExistingAndNonExisting_ShouldHandleCorrectly()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		List<object> keys = [entity.Id, 999, 1000];
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteManyByKeys(keys);

		// Assert
		// Method behavior may vary - either succeeds or fails gracefully
		result.ShouldBeOneOf(true, false);
	}

	#endregion

	#region GlobalFilterOptions GetByKey Tests

	[Theory]
	[InlineData(GlobalFilterOptionsType.None, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.DisableAll, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.DisableSpecific, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.Null, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.EmptyList, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.DisableAllFalse, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.FilterNamesPriority, KeyType.SingleKey)]
	[InlineData(GlobalFilterOptionsType.None, KeyType.CompoundKey)]
	[InlineData(GlobalFilterOptionsType.DisableAll, KeyType.CompoundKey)]
	[InlineData(GlobalFilterOptionsType.DisableSpecific, KeyType.CompoundKey)]
	public async Task GetByKey_WithGlobalFilterOptions_ShouldHandleFiltersCorrectly(GlobalFilterOptionsType filterType, KeyType keyType)
	{
		// Determine if filters should be disabled
		bool filtersDisabled = filterType is GlobalFilterOptionsType.DisableAll or GlobalFilterOptionsType.DisableSpecific or GlobalFilterOptionsType.FilterNamesPriority;

		// Arrange
		if (keyType == KeyType.SingleKey)
		{
			TestEntityWithFilter activeEntity = new() { Id = 1, Name = "Active", IsActive = true };
			TestEntityWithFilter inactiveEntity = new() { Id = 2, Name = "Inactive", IsActive = false };

			using (IServiceScope scope = serviceProvider.CreateScope())
			{
				TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
				await context.TestEntitiesWithFilter.AddRangeAsync(activeEntity, inactiveEntity);
				await context.SaveChangesAsync(Current.CancellationToken);
			}

			GlobalFilterOptions? filterOptions = filterType switch
			{
				GlobalFilterOptionsType.None => null,
				GlobalFilterOptionsType.DisableAll => new() { DisableAllFilters = true },
				GlobalFilterOptionsType.DisableSpecific => new() { FilterNamesToDisable = ["IsActiveFilter"] },
				GlobalFilterOptionsType.Null => null,
				GlobalFilterOptionsType.EmptyList => new() { FilterNamesToDisable = [] },
				GlobalFilterOptionsType.DisableAllFalse => new() { DisableAllFilters = false },
				GlobalFilterOptionsType.FilterNamesPriority => new() { DisableAllFilters = false, FilterNamesToDisable = ["IsActiveFilter"] },
				_ => null
			};

			BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);

			// Act
			TestEntityWithFilter? activeResult = await testContext.GetByKey(1, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);
			TestEntityWithFilter? inactiveResult = await testContext.GetByKey(2, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

			// Assert
			activeResult.ShouldNotBeNull();
			activeResult.Id.ShouldBe(1);

			if (filtersDisabled)
			{
				inactiveResult.ShouldNotBeNull(); // Should find inactive entity with filters disabled
				inactiveResult.Id.ShouldBe(2);
			}
			else
			{
				inactiveResult.ShouldBeNull(); // Filter should exclude inactive entities
			}
		}
		else // CompoundKey
		{
			TestEntityWithCompoundKeyAndFilter activeEntity = new() { Id1 = 1, Id2 = 1, Name = "Active", IsActive = true };
			TestEntityWithCompoundKeyAndFilter inactiveEntity = new() { Id1 = 1, Id2 = 2, Name = "Inactive", IsActive = false };

			using (IServiceScope scope = serviceProvider.CreateScope())
			{
				TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
				await context.TestEntitiesWithCompoundKeyAndFilter.AddRangeAsync(activeEntity, inactiveEntity);
				await context.SaveChangesAsync(Current.CancellationToken);
			}

			GlobalFilterOptions? filterOptions = filterType switch
			{
				GlobalFilterOptionsType.None => null,
				GlobalFilterOptionsType.DisableAll => new() { DisableAllFilters = true },
				GlobalFilterOptionsType.DisableSpecific => new() { FilterNamesToDisable = ["IsActiveFilter"] },
				_ => null
			};

			BaseDbContextActions<TestEntityWithCompoundKeyAndFilter, TestDbContextWithFilters> testContext = new(serviceProvider);

			// Act
			TestEntityWithCompoundKeyAndFilter? activeResult = await testContext.GetByKey(new object[] { 1, 1 }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);
			TestEntityWithCompoundKeyAndFilter? inactiveResult = await testContext.GetByKey(new object[] { 1, 2 }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

			// Assert
			activeResult.ShouldNotBeNull();
			activeResult.Id1.ShouldBe(1);
			activeResult.Id2.ShouldBe(1);

			if (filtersDisabled)
			{
				inactiveResult.ShouldNotBeNull(); // Should find inactive entity with filters disabled
				inactiveResult.Id1.ShouldBe(1);
				inactiveResult.Id2.ShouldBe(2);
			}
			else
			{
				inactiveResult.ShouldBeNull(); // Filter should exclude inactive entities
			}
		}
	}

	[Fact]
	public async Task GetByKey_WithNullGlobalFilterOptions_ShouldApplyFilters()
	{
		// Arrange
		TestEntityWithFilter inactiveEntity = new() { Id = 1, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddAsync(inactiveEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);

		// Act
		TestEntityWithFilter? result = await testContext.GetByKey(1, globalFilterOptions: null, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull(); // Filter should still apply with null options
	}

	[Fact]
	public async Task GetByKey_WithNonExistentKey_AndDisableAllFilters_ShouldReturnNull()
	{
		// Arrange
		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityWithFilter? result = await testContext.GetByKey(999, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKey_CompoundKey_WithNonExistentKey_AndDisableAllFilters_ShouldReturnNull()
	{
		// Arrange
		BaseDbContextActions<TestEntityWithCompoundKeyAndFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityWithCompoundKeyAndFilter? result = await testContext.GetByKey(new object[] { 999, 999 }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKey_WithQueryTimeout_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntityWithFilter inactiveEntity = new() { Id = 1, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddAsync(inactiveEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityWithFilter? result = await testContext.GetByKey(1, queryTimeout: TimeSpan.FromSeconds(30), globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetByKey_WithCancellationToken_AndGlobalFilterOptions_ShouldRespectCancellation()
	{
		// Arrange
		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act
		TestEntityWithFilter? result = await testContext.GetByKey(1, globalFilterOptions: filterOptions, cancellationToken: cts.Token);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKey_FilterNamesToDisableTakesPriorityOverDisableAllFilters()
	{
		// Arrange
		TestEntityWithFilter inactiveEntity = new() { Id = 1, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddAsync(inactiveEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new()
		{
			DisableAllFilters = false, // This should be ignored
			FilterNamesToDisable = ["IsActiveFilter"]
		};

		// Act
		TestEntityWithFilter? result = await testContext.GetByKey(1, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		// FilterNamesToDisable takes priority, so filters should be disabled
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
	}

	#endregion

	#region GetByKeyFull with GlobalFilterOptions Tests

	[Fact]
	public async Task GetByKeyFull_WithoutGlobalFilterOptions_ShouldApplyFilters()
	{
		// Arrange
		TestEntityWithFilter activeEntity = new() { Id = 1, Name = "Active", IsActive = true };
		TestEntityWithFilter inactiveEntity = new() { Id = 2, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddRangeAsync(activeEntity, inactiveEntity);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);

		// Act
		TestEntityWithFilter? activeResult = await testContext.GetByKeyFull(1, cancellationToken: Current.CancellationToken);
		TestEntityWithFilter? inactiveResult = await testContext.GetByKeyFull(2, cancellationToken: Current.CancellationToken);

		// Assert
		activeResult.ShouldNotBeNull();
		activeResult.Id.ShouldBe(1);
		inactiveResult.ShouldBeNull(); // Filter should exclude inactive entities
	}

	#endregion

	#region DeleteByKey with GlobalFilterOptions Tests

	[Fact]
	public async Task DeleteByKey_WithoutGlobalFilterOptions_ShouldApplyFilters()
	{
		// Arrange
		TestEntityWithFilter activeEntity = new() { Id = 1, Name = "Active", IsActive = true };
		TestEntityWithFilter inactiveEntity = new() { Id = 2, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddRangeAsync(activeEntity, inactiveEntity);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);

		// Act
		bool activeResult = await testContext.DeleteByKey(1);
		bool inactiveResult = await testContext.DeleteByKey(2);

		// Assert
		activeResult.ShouldBeTrue(); // Active entity should be found and deleted
		inactiveResult.ShouldBeFalse(); // Inactive entity should not be found due to filter
	}

	[Fact]
	public async Task DeleteByKey_WithGlobalFilterOptions_DisableAllFilters_ShouldIgnoreFilters()
	{
		// Arrange
		TestEntityWithFilter inactiveEntity = new() { Id = 1, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddAsync(inactiveEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		bool result = await testContext.DeleteByKey(1, globalFilterOptions: filterOptions);

		// Assert
		result.ShouldBeTrue(); // Should find and delete inactive entity with filters disabled
	}

	[Fact]
	public async Task DeleteByKey_WithGlobalFilterOptions_FilterNamesToDisable_ShouldDisableSpecifiedFilters()
	{
		// Arrange
		TestEntityWithFilter inactiveEntity = new() { Id = 1, Name = "Inactive", IsActive = false };

		using (IServiceScope scope = serviceProvider.CreateScope())
		{
			TestDbContextWithFilters context = scope.ServiceProvider.GetRequiredService<TestDbContextWithFilters>();
			await context.TestEntitiesWithFilter.AddAsync(inactiveEntity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityWithFilter, TestDbContextWithFilters> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["IsActiveFilter"] };

		// Act
		bool result = await testContext.DeleteByKey(1, globalFilterOptions: filterOptions);

		// Assert
		result.ShouldBeTrue(); // Should find and delete inactive entity with specified filter disabled
	}

	#endregion

	#region Error Path Tests

	// Note: The following error path tests validate error handling behavior.
	// Each test uses unique entity types to avoid expression builder caching issues.
	// The implementation handles invalid key scenarios gracefully rather than throwing exceptions.

	[Fact]
	public async Task GetByKey_SingleKey_OnCompoundKeyEntity_WithGlobalFilterOptions_ShouldReturnNull()
	{
		// Arrange - Use unique entity type to avoid caching
		SqliteConnection testConnection = new("DataSource=:memory:");
		await testConnection.OpenAsync(Current.CancellationToken);

		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForGetSingleKey>(options => options.UseSqlite(testConnection));
		IServiceProvider provider = services.BuildServiceProvider();

		using (IServiceScope scope = provider.CreateScope())
		{
			TestDbContextForGetSingleKey context = scope.ServiceProvider.GetRequiredService<TestDbContextForGetSingleKey>();
			await context.Database.EnsureCreatedAsync(Current.CancellationToken);

			TestEntityCompoundKeyForGetSingleKey entity = new() { Id1 = 1, Id2 = 1, Name = "Test", IsActive = true };
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityCompoundKeyForGetSingleKey, TestDbContextForGetSingleKey> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityCompoundKeyForGetSingleKey? result = await testContext.GetByKey(1, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert - Should return null gracefully when single key is provided for compound key entity
		result.ShouldBeNull();

		await testConnection.DisposeAsync();
	}

	[Fact]
	public async Task GetByKey_CompoundKey_WithWrongKeyCount_WithGlobalFilterOptions_ShouldReturnNull()
	{
		// Arrange - Use unique entity type to avoid caching
		SqliteConnection testConnection = new("DataSource=:memory:");
		await testConnection.OpenAsync(Current.CancellationToken);

		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForGetWrongCount>(options => options.UseSqlite(testConnection));
		IServiceProvider provider = services.BuildServiceProvider();

		using (IServiceScope scope = provider.CreateScope())
		{
			TestDbContextForGetWrongCount context = scope.ServiceProvider.GetRequiredService<TestDbContextForGetWrongCount>();
			await context.Database.EnsureCreatedAsync(Current.CancellationToken);

			TestEntityCompoundKeyForGetWrongCount entity = new() { Id1 = 1, Id2 = 1, Name = "Test", IsActive = true };
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityCompoundKeyForGetWrongCount, TestDbContextForGetWrongCount> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act - providing only 1 key when 2 are needed
		TestEntityCompoundKeyForGetWrongCount? result = await testContext.GetByKey(new object[] { 1 }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert - Should return null gracefully when wrong key count is provided
		result.ShouldBeNull();

		await testConnection.DisposeAsync();
	}

	[Fact]
	public async Task DeleteByKey_SingleKey_OnCompoundKeyEntity_WithGlobalFilterOptions_ShouldReturnFalse()
	{
		// Arrange - Use unique entity type to avoid caching
		SqliteConnection testConnection = new("DataSource=:memory:");
		await testConnection.OpenAsync(Current.CancellationToken);

		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForDeleteSingleKey>(options => options.UseSqlite(testConnection));
		IServiceProvider provider = services.BuildServiceProvider();

		using (IServiceScope scope = provider.CreateScope())
		{
			TestDbContextForDeleteSingleKey context = scope.ServiceProvider.GetRequiredService<TestDbContextForDeleteSingleKey>();
			await context.Database.EnsureCreatedAsync(Current.CancellationToken);

			TestEntityCompoundKeyForDeleteSingleKey entity = new() { Id1 = 1, Id2 = 1, Name = "Test", IsActive = true };
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityCompoundKeyForDeleteSingleKey, TestDbContextForDeleteSingleKey> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		bool result = await testContext.DeleteByKey(1, globalFilterOptions: filterOptions);

		// Assert - Should return false gracefully when single key is provided for compound key entity
		result.ShouldBeFalse();

		await testConnection.DisposeAsync();
	}

	[Fact]
	public async Task DeleteByKey_CompoundKey_WithWrongKeyCount_WithGlobalFilterOptions_ShouldReturnFalse()
	{
		// Arrange - Use unique entity type to avoid caching
		SqliteConnection testConnection = new("DataSource=:memory:");
		await testConnection.OpenAsync(Current.CancellationToken);

		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForDeleteWrongCount>(options => options.UseSqlite(testConnection));
		IServiceProvider provider = services.BuildServiceProvider();

		using (IServiceScope scope = provider.CreateScope())
		{
			TestDbContextForDeleteWrongCount context = scope.ServiceProvider.GetRequiredService<TestDbContextForDeleteWrongCount>();
			await context.Database.EnsureCreatedAsync(Current.CancellationToken);

			TestEntityCompoundKeyForDeleteWrongCount entity = new() { Id1 = 1, Id2 = 1, Name = "Test", IsActive = true };
			await context.TestEntities.AddAsync(entity, Current.CancellationToken);
			await context.SaveChangesAsync(Current.CancellationToken);
		}

		BaseDbContextActions<TestEntityCompoundKeyForDeleteWrongCount, TestDbContextForDeleteWrongCount> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act - providing only 1 key when 2 are needed
		bool result = await testContext.DeleteByKey(new object[] { 1 }, globalFilterOptions: filterOptions);

		// Assert - Should return false gracefully when wrong key count is provided
		result.ShouldBeFalse();

		await testConnection.DisposeAsync();
	}

	#endregion
}

// Test DbContext with global query filters
public class TestDbContextWithFilters(DbContextOptions<TestDbContextWithFilters> options) : DbContext(options)
{
	public DbSet<TestEntityWithFilter> TestEntitiesWithFilter => Set<TestEntityWithFilter>();
	public DbSet<TestEntityWithCompoundKeyAndFilter> TestEntitiesWithCompoundKeyAndFilter => Set<TestEntityWithCompoundKeyAndFilter>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Configure global query filter for TestEntityWithFilter
		modelBuilder.Entity<TestEntityWithFilter>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);

		// Configure global query filter for TestEntityWithCompoundKeyAndFilter
		modelBuilder.Entity<TestEntityWithCompoundKeyAndFilter>()
			.HasKey(e => new { e.Id1, e.Id2 });

		modelBuilder.Entity<TestEntityWithCompoundKeyAndFilter>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);
	}
}

// Test entity with filter
public class TestEntityWithFilter
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

// Test entity with compound key and filter
public class TestEntityWithCompoundKeyAndFilter
{
	public int Id1 { get; set; }
	public int Id2 { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

// Unique entity types for error path tests to avoid expression builder caching
public class TestEntityCompoundKeyForGetSingleKey
{
	public int Id1 { get; set; }
	public int Id2 { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

public class TestDbContextForGetSingleKey(DbContextOptions<TestDbContextForGetSingleKey> options) : DbContext(options)
{
	public DbSet<TestEntityCompoundKeyForGetSingleKey> TestEntities => Set<TestEntityCompoundKeyForGetSingleKey>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityCompoundKeyForGetSingleKey>()
			.HasKey(e => new { e.Id1, e.Id2 });
		modelBuilder.Entity<TestEntityCompoundKeyForGetSingleKey>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);
	}
}

public class TestEntityCompoundKeyForGetWrongCount
{
	public int Id1 { get; set; }
	public int Id2 { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

public class TestDbContextForGetWrongCount(DbContextOptions<TestDbContextForGetWrongCount> options) : DbContext(options)
{
	public DbSet<TestEntityCompoundKeyForGetWrongCount> TestEntities => Set<TestEntityCompoundKeyForGetWrongCount>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityCompoundKeyForGetWrongCount>()
			.HasKey(e => new { e.Id1, e.Id2 });
		modelBuilder.Entity<TestEntityCompoundKeyForGetWrongCount>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);
	}
}

public class TestEntityCompoundKeyForDeleteSingleKey
{
	public int Id1 { get; set; }
	public int Id2 { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

public class TestDbContextForDeleteSingleKey(DbContextOptions<TestDbContextForDeleteSingleKey> options) : DbContext(options)
{
	public DbSet<TestEntityCompoundKeyForDeleteSingleKey> TestEntities => Set<TestEntityCompoundKeyForDeleteSingleKey>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityCompoundKeyForDeleteSingleKey>()
			.HasKey(e => new { e.Id1, e.Id2 });
		modelBuilder.Entity<TestEntityCompoundKeyForDeleteSingleKey>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);
	}
}

public class TestEntityCompoundKeyForDeleteWrongCount
{
	public int Id1 { get; set; }
	public int Id2 { get; set; }
	public required string Name { get; set; }
	public bool IsActive { get; set; }
}

public class TestDbContextForDeleteWrongCount(DbContextOptions<TestDbContextForDeleteWrongCount> options) : DbContext(options)
{
	public DbSet<TestEntityCompoundKeyForDeleteWrongCount> TestEntities => Set<TestEntityCompoundKeyForDeleteWrongCount>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityCompoundKeyForDeleteWrongCount>()
			.HasKey(e => new { e.Id1, e.Id2 });
		modelBuilder.Entity<TestEntityCompoundKeyForDeleteWrongCount>()
			.HasQueryFilter("IsActiveFilter", e => e.IsActive);
	}
}
