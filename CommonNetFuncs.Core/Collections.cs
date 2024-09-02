using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using static System.Convert;

namespace CommonNetFuncs.Core;

public static class Collections
{
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
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="key">Key of new item to add to dictionary</param>
    /// <param name="value">Value of new item to add to dictionary</param>
    public static void AddDictionaryItem<K, V>(this IDictionary<K, V?> dict, K key, V? value = default) where K : notnull
    {
        dict.TryAdd(key, value);
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="keyValuePair">Key value pair to add to dictionary</param>
    public static void AddDictionaryItem<K, V>(this IDictionary<K, V> dict, KeyValuePair<K, V> keyValuePair) where K : notnull
    {
        dict.TryAdd(keyValuePair.Key, keyValuePair.Value);
    }

    /// <summary>
    /// Provides a safe way to add a new Dictionary key without having to worry about duplication
    /// </summary>
    /// <param name="dict">Dictionary to add item to</param>
    /// <param name="keyValuePairs">Enumerable of items to add to dictionary</param>
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
    /// Set values in an IEnumerable as an extension of linq
    /// </summary>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static void SetValue(this IEnumerable<string?> items, Func<string?, string?> updateMethod)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(updateMethod);

        IList<string?> list = items as IList<string?> ?? items.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            list[i] = updateMethod(list[i]);
        }
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
    /// <para>Allows you to act upon every element in an Array</para>
    /// <para>Used like outerArray.SetValue((array, indices) => array.SetValue(SomeMethod(outerArray.GetValue(indices)), indices))</para>
    /// </summary>
    /// <param name="array">Array to act upon</param>
    /// <param name="action">Action to perform on each element of the array</param>
    public static void SetValue(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) { return; }
        ArrayTraverse walker = new(array);
        do { action(array, walker.Position); }
        while (walker.Step());
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
    /// Select object from a collection by matching all non-null fields to an object of the same type comprising the collection
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
    /// Convert datatable to equivalent list of specified class
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToList<T>(DataTable table, bool convertShortToBool = false) where T : class, new()
    {
        List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];

        List<T?> list = new(table.Rows.Count);

        if (table.Rows.Count > 0)
        {
            DataRow firstRow = table.Rows[0];
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (table.Columns.Contains(propertyInfo.Name))
                {
                    if (convertShortToBool)
                    {
                        Type colType = firstRow[table.Columns[propertyInfo.Name]!].GetType();
                        map.Add(new(table.Columns[propertyInfo.Name]!, propertyInfo, convertShortToBool && (colType == typeof(short) || colType == typeof(short?))));
                    }
                    else
                    {
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, false));
                    }
                }
            }

            foreach (DataRow row in table.AsEnumerable())
            {
                if (row == null)
                {
                    list.Add(null);
                    continue;
                }
                T item = new();
                foreach ((DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort) pair in map)
                {
                    object? value = row[pair.DataColumn!];

                    //Handle issue where DB returns Int16 for boolean values
                    if (pair.IsShort && (pair.PropertyInfo!.PropertyType == typeof(bool) || pair.PropertyInfo!.PropertyType == typeof(bool?)))
                    {
                        pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? ToBoolean(value) : null);
                    }
                    else
                    {
                        pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? value : null);
                    }
                }
                list.Add(item);
            }
        }
        return list;
    }

    /// <summary>
    /// Convert datatable to equivalent list of specified class using a Parallel.Foreach loop to get data from each row
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="maxDegreeOfParallelism">Parallelism parameter to be used in Parallel.Foreach loop</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToListParallel<T>(DataTable table, int maxDegreeOfParallelism = -1, bool convertShortToBool = false) where T : class, new()
    {
        ConcurrentBag<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];

        ConcurrentBag<T?> bag = [];

        if (table.Rows.Count > 0)
        {
            DataRow firstRow = table.Rows[0];
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                if (table.Columns.Contains(propertyInfo.Name))
                {
                    if (convertShortToBool)
                    {
                        Type colType = firstRow[table.Columns[propertyInfo.Name]!].GetType();
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, convertShortToBool && (colType == typeof(short) || colType == typeof(short?))));
                    }
                    else
                    {
                        map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, false));
                    }
                }
            }

            Parallel.ForEach(table.AsEnumerable(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, row =>
            {
                T? item = new();
                if (row != null)
                {
                    foreach ((DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort) pair in map)
                    {
                        object? value = row[pair.DataColumn!];

                        //Handle issue where DB returns Int16 for boolean values
                        if (pair.IsShort && (pair.PropertyInfo!.PropertyType == typeof(bool) || pair.PropertyInfo!.PropertyType == typeof(bool?)))
                        {
                            pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? ToBoolean(value) : null);
                        }
                        else
                        {
                            pair.PropertyInfo!.SetValue(item, value is not System.DBNull ? value : null);
                        }
                    }
                }
                else
                {
                    item = null;
                }
                bag.Add(item);
            });
        }
        return bag.ToList();
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

internal class ArrayTraverse
{
    public int[] Position;
    private readonly int[] maxLengths;

    public ArrayTraverse(Array array)
    {
        maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            maxLengths[i] = array.GetLength(i) - 1;
        }
        Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < Position.Length; ++i)
        {
            if (Position[i] < maxLengths[i])
            {
                Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    Position[j] = 0;
                }
                return true;
            }
        }
        return false;
    }
}
