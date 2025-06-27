using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;

public sealed class NavigationPropertiesTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly TestDbContext _context;

    public NavigationPropertiesTests()
    {
        _fixture = new Fixture();
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: _fixture.Create<string>())
            .Options;
        _context = new TestDbContext(options);
    }

    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            disposed = true;
        }
    }

    ~NavigationPropertiesTests()
    {
        Dispose(false);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void GetNavigations_WithDifferentDepths_ReturnsCorrectNavigationPaths(int maxDepth)
    {
        // Arrange
        NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth);

        // Act
        HashSet<string> navigationPaths = NavigationProperties.GetNavigations<TestEntity>(_context, options);

        // Assert
        navigationPaths.ShouldNotBeEmpty();
        navigationPaths.All(path => path.Split('.').Length <= maxDepth + 1).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void GetNavigations_WithDifferentDepthsOneToMany_ReturnsCorrectNavigationPaths(int maxDepth)
    {
        // Arrange
        NavigationPropertiesOptions options = new(maxNavigationDepth: maxDepth);

        // Act
        HashSet<string> navigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, options);

        // Assert
        navigationPaths.ShouldNotBeEmpty();
        navigationPaths.All(path => path.Split('.').Length <= maxDepth + 1).ShouldBeTrue();
    }

    [Fact]
    public void GetNavigations_WithDifferentDepthsUsingCache_ReturnsCorrectNavigationPaths()
    {
        //TODO:: Get depth of 1, then 3, then 1 again , and ensure the cache is used correctly and extra navigations are left out when depth is less than max cached depth
        // Arrange
        HashSet<string> originalNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 1));
        HashSet<string> deeperNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 3));

        // Act
        HashSet<string> testNavigationPaths = NavigationProperties.GetNavigations<DeepTestEntity>(_context, new(maxNavigationDepth: 1));

        // Assert
        testNavigationPaths.ShouldNotBeEmpty();
        testNavigationPaths.ShouldBeEquivalentTo(originalNavigationPaths);
        testNavigationPaths.All(path => path.Split('.').Length <= 2).ShouldBeTrue(); // Depth of 1 means only direct navigations
        deeperNavigationPaths.Any(path => path.Split('.').Length > 2).ShouldBeTrue(); // Should have longer navigations than testNavigationPaths
        deeperNavigationPaths.ShouldNotBeSameAs(testNavigationPaths.Count); // Deeper paths should have more navigations
        deeperNavigationPaths.ShouldNotBeSameAs(originalNavigationPaths.Count); // Deeper paths should have more navigations
        deeperNavigationPaths.Count.ShouldBeGreaterThan(testNavigationPaths.Count); // Deeper paths should have more navigations
        deeperNavigationPaths.Count.ShouldBeGreaterThan(originalNavigationPaths.Count); // Deeper paths should have more navigations
    }

    [Fact]
    public void GetNavigations_WithCaching_ReturnsSameResult()
    {
        // Arrange
        NavigationPropertiesOptions options = new(useCaching: true);

        // Act
        HashSet<string> firstCall = NavigationProperties.GetNavigations<TestEntity>(_context, options);
        HashSet<string> secondCall = NavigationProperties.GetNavigations<TestEntity>(_context, options);

        // Assert
        firstCall.ShouldBe(secondCall);
    }

    [Fact]
    public void GetTopLevelNavigations_ReturnsOnlyDirectNavigations()
    {
        // Act
        List<string> topLevelNavigations = NavigationProperties.GetTopLevelNavigations<TestEntity>(_context);

        // Assert
        topLevelNavigations.ShouldNotBeEmpty();
        topLevelNavigations.All(nav => !nav.Contains('.')).ShouldBeTrue();
    }

    [Theory]
    [InlineData(typeof(JsonIgnoreAttribute))]
    [InlineData(typeof(XmlIgnoreAttribute))]
    public void GetTopLevelNavigations_WithIgnoredAttributes_ExcludesIgnoredProperties(Type attributeType)
    {
        // Arrange
        List<Type> ignoreList = [attributeType];

        // Act
        List<string> topLevelNavigations = NavigationProperties.GetTopLevelNavigations<TestEntityWithIgnores>(_context, ignoreList);

        // Assert
        topLevelNavigations.ShouldNotContain("IgnoredNavigation");
    }

    [Fact]
    public void IncludeNavigationProperties_AddsIncludeStatementsToQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = _context.TestEntities;

        // Act
        IQueryable<TestEntity> result = query.IncludeNavigationProperties(_context);

        // Assert
        result.Expression.ToString().ShouldContain("Include");
    }

    [Fact]
    public void RemoveNavigationProperties_SetsAllNavigationsToNull()
    {
        // Arrange
        TestEntity entity = new()
        {
            Id = 1,
            RelatedEntity = new TestRelatedEntity { Id = 2 }
        };

        // Act
        entity.RemoveNavigationProperties(_context);

        // Assert
        entity.RelatedEntity.ShouldBeNull();
    }

    // Test entities
    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        public DbSet<TestRelatedEntity> TestRelatedEntities => Set<TestRelatedEntity>();

        public DbSet<DeepTestEntity> DeepTestEntities => Set<DeepTestEntity>();

        public DbSet<DeepTestRelatedEntity> DeepTestRelatedEntities => Set<DeepTestRelatedEntity>();

        public DbSet<DeepTestRelatedEntity2> DeepTestRelatedEntity2s => Set<DeepTestRelatedEntity2>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>()
                .HasOne(e => e.RelatedEntity)
                .WithMany();

            modelBuilder.Entity<TestRelatedEntity>()
                .HasOne(e => e.Parent)
                .WithMany();

            modelBuilder.Entity<DeepTestEntity>()
                .HasOne(e => e.DeepTestRelatedEntity)
                .WithMany(e => e.TestEntities);

            modelBuilder.Entity<DeepTestRelatedEntity>()
                .HasOne(e => e.GetDeepTestRelatedEntity2)
                .WithMany(e => e.DeepTestRelatedEntities);

            modelBuilder.Entity<DeepTestRelatedEntity2>()
                .HasOne(e => e.DeepTestRelatedEntity3)
                .WithMany(e => e.DeepTestRelatedEntity2s);
        }
    }

    private sealed class TestEntity
    {
        public int Id { get; set; }

        public TestRelatedEntity? RelatedEntity { get; set; }
    }

    private sealed class TestRelatedEntity
    {
        public int Id { get; set; }

        public TestEntity? Parent { get; set; }
    }

    private sealed class TestEntityWithIgnores
    {
        public int Id { get; set; }

        [JsonIgnore]
        public TestRelatedEntity? IgnoredNavigation { get; set; }

        public TestRelatedEntity? NormalNavigation { get; set; }
    }

    private sealed class DeepTestEntity
    {
        public int Id { get; set; }

        public DeepTestRelatedEntity? DeepTestRelatedEntity { get; set; }
    }

    private class DeepTestRelatedEntity
    {
        public int Id { get; set; }

        public DeepTestRelatedEntity2? GetDeepTestRelatedEntity2 { get; set; }

        [JsonIgnore]
        public virtual ICollection<DeepTestEntity>? TestEntities { get; set; }
    }

    private class DeepTestRelatedEntity2
    {
        public int Id { get; set; }

        public DeepTestRelatedEntity3? DeepTestRelatedEntity3 { get; set; }

        [JsonIgnore]
        public virtual ICollection<DeepTestRelatedEntity>? DeepTestRelatedEntities { get; set; }
    }

    private class DeepTestRelatedEntity3
    {
        public int Id { get; set; }

        [JsonIgnore]
        public virtual ICollection<DeepTestRelatedEntity2>? DeepTestRelatedEntity2s { get; set; }
    }
}
