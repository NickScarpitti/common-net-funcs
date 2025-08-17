<<<<<<< HEAD
﻿using System.Reflection;
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web.Requests.Tests;

public class PatchCreatorTests
{
    private sealed class TestModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public decimal Amount { get; set; }

        public DateTime? Date { get; set; }

        public NestedModel? Nested { get; set; }
    }

    private sealed class NestedModel
    {
        public string? Value { get; set; }
    }

    private const string AddOperation = "add";
    private const string RemoveOperation = "remove";
    private const string ReplaceOperation = "replace";

    [Fact]
    public void CreatePatch_ShouldReturnEmptyPatch_WhenObjectsAreEqual()
    {
        TestModel original = new() { Id = 1, Name = "Test", Amount = 10.5m, Date = DateTime.UtcNow, Nested = new NestedModel { Value = "A" } };
        TestModel modified = JsonConvert.DeserializeObject<TestModel>(JsonConvert.SerializeObject(original))!;

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(10, 20)]
    public void CreatePatch_ShouldDetectIntChange(int originalId, int modifiedId)
    {
        TestModel original = new() { Id = originalId, Name = "Test" };
        TestModel modified = new() { Id = modifiedId, Name = "Test" };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Id");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modifiedId);
    }

    [Theory]
    [InlineData("Alpha", "Beta")]
    [InlineData("Test", "Test2")]
    public void CreatePatch_ShouldDetectStringChange(string originalName, string modifiedName)
    {
        TestModel original = new() { Id = 1, Name = originalName };
        TestModel modified = new() { Id = 1, Name = modifiedName };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modifiedName);
    }

    [Fact]
    public void CreatePatch_ShouldDetectDecimalChange()
    {
        TestModel original = new() { Id = 1, Amount = 1.23m };
        TestModel modified = new() { Id = 1, Amount = 4.56m };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Amount");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(4.56m);
    }

    [Fact]
    public void CreatePatch_ShouldDetectDateChange()
    {
        TestModel original = new() { Id = 1, Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        TestModel modified = new() { Id = 1, Date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Date");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modified.Date);
    }

    [Fact]
    public void CreatePatch_ShouldDetectAddedProperty()
    {
        JObject original = new() { ["A"] = 1 };
        JObject modified = new() { ["A"] = 1, ["B"] = 2 };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(AddOperation);
        patch.Operations[0].path.ShouldBe("/B");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(2);
    }

    [Fact]
    public void CreatePatch_ShouldDetectRemovedProperty()
    {
        JObject original = new() { ["A"] = 1, ["B"] = 2 };
        JObject modified = new() { ["A"] = 1 };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(RemoveOperation);
        patch.Operations[0].path.ShouldBe("/B");
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectChange()
    {
        TestModel original = new() { Id = 1, Nested = new NestedModel { Value = "X" } };
        TestModel modified = new() { Id = 1, Nested = new NestedModel { Value = "Y" } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested/Value");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("Y");
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectAdded()
    {
        TestModel original = new() { Id = 1, Nested = null };
        TestModel modified = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested");
        patch.Operations[0].value.ShouldBeOfType<JObject>();
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectRemoved()
    {
        TestModel original = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };
        TestModel modified = new() { Id = 1, Nested = null };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested");
    }

    [Fact]
    public void CreatePatch_ShouldUseCustomJsonSerializer()
    {
        TestModel original = new() { Id = 1, Name = "A" };
        TestModel modified = new() { Id = 1, Name = "B" };

        JsonSerializerSettings customSettings = new() { NullValueHandling = NullValueHandling.Ignore };
        JsonSerializer customSerializer = JsonSerializer.Create(customSettings);

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified, customSerializer);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("B");
    }

    [Fact]
    public void CreatePatch_ShouldHandleNullProperties()
    {
        TestModel original = new() { Id = 1, Name = null };
        TestModel modified = new() { Id = 1, Name = "NotNull" };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("NotNull");
    }

    [Fact]
    public void CreatePatch_ShouldHandleNullToNull_NoPatch()
    {
        TestModel original = new() { Id = 1, Name = null };
        TestModel modified = new() { Id = 1, Name = null };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.ShouldBeEmpty();
    }

    [Fact]
    public void CreatePatch_ShouldHandleComplexTypeChange()
    {
        var original = new { A = new { B = 1 } };
        var modified = new { A = new { B = 2 } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/A/B");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(2);
    }

    [Fact]
    public void FillPatchForObject_ShouldRecurse_WhenFloatTypeAndObjectTypeIsTrue()
    {
        // Arrange: Create nested objects where the property is a float and a nested object
        JObject originalNested = new()
        {
            ["Value"] = 1.23f
        };
        JObject modifiedNested = new()
        {
            ["Value"] = 4.56f
        };

        JObject original = new()
        {
            ["Nested"] = originalNested
        };
        JObject modified = new()
        {
            ["Nested"] = modifiedNested
        };

        JsonPatchDocument patch = new();

        // Act: Call FillPatchForObject directly via reflection since it's private
        MethodInfo? method = typeof(PatchCreator).GetMethod("FillPatchForObject", BindingFlags.NonPublic | BindingFlags.Static);
        method!.Invoke(null, new object[] { original, modified, patch, "/" });

        // Assert: Should have a replace operation for the nested float value
        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe("replace");
        patch.Operations[0].path.ShouldBe("/Nested/Value");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(4.56f);
    }
}
=======
﻿using System.Reflection;
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web.Requests.Tests;

public class PatchCreatorTests
{
    private sealed class TestModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public decimal Amount { get; set; }

        public DateTime? DateTime { get; set; }

        public DateOnly? DateOnly { get; set; }

        public DateTimeOffset? DateTimeOffset { get; set; }

        public NestedModel? Nested { get; set; }
    }

    private sealed class NestedModel
    {
        public string? Value { get; set; }
    }

    private const string AddOperation = "add";
    private const string RemoveOperation = "remove";
    private const string ReplaceOperation = "replace";

    [Fact]
    public void CreatePatch_ShouldReturnEmptyPatch_WhenObjectsAreEqual()
    {
        TestModel original = new() { Id = 1, Name = "Test", Amount = 10.5m, DateTime = DateTime.UtcNow, DateTimeOffset = DateTimeOffset.Now, DateOnly = DateOnly.FromDateTime(DateTime.Now), Nested = new NestedModel { Value = "A" } };
        TestModel modified = JsonConvert.DeserializeObject<TestModel>(JsonConvert.SerializeObject(original))!;

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(10, 20)]
    public void CreatePatch_ShouldDetectIntChange(int originalId, int modifiedId)
    {
        TestModel original = new() { Id = originalId, Name = "Test" };
        TestModel modified = new() { Id = modifiedId, Name = "Test" };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Id");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modifiedId);
    }

    [Theory]
    [InlineData("Alpha", "Beta")]
    [InlineData("Test", "Test2")]
    public void CreatePatch_ShouldDetectStringChange(string originalName, string modifiedName)
    {
        TestModel original = new() { Id = 1, Name = originalName };
        TestModel modified = new() { Id = 1, Name = modifiedName };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modifiedName);
    }

    [Fact]
    public void CreatePatch_ShouldDetectDecimalChange()
    {
        TestModel original = new() { Id = 1, Amount = 1.23m };
        TestModel modified = new() { Id = 1, Amount = 4.56m };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Amount");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(4.56m);
    }

    [Fact]
    public void CreatePatch_ShouldDetectDateChange()
    {
        DateTime originalDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime modifiedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TestModel original = new() { Id = 1, DateTime = originalDate, DateOnly = DateOnly.FromDateTime(originalDate), DateTimeOffset = new(originalDate) };
        TestModel modified = new() { Id = 1, DateTime = modifiedDate, DateOnly = DateOnly.FromDateTime(modifiedDate), DateTimeOffset = new(modifiedDate) };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(3);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/DateTime");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        patch.Operations[1].op.ShouldBe(ReplaceOperation);
        patch.Operations[1].path.ShouldBe("/DateOnly");
        patch.Operations[1].value.ShouldBeOfType<JValue>();
        patch.Operations[2].op.ShouldBe(ReplaceOperation);
        patch.Operations[2].path.ShouldBe("/DateTimeOffset");
        patch.Operations[2].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(modified.DateTime);
        DateOnly.TryParse(((JValue)patch.Operations[1].value).Value?.ToString(), out DateOnly dateOnlyResult).ShouldBeTrue();
        DateOnly? nullableDateOnlyResult = dateOnlyResult;
        nullableDateOnlyResult.ShouldBe(modified.DateOnly);
        ((DateTimeOffset?)((JValue)patch.Operations[2].value).Value).ShouldBe(modified.DateTimeOffset);
    }

    [Fact]
    public void CreatePatch_ShouldDetectAddedProperty()
    {
        JObject original = new() { ["A"] = 1 };
        JObject modified = new() { ["A"] = 1, ["B"] = 2 };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(AddOperation);
        patch.Operations[0].path.ShouldBe("/B");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(2);
    }

    [Fact]
    public void CreatePatch_ShouldDetectRemovedProperty()
    {
        JObject original = new() { ["A"] = 1, ["B"] = 2 };
        JObject modified = new() { ["A"] = 1 };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(RemoveOperation);
        patch.Operations[0].path.ShouldBe("/B");
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectChange()
    {
        TestModel original = new() { Id = 1, Nested = new NestedModel { Value = "X" } };
        TestModel modified = new() { Id = 1, Nested = new NestedModel { Value = "Y" } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested/Value");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("Y");
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectAdded()
    {
        TestModel original = new() { Id = 1, Nested = null };
        TestModel modified = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested");
        patch.Operations[0].value.ShouldBeOfType<JObject>();
    }

    [Fact]
    public void CreatePatch_ShouldDetectNestedObjectRemoved()
    {
        TestModel original = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };
        TestModel modified = new() { Id = 1, Nested = null };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Nested");
    }

    [Fact]
    public void CreatePatch_ShouldUseCustomJsonSerializer()
    {
        TestModel original = new() { Id = 1, Name = "A" };
        TestModel modified = new() { Id = 1, Name = "B" };

        JsonSerializerSettings customSettings = new() { NullValueHandling = NullValueHandling.Ignore };
        JsonSerializer customSerializer = JsonSerializer.Create(customSettings);

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified, customSerializer);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("B");
    }

    [Fact]
    public void CreatePatch_ShouldHandleNullProperties()
    {
        TestModel original = new() { Id = 1, Name = null };
        TestModel modified = new() { Id = 1, Name = "NotNull" };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/Name");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe("NotNull");
    }

    [Fact]
    public void CreatePatch_ShouldHandleNullToNull_NoPatch()
    {
        TestModel original = new() { Id = 1, Name = null };
        TestModel modified = new() { Id = 1, Name = null };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.ShouldBeEmpty();
    }

    [Fact]
    public void CreatePatch_ShouldHandleComplexTypeChange()
    {
        var original = new { A = new { B = 1 } };
        var modified = new { A = new { B = 2 } };

        JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe(ReplaceOperation);
        patch.Operations[0].path.ShouldBe("/A/B");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(2);
    }

    [Fact]
    public void FillPatchForObject_ShouldRecurse_WhenFloatTypeAndObjectTypeIsTrue()
    {
        // Arrange: Create nested objects where the property is a float and a nested object
        JObject originalNested = new()
        {
            ["Value"] = 1.23f
        };
        JObject modifiedNested = new()
        {
            ["Value"] = 4.56f
        };

        JObject original = new()
        {
            ["Nested"] = originalNested
        };
        JObject modified = new()
        {
            ["Nested"] = modifiedNested
        };

        JsonPatchDocument patch = new();

        // Act: Call FillPatchForObject directly via reflection since it's private
        MethodInfo? method = typeof(PatchCreator).GetMethod("FillPatchForObject", BindingFlags.NonPublic | BindingFlags.Static);
        method!.Invoke(null, new object[] { original, modified, patch, "/" });

        // Assert: Should have a replace operation for the nested float value
        patch.Operations.Count.ShouldBe(1);
        patch.Operations[0].op.ShouldBe("replace");
        patch.Operations[0].path.ShouldBe("/Nested/Value");
        patch.Operations[0].value.ShouldBeOfType<JValue>();
        ((JValue)patch.Operations[0].value).Value.ShouldBe(4.56f);
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
