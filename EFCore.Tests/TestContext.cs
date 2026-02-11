using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;


// Test types
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<TestEntity> TestEntities => Set<TestEntity>();

	public DbSet<TestEntityWithCompoundKey> TestEntitiesWithCompoundKey => Set<TestEntityWithCompoundKey>();

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
	}
}

public class TestEntity
{
	public int Id { get; set; }

	public required string Name { get; set; }

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
