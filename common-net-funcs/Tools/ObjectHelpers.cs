using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NLog;

namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper methods for complex classes and lists
/// </summary>
public static class ObjectHelpers
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of source object</typeparam>
    /// <typeparam name="TU">Type of destination object</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    /// <param name="dest">Object to copy common properties to</param>
    public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
    {
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(TU).GetProperties().Where(x => x.CanWrite);

        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destProps.Any(x => x.Name == sourceProp.Name))
            {
                PropertyInfo? p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                p?.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
    }

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of object being copied</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    /// <param name="dest">New class of desired output type</param>
    public static T CopyPropertiesToNew<T>(this T source, T dest)
    {
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(T).GetProperties().Where(x => x.CanWrite);

        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destProps.Any(x => x.Name == sourceProp.Name))
            {
                PropertyInfo? p = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                p?.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
        return dest;
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq
    /// </summary>
    /// <typeparam name="T">Type of object having values set</typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod)
    {
        foreach (T item in items)
        {
            updateMethod(item);
        }
        return items;
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq using a Parallel.ForEach loop
    /// </summary>
    /// <typeparam name="T">Type of object having values set</typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <param name="maxDegreeOfParallelism">Integer setting the max number of parallel operations allowed. Default of -1 allows maximum possible.</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValueParallel<T>(this IEnumerable<T> items, Action<T> updateMethod, int maxDegreeOfParallelism = -1)
    {
        ConcurrentBag<T> concurrentBag = new(items);
        Parallel.ForEach(concurrentBag, new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item => updateMethod(item));
        return concurrentBag;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object
    /// </summary>
    /// <typeparam name="T">Type of object to trim strings in</typeparam>
    /// <param name="obj">Object containing string properties to be trimmed</param>
    public static T TrimObjectStrings<T>(this T obj)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string?)prop.GetValue(obj);
                    if (!value.IsNullOrEmpty())
                    {
                        prop.SetValue(obj, value.TrimFull());
                    }
                }
            }
        }
        return obj;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object with the option to also trim them
    /// </summary>
    /// <typeparam name="T">Type of object to normalize strings in</typeparam>
    /// <param name="obj">Object containing string properties to be normalized</param>
    public static T NormalizeObjectStrings<T>(this T obj, bool enableTrim = true, NormalizationForm normalizationForm = NormalizationForm.FormKD)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string?)prop.GetValue(obj);
                    if (!value.IsNullOrEmpty())
                    {
                        if (enableTrim)
                        {
                            prop.SetValue(obj, value.TrimFull().Normalize(normalizationForm));
                        }
                        else
                        {
                            prop.SetValue(obj, value.Normalize(normalizationForm));
                        }
                    }
                }
            }
        }
        return obj;
    }

    /// <summary>
    /// Adds AddRange functionality to ConcurrentBag similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T">Type of object being added</typeparam>
    /// <param name="concurrentBag">ConcurrentBag to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    /// <param name="parallelOptions">ParallelOptions for Parallel.ForEach</param>
    public static void AddRangeParallel<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, ParallelOptions? parallelOptions = null)
    {
        Parallel.ForEach(toAdd.Where(x => x != null), parallelOptions ?? new(), item => concurrentBag.Add(item!));
    }

    /// <summary>
    /// Adds AddRange functionality to ConcurrentBag similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T">Type of object being added</typeparam>
    /// <param name="concurrentBag">ConcurrentBag to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    public static void AddRange<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd)
    {
        foreach (T? item in toAdd.Where(x => x != null))
        {
            concurrentBag.Add(item!);
        }
    }

    /// <summary>
    /// Create a single item list from an object
    /// </summary>
    /// <typeparam name="T">Type to use in list</typeparam>
    /// <param name="obj">Object to turn into a single item list</param>
    public static List<T> SingleToList<T>(this T obj)
    {
        return [obj];
    }

    /// <summary>
    /// Select object from a collection by matching all non-null fields to an object of the same time comprising the collection
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="queryable">Queryable collection to select from</param>
    /// <param name="partialObject">Object with fields to match with objects in the queryable collection</param>
    /// <returns>First object that matches all non-null fields in partialObject</returns>
    public static T? GetObjectByPartial<T>(this IQueryable<T> queryable, T partialObject) where T : class
    {
        // Get the properties of the object using reflection
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Build the expression tree for the conditions
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? conditions = null;

        foreach (PropertyInfo property in properties)
        {
            // Get the value of the property from the partial object
            object? partialValue = property.GetValue(partialObject);

            //Only compare non-null values since these are going to be the ones that matter
            if (partialValue != null)
            {
                // Build the condition for this property
                BinaryExpression? condition = Expression.Equal(Expression.Property(parameter, property), Expression.Constant(partialValue, property.PropertyType));

                // Combine the conditions using 'AndAlso' if this is not the first condition
                conditions = conditions == null ? condition : Expression.AndAlso(conditions, condition);
            }
        }

        T? model = null;
        if (conditions != null)
        {
            // Build the final lambda expression and execute the query
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(conditions, parameter);
            model = queryable.FirstOrDefault(lambda);
        }
        return model;
    }

    /// <summary>
    /// UNTESTED - Merge the field values from multiple instances of the same object
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="instances">Objects to merge fields from</param>
    public static T MergeInstances<T>(IEnumerable<T> instances) where T : class
    {
        T merged = instances.First();
        foreach (T instance in instances)
        {
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                object? value = property.GetValue(instance);
                object? mergedValue = property.GetValue(merged);

                if (value != default && mergedValue == default)
                {
                    property.SetValue(merged, value);
                }
            }
        }

        return merged;
    }

    /// <summary>
    /// Returns whether a Type has the specified attribute or not
    /// </summary>
    /// <param name="type">The type to check for the specified attribute</param>
    /// <param name="attributeName">The name of the attribute you are checking the provided type for</param>
    /// <returns>True if the object has the specified attribute</returns>
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

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="collection">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<T>([NotNullWhen(true)] this ICollection<T>? collection)
    {
        return collection?.Count > 0;
    }

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="list">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<T>([NotNullWhen(true)] this IList<T>? list)
    {
        return list?.Count > 0;
    }

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="bag">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<T>([NotNullWhen(true)] this ConcurrentBag<T>? bag)
    {
        return bag?.Count > 0;
    }

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="array">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<T>([NotNullWhen(true)] this T[]? array)
    {
        return array?.Length > 0;
    }

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type</typeparam>
    /// <typeparam name="T">Dictionary value type</typeparam>
    /// <param name="dict">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<TKey, T>([NotNullWhen(true)] this IDictionary<TKey, T>? dict) where TKey : notnull
    {
        return dict?.Count > 0;
    }

    /// <summary>
    /// Faster alternative to using the .Any() linq method
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type</typeparam>
    /// <typeparam name="T">Dictionary value type</typeparam>
    /// <param name="dict">Collection being checked for having elements</param>
    /// <returns>True if collection has any objects in it</returns>
    public static bool AnyFast<TKey, T>([NotNullWhen(true)] this ConcurrentDictionary<TKey, T>? dict) where TKey : notnull
    {
        return dict?.Count > 0;
    }

    /// <summary>
    /// Combine multiple expressions into a single expression
    /// </summary>
    /// <param name="expressions">Enumerable containing at least one expression</param>
    /// <returns>A single expression equivalent of the enumerated expressions passed in</returns>
    public static Expression<Func<T, bool>>? CombineExpressions<T>(IEnumerable<Expression<Func<T, bool>>> expressions)
    {
        Expression<Func<T, bool>>? combined = null;

        foreach (Expression<Func<T, bool>> expression in expressions)
        {
            if (combined == null)
            {
                combined = expression;
            }
            else
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

                ReplaceParameterVisitor leftVisitor = new(expression.Parameters[0], parameter);
                Expression left = leftVisitor.Visit(expression.Body);

                ReplaceParameterVisitor rightVisitor = new(combined.Parameters[0], parameter);
                Expression right = rightVisitor.Visit(combined.Body);

                combined = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
            }
        }

        return combined;
    }

    /// <summary>
    /// Get the number of properties in a class that are set to their default value
    /// </summary>
    /// <param name="obj">Object to count default properties in</param>
    /// <returns>Number of properties in a class that are set to their default value</returns>
    public static int CountDefaultProps<T>(this T obj) where T : class
    {
        return typeof(T).GetProperties().Count(x => x.CanWrite && x.GetValue(obj) == x.PropertyType.GetDefaultValue());
    }

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
    /// Checks to see if a type is a delegate
    /// </summary>
    /// <param name="type">Type to check to see if it's a delegate</param>
    /// <returns>True if type parameter is a delegate</returns>
    public static bool IsDelegate(this Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks to see if a type is an array
    /// </summary>
    /// <param name="type">Type to check if it's an array</param>
    /// <returns>True if type parameter is an array</returns>
    public static bool IsArray(this Type type)
    {
        return type.IsArray;
    }

    /// <summary>
    /// Checks to see if a type is a class other than a string
    /// </summary>
    /// <param name="type">Type to check to see if it's a class other than a string</param>
    /// <returns>True if type parameter is a class other than a string</returns>
    public static bool IsClassOtherThanString(this Type? type)
    {
        return type == null || (!type.IsValueType && type != typeof(string)); //Added type == null || - Nick
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="key">Key of new item to add to dict</param>
    /// <param name="value">Value of new item to add to dict</param>
    public static void AddDictionaryItem<K, V>(this IDictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        dict.TryAdd(key, value);
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="keyValuePair">Key value pair to add to dict</param>
    public static void AddDictionaryItem<K, V>(this IDictionary<K, V> dict, KeyValuePair<K, V> keyValuePair) where K : notnull
    {
        dict.TryAdd(keyValuePair.Key, keyValuePair.Value);
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="keyValuePairs">Enumerable of items to add to dict</param>
    public static void AddDictionaryItems<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> keyValuePairs) where K : notnull
    {
        foreach (KeyValuePair<K, V> keyValuePair in keyValuePairs)
        {
            if (!dict.ContainsKey(keyValuePair.Key))
            {
                dict.TryAdd(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    /// <summary>
    /// Compares two like objects against each other to check to see if they contain the same values
    /// </summary>
    /// <param name="obj1">First object to compare for value equality</param>
    /// <param name="obj2">Second object to compare for value equality</param>
    /// <returns>True if the two objects have the same value for all elements</returns>
    public static bool IsEqual(this object? obj1, object? obj2)
    {
        return IsEqual(obj1, obj2, null);
    }

    /// <summary>
    /// Compare two class objects for value equality
    /// </summary>
    /// <param name="obj1">First object to compare for value equality</param>
    /// <param name="obj2">First object to compare for value equality</param>
    /// <param name="exemptProps">Names of properties to not include in the matching check</param>
    /// <returns>True if both objects contain identical values for all properties except for the ones identified by exemptProps</returns>
    public static bool IsEqual(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null)
    {
        // They're both null.
        if (obj1 == null && obj2 == null)
        {
            return true;
        }
        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null)
        {
            return false;
        }
        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType())
        {
            return false;
        }

        IEnumerable<PropertyInfo> props = obj1.GetType().GetProperties();
        if (exemptProps?.Any() == true)
        {
            props = props.Where(x => exemptProps?.Contains(x.Name) != true);
        }
        foreach (PropertyInfo prop in props)
        {
            object aPropValue = prop.GetValue(obj1) ?? string.Empty;
            object bPropValue = prop.GetValue(obj2) ?? string.Empty;

            bool aIsNumeric = aPropValue.IsNumeric();
            bool bIsNumeric = bPropValue.IsNumeric();

            try
            {
                //This will prevent issues with numbers with varying decimal places from being counted as a difference
                if ((aIsNumeric && bIsNumeric && decimal.Parse(aPropValue.ToString()!) != decimal.Parse(bPropValue.ToString()!)) ||
                    (!(aIsNumeric && bIsNumeric) && aPropValue.ToString() != bPropValue.ToString()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check if an object is a numeric type
    /// </summary>
    /// <param name="testObject">Object to check to see if it's numeric</param>
    /// <returns>True if testObject is a numeric type</returns>
    public static bool IsNumeric(this object? testObject)
    {
        bool isNumeric = false;
        if (!testObject.IsNullOrWhiteSpace())
        {
            isNumeric = decimal.TryParse(testObject.ToString(), NumberStyles.Number, NumberFormatInfo.InvariantInfo, out _);
        }
        return isNumeric;
    }

    public static async Task<byte[]> ReadStreamAsync(Stream stream, int bufferSize = 4096)
    {
        int read;
        await using MemoryStream ms = new();
        byte[] buffer = new byte[bufferSize];
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
        }
        return ms.ToArray();
    }
}

public class ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter = oldParameter;
    private readonly ParameterExpression _newParameter = newParameter;

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : node;
    }
}
