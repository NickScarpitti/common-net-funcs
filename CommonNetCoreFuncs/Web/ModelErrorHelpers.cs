using CommonNetCoreFuncs.Tools;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CommonNetCoreFuncs.Web;

/// <summary>
/// For use with ASP.NET Core ModelStateDictionary
/// </summary>
public static class ModelErrorHelpers
{
    /// <summary>
    /// Convert ModelStateDictionary used by ASP.NET Core into a standard dictionary
    /// </summary>
    /// <param name="modelState"></param>
    /// <returns></returns>
    public static Dictionary<string, string?> ParseModelStateErrors(ModelStateDictionary modelState)
    {
        Dictionary<string, string?> errors = new();
        foreach (string modelStateKey in modelState.Keys)
        {
            var value = modelState[modelStateKey];
            if (value!.Errors.Count > 0)
            {
                string? errText = null;
                foreach (var error in value.Errors)
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
