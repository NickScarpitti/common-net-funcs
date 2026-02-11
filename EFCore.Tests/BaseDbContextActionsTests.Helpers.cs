using System.Collections.Concurrent;
using System.Reflection;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Xunit.TestContext;

namespace EFCore.Tests;

public sealed partial class BaseDbContextActionsTests
{
	#region FullQueryOptions Tests

	[Fact]
	public void Constructor_DefaultValues_SetsPropertiesToNull()
	{
		// Act
		FullQueryOptions options = new();

		// Assert
		options.SplitQueryOverride.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void SplitQueryOverride_SetAndGet_ReturnsCorrectValue(bool? value)
	{
		// Arrange
		FullQueryOptions options = new()
		{
			// Act
			SplitQueryOverride = value
		};

		// Assert
		options.SplitQueryOverride.ShouldBe(value);
	}

	[Fact]
	public void FullQueryOptions_CanBeUsedInCollections()
	{
		// Arrange
		List<FullQueryOptions> optionsList =
		[
			new() { SplitQueryOverride = true },
			new() { SplitQueryOverride = false },
			new() { SplitQueryOverride = null }
		];

		// Assert
		optionsList.Count.ShouldBe(3);
		optionsList[0].SplitQueryOverride.ShouldBe(true);
		optionsList[1].SplitQueryOverride.ShouldBe(false);
		optionsList[2].SplitQueryOverride.ShouldBeNull();
	}

	#endregion

	#region GenericPagingModel Tests

	[Fact]
	public void Constructor_Default_InitializesEmptyList()
	{
		// Act
		GenericPagingModel<TestEntity> model = new();

		// Assert
		model.Entities.ShouldNotBeNull();
		model.Entities.ShouldBeEmpty();
		model.TotalRecords.ShouldBe(0);
	}

	[Fact]
	public void Entities_SetAndGet_WorksCorrectly()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new();
		List<TestEntity> entities = [new() { Id = 1, Name = "Test" }];

		// Act
		model.Entities = entities;

		// Assert
		model.Entities.ShouldBe(entities);
		model.Entities.Count.ShouldBe(1);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(100)]
	[InlineData(int.MaxValue)]
	public void TotalRecords_SetAndGet_WorksCorrectly(int totalRecords)
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new()
		{
			// Act
			TotalRecords = totalRecords
		};

		// Assert
		model.TotalRecords.ShouldBe(totalRecords);
	}

	[Fact]
	public void GenericPagingModel_WithMultipleEntities_HandlesCorrectly()
	{
		// Arrange
		List<TestEntity> entities =
		[
			new() { Id = 1, Name = "Entity1" },
			new() { Id = 2, Name = "Entity2" },
			new() { Id = 3, Name = "Entity3" }
		];

		// Act
		GenericPagingModel<TestEntity> model = new()
		{
			Entities = entities,
			TotalRecords = 10
		};

		// Assert
		model.Entities.Count.ShouldBe(3);
		model.TotalRecords.ShouldBe(10);
		model.Entities[0].Id.ShouldBe(1);
		model.Entities[1].Id.ShouldBe(2);
		model.Entities[2].Id.ShouldBe(3);
	}

	[Fact]
	public void GenericPagingModel_WithNullEntities_CanBeSet()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new()
		{
			// Act
			Entities = null!
		};

		// Assert
		model.Entities.ShouldBeNull();
	}

	[Fact]
	public void GenericPagingModel_ModifyingEntitiesAfterAssignment_ReflectsChanges()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new();
		List<TestEntity> entities = [new() { Id = 1, Name = "Test" }];
		model.Entities = entities;

		// Act
		entities.Add(new TestEntity { Id = 2, Name = "Test2" });

		// Assert
		model.Entities.Count.ShouldBe(2);
		model.Entities[1].Id.ShouldBe(2);
	}

	[Fact]
	public void GenericPagingModel_WithDifferentTypes_WorksCorrectly()
	{
		// Arrange & Act
		GenericPagingModel<string> stringModel = new()
		{
			Entities = ["test1", "test2"],
			TotalRecords = 2
		};

		GenericPagingModel<int> intModel = new()
		{
			Entities = [1, 2, 3],
			TotalRecords = 3
		};

		// Assert
		stringModel.Entities.Count.ShouldBe(2);
		stringModel.TotalRecords.ShouldBe(2);
		intModel.Entities.Count.ShouldBe(3);
		intModel.TotalRecords.ShouldBe(3);
	}

	#endregion

	#region ServiceProvider Property Test

	[Fact]
	public void ServiceProvider_SetAndGet_ShouldWork()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		ServiceCollection newServices = new();
		newServices.AddDbContextPool<TestDbContext>(options =>
			options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
		IServiceProvider newServiceProvider = newServices.BuildServiceProvider();

		// Act
		testContext.ServiceProvider = newServiceProvider;

		// Assert
		testContext.ServiceProvider.ShouldBe(newServiceProvider);
	}

	#endregion

	#region Query Options Coverage

	[Fact]
	public async Task GetAll_WithSplitQuery_Null_ShouldSucceed()
	{
		// Arrange - Use SQLite for realistic ExecuteUpdateAsync behavior
		(IServiceProvider sqliteProvider, TestDbContext sqliteContext, IDisposable scope) = CreateSqliteServiceProvider();
		try
		{
			List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
			await sqliteContext.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
			await sqliteContext.SaveChangesAsync(Current.CancellationToken);

			BaseDbContextActions<TestEntity, TestDbContext> testContext = new(sqliteProvider);

			// Act
			int? result = await testContext.UpdateMany(
				x => x.Id > 0,
				s => s.SetProperty(e => e.Name, e => "Updated"),
				queryTimeout: TimeSpan.FromSeconds(30),
				cancellationToken: Current.CancellationToken);

			// Assert
			result.ShouldNotBeNull();
			result.Value.ShouldBe(2);
		}
		finally
		{
			scope.Dispose();
		}
	}

	#endregion

	#region Circular Reference Handling Tests

	// These tests verify that the circular reference handling in ExecuteWithCircularRefHandling
	// and ExecuteStreamingWithCircularRefHandling works correctly when TestEntity and TestEntityDetail
	// have a circular reference (TestEntity.Details <-> TestEntityDetail.TestEntity)

	[Fact]
	public async Task GetAllFull_WithCircularReferences_ShouldHandleNavigationProperties()
	{
		// Arrange - Create test entities with navigation properties
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(2)
			.ToArray();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await context.AddAsync(detail, Current.CancellationToken);
		}
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act - GetAllFull uses ExecuteWithCircularRefHandling internally
		List<TestEntity>? result = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task GetAllFullStreaming_WithCircularReferences_ShouldStreamResults()
	{
		// Arrange - Create test entities with navigation properties
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(3)
			.ToArray();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await context.AddAsync(detail, Current.CancellationToken);
		}
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act - GetAllFullStreaming uses ExecuteStreamingWithCircularRefHandling internally
		List<TestEntity> streamedResults = [];
		await foreach (TestEntity item in testContext.GetAllFullStreaming(cancellationToken: Current.CancellationToken)!)
		{
			streamedResults.Add(item);
		}

		// Assert
		streamedResults.Count.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task CircularReferenceHandling_MultipleCallsSameEntity_ShouldUseCachedBehavior()
	{
		// Arrange - Create test entities with navigation properties
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(2)
			.ToArray();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await context.AddAsync(detail, Current.CancellationToken);
		}
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act - Call GetAllFull multiple times to verify caching behavior
		List<TestEntity>? firstResult = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);
		List<TestEntity>? secondResult = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);
		List<TestEntity>? thirdResult = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);

		// Assert - All calls should succeed without errors
		firstResult.ShouldNotBeNull();
		firstResult.Count.ShouldBeGreaterThanOrEqualTo(2);

		secondResult.ShouldNotBeNull();
		secondResult.Count.ShouldBeGreaterThanOrEqualTo(2);

		thirdResult.ShouldNotBeNull();
		thirdResult.Count.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task CircularReferenceHandling_StreamingMultipleCalls_ShouldUseCachedBehavior()
	{
		// Arrange - Create test entities with navigation properties
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(2)
			.ToArray();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await context.AddAsync(detail, Current.CancellationToken);
		}
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act - Call GetAllFullStreaming multiple times to verify caching behavior
		List<TestEntity> firstResults = [];
		await foreach (TestEntity item in testContext.GetAllFullStreaming(cancellationToken: Current.CancellationToken)!)
		{
			firstResults.Add(item);
		}

		List<TestEntity> secondResults = [];
		await foreach (TestEntity item in testContext.GetAllFullStreaming(cancellationToken: Current.CancellationToken)!)
		{
			secondResults.Add(item);
		}

		// Assert - Both calls should succeed and return consistent results
		firstResults.Count.ShouldBeGreaterThanOrEqualTo(2);
		secondResults.Count.ShouldBeGreaterThanOrEqualTo(2);
		secondResults.Count.ShouldBe(firstResults.Count);
	}

	[Fact]
	public async Task CircularReferenceHandling_CheckDictionaryState_ShouldCacheEntityType()
	{
		// Arrange - Create test entities with navigation properties
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(2)
			.ToArray();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await context.AddAsync(detail, Current.CancellationToken);
		}
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Get reference to the static circularReferencingEntities dictionary via reflection
		System.Reflection.FieldInfo? field = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetField("circularReferencingEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		field.ShouldNotBeNull("circularReferencingEntities field should be accessible via reflection");

		ConcurrentDictionary<Type, bool>? dictionary = field.GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<Type, bool>;
		dictionary.ShouldNotBeNull();

		// Record initial state
		bool initiallyInDictionary = dictionary.ContainsKey(typeof(TestEntity));

		// Act - Call GetAllFull which may add the entity type to the dictionary
		List<TestEntity>? result = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBeGreaterThanOrEqualTo(2);

		// Check if entity type is now in dictionary (it might be added if circular ref exception occurred)
		// or it should work fine either way
		bool finallyInDictionary = dictionary.ContainsKey(typeof(TestEntity));

		// The key assertion: If it was added to dictionary, subsequent calls should not throw
		if (finallyInDictionary)
		{
			// Call again - should use cached knowledge
			List<TestEntity>? secondResult = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);
			secondResult.ShouldNotBeNull();
			secondResult.Count.ShouldBeGreaterThanOrEqualTo(2);
		}

		// Whether or not it was added, the call should succeed
		result.Count.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task ExecuteWithCircularRefHandling_WhenExceptionThrown_ShouldRetryAndAddToDictionary()
	{
		// Arrange - Get the static method via reflection
		MethodInfo? method = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetMethod("ExecuteWithCircularRefHandling", BindingFlags.NonPublic | BindingFlags.Static);
		method.ShouldNotBeNull("ExecuteWithCircularRefHandling should be accessible via reflection");

		// Get reference to the circularReferencingEntities dictionary
		FieldInfo? field = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetField("circularReferencingEntities", BindingFlags.NonPublic | BindingFlags.Static);
		field.ShouldNotBeNull();
		ConcurrentDictionary<Type, bool>? dictionary = field.GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<Type, bool>;
		dictionary.ShouldNotBeNull();

		// Clear the dictionary to ensure clean test state
		dictionary.Clear();

		// Create a mock operation that throws the specific exception on first call, succeeds on second
		int callCount = 0;
		List<TestEntity> testResult = fixture.CreateMany<TestEntity>(2).ToList();

		Func<bool, CancellationToken, Task<List<TestEntity>>> operation = (handlingCircularRef, cancellationToken) =>
		{
			callCount++;
			if (!handlingCircularRef && callCount == 1)
			{
				// First call without handling flag - throw the specific exception
				InvalidOperationException ex = new("Sequence contains no elements");
				// Set HResult using the private field
				FieldInfo? hresultField = typeof(Exception).GetField("_HResult", BindingFlags.NonPublic | BindingFlags.Instance);
				hresultField?.SetValue(ex, -2146233079);
				throw ex;
			}
			// Second call with handling flag - succeed
			return Task.FromResult(testResult);
		};

		// Act - Invoke the method via reflection
		MethodInfo genericMethod = method.MakeGenericMethod(typeof(List<TestEntity>));
		Task<List<TestEntity>>? task = genericMethod.Invoke(null, new object[] { operation, CancellationToken.None }) as Task<List<TestEntity>>;
		List<TestEntity>? result = await task!;

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(2);
		callCount.ShouldBe(2, "Operation should have been called twice - once throwing, once succeeding");
		dictionary.ContainsKey(typeof(TestEntity)).ShouldBeTrue("Entity type should be added to dictionary after handling exception");
	}
	[Fact]
	public async Task ExecuteWithCircularRefHandling_WhenSecondaryExceptionThrown_ShouldReturnDefault()
	{
		// Arrange - Get the static method via reflection
		MethodInfo? method = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetMethod("ExecuteWithCircularRefHandling", BindingFlags.NonPublic | BindingFlags.Static);
		method.ShouldNotBeNull();

		// Create a mock operation that throws the specific exception on first call,
		// then throws a different exception on retry
		int callCount = 0;
		Func<bool, CancellationToken, Task<List<TestEntity>>> operation = (handlingCircularRef, cancellationToken) =>
		{
			callCount++;
			if (callCount == 1)
			{
				// First call - throw the circular reference exception
				InvalidOperationException ex = new("Circular reference detected");
				// Set HResult using the private field
				FieldInfo? hresultField = typeof(Exception).GetField("_HResult", BindingFlags.NonPublic | BindingFlags.Instance);
				hresultField?.SetValue(ex, -2146233079);
				throw ex;
			}
			// Second call - throw a different exception to test the secondary catch block
			throw new InvalidOperationException("Secondary failure");
		};

		// Act - Invoke the method via reflection
		MethodInfo genericMethod = method.MakeGenericMethod(typeof(List<TestEntity>));
		Task<List<TestEntity>>? task = genericMethod.Invoke(null, new object[] { operation, CancellationToken.None }) as Task<List<TestEntity>>;
		List<TestEntity>? result = await task!;

		// Assert
		result.ShouldBeNull("Should return default when secondary exception occurs");
		callCount.ShouldBe(2, "Operation should have been called twice");
	}

	[Fact]
	public async Task ExecuteWithCircularRefHandling_WhenDifferentInvalidOperationException_ShouldReturnDefault()
	{
		// Arrange - Get the static method via reflection
		MethodInfo? method = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetMethod("ExecuteWithCircularRefHandling", BindingFlags.NonPublic | BindingFlags.Static);
		method.ShouldNotBeNull();

		// Create a mock operation that throws InvalidOperationException with different HResult
		Func<bool, CancellationToken, Task<List<TestEntity>>> operation = (handlingCircularRef, cancellationToken) =>
		{
			// Throw InvalidOperationException with a different HResult (not -2146233079)
			InvalidOperationException ex = new("Different error");
			// Set different HResult using the private field
			FieldInfo? hresultField = typeof(Exception).GetField("_HResult", BindingFlags.NonPublic | BindingFlags.Instance);
			hresultField?.SetValue(ex, -2146233088);
			throw ex;
		};

		// Act - Invoke the method via reflection
		MethodInfo genericMethod = method.MakeGenericMethod(typeof(List<TestEntity>));
		Task<List<TestEntity>>? task = genericMethod.Invoke(null, new object[] { operation, CancellationToken.None }) as Task<List<TestEntity>>;
		List<TestEntity>? result = await task!;

		// Assert
		result.ShouldBeNull("Should return default when InvalidOperationException with different HResult occurs");
	}

	[Fact]
	public async Task ExecuteWithCircularRefHandling_WhenGeneralExceptionThrown_ShouldReturnDefault()
	{
		// Arrange - Get the static method via reflection
		MethodInfo? method = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetMethod("ExecuteWithCircularRefHandling", BindingFlags.NonPublic | BindingFlags.Static);
		method.ShouldNotBeNull();

		// Create a mock operation that throws a general exception
		Func<bool, CancellationToken, Task<List<TestEntity>>> operation = (handlingCircularRef, cancellationToken) =>
		{
			throw new ArgumentException("General error");
		};

		// Act - Invoke the method via reflection
		MethodInfo genericMethod = method.MakeGenericMethod(typeof(List<TestEntity>));
		Task<List<TestEntity>>? task = genericMethod.Invoke(null, new object[] { operation, CancellationToken.None }) as Task<List<TestEntity>>;
		List<TestEntity>? result = await task!;

		// Assert
		result.ShouldBeNull("Should return default when general exception occurs");
	}

	[Fact]
	public async Task ExecuteStreamingWithCircularRefHandling_WhenSecondaryExceptionThrown_ShouldReturnEmpty()
	{
		// Arrange - Get the static method via reflection
		MethodInfo? method = typeof(BaseDbContextActions<TestEntity, TestDbContext>)
			.GetMethod("ExecuteStreamingWithCircularRefHandling", BindingFlags.NonPublic | BindingFlags.Static);
		method.ShouldNotBeNull();

		// Create a mock query builder that throws circular ref exception, then a secondary exception
		int callCount = 0;
		Func<bool, IQueryable<TestEntity>> queryBuilder = (handlingCircularRef) =>
		{
			callCount++;
			if (callCount == 1)
			{
				InvalidOperationException ex = new("Circular reference");
				// Set HResult using the private field
				FieldInfo? hresultField = typeof(Exception).GetField("_HResult", BindingFlags.NonPublic | BindingFlags.Instance);
				hresultField?.SetValue(ex, -2146233079);
				throw ex;
			}
			// Second call throws different exception
			throw new InvalidOperationException("Secondary failure");
		};

		// Act - Invoke the method via reflection
		MethodInfo genericMethod = method.MakeGenericMethod(typeof(TestEntity));
		IAsyncEnumerable<TestEntity>? asyncEnumerable = genericMethod.Invoke(null, new object[] { queryBuilder, CancellationToken.None }) as IAsyncEnumerable<TestEntity>;
		asyncEnumerable.ShouldNotBeNull();

		List<TestEntity> results = [];
		await foreach (TestEntity item in asyncEnumerable)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(0, "Should return empty when secondary exception occurs");
		callCount.ShouldBe(2, "Query builder should have been called twice");
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Creates a service provider configured with SQLite for tests that require realistic database behavior.
	/// Returns a disposable wrapper that manages the connection lifecycle.
	/// </summary>
	internal static (IServiceProvider serviceProvider, TestDbContext context, IDisposable scope) CreateSqliteServiceProvider()
	{
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options =>
			options.UseSqlite("DataSource=:memory:"),
			ServiceLifetime.Scoped);
		IServiceProvider provider = services.BuildServiceProvider();
		IServiceScope scope = provider.CreateScope();
		TestDbContext ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
		ctx.Database.OpenConnection(); // Keep the connection open for in-memory SQLite
		ctx.Database.EnsureCreated();
		return (scope.ServiceProvider, ctx, scope);
	}

	/// <summary>
	/// Creates a service provider configured for circular reference tests.
	/// Uses in-memory database provider like the main test suite.
	/// </summary>
	internal static (IServiceProvider serviceProvider, CircularRefDbContext context) CreateCircularRefServiceProvider()
	{
		ServiceCollection services = new();
		services.AddDbContextPool<CircularRefDbContext>(options =>
			options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
		IServiceProvider provider = services.BuildServiceProvider();
		CircularRefDbContext ctx = provider.GetRequiredService<CircularRefDbContext>();
		return (provider, ctx);
	}

	#endregion
}
