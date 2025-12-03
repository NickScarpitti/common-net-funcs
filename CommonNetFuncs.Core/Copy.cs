using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static class Copy
{
	#region Caching

	private static readonly CacheManager<(Type Source, Type Dest), Func<object, object?, int, int, object?>> DeepCopyCache = new();

	public static ICacheManagerApi<(Type Source, Type Dest), Func<object, object?, int, int, object?>> DeepCopyCacheManager => DeepCopyCache;

	private static readonly CacheManager<(Type SourceType, Type DestType), Dictionary<string, (Delegate Set, Delegate Get)>> CopyCache = new();

	public static ICacheManagerApi<(Type SourceType, Type DestType), Dictionary<string, (Delegate Set, Delegate Get)>> CopyCacheManager => CopyCache;

	/// <summary>
	/// Gets or adds a function from the deep copy cache based on the source and destination types.
	/// </summary>
	/// <param name="key">Cache key</param>
	/// <returns>Function for executing deep copy</returns>
	private static Func<object, object?, int, int, object?> GetOrAddFunctionFromDeepCopyCache((Type sourceType, Type destType) key)
	{
		if (DeepCopyCacheManager.IsUsingLimitedCache() ? DeepCopyCacheManager.GetLimitedCache().TryGetValue(key, out Func<object, object?, int, int, object?>? function) :
						DeepCopyCacheManager.GetCache().TryGetValue(key, out function))
		{
			return function!;
		}

		function = CreateCopyFunction(key.sourceType, key.destType);
		if (DeepCopyCacheManager.IsUsingLimitedCache())
		{
			DeepCopyCacheManager.TryAddLimitedCache(key, function);
		}
		else
		{
			DeepCopyCacheManager.TryAddCache(key, function);
		}

		return function;
	}

	/// <summary>
	/// Gets or adds a function from the shallow copy cache based on the source and destination types.
	/// </summary>
	/// <returns>Function for executing shallow copy</returns>
	private static Dictionary<string, (Delegate Set, Delegate Get)> GetOrAddFunctionFromCopyCache<TSource, TDest>()
	{
		(Type, Type) key = (typeof(TSource), typeof(TDest));
		bool isLimitedCache = CopyCacheManager.IsUsingLimitedCache();
		if (isLimitedCache ? CopyCacheManager.GetLimitedCache().TryGetValue(key, out Dictionary<string, (Delegate Set, Delegate Get)>? functions) :
						CopyCacheManager.GetCache().TryGetValue(key, out functions))
		{
			return functions!;
		}

		if (isLimitedCache)
		{
			return CopyCacheManager.GetOrAddLimitedCache(key, _ => CreatePropertyMappingsForCache<TSource, TDest>());
		}
		return CopyCacheManager.GetOrAddCache(key, _ => CreatePropertyMappingsForCache<TSource, TDest>());
	}

	#endregion

	/// <summary>
	/// Copy properties of the same name from one object to another
	/// </summary>
	/// <typeparam name="T">Type of source object</typeparam>
	/// <typeparam name="UT">Type of destination object</typeparam>
	/// <param name="source">Object to copy common properties from</param>
	/// <param name="dest">Object to copy common properties to</param>
	/// <param name="useCache">Optional: If <see langword="true"/>, will use cached property mappings. Default is <see langword="true"/></param>
	public static void CopyPropertiesTo<T, UT>(this T source, UT dest, bool useCache = true) where T : class? where UT : class?
	{
		if (source == null)
		{
			return;
		}
		if (useCache)
		{
			if (dest == null)
			{
				return;
			}

			foreach ((Action<UT, object?> Set, Func<T, object?> Get) in GetOrCreatePropertyMaps<T, UT>().Values)
			{
				Set(dest, Get(source));
			}
		}
		else
		{
			dest ??= Activator.CreateInstance<UT>();
			IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead);
			Dictionary<string, PropertyInfo> destPropDict = GetOrAddPropertiesFromReflectionCache(typeof(UT)).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

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
	/// <param name="useCache">Optional: If <see langword="true"/>, will use cached property mappings. Default is <see langword="true"/></param>
	/// <returns>A new instance of T with properties copied from <paramref name="source"/></returns>
	public static T CopyPropertiesToNew<T>(this T source, bool useCache = true) where T : class?, new()
	{
		if (source == null)
		{
			return default!;
		}

		T dest = new();
		if (useCache)
		{
			foreach ((Action<T, object?> Set, Func<T, object?> Get) in GetOrCreatePropertyMaps<T, T>().Values)
			{
				Set(dest, Get(source));
			}
		}
		else
		{
			foreach (PropertyInfo sourceProp in GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead))
			{
				sourceProp.SetValue(dest, sourceProp.GetValue(source, null), null);
			}
		}
		return dest;
	}

	/// <summary>
	/// Copy properties of the same name from one object to another
	/// </summary>
	/// <typeparam name="T">Type of object being copied</typeparam>
	/// <param name="source">Object to copy common properties from</param>
	/// <param name="useCache">Optional: If <see langword="true"/>, will use cached property mappings. Default is <see langword="true"/></param>
	/// <returns>A new instance of UT with properties copied from <paramref name="source"/></returns>
	public static UT CopyPropertiesToNew<T, UT>(this T source, bool useCache = true) where T : class where UT : class, new()
	{
		IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead);
		Dictionary<string, PropertyInfo> destPropDict = GetOrAddPropertiesFromReflectionCache(typeof(UT)).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

		UT dest = new();
		if (useCache)
		{
			foreach ((Action<UT, object?> Set, Func<T, object?> Get) in GetOrCreatePropertyMaps<T, UT>().Values)
			{
				Set(dest, Get(source));
			}
		}
		else
		{
			foreach (PropertyInfo sourceProp in sourceProps)
			{
				if (destPropDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp) && destProp.PropertyType == sourceProp.PropertyType)
				{
					destProp.SetValue(dest, sourceProp.GetValue(source, null), null);
				}
			}
		}
		return dest;
	}

	// Can handle collections
	/// <summary>
	/// Copies properties of one class to a new instance of a <see langword="class"/> using reflection based on property name matching
	/// </summary>
	/// <typeparam name="T">Type to copy values from</typeparam>
	/// <typeparam name="UT">Type to copy values to</typeparam>
	/// <param name="source">Object to copy values into new object from</param>
	/// <param name="maxDepth">Optional: How deep to recursively traverse. Default = -1 which is unlimited recursion.</param>
	/// <param name="useCache">Optional: If <see langword="true"/>, will use cached property mappings. Default is <see langword="true"/></param>
	/// <returns>A new instance of UT with properties of the same name from source populated.</returns>
	[return: NotNullIfNotNull(nameof(source))]
	public static UT? CopyPropertiesToNewRecursive<T, UT>(this T source, int maxDepth = -1, bool useCache = true) where T : class where UT : class, new()
	{
		if (source == null)
		{
			return default;
		}

		if (useCache)
		{
			return CopyPropertiesToNewRecursiveExpressionTrees<T, UT>(source, maxDepth)!;
		}
		else
		{
			if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(IEnumerable).IsAssignableFrom(typeof(UT)) && (typeof(T) != typeof(string)) && (typeof(UT) != typeof(string)))
			{
				return (UT?)CopyCollection(source, typeof(UT), maxDepth) ?? new();
			}

			return (UT?)CopyObject(source, typeof(UT), 0, maxDepth) ?? new();
		}
	}

	/// <summary>
	/// <para>Merge the field values from one instance into another of the same object</para>
	/// <para>Only default values will be overridden by mergeFromObjects</para>
	/// </summary>
	/// <typeparam name="T">Object type to merge instances of.</typeparam>
	/// <param name="mergeIntoObject">Object to merge properties into.</param>
	/// <param name="mergeFromObjects">Objects to merge properties from.</param>
	/// <param name="cancellationToken">Cancellation token for this operation.</param>
	/// <returns>The merged object.</returns>
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
	/// <typeparam name="T">Object type to merge instances of.</typeparam>
	/// <param name="mergeIntoObject">Object to merge properties into.</param>
	/// <param name="mergeFromObject">Object to merge properties from.</param>
	/// <param name="cancellationToken">Cancellation token for this operation.</param>
	/// <returns>The merged object.</returns>
	public static T MergeInstances<T>(this T mergeIntoObject, T mergeFromObject, CancellationToken cancellationToken = default) where T : class
	{
		foreach (PropertyInfo property in GetOrAddPropertiesFromReflectionCache(typeof(T)))
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

	#region Reflection Deep Copy Helpers

	/// <summary>
	/// Copies an object of one type to another type using reflection based on property name matching
	/// </summary>
	/// <param name="source">The source object to copy from.</param>
	/// <param name="destType">The destination type to copy to.</param>
	/// <param name="depth">The current depth of the copy operation.</param>
	/// <param name="maxDepth">The maximum depth of the copy operation. If -1, then there will be no limit on recursion depth.</param>
	/// <returns>A new instance of the destination type with properties copied from the source.</returns>
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
			IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromReflectionCache(sourceType).Where(x => x.CanRead);
			Dictionary<string, PropertyInfo> destPropsDict = GetOrAddPropertiesFromReflectionCache(destType).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

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

	/// <summary>
	/// Creates a deep copy of a collection, converting it to the specified destination type.
	/// </summary>
	/// <remarks>
	/// This method supports copying both simple collections (e.g. lists, arrays) and dictionaries.
	/// For dictionaries, both keys and values are copied. If the source or destination collection contains complex objects, they are recursively copied up to the specified <paramref name="maxDepth"/>.
	/// </remarks>
	/// <param name="source">The source collection to copy. Must implement <see cref="IEnumerable"/>.</param>
	/// <param name="destType">The type of the destination collection. Must be a collection type such as a list, array, or dictionary.</param>
	/// <param name="maxDepth">The maximum depth for recursive copying of nested objects. A value of 0 indicates no recursion, while higher values allow deeper copying of nested structures.</param>
	/// <param name="cancellationToken">Cancellation token for this operation.</param>
	/// <returns>A new collection of the specified type containing deep copies of the elements in the source collection. Returns <see langword="null"/> if <paramref name="source"/> is <see langword="null"/>.</returns>
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

	#endregion

	#region Expression Trees for Shallow Copying

	/// <summary>
	/// Creates or retrieves a dictionary of property mappings between the source and destination types.
	/// </summary>
	/// <remarks>
	/// This method is useful for scenarios where shallow copying of properties between objects of different types is required.
	/// The returned dictionary can be used to efficiently map and transfer property values.
	/// </remarks>
	/// <typeparam name="TSource">The type of the source object.</typeparam>
	/// <typeparam name="TDest">The type of the destination object.</typeparam>
	/// <returns>A dictionary where each key is the name of a property, and the value is a tuple containing an <see cref="Action{TDest, Object}"/> to set the property value on the destination object,
	/// and a <see cref="Func{TSource, Object}"/> to get the property value from the source object.</returns>
	public static Dictionary<string, (Action<TDest, object?> Set, Func<TSource, object?> Get)> GetOrCreatePropertyMaps<TSource, TDest>() where TSource : class? where TDest : class?
	{
		return GetOrAddFunctionFromCopyCache<TSource, TDest>().ToDictionary(kvp => kvp.Key, kvp => ((Action<TDest, object?>)kvp.Value.Set, (Func<TSource, object?>)kvp.Value.Get));
	}

	/// <summary>
	/// Creates a dictionary of property mappings between a source type and a destination type for caching purposes.
	/// </summary>
	/// <remarks>
	/// This method identifies properties with matching names and types in the source and destination types.
	/// It generates delegates for getting the value of the source property and setting the value of the destination property.
	/// Only properties that are readable in the source type and writable in the destination type are included in the mapping.
	/// </remarks>
	/// <typeparam name="TSource">The source type containing the properties to map from.</typeparam>
	/// <typeparam name="TDest">The destination type containing the properties to map to.</typeparam>
	/// <returns>A dictionary where each key is the name of a property shared by both the source and destination types, and the
	/// value is a tuple containing a setter delegate for the destination property and a getter delegate for the source property.</returns>
	private static Dictionary<string, (Delegate Set, Delegate Get)> CreatePropertyMappingsForCache<TSource, TDest>()
	{
		Dictionary<string, (Delegate Set, Delegate Get)> cacheableMaps = new();

		IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromReflectionCache(typeof(TSource)).Where(x => x.CanRead);
		Dictionary<string, PropertyInfo> destProps = GetOrAddPropertiesFromReflectionCache(typeof(TDest)).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

		foreach (PropertyInfo sProp in sourceProps)
		{
			if (destProps.TryGetValue(sProp.Name, out PropertyInfo? dProp) && dProp.PropertyType == sProp.PropertyType)
			{
				// Getter: (TSource src) => (object?)src.Prop
				ParameterExpression srcParam = Expression.Parameter(typeof(TSource), "src");
				Expression<Func<TSource, object?>> getExpr = Expression.Lambda<Func<TSource, object?>>(
					Expression.Convert(
							Expression.Property(
									typeof(TSource).IsInterface ? Expression.Convert(srcParam, sProp.DeclaringType!) : srcParam,
									sProp),
							typeof(object)),
					srcParam);
				Func<TSource, object?> get = getExpr.CompileFast();

				// Setter: (TDest dest, object? value) => dest.Prop = (TPropType)value
				ParameterExpression destParam = Expression.Parameter(typeof(TDest), "dest");
				ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
				Expression<Action<TDest, object?>> setExpr = Expression.Lambda<Action<TDest, object?>>(
										Expression.Assign(
												Expression.Property(
														typeof(TDest).IsInterface ? Expression.Convert(destParam, dProp.DeclaringType!) : destParam,
														dProp),
												Expression.Convert(valueParam, dProp.PropertyType)),
										destParam, valueParam);
				Action<TDest, object?> set = setExpr.CompileFast();

				string propertyKey = sProp.Name;
				cacheableMaps.Add(propertyKey, (set, get));
			}
		}

		return cacheableMaps;
	}

	#endregion

	#region Property Mapping For Recursive Copy

	/// <summary>
	/// Creates a robust recursive copy function using expression trees.
	/// </summary>
	/// <param name="source">Source object to copy from.</param>
	/// <param name="maxDepth">Optional: The maximum depth of recursion. Default is -1, which means unlimited recursion.</param>
	/// <returns>A new instance of the destination type with copied properties.</returns>
	private static UT? CopyPropertiesToNewRecursiveExpressionTrees<T, UT>(this T source, int maxDepth = -1) where T : class where UT : class, new()
	{
		if (source == null)
		{
			return default;
		}

		Func<object, object?, int, int, object?> copyFunc = GetOrCreateCopyFunction(typeof(T), typeof(UT));
		return (UT?)copyFunc(source, null, 0, maxDepth);
	}

	/// <summary>
	/// Gets or creates a compiled copy function for the given source and destination types
	/// </summary>
	/// <param name="sourceType">The type to copy properties from.</param>
	/// <param name="destType"> The type to copy properties to.</param>
	/// <returns>A compiled function that takes a source object, a destination object (can be null), current depth, and max depth, and returns the copied object.</returns>
	private static Func<object, object?, int, int, object?> GetOrCreateCopyFunction(Type sourceType, Type destType)
	{
		return GetOrAddFunctionFromDeepCopyCache((sourceType, destType));
	}

	/// <summary>
	/// Creates a compiled expression tree function for copying between two types
	/// </summary>
	/// <param name="sourceType">The type to copy properties from.</param>
	/// <param name="destType"> The type to copy properties to.</param>
	/// <returns>A compiled function that takes a source object, a destination object (can be null), current depth, and max depth, and returns the copied object.</returns>
	private static Func<object, object?, int, int, object?> CreateCopyFunction(Type sourceType, Type destType)
	{
		ParameterExpression sourceParam = Expression.Parameter(typeof(object), "source");
		ParameterExpression destParam = Expression.Parameter(typeof(object), "dest");
		ParameterExpression depthParam = Expression.Parameter(typeof(int), "depth");
		ParameterExpression maxDepthParam = Expression.Parameter(typeof(int), "maxDepth");

		ParameterExpression typedSource = Expression.Variable(sourceType, "typedSource");
		ParameterExpression result = Expression.Variable(typeof(object), "result");

		List<Expression> expressions = new();

		// Cast source to correct type
		expressions.Add(Expression.Assign(typedSource, Expression.Convert(sourceParam, sourceType)));

		// Handle simple types (string, primitives, etc.) - direct conversion
		if (destType.IsSimpleType())
		{
			// For simple types, we can't do property copying, so we try direct conversion
			if (sourceType == destType)
			{
				expressions.Add(Expression.Assign(result, Expression.Convert(typedSource, typeof(object))));
			}
			else if (CanConvertTypes(sourceType, destType))
			{
				expressions.Add(Expression.Assign(result, Expression.Convert(Expression.Convert(typedSource, destType), typeof(object))));
			}
			else
			{
				// Return default value for the type
				expressions.Add(Expression.Assign(result, Expression.Convert(destType.IsValueType ? Expression.Default(destType) : Expression.Constant(null, destType), typeof(object))));
			}
		}
		else
		{
			// For complex types, create instance and copy properties
			ParameterExpression typedDest = Expression.Variable(destType, "typedDest");

			// Create destination instance
			expressions.Add(Expression.Assign(typedDest, CreateInstanceExpression(destType)));

			// Get properties for both types
			PropertyInfo[] sourceProps = GetOrAddPropertiesFromReflectionCache(sourceType).Where(x => x.CanRead).ToArray();
			Dictionary<string, PropertyInfo> destPropsDict = GetOrAddPropertiesFromReflectionCache(destType).Where(x => x.CanWrite).ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);

			// Create property copy expressions
			foreach (PropertyInfo sourceProp in sourceProps)
			{
				if (!destPropsDict.TryGetValue(sourceProp.Name, out PropertyInfo? destProp))
				{
					continue;
				}

				Expression? copyExpression;

				MemberExpression sourceValue = Expression.Property(typedSource, sourceProp);
				MemberExpression destProperty = Expression.Property(typedDest, destProp);

				// Handle null values
				if (!sourceProp.PropertyType.IsValueType)
				{
					BinaryExpression nullCheck = Expression.Equal(sourceValue, Expression.Constant(null));
					BinaryExpression setNull = Expression.Assign(destProperty, Expression.Convert(Expression.Constant(null), destProp.PropertyType));

					Expression? copyLogic = CreateValueCopyExpression(sourceValue, destProperty, sourceProp.PropertyType, destProp.PropertyType, depthParam, maxDepthParam);
					copyExpression = copyLogic != null ? Expression.IfThenElse(nullCheck, setNull, copyLogic) : null;
				}
				else
				{
					copyExpression = CreateValueCopyExpression(sourceValue, destProperty, sourceProp.PropertyType, destProp.PropertyType, depthParam, maxDepthParam);
				}

				if (copyExpression != null)
				{
					expressions.Add(copyExpression);
				}
			}

			// Return the destination object
			expressions.Add(Expression.Assign(result, Expression.Convert(typedDest, typeof(object))));

			// Add typedDest to the block variables
			BlockExpression body = Expression.Block(new[] { typedSource, typedDest, result }, expressions.Concat([result]));

			Expression<Func<object, object?, int, int, object?>> lambda = Expression.Lambda<Func<object, object?, int, int, object?>>(body, sourceParam, destParam, depthParam, maxDepthParam);

			return lambda.CompileFast();
		}

		// For simple types, we don't need the extra variable
		expressions.Add(result);

		BlockExpression simpleBody = Expression.Block(new[] { typedSource, result }, expressions);

		Expression<Func<object, object?, int, int, object?>> simpleLambda = Expression.Lambda<Func<object, object?, int, int, object?>>(simpleBody, sourceParam, destParam, depthParam, maxDepthParam);

		return simpleLambda.CompileFast();
	}

	/// <summary>
	/// Creates an expression for instantiating a type
	/// </summary>
	/// <param name="type">The type to instantiate.</param>
	/// <returns>An expression that creates a new instance of the specified type.</returns>
	private static Expression CreateInstanceExpression(Type type)
	{
		// Check if type has parameterless constructor
		ConstructorInfo? defaultConstructor = type.GetConstructor(Type.EmptyTypes);
		if (defaultConstructor != null)
		{
			return Expression.New(type);
		}

		// For types without parameterless constructor, use Activator.CreateInstance
		MethodInfo createInstanceMethod = typeof(Activator).GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type) })!;
		return Expression.Convert(Expression.Call(createInstanceMethod, Expression.Constant(type)), type);
	}

	/// <summary>
	/// Checks if two types can be converted between each other
	/// </summary>
	/// <param name="sourceType">The source type.</param>
	/// <param name="destType">The destination type.</param>
	/// <returns> <see langword="true"/> if <paramref name="sourceType"/> can be converted into <paramref name="destType"/>, otherwise <see langword="false"/>.</returns>
	private static bool CanConvertTypes(Type sourceType, Type destType)
	{
		// Check for implicit/explicit conversions
		try
		{
			// This is a simple check - you might want to make this more sophisticated
			return sourceType == destType || destType.IsAssignableFrom(sourceType) || (sourceType.IsPrimitive && destType.IsPrimitive);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Creates an expression for copying a value with type compatibility checks
	/// </summary>
	/// <param name="sourceValue">The source value expression.</param>
	/// <param name="destProperty">The destination property expression.</param>
	/// <param name="sourceType">The source type.</param>
	/// <param name="destType">The destination type.</param>
	/// <param name="depthParam">The current depth parameter.</param>
	/// <param name="maxDepthParam">The maximum depth parameter.</param>
	/// <returns>An expression that copies the value from the source to the destination property.</returns>
	private static Expression? CreateValueCopyExpression(Expression sourceValue, Expression destProperty, Type sourceType, Type destType, ParameterExpression depthParam, ParameterExpression maxDepthParam)
	{
		// Direct assignment for same types or simple types
		if (sourceType == destType || (sourceType.IsSimpleType() && destType.IsSimpleType() && CanConvertTypes(sourceType, destType)))
		{
			return Expression.Assign(destProperty, Expression.Convert(sourceValue, destType));
		}

		// Handle collections
		if (sourceType.IsEnumerable() && destType.IsEnumerable())
		{
			MethodInfo copyCollectionMethod = typeof(Copy).GetMethod(nameof(CopyCollectionRuntime), BindingFlags.NonPublic | BindingFlags.Static)!;
			MethodCallExpression copyCall = Expression.Call(copyCollectionMethod, Expression.Convert(sourceValue, typeof(object)), Expression.Constant(destType), depthParam, maxDepthParam);
			return Expression.Assign(destProperty, Expression.Convert(copyCall, destType));
		}

		// Handle complex objects with depth checking
		if (sourceType.IsClass && destType.IsClass && !sourceType.IsSimpleType() && !destType.IsSimpleType())
		{
			// Check depth limits
			BinaryExpression depthCheck = Expression.AndAlso(Expression.NotEqual(maxDepthParam, Expression.Constant(-1)), Expression.GreaterThanOrEqual(depthParam, maxDepthParam));
			BinaryExpression setDefault = Expression.Assign(destProperty, Expression.Convert(Expression.Constant(null), destType));

			// Recursive copy call
			MethodInfo copyObjectMethod = typeof(Copy).GetMethod(nameof(CopyObjectRuntime), BindingFlags.NonPublic | BindingFlags.Static)!;
			MethodCallExpression recursiveCopy = Expression.Call(copyObjectMethod, Expression.Convert(sourceValue, typeof(object)), Expression.Constant(destType), Expression.Add(depthParam, Expression.Constant(1)), maxDepthParam);
			BinaryExpression assignCopy = Expression.Assign(destProperty, Expression.Convert(recursiveCopy, destType));
			return Expression.IfThenElse(depthCheck, setDefault, assignCopy);
		}

		// Fallback: try direct conversion
		if (destType.IsAssignableFrom(sourceType))
		{
			return Expression.Assign(destProperty, Expression.Convert(sourceValue, destType));
		}

		return null;
	}

	/// <summary>
	/// Runtime helper for copying collections.
	/// </summary>
	/// <param name="source">Source object to copy from.</param>
	/// <param name="destType">Destination type to copy to.</param>
	/// <param name="depth">Current depth of recursion.</param>
	/// <param name="maxDepth">Maximum depth of recursion.</param>
	/// <returns>A new instance of the destination type with copied properties or null if <paramref name="source"/> is null.</returns>
	private static object? CopyCollectionRuntime(object source, Type destType, int depth, int maxDepth)
	{
		if (source == null)
		{
			return null;
		}

		IEnumerable sourceEnumerable = (IEnumerable)source;

		// Handle arrays
		if (destType.IsArray)
		{
			Type elementType = destType.GetElementType()!;
			List<object> sourceList = sourceEnumerable.Cast<object>().ToList();
			Array destArray = Array.CreateInstance(elementType, sourceList.Count);

			for (int i = 0; i < sourceList.Count; i++)
			{
				object? copiedItem = CopyItemRuntime(sourceList[i], elementType, depth, maxDepth);
				destArray.SetValue(copiedItem, i);
			}

			return destArray;
		}

		// Handle dictionaries specifically
		if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			Type[] destGenericArgs = destType.GetGenericArguments();
			Type destKeyType = destGenericArgs[0];
			Type destValueType = destGenericArgs[1];

			object? destCollection = Activator.CreateInstance(destType);
			MethodInfo? addMethod = destType.GetMethod("Add");

			if (addMethod != null)
			{
				foreach (object item in sourceEnumerable)
				{
					if (item == null)
					{
						continue;
					}

					// Get the actual type of the source KeyValuePair
					Type sourceItemType = item.GetType();
					PropertyInfo sourceKeyProperty = sourceItemType.GetProperty("Key")!;
					PropertyInfo sourceValueProperty = sourceItemType.GetProperty("Value")!;

					// Extract key and value from the source KeyValuePair
					object? sourceKey = sourceKeyProperty.GetValue(item);
					object? sourceValue = sourceValueProperty.GetValue(item);

					// Copy key and value to destination types
					object? copiedKey = CopyItemRuntime(sourceKey, destKeyType, depth, maxDepth);
					object? copiedValue = CopyItemRuntime(sourceValue, destValueType, depth, maxDepth);

					addMethod.Invoke(destCollection, [copiedKey, copiedValue]);
				}
			}

			return destCollection;
		}

		// Handle generic collections (List, etc.)
		if (destType.IsGenericType)
		{
			Type[] genericArgs = destType.GetGenericArguments();
			Type elementType = genericArgs[0];

			// Create destination collection
			object? destCollection = Activator.CreateInstance(destType);
			MethodInfo? addMethod = destType.GetMethod("Add");

			if (addMethod != null)
			{
				foreach (object item in sourceEnumerable)
				{
					object? copiedItem = CopyItemRuntime(item, elementType, depth, maxDepth);
					addMethod.Invoke(destCollection, [copiedItem]);
				}
			}

			return destCollection;
		}

		return source;
	}

	/// <summary>
	/// Runtime helper for copying individual items
	/// </summary>
	/// <param name="item">The item to copy.</param>
	/// <param name="destType">The destination type to copy to.</param>
	/// <param name="depth">Current depth of recursion.</param>
	/// <param name="maxDepth">Maximum depth of recursion.</param>
	/// <returns>A copied item of the specified destination type, or <see cref="null"/> if <paramref name="item"/> <see cref="null"/>.</returns>
	private static object? CopyItemRuntime(object? item, Type destType, int depth, int maxDepth)
	{
		if (item == null)
		{
			return null;
		}

		Type itemType = item.GetType();

		if (itemType.IsSimpleType() || itemType == destType)
		{
			return item;
		}

		return CopyObjectRuntime(item, destType, depth, maxDepth);
	}

	/// <summary>
	/// Runtime helper for copying objects
	/// </summary>
	/// <param name="source">Source object to copy from.</param>
	/// <param name="destType">Destination type to copy to.</param>
	/// <param name="depth">Current depth of recursion.</param>
	/// <param name="maxDepth">Maximum depth of recursion.</param>
	/// <returns>A new instance of the destination type with copied properties or <see cref="null"/> if <paramref name="source"/> is <see cref="null"/>.</returns>
	private static object? CopyObjectRuntime(object source, Type destType, int depth, int maxDepth)
	{
		if (source == null)
		{
			return null;
		}

		Func<object, object?, int, int, object?> copyFunc = GetOrCreateCopyFunction(source.GetType(), destType);
		return copyFunc(source, null, depth, maxDepth);
	}

	#endregion
}
