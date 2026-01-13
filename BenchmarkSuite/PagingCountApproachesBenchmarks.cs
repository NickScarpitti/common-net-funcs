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
	private ServiceProvider _serviceProvider = null!;
	private IServiceScope _scope = null!;
	private BenchmarkDbContext _context = null!;
	private IQueryable<BenchmarkEntity> _unfilteredQuery = null!;
	private IQueryable<BenchmarkEntity> _filteredQuery = null!;
	private const int RecordCount = 10000;
	private const int PageSize = 50;
	private const int Skip = 100;
	[GlobalSetup]
	public async Task GlobalSetup()
	{
		ServiceCollection services = new();
		services.AddDbContext<BenchmarkDbContext>(options => options.UseInMemoryDatabase("PagingBenchmarkDb_" + Guid.NewGuid()));
		_serviceProvider = services.BuildServiceProvider();
		using IServiceScope scope = _serviceProvider.CreateScope();
		BenchmarkDbContext context = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		List<BenchmarkEntity> entities = Enumerable.Range(1, RecordCount).Select(i => new BenchmarkEntity { Id = i, Name = $"Entity_{i}", Value = i * 10, CreatedDate = DateTime.UtcNow.AddDays(-i), IsActive = i % 2 == 0 }).ToList();
		context.BenchmarkEntities.AddRange(entities);
		await context.SaveChangesAsync();
	}

	[IterationSetup]
	public void IterationSetup()
	{
		_scope = _serviceProvider.CreateScope();
		_context = _scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
		_unfilteredQuery = _context.Set<BenchmarkEntity>().AsNoTracking();
		_filteredQuery = _context.Set<BenchmarkEntity>().AsNoTracking().Where(x => x.IsActive);
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

	[Benchmark(Baseline = true, Description = "Single query approach - Unfiltered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> SingleQuery_Unfiltered()
	{
		var results = await _unfilteredQuery.Select(x => new { Entities = x, TotalCount = _unfilteredQuery.Count() }).Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = results.FirstOrDefault()?.TotalCount ?? 0,
			Entities = results.Select(x => x.Entities).ToList()
		};
	}

	[Benchmark(Description = "Two queries approach - Unfiltered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> TwoQueries_Unfiltered()
	{
		int totalRecords = await _unfilteredQuery.CountAsync(CancellationToken.None);
		List<BenchmarkEntity> entities = await _unfilteredQuery.Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = totalRecords,
			Entities = entities
		};
	}

	[Benchmark(Description = "Single query approach - Filtered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> SingleQuery_Filtered()
	{
		var results = await _filteredQuery.Select(x => new { Entities = x, TotalCount = _filteredQuery.Count() }).Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = results.FirstOrDefault()?.TotalCount ?? 0,
			Entities = results.Select(x => x.Entities).ToList()
		};
	}

	[Benchmark(Description = "Two queries approach - Filtered")]
	public async Task<GenericPagingModel<BenchmarkEntity>> TwoQueries_Filtered()
	{
		int totalRecords = await _filteredQuery.CountAsync(CancellationToken.None);
		List<BenchmarkEntity> entities = await _filteredQuery.Skip(Skip).Take(PageSize).ToListAsync(CancellationToken.None);
		return new GenericPagingModel<BenchmarkEntity>
		{
			TotalRecords = totalRecords,
			Entities = entities
		};
	}
}
