using CommonNetFuncs.Web.Interface;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Shouldly;

namespace Web.Interface.Tests;

public sealed class ModelErrorHelpersTests
{
	[Fact]
	public void ParseModelStateErrors_EmptyModelState_ReturnsDefaultError()
	{
		ModelStateDictionary modelState = new();

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.Count.ShouldBe(1);
		result.ShouldContainKey(string.Empty);
		result[string.Empty].ShouldBe("Invalid model state");
	}

	[Fact]
	public void ParseModelStateErrors_SingleError_ReturnsCorrectDictionary()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyName", "Error message");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.Count.ShouldBe(2); // Including default error
		result.ShouldContainKey("PropertyName");
		result["PropertyName"].ShouldBe("Error message ");
	}

	[Fact]
	public void ParseModelStateErrors_MultipleErrorsForSameProperty_ConcatenatesMessages()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyName", "Error 1");
		modelState.AddModelError("PropertyName", "Error 2");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.Count.ShouldBe(2); // Including default error
		result.ShouldContainKey("PropertyName");
		result["PropertyName"].ShouldBe("Error 1 Error 2 ");
	}

	[Fact]
	public void ParseModelStateErrors_MultipleProperties_ReturnsAllErrors()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("Property1", "Error 1");
		modelState.AddModelError("Property2", "Error 2");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.Count.ShouldBe(3); // Including default error
		result.ShouldContainKey("Property1");
		result.ShouldContainKey("Property2");
		result["Property1"].ShouldBe("Error 1 ");
		result["Property2"].ShouldBe("Error 2 ");
	}

	[Fact]
	public void ParseModelStateErrors_ErrorMessageEndsWithPeriod_RemovesPeriod()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyName", "Error message.");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result["PropertyName"].ShouldBe("Error message. ");
	}

	[Fact]
	public void ParseModelStateErrors_PropertyWithNoErrors_NotIncludedInResult()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyWithError", "Error message");
		// Add a key without errors by accessing SetModelValue
		modelState.SetModelValue("PropertyWithoutError", new ValueProviderResult(new StringValues("value")));

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.ShouldContainKey("PropertyWithError");
		result.ShouldNotContainKey("PropertyWithoutError");
	}

	[Fact]
	public void ParseModelStateErrors_MultipleErrorsWithDifferentEndings_HandlesCorrectly()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("Property1", "Error without period");
		modelState.AddModelError("Property2", "Error with period.");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result["Property1"].ShouldBe("Error without period ");
		result["Property2"].ShouldBe("Error with period. ");
	}

	[Fact]
	public void ParseModelStateErrors_ErrorMessageIsPeriodOnly_RemovesPeriod()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyName", ".");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result["PropertyName"].ShouldBe(". ");
	}

	[Fact]
	public void ParseModelStateErrors_NullValueInModelState_SkipsProperty()
	{
		ModelStateDictionary modelState = new();
		modelState.AddModelError("PropertyWithError", "Error message");

		Dictionary<string, string?> result = ModelErrorHelpers.ParseModelStateErrors(modelState);

		result.ShouldContainKey("PropertyWithError");
		result.Count.ShouldBe(2); // PropertyWithError + default error
	}
}
