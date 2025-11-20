using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;

public sealed class CollectionsTests
{
	private readonly Fixture _fixture;
	private readonly TestDbContext _context;

	public CollectionsTests()
	{
		_fixture = new Fixture();
		_fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => _fixture.Behaviors.Remove(x));
		_fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
				.UseInMemoryDatabase(databaseName: _fixture.Create<string>())
				.Options;
		_context = new TestDbContext(options);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetObjectByPartial_WithMatchingNonNullFields_ReturnsCorrectEntity(bool ignoreDefaultValues)
	{
		// Arrange
		TestEntity entity1 = new() { Id = 1, Name = "Test1", Value = 100, CreatedDate = DateTime.UtcNow };
		TestEntity entity2 = new() { Id = 2, Name = "Test2", Value = 200, CreatedDate = DateTime.UtcNow.AddDays(1) };
		_context.TestEntities.AddRange(entity1, entity2);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test1", Value = 0 };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, ignoreDefaultValues);

		// Assert
		if (ignoreDefaultValues)
		{
			result.ShouldNotBeNull();
			result.Id.ShouldBe(entity1.Id);
			result.Name.ShouldBe(entity1.Name);
		}
		else
		{
			// With ignoreDefaultValues = false, Value = 0 will be included in search
			result.ShouldBeNull(); // Won't match because entity1.Value = 100, not 0
		}
	}

	[Fact]
	public void GetObjectByPartial_WithNullFields_IgnoresNullFields()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100, CreatedDate = DateTime.UtcNow };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test", Value = 100, Description = null };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithDateTimeUtc_MatchesCorrectly()
	{
		// Arrange
		DateTime utcDate = DateTime.UtcNow;
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100, CreatedDate = utcDate };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { CreatedDate = utcDate };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithDateTimeLocal_ConvertsToUtc()
	{
		// Arrange
		DateTime localDate = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
		DateTime utcDate = localDate.ToUniversalTime();
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100, CreatedDate = utcDate };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { CreatedDate = localDate };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithDateTimeUnspecified_HandlesCorrectly()
	{
		// Arrange
		DateTime unspecifiedDate = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100, CreatedDate = unspecifiedDate };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { CreatedDate = unspecifiedDate };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithDateTimeOffset_ConvertsToUtc()
	{
		// Arrange
		DateTimeOffset dateTimeOffset = new(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(-5));
		TestEntityWithDateTimeOffset entity = new() { Id = 1, Name = "Test", CreatedDate = dateTimeOffset };
		_context.TestEntitiesWithDateTimeOffset.Add(entity);
		_context.SaveChanges();

		TestEntityWithDateTimeOffset partialObject = new() { CreatedDate = dateTimeOffset };

		// Act
		TestEntityWithDateTimeOffset? result = _context.TestEntitiesWithDateTimeOffset.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithNoMatchingEntity_ReturnsNull()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test1", Value = 100 };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "NonExistent" };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetObjectByPartial_WithAllNullFields_ReturnsNull()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100 };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = null, Description = null };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetObjectByPartial_WithDefaultValues_IgnoresWhenFlagIsTrue()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100 };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test", Value = 0, Id = 0 }; // 0 is default for int

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, ignoreDefaultValues: true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public void GetObjectByPartial_WithDefaultValues_IncludesWhenFlagIsFalse()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 0 };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		// When ignoreDefaultValues = false, ALL non-null properties are included
		// So we need to match Id and avoid CreatedDate comparison
		TestEntity partialObject = new() { Id = 1, Name = "Test", Value = 0 };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, ignoreDefaultValues: false);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetObjectByPartial_WithMultipleMatches_ReturnsFirst(bool ignoreDefaultValues)
	{
		// Arrange
		TestEntity entity1 = new() { Id = 1, Name = "Test", Value = 100 };
		TestEntity entity2 = new() { Id = 2, Name = "Test", Value = 200 };
		_context.TestEntities.AddRange(entity1, entity2);
		_context.SaveChanges();

		TestEntity partialObject;
		if (ignoreDefaultValues)
		{
			// When ignoring defaults, we can use just the Name property
			partialObject = new() { Name = "Test" };
		}
		else
		{
			// When not ignoring defaults, we need to match all properties to avoid including Id=0, Value=0, CreatedDate=default
			// Let's match the first entity
			partialObject = new() { Id = 1, Name = "Test", Value = 100 };
		}

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, ignoreDefaultValues);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Test");
		if (!ignoreDefaultValues)
		{
			result.Id.ShouldBe(1); // Should match entity1 since we specified Id=1
		}
	}

	[Fact]
	public void GetObjectByPartial_WithCancellationToken_RespondsToCancel()
	{
		// Arrange
		TestEntity entity = new() { Id = 1, Name = "Test", Value = 100 };
		_context.TestEntities.Add(entity);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test" };
		using CancellationTokenSource cts = new();
		cts.Cancel();

		// Act & Assert
		Should.Throw<OperationCanceledException>(() =>
				_context.TestEntities.GetObjectByPartial(_context, partialObject, cancellationToken: cts.Token));
	}

	[Fact]
	public void GetObjectByPartial_WithComplexMatch_CombinesMultipleConditions()
	{
		// Arrange
		TestEntity entity1 = new() { Id = 1, Name = "Test1", Value = 100, Description = "Desc1" };
		TestEntity entity2 = new() { Id = 2, Name = "Test2", Value = 100, Description = "Desc2" };
		TestEntity entity3 = new() { Id = 3, Name = "Test1", Value = 200, Description = "Desc1" };
		_context.TestEntities.AddRange(entity1, entity2, entity3);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test1", Value = 100, Description = "Desc1" };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity1.Id);
		result.Name.ShouldBe("Test1");
		result.Value.ShouldBe(100);
		result.Description.ShouldBe("Desc1");
	}

	[Fact]
	public void GetObjectByPartial_WithStringEmpty_MatchesCorrectly()
	{
		// Arrange
		TestEntity entity1 = new() { Id = 1, Name = "", Value = 100 };
		TestEntity entity2 = new() { Id = 2, Name = "Test", Value = 100 };
		_context.TestEntities.AddRange(entity1, entity2);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "" };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity1.Id);
		result.Name.ShouldBe("");
	}

	[Fact]
	public void GetObjectByPartial_WithNullableProperties_HandlesCorrectly()
	{
		// Arrange
		TestEntity entity1 = new() { Id = 1, Name = "Test", Value = 100, Description = "HasDescription" };
		TestEntity entity2 = new() { Id = 2, Name = "Test", Value = 100, Description = null };
		_context.TestEntities.AddRange(entity1, entity2);
		_context.SaveChanges();

		TestEntity partialObject = new() { Name = "Test", Value = 100 };

		// Act
		TestEntity? result = _context.TestEntities.GetObjectByPartial(_context, partialObject, true);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("Test");
		result.Value.ShouldBe(100);
	}

	// Test DbContext and entities
	private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
	{
		public DbSet<TestEntity> TestEntities => Set<TestEntity>();
		public DbSet<TestEntityWithDateTimeOffset> TestEntitiesWithDateTimeOffset => Set<TestEntityWithDateTimeOffset>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TestEntity>();
			modelBuilder.Entity<TestEntityWithDateTimeOffset>();
		}
	}

	private sealed class TestEntity
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int Value { get; set; }
		public string? Description { get; set; }
		public DateTime CreatedDate { get; set; }
	}

	private sealed class TestEntityWithDateTimeOffset
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public DateTimeOffset CreatedDate { get; set; }
	}
}
