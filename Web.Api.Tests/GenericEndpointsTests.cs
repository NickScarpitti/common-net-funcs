<<<<<<< HEAD
﻿using System.ComponentModel.DataAnnotations;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
}
=======
﻿using System.ComponentModel.DataAnnotations;
using AutoFixture.AutoFakeItEasy;
using CommonNetFuncs.EFCore;
using CommonNetFuncs.Web.Api;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly
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
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
