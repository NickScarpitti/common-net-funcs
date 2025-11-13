using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CommonNetFuncs.EFCore;
using Microsoft.VSDiagnostics;

namespace CommonNetFuncs.EFCore.Benchmarks;
[CPUUsageDiagnoser]
public class BaseDbContextActionsBenchmarks
{
    private IServiceProvider _serviceProvider;
    private TestDbContext _context;
    private BaseDbContextActions<TestEntity, TestDbContext> _actions;
    private TestEntity _entity;
    private List<TestEntity> _entities;
    private int _existingId;
    private int _maxId;
    private int _minId;
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<TestDbContext>();
        _actions = new BaseDbContextActions<TestEntity, TestDbContext>(_serviceProvider);
        // Seed data
        _entities = Enumerable.Range(1, 100).Select(i => new TestEntity { Id = i, Name = $"Entity {i}", CreatedDate = DateTime.UtcNow }).ToList();
        _context.TestEntities.AddRange(_entities);
        _context.SaveChanges();
        _entity = _entities[0];
        _existingId = _entities[41].Id; // ID 42 exists
        _maxId = _entities.Max(e => e.Id);
        _minId = _entities.Min(e => e.Id);
    }

    [Benchmark]
    public async Task Benchmark_GetByKey()
    {
        var result = await _actions.GetByKey(_existingId);
    }

    [Benchmark]
    public async Task Benchmark_GetAll()
    {
        var result = await _actions.GetAll();
    }

    [Benchmark]
    public async Task Benchmark_GetWithFilter()
    {
        var result = await _actions.GetWithFilter(x => x.Id > 50);
    }

    [Benchmark]
    public async Task Benchmark_GetOneWithFilter()
    {
        var result = await _actions.GetOneWithFilter(x => x.Id == _existingId);
    }

    [Benchmark]
    public async Task Benchmark_GetMaxByOrder()
    {
        var result = await _actions.GetMaxByOrder(x => true, x => x.Id);
    }

    [Benchmark]
    public async Task Benchmark_GetMinByOrder()
    {
        var result = await _actions.GetMinByOrder(x => true, x => x.Id);
    }

    // Minimal test entities for benchmarking
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
