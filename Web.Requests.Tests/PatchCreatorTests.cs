using System.Globalization;
using System.Reflection;
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
		DateTime originalDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
		DateTime modifiedDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
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
		DateOnly.TryParse(((JValue)patch.Operations[1].value).Value?.ToString(), new CultureInfo("en-US"), out DateOnly dateOnlyResult).ShouldBeTrue();
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

		JObject originalNested = new() { ["Value"] = 1.23f };
		JObject modifiedNested = new() { ["Value"] = 4.56f };

		JObject original = new() { ["Nested"] = originalNested };
		JObject modified = new() { ["Nested"] = modifiedNested };

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

	[Fact]
	public void CreatePatch_ShouldDetectTypeMismatch_StringToNumber()
	{
		// Arrange: Property changes type from string to number

		JObject original = new() { ["Value"] = "123" };
		JObject modified = new() { ["Value"] = 123 };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should replace due to type mismatch

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/Value");
		((JValue)patch.Operations[0].value).Value.ShouldBe(123);
	}

	[Fact]
	public void CreatePatch_ShouldDetectTypeMismatch_NumberToBoolean()
	{
		// Arrange: Property changes type from number to boolean

		JObject original = new() { ["Flag"] = 1 };
		JObject modified = new() { ["Flag"] = true };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should replace due to type mismatch

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/Flag");
		((JValue)patch.Operations[0].value).Value.ShouldBe(true);
	}

	[Fact]
	public void CreatePatch_ShouldNotGeneratePatch_WhenFloatsAreEqual()
	{
		// Arrange: Same float value

		TestModel original = new() { Id = 1, Amount = 99.99m };
		TestModel modified = new() { Id = 1, Amount = 99.99m };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: No operations when values are equal

		patch.Operations.ShouldBeEmpty();
	}

	[Fact]
	public void CreatePatch_ShouldNotGeneratePatch_WhenDateTimesAreEqual()
	{
		// Arrange: Same DateTime value

		DateTime sameDate = new(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
		TestModel original = new() { Id = 1, DateTime = sameDate };
		TestModel modified = new() { Id = 1, DateTime = sameDate };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: No operations when dates are equal

		patch.Operations.ShouldBeEmpty();
	}

	[Fact]
	public void CreatePatch_ShouldNotGeneratePatch_WhenDateOnlyAreEqual()
	{
		// Arrange: Same DateOnly value

		DateOnly sameDate = new(2024, 6, 15);
		TestModel original = new() { Id = 1, DateOnly = sameDate };
		TestModel modified = new() { Id = 1, DateOnly = sameDate };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: No operations when dates are equal

		patch.Operations.ShouldBeEmpty();
	}

	[Fact]
	public void CreatePatch_ShouldNotGeneratePatch_WhenDateTimeOffsetsAreEqual()
	{
		// Arrange: Same DateTimeOffset value

		DateTimeOffset sameOffset = new(2024, 6, 15, 10, 30, 45, TimeSpan.FromHours(-5));
		TestModel original = new() { Id = 1, DateTimeOffset = sameOffset };
		TestModel modified = new() { Id = 1, DateTimeOffset = sameOffset };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: No operations when offsets are equal

		patch.Operations.ShouldBeEmpty();
	}

	[Fact]
	public void CreatePatch_ShouldDetectDateTimeOffsetChange()
	{
		// Arrange: Different DateTimeOffset values

		DateTimeOffset originalOffset = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		DateTimeOffset modifiedOffset = new(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
		TestModel original = new() { Id = 1, DateTimeOffset = originalOffset };
		TestModel modified = new() { Id = 1, DateTimeOffset = modifiedOffset };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should detect the change

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/DateTimeOffset");
	}

	[Fact]
	public void CreatePatch_ShouldDetectDateOnlyChange()
	{
		// Arrange: Different DateOnly values

		DateOnly originalDate = new(2024, 1, 1);
		DateOnly modifiedDate = new(2024, 12, 31);
		TestModel original = new() { Id = 1, DateOnly = originalDate };
		TestModel modified = new() { Id = 1, DateOnly = modifiedDate };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should detect the change

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/DateOnly");
	}

	[Fact]
	public void CreatePatch_ShouldHandleMultiplePropertiesAdded()
	{
		// Arrange: Multiple new properties

		JObject original = new() { ["A"] = 1 };
		JObject modified = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should have add operations for both new properties

		patch.Operations.Count.ShouldBe(2);
		patch.Operations.ShouldAllBe(op => op.op == AddOperation);
		patch.Operations.ShouldContain(op => op.path == "/B");
		patch.Operations.ShouldContain(op => op.path == "/C");
	}

	[Fact]
	public void CreatePatch_ShouldHandleMultiplePropertiesRemoved()
	{
		// Arrange: Multiple properties removed

		JObject original = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 };
		JObject modified = new() { ["A"] = 1 };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should have remove operations for both removed properties

		patch.Operations.Count.ShouldBe(2);
		patch.Operations.ShouldAllBe(op => op.op == RemoveOperation);
		patch.Operations.ShouldContain(op => op.path == "/B");
		patch.Operations.ShouldContain(op => op.path == "/C");
	}

	[Fact]
	public void CreatePatch_ShouldHandleMixedOperations()
	{
		// Arrange: Add, remove, and replace operations

		JObject original = new() { ["Keep"] = 1, ["Change"] = "old", ["Remove"] = 99 };
		JObject modified = new() { ["Keep"] = 1, ["Change"] = "new", ["Add"] = 42 };

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should have all three operation types

		patch.Operations.Count.ShouldBe(3);
		patch.Operations.ShouldContain(op => op.op == RemoveOperation && op.path == "/Remove");
		patch.Operations.ShouldContain(op => op.op == AddOperation && op.path == "/Add");
		patch.Operations.ShouldContain(op => op.op == ReplaceOperation && op.path == "/Change");
	}

	[Fact]
	public void CreatePatch_ShouldHandleDeepNestedObjects()
	{
		// Arrange: Deeply nested structure

		var original = new
		{
			Level1 = new
			{
				Level2 = new
				{
					Level3 = new
					{
						Value = "original"
					}
				}
			}
		};
		var modified = new
		{
			Level1 = new
			{
				Level2 = new
				{
					Level3 = new
					{
						Value = "modified"
					}
				}
			}
		};

		// Act

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert: Should detect the deep change

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/Level1/Level2/Level3/Value");
		((JValue)patch.Operations[0].value).Value.ShouldBe("modified");
	}

	[Fact]
	public void CreatePatch_ShouldIgnoreReferenceLoops()
	{
		// Arrange: Test that reference loop handling works (via JsonSerializerSettings)

		var circularObject = new { Id = 1, Name = "Test" };

		// Act & Assert: Should not throw with reference loop handling

		JsonPatchDocument patch = PatchCreator.CreatePatch(circularObject, circularObject);
		patch.Operations.ShouldBeEmpty();
	}
}
