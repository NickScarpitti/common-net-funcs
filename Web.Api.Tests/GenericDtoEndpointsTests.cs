using System.ComponentModel.DataAnnotations;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Web.Api.Tests;

public sealed class GenericDtoEndpointsTests
{
	private readonly IFixture fixture;
	private readonly GenericDotEndpoints sut;

	public GenericDtoEndpointsTests()
	{
		fixture = new Fixture()
				.Customize(new AutoFakeItEasyCustomization());
		sut = new GenericDotEndpoints();
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

		ActionResult<List<TestOutDto>> result = await sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

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

		ActionResult<List<TestOutDto>> result = await sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions, removeNavigationProps);

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

		ActionResult<TestOutDto> result = await sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

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

		ActionResult<List<TestOutDto>> result = await sut.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

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

		ActionResult<TestOutDto> result = await sut.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region GenericEndpoints Tests


	private readonly GenericEndpoints genericEndpoints = new();

	#region GenericEndpoints.CreateMany Tests


	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GenericEndpoints_CreateMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions, removeNavigationProps);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
		A.CallTo(() => dbContextActions.CreateMany(models, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GenericEndpoints_CreateMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_CreateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.CreateMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored))
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	#endregion

	#region GenericEndpoints.Delete Tests


	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GenericEndpoints_Delete_WhenSuccessful_ReturnsOkWithModel(bool removeNavigationProps)
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions, removeNavigationProps);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
		A.CallTo(() => dbContextActions.DeleteByObject(model, removeNavigationProps, null)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GenericEndpoints_Delete_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Delete_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteByObject(A<TestEntity>.Ignored, A<bool>.Ignored, A<GlobalFilterOptions?>.Ignored))
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Delete_WithGlobalFilterOptions_CallsDeleteWithOptions()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		GlobalFilterOptions filterOptions = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions, false, filterOptions);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.DeleteByObject(model, false, filterOptions)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.DeleteMany (IEnumerable) Tests


	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GenericEndpoints_DeleteMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(models, removeNavigationProps, null)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions, removeNavigationProps);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(models);
	}

	[Fact]
	public async Task GenericEndpoints_DeleteMany_WhenDeleteManyReturnsFalse_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored, A<GlobalFilterOptions?>.Ignored)).Returns(false);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored, A<GlobalFilterOptions?>.Ignored)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteMany_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored, A<GlobalFilterOptions?>.Ignored))
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteMany_WithGlobalFilterOptions_CallsDeleteManyWithOptions()
	{
		// Arrange

		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		GlobalFilterOptions filterOptions = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(models, false, filterOptions)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions, false, filterOptions);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.DeleteMany(models, false, filterOptions)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.DeleteMany (Expression) Tests


	[Fact]
	public async Task GenericEndpoints_DeleteManyByExpression_WhenSuccessful_ReturnsOkWithCount()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, null, A<CancellationToken>.Ignored)).Returns(3);

		// Act

		ActionResult<int> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(3);
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByExpression_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, null, A<CancellationToken>.Ignored)).Returns(Task.FromResult<int?>(null));

		// Act

		ActionResult<int> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByExpression_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<System.Linq.Expressions.Expression<Func<TestEntity, bool>>>.Ignored, A<GlobalFilterOptions?>.Ignored, A<CancellationToken>.Ignored))
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<int> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByExpression_WithGlobalFilterAndCancellation_PassesParameters()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		GlobalFilterOptions filterOptions = new();
		CancellationToken cancellationToken = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, filterOptions, cancellationToken)).Returns(3);

		// Act

		ActionResult<int> result = await genericEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions, filterOptions, cancellationToken);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, filterOptions, cancellationToken)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.DeleteManyByKeys Tests


	[Fact]
	public async Task GenericEndpoints_DeleteManyByKeys_WhenSuccessful_ReturnsOkWithKeys()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys, null)).Returns(true);

		// Act
		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(keys);
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByKeys_WhenDeleteFails_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys, null)).Returns(false);

		// Act
		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByKeys_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByKeys_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>.Ignored, A<GlobalFilterOptions?>.Ignored))
				.Throws<InvalidOperationException>();

		// Act
		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_DeleteManyByKeys_WithGlobalFilterOptions_PassesOptions()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		GlobalFilterOptions filterOptions = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys, filterOptions)).Returns(true);

		// Act
		ActionResult<List<TestEntity>> result = await genericEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions, filterOptions);

		// Assert
		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(keys, filterOptions)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.UpdateMany Tests


	[Fact]
	public async Task GenericEndpoints_UpdateMany_WhenSuccessful_ReturnsOkWithCount()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => { };
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, A<Action<UpdateSettersBuilder<TestEntity>>>.Ignored, null, null, A<CancellationToken>.Ignored)).Returns(3);

		// Act

		ActionResult<int> result = await genericEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		((OkObjectResult)result.Result!).Value.ShouldBe(3);
	}

	[Fact]
	public async Task GenericEndpoints_UpdateMany_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => { };
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, A<Action<UpdateSettersBuilder<TestEntity>>>.Ignored, null, null, A<CancellationToken>.Ignored)).Returns(Task.FromResult<int?>(null));

		// Act

		ActionResult<int> result = await genericEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_UpdateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => { };
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(A<System.Linq.Expressions.Expression<Func<TestEntity, bool>>>.Ignored,
				A<Action<UpdateSettersBuilder<TestEntity>>>.Ignored, A<TimeSpan?>.Ignored,
				A<GlobalFilterOptions?>.Ignored, A<CancellationToken>.Ignored))
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<int> result = await genericEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_UpdateMany_WithGlobalFilterAndCancellation_PassesParameters()
	{
		// Arrange

		System.Linq.Expressions.Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => { };
		GlobalFilterOptions filterOptions = new();
		CancellationToken cancellationToken = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, A<Action<UpdateSettersBuilder<TestEntity>>>.Ignored, null, filterOptions, cancellationToken)).Returns(3);

		// Act

		ActionResult<int> result = await genericEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions, filterOptions, cancellationToken);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, setPropertyCalls, null, filterOptions, cancellationToken)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.Patch (Single Key) Tests


	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange

		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Will fail Required validation


		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenNoPatchOperations_ReturnsOriginalModel()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges())
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_SingleKey_WithGlobalFilterOptions_PassesOptions()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");
		GlobalFilterOptions filterOptions = new();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, filterOptions, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions, filterOptions);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.GetByKey(1, null, filterOptions, default)).MustHaveHappenedOnceExactly();
	}

	#endregion

	#region GenericEndpoints.Patch (Multi Key) Tests


	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenSuccessful_ReturnsOkWithUpdatedModel()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		TestEntity? updatedModel = (result.Result as OkObjectResult)!.Value as TestEntity;
		updatedModel.ShouldNotBeNull();
		updatedModel.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange

		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Will fail Required validation


		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<ObjectResult>();
		((ObjectResult)result.Result!).StatusCode.ShouldBe(400);
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WithGlobalFilterOptions_PassesOptions()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");
		GlobalFilterOptions filterOptions = new();
		object[] keys = new object[] { 1, 2 };

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(keys), null, filterOptions, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(keys, patch, dbContextActions, filterOptions);

		// Assert

		result.Result.ShouldBeOfType<OkObjectResult>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(keys), null, filterOptions, default)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, "Updated Name");

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);
		A.CallTo(() => dbContextActions.SaveChanges())
				.Throws<InvalidOperationException>();

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.Result.ShouldBeOfType<NoContentResult>();
	}

	[Fact]
	public async Task GenericEndpoints_Patch_MultiKey_WhenNoPatchOperations_ReturnsOriginalModel()
	{
		// Arrange

		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.That.IsSameSequenceAs(new object[] { 1, 2 }), null, A<GlobalFilterOptions?>.Ignored, default)).Returns(model);

		// Act

		ActionResult<TestEntity> result = await genericEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert

		result.ShouldNotBeNull();
		result.Result.ShouldBeOfType<OkObjectResult>();
		(result.Result as OkObjectResult)!.Value.ShouldBe(model);
	}

	#endregion


	#endregion
}
