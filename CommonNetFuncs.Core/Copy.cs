using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(UT).GetProperties().Where(x => x.CanWrite);

        foreach (PropertyInfo sourceProp in sourceProps.Where(x => destProps.Any(y => y.Name.StrComp(x.Name))))
        {
            PropertyInfo? destProp = destProps.FirstOrDefault(x => x.Name.StrComp(sourceProp.Name));
            destProp?.SetValue(dest, sourceProp.GetValue(source, null), null);
        }
    }

    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T">Type of object being copied</typeparam>
    /// <param name="source">Object to copy common properties from</param>
    public static T CopyPropertiesToNew<T>(this T source) where T : new()
    {
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(T).GetProperties().Where(x => x.CanWrite);

        T dest = new();
        foreach (PropertyInfo sourceProp in sourceProps.Where(x => destProps.Any(y => y.Name.StrComp(x.Name))))
        {
            PropertyInfo? destProp = destProps.FirstOrDefault(x => x.Name.StrComp(sourceProp.Name));
            destProp?.SetValue(dest, sourceProp.GetValue(source, null), null);
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
        IEnumerable<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(x => x.CanRead);
        IEnumerable<PropertyInfo> destProps = typeof(UT).GetProperties().Where(x => x.CanWrite);

        UT dest = new();
        foreach (PropertyInfo sourceProp in sourceProps.Where(x => destProps.Any(y => y.Name.StrComp(x.Name))))
        {
            PropertyInfo? destProp = destProps.FirstOrDefault(x => x.Name.StrComp(sourceProp.Name));
            destProp?.SetValue(dest, sourceProp.GetValue(source, null), null);
        }
        return dest;
    }

    //Can handle collections
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
        if (source == null) return default;

        if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(IEnumerable).IsAssignableFrom(typeof(UT)) && typeof(T) != typeof(string) && typeof(UT) != typeof(string))
        {
            return (UT?)CopyCollection(source, typeof(UT), maxDepth) ?? new();
        }

        return (UT?)CopyObject(source, typeof(UT), 0, maxDepth) ?? new();
    }

    private static object? CopyObject(object source, Type destType, int depth, int maxDepth)
    {
        if (source == null) return null;

        Type sourceType = source.GetType();
        object? dest = Activator.CreateInstance(destType);

        if (sourceType == destType)
        {
            dest = source;
        }
        else
        {
            IEnumerable<PropertyInfo> sourceProps = sourceType.GetProperties().Where(x => x.CanRead);
            IEnumerable<PropertyInfo> destProps = destType.GetProperties().Where(x => x.CanWrite);

            foreach (PropertyInfo sourceProp in sourceProps.Where(x => destProps.Any(y => y.Name.StrComp(x.Name))))
            {
                PropertyInfo? destProp = destProps.FirstOrDefault(x => x.Name.StrComp(sourceProp.Name));
                if (destProp == null) continue;

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
                else if ((maxDepth == -1 || depth < maxDepth) && sourceProp.PropertyType.IsClass)
                {
                    object? nestedValue = CopyObject(value, destProp.PropertyType, depth + 1, maxDepth);
                    destProp.SetValue(dest, nestedValue, null);
                }
                else
                {
                    destProp.SetValue(dest, value, null);
                }
            }
        }

        return dest;
    }

    private static object? CopyCollection(object source, Type destType, int maxDepth)
    {
        if (source == null) return null;

        IEnumerable sourceCollection = (IEnumerable)source;

        // Check if the destination type is a dictionary
        if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
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
            foreach (object item in sourceCollection)
            {
                object key = kvpType.GetProperty("Key")!.GetValue(item, null)!;
                object? value = kvpType.GetProperty("Value")!.GetValue(item, null);

                object copiedKey = CopyObject(key, destKeyType, 1, maxDepth)!;
                object? copiedValue = value == null ? null : CopyObject(value, destValueType, 0, maxDepth);
                destDictionary.Add(copiedKey, copiedValue);
            }

            return destDictionary;
        }
        else
        {
            Type elementType = destType.IsArray ? destType.GetElementType() ?? typeof(object) : destType.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType)!;

            foreach (object item in sourceCollection)
            {
                object? copiedItem = CopyObject(item, elementType, 0, maxDepth);

                list.Add(copiedItem);
            }

            if (destType.IsInterface || destType == listType)
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
                    addMethod.Invoke(destCollection, [item]);
                }
            }

            return destCollection;
        }
    }

    /// <summary>
    /// <para>UNTESTED - Merge the field values from one instance into another of the same object</para>
    /// <para>Only default values will be overridden by mergeFromObjs</para>
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="mergeIntoObject">Object to merge properties into</param>
    /// <param name="mergeFromObjects">Objects to merge properties from</param>
    public static T MergeInstances<T>(this T mergeIntoObject, IEnumerable<T> mergeFromObjects) where T : class
    {
        foreach (T instance in mergeFromObjects)
        {
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                object? value = property.GetValue(instance);
                object? mergedValue = property.GetValue(mergeIntoObject);

                if (value != default && mergedValue == default)
                {
                    property.SetValue(mergeIntoObject, value);
                }
            }
        }

        return mergeIntoObject;
    }

    /// <summary>
    /// <para>UNTESTED - Merge the field values from one instance into another of the same object</para>
    /// <para>Only default values will be overridden by mergeFromObj</para>
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="mergeIntoObject">Object to merge properties into</param>
    /// <param name="mergeFromObject">Object to merge properties from</param>
    public static T MergeInstances<T>(this T mergeIntoObject, T mergeFromObject) where T : class
    {
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            object? value = property.GetValue(mergeFromObject);
            object? mergedValue = property.GetValue(mergeIntoObject);

            if (value != default && mergedValue == default)
            {
                property.SetValue(mergeIntoObject, value);
            }
        }

        return mergeIntoObject;
    }
}
