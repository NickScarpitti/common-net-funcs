using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using CommonNetFuncs.EFCore;
using Microsoft.VSDiagnostics;

namespace CommonNetFuncs.EFCore.Benchmarks;
[CPUUsageDiagnoser]
public class NavigationPropertiesBenchmarks
{
    private TestDbContext _context;
    private TestEntity _entity;
    private IQueryable<TestEntity> _query;
    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _context = new TestDbContext(options);
        _entity = new TestEntity
        {
            Id = 1,
            RelatedEntity = new TestRelatedEntity
            {
                Id = 2
            }
        };
        _context.TestEntities.Add(_entity);
        _context.SaveChanges();
        _query = _context.TestEntities;
    }

    [Benchmark]
    public void Benchmark_IncludeNavigationProperties()
    {
        var result = _query.IncludeNavigationProperties(_context).ToList();
    }

    [Benchmark]
    public void Benchmark_GetNavigations()
    {
        var navs = NavigationProperties.GetNavigations<TestEntity>(_context);
    }

    [Benchmark]
    public void Benchmark_GetTopLevelNavigations()
    {
        var navs = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context);
    }

    [Benchmark]
    public void Benchmark_RemoveNavigationProperties()
    {
        var entity = new TestEntity
        {
            Id = 1,
            RelatedEntity = new TestRelatedEntity
            {
                Id = 2
            }
        };
        entity.RemoveNavigationProperties(_context);
    }

    // Minimal test entities for benchmarking
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<TestRelatedEntity> TestRelatedEntities => Set<TestRelatedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasOne(x => x.RelatedEntity).WithMany();
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public TestRelatedEntity? RelatedEntity { get; set; }
    }

    public class TestRelatedEntity
    {
        public int Id { get; set; }
    }
}