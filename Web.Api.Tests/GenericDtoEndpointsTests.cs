using System.ComponentModel.DataAnnotations;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Tests;

public sealed class GenericDtoEndpointsTests
{
	private readonly IFixture fixture;
	private readonly GenericDotEndpoints _sut;

	public GenericDtoEndpointsTests()
	{
		fixture = new Fixture()
				.Customize(new AutoFakeItEasyCustomization());
		_sut = new GenericDotEndpoints();
	}

	public sealed class TestEntity
	{
		public int Id { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;
	}

	public sealed class TestInDto
	{
		public int Id { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;
	}

	public sealed class TestOutDto
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;
	}

	public sealed class TestDbContext : DbContext
	{
		public DbSet<TestEntity> TestEntities { get; set; } = null!;
	}

	#region CreateMany Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CreateMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
		A.CallTo(() => dbContextActions.CreateMany(A<IEnumerable<TestEntity>>.Ignored, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task CreateMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task CreateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.CreateMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored))
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region Delete Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Delete_WhenSuccessful_ReturnsOkWithModel(bool removeNavigationProps)
	{
		// Arrange
		TestInDto model = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestOutDto> result = await _sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
		A.CallTo(() => dbContextActions.DeleteByObject(A<TestEntity>.Ignored, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Delete_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestInDto model = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<TestOutDto> result = await _sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Delete_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		TestInDto model = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteByObject(A<TestEntity>.Ignored, A<bool>.Ignored))
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<TestOutDto> result = await _sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region DeleteMany Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DeleteMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, removeNavigationProps)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
	}

	[Fact]
	public async Task DeleteMany_WhenDeleteManyReturnsFalse_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored)).Returns(false);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteMany_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored))
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region DeleteManyByKeys Tests

	[Fact]
	public async Task DeleteManyByKeys_WhenSuccessful_ReturnsOkWithKeys()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys)).Returns(true);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(keys);
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenDeleteFails_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys)).Returns(false);

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>.Ignored))
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<List<TestOutDto>> result = await _sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region Patch Tests (Single Key)

	[Fact]
	public async Task Patch_SingleKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_SingleKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Patch_SingleKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Will fail Required validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task Patch_SingleKey_WhenNoPatchOperations_ReturnsOriginalModel()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
	}

	[Fact]
	public async Task Patch_SingleKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Patch_SingleKey_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges())
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region Patch Tests (Multi Key)

	[Fact]
	public async Task Patch_MultiKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_MultiKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Patch_MultiKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Will fail Required validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(model);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task Patch_MultiKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<TestOutDto> result = await _sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region Update Tests (Single Key)

	[Fact]
	public async Task Update_SingleKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();
		inDto.Name = "Updated Name";

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(dbModel);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestOutDto? outDto = (result.Result as OkObjectResult)!.Value as TestOutDto;
		outDto.ShouldNotBeNull();
		A.CallTo(() => dbContextActions.Update(A<TestEntity>.Ignored)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Update_SingleKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		TestInDto inDto = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Update_SingleKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();
		inDto.Name = null!; // Will fail Required validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(dbModel);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task Update_SingleKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(dbModel);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Update_SingleKey_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(dbModel);
		A.CallTo(() => dbContextActions.SaveChanges()).Throws<InvalidOperationException>();

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region Update Tests (Multi Key)

	[Fact]
	public async Task Update_MultiKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();
		inDto.Name = "Updated Name";

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(dbModel);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestOutDto? outDto = (result.Result as OkObjectResult)!.Value as TestOutDto;
		outDto.ShouldNotBeNull();
		A.CallTo(() => dbContextActions.Update(A<TestEntity>.Ignored)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Update_MultiKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		TestInDto inDto = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task Update_MultiKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();
		inDto.Name = null!; // Will fail Required validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(dbModel);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task Update_MultiKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestEntity dbModel = fixture.Create<TestEntity>();
		TestInDto inDto = fixture.Create<TestInDto>();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(dbModel);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		ActionResult<TestOutDto> result = await _sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion
}
