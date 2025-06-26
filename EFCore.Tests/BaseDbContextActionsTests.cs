using System.Linq.Expressions;
using System.Text.Json.Serialization;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Tests;

public sealed class BaseDbContextActionsTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Fixture _fixture;
    private readonly TestDbContext _context;

    public BaseDbContextActionsTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => _fixture.Behaviors.Remove(x));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Setup in-memory database
        ServiceCollection services = new();
        services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<TestDbContext>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetByKey_WithValidKey_ShouldReturnExpectedEntity(bool full)
    {
        // Arrange
        TestEntity testEntity = _fixture.Create<TestEntity>();
        await _context.TestEntities.AddAsync(testEntity);
        await _context.SaveChangesAsync();

        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        TestEntity? result = await testContext.GetByKey(full, testEntity.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(testEntity.Id);
        result.Name.ShouldBe(testEntity.Name);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task GetAll_WithEntities_ShouldReturnAllEntities(bool full, bool trackEntities)
    {
        // Arrange
        List<TestEntity> entities = _fixture.CreateMany<TestEntity>(3).ToList();
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        List<TestEntity>? results = await testContext.GetAll(full, trackEntities: trackEntities);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(entities.Count);
        results.Select(x => x.Id).ShouldBe(entities.Select(x => x.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetWithFilter_WithValidPredicate_ShouldReturnFilteredEntities(bool full)
    {
        // Arrange
        List<TestEntity> entities = _fixture.CreateMany<TestEntity>(5).ToList();
        string targetName = entities[0].Name;
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        List<TestEntity>? results = await testContext.GetWithFilter(full, filter);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe(targetName);
    }

    [Fact]
    public async Task GetOneWithFilter_WithValidPredicate_ShouldReturnSingleEntity()
    {
        // Arrange
        List<TestEntity> entities = _fixture.CreateMany<TestEntity>(3).ToList();
        string targetName = entities[0].Name;
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        TestEntity? result = await testContext.GetOneWithFilter(filter);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(targetName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetMaxByOrder_ShouldReturnEntityWithMaxValue(bool full)
    {
        // Arrange
        List<TestEntity> entities = _fixture.CreateMany<TestEntity>(3).ToList();
        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        Expression<Func<TestEntity, bool>> filter = _ => true;
        Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        TestEntity? result = await testContext.GetMaxByOrder(full, filter, orderExpression);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entities.Max(x => x.Id));
    }

    [Fact]
    public async Task Create_WithValidEntity_ShouldAddToDatabase()
    {
        // Arrange
        TestEntity entity = _fixture.Create<TestEntity>();
        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        await testContext.Create(entity);
        await testContext.SaveChanges();

        // Assert
        TestEntity? savedEntity = await _context.TestEntities.FindAsync(entity.Id);
        savedEntity.ShouldNotBeNull();
        savedEntity.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async Task Update_WithValidEntity_ShouldUpdateDatabase()
    {
        // Arrange
        TestEntity entity = _fixture.Create<TestEntity>();
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        string updatedName = _fixture.Create<string>();
        entity.Name = updatedName;

        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        testContext.Update(entity);
        await testContext.SaveChanges();

        // Assert
        TestEntity? savedEntity = await _context.TestEntities.FindAsync(entity.Id);
        savedEntity.ShouldNotBeNull();
        savedEntity.Name.ShouldBe(updatedName);
    }

    [Fact]
    public async Task DeleteByKey_WithValidKey_ShouldRemoveFromDatabase()
    {
        // Arrange
        TestEntity entity = _fixture.Create<TestEntity>();
        await _context.TestEntities.AddAsync(entity);
        await _context.SaveChangesAsync();

        BaseDbContextActions<TestEntity, TestDbContext> testContext = new(_serviceProvider);

        // Act
        bool result = await testContext.DeleteByKey(entity.Id);
        await testContext.SaveChanges();

        // Assert
        result.ShouldBeTrue();
        TestEntity? deletedEntity = await _context.TestEntities.FindAsync(entity.Id);
        deletedEntity.ShouldBeNull();
    }
}

// Test types
public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public sealed class TestEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedDate { get; set; }

    public ICollection<TestEntityDetail>? Details { get; set; }
}

public sealed class TestEntityDetail
{
    public int Id { get; set; }

    public required string Description { get; set; }

    public int TestEntityId { get; set; }

    [JsonIgnore]
    public TestEntity? TestEntity { get; set; }
}
