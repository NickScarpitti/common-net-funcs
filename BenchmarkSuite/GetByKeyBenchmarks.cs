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
	private ServiceProvider serviceProvider = null!;
	private IServiceScope scope = null!;
	private BaseDbContextActions<BenchmarkEntity, BenchmarkDbContext> dbActions = null!;
	private int existingEntityId;
	private const int RecordCount = 1000;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		ServiceCollection services = new();
		// Configure in-memory database
		services.AddDbContext<BenchmarkDbContext>(options => options.UseInMemoryDatabase("GetByKeyBenchmarkDb"));
		serviceProvider = services.BuildServiceProvider();
		// Seed database with test data once
		using IServiceScope localScope = serviceProvider.CreateScope();
		BenchmarkDbContext context = localScope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		List<BenchmarkEntity> entities = Enumerable.Range(1, RecordCount).Select(i => new BenchmarkEntity { Id = i, Name = $"Entity_{i}", Value = i * 10, CreatedDate = DateTime.UtcNow.AddDays(-i), IsActive = i % 2 == 0 }).ToList();
		context.BenchmarkEntities.AddRange(entities);
		await context.SaveChangesAsync();
		existingEntityId = entities[RecordCount / 2].Id; // Middle entity
	}

	[IterationSetup]
	public void IterationSetup()
	{
		// Create a new scope for each iteration to get a fresh DbContext
		scope = serviceProvider.CreateScope();
		dbActions = new BaseDbContextActions<BenchmarkEntity, BenchmarkDbContext>(scope.ServiceProvider);
	}

	[IterationCleanup]
	public void IterationCleanup()
	{
		scope?.Dispose();
	}

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		serviceProvider?.Dispose();
	}

	[Benchmark(Baseline = true)]
	public async Task<BenchmarkEntity?> GetByKey_NoFilters()
	{
		return await dbActions.GetByKey(existingEntityId);
	}

	[Benchmark]
	public async Task<BenchmarkEntity?> GetByKey_WithGlobalFiltersDisabled()
	{
		GlobalFilterOptions filterOptions = new()
		{
			DisableAllFilters = true
		};
		return await dbActions.GetByKey(existingEntityId, globalFilterOptions: filterOptions);
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
		return await dbActions.GetByKey(existingEntityId, globalFilterOptions: filterOptions);
	}

	[Benchmark]
	public async Task<BenchmarkEntity?> GetByKey_WithTimeout()
	{
		return await dbActions.GetByKey(existingEntityId, queryTimeout: TimeSpan.FromSeconds(30));
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
