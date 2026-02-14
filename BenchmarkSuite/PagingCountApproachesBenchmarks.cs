using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VSDiagnostics;

namespace BenchmarkSuite;
/// <summary>
/// Compares two approaches for paging with record count:
/// 1. Single query with projection (BuildPagingResult method)
/// 2. Two separate queries (separate CountAsync and ToListAsync)
/// </summary>
[MediumRunJob(RuntimeMoniker.Net10_0)]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class PagingCountApproachesBenchmarks
{
	private ServiceProvider serviceProvider = null!;
	private IServiceScope scope = null!;
	private IQueryable<BenchmarkEntity> unfilteredQuery = null!;
	private IQueryable<BenchmarkEntity> filteredQuery = null!;
	private const int RecordCount = 10000;
	private const int PageSize = 50;
	private const int Skip = 100;

	[GlobalSetup]
	public async Task GlobalSetup()
	{
		ServiceCollection services = new();
		services.AddDbContext<BenchmarkDbContext>(options => options.UseInMemoryDatabase("PagingBenchmarkDb_" + Guid.NewGuid()));
		serviceProvider = services.BuildServiceProvider();
		using IServiceScope serviceScope = serviceProvider.CreateScope();
		BenchmarkDbContext context = serviceScope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		List<BenchmarkEntity> entities = Enumerable.Range(1, RecordCount).Select(i => new BenchmarkEntity { Id = i, Name = $"Entity_{i}", Value = i * 10, CreatedDate = DateTime.UtcNow.AddDays(-i), IsActive = i % 2 == 0 }).ToList();
		context.BenchmarkEntities.AddRange(entities);
		await context.SaveChangesAsync();
	}

	[IterationSetup]
	public void IterationSetup()
	{
		scope = serviceProvider.CreateScope();
		BenchmarkDbContext context = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		unfilteredQuery = context.Set<BenchmarkEntity>().AsNoTracking();
		filteredQuery = context.Set<BenchmarkEntity>().AsNoTracking().Where(x => x.IsActive);
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

	[Benchmark(Baseline = true, Description = "Single query approach - Unfiltered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> SingleQuery_Unfiltered()
	{
		var results = await unfilteredQuery.Select(x => new { Entities = x, TotalCount = unfilteredQuery.Count() }).Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = results.FirstOrDefault()?.TotalCount ?? 0,
			Entities = results.ConvertAll(x => x.Entities)
		};
	}

	[Benchmark(Description = "Two queries approach - Unfiltered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> TwoQueries_Unfiltered()
	{
		int totalRecords = await unfilteredQuery.CountAsync(CancellationToken.None);
		List<BenchmarkEntity> entities = await unfilteredQuery.Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = totalRecords,
			Entities = entities
		};
	}

	[Benchmark(Description = "Single query approach - Filtered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> SingleQuery_Filtered()
	{
		var results = await filteredQuery.Select(x => new { Entities = x, TotalCount = filteredQuery.Count() }).Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = results.FirstOrDefault()?.TotalCount ?? 0,
			Entities = results.ConvertAll(x => x.Entities)
		};
	}

	[Benchmark(Description = "Two queries approach - Filtered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> TwoQueries_Filtered()
	{
		int totalRecords = await filteredQuery.CountAsync(CancellationToken.None);
		List<BenchmarkEntity> entities = await filteredQuery.Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = totalRecords,
			Entities = entities
		};
	}
}
