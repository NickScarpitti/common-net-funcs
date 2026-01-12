using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;

using static System.Convert;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static partial class Collections
{
	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="collection">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="collection"/> has any objects in it.</returns>
	public static bool AnyFast<T>([NotNullWhen(true)] this ICollection<T>? collection)
	{
		return collection?.Count > 0;
	}

	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="list">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="list"/> has any objects in it.</returns>
	public static bool AnyFast<T>([NotNullWhen(true)] this IList<T>? list)
	{
		return list?.Count > 0;
	}

	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="bag">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="bag"/> has any objects in it.</returns>
	public static bool AnyFast<T>([NotNullWhen(true)] this ConcurrentBag<T>? bag)
	{
		return bag?.IsEmpty == false;
	}

	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="array">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="array"/> has any objects in it.</returns>
	public static bool AnyFast<T>([NotNullWhen(true)] this T[]? array)
	{
		return array?.Length > 0;
	}

	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="TKey">Dictionary key type.</typeparam>
	/// <typeparam name="T">Dictionary value type.</typeparam>
	/// <param name="dict">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="dict"/> has any objects in it.</returns>
	public static bool AnyFast<TKey, T>([NotNullWhen(true)] this IDictionary<TKey, T>? dict) where TKey : notnull
	{
		return dict?.Count > 0;
	}

	/// <summary>
	/// Faster alternative to using the .Any() linq method to address the CA1860 code issue.
	/// </summary>
	/// <typeparam name="TKey">Dictionary key type.</typeparam>
	/// <typeparam name="T">Dictionary value type.</typeparam>
	/// <param name="dict">Collection being checked for having elements.</param>
	/// <returns><see langword="true"/> if <paramref name="dict"/> has any objects in it.</returns>
	public static bool AnyFast<TKey, T>([NotNullWhen(true)] this ConcurrentDictionary<TKey, T>? dict) where TKey : notnull
	{
		return dict?.IsEmpty == false;
	}

	/// <summary>
	/// Provides a safe way to add a new Dictionary key without having to worry about duplication.
	/// </summary>
	/// <param name="dict">Dictionary to add item to.</param>
	/// <param name="keyValuePair">Key value pair to add to <paramref name="dict"/>.</param>
	public static void AddDictionaryItem<K, V>(this IDictionary<K, V> dict, KeyValuePair<K, V> keyValuePair) where K : notnull
	{
		dict.TryAdd(keyValuePair.Key, keyValuePair.Value);
	}

	/// <summary>
	/// Provides a safe way to add a new <see cref="KeyValuePair{TKey, TValue}"/> to an <see cref="IDictionary{TKey, TValue}"/> key without having to worry about duplication.
	/// </summary>
	/// <param name="dict">Dictionary to add item to.</param>
	/// <param name="keyValuePairs">Enumerable of items to add to <paramref name="dict"/>.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	public static void AddDictionaryItems<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> keyValuePairs, CancellationToken cancellationToken = default) where K : notnull
	{
		foreach (KeyValuePair<K, V> keyValuePair in keyValuePairs)
		{
			cancellationToken.ThrowIfCancellationRequested();
			dict.TryAdd(keyValuePair.Key, keyValuePair.Value);
		}
	}

	/// <summary>
	/// Adds AddRange functionality to <see cref="ConcurrentBag{T}"/> similar to a <see cref="List{T}"/>.
	/// </summary>
	/// <remarks>Null items are not added to the <paramref name="concurrentBag"/></remarks>
	/// <typeparam name="T">Type of object being added</typeparam>
	/// <param name="concurrentBag"><see cref="ConcurrentBag{T}"/> to add list of items to.</param>
	/// <param name="toAdd">Items to add to <paramref name="concurrentBag"/>.</param>
	/// <param name="parallelOptions">ParallelOptions for Parallel.ForEach.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	public static void AddRangeParallel<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, ParallelOptions? parallelOptions = null, CancellationToken cancellationToken = default)
	{
		Parallel.ForEach(toAdd.SelectNonNull(), parallelOptions ?? new(), item =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			concurrentBag.Add(item!);
		});
	}

	/// <summary>
	/// Adds AddRange functionality to <see cref="ConcurrentBag{T}"/> similar to a <see cref="List{T}">.
	/// </summary>
	/// <remarks>Null items are not added to the <paramref name="concurrentBag"/>.</remarks>
	/// <typeparam name="T">Type of object being added.</typeparam>
	/// <param name="concurrentBag"><see cref="ConcurrentBag{T}"/> to add list of items to.</param>
	/// <param name="toAdd">Items to add to <paramref name="concurrentBag"/>.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	public static void AddRange<T>(this ConcurrentBag<T> concurrentBag, IEnumerable<T?> toAdd, CancellationToken cancellationToken = default)
	{
		foreach (T? item in toAdd.SelectNonNull())
		{
			cancellationToken.ThrowIfCancellationRequested();
			concurrentBag.Add(item!);
		}
	}

	/// <summary>
	/// Adds AddRange functionality to <see cref="HashSet{T}"/> similar to a <see cref="List{T}"/>. Skips null items
	/// </summary>
	/// <remarks>Null items are not added to the <paramref name="hashSet"/></remarks>
	/// <typeparam name="T">Type of object being added</typeparam>
	/// <param name="hashSet"><see cref="HashSet{T}"/> to add list of items to</param>
	/// <param name="toAdd">Items to add to <paramref name="hashSet"/></param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T?> toAdd, CancellationToken cancellationToken = default)
	{
		foreach (T? item in toAdd.SelectNonNull())
		{
			cancellationToken.ThrowIfCancellationRequested();
			hashSet.Add(item!);
		}
	}

	/// <summary>
	/// Set values in an <see cref="IEnumerable{T}"/> as an extension of linq.
	/// </summary>
	/// <typeparam name="T">Type of object having values set.</typeparam>
	/// <param name="items">Items to have the updateMethod expression performed on.</param>
	/// <param name="updateMethod">Lambda expression of the action to perform.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="IEnumerable{T}"> with values updated according to <paramref name="updateMethod"/></returns>
	public static void SetValue<T>(this IEnumerable<T> items, Action<T> updateMethod, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(updateMethod);

		foreach (T item in items)
		{
			cancellationToken.ThrowIfCancellationRequested();
			updateMethod(item);
		}
	}

	/// <summary>
	/// Set values in an <see cref="IEnumerable{T}"/> as an extension of linq.
	/// </summary>
	/// <param name="items">Items to have the updateMethod expression performed on.</param>
	/// <param name="updateMethod">Lambda expression of the action to perform.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="IEnumerable{T}"/> with values updated according to <paramref name="updateMethod"/>.</returns>
	public static void SetValue(this IEnumerable<string?> items, Func<string?, string?> updateMethod, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(updateMethod);

		IList<string?> list = items as IList<string?> ?? items.ToList();

		for (int i = 0; i < list.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			list[i] = updateMethod(list[i]);
		}
	}

	/// <summary>
	/// <para>Allows you to act upon every element in <paramref name="array"/></para>
	/// <para>Used like outerArray.SetValue((array, indices) => array.SetValue(SomeMethod(outerArray.GetValue(indices)), indices))</para>
	/// </summary>
	/// <param name="array">Array to act upon.</param>
	/// <param name="updateMethod">Action to perform on each element in <paramref name="array"/>.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	public static void SetValue(this Array array, Action<Array, int[]> updateMethod, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(array);
		ArgumentNullException.ThrowIfNull(updateMethod);

		if (array.LongLength == 0)
		{
			return;
		}
		ArrayTraverse walker = new(array);
		do
		{
			cancellationToken.ThrowIfCancellationRequested();
			updateMethod(array, walker.Position);
		} while (walker.Step());
	}

	/// <summary>
	/// Set values in an <see cref="IEnumerable{T}"/> as an extension of linq.
	/// </summary>
	/// <typeparam name="T">Type of object having values set.</typeparam>
	/// <param name="items">Items to have the updateMethod expression performed on.</param>
	/// <param name="updateMethod">Lambda expression of the action to perform.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="IEnumerable{T}"> with values updated according to <paramref name="updateMethod"/></returns>
	public static IEnumerable<T> SetValueEnumerate<T>(this IEnumerable<T> items, Action<T> updateMethod, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(updateMethod);

		return Enumerate();

		IEnumerable<T> Enumerate()
		{
			foreach (T item in items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				updateMethod(item);
				yield return item;
			}
		}
	}

	/// <summary>
	/// Set values in an <see cref="IEnumerable{T}"/> as an extension of linq.
	/// </summary>
	/// <param name="items">Items to have the updateMethod expression performed on.</param>
	/// <param name="updateMethod">Lambda expression of the action to perform.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="IEnumerable{T}"/> with values updated according to <paramref name="updateMethod"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> or <paramref name="updateMethod"/> are null</exception>
	public static IEnumerable<string?> SetValueEnumerate(this IEnumerable<string?> items, Func<string?, string?> updateMethod, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(updateMethod);

		List<string?> list = items.ToList();

		return Enumerate();

		IEnumerable<string?> Enumerate()
		{
			for (int i = 0; i < list.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				list[i] = updateMethod(list[i]);
				yield return list[i];
			}
		}
	}

	/// <summary>
	/// Set values in an <see cref="IEnumerable{T}"/> as an extension of linq using a Parallel.ForEach loop.
	/// </summary>
	/// <typeparam name="T">Type of object having values set.</typeparam>
	/// <param name="items">Items to have the updateMethod expression performed on.</param>
	/// <param name="updateMethod">Lambda expression of the action to perform.</param>
	/// <param name="maxDegreeOfParallelism">Integer setting the max number of parallel operations allowed. Default of -1 allows maximum possible.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="IEnumerable{T}"/> with values updated according to <paramref name="updateMethod"/>.</returns>
	public static void SetValueParallel<T>(this IEnumerable<T> items, Action<T> updateMethod, int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(updateMethod);

		Parallel.ForEach(items, new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, item =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			updateMethod(item);
		});
	}

	/// <summary>
	/// Select only strings that are not <see langword="null"/>, empty, or only whitespace.
	/// </summary>
	/// <param name="items">Enumerable of strings to select from.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> containing all string values from <paramref name="items"/> that are not null, empty, or only whitespace.</returns>
	[return: NotNullIfNotNull(nameof(items))]
	public static IEnumerable<string>? SelectNonEmpty(this IEnumerable<string?>? items)
	{
		return items?.Where(x => !x.IsNullOrWhiteSpace()).Select(x => x!);
	}

	/// <summary>
	/// Select only objects that are not <see langword="null"/>.
	/// </summary>
	/// <param name="items">Enumerable of objects to select from.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> containing all object values from <paramref name="items"/> that are not <see langword="null"/>.</returns>
	[return: NotNullIfNotNull(nameof(items))]
	public static IEnumerable<T>? SelectNonNull<T>(this IEnumerable<T?>? items)
	{
		return items?.Where(x => x != null!).Select(x => x!);
	}

	/// <summary>
	/// Create a single item <see cref="List{T}"/> from an object.
	/// </summary>
	/// <remarks>Null safe, returns an empty list if <paramref name="obj"/> is <see langword="null"/>.</remarks>
	/// <typeparam name="T">Type to use in list.</typeparam>
	/// <param name="obj">Object to turn into a single item list.</param>
	/// <returns>A <see cref="List{T}"/> containing the single item or an empty <see cref="List{T}"> if the item is <see langword="null"/>.</returns>
	public static List<T> SingleToList<T>(this T? obj)
	{
		return obj != null! ? [obj!] : [];
	}

	/// <summary>
	/// Create a single item <see cref="List{T}"/> from an object.
	/// </summary>
	/// <remarks>Null safe, returns an empty list if <paramref name="s"/> is <see langword="null"/>.</remarks>
	/// <param name="s">Object to turn into a single item list.</param>
	/// <param name="allowEmptyValues">Optional: If <see langword="true"/>, allows empty strings in the list, otherwise they are excluded. Default is <see langword="false"/>.</param>
	/// <returns>A <see cref="List{T}"/> containing the single item or an empty <see cref="List{T}"> if the item is <see langword="null"/>.</returns>
	public static List<string> SingleToList(this string? s, bool allowEmptyValues = false)
	{
		return !allowEmptyValues ? (!s.IsNullOrWhiteSpace()) ? [s] : [] : s != null ? [s] : [];
	}

	/// <summary>
	/// Select object from an <see cref="IQueryable{T}"/> by matching all non-null fields to an object of the same type comprising the collection.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="queryable">Queryable collection to select from</param>
	/// <param name="partialObject">Object with fields to match with objects in the queryable collection</param>
	/// <param name="ignoreDefaultValues">Optional: Ignore default values in retrieval when true. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns>First object that matches all non-null fields in <paramref name="partialObject"/></returns>
	public static T? GetObjectByPartial<T>(this IQueryable<T> queryable, T partialObject, bool ignoreDefaultValues = false, CancellationToken cancellationToken = default) where T : class
	{
		// Get the properties of the object using reflection
		//PropertyInfo[] properties = typeof(TObj).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T));

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

			//if (partialValue is DateTime dateTimeValue)
			//{
			//	partialValue = dateTimeValue.ToUniversalTime();
			//}

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
	/// Clears the <see cref="List{T}"/>, shrinks the capacity back to the default, and optionally performs optimized garbage collection.
	/// </summary>
	/// <param name="list">The list to run the Clear and TrimExcess actions on.</param>
	/// <param name="forceGc">If true, requests optimized garbage collection targeting generation 2 (large objects) after clearing and trimming. Use only when dealing with very large collections where immediate memory reclamation is critical.</param>
	public static void ClearTrim<T>(this List<T>? list, bool forceGc = false)
	{
		if (list == null)
		{
			return;
		}
		list.Clear();
		list.TrimExcess();

		if (forceGc)
		{
			GC.Collect(2, GCCollectionMode.Optimized, false);
		}
	}

	/// <summary>
	/// Clears the <see cref="Dictionary{TKey, TValue}"/>, shrinks the capacity back to the default, and optionally performs optimized garbage collection.
	/// </summary>
	/// <param name="dict">The dictionary to run the Clear and TrimExcess actions on.</param>
	/// <param name="forceGc">If true, requests optimized garbage collection targeting generation 2 (large objects) after clearing and trimming. Use only when dealing with very large collections where immediate memory reclamation is critical.</param>
	public static void ClearTrim<TKey, TValue>(this Dictionary<TKey, TValue>? dict, bool forceGc = false) where TKey : notnull
	{
		if (dict == null)
		{
			return;
		}
		dict.Clear();
		dict.TrimExcess();

		if (forceGc)
		{
			GC.Collect(2, GCCollectionMode.Optimized, false);
		}
	}

	/// <summary>
	/// Clears the <see cref="HashSet{T}"/>, shrinks the capacity back to the default, and optionally performs optimized garbage collection.
	/// </summary>
	/// <param name="hashSet">The hashSet to run the Clear and TrimExcess actions on.</param>
	/// <param name="forceGc">If true, requests optimized garbage collection targeting generation 2 (large objects) after clearing and trimming. Use only when dealing with very large collections where immediate memory reclamation is critical.</param>
	public static void ClearTrim<T>(this HashSet<T>? hashSet, bool forceGc = false)
	{
		if (hashSet == null)
		{
			return;
		}
		hashSet.Clear();
		hashSet.TrimExcess();

		if (forceGc)
		{
			GC.Collect(2, GCCollectionMode.Optimized, false);
		}
	}

	/// <summary>
	/// Clears the <see cref="Stack{T}"/>, shrinks the capacity back to the default, and optionally performs optimized garbage collection.
	/// </summary>
	/// <param name="stack">The stack to run the Clear and TrimExcess actions on.</param>
	/// <param name="forceGc">If true, requests optimized garbage collection targeting generation 2 (large objects) after clearing and trimming. Use only when dealing with very large collections where immediate memory reclamation is critical.</param>
	public static void ClearTrim<T>(this Stack<T>? stack, bool forceGc = false)
	{
		if (stack == null)
		{
			return;
		}
		stack.Clear();
		stack.TrimExcess();

		if (forceGc)
		{
			GC.Collect(2, GCCollectionMode.Optimized, false);
		}
	}

	/// <summary>
	/// Clears the <see cref="Queue{T}"/>, shrinks the capacity back to the default, and optionally performs optimized garbage collection.
	/// </summary>
	/// <param name="queue">The queue to run the Clear and TrimExcess actions on.</param>
	/// <param name="forceGc">If true, requests optimized garbage collection targeting generation 2 (large objects) after clearing and trimming. Use only when dealing with very large collections where immediate memory reclamation is critical.</param>
	public static void ClearTrim<T>(this Queue<T>? queue, bool forceGc = false)
	{
		if (queue == null)
		{
			return;
		}
		queue.Clear();
		queue.TrimExcess();

		if (forceGc)
		{
			GC.Collect(2, GCCollectionMode.Optimized, false);
		}
	}

	/// <summary>
	/// Convert <see cref="DataTable"/> to equivalent <see cref="List{T}"/> of specified <see langword="class"/>.
	/// </summary>
	/// <typeparam name="T">Class to use in table conversion.</typeparam>
	/// <param name="table">Table to convert to list.</param>
	/// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="List{T}"/> containing the values from <see cref="DataTable"/> as the specified <see langword="class"/>.</returns>
	public static List<T> ToList<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
	{
		List<T> list = new(table.Rows.Count);
		if (table.Rows.Count > 0)
		{
			IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
			foreach (DataRow row in table.AsEnumerable())
			{
				cancellationToken.ThrowIfCancellationRequested();
				T? rowData = row.ParseRowValues<T>(map, cancellationToken);
				if (!EqualityComparer<T?>.Default.Equals(rowData, default))
				{
					list.Add(rowData);
				}
			}
		}
		return list;
	}

	/// <summary>
	/// Convert <see cref="DataTable"/> to equivalent <see cref="List{T}"/> of specified <see langword="class"/> using a Parallel.Foreach loop to get data from each row.
	/// </summary>
	/// <typeparam name="T">Class to use in table conversion.</typeparam>
	/// <param name="table">Table to convert to list.</param>
	/// <param name="maxDegreeOfParallelism">Parallelism parameter to be used in Parallel.Foreach loop.</param>
	/// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="List{T}"/> containing values from  <see cref="DataTable"/> as the specified <see langword="class"/></returns>
	public static List<T> ToListParallel<T>(this DataTable table, int maxDegreeOfParallelism = -1, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
	{
		ConcurrentBag<T> bag = [];
		if (table.Rows.Count > 0)
		{
			IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
			Parallel.ForEach(table.AsEnumerable(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, row =>
			{
				T? rowData = row.ParseRowValues<T>(map, cancellationToken);
				if (!EqualityComparer<T?>.Default.Equals(rowData, default))
				{
					bag.Add(rowData);
				}
			});
		}
		return bag.ToList();
	}

	/// <summary>
	/// Convert <see cref="DataTable"/> to equivalent <see cref="List{T}"/> of specified <see langword="class"/> using a Parallel.Foreach loop to get data from each row.
	/// </summary>
	/// <typeparam name="T">Class to use in table conversion.</typeparam>
	/// <param name="table">Table to convert to list.</param>
	/// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="List{T}"/> containing table values as the specified <see langword="class"/>.</returns>
	public static IEnumerable<T> ToEnumerableParallel<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
	{
		IEnumerable<T> values = [];
		if (table.Rows.Count > 0)
		{
			IReadOnlyList<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = table.GetDataTableMap<T>(convertShortToBool, cancellationToken: cancellationToken);
			values = table.AsEnumerable().AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(row => row.ParseRowValues<T>(map, cancellationToken)!).Where(x => !EqualityComparer<T?>.Default.Equals(x, default));
		}
		return values;
	}

	/// <summary>
	/// Convert <see cref="DataTable"/> to equivalent <see cref="List{T}"/> of specified <see langword="class"/> using a Parallel.Foreach loop to get data from each row.
	/// </summary>
	/// <typeparam name="T">Class to use in table conversion.</typeparam>
	/// <param name="table">Table to convert to list.</param>
	/// <param name="convertShortToBool">Optional: Allow checking for parameters that are short values in the table that correlate to a bool parameter when true. Default is false.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns><see cref="List{T}"/> containing table values as the specified <see langword="class"/>.</returns>
	public static IEnumerable<T> ToEnumerableStreaming<T>(this DataTable table, bool convertShortToBool = false, CancellationToken cancellationToken = default) where T : class, new()
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
				//outstandingItem.Start();
				outstandingItem.Start();

				if (tmp?.Result != null)
				{
					yield return tmp.Result;
				}
			}

			if (outstandingItem?.Result != null)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return outstandingItem.Result;
			}
		}
	}

	/// <summary>
	/// Get the mapping between DataTable columns and class properties for a specific class type.
	/// </summary>
	/// <typeparam name="T">Type of class to map.</typeparam>
	/// <param name="table">DataTable to map.</param>
	/// <param name="convertShortToBool">Allow checking for parameters that are short values in the table that correlate to a bool parameter when true.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for the operation.</param>
	/// <returns>Mapping between DataTable columns and class properties.</returns>
	private static List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> GetDataTableMap<T>(this DataTable table, bool convertShortToBool, CancellationToken cancellationToken = default)
	{
		List<(DataColumn DataColumn, PropertyInfo PropertyInfo, bool IsShort)> map = [];
		DataRow firstRow = table.Rows[0];
		foreach (PropertyInfo propertyInfo in GetOrAddPropertiesFromReflectionCache(typeof(T)))
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

	/// <summary>
	/// Get the mapping between row values and <see langword="class"/> properties for a specific <see langword="class"/> type.
	/// </summary>
	/// <typeparam name="T">Type of class to map.</typeparam>
	/// <param name="row">DataRow to map.</param>
	/// <param name="map">Mapping between DataTable columns and class properties.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for the operation.</param>
	/// <returns>Mapped <see langword="class"/> instance.</returns>
	/// <exception cref="InvalidCastException"></exception>
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
						else if (DateOnly.TryParse((string)value, CultureInfo.InvariantCulture, out DateOnly dateOnlyValue))
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
						else if (DateTime.TryParse((string)value, CultureInfo.InvariantCulture, out DateTime dateTimeValue))
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
	/// Convert an <see cref="IEnumerable{T}"/> into equivalent <see cref="DataTable"/> object using expression trees.
	/// </summary>
	/// <typeparam name="T">Class to use in table creation.</typeparam>
	/// <param name="data">Collection to convert into a DataTable.</param>
	/// <param name="dataTable">DataTable to optionally insert data into.</param>
	/// <param name="useParallel">Optional: Parallelizes the conversion. Default is <see langword="false"/>.</param>
	/// <param name="approximateCount">Optional: Used for pre-allocating variable size when using parallelization, default is data.Count().</param>
	/// <param name="degreeOfParallelism">Optional: Used for setting number of parallel operations when using parallelization, default is -1 (#cores on machine).</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns>A <see cref="DataTable"/> representation of <paramref name="data"/>.</returns>
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

	///// <summary>
	///// Convert an <see cref="IEnumerable{TObj}"/> into equivalent <see cref="DataTable"/> object using expression trees.
	///// </summary>
	///// <typeparam name="TObj">Class to use in table creation.</typeparam>
	///// <param name="data">Collection to convert into a DataTable.</param>
	///// <param name="dataTable">DataTable to optionally insert data into.</param>
	///// <param name="useParallel">Optional: Parallelizes the conversion. Default is <see langword="false"/>.</param>
	///// <param name="approximateCount">Optional: Used for pre-allocating variable size when using parallelization, default is data.Count().</param>
	///// <param name="degreeOfParallelism">Optional: Used for setting number of parallel operations when using parallelization, default is -1 (#cores on machine).</param>
	///// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	///// <returns>A <see cref="DataTable"/> representation of <paramref name="data"/>.</returns>
	//[Obsolete("Please use ToDataTable<TObj>(this IEnumerable<TObj>? data, DataTable? dataTable = null, bool useExpressionTrees = true, bool useParallel = false, int? approximateCount = null, int degreeOfParallelism = -1, CancellationToken cancellationToken = default) instead", false)]
	//[return: NotNullIfNotNull(nameof(data))]
	//public static DataTable? ToDataTableReflection<TObj>(this IEnumerable<TObj>? data, DataTable? dataTable = null, bool useParallel = false, int? approximateCount = null, int degreeOfParallelism = -1, CancellationToken cancellationToken = default) where TObj : class, new()
	//{
	//	if (data == null)
	//	{
	//		return null;
	//	}

	//	dataTable ??= new();
	//	PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(TObj));

	//	// Remove invalid columns
	//	IEnumerable<string> propertyNames = properties.Select(x => x.Name);
	//	DataColumn[] columns = new DataColumn[dataTable.Columns.Count];
	//	dataTable.Columns.CopyTo(columns, 0);
	//	foreach (DataColumn? col in columns.Where(x => !propertyNames.Contains(x.ColumnName)))
	//	{
	//		dataTable.Columns.Remove(col.ColumnName);
	//	}

	//	// Create columns
	//	foreach (PropertyInfo? prop in properties.Where(x => !dataTable.Columns.Contains(x.Name)))
	//	{
	//		dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
	//	}

	//	// Add rows
	//	if (!useParallel)
	//	{
	//		foreach (TObj item in data.Where(x => x != null))
	//		{
	//			cancellationToken.ThrowIfCancellationRequested();
	//			DataRow row = dataTable.NewRow();
	//			foreach (PropertyInfo prop in properties)
	//			{
	//				row[prop.Name] = prop.GetValue(item) ?? System.DBNull.Value;
	//			}
	//			dataTable.Rows.Add(row);
	//		}
	//	}
	//	else
	//	{
	//		// Process items in parallel and collect results
	//		int columnCount = dataTable.Columns.Count;
	//		List<object[]> rows = new(approximateCount ?? data.Count());
	//		object lockObj = new();

	//		ParallelOptions options = new() { MaxDegreeOfParallelism = (degreeOfParallelism == -1) ? Environment.ProcessorCount : degreeOfParallelism };
	//		Parallel.ForEach(data, options, () => new List<object[]>(), (item, _, localRows) =>
	//		{
	//			cancellationToken.ThrowIfCancellationRequested();
	//			object[] rowValues = new object[columnCount];
	//			for (int i = 0; i < columnCount; i++)
	//			{
	//				rowValues[i] = properties[i].GetValue(item) ?? System.DBNull.Value;
	//			}

	//			localRows.Add(rowValues);
	//			return localRows;
	//		},
	//			localRows =>
	//			{
	//				lock (lockObj)
	//				{
	//					rows.AddRange(localRows);
	//				}
	//			});

	//		// Add all rows to the table
	//		foreach (object[] rowValues in rows)
	//		{
	//			cancellationToken.ThrowIfCancellationRequested();
	//			dataTable.Rows.Add(rowValues);
	//		}
	//	}

	//	return dataTable;
	//}

	private static readonly ConcurrentDictionary<Type, TypeAccessor> TypeAccessorCache = new();

	private sealed class TypeAccessor
	{
		public readonly DataColumnCollection ColumnDefinitions;
		public readonly Func<object, object>[] PropertyGetters;
		public readonly string[] PropertyNames;
		public readonly Type[] PropertyTypes;
		public readonly DataTable SchemaTable;

		public TypeAccessor(Type type)
		{
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead).ToArray();

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

		/// <summary>
		/// Creates a <see cref="Delegate"/> to get the value of a  <see langword="property"/> from an instance of a <see langword="class"/>.
		/// </summary>
		/// <param name="type">The type of the class.</param>
		/// <param name="property">The PropertyInfo for the property being accessed.</param>
		/// <returns>A <see cref="Delegate"/> that gets the property value.</returns>
		private static Func<object, object> CreatePropertyGetter(Type type, PropertyInfo property)
		{
			ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
			UnaryExpression convertInstance = Expression.Convert(instance, type);
			MemberExpression propertyAccess = Expression.Property(convertInstance, property);
			UnaryExpression convertProperty = Expression.Convert(propertyAccess, typeof(object));
			return Expression.Lambda<Func<object, object>>(convertProperty, instance).CompileFast();
		}
	}

	/// <summary>
	/// Convert an <see cref="IEnumerable{T}"/> into equivalent <see cref="DataTable"/> object using expression trees.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the collection.</typeparam>
	/// <param name="data">The collection to convert.</param>
	/// <param name="dataTable">The DataTable to populate.</param>
	/// <param name="useParallel">Whether to use parallel processing.</param>
	/// <param name="approximateCount">An optional estimate of the number of items in the collection. If null, <paramref name="data"/>.Count() will be used.</param>
	/// <param name="degreeOfParallelism">The degree of parallelism to use if <paramref name="useParallel"/> is true.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns>A <see cref="DataTable"/> representation of <paramref name="data"/>.</returns>
	private static DataTable ToDataTableExpressionTrees<T>(this IEnumerable<T> data, DataTable dataTable, bool useParallel, int? approximateCount, int degreeOfParallelism, CancellationToken cancellationToken = default) where T : class, new()
	{
		TypeAccessor typeAccessor = TypeAccessorCache.GetOrAdd(typeof(T), t => new TypeAccessor(t));

		// Remove invalid columns
		DataColumn[] columns = new DataColumn[dataTable.Columns.Count];
		dataTable.Columns.CopyTo(columns, 0);
		foreach (DataColumn? col in columns.Where(col => !typeAccessor.PropertyNames.Contains(col.ColumnName)))
		{
			dataTable.Columns.Remove(col.ColumnName);
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

		// Add the rows
		if (!useParallel)
		{
			foreach (T item in data)
			{
				object[] rowValues = new object[columnCount];
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
	/// Combine multiple expressions into a single expression.
	/// </summary>
	/// <param name="expressions">Enumerable containing at least one expression.</param>
	/// <returns>A single expression equivalent of the enumerated expressions passed in.</returns>
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
	/// Performs a string aggregation on the designated <see langword="property"/>, using all other properties as the group by.
	/// </summary>
	/// <param name="collection">Collection to perform the string aggregation on based on the property identified</param>
	/// <param name="propToAgg">Property to string aggregate</param>
	/// <param name="separator">Optional: String value used between aggregated values. Default is ';'.</param>
	/// <param name="distinct">Optional: Whether to include only distinct values in the aggregation. Default is true.</param>
	/// <param name="parallel">Optional: Whether to perform the aggregation in parallel or not. Default is <see langword="false"/>.</param>
	/// <returns><see cref="IEnumerable{T}"/> with specified <see langword="property"/> aggregated</returns>
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
	/// <param name="distinct">Optional: Whether to include only distinct values in the aggregation. Default is true.</param>
	/// <param name="parallel">Optional: Whether to perform the aggregation in parallel or not. Default is <see langword="false"/>.</param>
	/// <returns><see cref="IEnumerable{T}"/> with specified properties aggregated</returns>
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

		PropertyInfo[] properties = GetOrAddPropertiesFromReflectionCache(typeof(T));
		PropertyInfo[] groupingProperties = properties.Where(p => !propsToAgg.Contains(p.Name)).ToArray();

		return !groupingProperties.AnyFast() || (propsToAgg.Intersect(properties.Select(x => x.Name)).Count() < propsToAgg.Length)
			? throw new ArgumentException($"Invalid aggregate property values. All values in propsToAgg must be present in type {typeof(T)}", nameof(propsToAgg))
			: !parallel
			? collection.GroupBy(x => new { GroupKey = string.Join("|", groupingProperties.Select(y => y.GetValue(x)?.ToString() ?? string.Empty)) })
				//return collection.GroupBy(_ => new { GroupKey = string.Join("|", groupingProperties.Select(x => x.GetValue(x)?.ToString() ?? string.Empty)) })
				.Select(x =>
				{
					T result = new();
					foreach (PropertyInfo prop in properties)
					{
						if (propsToAgg.Contains(prop.Name))
						{
							string aggregatedValue = distinct ? string.Join(separator, x.Select(y => prop.GetValue(y)?.ToString() ?? string.Empty).Distinct()) :
												string.Join(separator, x.Select(y => prop.GetValue(y)?.ToString() ?? string.Empty));
							prop.SetValue(result, aggregatedValue);
						}
						else
						{
							prop.SetValue(result, prop.GetValue(x.First()));
						}
					}
					return result;
				})
			: collection.AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered)
				.GroupBy(x => new { GroupKey = string.Join("|", groupingProperties.Select(y => y.GetValue(x)?.ToString() ?? string.Empty)) })
				.Select(x =>
				{
					T result = new();
					foreach (PropertyInfo prop in properties)
					{
						if (propsToAgg.Contains(prop.Name))
						{
							string aggregatedValue = distinct ? string.Join(separator, x.Select(y => prop.GetValue(y)?.ToString() ?? string.Empty).Distinct()) :
												string.Join(separator, x.Select(y => prop.GetValue(y)?.ToString() ?? string.Empty));
							prop.SetValue(result, aggregatedValue);
						}
						else
						{
							prop.SetValue(result, prop.GetValue(x.First()));
						}
					}
					return result;
				});
	}

	/// <summary>
	/// Returns the index of the first occurrence of an object in a <see cref="IEnumerable{T}"/>.
	/// </summary>
	/// <param name="collection">IEnumerable to get index of value in</param>
	/// <param name="value">Value to get the index of</param>
	/// <returns>Index of the first occurrence of value in <paramref name="collection"/> or -1 if not found.</returns>
	public static int IndexOf<T>(this IEnumerable<T> collection, T value)
	{
		return collection.IndexOf(value, null);
	}

	/// <summary>
	/// Returns the index of the first occurrence of an object in a <see cref="IEnumerable{T}"/>, using the specified <see cref="IEqualityComparer{T}"/> for equality checks.
	/// </summary>
	/// <param name="collection">Collection to get index of object in</param>
	/// <param name="value">Value to check for equality in collection</param>
	/// <param name="comparer">Comparer used when checking for equality with value</param>
	/// <returns>Index of the first occurrence of value in <paramref name="collection"/> or -1 if not found</returns>
	public static int IndexOf<T>(this IEnumerable<T> collection, T value, IEqualityComparer<T>? comparer)
	{
		comparer ??= EqualityComparer<T>.Default;
		var found = collection.Select((a, i) => new { a, i }).FirstOrDefault(x => comparer.Equals(x.a, value));
		return (found == null) ? (-1) : found.i;
	}

	/// <summary>
	/// Checks to see if the value is in the <see langword="enum"/> type specified.
	/// </summary>
	/// <typeparam name="T">Enum to check against for validity.</typeparam>
	/// <param name="value">Value to check to see if it's in the specified enum.</param>
	/// <returns><see langword="true"/> if value is a valid value of the specified <see langword="enum"/>, otherwise false</returns>
	public static bool IsIn<T>(this object value) where T : Enum
	{
		return Enum.IsDefined(typeof(T), value);
	}

	/// <summary>
	/// Generates all possible combinations of the provided source collections.
	/// </summary>
	/// <param name="sources">The source collections to combine.</param>
	/// <param name="maxCombinations">Optional: The maximum number of combinations to generate. Default is <see langword="null"/> (no limit).</param>
	/// <param name="separator">Optional: String value used between aggregated values. Default is '|'.</param>
	/// <param name="nullReplacement">Optional: String value used to replace null values. Default is <see langword="null"/>.</param>
	/// <returns>A set of unique combinations generated from the source collections up to the quantity specified by <paramref name="maxCombinations"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="maxCombinations"/> is not null and is less than 1.</exception>
	public static HashSet<string> GetCombinations(this IEnumerable<IEnumerable<string?>> sources, int? maxCombinations = null, string separator = "|", string? nullReplacement = default)
	{
		if (maxCombinations.HasValue && maxCombinations < 1)
		{
			throw new ArgumentException("Max combinations must be either null, or greater than 0", nameof(maxCombinations));
		}

		// Convert to array for multiple enumeration and validation
		string?[][] sourcesArray = sources.Select(x => x.Any() ? x.Distinct().Select(x => x ?? nullReplacement).ToArray() : [nullReplacement]).ToArray();

		// Validate inputs
		if (!sourcesArray.AnyFast())
		{
			return [];
		}

		// Calculate total possible combinations
		//long totalCombinations = sourcesArray.Aggregate(1L, (acc, curr) => acc * curr.Length);

		// Get the number of elements we're combining
		int length = sourcesArray.Length;

		// Create initial combination with first sequence
		List<List<string>> current = sourcesArray[0].Select(x => new[] { x?.ToString() ?? string.Empty }.ToList()).ToList();

		// Build up combinations for remaining sequences
		for (int i = 1; i < length; i++)
		{
			current = current.SelectMany(existingCombo => sourcesArray[i].Select(x => new List<string>(existingCombo) { x?.ToString() ?? string.Empty })).ToList();
			if (maxCombinations.HasValue && current.Count >= maxCombinations) // Stop making combinations once reaching the maxCombinations
			{
				break;
			}
		}

		// Convert the results to strings with separator and return as HashSet
		return maxCombinations == null || !maxCombinations.HasValue || current.Count < maxCombinations
			? new HashSet<string>(current.Select(x => string.Join(separator, x)))
			: current.Select(x => string.Join(separator, x)).Take((int)maxCombinations).ToHashSet();
	}

	/// <summary>
	/// Get all or a limited quantity of all possible combinations of the provided source collections in a randomized order.
	/// </summary>
	/// <param name="sources">The source collections to combine.</param>
	/// <param name="maxCombinations">Optional: The maximum number of combinations to generate. Default is <see langword="null"/> (no limit).</param>
	/// <param name="separator">Optional: String value used between aggregated values. Default is '|'.</param>
	/// <param name="nullReplacement">Optional: String value used to replace null values. Default is <see langword="null"/>.</param>
	/// <returns>A set of unique combinations generated from the source collections up to the quantity specified by <paramref name="maxCombinations"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="maxCombinations"/> is not null and is less than 1.</exception>
	public static HashSet<string> GetRandomCombinations(this IEnumerable<IEnumerable<string?>> sources, int? maxCombinations = null, string separator = "|", string? nullReplacement = default)
	{
		if (maxCombinations is < 1)
		{
			throw new ArgumentException("Max combinations must be either null, or greater than 0", nameof(maxCombinations));
		}

		HashSet<string> allCombinations = sources.GetCombinations(null, separator, nullReplacement);
		if (maxCombinations != null && maxCombinations < allCombinations.Count)
		{
			return sources.GetCombinations(null, separator, nullReplacement).GetUniqueRandomElements((int)maxCombinations).ToHashSet(); // Select elements at random
		}
		return allCombinations.Shuffle().ToHashSet(); // Return all options in a randomized order
	}

	/// <summary>
	/// Generates a set of unique combinations from the provided source collections.
	/// </summary>
	/// <param name="sources">The source collections to combine.</param>
	/// <param name="maxCombinations">Optional: The maximum number of combinations to generate. Default is <see langword="null"/> (no limit).</param>
	/// <param name="separator">Optional: String value used between aggregated values. Default is '|'.</param>
	/// <param name="nullReplacement">Optional: String value used to replace null values. Default is <see langword="null"/>.</param>
	/// <returns>An <see cref="IEnumerable{T}"/> of unique combinations up to the quantity specified by <paramref name="maxCombinations"/>.</returns>
	/// <exception cref="ArgumentException"></exception>
	public static IEnumerable<string> GetEnumeratedCombinations(this IEnumerable<IEnumerable<string?>> sources, int? maxCombinations = null, string separator = "|", string? nullReplacement = default)
	{
		// Prepare the sources as arrays for efficient indexing
		string?[][] sourcesArray = sources.Select(x => x.Any() ? x.Distinct().Select(v => v ?? nullReplacement).ToArray() : [nullReplacement]).ToArray();

		if (!sourcesArray.AnyFast())
		{
			yield break;
		}

		//long totalCombinations = sourcesArray.Aggregate(1L, (acc, curr) => acc * curr.Length);
		//if (maxCombinations.HasValue && totalCombinations > maxCombinations.Value)
		//{
		//    throw new ArgumentException($"Total possible combinations ({totalCombinations}) exceeds maximum allowed ({maxCombinations.Value})");
		//}

		HashSet<string> yielded = new();

		IEnumerable<string> Recurse(int depth, List<string> current)
		{
			if (depth == sourcesArray.Length)
			{
				string combination = string.Join(separator, current);
				if (yielded.Add(combination) && (maxCombinations == null || yielded.Count <= maxCombinations)) // Break if we've yielded enough combinations
				{
					yield return combination;
				}

				yield break;
			}

			foreach (string? value in sourcesArray[depth])
			{
				current.Add(value?.ToString() ?? string.Empty);
				foreach (string result in Recurse(depth + 1, current))
				{
					yield return result;
				}

				current.RemoveAt(current.Count - 1);
			}
		}

		foreach (string combination in Recurse(0, new List<string>(sourcesArray.Length)))
		{
			yield return combination;
		}
	}
}

/// <summary>
/// Replaces occurrences of a specified parameter in an expression tree with a new parameter.
/// </summary>
/// <param name="oldParameter">Parameter to be replaced by <paramref name="newParameter"/></param>
/// <param name="newParameter">New parameter to replace the <paramref name="oldParameter"/>.</param>
public sealed class ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter) : ExpressionVisitor
{
	private readonly ParameterExpression _oldParameter = oldParameter;
	private readonly ParameterExpression _newParameter = newParameter;

	protected override Expression VisitParameter(ParameterExpression node)
	{
		return (node == _oldParameter) ? _newParameter : node;
	}
}

/// <summary>
/// Traverses a multi-dimensional array.
/// </summary>
public sealed class ArrayTraverse
{
	private readonly int[] maxLengths;

	public int[] Position { get; set; }

	public ArrayTraverse(Array array)
	{
		maxLengths = new int[array.Rank];
		for (int i = 0; i < array.Rank; ++i)
		{
			maxLengths[i] = array.GetLength(i) - 1;
		}
		Position = new int[array.Rank];
	}

	/// <summary>
	/// Move to the next element in the array
	/// </summary>
	/// <returns>True if able to move to next element in the array, otherwise false, indicating the end of the array has been reached.</returns>
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
