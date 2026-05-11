using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using static Xunit.TestContext;

namespace Web.Api.Tests;

public sealed class GenericMinimalEndpointsTests
{
	private readonly IFixture fixture;

	public GenericMinimalEndpointsTests()
	{
		fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
	}

	public sealed class TestEntity
	{
		public int Id { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;
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
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestEntity>>>();
		((Ok<IEnumerable<TestEntity>>)result.Result).Value.ShouldBe(models);
		A.CallTo(() => dbContextActions.CreateMany(models, removeNavigationProps)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task CreateMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task CreateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.CreateMany(A<IEnumerable<TestEntity>>.Ignored, A<bool>.Ignored)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.CreateMany<TestEntity, TestDbContext>(models, dbContextActions);

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
		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		Results<Ok<TestEntity>, NoContent> result = await GenericMinimalEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestEntity>>();
		((Ok<TestEntity>)result.Result).Value.ShouldBe(model);
		A.CallTo(() => dbContextActions.DeleteByObject(model, removeNavigationProps, null)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task Delete_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		Results<Ok<TestEntity>, NoContent> result = await GenericMinimalEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task Delete_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		TestEntity model = fixture.Create<TestEntity>();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteByObject(A<TestEntity>.Ignored, A<bool>.Ignored, A<GlobalFilterOptions?>.Ignored)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<TestEntity>, NoContent> result = await GenericMinimalEndpoints.Delete<TestEntity, TestDbContext>(model, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region DeleteMany (IEnumerable) Tests

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task DeleteMany_WhenSuccessful_ReturnsOkWithModels(bool removeNavigationProps)
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._, A<GlobalFilterOptions?>._)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(true);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions, removeNavigationProps);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestEntity>>>();
	}

	[Fact]
	public async Task DeleteMany_WhenEmptyList_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = [];
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenDeleteReturnsFalse_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._, A<GlobalFilterOptions?>._)).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenSaveFails_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._, A<GlobalFilterOptions?>._)).Returns(true);
		A.CallTo(() => dbContextActions.SaveChanges()).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<TestEntity> models = fixture.CreateMany<TestEntity>(3).ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<IEnumerable<TestEntity>>._, A<bool>._, A<GlobalFilterOptions?>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(models, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region DeleteMany (Expression) Tests

	[Fact]
	public async Task DeleteMany_WithExpression_WhenSuccessful_ReturnsOkWithCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		const int expectedDeletedCount = 3;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(expectedDeletedCount);

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<Ok<int>>();
		((Ok<int>)result.Result).Value.ShouldBe(expectedDeletedCount);
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenReturnsZero_ReturnsOkWithZero()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 100;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(A<Expression<Func<TestEntity, bool>>>._, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(0);

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<Ok<int>>();
		((Ok<int>)result.Result).Value.ShouldBe(0);
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(Task.FromResult<int?>(null));

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteMany_WithExpression_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteMany(whereClause, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.DeleteMany<TestEntity, TestDbContext>(whereClause, dbContextActions, cancellationToken: Current.CancellationToken);

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
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._, A<GlobalFilterOptions?>._)).Returns(true);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<IEnumerable<TestEntity>>>();
		((Ok<IEnumerable<TestEntity>>)result.Result).Value.ShouldNotBeNull();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenDeleteFails_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._, A<GlobalFilterOptions?>._)).Returns(false);

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

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
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task DeleteManyByKeys_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		List<object> keys = fixture.CreateMany<int>(3).Cast<object>().ToList();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.DeleteManyByKeys(A<IEnumerable<object>>._, A<GlobalFilterOptions?>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<IEnumerable<TestEntity>>, NoContent> result = await GenericMinimalEndpoints.DeleteManyByKeys<TestEntity, TestDbContext>(keys, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region UpdateMany Tests

	[Fact]
	public async Task UpdateMany_WhenSuccessful_ReturnsOkWithUpdatedCount()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => builder.SetProperty(e => e.Name, "Updated");
		const int expectedUpdatedCount = 5;
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, setPropertyCalls, A<TimeSpan?>._, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(expectedUpdatedCount);

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<Ok<int>>();
		((Ok<int>)result.Result).Value.ShouldBe(expectedUpdatedCount);
	}

	[Fact]
	public async Task UpdateMany_WhenReturnsZero_ReturnsOkWithZero()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => builder.SetProperty(e => e.Name, "Updated");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, setPropertyCalls, A<TimeSpan?>._, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(0);

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<Ok<int>>();
		((Ok<int>)result.Result).Value.ShouldBe(0);
	}

	[Fact]
	public async Task UpdateMany_WhenReturnsNull_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => builder.SetProperty(e => e.Name, "Updated");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, setPropertyCalls, A<TimeSpan?>._, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Returns(Task.FromResult<int?>(null));

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions, cancellationToken: Current.CancellationToken);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	[Fact]
	public async Task UpdateMany_WhenExceptionThrown_ReturnsNoContent()
	{
		// Arrange
		Expression<Func<TestEntity, bool>> whereClause = x => x.Id > 5;
		Action<UpdateSettersBuilder<TestEntity>> setPropertyCalls = builder => builder.SetProperty(e => e.Name, "Updated");
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.UpdateMany(whereClause, setPropertyCalls, A<TimeSpan?>._, A<GlobalFilterOptions?>._, A<CancellationToken>._)).Throws<InvalidOperationException>();

		// Act
		Results<Ok<int>, NoContent> result = await GenericMinimalEndpoints.UpdateMany<TestEntity, TestDbContext>(whereClause, setPropertyCalls, dbContextActions, cancellationToken: Current.CancellationToken);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestEntity>>();
		((Ok<TestEntity>)result.Result).Value!.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_SingleKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestEntity>>();
		((Ok<TestEntity>)result.Result).Value.ShouldBe(model);
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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(1, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<Ok<TestEntity>>();
		((Ok<TestEntity>)result.Result).Value!.Name.ShouldBe("Updated Name");
	}

	[Fact]
	public async Task Patch_MultiKey_WhenModelNotFound_ReturnsNoContent()
	{
		// Arrange
		JsonPatchDocument<TestEntity> patch = new();
		IBaseDbContextActions<TestEntity, TestDbContext> dbContextActions = A.Fake<IBaseDbContextActions<TestEntity, TestDbContext>>();
		A.CallTo(() => dbContextActions.GetByKey(A<object[]>.Ignored, null, default)).Returns(Task.FromResult<TestEntity?>(null));

		// Act
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

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
		Results<Ok<TestEntity>, NoContent, ValidationProblem> result = await GenericMinimalEndpoints.Patch<TestEntity, TestDbContext>(new object[] { 1, 2 }, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<NoContent>();
	}

	#endregion

	#region Patch Tests (Object-Level Validation)

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
		Results<Ok<TestEntityWithObjectValidation>, NoContent, ValidationProblem> result =
			await GenericMinimalEndpoints.Patch<TestEntityWithObjectValidation, TestDbContext>(1, patch, dbContextActions);

		// Assert
		result.Result.ShouldBeOfType<ValidationProblem>();
	}

	#endregion
}
