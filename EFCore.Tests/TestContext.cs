using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;


// Test types
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<TestEntity> TestEntities => Set<TestEntity>();

	public DbSet<TestEntityWithCompoundKey> TestEntitiesWithCompoundKey => Set<TestEntityWithCompoundKey>();

	public DbSet<TestEntityWithDateTimeOffset> TestEntitiesWithDateTimeOffset => Set<TestEntityWithDateTimeOffset>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntity>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<TestEntityDetail>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<TestEntityDetail>()
			.HasOne(e => e.TestEntity)
			.WithMany(e => e.Details)
			.HasForeignKey(e => e.TestEntityId);

		modelBuilder.Entity<TestEntityWithCompoundKey>()
			.HasKey(e => new { e.Key1, e.Key2 });

		modelBuilder.Entity<TestEntityWithDateTimeOffset>()
			.HasKey(e => e.Id);
	}
}

public class TestEntity
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public int Value { get; set; }

	public string? Description { get; set; }

	public DateTime CreatedDate { get; set; }

	public ICollection<TestEntityDetail>? Details { get; set; }
}

public class TestEntityDetail
{
	public int Id { get; set; }

	public required string Description { get; set; }

	public int TestEntityId { get; set; }

	[JsonIgnore]
	public TestEntity? TestEntity { get; set; }
}

public class TestEntityWithCompoundKey
{
	public int Key1 { get; set; }

	public int Key2 { get; set; }

	public required string Name { get; set; }
}

public class TestEntityWithDateTimeOffset
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public DateTimeOffset CreatedDate { get; set; }
}

// Circular reference test types
public class CircularRefDbContext(DbContextOptions<CircularRefDbContext> options) : DbContext(options)
{
	public DbSet<ParentEntity> Parents => Set<ParentEntity>();

	public DbSet<ChildEntity> Children => Set<ChildEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<ParentEntity>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<ChildEntity>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<ChildEntity>()
			.HasOne(c => c.Parent)
			.WithMany(p => p.Children)
			.HasForeignKey(c => c.ParentId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

public class ParentEntity
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public ICollection<ChildEntity> Children { get; set; } = new List<ChildEntity>();
}

public class ChildEntity
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public int ParentId { get; set; }

	public ParentEntity? Parent { get; set; }
}

// Context and entity for GlobalFilterOptions tests to avoid cache interference
public class TestDbContextForFilters(DbContextOptions<TestDbContextForFilters> options) : DbContext(options)
{
	public DbSet<TestEntityForFilters> TestEntities => Set<TestEntityForFilters>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityForFilters>()
			.HasKey(e => e.Id);
	}
}

public class TestEntityForFilters
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public int Value { get; set; }

	public string? Description { get; set; }

	public DateTime CreatedDate { get; set; }
}

// Context and entities for navigation property tests to avoid cache interference
public class TestDbContextForNavigation(DbContextOptions<TestDbContextForNavigation> options) : DbContext(options)
{
	public DbSet<TestEntityForNavigation> TestEntities => Set<TestEntityForNavigation>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntityForNavigation>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<TestEntityDetailForNavigation>()
			.HasKey(e => e.Id);

		modelBuilder.Entity<TestEntityDetailForNavigation>()
			.HasOne(e => e.TestEntity)
			.WithMany(e => e.Details)
			.HasForeignKey(e => e.TestEntityId);
	}
}

public class TestEntityForNavigation
{
	public int Id { get; set; }

	public required string Name { get; set; }

	public int Value { get; set; }

	public string? Description { get; set; }

	public DateTime CreatedDate { get; set; }

	public ICollection<TestEntityDetailForNavigation>? Details { get; set; }
}

public class TestEntityDetailForNavigation
{
	public int Id { get; set; }

	public required string Description { get; set; }

	public int TestEntityId { get; set; }

	[JsonIgnore]
	public TestEntityForNavigation? TestEntity { get; set; }
}
