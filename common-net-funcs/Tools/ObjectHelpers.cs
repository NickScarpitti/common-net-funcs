using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper methods for complex classes and lists
/// </summary>
public static class ObjectHelpers
{
    /// <summary>
    /// Copy properties of the same name from one object to another
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TU"></typeparam>
    /// <param name="source"></param>
    /// <param name="dest"></param>
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
    /// Set values in an IEnumerable as an extension of linq
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod)
    {
        foreach (T item in items)
        {
            updateMethod(item);
        }
        return items.ToList();
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq using a Parallel.ForEach loop
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <param name="maxDegreeOfParallelism">Integer setting the max number of parallel operations allowed. Default of -1 allows maximum possible.</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValueParallel<T>(this IEnumerable<T> items, Action<T> updateMethod, int maxDegreeOfParallelism = -1)
    {
        ConcurrentBag<T> concurrentBag = new(items);
        Parallel.ForEach(concurrentBag, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item =>
        {
            updateMethod(item);
        });
        return concurrentBag;
    }

    /// <summary>
    /// Removes excess spaces in string properties inside of an object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    public static void TrimObjectStrings<T>(this T obj)
    {
        PropertyInfo[] props = typeof(T).GetProperties();
        if (props != null)
        {
            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? value = (string)(prop.GetValue(obj) ?? string.Empty);
                    if (!string.IsNullOrEmpty(value))
                    {
                        value = Regex.Replace(value.Trim(), @"\s+", " "); //Replaces any multiples of spacing with a single space
                        prop.SetValue(obj, value.Trim());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds AddRange functionality to ConcurrentBag similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="concurrentBag">ConcurrentBag to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    /// <param name="parallelOptions">ParallelOptions for Parallel.ForEach</param>
    public static void AddRange<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, ParallelOptions? parallelOptions = null)
    {
        Parallel.ForEach(toAdd.Where(x => x != null), parallelOptions ?? new(), item =>
        {
            concurrentBag.Add(item!);
        });
    }

    /// <summary>
    /// Create a single item list from an object
    /// </summary>
    /// <typeparam name="T">Type to use in list</typeparam>
    /// <param name="obj">Object to turn into a single item list</param>
    /// <returns></returns>
    public static List<T> TolList<T>(this T obj)
    {
        return new List<T>() { obj };
    }
}
