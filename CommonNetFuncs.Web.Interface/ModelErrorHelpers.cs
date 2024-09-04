using Microsoft.AspNetCore.Mvc.ModelBinding;
using static CommonNetFuncs.Core.Strings;

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
                string? errText = null;
                foreach (ModelError error in value.Errors)
                {
                    errText += error.ErrorMessage + "";
                }
                if (errText.Right(1) == ".")
                {
                    errText = errText![0..^1]; //Removes last character
                }
                errors.Add(modelStateKey, errText);
            }
        }
        errors.Add("", "Invalid model state");
        return errors;
    }
}
