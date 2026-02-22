using System.Linq.Expressions;
using CommonNetFuncs.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Xunit.TestContext;

namespace EFCore.Tests;

public sealed partial class BaseDbContextActionsTests
{
	#region GetByKey Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetByKey_WithValidKey_ShouldReturnExpectedEntity(bool full)
	{
		// Arrange
		TestEntity testEntity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(testEntity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(full, testEntity.Id, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(testEntity.Id);
		result.Name.ShouldBe(testEntity.Name);
	}

	[Fact]
	public async Task GetByKeyFull_WithValidKey_ShouldReturnExpectedEntity()
	{
		// Arrange
		TestEntity testEntity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(testEntity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(testEntity.Id, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(testEntity.Id);
		result.Name.ShouldBe(testEntity.Name);
	}

	[Fact]
	public async Task GetByKey_WithCompoundKey_ShouldReturnEntity()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(new object[] { entity.Id }, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetByKeyFullAndNot_WithCompoundKey_ShouldReturnEntity(bool full)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(full, new object[] { entity.Id }, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetByKeyFull_WithCompoundKey_ShouldReturnEntity()
	{
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetByKeyFull(new object[] { entity.Id }, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetByKey_WithInvalidKey_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetByKey(-1, cancellationToken: Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKey_WithCancellation_ShouldThrow()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		TestEntity? result = await testContext.GetByKey(1, cancellationToken: cts.Token);
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKeyFull_WithCompoundKeyAndTrackEntities_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKeyFull(new object[] { entity.Id }, trackEntities: true, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	#endregion

	#region GetAll Tests

	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public async Task GetAll_WithEntities_ShouldReturnAllEntities(bool full, bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetAll(full, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
		results.Select(x => x.Id).ShouldBe(entities.Select(x => x.Id));
	}

	[Fact]
	public async Task GetAllFull_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity>? results = await testContext.GetAllFull(cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllStreaming_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetAllStreaming(cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllFullStreaming_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetAllFullStreaming(cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<string>? results = await testContext.GetAll(x => x.Name, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetAllFull_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<string>? results = await testContext.GetAllFull(x => x.Name, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetAll_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetAll(cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetAllFull_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetAllFull(cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetAll_WithDisableAllFiltersTrue_ShouldDisableAllFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		List<TestEntity>? results = await testContext.GetAll(true, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithFilterNamesToDisable_ShouldDisableSpecificFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["TestFilter"] };

		// Act
		List<TestEntity>? results = await testContext.GetAll(true, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithEmptyFilterNamesToDisableAndDisableAllFiltersFalse_ShouldNotDisableFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false, FilterNamesToDisable = [] };

		// Act
		List<TestEntity>? results = await testContext.GetAll(true, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithNullFilterNamesToDisableAndDisableAllFiltersTrue_ShouldDisableAllFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true, FilterNamesToDisable = null };

		// Act
		List<TestEntity>? results = await testContext.GetAll(true, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithEmptyFilterNamesToDisableAndDisableAllFiltersTrue_ShouldDisableAllFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = true,
			FilterNamesToDisable = Array.Empty<string>()
		};

		// Act
		List<TestEntity>? results = await testContext.GetAll(globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllFull_WithNullFilterNamesToDisableAndDisableAllFiltersFalse_ShouldNotDisableFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = null
		};

		// Act
		List<TestEntity>? results = await testContext.GetAllFull(globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllStreaming_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetAllStreaming(
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity entity in stream)
			{
				results.Add(entity);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllStreamingFull_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["TestFilter"] };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetAllFullStreaming(
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity entity in stream)
			{
				results.Add(entity);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllStreaming_WithProjectionAndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		List<string> results = [];
		IAsyncEnumerable<string>? stream = testContext.GetAllStreaming(
			x => x.Name,
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (string name in stream)
			{
				results.Add(name);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllFullStreaming_WithProjectionAndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = [] };

		// Act
		List<string> results = [];
		IAsyncEnumerable<string>? stream = testContext.GetAllFullStreaming(
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (string name in stream)
			{
				results.Add(name);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAll_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string>? results = await testContext.GetAll(
			x => x.Name,
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllFull_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string>? results = await testContext.GetAllFull(
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1"] },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	#endregion

	#region GetWithFilter Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilter_WithValidPredicate_ShouldReturnFilteredEntities(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(full, filter, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetWithFilterStreaming_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterStreaming(filter, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterStreamingFullAndNot_ShouldReturnFilteredEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterStreaming(full, filter, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterFullStreaming(filter, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Fact]
	public async Task GetWithFilter_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilter(filter, x => x.Name, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilter(full, filter, x => x.Name, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterFull_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilterFull(filter, x => x.Name, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterFull_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;

		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterStreamingFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterStreaming(full, filter, x => x.Name, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterStreaming(filter, x => x.Name, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterFullStreaming(filter, x => x.Name, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilter_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetWithFilter(_ => true, cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		List<TestEntity>? result = await testContext.GetWithFilterFull(x => x.Name == targetName, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetWithFilter_WithDisableAllFiltersTrue_ShouldDisableAllFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(true, x => x.Name == targetName, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetWithFilter_WithFilterNamesToDisable_ShouldDisableSpecificFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["TestFilter", "AnotherFilter"] };

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(true, x => x.Name == targetName, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetWithFilter_WithEmptyFilterNamesToDisableAndDisableAllFiltersTrue_ShouldDisableAllFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = true,
			FilterNamesToDisable = Array.Empty<string>()
		};

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
	}

	[Fact]
	public async Task GetWithFilterFull_WithNullFilterNamesToDisableAndDisableAllFiltersFalse_ShouldNotDisableFilters()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = null
		};

		// Act
		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
	}

	[Fact]
	public async Task GetWithFilter_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "SomeFilter", "AnotherFilter" }
		};

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
	}

	[Fact]
	public async Task GetWithFilterFull_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "FilterName1", "FilterName2" }
		};

		// Act
		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
	}

	[Fact]
	public async Task GetWithFilterFull_WithSplitQueryOverrideTrue_ShouldUseSplitQuery()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		entity.Details = new List<TestEntityDetail> { fixture.Create<TestEntityDetail>() };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilterFull_WithSplitQueryOverrideFalse_ShouldUseSingleQuery()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		entity.Details = new List<TestEntityDetail> { fixture.Create<TestEntityDetail>() };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = false };

		// Act
		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_WithSplitQueryOverride_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		entity.Details = new List<TestEntityDetail> { fixture.Create<TestEntityDetail>() };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = false };
		List<TestEntity> results = new();

		// Act
		IAsyncEnumerable<TestEntity>? stream = testContext.GetWithFilterFullStreaming(filter, fullQueryOptions: options, cancellationToken: Current.CancellationToken);
		if (stream != null)
		{
			await foreach (TestEntity item in stream)
			{
				results.Add(item);
			}
		}

		// Assert
		results.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetWithFilterStreaming(
			x => x.Id > 0,
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity entity in stream)
			{
				results.Add(entity);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["Filter1", "Filter2"] };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetWithFilterFullStreaming(
			x => x.Id > 0,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity entity in stream)
			{
				results.Add(entity);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithProjectionAndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		List<string> results = [];
		IAsyncEnumerable<string>? stream = testContext.GetWithFilterStreaming(
			x => x.Id > 0,
			x => x.Name,
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (string name in stream)
			{
				results.Add(name);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_WithProjectionAndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = null };

		// Act
		List<string> results = [];
		IAsyncEnumerable<string>? stream = testContext.GetWithFilterFullStreaming(
			x => x.Id > 0,
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (string name in stream)
			{
				results.Add(name);
			}
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilter_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string>? results = await testContext.GetWithFilter(
			x => x.Id > 0,
			x => x.Name,
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = false },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterFull_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string>? results = await testContext.GetWithFilterFull(
			x => x.Id > 0,
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = null },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilter_WithSplitQuery_True_ShouldSucceed()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(true, x => x.Id > 0, fullQueryOptions: new FullQueryOptions { SplitQueryOverride = true }, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilter_WithSplitQuery_False_ShouldSucceed()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(true, x => x.Id > 0, fullQueryOptions: new FullQueryOptions { SplitQueryOverride = false }, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithSplitQuery_True_ShouldStreamResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity entity in testContext.GetWithFilterStreaming(true, x => x.Id > 0, fullQueryOptions: new FullQueryOptions { SplitQueryOverride = true },
			cancellationToken: Current.CancellationToken) ?? AsyncEnumerable.Empty<TestEntity>())
		{
			results.Add(entity);
		}

		// Assert
		results.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithSplitQuery_False_ShouldStreamResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity entity in testContext.GetWithFilterStreaming(true, x => x.Id > 0, fullQueryOptions: new FullQueryOptions { SplitQueryOverride = false },
			cancellationToken: Current.CancellationToken) ?? AsyncEnumerable.Empty<TestEntity>())
		{
			results.Add(entity);
		}

		// Assert
		results.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetWithFilterFull_WithTrackedEntities_ShouldTrackEntities()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(true, x => x.Id > 0, trackEntities: true, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithFullQuery_AndTracking_ShouldStreamResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity entity in testContext.GetWithFilterStreaming(true, x => x.Id > 0, trackEntities: true, cancellationToken: Current.CancellationToken) ?? AsyncEnumerable.Empty<TestEntity>())
		{
			results.Add(entity);
		}

		// Assert
		results.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetWithFilter_WithError_ShouldReturnNull()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		List<TestEntity>? result = await testContext.GetWithFilter(x => x.Id > 0, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithError_ShouldHandleGracefully()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity entity in testContext.GetWithFilterStreaming(false, x => x.Id > 0, cancellationToken: Current.CancellationToken) ?? AsyncEnumerable.Empty<TestEntity>())
		{
			results.Add(entity);
		}

		// Assert
		results.Count.ShouldBe(0);
	}

	[Fact]
	public async Task GetWithFilterFull_WithNavigationProperties_ShouldSucceed()
	{
		// Arrange
		const string dbName = nameof(GetWithFilterFull_WithNavigationProperties_ShouldSucceed);
		ServiceCollection services = new();
		services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(databaseName: dbName));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntity parent = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.With(x => x.Name, "CircularTest")
			.Create();
		await ctx.TestEntities.AddAsync(parent, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = fixture.Build<TestEntityDetail>()
			.With(x => x.TestEntityId, parent.Id)
			.Without(x => x.TestEntity)
			.Create();
		await ctx.AddAsync(detail, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		FullQueryOptions fullQueryOptions = new();

		// Act - GetWithFilterFull uses ExecuteWithCircularRefHandling
		List<TestEntity>? results = await testContext.GetWithFilterFull(
			x => x.Name == "CircularTest",
			fullQueryOptions: fullQueryOptions,
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Details.ShouldNotBeNull();
		results[0].Details!.Count.ShouldBe(1);
		await ctx.DisposeAsync();
	}

	[Fact]
	public async Task GetWithFilterStreamingFull_WithNavigationProperties_ShouldSucceed()
	{
		// Arrange
		const string dbName = nameof(GetWithFilterStreamingFull_WithNavigationProperties_ShouldSucceed);
		ServiceCollection services = new();
		services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(databaseName: dbName));
		IServiceProvider provider = services.BuildServiceProvider();
		IServiceScope scope = provider.CreateScope();
		TestDbContext ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntity parent = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.With(x => x.Name, "StreamTest")
			.Create();
		await ctx.TestEntities.AddAsync(parent, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail1 = fixture.Build<TestEntityDetail>()
			.With(x => x.TestEntityId, parent.Id)
			.Without(x => x.TestEntity)
			.Create();
		TestEntityDetail detail2 = fixture.Build<TestEntityDetail>()
			.With(x => x.TestEntityId, parent.Id)
			.Without(x => x.TestEntity)
			.Create();
		await ctx.AddRangeAsync(new object[] { detail1, detail2 }, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		FullQueryOptions fullQueryOptions = new();

		// Act - GetWithFilterFullStreaming uses ExecuteStreamingWithCircularRefHandling
		IAsyncEnumerable<TestEntity>? stream = testContext.GetWithFilterFullStreaming(
			x => x.Name == "StreamTest",
			fullQueryOptions: fullQueryOptions,
			cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
		List<TestEntity> results = new();
		await foreach (TestEntity entity in stream)
		{
			results.Add(entity);
		}

		results.Count.ShouldBe(1);
		results[0].Details.ShouldNotBeNull();
		results[0].Details!.Count.ShouldBe(2);

		// Cleanup
		await ctx.DisposeAsync();
		scope.Dispose();

	}

	#endregion

	#region GetOneWithFilter Tests

	[Fact]
	public async Task GetOneWithFilter_WithValidPredicate_ShouldReturnSingleEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(filter, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilterFullAndNot_WithValidPredicate_ShouldReturnSingleEntity(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(full, filter, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetOneWithFilterFull_ShouldReturnEntity()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetOneWithFilterFull(x => x.Name == targetName, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetOneWithFilter_WithProjection_ShouldReturnProjectedEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilter(x => x.Id == target.Id, x => x.Name, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntity(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilter(full, x => x.Id == target.Id, x => x.Name, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithProjection_ShouldReturnProjectedEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilterFull(x => x.Id == target.Id, x => x.Name, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetOneWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetOneWithFilterFull(x => x.Id == entity.Id, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilter_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(x => x.Id == entity.Id, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetOneWithFilter_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Id == entity.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Id == entity.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		TestEntity? result = await testContext.GetOneWithFilterFull(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetOneWithFilter_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Id == entity.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "Filter1" }
		};

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetOneWithFilter_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilter(
			x => x.Id == entity.Id,
			x => x.Name,
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true },
			cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(entity.Name);
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilterFull(
			x => x.Id == entity.Id,
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = [] },
			cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(entity.Name);
	}

	[Fact]
	public async Task GetOneWithFilter_WithError_ShouldReturnNull()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(x => x.Id > 0, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithCircularReferences_ShouldHandleGracefully()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection), ServiceLifetime.Transient);
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntity parent = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.With(x => x.Name, "UniqueCircular")
			.Create();
		await ctx.TestEntities.AddAsync(parent, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = fixture.Build<TestEntityDetail>()
			.With(x => x.TestEntityId, parent.Id)
			.Without(x => x.TestEntity)
			.Create();
		await ctx.AddAsync(detail, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		FullQueryOptions fullQueryOptions = new();

		// Act - GetOneWithFilterFull uses ExecuteWithCircularRefHandling
		TestEntity? result = await testContext.GetOneWithFilterFull(
			x => x.Name == "UniqueCircular",
			fullQueryOptions: fullQueryOptions,
			cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Details.ShouldNotBeNull();
		result.Details.Count.ShouldBe(1);

		await ctx.DisposeAsync();
		await connection.DisposeAsync();
	}

	#endregion

	#region GetMaxByOrder Tests

	[Fact]
	public async Task GetMaxByOrder_ShouldReturnEntityWithMaxValue()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(filter, orderExpression, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxByOrderFullAndNot_ShouldReturnEntityWithMaxValue(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(full, filter, orderExpression, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMaxByOrderFull_ShouldReturnEntityWithMaxValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMaxByOrderFull(_ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMaxByOrderFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetMaxByOrderFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxByOrder_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMaxByOrder_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(filter, orderExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMaxByOrder_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "MaxOrderFilter" }
		};

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(filter, orderExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	#endregion

	#region GetMinByOrder Tests

	[Fact]
	public async Task GetMinByOrder_ShouldReturnEntityWithMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrder(_ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinByOrderFullAndNot_ShouldReturnEntityWithMinValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrder(full, _ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task GetMinByOrderFull_ShouldReturnEntityWithMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrderFull(_ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMinByOrderFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetMinByOrderFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinByOrder_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMinByOrder(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task GetMinByOrder_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		TestEntity? result = await testContext.GetMinByOrder(filter, orderExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMinByOrder_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "MinOrderFilter" }
		};

		// Act
		TestEntity? result = await testContext.GetMinByOrder(filter, orderExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMinByOrderFull_WithCircularReferences_ShouldHandleGracefully()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection), ServiceLifetime.Transient);
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(3)
			.OrderBy(x => x.Id)
			.ToArray();
		await ctx.TestEntities.AddRangeAsync(entities);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		foreach (TestEntity entity in entities)
		{
			TestEntityDetail detail = fixture.Build<TestEntityDetail>()
				.With(x => x.TestEntityId, entity.Id)
				.Without(x => x.TestEntity)
				.Create();
			await ctx.AddAsync(detail, Current.CancellationToken);
		}
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		FullQueryOptions fullQueryOptions = new();

		// Act - GetMinByOrderFull uses ExecuteWithCircularRefHandling
		TestEntity? result = await testContext.GetMinByOrderFull(
			x => x.Id > 0,
			x => x.Id,
			fullQueryOptions: fullQueryOptions,
			cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Details.ShouldNotBeNull();
		result.Details.Count.ShouldBe(1);

		await ctx.DisposeAsync();
		await connection.DisposeAsync();
	}

	#endregion

	#region GetMax Tests

	[Fact]
	public async Task GetMax_ShouldReturnMaxValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMax(_ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxFullAndNot_ShouldReturnMaxValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMax(full, _ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMax_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int result = await testContext.GetMax(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMax_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> selectExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		int? result = await testContext.GetMax(filter, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMax_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> selectExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "MaxFilter" }
		};

		// Act
		int? result = await testContext.GetMax(filter, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMax_WithData_ShouldReturnMaxValue()
	{
		// Arrange
		List<TestEntity> entities = new()
		{
			new TestEntity { Id = 1, Name = "A" },
			new TestEntity { Id = 5, Name = "B" },
			new TestEntity { Id = 3, Name = "C" }
		};
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int? max = await testContext.GetMax(x => x.Id > 0, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert
		max.ShouldNotBeNull();
		max.Value.ShouldBe(5);
	}

	[Fact]
	public async Task GetMax_WithFilter_ShouldReturnMaxValue()
	{
		// Arrange
		List<TestEntity> entities = new()
		{
			new TestEntity { Id = 1, Name = "A" },
			new TestEntity { Id = 5, Name = "B" },
			new TestEntity { Id = 3, Name = "C" }
		};
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int? max = await testContext.GetMax(x => x.Id < 5, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert
		max.ShouldNotBeNull();
		max.Value.ShouldBe(3);
	}

	[Fact]
	public async Task GetMax_WithError_ShouldReturnZero()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		int? result = await testContext.GetMax(x => x.Id > 0, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert - returns 0 (default value) on error
		result.ShouldBe(0);
	}

	#endregion

	#region GetMin Tests

	[Fact]
	public async Task GetMin_ShouldReturnMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMin(_ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinFullAndNot_ShouldReturnMinValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMin(full, _ => true, x => x.Id, cancellationToken: Current.CancellationToken);

		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMin_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int result = await testContext.GetMin(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task GetMin_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> selectExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		int? result = await testContext.GetMin(filter, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMin_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> selectExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "MinFilter" }
		};

		// Act
		int? result = await testContext.GetMin(filter, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMin_WithData_ShouldReturnMinValue()
	{
		// Arrange
		List<TestEntity> entities = new()
		{
			new TestEntity { Id = 5, Name = "A" },
			new TestEntity { Id = 1, Name = "B" },
			new TestEntity { Id = 3, Name = "C" }
		};
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int? min = await testContext.GetMin(x => x.Id > 0, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert
		min.ShouldNotBeNull();
		min.Value.ShouldBe(1);
	}

	[Fact]
	public async Task GetMin_WithFilter_ShouldReturnMinValue()
	{
		// Arrange
		List<TestEntity> entities = new()
		{
			new TestEntity { Id = 5, Name = "A" },
			new TestEntity { Id = 1, Name = "B" },
			new TestEntity { Id = 3, Name = "C" }
		};
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int? min = await testContext.GetMin(x => x.Id > 1, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert
		min.ShouldNotBeNull();
		min.Value.ShouldBe(3);
	}

	[Fact]
	public async Task GetMin_WithError_ShouldReturnZero()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		int? result = await testContext.GetMin(x => x.Id > 0, x => x.Id, cancellationToken: Current.CancellationToken);

		// Assert - returns 0 (default value) on error
		result.ShouldBe(0);
	}

	#endregion

	#region GetCount Tests

	[Fact]
	public async Task GetCount_ShouldReturnCorrectCount()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int count = await testContext.GetCount(_ => true, cancellationToken: Current.CancellationToken);

		count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetCount_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		int result = await testContext.GetCount(filter, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetCount_WithQueryTimeout_ShouldSucceed()
	{
		// Arrange - Use SQLite for query timeout support
		(IServiceProvider sqliteProvider, TestDbContext sqliteContext, IDisposable scope) = CreateSqliteServiceProvider();
		try
		{
			List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
			await sqliteContext.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
			await sqliteContext.SaveChangesAsync(Current.CancellationToken);

			BaseDbContextActions<TestEntity, TestDbContext> testContext = new(sqliteProvider);

			// Act
			int count = await testContext.GetCount(x => x.Id > 0, queryTimeout: TimeSpan.FromSeconds(30), cancellationToken: Current.CancellationToken);

			// Assert
			count.ShouldBe(2);
		}
		finally
		{
			scope.Dispose();
		}
	}

	[Fact]
	public async Task GetCount_WithError_ShouldReturnZero()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act - should return 0 on error
		int result = await testContext.GetCount(x => x.Id > 0, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBe(0);
	}

	#endregion

	#region GetNavigationWithFilter Tests

	[Fact]
	public async Task GetNavigationWithFilter_ShouldReturnEntities()
	{
		// Setup related entities
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		List<TestEntity>? results = await testContext.GetNavigationWithFilter(where, select, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetNavigationWithFilterFull_ShouldReturnEntities()
	{
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		List<TestEntity>? results = await testContext.GetNavigationWithFilterFull(where, select, cancellationToken: Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetNavigationWithFilter_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilter(where, select, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetNavigationWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilter(full, where, select, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Fact]
	public async Task GetNavigationWithFilterStreaming_ShouldReturnStreamedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterStreaming(where, select, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetNavigationWithFilterStreamingFullAndNot_ShouldReturnStreamedEntities(bool full)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterStreaming(full, where, select, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Fact]
	public async Task GetNavigationWithFilterFullStreaming_ShouldReturnStreamedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterFullStreaming(where, select, cancellationToken: Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetNavigationWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? result = await testContext.GetNavigationWithFilterFull(where, select, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		result[0].Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetNavigationWithFilterFull_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> whereExpression = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilterFull(whereExpression, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetNavigationWithFilterFull_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> whereExpression = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "NavigationFilter" }
		};

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilterFull(whereExpression, selectExpression, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetNavigationWithFilterStreaming_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = new() { Description = "Detail", TestEntityId = entity.Id, TestEntity = entity };
		await context.Set<TestEntityDetail>().AddAsync(detail, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetNavigationWithFilterStreaming<TestEntityDetail>(
			d => d.Id > 0,
			d => d.TestEntity!,
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity e in stream)
			{
				results.Add(e);
			}
		}

		// Assert
		results.Count.ShouldBeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task GetNavigationWithFilterFullStreaming_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = new() { Description = "Detail", TestEntityId = entity.Id, TestEntity = entity };
		await context.Set<TestEntityDetail>().AddAsync(detail, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["FilterX"] };

		// Act
		List<TestEntity> results = [];
		IAsyncEnumerable<TestEntity>? stream = testContext.GetNavigationWithFilterFullStreaming<TestEntityDetail>(
			d => d.Id > 0,
			d => d.TestEntity!,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: filterOptions,
			cancellationToken: Current.CancellationToken);

		if (stream != null)
		{
			await foreach (TestEntity e in stream)
			{
				results.Add(e);
			}
		}

		// Assert
		results.Count.ShouldBeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task GetNavigationWithFilter_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = new() { Description = "Detail", TestEntityId = entity.Id, TestEntity = entity };
		await context.Set<TestEntityDetail>().AddAsync(detail, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilter<TestEntityDetail>(
			d => d.Id > 0,
			d => d.TestEntity!,
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task GetNavigationWithFilterFull_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		TestEntityDetail detail = new() { Description = "Detail", TestEntityId = entity.Id, TestEntity = entity };
		await context.Set<TestEntityDetail>().AddAsync(detail, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilterFull<TestEntityDetail>(
			d => d.Id > 0,
			d => d.TestEntity!,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1"] },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBeGreaterThanOrEqualTo(1);
	}

	#endregion

	#region GetWithPagingFilter Tests

	[Fact]
	public async Task GetWithPagingFilter_ShouldReturnPagedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id), skip: 1, pageSize: 2,
			cancellationToken: Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithPagingFilterFullAndNot_ShouldReturnPagedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(full, whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id), skip: 1, pageSize: 2,
			cancellationToken: Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_ShouldReturnPagedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1, pageSize: 2,
			cancellationToken: Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithPagingFilterFullAndNot_TKey_ShouldReturnPagedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(full, whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1, pageSize: 2,
			cancellationToken: Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 1, pageSize: 2, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithProjection_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<string> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x.Name, orderByString: nameof(TestEntity.Id),
			skip: 1, pageSize: 2, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
		result.Entities.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetWithPagingFilterFull_TKey_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1,
			pageSize: 2, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithZeroPageSize_ShouldUseZeroPageSize()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 0, pageSize: 0, cancellationToken: Current.CancellationToken);

		// Assert - This overload uses pageSize directly, so 0 means 0 entities
		result.Entities.Count.ShouldBe(entities.Count);
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_WithZeroPageSize_ShouldReturnAll()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id,
			skip: 0, pageSize: 0, cancellationToken: Current.CancellationToken);

		// Assert - This overload treats 0 as int.MaxValue
		result.Entities.Count.ShouldBe(entities.Count);
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithLargeSkip_ShouldHandleCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 10, pageSize: 2, cancellationToken: Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(0);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_WithLargeSkip_ShouldHandleCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(
			whereExpression: _ => true,
			selectExpression: x => x,
			ascendingOrderExpression: x => x.Id,
			skip: 10, // Skip more than exists
			pageSize: 2,
			cancellationToken: Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(0);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilter(filter, selectExpression, skip: 0, pageSize: 2, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilterFull(filter, selectExpression, nameof(TestEntity.Id), skip: 0, pageSize: 2, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "PagingFilter" }
		};

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilter(filter, selectExpression, skip: 0, pageSize: 2, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithSpecificFilterNamesToDisable_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "PagingFullFilter" }
		};

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilterFull(filter, selectExpression, nameof(TestEntity.Id), skip: 0, pageSize: 2, globalFilterOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithTrackedEntitiesAndSplitQuery_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		foreach (TestEntity entity in entities)
		{
			entity.Details = new List<TestEntityDetail> { fixture.Create<TestEntityDetail>() };
		}
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilterFull(filter, selectExpression, nameof(TestEntity.Id), skip: 0, pageSize: 2, trackEntities: true, fullQueryOptions: options, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithGlobalFilterOptions_DisableAll_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> results = await testContext.GetWithPagingFilter(
			x => x.Id > 0,
			x => x,
			skip: 0,
			pageSize: 3,
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Entities.Count.ShouldBe(3);
		results.TotalRecords.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> results = await testContext.GetWithPagingFilter(
			x => x.Id > 0,
			x => x,
			x => x.Id,
			skip: 0,
			pageSize: 3,
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1"] },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Entities.Count.ShouldBe(3);
		results.TotalRecords.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_TKey_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> results = await testContext.GetWithPagingFilterFull(
			x => x.Id > 0,
			x => x,
			x => x.Id,
			skip: 0,
			pageSize: 3,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = false },
			cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Entities.Count.ShouldBe(3);
		results.TotalRecords.ShouldBeGreaterThanOrEqualTo(3);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithZeroPageSize_ShouldReturnAllMatching()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act - pageSize 0 should return all records
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilter(x => x.Id > 0, e => e, skip: 0, pageSize: 0, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(5);
		result.Entities.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithZeroPageSize_ShouldReturnAllMatching()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilterFull(x => x.Id > 0, e => e, orderByString: "Id", skip: 0, pageSize: 0, fullQueryOptions: new FullQueryOptions(), cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(5);
		result.Entities.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithError_ShouldThrow()
	{
		// Arrange
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContext ctx = provider.GetRequiredService<TestDbContext>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);
		await ctx.DisposeAsync();
		await connection.DisposeAsync();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act & Assert - throws because ApplyTrackingAndFilters is called before error handling
		await Should.ThrowAsync<ObjectDisposedException>(async () =>
			await testContext.GetWithPagingFilter(x => x.Id > 0, e => e, skip: 0, pageSize: 10));
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithNavigationProperties_ShouldSucceed()
	{
		// Arrange - Test GetWithPagingFilterFull with navigation properties (circular reference handling)
		TestEntity[] entities = fixture.Build<TestEntity>()
			.Without(x => x.Details)
			.CreateMany(5)
			.ToArray();

		foreach (TestEntity entity in entities)
		{
			entity.Details = new List<TestEntityDetail>
			{
				fixture.Build<TestEntityDetail>()
					.Without(x => x.TestEntity)
					.Create()
			};
		}

		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		FullQueryOptions fullQueryOptions = new();

		// Act - GetWithPagingFilterFull uses ExecuteWithCircularRefHandling
		GenericPagingModel<TestEntity>? result = await testContext.GetWithPagingFilterFull(
			x => x.Id > 0,
			x => x,
			nameof(TestEntity.Id),
			skip: 0,
			pageSize: 10,
			fullQueryOptions: fullQueryOptions,
			cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.TotalRecords.ShouldBe(5);
		result.Entities.Count.ShouldBe(5);
	}

	#endregion

	#region GetQuery Tests

	[Fact]
	public void GetQueryAll_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAll(x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAll();

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull();

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilter(_ => true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithHandlingCircularRef_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(handlingCircularRefException: true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithHandlingCircularRef_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, handlingCircularRefException: true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilter(_ => true, x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(_ => true, x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryNavigationWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, nameof(TestEntity.Id));

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_TKey_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, x => x.Id);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryAllFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryNavigationWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryPagingWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, nameof(TestEntity.Id), fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryPagingWithFilterFull_TKey_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, x => x.Id, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAll_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAll(trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAll_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAll(x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilter_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilter(_ => true, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilter_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilter(_ => true, x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAllFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAllFull_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilterFull_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(_ => true, x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryNavigationWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryPagingWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			_ => true,
			x => x,
			nameof(TestEntity.Id),
			trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryPagingWithFilterFull_TKey_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			_ => true,
			x => x,
			x => x.Id,
			trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryAll(globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryWithFilter(filter, globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryAllFull(globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryWithFilterFull(filter, globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryNavigationWithFilterFull_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereExpression = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryNavigationWithFilterFull(whereExpression, selectExpression, globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, TestEntity>> selectExpression = x => x;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryPagingWithFilterFull(filter, selectExpression, nameof(TestEntity.Id), globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_WithSpecificFilterNamesToDisable_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "QueryFilter" }
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryAll(globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithSpecificFilterNamesToDisable_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "QueryFilter1", "QueryFilter2" }
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryWithFilter(filter, globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithSpecificFilterNamesToDisable_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "FullFilter" }
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryAllFull(globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithSpecificFilterNamesToDisable_ShouldReturnQueryable()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> filter = _ => true;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions options = new()
		{
			DisableAllFilters = false,
			FilterNamesToDisable = new[] { "FullQueryFilter" }
		};

		// Act
		IQueryable<TestEntity> result = testContext.GetQueryWithFilterFull(filter, globalFilterOptions: options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_WithGlobalFilterOptions_AndDisableAllFilters_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAll(globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_WithProjection_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAll(x => x.Name, globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1"] });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithGlobalFilterOptions_DisableAllFalse_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilter(x => x.Id > 0, globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = false });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithProjection_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilter(
			x => x.Id > 0,
			x => x.Name,
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = null });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithGlobalFilterOptions_DisableAll_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithProjection_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1", "Filter2"] });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithGlobalFilterOptions_DisableAll_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(
			x => x.Id > 0,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithProjection_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(
			x => x.Id > 0,
			x => x.Name,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = [] });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryNavigationWithFilterFull_WithGlobalFilterOptions_DisableAllFalse_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull<TestEntityDetail>(
			d => d.Id > 0,
			d => d.TestEntity!,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = false });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_WithGlobalFilterOptions_FilterNames_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			x => x.Id > 0,
			x => x,
			"Id",
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { FilterNamesToDisable = ["Filter1"] });

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_TKey_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			x => x.Id > 0,
			x => x,
			x => x.Id,
			fullQueryOptions: new FullQueryOptions(),
			globalFilterOptions: new GlobalFilterOptions { DisableAllFilters = true });

		// Assert
		query.ShouldNotBeNull();
	}

	#endregion

	#region Additional Read Tests

	[Fact]
	public async Task GetByKey_Array_WithGlobalFilterOptions_DisableAllFilters_ShouldWork()
	{
		// Arrange - Using separate entity type to avoid cache interference
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForFilters>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContextForFilters ctx = provider.GetRequiredService<TestDbContextForFilters>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntityForFilters entity = fixture.Create<TestEntityForFilters>();
		await ctx.TestEntities.AddAsync(entity, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntityForFilters, TestDbContextForFilters> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityForFilters? result = await testContext.GetByKey(new object[] { entity.Id }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
		await connection.DisposeAsync();
	}

	[Fact]
	public async Task GetByKey_Array_WithGlobalFilterOptions_FilterNamesToDisable_ShouldWork()
	{
		// Arrange - Using separate entity type to avoid cache interference
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForFilters>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContextForFilters ctx = provider.GetRequiredService<TestDbContextForFilters>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntityForFilters entity = fixture.Create<TestEntityForFilters>();
		await ctx.TestEntities.AddAsync(entity, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntityForFilters, TestDbContextForFilters> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { FilterNamesToDisable = ["TestFilter"] };

		// Act
		TestEntityForFilters? result = await testContext.GetByKey(new object[] { entity.Id }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
		await connection.DisposeAsync();
	}

	[Fact]
	public async Task GetByKeyFull_Array_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange - Using separate entity type to avoid cache interference
		SqliteConnection connection = new("DataSource=:memory:");
		await connection.OpenAsync(Current.CancellationToken);
		ServiceCollection services = new();
		services.AddDbContext<TestDbContextForFilters>(options => options.UseSqlite(connection));
		IServiceProvider provider = services.BuildServiceProvider();
		TestDbContextForFilters ctx = provider.GetRequiredService<TestDbContextForFilters>();
		await ctx.Database.EnsureCreatedAsync(Current.CancellationToken);

		TestEntityForFilters entity = fixture.Build<TestEntityForFilters>()
			.With(x => x.CreatedDate, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.Create();
		await ctx.TestEntities.AddAsync(entity, Current.CancellationToken);
		await ctx.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntityForFilters, TestDbContextForFilters> testContext = new(provider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };

		// Act
		TestEntityForFilters? result = await testContext.GetByKeyFull(new object[] { entity.Id }, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
		await connection.DisposeAsync();
	}

	[Fact]
	public async Task GetAll_WithGlobalFilterOptions_AndProjection_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		List<string>? results = await testContext.GetAll(e => e.Name, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(3);
	}

	[Fact]
	public void GetAllStreaming_WithGlobalFilterOptions_ShouldReturnStreamedResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false, FilterNamesToDisable = [] };

		// Act
		IAsyncEnumerable<TestEntity>? stream = testContext.GetAllStreaming(globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
	}

	[Fact]
	public void GetAllStreaming_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		IAsyncEnumerable<string>? stream = testContext.GetAllStreaming(e => e.Name, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilter_WithGlobalFilterOptions_AndProjection_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		List<string>? results = await testContext.GetWithFilter(e => e.Id > 0, e => e.Name, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(3);
	}

	[Fact]
	public async Task GetOneWithFilter_WithGlobalFilterOptions_AndProjection_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		string? result = await testContext.GetOneWithFilter(e => e.Id == entity.Id, e => e.Name, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(entity.Name);
	}

	[Fact]
	public async Task GetMaxByOrder_WithWhereExpression_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(e => e.Id > 0, e => e.Id, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMinByOrder_WithWhereExpression_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		TestEntity? result = await testContext.GetMinByOrder(e => e.Id > 0, e => e.Id, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMax_WithGlobalFilterOptions_AndWhereExpression_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		int result = await testContext.GetMax(e => e.Id > 0, e => e.Id, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetMin_WithGlobalFilterOptions_AndWhereExpression_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		int result = await testContext.GetMin(e => e.Id > 0, e => e.Id, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithOrderByString_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(e => e.Id > 0, e => e, "Id", 0, 2, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithAscendingOrderExpression_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(e => e.Id > 0, e => e, e => e.Id, 0, 2, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(2);
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_WithOrderByString_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = false };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(e => e.Id > 0, e => e, "Id", fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions);

		// Assert
		query.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetAllFull_WithProjection_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = false };

		// Act
		List<string>? results = await testContext.GetAllFull(e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(3);
	}

	[Fact]
	public void GetAllFullStreaming_WithProjection_AndGlobalFilterOptions_ShouldReturnStream()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = true };

		// Act
		IAsyncEnumerable<string>? stream = testContext.GetAllFullStreaming(e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
	}

	[Fact]
	public void GetAllFullStreaming_WithGlobalFilterOptions_ShouldReturnStream()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false, FilterNamesToDisable = [] };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = false };

		// Act
		IAsyncEnumerable<TestEntity>? stream = testContext.GetAllFullStreaming(fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithFilterFull_WithProjection_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = null };

		// Act
		List<string>? results = await testContext.GetWithFilterFull(e => e.Id > 0, e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(3);
	}

	[Fact]
	public void GetWithFilterFullStreaming_WithProjection_WithGlobalFilterOptions_ShouldReturnStream()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = null };

		// Act
		IAsyncEnumerable<string>? stream = testContext.GetWithFilterFullStreaming(e => e.Id > 0, e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		stream.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithProjection_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = false };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(e => e.Id > 0, e => e, "Id", 0, 2, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithAscendingOrderExpression_WithGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(e => e.Id > 0, e => e, e => e.Id, 0, 2, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(2);
	}

	[Fact]
	public void GetQueryAllFull_WithProjection_AndGlobalFilterOptions_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = null };

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions);

		// Assert
		query.ShouldNotBeNull();
		query.Count().ShouldBe(3);
	}

	[Fact]
	public void GetQueryWithFilterFull_WithProjection_WithGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = null };

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(e => e.Id > 0, e => e.Name, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions);

		// Assert
		query.ShouldNotBeNull();
		query.Count().ShouldBe(3);
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_WithAscendingOrderExpression_AndGlobalFilterOptions_ShouldReturnQueryable()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullQueryOptions = new() { SplitQueryOverride = null };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(e => e.Id > 0, e => e, e => e.Id, fullQueryOptions: fullQueryOptions, globalFilterOptions: filterOptions);

		// Assert
		query.ShouldNotBeNull();
	}

	#endregion

	#region Error Path and Edge Case Tests

	[Fact]
	public async Task GetByKeyFull_CompoundKey_WithGlobalFilterOptions_DisableAllFilters_ShouldWork()
	{
		// Arrange
		TestEntityWithCompoundKey entity = fixture.Create<TestEntityWithCompoundKey>();
		await context.TestEntitiesWithCompoundKey.AddAsync(entity, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntityWithCompoundKey, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		TestEntityWithCompoundKey? result = await testContext.GetByKeyFull(new object[] { entity.Key1, entity.Key2 }, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Key1.ShouldBe(entity.Key1);
		result.Key2.ShouldBe(entity.Key2);
	}

	[Fact]
	public async Task GetAllFull_WithGlobalFilterOptions_AndFullQueryOptions_ShouldReturnEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = true };

		// Act
		List<TestEntity>? result = await testContext.GetAllFull(fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
	}

	[Fact]
	public async Task GetAllFull_WithProjection_WithGlobalFilterOptions_AndFullQueryOptions_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = true };

		// Act
		List<string>? result = await testContext.GetAllFull(e => e.Name, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(3);
	}

	[Fact]
	public async Task GetWithFilterFull_WithGlobalFilterOptions_DisableAllFilters_ShouldReturnFilteredEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		List<TestEntity>? result = await testContext.GetWithFilterFull(e => e.Id > 0, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithFilterFull_WithProjection_WithGlobalFilterOptions_DisableAllFilters_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = true };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		List<string>? result = await testContext.GetWithFilterFull(e => e.Id > 0, e => e.Name, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(5);
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithGlobalFilterOptions_ShouldReturnFirstMatch()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		TestEntity? result = await testContext.GetOneWithFilterFull(e => e.Id > 0, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithProjection_WithGlobalFilterOptions_ShouldReturnProjectedMatch()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		string? result = await testContext.GetOneWithFilterFull(e => e.Id > 0, e => e.Name, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetMaxByOrderFull_WithGlobalFilterOptions_ShouldReturnMaxEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		TestEntity? result = await testContext.GetMaxByOrderFull(e => e.Id > 0, e => e.Id, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithGlobalFilterOptions_ShouldReturnPagedResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act - use orderByString instead of null
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(e => e.Id > 0, e => e, orderByString: "Id", skip: 0, pageSize: 5, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(5);
		result.TotalRecords.ShouldBe(10);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithOrderExpression_WithGlobalFilterOptions_ShouldReturnOrderedPagedResults()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, Current.CancellationToken);
		await context.SaveChangesAsync(Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		GlobalFilterOptions filterOptions = new() { DisableAllFilters = false };
		FullQueryOptions fullOptions = new() { SplitQueryOverride = false };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(e => e.Id > 0, e => e, e => e.Id, skip: 0, pageSize: 5, fullQueryOptions: fullOptions, globalFilterOptions: filterOptions, cancellationToken: Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Entities.Count.ShouldBe(5);
		result.TotalRecords.ShouldBe(10);
	}

	#endregion
}
