using CommonNetFuncs.EFCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;

public sealed class QueryExtensionsTests : IDisposable
{
	private readonly Fixture fixture;
	private readonly SqliteConnection connection;
	private readonly TestDbContext context;
	private bool disposed;

	public QueryExtensionsTests()
	{
		fixture = new Fixture();
		fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => fixture.Behaviors.Remove(x));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
		context = new TestDbContext(options);
		context.Database.EnsureCreated();
	}

	[Fact]
	public void WhereIf_WithTrueCondition_ShouldApplyPredicate()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		int targetId = entities[2].Id;
		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Id == targetId);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(1);
		resultList[0].Id.ShouldBe(targetId);
	}

	[Fact]
	public void WhereIf_WithFalseCondition_ShouldNotApplyPredicate()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		int targetId = entities[2].Id;
		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(false, x => x.Id == targetId);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(5); // Should return all entities
	}

	[Fact]
	public void WhereIf_WithTrueConditionAndStringPredicate_ShouldApplyFilter()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(4).ToList();
		string targetName = "SpecificName";
		entities[1].Name = targetName;
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Name == targetName);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(1);
		resultList[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public void WhereIf_WithMultipleConditions_ShouldChainProperly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Value = i * 10;
		}
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();
		bool applyFirstFilter = true;
		bool applySecondFilter = true;

		// Act
		IQueryable<TestEntity> result = query
			.WhereIf(applyFirstFilter, x => x.Value >= 30)
			.WhereIf(applySecondFilter, x => x.Value <= 60);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(4); // Values 30, 40, 50, 60
		resultList.ShouldAllBe(x => x.Value >= 30 && x.Value <= 60);
	}

	[Fact]
	public void WhereIf_WithFirstConditionFalse_ShouldOnlyApplySecondFilter()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Value = i * 10;
		}
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();
		bool applyFirstFilter = false;
		bool applySecondFilter = true;

		// Act
		IQueryable<TestEntity> result = query
			.WhereIf(applyFirstFilter, x => x.Value >= 30)
			.WhereIf(applySecondFilter, x => x.Value <= 60);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(7); // Values 0, 10, 20, 30, 40, 50, 60
		resultList.ShouldAllBe(x => x.Value <= 60);
	}

	[Fact]
	public void WhereIf_WithBothConditionsFalse_ShouldReturnAllEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();
		bool applyFirstFilter = false;
		bool applySecondFilter = false;

		// Act
		IQueryable<TestEntity> result = query
			.WhereIf(applyFirstFilter, x => x.Value >= 100)
			.WhereIf(applySecondFilter, x => x.Value <= 50);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(5);
	}

	[Fact]
	public void WhereIf_WithEmptyQueryable_ShouldReturnEmpty()
	{
		// Arrange
		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Id > 0);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(0);
	}

	[Fact]
	public void WhereIf_WithComplexPredicate_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].Name = i % 2 == 0 ? "Even" : "Odd";
			entities[i].Value = i * 10;
		}
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Name == "Even" && x.Value >= 40);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.ShouldAllBe(x => x.Name == "Even" && x.Value >= 40);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 5)]
	public void WhereIf_WithTheoryData_ShouldBehaveCorrectly(bool condition, int expectedCount)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		int targetId = entities[0].Id;
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(condition, x => x.Id == targetId);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.Count.ShouldBe(expectedCount);
	}

	[Fact]
	public void WhereIf_WithNullCheckPredicate_ShouldWork()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		entities[0].Description = null;
		entities[1].Description = "HasDescription";
		entities[2].Description = null;
		entities[3].Description = "AlsoHasDescription";
		entities[4].Description = null;
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Description != null);
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(2);
		resultList.ShouldAllBe(x => x.Description != null);
	}

	[Fact]
	public void WhereIf_WithDateComparison_ShouldWork()
	{
		// Arrange
		DateTime referenceDate = DateTime.UtcNow.AddDays(-5);
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		for (int i = 0; i < entities.Count; i++)
		{
			entities[i].CreatedDate = referenceDate.AddDays(i);
		}
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.CreatedDate >= referenceDate.AddDays(2));
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.ShouldNotBeNull();
		resultList.Count.ShouldBe(3); // Days 2, 3, 4
	}

	[Fact]
	public void WhereIf_WithDeferredExecution_ShouldNotExecuteImmediately()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		IQueryable<TestEntity> query = context.TestEntities.AsQueryable();

		// Act - This should not execute the query yet
		IQueryable<TestEntity> result = query.WhereIf(true, x => x.Id > 0);

		// Add more entities
		List<TestEntity> moreEntities = fixture.CreateMany<TestEntity>(3).ToList();
		context.TestEntities.AddRange(moreEntities);
		context.SaveChanges();

		// Execute the query now
		List<TestEntity> resultList = result.ToList();

		// Assert
		resultList.Count.ShouldBe(8); // Should include all 8 entities (5 + 3)
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposed)
		{
			return;
		}

		if (disposing)
		{
			context.Dispose();
			connection.Close();
			connection.Dispose();
		}

		disposed = true;
	}
}
