using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CommonNetFuncs.DeepClone;

public static class Serialize
{
  /// <summary>
    /// Deep clone a class (cloned object doesn't retain memory references) using serialization (slowest)
    /// </summary>
    /// <typeparam name="T">Type of object to clone.</typeparam>
    /// <param name="original">Object to clone</param>
    /// <returns>An exact copy of the original object that is distinct from the original object.</returns>
  [return: NotNullIfNotNull(nameof(original))]
  [Obsolete("Please use CommonNetFuncs.DeepClone.ExpressionTrees.DeepClone instead")]
  public static T? DeepCloneS<T>(this T? original) where T : class
  {
    if (original == null)
    {
      return null;
    }
    string serialized = JsonSerializer.Serialize(original);
    return JsonSerializer.Deserialize<T>(serialized) ?? throw new JsonException("Unable to deserialize cloned object");
  }
}
