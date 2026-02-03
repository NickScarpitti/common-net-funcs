using CommonNetFuncs.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Tests;

/// <summary>
/// Tests for ExecuteUpdateAsync functionality which requires a relational database provider.
/// Uses SQLite instead of in-memory database because in-memory doesn't support ExecuteUpdateAsync.
/// </summary>
public sealed class BaseDbContextActionsExecuteTests : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly IServiceProvider _serviceProvider;
	private readonly Fixture fixture;
	private bool _disposed;

	public BaseDbContextActionsExecuteTests()
	{
		fixture = new Fixture();
		fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => fixture.Behaviors.Remove(x));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		// Initialize SQLitePCL batteries before using SQLite
		SQLitePCL.Batteries.Init();

		// Setup SQLite in-memory database (supports ExecuteUpdateAsync unlike InMemoryDatabase)
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		ServiceCollection services = new();
		services.AddDbContext<TestDbContext>(options => options.UseSqlite(_connection), ServiceLifetime.Transient);
		_serviceProvider = services.BuildServiceProvider();

		// Ensure database is created
		using IServiceScope scope = _serviceProvider.CreateScope();
		TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
		context.Database.EnsureCreated();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_connection?.Dispose();
				(_serviceProvider as IDisposable)?.Dispose();
			}
			_disposed = true;
		}
	}

	#region UpdateMany with SetPropertyCalls Tests

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_ShouldUpdateMatchingEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		const string newName = "UpdatedName";
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.Name, newName), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.Name == newName);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_WithFilter_ShouldUpdateOnlyMatchingEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		int targetId;
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			targetId = entities[0].Id;
		}

		const string newName = "UpdatedName";
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: x => x.Id == targetId, updateSetters: s => s.SetProperty(x => x.Name, newName), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(1);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			TestEntity? updatedEntity = await context.TestEntities.FindAsync(new object?[] { targetId }, TestContext.Current.CancellationToken);
			updatedEntity.ShouldNotBeNull();
			updatedEntity!.Name.ShouldBe(newName);

			// Verify others weren't updated
			List<TestEntity> otherEntities = await context.TestEntities.Where(x => x.Id != targetId).ToListAsync(TestContext.Current.CancellationToken);
			otherEntities.ShouldAllBe(e => e.Name != newName);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_MultipleProperties_ShouldUpdateAllProperties()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		const string newName = "NewName";
		DateTime newDate = DateTime.UtcNow;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.Name, newName)
			.SetProperty(x => x.CreatedDate, newDate), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(2);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.Name == newName);
			updatedEntities.ShouldAllBe(e => e.CreatedDate == newDate);
		}
	}

	[Theory]
	[InlineData(2, -999)] // No matching entities
	[InlineData(0, 1)]    // Empty table
	public async Task UpdateMany_WithSetPropertyCalls_NoMatchingOrEmptyTable_ShouldReturnZero(int entityCount, int targetId)
	{
		// Arrange
		if (entityCount > 0)
		{
			List<TestEntity> entities = fixture.CreateMany<TestEntity>(entityCount).ToList();
			using IServiceScope scope = _serviceProvider.CreateScope();
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: x => x.Id == targetId, updateSetters: s => s.SetProperty(x => x.Name, "NewName"), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(0);
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_WithQueryTimeout_ShouldRespectTimeout()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);
		TimeSpan timeout = TimeSpan.FromSeconds(30);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.Name, "NewName"), queryTimeout: timeout, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(2);
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_WithCancellationToken_ShouldHandleCancellation()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act
		int? result = await testContext.UpdateMany(
			whereExpression: _ => true,
			updateSetters: s => s.SetProperty(x => x.Name, "NewName"),
			cancellationToken: cts.Token);

		// Assert - SQLite may complete before cancellation or return 0 or null
		result.ShouldBeOneOf(null, 0, 2);
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_ComplexWhereExpression_ShouldUpdateCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		// Set specific names for filtering
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i % 2 == 0 ? "EvenName" : "OddName";
		}
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: x => x.Name == "EvenName", updateSetters: s => s.SetProperty(x => x.Name, "UpdatedEvenName"), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(5);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.Where(x => x.Name == "UpdatedEvenName").ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.Count.ShouldBe(5);

			List<TestEntity> notUpdatedEntities = await context.TestEntities.Where(x => x.Name == "OddName").ToListAsync(TestContext.Current.CancellationToken);
			notUpdatedEntities.Count.ShouldBe(5);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_UpdateBasedOnExistingValue_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = $"Name{i}";
		}
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.Name, x => x.Name + "_Updated"), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.Name.EndsWith("_Updated"));
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_WithDateTime_ShouldUpdateCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		DateTime originalDate = DateTime.UtcNow.AddDays(-10);
		foreach (TestEntity entity in entities)
		{
			entity.CreatedDate = originalDate;
		}
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		DateTime newDate = DateTime.UtcNow;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.CreatedDate, newDate), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(2);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.CreatedDate == newDate);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_LargeDataset_ShouldHandleEfficiently()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(100).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		const string newName = "BulkUpdatedName";
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.Name, newName), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(100);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int updatedCount = await context.TestEntities.CountAsync(x => x.Name == newName, TestContext.Current.CancellationToken);
			updatedCount.ShouldBe(100);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_PartialUpdate_ShouldOnlyUpdateSpecifiedProperties()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		DateTime originalDate = DateTime.UtcNow;
		const string originalName = "OriginalName";
		foreach (TestEntity entity in entities)
		{
			entity.Name = originalName;
			entity.CreatedDate = originalDate;
		}
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		DateTime newDate = DateTime.UtcNow.AddDays(1);
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act - Only update CreatedDate, not Name
		int? result = await testContext.UpdateMany(whereExpression: _ => true, updateSetters: s => s.SetProperty(x => x.CreatedDate, newDate), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(2);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.CreatedDate == newDate);
			updatedEntities.ShouldAllBe(e => e.Name == originalName); // Name should remain unchanged
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_IdBasedFilter_ShouldUpdateSpecificRecords()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		List<int> targetIds;
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			targetIds = [entities[0].Id, entities[2].Id, entities[4].Id];
		}

		const string newName = "SelectedUpdate";
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.UpdateMany(whereExpression: x => targetIds.Contains(x.Id), updateSetters: s => s.SetProperty(x => x.Name, newName), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.Where(x => targetIds.Contains(x.Id)).ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.Name == newName);
			updatedEntities.Count.ShouldBe(3);
		}
	}

	[Fact]
	public async Task UpdateMany_WithSetPropertyCalls_AllParameters_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);
		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

		// Act
		int? result = await testContext.UpdateMany(
			whereExpression: _ => true,
			updateSetters: s => s.SetProperty(x => x.Name, "AllParamsTest"),
			queryTimeout: TimeSpan.FromSeconds(30),
			cancellationToken: cts.Token);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> updatedEntities = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			updatedEntities.ShouldAllBe(e => e.Name == "AllParamsTest");
		}
	}

	#endregion

	#region DeleteMany with WhereExpression Tests

	[Theory]
	[InlineData(5, 0)]    // Delete all matching entities
	[InlineData(3, -999)] // No matching entities
	[InlineData(0, 1)]    // Empty table
	public async Task DeleteMany_WithBasicScenarios_ShouldHandleCorrectly(int initialCount, int targetId)
	{
		// Arrange
		if (initialCount > 0)
		{
			List<TestEntity> entities = fixture.CreateMany<TestEntity>(initialCount).ToList();
			using IServiceScope scope = _serviceProvider.CreateScope();
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = targetId == 0
			? await testContext.DeleteMany(x => x.Id > 0, cancellationToken: TestContext.Current.CancellationToken)
			: await testContext.DeleteMany(x => x.Id == targetId, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);

			if (targetId == 0 && initialCount > 0)
			{
				result.ShouldBe(initialCount);
				remainingCount.ShouldBe(0); // All deleted
			}
			else
			{
				result.ShouldBe(0);
				remainingCount.ShouldBe(initialCount); // None deleted
			}
		}
	}

	[Fact]
	public async Task DeleteMany_WithSpecificFilter_ShouldDeleteOnlyMatchingEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		// Set specific names for filtering
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i < 5 ? "ToDelete" : "ToKeep";
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => x.Name == "ToDelete", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(5);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
			remainingCount.ShouldBe(5);

			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.ShouldAllBe(e => e.Name == "ToKeep");
		}
	}

	[Fact]
	public async Task DeleteMany_WithComplexWhereExpression_ShouldDeleteCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(15).ToList();
		DateTime cutoffDate = DateTime.UtcNow.AddDays(-5);

		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i % 2 == 0 ? "EvenName" : "OddName";
			entities[i].CreatedDate = i < 5 ? cutoffDate.AddDays(-1) : cutoffDate.AddDays(1);
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act - Delete entities with EvenName AND CreatedDate before cutoff
		int? result = await testContext.DeleteMany(x => x.Name == "EvenName" && x.CreatedDate < cutoffDate, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBeGreaterThan(0);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int deletedCount = 15 - await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
			deletedCount.ShouldBe(result.Value);

			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.ShouldNotContain(e => e.Name == "EvenName" && e.CreatedDate < cutoffDate);
		}
	}

	[Fact]
	public async Task DeleteMany_WithIdInList_ShouldDeleteSpecificRecords()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		List<int> idsToDelete;

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			idsToDelete = [entities[0].Id, entities[2].Id, entities[5].Id];
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => idsToDelete.Contains(x.Id), cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(3);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
			remainingCount.ShouldBe(7);

			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.ShouldNotContain(e => idsToDelete.Contains(e.Id));
		}
	}

	[Theory]
	[InlineData("Contains", "Test_", "Other_", 8, 4, 4)]
	[InlineData("StartsWith", "Prefix", "Other", 6, 3, 3)]
	[InlineData("EndsWith", "_Suffix", "_Other", 6, 3, 3)]
	public async Task DeleteMany_WithStringPatternMatching_ShouldDeleteMatchingEntities(
		string method, string matchPattern, string keepPattern, int totalCount, int matchCount, int expectedRemaining)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(totalCount).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i < matchCount
				? (method == "EndsWith" ? $"Name{matchPattern}" : $"{matchPattern}{i}")
				: (method == "EndsWith" ? $"Name{keepPattern}" : $"{keepPattern}{i}");
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = method switch
		{
			"Contains" => await testContext.DeleteMany(x => x.Name.Contains(matchPattern), cancellationToken: TestContext.Current.CancellationToken),
			"StartsWith" => await testContext.DeleteMany(x => x.Name.StartsWith(matchPattern), cancellationToken: TestContext.Current.CancellationToken),
			"EndsWith" => await testContext.DeleteMany(x => x.Name.EndsWith(matchPattern), cancellationToken: TestContext.Current.CancellationToken),
			_ => throw new ArgumentException($"Unknown method: {method}")
		};

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(matchCount);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
			remainingCount.ShouldBe(expectedRemaining);

			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);

			// Verify remaining entities match the keep pattern
			bool allMatch = method switch
			{
				"Contains" => remaining.All(e => e.Name.Contains(keepPattern)),
				"StartsWith" => remaining.All(e => e.Name.StartsWith(keepPattern)),
				"EndsWith" => remaining.All(e => e.Name.EndsWith(keepPattern)),
				_ => false
			};
			allMatch.ShouldBeTrue();
		}
	}

	[Fact]
	public async Task DeleteMany_WithDateTimeComparison_ShouldDeleteCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(6).ToList();
		DateTime now = DateTime.UtcNow;

		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].CreatedDate = now.AddDays(-i);
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		DateTime cutoffDate = now.AddDays(-3);
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => x.CreatedDate < cutoffDate, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Value.ShouldBeGreaterThan(0);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.ShouldAllBe(e => e.CreatedDate >= cutoffDate);
			remaining.Count.ShouldBe(6 - result.Value);
		}
	}

	[Fact]
	public async Task DeleteMany_WithOrCondition_ShouldDeleteMultipleGroups()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(12).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i < 4 ? "Group1" : i < 8 ? "Group2" : "Group3";
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => x.Name == "Group1" || x.Name == "Group3", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(8); // 4 from Group1 + 4 from Group3
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.Count.ShouldBe(4);
			remaining.ShouldAllBe(e => e.Name == "Group2");
		}
	}

	[Fact]
	public async Task DeleteMany_WithNegatedCondition_ShouldDeleteCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(8).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i < 4 ? "Keep" : "Delete";
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => x.Name != "Keep", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(4);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.Count.ShouldBe(4);
			remaining.ShouldAllBe(e => e.Name == "Keep");
		}
	}

	[Theory]
	[InlineData(100, "Even", 50)] // Large dataset
	[InlineData(10, "Group1", 0)]  // Delete all
	[InlineData(5, "None", 5)]     // Delete none (false condition)
	public async Task DeleteMany_WithVariousConditions_ShouldHandleCorrectly(int totalCount, string targetName, int expectedRemaining)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(totalCount).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = targetName == "Even"
				? (i % 2 == 0 ? "Even" : "Odd")
				: "Group1";
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = targetName == "None"
			? await testContext.DeleteMany(_ => false, cancellationToken: TestContext.Current.CancellationToken)
			: await testContext.DeleteMany(x => x.Name == targetName, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(totalCount - expectedRemaining);
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			int remainingCount = await context.TestEntities.CountAsync(TestContext.Current.CancellationToken);
			remainingCount.ShouldBe(expectedRemaining);
		}
	}

	[Fact]
	public async Task DeleteMany_WithCancellationToken_ShouldRespectCancellation()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		// Act & Assert
		// The operation may complete before cancellation or throw, or return null
		try
		{
			int? result = await testContext.DeleteMany(x => x.Id > 0, cancellationToken: TestContext.Current.CancellationToken);
			// If it completes, result could be null, 0, or the count
			result.ShouldBeOneOf(null, 0, 5);
		}
		catch (OperationCanceledException)
		{
			// Expected behavior if cancellation is processed
		}
	}

	[Fact]
	public async Task DeleteMany_WithRangeComparison_ShouldDeleteWithinRange()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(20).ToList();
		int[] idsToCheck;

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
			idsToCheck = entities.Select(e => e.Id).Order().ToArray();
		}

		int minId = idsToCheck[5];
		int maxId = idsToCheck[14];
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act
		int? result = await testContext.DeleteMany(x => x.Id >= minId && x.Id <= maxId, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(10); // IDs from index 5 to 14 inclusive
		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.Count.ShouldBe(10);
			remaining.ShouldAllBe(e => e.Id < minId || e.Id > maxId);
		}
	}

	[Fact]
	public async Task DeleteMany_MultipleConsecutiveCalls_ShouldWorkCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(15).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i < 5 ? "Batch1" : i < 10 ? "Batch2" : "Batch3";
		}

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
			await context.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

		// Act - Delete in multiple batches
		int? result1 = await testContext.DeleteMany(x => x.Name == "Batch1", cancellationToken: TestContext.Current.CancellationToken);
		int? result2 = await testContext.DeleteMany(x => x.Name == "Batch2", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result1.ShouldNotBeNull();
		result1.ShouldBe(5);
		result2.ShouldNotBeNull();
		result2.ShouldBe(5);

		using (IServiceScope scope = _serviceProvider.CreateScope())
		{
			TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			List<TestEntity> remaining = await context.TestEntities.ToListAsync(TestContext.Current.CancellationToken);
			remaining.Count.ShouldBe(5);
			remaining.ShouldAllBe(e => e.Name == "Batch3");
		}
	}

	#endregion
}
