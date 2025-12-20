using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommonNetFuncs.Web.Interface;

/// <summary>
/// For use with ASP.NET Core ModelStateDictionary
/// </summary>
public static class ModelErrorHelpers
{
	/// <summary>
	/// Convert ModelStateDictionary used by ASP.NET Core into a standard dictionary
	/// </summary>
	/// <param name="modelState">ASP.NET ModelStateDictionary object to parse</param>
	public static Dictionary<string, string?> ParseModelStateErrors(ModelStateDictionary modelState)
	{
		Dictionary<string, string?> errors = [];
		foreach (string modelStateKey in modelState.Keys)
		{
			ModelStateEntry? value = modelState[modelStateKey];
			if (value?.Errors.Count > 0)
			{
				StringBuilder stringBuilder = new();
				foreach (ModelError error in value.Errors)
				{
					stringBuilder.Append($"{error.ErrorMessage} ");
				}

				if (stringBuilder[^1] == '.')
				{
					stringBuilder.Remove(stringBuilder.Length - 1, 1);
				}
				errors.Add(modelStateKey, stringBuilder.ToString());
			}
		}
		errors.Add(string.Empty, "Invalid model state");
		return errors;
	}
}
