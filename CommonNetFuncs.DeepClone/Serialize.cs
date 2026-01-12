//using System.Diagnostics.CodeAnalysis;
//using System.Text.Json;

//namespace CommonNetFuncs.DeepClone;

//public static class Serialize
//{
//  /// <summary>
//  /// Deep clone a class (cloned object doesn't retain memory references) using serialization (slowest)
//  /// </summary>
//  /// <typeparam name="TObj">Type of object to clone.</typeparam>
//  /// <param name="original">Object to clone</param>
//  /// <returns>An exact copy of the original object that is distinct from the original object.</returns>
//  [return: NotNullIfNotNull(nameof(original))]
//  [Obsolete("Please use CommonNetFuncs.DeepClone.ExpressionTrees.DeepClone instead")]
//  public static TObj? DeepCloneS<TObj>(this TObj? original) where TObj : class
//  {
//    if (original == null)
//    {
//      return null;
//    }
//    string serialized = JsonSerializer.Serialize(original);
//    return JsonSerializer.Deserialize<TObj>(serialized) ?? throw new JsonException("Unable to deserialize cloned object");
//  }
//}
