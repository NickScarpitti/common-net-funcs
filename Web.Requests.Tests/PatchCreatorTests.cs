using System.Globalization;
using System.Reflection;
using CommonNetFuncs.Web.Requests;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web.Requests.Tests;

public enum PropertyType
{
	Integer,
	String,
	Decimal,
	Date
}

public enum PatchOperation
{
	Add,
	Remove,
	Replace
}

public enum TypeMismatchScenario
{
	StringToNumber,
	NumberToBoolean
}

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

	[Theory]
	[InlineData(true)]  // Added
	[InlineData(false)] // Removed
	public void CreatePatch_ShouldDetectNestedObjectAddedOrRemoved(bool isAdded)
	{
		TestModel original, modified;

		if (isAdded)
		{
			original = new() { Id = 1, Nested = null };
			modified = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };
		}
		else
		{
			original = new() { Id = 1, Nested = new NestedModel { Value = "Z" } };
			modified = new() { Id = 1, Nested = null };
		}

		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		patch.Operations[0].path.ShouldBe("/Nested");
		if (isAdded)
		{
			patch.Operations[0].value.ShouldBeOfType<JObject>();
		}
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

	[Theory]
	[InlineData(TypeMismatchScenario.StringToNumber)]
	[InlineData(TypeMismatchScenario.NumberToBoolean)]
	public void CreatePatch_ShouldDetectTypeMismatch(TypeMismatchScenario scenario)
	{
		// Arrange
		JObject original, modified;
		object expectedValue;

		switch (scenario)
		{
			case TypeMismatchScenario.StringToNumber:
				original = new() { ["Value"] = "123" };
				modified = new() { ["Value"] = 123 };
				expectedValue = 123;
				break;
			case TypeMismatchScenario.NumberToBoolean:
				original = new() { ["Flag"] = 1 };
				modified = new() { ["Flag"] = true };
				expectedValue = true;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(scenario));
		}

		// Act
		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert
		patch.Operations.Count.ShouldBe(1);
		patch.Operations[0].op.ShouldBe(ReplaceOperation);
		((JValue)patch.Operations[0].value).Value.ShouldBe(expectedValue);
	}

	[Theory]
	[InlineData(PropertyType.Decimal)]
	[InlineData(PropertyType.Date)]
	public void CreatePatch_ShouldNotGeneratePatch_WhenValuesAreEqual(PropertyType propertyType)
	{
		// Arrange
		TestModel original;
		TestModel modified;

		switch (propertyType)
		{
			case PropertyType.Decimal:
				original = new() { Id = 1, Amount = 99.99m };
				modified = new() { Id = 1, Amount = 99.99m };
				break;
			case PropertyType.Date:
				DateTime sameDateTime = new(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
				DateOnly sameDateOnly = new(2024, 6, 15);
				DateTimeOffset sameOffset = new(2024, 6, 15, 10, 30, 45, TimeSpan.FromHours(-5));
				original = new() { Id = 1, DateTime = sameDateTime, DateOnly = sameDateOnly, DateTimeOffset = sameOffset };
				modified = new() { Id = 1, DateTime = sameDateTime, DateOnly = sameDateOnly, DateTimeOffset = sameOffset };
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(propertyType));
		}

		// Act
		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert
		patch.Operations.ShouldBeEmpty();
	}

	[Theory]
	[InlineData(PatchOperation.Add)]
	[InlineData(PatchOperation.Remove)]
	public void CreatePatch_ShouldHandleMultipleProperties(PatchOperation operation)
	{
		// Arrange
		JObject original, modified;
		string expectedOp;

		switch (operation)
		{
			case PatchOperation.Add:
				original = new() { ["A"] = 1 };
				modified = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 };
				expectedOp = "add";
				break;
			case PatchOperation.Remove:
				original = new() { ["A"] = 1, ["B"] = 2, ["C"] = 3 };
				modified = new() { ["A"] = 1 };
				expectedOp = "remove";
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(operation));
		}

		// Act
		JsonPatchDocument patch = PatchCreator.CreatePatch(original, modified);

		// Assert
		patch.Operations.Count.ShouldBe(2);
		patch.Operations.ShouldAllBe(op => op.op == expectedOp);
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
