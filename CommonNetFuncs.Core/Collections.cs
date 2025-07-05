using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
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
    public static void AddDictionaryItems<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> keyValuePairs, CancellationToken cancellationToken = default) where K : notnull
    {
        foreach (KeyValuePair<K, V> keyValuePair in keyValuePairs)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
    public static void AddRangeParallel<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, ParallelOptions? parallelOptions = null, CancellationToken cancellationToken = default)
    {
        Parallel.ForEach(toAdd.SelectNonNull(), parallelOptions ?? new(), item =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            concurrentBag.Add(item!);
        });
    }

    /// <summary>
    /// Adds AddRange functionality to ConcurrentBag similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T">Type of object being added</typeparam>
    /// <param name="concurrentBag">ConcurrentBag to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    public static void AddRange<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, CancellationToken cancellationToken = default)
    {
        foreach (T? item in toAdd.SelectNonNull())
        {
            cancellationToken.ThrowIfCancellationRequested();
            concurrentBag.Add(item!);
        }
    }

    /// <summary>
    /// Adds AddRange functionality to HashSet similar to a list. Skips null items
    /// </summary>
    /// <typeparam name="T">Type of object being added</typeparam>
    /// <param name="hashSet">HashSet to add list of items to</param>
    /// <param name="toAdd">Items to add to the ConcurrentBag object</param>
    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T?> toAdd, CancellationToken cancellationToken = default)
    {
        foreach (T? item in toAdd.SelectNonNull())
        {
            cancellationToken.ThrowIfCancellationRequested();
            hashSet.Add(item!);
        }
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq
    /// </summary>
    /// <typeparam name="T">Type of object having values set</typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod, CancellationToken cancellationToken = default)
    {
        foreach (T item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
    public static List<string?> SetValue(this IEnumerable<string?> items, Func<string?, string?> updateMethod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(updateMethod);

        IList<string?> list = items as IList<string?> ?? items.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            list[i] = updateMethod(list[i]);
        }
        return list.ToList();
    }

    /// <summary>
    /// Set values in an IEnumerable as an extension of linq using a Parallel.ForEach loop
    /// </summary>
    /// <typeparam name="T">Type of object having values set</typeparam>
    /// <param name="items">Items to have the updateMethod expression performed on</param>
    /// <param name="updateMethod">Lambda expression of the action to perform</param>
    /// <param name="maxDegreeOfParallelism">Integer setting the max number of parallel operations allowed. Default of -1 allows maximum possible.</param>
    /// <returns>IEnumerable with values updated according to updateMethod</returns>
    public static IEnumerable<T> SetValueParallel<T>(this IEnumerable<T> items, Action<T> updateMethod, int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
    {
        ConcurrentBag<T> concurrentBag = new(items);
        Parallel.ForEach(concurrentBag, new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            updateMethod(item);
        });
        return concurrentBag;
    }

    /// <summary>
    /// <para>Allows you to act upon every element in an Array</para> <para>Used like outerArray.SetValue((array, indices) => array.SetValue(SomeMethod(outerArray.GetValue(indices)), indices))</para>
    /// </summary>
    /// <param name="array">Array to act upon</param>
    /// <param name="action">Action to perform on each element of the array</param>
    public static void SetValue(this Array array, Action<Array, int[]> action, CancellationToken cancellationToken = default)
    {
        if (array.LongLength == 0)
        {
            return;
        }
        ArrayTraverse walker = new(array);
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            action(array, walker.Position);
        } while (walker.Step());
    }

    /// <summary>
    /// Select only strings that are not null, empty, or only whitespace
    /// </summary>
    /// <param name="items">Enumerable of strings to select from</param>
    /// <returns>
    /// An enumerable containing all string values from the original collection that are not null, empty, or only whitespace
    /// </returns>
    [return: NotNullIfNotNull(nameof(items))]
    public static IEnumerable<string>? SelectNonEmpty(this IEnumerable<string?>? items)
    {
        return items?.Where(x => !x.IsNullOrWhiteSpace()).Select(x => x!);
    }

    /// <summary>
    /// Select only objects that are not null
    /// </summary>
    /// <param name="items">Enumerable of objects to select from</param>
    /// <returns>An enumerable containing all object values from the original collection that are not null</returns>
    [return: NotNullIfNotNull(nameof(items))]
    public static IEnumerable<T>? SelectNonNull<T>(this IEnumerable<T?>? items)
    {
        return items?.Where(x => x != null).Select(x => x!);
    }

    /// <summary>
    /// Create a single item list from an object
    /// </summary>
    /// <typeparam name="T">Type to use in list</typeparam>
    /// <param name="obj">Object to turn into a single item list</param>
    [Obsolete("Use collection expression instead")]
    public static List<T> SingleToList<T>(this T? obj)
    {
        return (obj != null) ? [obj] : [];
    }

    /// <summary>
    /// Create a single item list from an object
    /// </summary>
    /// <param name="obj">Object to turn into a single item list</param>
    [Obsolete("Use collection expression instead")]
    public static List<string> SingleToList(this string? obj, bool allowEmptyStrings = false)
    {
        if (!allowEmptyStrings)
        {
            return (!obj.IsNullOrWhiteSpace()) ? [obj] : [];
        }
        return (obj != null) ? [obj] : [];
    }

    /// <summary>
    /// Select object from a collection by matching all non-null fields to an object of the same type comprising the collection
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="queryable">Queryable collection to select from</param>
    /// <param name="partialObject">Object with fields to match with objects in the queryable collection</param>
    /// <param name="ignoreDefaultValues">Ignore default values in retrieval when true</param>
    /// <returns>First object that matches all non-null fields in partialObject</returns>
    public static T? GetObjectByPartial<T>(this IQueryable<T> queryable, T partialObject, bool ignoreDefaultValues = false, CancellationToken cancellationToken = default) where T : class
    {
        // Get the properties of the object using reflection
        PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Build the expression tree for the conditions
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? conditions = null;

        foreach (PropertyInfo property in properties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get the value of the property from the partial object
            object? partialValue = property.GetValue(partialObject);

            // Skip null values
            if (partialValue == null)
            {
                continue;
            }

            // Skip default values if ignoreDefaultValues is true
            if (ignoreDefaultValues)
            {
                // Get the default value for this property type
                object? defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;

                // Skip if the value equals the default value for its type
                if (Equals(partialValue, defaultValue))
                {
                    continue;
                }
            }

            // Only compare non-null (and potentially non-default) values since these are going to be the ones that matter
            // Build the condition for this property
            BinaryExpression? condition = Expression.Equal(Expression.Property(parameter, property), Expression.Constant(partialValue, property.PropertyType));

            // Combine the conditions using 'AndAlso' if this is not the first condition
            conditions = (conditions == null) ? condition : Expression.AndAlso(conditions, condition);
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
    public static List<T?> ToList<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        List<T?> list = new(table.Rows.Count);
        if (table.Rows.Count > 0)
        {
            IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
            foreach (DataRow row in table.AsEnumerable())
            {
                cancellationToken.ThrowIfCancellationRequested();
                list.Add(row.ParseRowValues<T>(map, cancellationToken: cancellationToken));
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
    public static List<T?> ToListParallel<T>(this DataTable table, int maxDegreeOfParallelism = -1, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        ConcurrentBag<T?> bag = [];
        if (table.Rows.Count > 0)
        {
            IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
            Parallel.ForEach(table.AsEnumerable(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, row => bag.Add(row.ParseRowValues<T>(map, cancellationToken)));
        }
        return bag.ToList();
    }

    /// <summary>
    /// Convert datatable to equivalent list of specified class using a Parallel.Foreach loop to get data from each row
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static IEnumerable<T?> ToEnumerableParallel<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        IEnumerable<T?> values = [];
        if (table.Rows.Count > 0)
        {
            IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
            values = table.AsEnumerable().AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(row => row.ParseRowValues<T>(map, cancellationToken));
        }
        return values;
    }

    /// <summary>
    /// Convert datatable to equivalent list of specified class using a Parallel.Foreach loop to get data from each row
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true</param>
    /// <returns>List containing table values as the specified class</returns>
    public static IEnumerable<T?> ToEnumerableStreaming<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (table.Rows.Count > 0)
        {
            IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken);
            Task<T?>? outstandingItem = null;
            T? Transform(object x)
            {
                return ParseRowValues<T>((DataRow)x, map, cancellationToken);
            }

            foreach (DataRow row in table.AsEnumerable())
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task<T?>? tmp = outstandingItem;

                // note: passed in as "state", not captured, so not a foreach/capture bug
                outstandingItem = new(Transform!, row);
                outstandingItem.Start();

                if (tmp != null)
                {
                    yield return tmp.Result;
                }
            }

            if (outstandingItem != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return outstandingItem.Result;
            }
        }
    }

    private static List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> GetDataTableMap<T>(this DataTable table, bool convertShortToBool, CancellationToken cancellationToken = default)
    {
        List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];
        DataRow firstRow = table.Rows[0];
        foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (table.Columns.Contains(propertyInfo.Name))
            {
                if (convertShortToBool)
                {
                    Type colType = firstRow[table.Columns[propertyInfo.Name]!].GetType();
                    map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, convertShortToBool && ((colType == typeof(short)) || (colType == typeof(short?)))));
                }
                else
                {
                    map.Add((table.Columns[propertyInfo.Name]!, propertyInfo, false));
                }
            }
        }
        return map;
    }

    private static T? ParseRowValues<T>(this DataRow row, IEnumerable<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map, CancellationToken cancellationToken = default) where T : class, new()
    {
        T? item = new();

        foreach ((DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort) pair in map)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? value = row[pair.DataColumn!];

            // Handle issue where DB returns Int16 for boolean values
            if (value is not System.DBNull)
            {
                if (pair.IsShort && ((pair.PropertyInfo!.PropertyType == typeof(bool)) || (pair.PropertyInfo!.PropertyType == typeof(bool?))))
                {
                    pair.PropertyInfo!.SetValue(item, ToBoolean(value));
                }
                else
                {
                    Type valueType = value.GetType();
                    if (((pair.PropertyInfo.PropertyType == typeof(DateOnly)) || (pair.PropertyInfo.PropertyType == typeof(DateOnly?))) && (valueType != typeof(DateOnly)) && (valueType != typeof(DateOnly?)))
                    {
                        if ((valueType == typeof(DateTime)) || (valueType == typeof(DateTime?)))
                        {
                            pair.PropertyInfo!.SetValue(item, DateOnly.FromDateTime((DateTime)value));
                        }
                        else if (DateOnly.TryParse((string)value, out DateOnly dateOnlyValue))
                        {
                            pair.PropertyInfo!.SetValue(item, dateOnlyValue);
                        }
                        else
                        {
                            throw new InvalidCastException($"Unable to convert value '{value}' to DateOnly for property '{pair.PropertyInfo.Name}'");
                        }
                    }
                    else if (((pair.PropertyInfo.PropertyType == typeof(DateTime)) || (pair.PropertyInfo.PropertyType == typeof(DateTime?))) && (valueType != typeof(DateTime)) && (valueType != typeof(DateTime?)))
                    {
                        if ((valueType == typeof(DateOnly)) || (valueType == typeof(DateOnly?)))
                        {
                            pair.PropertyInfo!.SetValue(item, ((DateOnly)value).ToDateTime(TimeOnly.MinValue));
                        }
                        else if (DateTime.TryParse((string)value, out DateTime dateTimeValue))
                        {
                            pair.PropertyInfo!.SetValue(item, dateTimeValue);
                        }
                        else
                        {
                            throw new InvalidCastException($"Unable to convert value '{value}' to DateTime for property '{pair.PropertyInfo.Name}'");
                        }
                    }
                    else
                    {
                        pair.PropertyInfo!.SetValue(item, value);
                    }
                }
            }
            else
            {
                pair.PropertyInfo!.SetValue(item, null);
            }
        }
        return item;
    }

    /// <summary>
    /// Convert a collection into equivalent DataTable object using expression trees
    /// </summary>
    /// <typeparam name="T">Class to use in table creation</typeparam>
    /// <param name="data">Collection to convert into a DataTable</param>
    /// <param name="dataTable">DataTable to optionally insert data into</param>
    /// <param name="useParallel">Parallelizes the conversion</param>
    /// <param name="approximateCount">Used for pre-allocating variable size when using parallelization, default is data.Count()</param>
    /// <param name="degreeOfParallelism">Used for setting number of parallel operations when using parallelization, default is -1 (#cores on machine)</param>
    /// <returns>A DaataTable representation of the collection that was passed in</returns>
    [return: NotNullIfNotNull(nameof(data))]
    public static DataTable? ToDataTable<T>(this IEnumerable<T>? data, DataTable? dataTable = null, bool useParallel = false, int? approximateCount = null, int degreeOfParallelism = -1, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (data == null)
        {
            return null;
        }

        dataTable ??= new();
        return data.ToDataTableExpressionTrees(dataTable, useParallel, approximateCount, degreeOfParallelism, cancellationToken);
    }

    /// <summary>
    /// Convert a collection into equivalent DataTable object using reflection
    /// </summary>
    /// <typeparam name="T">Class to use in table creation</typeparam>
    /// <param name="data">Collection to convert into a DataTable</param>
    /// <param name="dataTable">DataTable to optionally insert data into</param>
    /// <param name="useParallel">Parallelizes the conversion</param>
    /// <param name="approximateCount">Used for pre-allocating variable size when using parallelization, default is data.Count()</param>
    /// <param name="degreeOfParallelism">Used for setting number of parallel operations when using parallelization, default is -1 (#cores on machine)</param>
    /// <returns>A DaataTable representation of the collection that was passed in</returns>
    [Obsolete("Please use ToDataTable<T>(this IEnumerable<T>? data, DataTable? dataTable = null, bool useExpressionTrees = true, bool useParallel = false, int? approximateCount = null, int degreeOfParallelism = -1, CancellationToken cancellationToken = default) instead", false)]
    [return: NotNullIfNotNull(nameof(data))]
    public static DataTable? ToDataTableReflection<T>(this IEnumerable<T>? data, DataTable? dataTable = null, bool useParallel = false, int? approximateCount = null, int degreeOfParallelism = -1, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (data == null)
        {
            return null;
        }

        dataTable ??= new();
        PropertyInfo[] properties = typeof(T).GetProperties();

        // Remove invalid columns
        IEnumerable<string> propertyNames = properties.Select(x => x.Name);
        DataColumn[] columns = new DataColumn[dataTable.Columns.Count];
        dataTable.Columns.CopyTo(columns, 0);
        foreach (DataColumn col in columns)
        {
            if (!propertyNames.Contains(col.ColumnName))
            {
                dataTable.Columns.Remove(col.ColumnName);
            }
        }

        // Create columns
        foreach (PropertyInfo prop in properties)
        {
            if (!dataTable.Columns.Contains(prop.Name))
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
        }

        // Add rows
        if (!useParallel)
        {
            foreach (T item in data.Where(x => x != null))
            {
                cancellationToken.ThrowIfCancellationRequested();
                DataRow row = dataTable.NewRow();
                foreach (PropertyInfo prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? System.DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }
        }
        else
        {
            // Process items in parallel and collect results
            int columnCount = dataTable.Columns.Count;
            List<object[]> rows = new(approximateCount ?? data.Count());
            object lockObj = new();

            ParallelOptions options = new() { MaxDegreeOfParallelism = (degreeOfParallelism == -1) ? Environment.ProcessorCount : degreeOfParallelism };
            Parallel.ForEach(data, options, () => new List<object[]>(), (item, _, localRows) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                object[] rowValues = new object[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    rowValues[i] = properties[i].GetValue(item) ?? System.DBNull.Value;
                }

                localRows.Add(rowValues);
                return localRows;
            },
            localRows =>
            {
                lock (lockObj)
                {
                    rows.AddRange(localRows);
                }
            });

            // Add all rows to the table
            foreach (object[] rowValues in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dataTable.Rows.Add(rowValues);
            }
        }

        return dataTable;
    }

    private static readonly ConcurrentDictionary<Type, TypeAccessor> _typeAccessorCache = new();

    private sealed class TypeAccessor
    {
        public readonly DataColumnCollection ColumnDefinitions;
        public readonly Func<object, object>[] PropertyGetters;
        public readonly string[] PropertyNames;
        public readonly Type[] PropertyTypes;
        public readonly DataTable SchemaTable;

        public TypeAccessor(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();

            SchemaTable = new DataTable();
            PropertyGetters = new Func<object, object>[properties.Length];
            PropertyNames = new string[properties.Length];
            PropertyTypes = new Type[properties.Length];

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                PropertyNames[i] = property.Name;
                PropertyGetters[i] = CreatePropertyGetter(type, property);
                PropertyTypes[i] = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                SchemaTable.Columns.Add(property.Name, PropertyTypes[i]);
            }

            ColumnDefinitions = SchemaTable.Columns;
        }

        private static Func<object, object> CreatePropertyGetter(Type type, PropertyInfo property)
        {
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            UnaryExpression convertInstance = Expression.Convert(instance, type);
            MemberExpression propertyAccess = Expression.Property(convertInstance, property);
            UnaryExpression convertProperty = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertProperty, instance).CompileFast();
        }
    }

    private static DataTable ToDataTableExpressionTrees<T>(this IEnumerable<T> data, DataTable dataTable, bool useParallel, int? approximateCount, int degreeOfParallelism, CancellationToken cancellationToken = default) where T : class, new()
    {
        TypeAccessor typeAccessor = _typeAccessorCache.GetOrAdd(typeof(T), t => new TypeAccessor(t));

        // Remove invalid columns
        DataColumn[] columns = new DataColumn[dataTable.Columns.Count];
        dataTable.Columns.CopyTo(columns, 0);
        foreach (DataColumn col in columns)
        {
            if (!typeAccessor.PropertyNames.Contains(col.ColumnName))
            {
                dataTable.Columns.Remove(col.ColumnName);
            }
        }

        foreach (DataColumn col in typeAccessor.ColumnDefinitions)
        {
            if (!dataTable.Columns.Contains(col.ColumnName))
            {
                dataTable.Columns.Add(new DataColumn(col.ColumnName, col.DataType));
            }
        }

        Func<object, object>[] propertyGetters = typeAccessor.PropertyGetters;
        int columnCount = propertyGetters.Length;
        object[] rowValues = new object[columnCount];

        // Add the rows
        if (!useParallel)
        {
            foreach (T item in data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int i = 0; i < columnCount; i++)
                {
                    rowValues[i] = propertyGetters[i](item) ?? System.DBNull.Value;
                }
                dataTable.Rows.Add(rowValues);
            }
        }
        else
        {
            // Process items in parallel and collect results
            List<object[]> rows = new(approximateCount ?? data.Count());
            object lockObj = new();

            ParallelOptions options = new() { MaxDegreeOfParallelism = (degreeOfParallelism == -1) ? Environment.ProcessorCount : degreeOfParallelism };
            Parallel.ForEach(data, options, () => new List<object[]>(), (item, _, localRows) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                object[] rowValues = new object[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    rowValues[i] = propertyGetters[i](item) ?? System.DBNull.Value;
                }
                localRows.Add(rowValues);
                return localRows;
            },
            localRows =>
            {
                lock (lockObj)
                {
                    rows.AddRange(localRows);
                }
            });

            // Add all rows to the table
            foreach (object[] rowVals in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dataTable.Rows.Add(rowVals);
            }
        }

        return dataTable;
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
    /// Performs a string aggregation on the designated property, using all other properties as the group by
    /// </summary>
    /// <param name="collection">Collection to perform the string aggregation on based on the property identified</param>
    /// <param name="propToAgg">Property to string aggregate</param>
    /// <param name="separator">String value used between aggregated values</param>
    /// <returns>List with specified property aggregated</returns>
    public static IEnumerable<T> StringAggProps<T>(this IEnumerable<T>? collection, string propToAgg, string separator = ";", bool distinct = true, bool parallel = false) where T : class, new()
    {
        return collection.StringAggProps([propToAgg], separator, distinct, parallel);
    }

    /// <summary>
    /// Performs a string aggregation on the designated properties, using all other properties as the group by
    /// </summary>
    /// <param name="collection">Collection to perform the string aggregation on based on the properties identified</param>
    /// <param name="propsToAgg">Properties to string aggregate</param>
    /// <param name="separator">String value used between aggregated values</param>
    /// <returns>List with specified properties aggregated</returns>
    public static IEnumerable<T> StringAggProps<T>(this IEnumerable<T>? collection, string[] propsToAgg, string separator = ";", bool distinct = true, bool parallel = false) where T : class, new()
    {
        if (collection?.Any() != true)
        {
            return [];
        }

        if (!propsToAgg.AnyFast())
        {
            throw new ArgumentException("There must be at least one property identified in propsToAgg", nameof(propsToAgg));
        }

        PropertyInfo[] properties = typeof(T).GetProperties();
        PropertyInfo[] groupingProperties = properties.Where(p => !propsToAgg.Contains(p.Name)).ToArray();

        if (!groupingProperties.AnyFast() || (propsToAgg.Intersect(properties.Select(x => x.Name)).Count() < propsToAgg.Length))
        {
            throw new ArgumentException($"Invalid aggregate property values. All values in propsToAgg must be present in type {typeof(T)}", nameof(propsToAgg));
        }

        if (!parallel)
        {
            return collection.GroupBy(x => new { GroupKey = string.Join("|", groupingProperties.Select(p => p.GetValue(x)?.ToString() ?? string.Empty)) })
                .Select(g =>
                {
                    T result = new();
                    foreach (PropertyInfo prop in properties)
                    {
                        if (propsToAgg.Contains(prop.Name))
                        {
                            string aggregatedValue = distinct ? string.Join(separator, g.Select(x => prop.GetValue(x)?.ToString() ?? string.Empty).Distinct()) :
                                string.Join(separator, g.Select(x => prop.GetValue(x)?.ToString() ?? string.Empty));
                            prop.SetValue(result, aggregatedValue);
                        }
                        else
                        {
                            prop.SetValue(result, prop.GetValue(g.First()));
                        }
                    }
                    return result;
                });
        }
        else
        {
            return collection.AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .GroupBy(x => new { GroupKey = string.Join("|", groupingProperties.Select(p => p.GetValue(x)?.ToString() ?? string.Empty)) })
                .Select(g =>
                {
                    T result = new();
                    foreach (PropertyInfo prop in properties)
                    {
                        if (propsToAgg.Contains(prop.Name))
                        {
                            string aggregatedValue = distinct ? string.Join(separator, g.Select(x => prop.GetValue(x)?.ToString() ?? string.Empty).Distinct()) :
                                string.Join(separator, g.Select(x => prop.GetValue(x)?.ToString() ?? string.Empty));
                            prop.SetValue(result, aggregatedValue);
                        }
                        else
                        {
                            prop.SetValue(result, prop.GetValue(g.First()));
                        }
                    }
                    return result;
                });
        }
    }

    /// <summary>
    /// Returns the index of the first occurrence of an object in a collection.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="value"></param>
    /// <returns>Index of the first occurrence of value in the collection or -1 if not found</returns>
    public static int IndexOf<T>(this IEnumerable<T> collection, T value)
    {
        return collection.IndexOf(value, null);
    }

    /// <summary>
    /// Returns the index of the first occurrence of an object in a collection, using the specified comparer for equality checks.
    /// </summary>
    /// <param name="collection">Collection to get index of object in</param>
    /// <param name="value">Value to check for equality in collection</param>
    /// <param name="comparer">Comparer used when checking for equality with value</param>
    /// <returns>Index of the first occurrence of value in the collection or -1 if not found</returns>
    public static int IndexOf<T>(this IEnumerable<T> collection, T value, IEqualityComparer<T>? comparer)
    {
        comparer ??= EqualityComparer<T>.Default;
        var found = collection.Select((a, i) => new { a, i }).FirstOrDefault(x => comparer.Equals(x.a, value));
        return (found == null) ? (-1) : found.i;
    }

    /// <summary>
    /// Checks to see if the value is in the enum type specified
    /// </summary>
    /// <typeparam name="T">Enum to check against for validity</typeparam>
    /// <param name="value">Value to check to see if it's in the specified enum</param>
    /// <returns>True if value is a valid value of the specified enum, otherwise false</returns>
    public static bool IsIn<T>(this object value) where T : Enum
    {
        return Enum.IsDefined(typeof(T), value);
    }

    public static HashSet<string> GetCombinations(this IEnumerable<IEnumerable<string?>> sources, int? maxCombinations = null, string separator = "|", string? nullReplacement = default)
    {
        // Convert to array for multiple enumeration and validation
        string?[][] sourcesArray = sources.Select(x => x.Any() ? x.Distinct().Select(x => x ?? nullReplacement).ToArray() : [nullReplacement]).ToArray();

        // Validate inputs
        if (!sourcesArray.AnyFast())
        {
            return [];
        }

        // Calculate total possible combinations
        long totalCombinations = sourcesArray.Aggregate(1L, (acc, curr) => acc * curr.Length);

        // Check if total combinations exceed max (if specified)
        if (maxCombinations.HasValue && (totalCombinations > maxCombinations.Value))
        {
            throw new ArgumentException($"Total possible combinations ({totalCombinations}) exceeds maximum allowed ({maxCombinations.Value})");
        }

        // Get the number of elements we're combining
        int length = sourcesArray.Length;

        // Create initial combination with first sequence
        List<List<string>> current = sourcesArray[0].Select(x => new[] { x?.ToString() ?? string.Empty }.ToList()).ToList();

        // Build up combinations for remaining sequences
        for (int i = 1; i < length; i++)
        {
            current = current.SelectMany(existingCombo => sourcesArray[i].Select(x => new List<string>(existingCombo) { x?.ToString() ?? string.Empty })).ToList();
        }

        // Convert the results to strings with separator and return as HashSet
        return new HashSet<string>(current.Select(x => string.Join(separator, x)));
    }
}

public sealed class ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter = oldParameter;
    private readonly ParameterExpression _newParameter = newParameter;

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return (node == _oldParameter) ? _newParameter : node;
    }
}

public sealed class ArrayTraverse
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

public class FixedFIFODictionary<TKey, TValue> : IDictionary<TKey, TValue?> where TKey : notnull
{
    private readonly Lock addLock = new();
    private readonly int capacity;
    private readonly OrderedDictionary<TKey, TValue?> dictionary;

    //private Queue<TKey> insertionOrderQueue;

    public FixedFIFODictionary(int capacity, IDictionary<TKey, TValue?>? sourceDictionary = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        if (sourceDictionary != null && sourceDictionary.Count > capacity)
        {
            throw new ArgumentException("Source dictionary exceeds the specified capacity.", nameof(sourceDictionary));
        }

        this.capacity = capacity;
        dictionary = new OrderedDictionary<TKey, TValue?>(capacity);
        //insertionOrderQueue = new Queue<TKey>(capacity);

        if (sourceDictionary != null)
        {
            foreach (KeyValuePair<TKey, TValue?> kvp in sourceDictionary)
            {
                dictionary[kvp.Key] = kvp.Value;
                //insertionOrderQueue.Enqueue(kvp.Key);
            }
        }
    }

    public ICollection<TKey> Keys => dictionary.Keys;

    public ICollection<TValue?> Values => dictionary.Values;

    public int Count => dictionary.Count;

    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).IsReadOnly;

    public TValue? this[TKey key]
    {
        get
        {
            lock (addLock)
            {
                return dictionary[key];
            }
        }
        set
        {
            lock (addLock)
            {
                if (dictionary.ContainsKey(key))
                {
                    // Update existing item
                    dictionary[key] = value;
                    // Note: We're not changing its position in the queue
                }
                else
                {
                    // Add new item
                    if (dictionary.Count >= capacity)
                    {
                        // Remove oldest item
                        //TKey oldestKey = insertionOrderQueue.Dequeue();
                        TKey oldestKey = dictionary.GetAt(0).Key;
                        dictionary.Remove(oldestKey);
                    }
                    dictionary[key] = value;
                    //insertionOrderQueue.Enqueue(key);
                }
            }
        }
    }

    public bool ContainsKey(TKey key)
    {
        return dictionary.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        return dictionary.TryGetValue(key, out value);
    }

    public void Clear()
    {
        dictionary.Clear();
        //insertionOrderQueue.Clear();
    }

    public void Add(TKey key, TValue? value)
    {
        dictionary.Add(key, value);
        //insertionOrderQueue.Enqueue(key);
    }

    public bool Remove(TKey key)
    {
        //if (dictionary.Remove(key))
        //{
        //    insertionOrderQueue = new Queue<TKey>(insertionOrderQueue.Where(k => !EqualityComparer<TKey>.Default.Equals(k, key)));
        //    return true;
        //}
        //return false;
        return dictionary.Remove(key);
    }

    public void Add(KeyValuePair<TKey, TValue?> item)
    {
        ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Add(item);
        //insertionOrderQueue.Enqueue(item.Key);
    }

    public bool Contains(KeyValuePair<TKey, TValue?> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue?>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue?> item)
    {
        //if (((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Remove(item))
        //{
        //    insertionOrderQueue = new Queue<TKey>(insertionOrderQueue.Where(k => !EqualityComparer<TKey>.Default.Equals(k, item.Key)));
        //    return true;
        //}
        //return false;
        return ((ICollection<KeyValuePair<TKey, TValue?>>)dictionary).Remove(item);
    }

    public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<TKey, TValue?>>)dictionary).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)dictionary).GetEnumerator();
    }
}
