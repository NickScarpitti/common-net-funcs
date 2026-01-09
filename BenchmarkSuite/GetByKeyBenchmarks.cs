using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VSDiagnostics;

namespace BenchmarkSuite;

[MediumRunJob(RuntimeMoniker.Net10_0)]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class GetByKeyBenchmarks
{
	private ServiceProvider _serviceProvider = null!;
	private IServiceScope _scope = null!;
	private BaseDbContextActions<BenchmarkEntity, BenchmarkDbContext> _dbActions = null!;
	private int _existingEntityId;
	private const int RecordCount = 1000;
	[GlobalSetup]
	public async Task GlobalSetup()
	{
		ServiceCollection services = new();
		// Configure in-memory database
		services.AddDbContext<BenchmarkDbContext>(options => options.UseInMemoryDatabase("GetByKeyBenchmarkDb"));
		_serviceProvider = services.BuildServiceProvider();
		// Seed database with test data once
		using IServiceScope scope = _serviceProvider.CreateScope();
		BenchmarkDbContext context = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		List<BenchmarkEntity> entities = Enumerable.Range(1, RecordCount).Select(i => new BenchmarkEntity { Id = i, Name = $"Entity_{i}", Value = i * 10, CreatedDate = DateTime.UtcNow.AddDays(-i), IsActive = i % 2 == 0 }).ToList();
		context.BenchmarkEntities.AddRange(entities);
		await context.SaveChangesAsync();
		_existingEntityId = entities[RecordCount / 2].Id; // Middle entity
	}

	[IterationSetup]
	public void IterationSetup()
	{
		// Create a new scope for each iteration to get a fresh DbContext
		_scope = _serviceProvider.CreateScope();
		_dbActions = new BaseDbContextActions<BenchmarkEntity, BenchmarkDbContext>(_scope.ServiceProvider);
	}

	[IterationCleanup]
	public void IterationCleanup()
	{
		_scope?.Dispose();
	}

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		_serviceProvider?.Dispose();
	}

	[Benchmark(Baseline = true)]
	public async Task<BenchmarkEntity?> GetByKey_NoFilters()
	{
		return await _dbActions.GetByKey(_existingEntityId);
	}

	[Benchmark]
	public async Task<BenchmarkEntity?> GetByKey_WithGlobalFiltersDisabled()
	{
		GlobalFilterOptions filterOptions = new()
		{
			DisableAllFilters = true
		};
		return await _dbActions.GetByKey(_existingEntityId, globalFilterOptions: filterOptions);
	}

	[Benchmark]
	public async Task<BenchmarkEntity?> GetByKey_WithSpecificFilterDisabled()
	{
		GlobalFilterOptions filterOptions = new()
		{
			FilterNamesToDisable = new[]
						{
								"TestFilter"
						}
		};
		return await _dbActions.GetByKey(_existingEntityId, globalFilterOptions: filterOptions);
	}

	[Benchmark]
	public async Task<BenchmarkEntity?> GetByKey_WithTimeout()
	{
		return await _dbActions.GetByKey(_existingEntityId, queryTimeout: TimeSpan.FromSeconds(30));
	}
}

// Benchmark-specific DbContext and Entity
public class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options)
{
	public DbSet<BenchmarkEntity> BenchmarkEntities => Set<BenchmarkEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<BenchmarkEntity>().HasKey(e => e.Id);
	}
}

public class BenchmarkEntity
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public int Value { get; set; }
	public DateTime CreatedDate { get; set; }
	public bool IsActive { get; set; }
}
