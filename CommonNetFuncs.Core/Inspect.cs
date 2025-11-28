using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using FastExpressionCompiler;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static class Inspect
{
  /// <summary>
  /// Gets the default value of the provided type
  /// </summary>
  /// <param name="type">Type to get the default value of</param>
  /// <returns>The default value of the provided type</returns>
  public static object? GetDefaultValue(this Type type)
  {
    return type.IsValueType ? RuntimeHelpers.GetUninitializedObject(type) : null;
  }

  /// <summary>
  /// Get the number of properties in a class that are set to their default value
  /// </summary>
  /// <param name="obj">Object to count default properties in</param>
  /// <returns>Number of properties in a class that are set to their default value</returns>
  public static int CountDefaultProps<T>(this T obj) where T : class
  {
    return GetOrAddPropertiesFromReflectionCache(typeof(T)).Count(x => x.CanWrite && Equals(x.GetValue(obj), x.PropertyType.GetDefaultValue()));
  }

  /// <summary>
  /// Returns whether a Type has the specified attribute or not
  /// </summary>
  /// <param name="type">The type to check for the specified attribute</param>
  /// <param name="attributeName">The name of the attribute you are checking the provided type for</param>
  /// <returns><see langword="true"/> if the object has the specified attribute, otherwise false</returns>
  public static bool ObjectHasAttribute(this Type type, string attributeName)
  {
    bool hasAttribute = false;
    foreach (object item in type.GetCustomAttributes(true))
    {
      object? typeIdObject = item.GetType().GetProperty("TypeId")?.GetValue(item, null);

      if (typeIdObject != null)
      {
        string? attrName = typeIdObject.GetType().GetProperty("Name")?.GetValue(typeIdObject, null)?.ToString();
        if (attrName?.StrEq(attributeName) == true)
        {
          hasAttribute = true;
          break;
        }
      }
    }
    return hasAttribute;
  }

  ///// <summary>
  ///// Compares two like objects against each other to check to see if they contain the same values
  ///// </summary>
  ///// <param name="obj1">First object to compare for value equality</param>
  ///// <param name="obj2">Second object to compare for value equality</param>
  ///// <returns><see langword="true"/> if the two objects have the same value for all elements, otherwise false</returns>
  //[Obsolete("Please use IsEqual method instead")]
  //public static bool IsEqualR(this object? obj1, object? obj2)
  //{
  //	return obj1.IsEqualR(obj2, null);
  //}

  ///// <summary>
  ///// Compare two class objects for value equality
  ///// </summary>
  ///// <param name="obj1">First object to compare for value equality</param>
  ///// <param name="obj2">Second object to compare for value equality</param>
  ///// <param name="exemptProps">Names of properties to not include in the matching check</param>
  ///// <returns><see langword="true"/> if both objects contain identical values for all properties except for the ones identified by exemptProps, otherwise false</returns>
  //[Obsolete("Please use IsEqual method instead")]
  //public static bool IsEqualR(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null)
  //{
  //	// They're both null.
  //	if ((obj1 == null) && (obj2 == null))
  //	{
  //		return true;
  //	}

  //	// One is null, so they can't be the same.
  //	if ((obj1 == null) || (obj2 == null))
  //	{
  //		return false;
  //	}

  //	// How can they be the same if they're different types?
  //	if (obj1.GetType() != obj1.GetType())
  //	{
  //		return false;
  //	}

  //	IEnumerable<PropertyInfo> props = GetOrAddPropertiesFromReflectionCache(obj1.GetType());
  //	if (exemptProps?.Any() == true)
  //	{
  //		props = props.Where(x => exemptProps?.Contains(x.Name) != true);
  //	}

  //	foreach (PropertyInfo prop in props)
  //	{
  //		object aPropValue = prop.GetValue(obj1) ?? string.Empty;
  //		object bPropValue = prop.GetValue(obj2) ?? string.Empty;

  //		bool aIsNumeric = aPropValue.IsNumeric();
  //		bool bIsNumeric = bPropValue.IsNumeric();

  //		try
  //		{
  //			// This will prevent issues with numbers with varying decimal places from being counted as a difference
  //			if ((aIsNumeric && bIsNumeric && (decimal.Parse(aPropValue.ToString()!) != decimal.Parse(bPropValue.ToString()!))) ||
  //									(!(aIsNumeric && bIsNumeric) && !aPropValue.ToString().StrComp(bPropValue.ToString())))
  //			{
  //				return false;
  //			}
  //		}
  //		catch (Exception ex)
  //		{
  //			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
  //			return false;
  //		}
  //	}
  //	return true;
  //}

  // This class is used to track object pairs being compared
  private sealed class ComparisonContext
  {
    private readonly HashSet<(object, object)> _comparingPairs = [];

    public bool TryAddPair(object obj1, object obj2)
    {
      return _comparingPairs.Add((obj1, obj2));
    }

    public void RemovePair(object obj1, object obj2)
    {
      _comparingPairs.Remove((obj1, obj2));
    }
  }

  private static readonly AsyncLocal<ComparisonContext?> CurrentContext = new();
  private static readonly ConcurrentDictionary<Tuple<Type, bool, bool>, Func<object, object, IEnumerable<string>, bool>> CompareDelegates = [];

  /// <summary>
  /// Compare two class objects for value equality
  /// </summary>
  /// <param name="obj1">First object to compare for value equality</param>
  /// <param name="obj2">Second object to compare for value equality</param>
  /// <param name="exemptProps">Optional: Names of properties to not include in the matching check</param>
  /// <param name="ignoreStringCase">Optional: If <see langword="true"/>, will ignore case when comparing string properties between obj1 and obj2, otherwise will use case sensitive comparison.</param>
  /// <param name="recursive">Optional: If <see langword="true"/>, will recursively compare </param>
  /// <returns><see langword="true"/> if the two objects are equal by values, otherwise false</returns>
  public static bool IsEqual(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null, bool ignoreStringCase = false, bool recursive = true)
  {
    // Initialize context if this is the top-level call
    bool isTopLevel = CurrentContext.Value == null;
    if (isTopLevel)
    {
      CurrentContext.Value = new ComparisonContext();
    }

    try
    {
      // Null checks (same as before)
      if ((obj1 == null) && (obj2 == null))
      {
        return true;
      }

      if ((obj1 == null) || (obj2 == null))
      {
        return false;
      }

      Type type = obj1.GetType();

      if (type != obj2.GetType())
      {
        return false;
      }

      // If we're already comparing these objects, return true to break the cycle
      if (!CurrentContext.Value!.TryAddPair(obj1, obj2))
      {
        return true;
      }

      exemptProps ??= [];
      Tuple<Type, bool, bool> key = Tuple.Create(type, ignoreStringCase, recursive);
      if (!CompareDelegates.TryGetValue(key, out Func<object, object, IEnumerable<string>, bool>? compareDelegate))
      {
        compareDelegate = CreateCompareDelegate(type, ignoreStringCase, recursive);
        CompareDelegates[key] = compareDelegate;
      }

      return compareDelegate(obj1, obj2, exemptProps);
    }
    finally
    {
      // Clean up
      if ((obj1 != null) && (obj2 != null))
      {
        CurrentContext.Value?.RemovePair(obj1, obj2);
      }

      // Clear context if this was the top-level call
      if (isTopLevel)
      {
        CurrentContext.Value = null;
      }
    }
  }

  /// <summary>
  /// Creates a delegate for comparing two objects of the specified type for value equality.
  /// </summary>
  /// <param name="type">Type of the object to be compared</param>
  /// <param name="ignoreStringCase">If <see langword="true"/>, will ignore case when comparing string properties for value equlity</param>
  /// <param name="recursive">If <see langword="true"/>, will recursively compare properties of complex types</param>
  /// <returns>A delegate for comparing two objects of the specified type for value equality</returns>
  private static Func<object, object, IEnumerable<string>, bool> CreateCompareDelegate(Type type, bool ignoreStringCase, bool recursive)
  {
    ParameterExpression obj1Param = Expression.Parameter(typeof(object), "obj1");
    ParameterExpression obj2Param = Expression.Parameter(typeof(object), "obj2");
    ParameterExpression exemptPropsParam = Expression.Parameter(typeof(IEnumerable<string>), "exemptProps");

    UnaryExpression typedObj1 = Expression.Convert(obj1Param, type);
    UnaryExpression typedObj2 = Expression.Convert(obj2Param, type);

    IEnumerable<PropertyInfo> properties = GetOrAddPropertiesFromReflectionCache(type).Where(x => x.CanRead && (x.GetIndexParameters().Length == 0));

    List<Expression> comparisons = [];

    foreach (PropertyInfo prop in properties)
    {
      MethodCallExpression propExemptCheck = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(string)], exemptPropsParam, Expression.Constant(prop.Name));

      MemberExpression value1 = Expression.Property(typedObj1, prop);
      MemberExpression value2 = Expression.Property(typedObj2, prop);

      Expression comparison;

      if (prop.PropertyType == typeof(string))
      {
        if (ignoreStringCase)
        {
          MethodInfo? equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;
          comparison = Expression.Call(equalsMethod, value1, value2, Expression.Constant(StringComparison.OrdinalIgnoreCase));
        }
        else
        {
          MethodInfo? equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!;
          comparison = Expression.Call(equalsMethod, value1, value2);
        }
      }
      else if (prop.PropertyType.IsValueType)
      {
        comparison = Expression.Equal(value1, value2);
      }
      else if (typeof(IComparable).IsAssignableFrom(prop.PropertyType))
      {
        comparison = Expression.Equal(Expression.Call(value1, "CompareTo", null, value2), Expression.Constant(0));
      }
      else if (recursive && !prop.PropertyType.IsValueType && (prop.PropertyType != typeof(string)))
      {
        MethodInfo? isEqualMethod = typeof(Inspect).GetMethod(nameof(IsEqual), [typeof(object), typeof(object), typeof(IEnumerable<string>), typeof(bool), typeof(bool)])!;
        comparison = Expression.Call(isEqualMethod, value1, value2, Expression.Constant(null, typeof(IEnumerable<string>)), Expression.Constant(ignoreStringCase), Expression.Constant(recursive));
      }
      else
      {
        // comparison = Expression.Equal(value1, value2);
        comparison = Expression.Constant(true);
      }

      comparisons.Add(Expression.Condition(propExemptCheck, Expression.Constant(true), comparison));
    }

    Expression andAlsoExpression = comparisons.Aggregate(Expression.And);

    Expression<Func<object, object, IEnumerable<string>, bool>> lambda = Expression.Lambda<Func<object, object, IEnumerable<string>, bool>>(
            andAlsoExpression, obj1Param, obj2Param, exemptPropsParam);

    return lambda.CompileFast();
  }

  /// <summary>
  /// Gets a hash string representing the object's value, using the specified algorithm (default MD5). Order of collection elements does not affect the hash.
  /// </summary>
  /// <param name="obj">Object to get hash value from</param>
  /// <param name="hashAlgorithm">Optional: Hash algorithm to use. Default is MD5.</param>
  /// <returns>Hash string representing the object's value</returns>
  public static string GetHashForObject<T>(this T obj, EHashAlgorithm hashAlgorithm = EHashAlgorithm.MD5) where T : class?
  {
    if (obj == null)
    {
      return "null";
    }

    HashAlgorithm algorithm = hashAlgorithm switch
    {
      EHashAlgorithm.SHA1 => SHA1.Create(),
      EHashAlgorithm.MD5 => MD5.Create(),
      EHashAlgorithm.SHA256 => SHA256.Create(),
      EHashAlgorithm.SHA384 => SHA384.Create(),
      _ => SHA512.Create()
    };

    IOrderedEnumerable<PropertyInfo> properties = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead).OrderBy(x => x.Name);

    using MemoryStream ms = new();
    using BinaryWriter writer = new(ms);
    foreach (PropertyInfo property in properties)
    {
      object? value = property.GetValue(obj);
      if (value != null)
      {
        WriteValue(writer, value);
      }
    }

    return Convert.ToHexStringLower(algorithm.ComputeHash(ms.ToArray()));
  }

  /// <summary>
  /// Gets a hash string representing the object's value asynchronously, using the specified algorithm (default MD5). Order of collection elements does not affect the hash.
  /// </summary>
  /// <param name="obj">Object to get hash value from</param>
  /// <param name="hashAlgorithm">Optional: Hash algorithm to use. Default is MD5.</param>
  /// <returns>Hash string representing the object's value</returns>
  public static async Task<string> GetHashForObjectAsync<T>(this T obj, EHashAlgorithm hashAlgorithm = EHashAlgorithm.MD5) where T : class
  {
    if (obj == null)
    {
      return "null";
    }

    HashAlgorithm algorithm = hashAlgorithm switch
    {
      EHashAlgorithm.SHA1 => SHA1.Create(),
      EHashAlgorithm.MD5 => MD5.Create(),
      EHashAlgorithm.SHA256 => SHA256.Create(),
      EHashAlgorithm.SHA384 => SHA384.Create(),
      _ => SHA512.Create()
    };

    IOrderedEnumerable<PropertyInfo> properties = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead).OrderBy(x => x.Name);

    await using MemoryStream ms = new();
    await using BinaryWriter writer = new(ms);
    foreach (PropertyInfo property in properties)
    {
      object? value = property.GetValue(obj);
      if (value != null)
      {
        await WriteValueAsync(writer, value).ConfigureAwait(false);
      }
    }
    await ms.FlushAsync().ConfigureAwait(false);
    ms.Position = 0; // Reset stream position for reading
    return Convert.ToHexStringLower(await algorithm.ComputeHashAsync(ms).ConfigureAwait(false));
  }

  /// <summary>
  /// Writes the value to the binary writer.
  /// </summary>
  /// <param name="writer">Binary writer to write the value to</param>
  /// <param name="value">Value to write to the binary writer</param>
  private static void WriteValue(BinaryWriter writer, object value)
  {
    if (value == null)
    {
      writer.Write("null");
      return;
    }

    Type type = value.GetType();

    // Handle collections
    if (value is IEnumerable enumerable and not string)
    {
      // Convert collection to list of sorted hashes
      List<string> itemHashes = [];
      foreach (object item in enumerable)
      {
        using MemoryStream itemMs = new();
        using BinaryWriter itemWriter = new(itemMs);
        WriteValue(itemWriter, item);
        itemHashes.Add(BitConverter.ToString(MD5.HashData(itemMs.ToArray())));
      }

      // Sort the hashes to ensure order independence
      itemHashes.Sort();

      // Write the sorted collection
      writer.Write("[");
      foreach (string itemHash in itemHashes)
      {
        writer.Write(itemHash);
        writer.Write(",");
      }
      writer.Write("]");
      return;
    }

    // Handle primitive types and strings
    if (type.IsPrimitive || (value is string) || (value is decimal))
    {
      writer.Write(value.ToString()!);
      return;
    }

    // Handle complex objects recursively
    IOrderedEnumerable<PropertyInfo> properties = GetOrAddPropertiesFromReflectionCache(type).Where(x => x.CanRead).OrderBy(x => x.Name);

    writer.Write("{");
    foreach (PropertyInfo property in properties)
    {
      writer.Write(property.Name);
      writer.Write(":");
      WriteValue(writer, property.GetValue(value)!);
      writer.Write(",");
    }
    writer.Write("}");
  }

  /// <summary>
  /// Writes the value to the binary writer.
  /// </summary>
  /// <param name="writer">Binary writer to write the value to</param>
  /// <param name="value">Value to write to the binary writer</param>
  private static async Task WriteValueAsync(BinaryWriter writer, object value)
  {
    if (value == null)
    {
      writer.Write("null");
      return;
    }

    Type type = value.GetType();

    // Handle collections
    if (value is IEnumerable enumerable and not string)
    {
      // Convert collection to list of sorted hashes
      List<string> itemHashes = [];
      foreach (object item in enumerable)
      {
        await using MemoryStream itemMs = new();
        await using BinaryWriter itemWriter = new(itemMs);
        await WriteValueAsync(itemWriter, item).ConfigureAwait(false);
        itemHashes.Add(BitConverter.ToString(await MD5.HashDataAsync(itemMs).ConfigureAwait(false)));
      }

      // Sort the hashes to ensure order independence
      itemHashes.Sort();

      // Write the sorted collection
      writer.Write("[");
      foreach (string itemHash in itemHashes)
      {
        writer.Write(itemHash);
        writer.Write(",");
      }
      writer.Write("]");
      return;
    }

    // Handle primitive types and strings
    if (type.IsPrimitive || (value is string) || (value is decimal))
    {
      writer.Write(value.ToString()!);
      return;
    }

    // Handle complex objects recursively
    IOrderedEnumerable<PropertyInfo> properties = GetOrAddPropertiesFromReflectionCache(type).Where(x => x.CanRead).OrderBy(x => x.Name);

    writer.Write("{");
    foreach (PropertyInfo property in properties)
    {
      writer.Write(property.Name);
      writer.Write(":");
      await WriteValueAsync(writer, property.GetValue(value)!).ConfigureAwait(false);
      writer.Write(",");
    }
    writer.Write("}");
  }
}
