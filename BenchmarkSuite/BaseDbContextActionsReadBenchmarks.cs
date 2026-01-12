using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class BaseDbContextActionsReadBenchmarks : IDisposable
{
	private IServiceProvider? _serviceProvider;
	private BaseDbContextActions<TestEntity, TestDbContext>? _actions;
	private List<TestEntity> _entities = null!;
	private bool _disposed;

	[GlobalSetup]
	public void Setup()
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(
			options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()),
			ServiceLifetime.Transient);
		_serviceProvider = services.BuildServiceProvider();

		using (var scope = _serviceProvider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			context.Database.EnsureCreated();

			// Seed data - 100 records for realistic performance testing
			_entities = Enumerable.Range(1, 100)
				.Select(i => new TestEntity
				{
					Id = i,
					Name = $"Entity_{i}",
					CreatedDate = DateTime.UtcNow.AddDays(-i)
				})
				.ToList();

			context.TestEntities.AddRange(_entities);
			context.SaveChanges();
		}

		_actions = new BaseDbContextActions<TestEntity, TestDbContext>(_serviceProvider);
	}

	[Benchmark(Description = "GetByKey - no filters")]
	public async Task<TestEntity?> GetByKey_NoFilters()
	{
		return await _actions!.GetByKey(50);
	}

	[Benchmark(Description = "GetByKey - with disabled filters")]
	public async Task<TestEntity?> GetByKey_WithDisabledFilters()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		return await _actions!.GetByKey(50, globalFilterOptions: filterOptions);
	}

	[Benchmark(Description = "GetByKey - compound key, no filters")]
	public async Task<TestEntity?> GetByKey_CompoundKey_NoFilters()
	{
		return await _actions!.GetByKey(new object[] { 50 });
	}

	[Benchmark(Description = "GetByKey - compound key, with disabled filters")]
	public async Task<TestEntity?> GetByKey_CompoundKey_WithDisabledFilters()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		return await _actions!.GetByKey(new object[] { 50 }, globalFilterOptions: filterOptions);
	}

	[Benchmark(Description = "GetAll - basic")]
	public async Task<List<TestEntity>?> GetAll_Basic()
	{
		return await _actions!.GetAll();
	}

	[Benchmark(Description = "GetAll - with projection")]
	public async Task<List<string>?> GetAll_WithProjection()
	{
		return await _actions!.GetAll(x => x.Name);
	}

	[Benchmark(Description = "GetWithFilter - simple where clause")]
	public async Task<List<TestEntity>?> GetWithFilter_Simple()
	{
		return await _actions!.GetWithFilter(x => x.Id > 50);
	}

	[Benchmark(Description = "GetWithFilter - complex where clause")]
	public async Task<List<TestEntity>?> GetWithFilter_Complex()
	{
		return await _actions!.GetWithFilter(x => x.Id > 25 && x.Id < 75 && x.Name.StartsWith("Entity"));
	}

	[Benchmark(Description = "GetWithFilter - with projection")]
	public async Task<List<int>?> GetWithFilter_WithProjection()
	{
		return await _actions!.GetWithFilter(x => x.Id > 50, x => x.Id);
	}

	[Benchmark(Description = "GetOneWithFilter")]
	public async Task<TestEntity?> GetOneWithFilter()
	{
		return await _actions!.GetOneWithFilter(x => x.Id == 50);
	}

	[Benchmark(Description = "GetCount")]
	public async Task<int> GetCount()
	{
		return await _actions!.GetCount(x => x.Id > 50);
	}

	[Benchmark(Description = "GetMaxByOrder")]
	public async Task<TestEntity?> GetMaxByOrder()
	{
		return await _actions!.GetMaxByOrder(x => x.Id > 0, x => x.Id);
	}

	[Benchmark(Description = "GetMinByOrder")]
	public async Task<TestEntity?> GetMinByOrder()
	{
		return await _actions!.GetMinByOrder(x => x.Id > 0, x => x.Id);
	}

	[Benchmark(Description = "GetMax")]
	public async Task<int> GetMax()
	{
		return await _actions!.GetMax(x => x.Id > 0, x => x.Id);
	}

	[Benchmark(Description = "GetMin")]
	public async Task<int> GetMin()
	{
		return await _actions!.GetMin(x => x.Id > 0, x => x.Id);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			(_serviceProvider as IDisposable)?.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Scale benchmarks testing with 10,000+ queries to identify performance characteristics at scale
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class BaseDbContextActionsScaleBenchmarks : IDisposable
{
	private IServiceProvider? _serviceProvider;
	private BaseDbContextActions<TestEntity, TestDbContext>? _actions;
	private bool _disposed;
	private const int ScaleIterations = 10000;

	[GlobalSetup]
	public void Setup()
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(
			options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()),
			ServiceLifetime.Transient);
		_serviceProvider = services.BuildServiceProvider();

		using (var scope = _serviceProvider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			context.Database.EnsureCreated();

			// Seed 1000 records for scale testing
			var entities = Enumerable.Range(1, 1000)
				.Select(i => new TestEntity
				{
					Id = i,
					Name = $"Entity_{i}",
					CreatedDate = DateTime.UtcNow.AddDays(-i)
				})
				.ToList();

			context.TestEntities.AddRange(entities);
			context.SaveChanges();
		}

		_actions = new BaseDbContextActions<TestEntity, TestDbContext>(_serviceProvider);
	}

	[Benchmark(Description = "Scale: 10k GetByKey - FindAsync path")]
	public async Task Scale_GetByKey_FindAsync_10k()
	{
		for (int i = 0; i < ScaleIterations; i++)
		{
			int id = (i % 1000) + 1;
			await _actions!.GetByKey(id);
		}
	}

	[Benchmark(Description = "Scale: 10k GetByKey - Expression path")]
	public async Task Scale_GetByKey_Expression_10k()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		for (int i = 0; i < ScaleIterations; i++)
		{
			int id = (i % 1000) + 1;
			await _actions!.GetByKey(id, globalFilterOptions: filterOptions);
		}
	}

	[Benchmark(Description = "Scale: 10k GetByKey compound - FindAsync path")]
	public async Task Scale_GetByKey_Compound_FindAsync_10k()
	{
		for (int i = 0; i < ScaleIterations; i++)
		{
			int id = (i % 1000) + 1;
			await _actions!.GetByKey(new object[] { id });
		}
	}

	[Benchmark(Description = "Scale: 10k GetByKey compound - Expression path")]
	public async Task Scale_GetByKey_Compound_Expression_10k()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		for (int i = 0; i < ScaleIterations; i++)
		{
			int id = (i % 1000) + 1;
			await _actions!.GetByKey(new object[] { id }, globalFilterOptions: filterOptions);
		}
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			(_serviceProvider as IDisposable)?.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Memory-focused benchmarks to identify allocation hotspots
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class BaseDbContextActionsMemoryBenchmarks : IDisposable
{
	private IServiceProvider? _serviceProvider;
	private BaseDbContextActions<TestEntity, TestDbContext>? _actions;
	private bool _disposed;

	[GlobalSetup]
	public void Setup()
	{
		var services = new ServiceCollection();
		services.AddDbContext<TestDbContext>(
			options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()),
			ServiceLifetime.Transient);
		_serviceProvider = services.BuildServiceProvider();

		using (var scope = _serviceProvider.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
			context.Database.EnsureCreated();

			var entities = Enumerable.Range(1, 100)
				.Select(i => new TestEntity
				{
					Id = i,
					Name = $"Entity_{i}",
					CreatedDate = DateTime.UtcNow.AddDays(-i)
				})
				.ToList();

			context.TestEntities.AddRange(entities);
			context.SaveChanges();
		}

		_actions = new BaseDbContextActions<TestEntity, TestDbContext>(_serviceProvider);
	}

	[Benchmark(Description = "Memory: GetByKey FindAsync")]
	public async Task<TestEntity?> Memory_GetByKey_FindAsync()
	{
		return await _actions!.GetByKey(50);
	}

	[Benchmark(Description = "Memory: GetByKey Expression")]
	public async Task<TestEntity?> Memory_GetByKey_Expression()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		return await _actions!.GetByKey(50, globalFilterOptions: filterOptions);
	}

	[Benchmark(Description = "Memory: GetByKey compound FindAsync")]
	public async Task<TestEntity?> Memory_GetByKey_Compound_FindAsync()
	{
		return await _actions!.GetByKey(new object[] { 50 });
	}

	[Benchmark(Description = "Memory: GetByKey compound Expression")]
	public async Task<TestEntity?> Memory_GetByKey_Compound_Expression()
	{
		var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		return await _actions!.GetByKey(new object[] { 50 }, globalFilterOptions: filterOptions);
	}

	[Benchmark(Description = "Memory: Expression building overhead")]
	public void Memory_ExpressionBuilding_Overhead()
	{
		// Test pure expression building cost without DB access
		for (int i = 0; i < 100; i++)
		{
			var parameter = Expression.Parameter(typeof(TestEntity), "x");
			var property = Expression.Property(parameter, "Id");
			var constant = Expression.Constant(50, typeof(int));
			var equality = Expression.Equal(property, constant);
			var lambda = Expression.Lambda<Func<TestEntity, bool>>(equality, parameter);
			var compiled = lambda.Compile();
		}
	}

	[Benchmark(Description = "Memory: GlobalFilterOptions allocation")]
	public void Memory_FilterOptions_Allocation()
	{
		// Test allocation cost of creating filter options
		for (int i = 0; i < 1000; i++)
		{
			var filterOptions = new GlobalFilterOptions { DisableAllFilters = true };
		}
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			(_serviceProvider as IDisposable)?.Dispose();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}

// Test entity matching the one from EFCore.Tests
public class TestEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public DateTime CreatedDate { get; set; }
	public ICollection<TestEntityDetail>? Details { get; set; }
}

public class TestEntityDetail
{
	public int Id { get; set; }
	public string Description { get; set; } = string.Empty;
	public int TestEntityId { get; set; }
	public TestEntity? TestEntity { get; set; }
}

public class TestDbContext : DbContext
{
	public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

	public DbSet<TestEntity> TestEntities => Set<TestEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TestEntity>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Name).HasMaxLength(200);
		});

		modelBuilder.Entity<TestEntityDetail>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Description).HasMaxLength(500);
			entity.HasOne(e => e.TestEntity)
				.WithMany(e => e.Details)
				.HasForeignKey(e => e.TestEntityId);
		});
	}
}
