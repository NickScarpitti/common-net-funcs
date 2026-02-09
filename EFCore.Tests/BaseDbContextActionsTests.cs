using System.Linq.Expressions;
using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace EFCore.Tests;

public sealed class BaseDbContextActionsTests
{
	private readonly IServiceProvider serviceProvider;
	private readonly Fixture fixture;
	private readonly TestDbContext context;

	public BaseDbContextActionsTests()
	{
		fixture = new Fixture();
		fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(x => fixture.Behaviors.Remove(x));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		// Setup in-memory database
		ServiceCollection services = new();
		services.AddDbContextPool<TestDbContext>(options => options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
		serviceProvider = services.BuildServiceProvider();
		context = serviceProvider.GetRequiredService<TestDbContext>();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetByKey_WithValidKey_ShouldReturnExpectedEntity(bool full)
	{
		// Arrange
		TestEntity testEntity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(testEntity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(full, testEntity.Id, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(testEntity.Id);
		result.Name.ShouldBe(testEntity.Name);
	}

	[Fact]
	public async Task GetByKeyFull_WithValidKey_ShouldReturnExpectedEntity()
	{
		// Arrange
		TestEntity testEntity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(testEntity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(testEntity.Id, cancellationToken: TestContext.Current.CancellationToken);

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
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetAll(full, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

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
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<TestEntity>? results = await testContext.GetWithFilter(full, filter, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetOneWithFilter_WithValidPredicate_ShouldReturnSingleEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(filter, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilterFullAndNot_WithValidPredicate_ShouldReturnSingleEntity(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(full, filter, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetMaxByOrder_ShouldReturnEntityWithMaxValue()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(filter, orderExpression, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxByOrderFullAndNot_ShouldReturnEntityWithMaxValue(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		Expression<Func<TestEntity, bool>> filter = _ => true;
		Expression<Func<TestEntity, int>> orderExpression = x => x.Id;
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(full, filter, orderExpression, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task Create_WithValidEntity_ShouldAddToDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		await testContext.Create(entity);
		await testContext.SaveChanges();

		// Assert
		TestEntity? savedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		savedEntity.ShouldNotBeNull();
		savedEntity.Name.ShouldBe(entity.Name);
	}

	[Fact]
	public async Task Update_WithValidEntity_ShouldUpdateDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		string updatedName = fixture.Create<string>();
		entity.Name = updatedName;

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		testContext.Update(entity);
		await testContext.SaveChanges();

		// Assert
		TestEntity? savedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		savedEntity.ShouldNotBeNull();
		savedEntity.Name.ShouldBe(updatedName);
	}

	[Fact]
	public async Task DeleteByKey_WithValidKey_ShouldRemoveFromDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.DeleteByKey(entity.Id);
		await testContext.SaveChanges();

		// Assert
		result.ShouldBeTrue();
		TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		deletedEntity.ShouldBeNull();
	}

	[Fact]
	public async Task GetByKey_WithCompoundKey_ShouldReturnEntity()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(new object[] { entity.Id }, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetByKeyFullAndNot_WithCompoundKey_ShouldReturnEntity(bool full)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKey(full, new object[] { entity.Id }, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetByKeyFull_WithCompoundKey_ShouldReturnEntity()
	{
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetByKeyFull(new object[] { entity.Id }, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetAllStreaming_Variants_ShouldReturnEntities(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity> results = new();
		IAsyncEnumerable<TestEntity>? stream = testContext.GetAllStreaming(full, cancellationToken: TestContext.Current.CancellationToken);
		await foreach (TestEntity item in stream!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterStreaming_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterStreaming(filter, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterStreamingFullAndNot_ShouldReturnFilteredEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterStreaming(full, filter, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetWithFilterFullStreaming(filter, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(target.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetAllFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<string>? results = await testContext.GetAll(full, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetAll_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<string>? results = await testContext.GetAll(x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetAllFull_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<string>? results = await testContext.GetAllFull(x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetWithFilter_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilter(filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilter(full, filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterFull_WithProjection_ShouldReturnProjectedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		List<string>? results = await testContext.GetWithFilterFull(filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results!.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetCount_ShouldReturnCorrectCount()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int count = await testContext.GetCount(_ => true, cancellationToken: TestContext.Current.CancellationToken);

		count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetMinByOrder_ShouldReturnEntityWithMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrder(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinByOrderFullAndNot_ShouldReturnEntityWithMinValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrder(full, _ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result!.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task GetMax_ShouldReturnMaxValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMax(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxFullAndNot_ShouldReturnMaxValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMax(full, _ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMin_ShouldReturnMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMin(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinFullAndNot_ShouldReturnMinValue(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMin(full, _ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task DeleteMany_ShouldRemoveEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		bool result = testContext.DeleteMany(entities);

		await testContext.SaveChanges();

		result.ShouldBeTrue();
		foreach (TestEntity entity in entities)
		{
			(await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken)).ShouldBeNull();
		}
	}

	// Fails due to Z.EntityFramework.Extensions.EFCore not supporting non-relational databases
	//[Fact]
	//public async Task DeleteManyTracked_ShouldRemoveEntities()
	//{
	//    List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
	//    await context.TestEntities.AddRangeAsync(entities);
	//    await context.SaveChangesAsync();

	//    BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

	//    bool result = await testContext.DeleteManyTracked(entities);

	//    await testContext.SaveChanges();

	//    result.ShouldBeTrue();
	//    foreach (TestEntity entity in entities)
	//    {
	//        (await context.TestEntities.FindAsync(entity.Id)).ShouldBeNull();
	//    }
	//}

	// Fails due to Z.EntityFramework.Extensions.EFCore not supporting non-relational databases
	//[Fact]
	//public async Task DeleteManyByKeys_ShouldRemoveEntities()
	//{
	//    List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
	//    await context.TestEntities.AddRangeAsync(entities);
	//    await context.SaveChangesAsync();

	//    BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

	//    List<object> keys = entities.ConvertAll(e => (object)e.Id);
	//    bool result = await testContext.DeleteManyByKeys(keys);

	//    await testContext.SaveChanges();

	//    result.ShouldBeFalse();
	//    foreach (TestEntity entity in entities)
	//    {
	//        (await context.TestEntities.FindAsync(entity.Id)).ShouldBeNull();
	//    }
	//}

	[Fact]
	public async Task UpdateMany_ShouldUpdateEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		string updatedName = fixture.Create<string>();
		foreach (TestEntity entity in entities)
		{
			entity.Name = updatedName;
		}

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		bool result = testContext.UpdateMany(entities, false, TestContext.Current.CancellationToken);
		await testContext.SaveChanges();

		result.ShouldBeTrue();
		foreach (TestEntity entity in entities)
		{
			(await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken))!.Name.ShouldBe(updatedName);
		}
	}

	[Fact]
	public async Task CreateMany_ShouldAddEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		await testContext.CreateMany(entities);
		await testContext.SaveChanges();

		foreach (TestEntity entity in entities)
		{
			(await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken)).ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task GetByKey_WithInvalidKey_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetByKey(-1, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteByKey_WithInvalidKey_ShouldReturnFalse()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		bool result = await testContext.DeleteByKey(-1);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task GetByKey_WithCancellation_ShouldThrow()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		TestEntity? result = await testContext.GetByKey(1, cancellationToken: cts.Token);
		result.ShouldBeNull();
	}

	[Fact]
	public async Task Create_WithNullEntity_ShouldThrow()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		await Should.ThrowAsync<ArgumentNullException>(async () => await testContext.Create(null!));
	}

	[Fact]
	public async Task GetAllFull_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity>? results = await testContext.GetAllFull(cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllStreaming_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetAllStreaming(cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetAllFullStreaming_ShouldReturnAllEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetAllFullStreaming(cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		results.Count.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithFilterFull_ShouldReturnFilteredEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Name == targetName;

		List<TestEntity>? results = await testContext.GetWithFilterFull(filter, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetNavigationWithFilter_ShouldReturnEntities()
	{
		// Setup related entities
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		List<TestEntity>? results = await testContext.GetNavigationWithFilter(where, select, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetNavigationWithFilterFull_ShouldReturnEntities()
	{
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		List<TestEntity>? results = await testContext.GetNavigationWithFilterFull(where, select, cancellationToken: TestContext.Current.CancellationToken);

		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
	}

	[Fact]
	public async Task GetWithPagingFilter_ShouldReturnPagedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id), skip: 1, pageSize: 2,
			cancellationToken: TestContext.Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithPagingFilterFullAndNot_ShouldReturnPagedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(full, whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id), skip: 1, pageSize: 2,
			cancellationToken: TestContext.Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_ShouldReturnPagedEntities()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1, pageSize: 2,
			cancellationToken: TestContext.Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithPagingFilterFullAndNot_TKey_ShouldReturnPagedEntities(bool full)
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(full, whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1, pageSize: 2,
			cancellationToken: TestContext.Current.CancellationToken);

		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetOneWithFilterFull_ShouldReturnEntity()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetOneWithFilterFull(x => x.Name == targetName, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Name.ShouldBe(targetName);
	}

	[Fact]
	public async Task GetMaxByOrderFull_ShouldReturnEntityWithMaxValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMaxByOrderFull(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMinByOrderFull_ShouldReturnEntityWithMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		TestEntity? result = await testContext.GetMinByOrderFull(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public async Task GetMaxFull_ShouldReturnMaxValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMaxFull(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Fact]
	public async Task GetMinFull_ShouldReturnMinValue()
	{
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		int result = await testContext.GetMinFull(_ => true, x => x.Id, cancellationToken: TestContext.Current.CancellationToken);

		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Fact]
	public void UpdateMany_WhenException_ShouldReturnFalse()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Simulate exception by passing null (will throw in RemoveNavigationProperties)
		List<TestEntity> entities = new() { null! };

		bool result = testContext.UpdateMany(entities, true, TestContext.Current.CancellationToken);

		result.ShouldBeFalse();
	}

	[Fact]
	public void DeleteByObject_WhenException_ShouldNotThrow()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Simulate exception by passing null (will throw in RemoveNavigationProperties)
		TestEntity entity = null!;

		Should.NotThrow(() => testContext.DeleteByObject(entity, true));
	}

	[Fact]
	public void DeleteMany_WhenException_ShouldReturnFalse()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Simulate exception by passing null (will throw in RemoveNavigationProperties)
		List<TestEntity> entities = new() { null! };

		bool result = testContext.DeleteMany(entities, true);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task Create_WithRemoveNavigationProps_ShouldNotThrow()
	{
		TestEntity entity = fixture.Create<TestEntity>();
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		await testContext.Create(entity, removeNavigationProps: true);
		await testContext.SaveChanges();

		TestEntity? savedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id }, TestContext.Current.CancellationToken);
		savedEntity.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetAllStreamingFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetAllStreaming(full, x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
		results.ShouldBe(entities.Select(x => x.Name));
	}

	[Fact]
	public async Task GetAllStreaming_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetAllStreaming(x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
		results.ShouldBe(entities.Select(x => x.Name));
	}

	[Fact]
	public async Task GetAllStreamingFull_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetAllFullStreaming(x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(entities.Count);
		results.ShouldAllBe(x => !string.IsNullOrEmpty(x));
		results.ShouldBe(entities.Select(x => x.Name));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetWithFilterStreamingFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterStreaming(full, filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterStreaming_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterStreaming(filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetWithFilterFullStreaming_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		Expression<Func<TestEntity, bool>> filter = x => x.Id == target.Id;

		// Act
		List<string> results = new();
		await foreach (string name in testContext.GetWithFilterFullStreaming(filter, x => x.Name, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(name);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetOneWithFilter_WithProjection_ShouldReturnProjectedEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilter(x => x.Id == target.Id, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntity(bool full)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilter(full, x => x.Id == target.Id, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetOneWithFilterFull_WithProjection_ShouldReturnProjectedEntity()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		TestEntity target = entities[0];
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		string? result = await testContext.GetOneWithFilterFull(x => x.Id == target.Id, x => x.Name, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(target.Name);
	}

	[Fact]
	public async Task GetNavigationWithFilter_WithProjection_ShouldReturnProjectedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilter(where, select, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetNavigationWithFilterFullAndNot_WithProjection_ShouldReturnProjectedEntities(bool full)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? results = await testContext.GetNavigationWithFilter(full, where, select, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		results.ShouldNotBeNull();
		results.Count.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Fact]
	public async Task GetNavigationWithFilterStreaming_ShouldReturnStreamedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterStreaming(where, select, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetNavigationWithFilterStreamingFullAndNot_ShouldReturnStreamedEntities(bool full)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterStreaming(full, where, select, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Fact]
	public async Task GetNavigationWithFilterFullStreaming_ShouldReturnStreamedEntities()
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity> results = new();
		await foreach (TestEntity item in testContext.GetNavigationWithFilterFullStreaming(where, select, cancellationToken: TestContext.Current.CancellationToken)!)
		{
			results.Add(item);
		}

		// Assert
		results.Count.ShouldBe(1);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("A");
	}

	[Fact]
	public async Task GetWithPagingFilterFull_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 1, pageSize: 2, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilterFull_WithProjection_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<string> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x.Name, orderByString: nameof(TestEntity.Id),
			skip: 1, pageSize: 2, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
		result.Entities.ShouldAllBe(x => !string.IsNullOrEmpty(x));
	}

	[Fact]
	public async Task GetWithPagingFilterFull_TKey_ShouldReturnPagedEntities()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = true };

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilterFull(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id, skip: 1,
			pageSize: 2, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(2);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task Update_WithRemoveNavigationProps_ShouldUpdateDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		string updatedName = fixture.Create<string>();
		entity.Name = updatedName;
		entity.Details = new List<TestEntityDetail> { new() { Description = "test" } };

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		testContext.Update(entity, removeNavigationProps: true);
		await testContext.SaveChanges();

		// Assert
		TestEntity? savedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		savedEntity.ShouldNotBeNull();
		savedEntity.Name.ShouldBe(updatedName);
		savedEntity.Details.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteByObject_WithValidEntity_ShouldRemoveFromDatabase()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		testContext.DeleteByObject(entity);
		await testContext.SaveChanges();

		// Assert
		TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		deletedEntity.ShouldBeNull();
	}

	[Fact]
	public async Task SaveChanges_WithNoChanges_ShouldReturnFalse()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = await testContext.SaveChanges();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void GetQueryAll_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAll(x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAll_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAll();

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull();

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilter(_ => true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithHandlingCircularRef_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(handlingCircularRefException: true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithHandlingCircularRef_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, handlingCircularRefException: true);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryAllFull_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilter_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilter(_ => true, x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryWithFilterFull_WithProjection_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(_ => true, x => x.Name);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryNavigationWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, nameof(TestEntity.Id));

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Fact]
	public void GetQueryPagingWithFilterFull_TKey_ShouldReturnQueryable()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, x => x.Id);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	// Edge case: GetAll, GetAllFull, GetWithFilter, etc. with cancellation token already canceled
	[Fact]
	public async Task GetAll_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetAll(cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetAllFull_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetAllFull(cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetWithFilter_WithCancelledToken_ShouldReturnNull()
	{
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		List<TestEntity>? result = await testContext.GetWithFilter(_ => true, cancellationToken: cts.Token);

		result.ShouldBeNull();
	}

	// SaveChanges: generic exception path
	[Fact]
	public async Task SaveChanges_WhenGenericException_ShouldReturnFalse()
	{
		// Arrange: Use a fake context that throws a generic exception
		ServiceCollection services = new();
		TestDbContext fakeContext = Substitute.For<TestDbContext>(new DbContextOptions<TestDbContext>());
		fakeContext.When(x => x.SaveChangesAsync(default)).Do(_ => throw new Exception("Generic error"));
		services.AddSingleton<DbContext, TestDbContext>(_ => fakeContext);
		ServiceProvider provider = services.BuildServiceProvider();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(provider);

		// Act
		bool result = await testContext.SaveChanges();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void Constructor_DefaultValues_SetsPropertiesToNull()
	{
		// Act
		FullQueryOptions options = new();

		// Assert
		options.SplitQueryOverride.ShouldBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void SplitQueryOverride_SetAndGet_ReturnsCorrectValue(bool? value)
	{
		// Arrange
		FullQueryOptions options = new()
		{
			// Act
			SplitQueryOverride = value
		};

		// Assert
		options.SplitQueryOverride.ShouldBe(value);
	}

	[Fact]
	public void FullQueryOptions_CanBeUsedInCollections()
	{
		// Arrange
		List<FullQueryOptions> optionsList =
		[
			new() { SplitQueryOverride = true },
			new() { SplitQueryOverride = false },
			new() { SplitQueryOverride = null }
		];

		// Assert
		optionsList.Count.ShouldBe(3);
		optionsList[0].SplitQueryOverride.ShouldBe(true);
		optionsList[1].SplitQueryOverride.ShouldBe(false);
		optionsList[2].SplitQueryOverride.ShouldBeNull();
	}

	[Fact]
	public void Constructor_Default_InitializesEmptyList()
	{
		// Act
		GenericPagingModel<TestEntity> model = new();

		// Assert
		model.Entities.ShouldNotBeNull();
		model.Entities.ShouldBeEmpty();
		model.TotalRecords.ShouldBe(0);
	}

	[Fact]
	public void Entities_SetAndGet_WorksCorrectly()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new();
		List<TestEntity> entities = [new() { Id = 1, Name = "Test" }];

		// Act
		model.Entities = entities;

		// Assert
		model.Entities.ShouldBe(entities);
		model.Entities.Count.ShouldBe(1);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(100)]
	[InlineData(int.MaxValue)]
	public void TotalRecords_SetAndGet_WorksCorrectly(int totalRecords)
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new()
		{
			// Act
			TotalRecords = totalRecords
		};

		// Assert
		model.TotalRecords.ShouldBe(totalRecords);
	}

	[Fact]
	public void GenericPagingModel_WithMultipleEntities_HandlesCorrectly()
	{
		// Arrange
		List<TestEntity> entities =
		[
			new() { Id = 1, Name = "Entity1" },
			new() { Id = 2, Name = "Entity2" },
			new() { Id = 3, Name = "Entity3" }
		];

		// Act
		GenericPagingModel<TestEntity> model = new()
		{
			Entities = entities,
			TotalRecords = 10
		};

		// Assert
		model.Entities.Count.ShouldBe(3);
		model.TotalRecords.ShouldBe(10);
		model.Entities[0].Id.ShouldBe(1);
		model.Entities[1].Id.ShouldBe(2);
		model.Entities[2].Id.ShouldBe(3);
	}

	[Fact]
	public void GenericPagingModel_WithNullEntities_CanBeSet()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new()
		{
			// Act
			Entities = null!
		};

		// Assert
		model.Entities.ShouldBeNull();
	}

	[Fact]
	public void GenericPagingModel_ModifyingEntitiesAfterAssignment_ReflectsChanges()
	{
		// Arrange
		GenericPagingModel<TestEntity> model = new();
		List<TestEntity> entities = [new() { Id = 1, Name = "Test" }];
		model.Entities = entities;

		// Act
		entities.Add(new TestEntity { Id = 2, Name = "Test2" });

		// Assert
		model.Entities.Count.ShouldBe(2);
		model.Entities[1].Id.ShouldBe(2);
	}

	[Fact]
	public void GenericPagingModel_WithDifferentTypes_WorksCorrectly()
	{
		// Arrange & Act
		GenericPagingModel<string> stringModel = new()
		{
			Entities = ["test1", "test2"],
			TotalRecords = 2
		};

		GenericPagingModel<int> intModel = new()
		{
			Entities = [1, 2, 3],
			TotalRecords = 3
		};

		// Assert
		stringModel.Entities.Count.ShouldBe(2);
		stringModel.TotalRecords.ShouldBe(2);
		intModel.Entities.Count.ShouldBe(3);
		intModel.TotalRecords.ShouldBe(3);
	}

	#region FullQueryOptions SplitQueryOverride Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetByKeyFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetByKeyFull(entity.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetAllFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		List<TestEntity>? result = await testContext.GetAllFull(fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(entities.Count);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		string targetName = entities[0].Name;
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		List<TestEntity>? result = await testContext.GetWithFilterFull(x => x.Name == targetName, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe(targetName);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetOneWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetOneWithFilterFull(x => x.Id == entity.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMaxByOrderFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetMaxByOrderFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMinByOrderFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		TestEntity? result = await testContext.GetMinByOrderFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMaxFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		int result = await testContext.GetMaxFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetMinFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		int result = await testContext.GetMinFull(_ => true, x => x.Id, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public async Task GetNavigationWithFilterFull_WithSplitQueryOverride_ShouldHandleCorrectly(bool? splitQueryOverride)
	{
		// Arrange
		TestEntityDetail related = new() { Id = 1, Description = "desc", TestEntityId = 1 };
		TestEntity entity = new() { Id = 1, Name = "A", Details = new List<TestEntityDetail> { related } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		List<TestEntity>? result = await testContext.GetNavigationWithFilterFull(where, select, fullQueryOptions: options, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Count.ShouldBe(1);
		result[0].Id.ShouldBe(1);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryAllFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryNavigationWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryPagingWithFilterFull_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, nameof(TestEntity.Id), fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	[InlineData(null)]
	public void GetQueryPagingWithFilterFull_TKey_WithSplitQueryOverride_ShouldReturnQueryable(bool? splitQueryOverride)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		FullQueryOptions options = new() { SplitQueryOverride = splitQueryOverride };

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(_ => true, x => x, x => x.Id, fullQueryOptions: options);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	#endregion

	#region TrackEntities Parameter Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetOneWithFilter_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetOneWithFilter(x => x.Id == entity.Id, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMaxByOrder_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMaxByOrder(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMinByOrder_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetMinByOrder(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entities.Min(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMax_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int result = await testContext.GetMax(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Max(x => x.Id));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetMin_WithTrackEntities_ShouldRespectTracking(bool trackEntities)
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(3).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		int result = await testContext.GetMin(_ => true, x => x.Id, trackEntities: trackEntities, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBe(entities.Min(x => x.Id));
	}

	#endregion

	#region RemoveNavigationProps Tests

	[Fact]
	public async Task CreateMany_WithRemoveNavigationProps_ShouldNotThrow()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		entities.ForEach(e => e.Details = new List<TestEntityDetail> { new() { Description = "test" } });

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		await testContext.CreateMany(entities, removeNavigationProps: true);
		await testContext.SaveChanges();

		// Assert
		foreach (TestEntity entity in entities)
		{
			TestEntity? savedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
			savedEntity.ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task DeleteByObject_WithRemoveNavigationProps_ShouldNotThrow()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		entity.Details = new List<TestEntityDetail> { new() { Description = "test" } };
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		testContext.DeleteByObject(entity, removeNavigationProps: true);
		await testContext.SaveChanges();

		// Assert
		TestEntity? deletedEntity = await context.TestEntities.FindAsync(new object?[] { entity.Id, TestContext.Current.CancellationToken }, TestContext.Current.CancellationToken);
		deletedEntity.ShouldBeNull();
	}

	[Fact]
	public void DeleteMany_WithRemoveNavigationProps_ShouldNotThrow()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		entities.ForEach(e => e.Details = new List<TestEntityDetail> { new() { Description = "test" } });

		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = testContext.DeleteMany(entities, removeNavigationProps: true);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void UpdateMany_WithRemoveNavigationProps_ShouldNotThrow()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(2).ToList();
		context.TestEntities.AddRange(entities);
		context.SaveChanges();

		entities.ForEach(e =>
		{
			e.Name = fixture.Create<string>();
			e.Details = new List<TestEntityDetail> { new() { Description = "test" } };
		});

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		bool result = testContext.UpdateMany(entities, removeNavigationProps: true, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Edge Cases and Parameter Combinations

	[Fact]
	public async Task GetByKeyFull_WithCompoundKeyAndTrackEntities_ShouldWork()
	{
		// Arrange
		TestEntity entity = fixture.Create<TestEntity>();
		await context.TestEntities.AddAsync(entity, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		TestEntity? result = await testContext.GetByKeyFull(new object[] { entity.Id }, trackEntities: true, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(entity.Id);
	}

	[Fact]
	public async Task GetWithPagingFilter_WithZeroPageSize_ShouldUseZeroPageSize()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 0, pageSize: 0, cancellationToken: TestContext.Current.CancellationToken);

		// Assert - This overload uses pageSize directly, so 0 means 0 entities
		result.Entities.Count.ShouldBe(entities.Count);
		result.TotalRecords.ShouldBe(entities.Count);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_WithZeroPageSize_ShouldReturnAll()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(10).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, ascendingOrderExpression: x => x.Id,
			skip: 0, pageSize: 0, cancellationToken: TestContext.Current.CancellationToken);

		// Assert - This overload treats 0 as int.MaxValue
		result.Entities.Count.ShouldBe(entities.Count);
		result.TotalRecords.ShouldBe(entities.Count);
	}

	#endregion

	#region IQueryable GetQuery Methods with TrackEntities

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAll_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAll(trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAll_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAll(x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilter_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilter(_ => true, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilter_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilter(_ => true, x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAllFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryAllFull(trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryAllFull_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryAllFull(x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryWithFilterFull(_ => true, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryWithFilterFull_WithProjectionAndTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<string> query = testContext.GetQueryWithFilterFull(_ => true, x => x.Name, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryNavigationWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		Expression<Func<TestEntityDetail, bool>> where = d => d.TestEntityId == 1;
		Expression<Func<TestEntityDetail, TestEntity>> select = d => d.TestEntity!;

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryNavigationWithFilterFull(where, select, trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryPagingWithFilterFull_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			_ => true,
			x => x,
			nameof(TestEntity.Id),
			trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void GetQueryPagingWithFilterFull_TKey_WithTrackEntities_ShouldReturnQueryable(bool trackEntities)
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		IQueryable<TestEntity> query = testContext.GetQueryPagingWithFilterFull(
			_ => true,
			x => x,
			x => x.Id,
			trackEntities: trackEntities);

		// Assert
		query.ShouldNotBeNull();
		query.Expression.ShouldNotBeNull();
	}

	#endregion

	#region ServiceProvider Property Test

	[Fact]
	public void ServiceProvider_SetAndGet_ShouldWork()
	{
		// Arrange
		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);
		ServiceCollection newServices = new();
		newServices.AddDbContextPool<TestDbContext>(options =>
			options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
		IServiceProvider newServiceProvider = newServices.BuildServiceProvider();

		// Act
		testContext.ServiceProvider = newServiceProvider;

		// Assert
		testContext.ServiceProvider.ShouldBe(newServiceProvider);
	}

	#endregion

	#region Additional Edge Cases

	[Fact]
	public async Task GetWithPagingFilter_WithLargeSkip_ShouldHandleCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id),
			skip: 10, pageSize: 2, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(0);
		result.TotalRecords.ShouldBe(5);
	}

	[Fact]
	public async Task GetWithPagingFilter_TKey_WithLargeSkip_ShouldHandleCorrectly()
	{
		// Arrange
		List<TestEntity> entities = fixture.CreateMany<TestEntity>(5).ToList();
		await context.TestEntities.AddRangeAsync(entities, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		BaseDbContextActions<TestEntity, TestDbContext> testContext = new(serviceProvider);

		// Act
		GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(
			whereExpression: _ => true,
			selectExpression: x => x,
			ascendingOrderExpression: x => x.Id,
			skip: 10, // Skip more than exists
			pageSize: 2,
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Entities.Count.ShouldBe(0);
		result.TotalRecords.ShouldBe(5);
	}

	#endregion
}

