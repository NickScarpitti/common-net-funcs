using System.ComponentModel.DataAnnotations;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Tests;

public sealed class GenericMinimalDtoEndpointsTests
{
	private readonly IFixture fixture;

	public GenericMinimalDtoEndpointsTests()
	{
		fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
	}

	public sealed class TestEntity
	{
		public int Id { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		public string Description { get; set; } = string.Empty;
	}

	public sealed class TestEntityWithObjectValidation : IValidatableObject
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public bool AlwaysFail { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (AlwaysFail)
			{
				yield return new ValidationResult("Object level validation failed");
			}
		}
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
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestOutDto>>>();
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
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task CreateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.CreateMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.CreateMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent> result = await GenericMinimalDtoEndpoints.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
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
		Results<Ok<TestOutDto>, NoContent> result = await GenericMinimalDtoEndpoints.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task Delete_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		TestInDto model = fixture.Create<TestInDto>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteByObject(A<TestEntity>.Ignored, A<bool>.Ignored)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<TestOutDto>, NoContent> result = await GenericMinimalDtoEndpoints.Delete<TestEntity, TestDbContext, TestInDto, TestOutDto>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestOutDto>>>();
	}

	[Fact]
	public async Task DeleteMany_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenDeleteReturnsFalse_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._)).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestInDto> models = fixture.CreateMany<TestInDto>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteMany<TestEntity, TestDbContext, TestInDto, TestOutDto>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region DeleteManyByKeys Tests

	[Fact]
	public async Task DeleteManyByKeys_WhenSuccessful_ReturnsOkWithKeys()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._)).Returns(true);

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestOutDto>>>();
		((Ok<IEnumerable<TestOutDto>>)result.Result).Value.ShouldNotBeNull();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenDeleteFails_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._)).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestOutDto>>, NoContent> result = await GenericMinimalDtoEndpoints.DeleteManyByKeys<TestEntity, TestDbContext, TestOutDto>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
		((Ok<TestOutDto>)result.Result).Value!.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_SingleKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
	}

	[Fact]
	public async Task Patch_SingleKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null); // Required - will fail validation

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		A.CallTo(() => dbContextActions.SaveChanges()).Throws<InvalidOperationException>();

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
		((Ok<TestOutDto>)result.Result).Value!.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_MultiKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task Patch_MultiKey_WhenValidationFails_ReturnsValidationProblem()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		JsonPatchDocument<TestEntity> patch = new();
		patch.Replace(x => x.Name, null);

		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(model);

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Patch<TestEntity, TestDbContext, TestOutDto>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestOutDto>>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
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
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result = await GenericMinimalDtoEndpoints.Update<TestEntity, TestDbContext, TestInDto, TestOutDto>(new object[] { 1, 2 }, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region Patch Object-Level Validation Tests

	[Fact]
	public async Task Patch_SingleKey_WhenObjectLevelValidationFails_ReturnsProblemWithErrorKey()
	{
		// Arrange
		TestEntityWithObjectValidation model = new() { Id = 1, Name = "Test", AlwaysFail = false };
		JsonPatchDocument<TestEntityWithObjectValidation> patch = new();
		patch.Replace(x => x.AlwaysFail, true); // triggers object-level IValidatableObject error with no member name

		IBaseDbContextActions<TestEntityWithObjectValidation, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntityWithObjectValidation, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(model);

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result =
			await GenericMinimalDtoEndpoints.Patch<TestEntityWithObjectValidation, TestDbContext, TestOutDto>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
	}

	#endregion

	#region Update Object-Level Validation Tests

	[Fact]
	public async Task Update_SingleKey_WhenObjectLevelValidationFails_ReturnsProblemWithErrorKey()
	{
		// Arrange
		TestEntityWithObjectValidation dbModel = new() { Id = 1, Name = "Test", AlwaysFail = false };
		TestInDto inDto = new() { Id = 1, Name = "Test" };

		IBaseDbContextActions<TestEntityWithObjectValidation, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntityWithObjectValidation, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(dbModel);

		// The CopyPropertiesTo will set AlwaysFail = false (from inDto default)
		// We need AlwaysFail = true on the entity AFTER copying from inDto
		// Use a special DTO that sets AlwaysFail = true
		dbModel.AlwaysFail = true; // set so that after DeepClone + CopyPropertiesTo, AlwaysFail stays false in updateModel

		// Actually, to trigger the object-level error, we need the updateModel to have AlwaysFail = true.
		// Since inDto does NOT have AlwaysFail, CopyPropertiesTo won't set it.
		// So start with dbModel.AlwaysFail = true; after DeepClone, updateModel.AlwaysFail = true;
		// CopyPropertiesTo(inDto -> updateModel) won't affect AlwaysFail (no such prop on inDto).
		// Wait, CopyPropertiesTo copies from source to target using matching props.
		// inDto has Id, Name, Description - not AlwaysFail.
		// So updateModel.AlwaysFail stays true after CopyPropertiesTo.

		// Act
		Results<Ok<TestOutDto>, NoContent, ValidationProblem> result =
			await GenericMinimalDtoEndpoints.Update<TestEntityWithObjectValidation, TestDbContext, TestInDto, TestOutDto>(1, inDto, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
	}

	#endregion
}
