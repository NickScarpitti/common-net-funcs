using CommonNetFuncs.Core;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.Web.Requests;

/// <summary>
/// Uses Newtonsoft.Json to create a JsonPatchDocument
/// </summary>
public static class PatchCreator
{
    private static readonly JsonSerializerSettings settings = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
    private static readonly JsonSerializer serializer = JsonSerializer.Create(settings);

    /// <summary>
    /// Converts two like models to JObjects and passes them into the FillPatchForObject method to create a JSON patch document
    /// From Source2
    /// </summary>
    /// <param name="originalObject">Object state being changed FROM</param>
    /// <param name="modifiedObject">Object state being changed TO</param>
    /// <param name="jsonSerializer">Custom Newtonsoft serializer to override default</param>
    /// <returns>JsonPatchDocument document of changes from originalObject to modifiedObject</returns>
    public static JsonPatchDocument CreatePatch(object originalObject, object modifiedObject, JsonSerializer? jsonSerializer = null)
    {
        JObject original = JObject.FromObject(originalObject, jsonSerializer ?? serializer);
        JObject modified = JObject.FromObject(modifiedObject, jsonSerializer ?? serializer);

        JsonPatchDocument patch = new();
        FillPatchForObject(original, modified, patch, "/");

        return patch;
    }

    /// <summary>
    /// Compares two JObjects together and populates a JsonPatchDocument with the differences
    /// From Source2
    /// </summary>
    /// <param name="orig">Original object to be compared to</param>
    /// <param name="mod">Modified version of the original object</param>
    /// <param name="patch">The json patch document to write the patch instructions to</param>
    /// <param name="path"></param>
    private static void FillPatchForObject(JObject orig, JObject mod, JsonPatchDocument patch, string path)
    {
        string[] origNames = orig.Properties().Select(x => x.Name).ToArray();
        string[] modNames = mod.Properties().Select(x => x.Name).ToArray();

        // Names removed in modified
        foreach (string? k in origNames.Except(modNames))
        {
            JProperty? prop = orig.Property(k);
            patch.Remove($"{path}{prop!.Name}");
        }

        // Names added in modified
        foreach (string? k in modNames.Except(origNames))
        {
            JProperty? prop = mod.Property(k);
            patch.Add($"{path}{prop!.Name}", prop.Value);
        }

        // Present in both
        foreach (string? k in origNames.Intersect(modNames))
        {
            JProperty? origProp = orig.Property(k);
            JProperty? modProp = mod.Property(k);

            if (origProp?.Value.Type != modProp?.Value.Type)
            {
                patch.Replace($"{path}{modProp?.Name}", modProp?.Value);
            }
            else if (origProp?.Value.Type == JTokenType.Float)
            {
                decimal? origDec = null;
                decimal? modDec = null;
                if (decimal.TryParse(origProp?.Value.ToString(Formatting.None), out decimal origDecimal))
                {
                    origDec = origDecimal;
                }
                if (decimal.TryParse(modProp?.Value.ToString(Formatting.None), out decimal modDecimal))
                {
                    modDec = modDecimal;
                }

                if (modDec != origDec)
                {
                    if (origProp?.Value.Type == JTokenType.Object)
                    {
                        // Recurse into objects
                        FillPatchForObject(origProp.Value as JObject ?? [], modProp?.Value as JObject ?? [], patch, $"{path}{modProp?.Name}/");
                    }
                    else
                    {
                        // Replace values directly
                        patch.Replace($"{path}{modProp?.Name}", modProp?.Value);
                    }
                }
            }
            else if (!(origProp?.Value.ToString(Formatting.None)).StrComp(modProp?.Value.ToString(Formatting.None)) && origProp?.Value.Type != JTokenType.Date)
            {
                if (origProp?.Value.Type == JTokenType.Object)
                {
                    // Recurse into objects
                    FillPatchForObject(origProp.Value as JObject ?? [], modProp?.Value as JObject ?? [], patch, $"{path}{modProp?.Name}/");
                }
                else
                {
                    // Replace values directly
                    patch.Replace($"{path}{modProp?.Name}", modProp?.Value);
                }
            }
            else if (origProp?.Value.Type == JTokenType.Date && modProp?.Value.Type == JTokenType.Date)
            {
                string originalDts = origProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");
                string modDts = modProp.Value.ToString(Formatting.None).Replace(@"""", "").Replace(@"\", "");

                bool originalSucceed = DateTime.TryParse(originalDts, out DateTime originalDate);
                bool modSucceed = DateTime.TryParse(modDts, out DateTime modDate);

                if (modSucceed && (originalSucceed ? originalDate : null) != modDate)
                {
                    // Replace values directly
                    patch.Replace($"{path}{modProp.Name}", modProp.Value);
                }
            }
        }
    }
}
