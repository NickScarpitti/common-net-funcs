using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Web.Api.Tests;

public sealed class GenericEndpointsTests
{
	private readonly IFixture _fixture;
	private readonly GenericEndpoints _sut;

	public GenericEndpointsTests()
	{
		_fixture = new Fixture()
				.Customize(new AutoFakeItEasyCustomization());
		_sut = new GenericEndpoints();
	}

	public sealed class TestEntity
	{
		public int Id { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;
	}

	public sealed class TestDbContext : DbContext
	{
		public DbSet<TestEntity> TestEntities { get; set; } = null!;
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CreateMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange
		List<TestEntity> models = _fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<List<TestEntity>> result = await _sut.CreateMany(models, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
		A.CallTo(() => dbContextActions.CreateMany(models, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task CreateMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = _fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<List<TestEntity>> result = await _sut.CreateMany(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Delete_WhenSuccessful_ReturnsOkWithModel(bool removeNavigationProps)
	{
		// Arrange
		TestEntity model = _fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestEntity> result = await _sut.Delete(model, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
		A.CallTo(() => dbContextActions.DeleteByObject(model, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DeleteMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange
		List<TestEntity> models = _fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(models, removeNavigationProps)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<List<TestEntity>> result = await _sut.DeleteMany(models, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenSuccessful_ReturnsOkWithKeys()
	{
		// Arrange
		List<object> keys = _fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys)).Returns(true);

		// Act
		ActionResult<List<TestEntity>> result = await _sut.DeleteManyByKeys(keys, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(keys);
	}

	[Fact]
	public async Task Patch_SingleKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity model = _fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestEntity> result = await _sut.Patch(1, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_MultiKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity model = _fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestEntity> result = await _sut.Patch(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		ActionResult<TestEntity> result = await _sut.Patch(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Patch_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity model = _fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Will fail Required validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		ActionResult<TestEntity> result = await _sut.Patch(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task Patch_WhenNoPatchOperations_ReturnsOriginalModel()
	{
		// Arrange
		TestEntity model = _fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		ActionResult<TestEntity> result = await _sut.Patch(1, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenSuccessful_ReturnsOkWithDeletedCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		const int expectedDeletedCount = 3;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).Returns(expectedDeletedCount);

		// Act
		ActionResult<int> result = await _sut.DeleteMany(whereClause, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(expectedDeletedCount);
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenReturnsZero_ReturnsOkWithZero()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 100;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).Returns(0);

		// Act
		ActionResult<int> result = await _sut.DeleteMany(whereClause, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(0);
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).Returns(Task.FromResult<int?>(null));

		// Act
		ActionResult<int> result = await _sut.DeleteMany(whereClause, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).Throws<InvalidOperationException>();

		// Act
		ActionResult<int> result = await _sut.DeleteMany(whereClause, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task UpdateMany_WhenSuccessful_ReturnsOkWithUpdatedCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>>? updateSettersConfig = (builder) => builder.SetProperty(e => e.Name, "Updated Name");
		const int expectedUpdatedCount = 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).Returns(expectedUpdatedCount);

		// Act
		ActionResult<int> result = await _sut.UpdateMany(whereClause, updateSettersConfig, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(expectedUpdatedCount);
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task UpdateMany_WhenReturnsZero_ReturnsOkWithZero()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 100;
		Action<UpdateSettersBuilder<TestEntity>>? updateSettersConfig = (builder) => builder.SetProperty(e => e.Name, "Updated Name");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).Returns(0);

		// Act
		ActionResult<int> result = await _sut.UpdateMany(whereClause, updateSettersConfig, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(0);
	}

	[Fact]
	public async Task UpdateMany_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;

		Action<UpdateSettersBuilder<TestEntity>>? updateSettersConfig = (builder) => builder.SetProperty(e => e.Name, "Updated Name");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).Returns(Task.FromResult<int?>(null));

		// Act
		ActionResult<int> result = await _sut.UpdateMany(whereClause, updateSettersConfig, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task UpdateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>>? updateSettersConfig = (builder) => builder.SetProperty(e => e.Name, "Updated Name");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).Throws<InvalidOperationException>();

		// Act
		ActionResult<int> result = await _sut.UpdateMany(whereClause, updateSettersConfig, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task UpdateMany_WithComplexSetPropertyCalls_WhenSuccessful_ReturnsOkWithUpdatedCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5 && x.Name.StartsWith("Test");
		Action<UpdateSettersBuilder<TestEntity>>? updateSettersConfig = (builder) => builder.SetProperty(e => e.Name, "New Name").SetProperty(e => e.Id, e => e.Id + 100); // Not used in this test

		const int expectedUpdatedCount = 2;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, updateSettersConfig)).Returns(expectedUpdatedCount);

		// Act
		ActionResult<int> result = await _sut.UpdateMany(whereClause, updateSettersConfig, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(expectedUpdatedCount);
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenLargeNumberDeleted_ReturnsOkWithCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Name.Contains("delete");
		const int expectedDeletedCount = 1000;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause)).Returns(expectedDeletedCount);

		// Act
		ActionResult<int> result = await _sut.DeleteMany(whereClause, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(expectedDeletedCount);
	}
}
