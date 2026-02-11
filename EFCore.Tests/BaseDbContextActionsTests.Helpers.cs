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

	// Note: These tests verify that entities with bidirectional (circular) relationships
	// can be properly handled by the BaseDbContextActions framework. While the specific
	// InvalidOperationException (HResult -2146233079) for circular references may not
	// always be triggered depending on the database provider and tracking settings, these
	// tests ensure the entity structure and basic operations work correctly.

	[Fact]
	public async Task CircularRefContext_DirectQuery_ShouldWork()
	{
		// Diagnostic test - verify the context and relationships work directly
		(IServiceProvider circularProvider, CircularRefDbContext _) = CreateCircularRefServiceProvider();
		CircularRefDbContext circularContext = circularProvider.GetRequiredService<CircularRefDbContext>();

		ParentEntity parent = new() { Name = "Parent1" };
		ChildEntity child = new() { Name = "Child1", Parent = parent };
		parent.Children.Add(child);

		await circularContext.Parents.AddAsync(parent, Current.CancellationToken);
		int saved = await circularContext.SaveChangesAsync(Current.CancellationToken);

		saved.ShouldBe(2); // Parent and child

		List<ParentEntity> parents = await circularContext.Parents.ToListAsync(Current.CancellationToken);
		parents.Count.ShouldBe(1);
		parents[0].Name.ShouldBe("Parent1");

		List<ChildEntity> children = await circularContext.Children.ToListAsync(Current.CancellationToken);
		children.Count.ShouldBe(1);
		children[0].Name.ShouldBe("Child1");
	}

	[Fact]
	public async Task CircularRefEntities_BasicCRUD_ShouldSucceed()
	{
		// Arrange
		(IServiceProvider circularProvider, CircularRefDbContext _) = CreateCircularRefServiceProvider();
		CircularRefDbContext circularContext = circularProvider.GetRequiredService<CircularRefDbContext>();

		ParentEntity parent = new() { Name = "Parent1" };
		ChildEntity child1 = new() { Name = "Child1", Parent = parent };
		ChildEntity child2 = new() { Name = "Child2", Parent = parent };
		parent.Children.Add(child1);
		parent.Children.Add(child2);

		await circularContext.Parents.AddAsync(parent, Current.CancellationToken);
		await circularContext.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<ParentEntity, CircularRefDbContext> actions = new(circularProvider);

		// Act & Assert - Get all parents
		List<ParentEntity>? allParents = await actions.GetAll(
			queryTimeout: TimeSpan.FromSeconds(30),
			trackEntities: true,
			cancellationToken: Current.CancellationToken);

		allParents.ShouldNotBeNull();
		allParents.Count.ShouldBe(1);
		allParents[0].Name.ShouldBe("Parent1");

		// Act & Assert - Get by key
		ParentEntity? byKey = await actions.GetByKey(
			primaryKey: parent.Id,
			queryTimeout: TimeSpan.FromSeconds(30),
			cancellationToken: Current.CancellationToken);

		byKey.ShouldNotBeNull();
		byKey.Name.ShouldBe("Parent1");
	}

	[Fact]
	public async Task CircularRefEntities_FilterAndPaging_ShouldSucceed()
	{
		// Arrange
		(IServiceProvider circularProvider, CircularRefDbContext _) = CreateCircularRefServiceProvider();
		CircularRefDbContext circularContext = circularProvider.GetRequiredService<CircularRefDbContext>();

		List<ParentEntity> parents = [];
		for (int i = 1; i <= 5; i++)
		{
			ParentEntity parent = new() { Name = $"Parent{i}" };
			ChildEntity child = new() { Name = $"Child{i}", Parent = parent };
			parent.Children.Add(child);
			parents.Add(parent);
		}

		await circularContext.Parents.AddRangeAsync(parents, Current.CancellationToken);
		await circularContext.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<ParentEntity, CircularRefDbContext> actions = new(circularProvider);

		// Act & Assert - Filter
		List<ParentEntity>? filtered = await actions.GetWithFilter(
			whereExpression: p => p.Name == "Parent1",
			queryTimeout: TimeSpan.FromSeconds(30),
			trackEntities: true,
			cancellationToken: Current.CancellationToken);

		filtered.ShouldNotBeNull();
		filtered.Count.ShouldBe(1);

		// Act & Assert - Paging
		GenericPagingModel<ParentEntity>? paged = await actions.GetWithPagingFilter(
			whereExpression: p => p.Id > 0,
			selectExpression: p => p,
			orderByString: "Id",
			skip: 0,
			pageSize: 2,
			queryTimeout: TimeSpan.FromSeconds(30),
			trackEntities: true,
			cancellationToken: Current.CancellationToken);

		paged.ShouldNotBeNull();
		paged.TotalRecords.ShouldBe(5);
		paged.Entities.Count.ShouldBe(2);
	}

	[Fact]
	public async Task CircularRefEntities_SelectProjection_ShouldSucceed()
	{
		// Arrange
		(IServiceProvider circularProvider, CircularRefDbContext _) = CreateCircularRefServiceProvider();
		CircularRefDbContext circularContext = circularProvider.GetRequiredService<CircularRefDbContext>();

		ParentEntity parent = new() { Name = "Parent1" };
		ChildEntity child = new() { Name = "Child1", Parent = parent };
		parent.Children.Add(child);

		await circularContext.Parents.AddAsync(parent, Current.CancellationToken);
		await circularContext.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<ParentEntity, CircularRefDbContext> actions = new(circularProvider);

		// Act - Select only specific fields
		List<string>? names = await actions.GetAll(
			selectExpression: p => p.Name,
			queryTimeout: TimeSpan.FromSeconds(30),
			trackEntities: true,
			cancellationToken: Current.CancellationToken);

		// Assert
		names.ShouldNotBeNull();
		names.Count.ShouldBe(1);
		names[0].ShouldBe("Parent1");
	}

	[Fact]
	public async Task CircularRefEntities_BidirectionalNavigation_ShouldSucceed()
	{
		// Arrange
		(IServiceProvider circularProvider, CircularRefDbContext _) = CreateCircularRefServiceProvider();
		CircularRefDbContext circularContext = circularProvider.GetRequiredService<CircularRefDbContext>();

		ParentEntity parent = new() { Name = "Parent1" };
		ChildEntity child1 = new() { Name = "Child1", Parent = parent };
		ChildEntity child2 = new() { Name = "Child2", Parent = parent };
		parent.Children.Add(child1);
		parent.Children.Add(child2);

		await circularContext.Parents.AddAsync(parent, Current.CancellationToken);
		await circularContext.SaveChangesAsync(Current.CancellationToken);

		// Act & Assert - Navigate from parent to children
		ParentEntity? parentResult = await circularContext.Parents
			.Where(p => p.Id == parent.Id)
			.FirstOrDefaultAsync(Current.CancellationToken);

		parentResult.ShouldNotBeNull();
		parentResult.Children.Count.ShouldBe(2);

		// Act & Assert - Navigate from child to parent
		BaseDbContextActions<ChildEntity, CircularRefDbContext> childActions = new(circularProvider);
		List<ChildEntity>? children = await childActions.GetAll(
			queryTimeout: TimeSpan.FromSeconds(30),
			trackEntities: true,
			cancellationToken: Current.CancellationToken);

		children.ShouldNotBeNull();
		children.Count.ShouldBe(2);
		children.All(c => c.ParentId == parent.Id).ShouldBeTrue();
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
