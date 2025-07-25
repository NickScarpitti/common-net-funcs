﻿using Microsoft.AspNetCore.JsonPatch;
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
    /// <param name="originalObject">Original object to be compared to</param>
    /// <param name="modObject">Modified version of the original object</param>
    /// <param name="patch">The json patch document to write the patch instructions to</param>
    /// <param name="path">Property path</param>
    private static void FillPatchForObject(JObject originalObject, JObject modObject, JsonPatchDocument patch, string path)
    {
        Dictionary<string, JProperty> origProps = originalObject.Properties().ToDictionary(p => p.Name, p => p);
        Dictionary<string, JProperty> modProps = modObject.Properties().ToDictionary(p => p.Name, p => p);

        HashSet<string> origNames = new(origProps.Keys);
        HashSet<string> modNames = new(modProps.Keys);

        // Names removed in modified
        foreach (string? k in origNames.Except(modNames))
        {
            JProperty? prop = originalObject.Property(k);
            patch.Remove($"{path}{prop!.Name}");
        }

        // Names added in modified
        foreach (string? k in modNames.Except(origNames))
        {
            JProperty? prop = modObject.Property(k);
            patch.Add($"{path}{prop!.Name}", prop.Value);
        }

        // Present in both
        foreach (string? k in origNames.Intersect(modNames))
        {
            JProperty origProp = origProps[k];
            JProperty modProp = modProps[k];

            JTokenType? origType = origProp.Value.Type;
            JTokenType? modType = modProp.Value.Type;
            //JProperty? origProp = originalObject.Property(k); // Slightly slower than using the dictionary
            //JProperty? modProp = modObject.Property(k); // Slightly slower than using the dictionary

            if (origType != modType)
            {
                patch.Replace($"{path}{modProp.Name}", modProp.Value);
                continue;
            }

            if (origType == JTokenType.Float)
            {
                decimal origDec = origProp.Value.Value<decimal>();
                decimal modDec = modProp.Value.Value<decimal>();
                if (modDec != origDec)
                {
                    // Replace values directly
                    patch.Replace($"{path}{modProp.Name}", modProp.Value);
                }
                continue;
            }

            if (origType == JTokenType.Object)
            {
                FillPatchForObject((JObject)origProp.Value, (JObject)modProp.Value, patch, $"{path}{modProp.Name}/");
                continue;
            }

            string? origStr = origProp.Value.ToString(Formatting.None);
            string? modStr = modProp.Value.ToString(Formatting.None);

            if (!origStr.StrComp(modStr) && origType != JTokenType.Date)
            {
                patch.Replace($"{path}{modProp.Name}", modProp.Value);
                continue;
            }

            if (origType == JTokenType.Date)
            {
                DateTime origDate = origProp.Value.Value<DateTime>();
                DateTime modDate = modProp.Value.Value<DateTime>();
                if (origDate != modDate)
                {
                    patch.Replace($"{path}{modProp.Name}", modProp.Value);
                }
            }
        }
    }
}
