using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static class Copy
{
    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of source object</typeparam>
    /// <typeparam name="UT">Type of destination object</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    /// <param name="dest">Object to copy common properties to</param>
    public static void CopyPropertiesTo<T, UT>(this T source, UT dest)
    {
        if (source == null)
        {
            dest = default!;
        }
        else
        {
            dest ??= Activator.CreateInstance<UT>();
            IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromCache(typeof(T)).Where(x => x.CanRead);
            Dictionary<string, PropertyInfo> destPropDict = GetOrAddPropertiesFromCache(typeof(UT)).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

            foreach (PropertyInfo sourceProp in sourceProps)
            {
                if (destPropDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp) && destProp.PropertyType == sourceProp.PropertyType)
                {
                    destProp.SetValue(dest, sourceProp.GetValue(source, null), null);
                }
            }
        }
    }

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of object being copied</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    public static T CopyPropertiesToNew<T>(this T source) where T : new()
    {
        if (source == null)
        {
            return default!;
        }

        IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromCache(typeof(T)).Where(x => x.CanRead);
        Dictionary<string, PropertyInfo> destPropDict = sourceProps.ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

        T dest = new();
        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destPropDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp) && destProp.PropertyType == sourceProp.PropertyType)
            {
                destProp.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
        return dest;
    }

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of object being copied</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    public static UT CopyPropertiesToNew<T, UT>(this T source) where UT : new()
    {
        IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromCache(typeof(T)).Where(x => x.CanRead);
        Dictionary<string, PropertyInfo> destPropDict = GetOrAddPropertiesFromCache(typeof(UT)).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

        UT dest = new();
        foreach (PropertyInfo sourceProp in sourceProps)
        {
            if (destPropDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp) && destProp.PropertyType == sourceProp.PropertyType)
            {
                destProp.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
        return dest;
    }

    // Can handle collections
    /// <summary>
    /// Copies properties of one class to a new instance of a class using reflection based on property name matching
    /// </summary>
    /// <typeparam name="T">Type to copy values from</typeparam>
    /// <typeparam name="UT">Type to copy values to</typeparam>
    /// <param name="source">Object to copy values into new object from</param>
    /// <param name="maxDepth">How deep to recursively traverse. Default = -1 which is unlimited recursion</param>
    /// <returns>A new instance of UT with properties of the same name from source populated</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static UT? CopyPropertiesToNewRecursive<T, UT>(this T source, int maxDepth = -1) where UT : new()
    {
        if (source == null)
        {
            return default;
        }

        if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(IEnumerable).IsAssignableFrom(typeof(UT)) && (typeof(T) != typeof(string)) && (typeof(UT) != typeof(string)))
        {
            return (UT?)CopyCollection(source, typeof(UT), maxDepth) ?? new();
        }

        return (UT?)CopyObject(source, typeof(UT), 0, maxDepth) ?? new();
    }

    private static object? CopyObject(object source, Type destType, int depth, int maxDepth)
    {
        if (source == null)
        {
            return null;
        }

        Type sourceType = source.GetType();
        object? dest = Activator.CreateInstance(destType);

        if (sourceType == destType)
        {
            dest = source;
        }
        else
        {
            IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromCache(sourceType).Where(x => x.CanRead);
            //IEnumerable<PropertyInfo> destProps = GetOrAddPropertiesFromCache(destType).Where(x => x.CanWrite);
            Dictionary<string, PropertyInfo> destPropsDict = GetOrAddPropertiesFromCache(destType).Where(x => x.CanWrite).ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            foreach (PropertyInfo sourceProp in sourceProps)
            {
                if (!destPropsDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp) || destProp == null)
                {
                    continue; // Skip if the property does not exist in the destination type
                }

                object? value = sourceProp.GetValue(source, null);
                if (value == null)
                {
                    destProp.SetValue(dest, null, null);
                    continue;
                }

                if (sourceProp.PropertyType.IsSimpleType())
                {
                    destProp.SetValue(dest, value, null);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType) && typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType))
                {
                    object? collectionValue = CopyCollection(value, destProp.PropertyType, maxDepth);
                    destProp.SetValue(dest, collectionValue, null);
                }
                else if (((maxDepth == -1) || (depth < maxDepth)) && sourceProp.PropertyType.IsClass)
                {
                    object? nestedValue = CopyObject(value, destProp.PropertyType, depth + 1, maxDepth);
                    destProp.SetValue(dest, nestedValue, null);
                }
                else if (!sourceProp.PropertyType.IsClass && destProp.GetType() == value.GetType()) //Only do direct assignment if the types are the same and not a class
                {
                    destProp.SetValue(dest, value, null);
                }
                else
                {
                    destProp.SetValue(dest, default, null); //Set to default if the types are not the same
                }
            }
        }

        return dest;
    }

    private static object? CopyCollection(object source, Type destType, int maxDepth, CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            return null;
        }

        IEnumerable sourceCollection = (IEnumerable)source;

        // Check if the destination type is a dictionary
        if (destType.IsGenericType && (destType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            Type[] sourceGenericArgs = source.GetType().GetGenericArguments();
            Type sourceKeyType = sourceGenericArgs[0];
            Type sourceValueType = sourceGenericArgs[1];

            Type[] destGenericArgs = destType.GetGenericArguments();
            Type destKeyType = destGenericArgs[0];
            Type destValueType = destGenericArgs[1];

            // Create a new dictionary
            IDictionary destDictionary = (IDictionary)Activator.CreateInstance(destType)!;

            // Create a generic type for KeyValuePair<TKey, TValue>
            Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(sourceKeyType, sourceValueType);

            // Copy key-value pairs

            bool? keyIsSimpleType = null;
            bool? valueIsSimpleType = null;
            foreach (object item in sourceCollection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                object key = kvpType.GetProperty("Key")!.GetValue(item, null)!;
                object? value = kvpType.GetProperty("Value")!.GetValue(item, null);
                keyIsSimpleType ??= key.GetType().IsSimpleType();
                valueIsSimpleType ??= value?.GetType().IsSimpleType();

                object copiedKey = (bool)keyIsSimpleType ? key : CopyObject(key, destKeyType, 1, maxDepth)!;
                object? copiedValue = value == null ? null : (bool)valueIsSimpleType! ? value : CopyObject(value, destValueType, 0, maxDepth);
                destDictionary.Add(copiedKey, copiedValue);
            }

            return destDictionary;
        }
        else
        {
            Type elementType = destType.IsArray ? (destType.GetElementType() ?? typeof(object)) : destType.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType)!;

            bool? itemIsSimpleType = null;
            foreach (object? item in sourceCollection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                itemIsSimpleType ??= item?.GetType().IsSimpleType();
                object? copiedItem = item == null ? null : (bool)itemIsSimpleType! ? item : CopyObject(item, elementType, 0, maxDepth);

                list.Add(copiedItem);
            }

            if (destType.IsInterface || (destType == listType))
            {
                return list;
            }

            if (destType.IsArray)
            {
                Array array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                return array;
            }

            object? destCollection = Activator.CreateInstance(destType);
            MethodInfo? addMethod = destType.GetMethod("Add");
            if (addMethod != null)
            {
                foreach (object? item in list)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    addMethod.Invoke(destCollection, [item]);
                }
            }

            return destCollection;
        }
    }

    /// <summary>
    /// <para>Merge the field values from one instance into another of the same object</para>
    /// <para>Only default values will be overridden by mergeFromObjects</para>
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="mergeIntoObject">Object to merge properties into</param>
    /// <param name="mergeFromObjects">Objects to merge properties from</param>
    public static T MergeInstances<T>(this T mergeIntoObject, IEnumerable<T> mergeFromObjects, CancellationToken cancellationToken = default) where T : class
    {
        foreach (T instance in mergeFromObjects)
        {
            mergeIntoObject.MergeInstances(instance, cancellationToken);
        }

        return mergeIntoObject;
    }

    /// <summary>
    /// <para>Merge the field values from one instance into another of the same object</para>
    /// <para>Only default values will be overridden by mergeFromObject</para>
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="mergeIntoObject">Object to merge properties into</param>
    /// <param name="mergeFromObject">Object to merge properties from</param>
    public static T MergeInstances<T>(this T mergeIntoObject, T mergeFromObject, CancellationToken cancellationToken = default) where T : class
    {
        foreach (PropertyInfo property in GetOrAddPropertiesFromCache(typeof(T)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? value = property.GetValue(mergeFromObject);
            object? mergedValue = property.GetValue(mergeIntoObject);

            object? defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;

            if ((value != default) && (mergedValue?.Equals(defaultValue) != false))
            {
                property.SetValue(mergeIntoObject, value);
            }
        }

        return mergeIntoObject;
    }
}
