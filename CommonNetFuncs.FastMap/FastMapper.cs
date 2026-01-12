using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommonNetFuncs.Core;
using FastExpressionCompiler;

using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.FastMap;

/// <summary>
/// High-performance object mapper using compiled expression trees with aggressive inlining.
/// Optimized for minimal overhead - targets near-manual-mapping performance.
/// </summary>
public static class FastMapper
{
	#region Caching

	/// <summary>
	/// Cache key struct - more efficient than tuple for dictionary lookups
	/// </summary>
	[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly struct MapperCacheKey(Type sourceType, Type destType) : IEquatable<MapperCacheKey>
	{
		public readonly Type SourceType = sourceType;
		public readonly Type DestType = destType;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(MapperCacheKey other)
		{
			return SourceType == other.SourceType && DestType == other.DestType;
		}

		public override bool Equals(object? obj)
		{
			return obj is MapperCacheKey key && Equals(key);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(SourceType, DestType);
		}

		public static bool operator ==(MapperCacheKey left, MapperCacheKey right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MapperCacheKey left, MapperCacheKey right)
		{
			return !left.Equals(right);
		}
	}

	// CacheManager-based cache for configurable caching (LRU support, size limits)
	private static readonly CacheManager<MapperCacheKey, Delegate> ManagedMapperCache = new();

	/// <summary>
	/// Gets the CacheManager API for configuring mapper cache behavior (LRU, size limits, etc.).
	/// </summary>
	public static ICacheManagerApi<MapperCacheKey, Delegate> CacheManager => ManagedMapperCache;

	/// <summary>
	/// Gets or adds a mapper from the managed cache using the configured caching strategy.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Delegate GetOrAddFromManagedCache<TSource, TDest>(MapperCacheKey key)
	{
		return CacheManager.IsUsingLimitedCache()
			? CacheManager.GetOrAddLimitedCache(key, _ => CreateMapper<TSource, TDest>())
			: CacheManager.GetOrAddCache(key, _ => CreateMapper<TSource, TDest>());
	}

	/// <summary>
	/// Clears all mapper caches (both static generic and managed).
	/// Note: Static generic class cache cannot be cleared at runtime.
	/// </summary>
	public static void ClearCache()
	{
		CacheManager.ClearAllCaches();
	}

	/// <summary>
	/// Static generic class for fastest possible mapper lookup - no dictionary access needed after first call.
	/// This is used by the parameterless FasterMap overload for maximum performance.
	/// Each unique initialization of TSource/TDest creates a new static class instance with its own cached mapper.
	/// Not cleared at runtime - remains for the lifetime of the AppDomain.
	/// </summary>
	private static class MapperCache<TSource, TDest> where TSource : class? where TDest : class?
	{
		public static readonly Func<TSource, TDest> Mapper = CreateMapper<TSource, TDest>();
	}

	#endregion

	#region Public API

	/// <summary>
	/// Maps source object to a new instance of the destination type.
	/// Uses static generic caching for maximum performance (fastest path).
	/// </summary>
	/// <typeparam name="TSource">Type to map data from</typeparam>
	/// <typeparam name="TDest">Type to map data to</typeparam>
	/// <param name="source">Object to map data from</param>
	/// <returns>New instance of TDest with values populated from source object</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: NotNullIfNotNull(nameof(source))]
	public static TDest? FastMap<TSource, TDest>(this TSource source) where TSource : class? where TDest : class?
	{
		if (source is null)
		{
			return default;
		}

		// Use static generic class for fastest possible cache access
		return MapperCache<TSource, TDest>.Mapper(source)!;
	}

	/// <summary>
	/// Maps source object to a new instance of the destination type.
	/// When useCache is true, uses CacheManager for configurable caching (supports LRU, size limits).
	/// When useCache is false, creates a new mapper each time (useful for one-off mappings).
	/// </summary>
	/// <typeparam name="TSource">Type to map data from</typeparam>
	/// <typeparam name="TDest">Type to map data to</typeparam>
	/// <param name="source">Object to map data from</param>
	/// <param name="useCache">When true, uses CacheManager-based caching. When false, creates mapper without caching.</param>
	/// <returns>New instance of TDest with values populated from source object</returns>
	[return: NotNullIfNotNull(nameof(source))]
	public static TDest? FastMap<TSource, TDest>(this TSource source, bool useCache) where TSource : class? where TDest : class?
	{
		if (source is null)
			return default;

		Func<TSource, TDest> mapper = useCache
			? (Func<TSource, TDest>)GetOrAddFromManagedCache<TSource, TDest>(new(typeof(TSource), typeof(TDest)))
			: CreateMapper<TSource, TDest>();

		return mapper(source)!;
	}

	#endregion

	private static Func<TSource, TDest> CreateMapper<TSource, TDest>()
	{
		Type sourceType = typeof(TSource);
		Type destType = typeof(TDest);

		ParameterExpression sourceParam = Expression.Parameter(sourceType, "src");
		Expression body = CreateMappingExpression(sourceParam, sourceType, destType);

		Expression<Func<TSource, TDest>> lambda = Expression.Lambda<Func<TSource, TDest>>(body, sourceParam);
		return lambda.CompileFast();
	}

	private static Expression CreateMappingExpression(Expression source, Type sourceType, Type destType)
	{
		// Handle collections
		if (IsCollection(sourceType) && IsCollection(destType))
		{
			return CreateCollectionMappingExpression(source, sourceType, destType);
		}

		// Handle dictionaries
		if (IsDictionary(sourceType) && IsDictionary(destType))
		{
			return CreateDictionaryMappingExpression(source, sourceType, destType);
		}

		// Handle object mapping
		return CreateObjectMappingExpression(source, sourceType, destType);
	}

	private static MemberInitExpression CreateObjectMappingExpression(Expression source, Type sourceType, Type destType)
	{
		PropertyInfo[] sourceProps = GetProperties(sourceType);
		PropertyInfo[] destProps = GetProperties(destType);

		// Build member bindings for MemberInit expression (most efficient)
		List<MemberBinding> bindings = [];

		foreach (PropertyInfo destProp in destProps)
		{
			if (!destProp.CanWrite)
			{
				continue;
			}

			PropertyInfo? sourceProp = Array.Find(sourceProps, p => p.CanRead && p.Name == destProp.Name);
			if (sourceProp is null)
			{
				continue;
			}

			MemberExpression sourceAccess = Expression.Property(source, sourceProp);
			Expression valueExpr;

			if (sourceProp.PropertyType == destProp.PropertyType)
			{
				// Direct assignment - same types
				valueExpr = sourceAccess;
			}
			else if (IsCollection(sourceProp.PropertyType) && IsCollection(destProp.PropertyType))
			{
				// Collection mapping - inline
				valueExpr = CreateCollectionMappingExpression(sourceAccess, sourceProp.PropertyType, destProp.PropertyType);

				// Add null check for collections
				if (!sourceProp.PropertyType.IsValueType)
				{
					valueExpr = Expression.Condition(
							Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
							Expression.Default(destProp.PropertyType),
							valueExpr
						);
				}
			}
			else if (IsDictionary(sourceProp.PropertyType) && IsDictionary(destProp.PropertyType))
			{
				// Dictionary mapping - inline
				valueExpr = CreateDictionaryMappingExpression(sourceAccess, sourceProp.PropertyType, destProp.PropertyType);

				if (!sourceProp.PropertyType.IsValueType)
				{
					valueExpr = Expression.Condition(
							Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
							Expression.Default(destProp.PropertyType),
							valueExpr
						);
				}
			}
			else if (!sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType)
			{
				// Nested object mapping - INLINE the mapping instead of recursive call
				valueExpr = CreateObjectMappingExpression(sourceAccess, sourceProp.PropertyType, destProp.PropertyType);

				// Add null check for reference types
				valueExpr = Expression.Condition(
					Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
					Expression.Default(destProp.PropertyType),
					valueExpr);
			}
			else
			{
				// Incompatible types - skip
				continue;
			}

			bindings.Add(Expression.Bind(destProp, valueExpr));
		}

		// Use MemberInit for efficient object creation
		NewExpression newExpr = Expression.New(destType);
		return Expression.MemberInit(newExpr, bindings);
	}

	private static Expression CreateCollectionMappingExpression(Expression source, Type sourceType, Type destType)
	{
		Type sourceElemType = GetElementType(sourceType);
		Type destElemType = GetElementType(destType);

		// Same element type - use optimized copy methods
		if (sourceElemType == destElemType)
		{
			return CreateSameTypeCollectionCopy(source, destType, sourceElemType);
		}

		// Different element types - need to map each element
		return CreateMappedCollectionExpression(source, destType, sourceElemType, destElemType);
	}

	private static Expression CreateSameTypeCollectionCopy(Expression source, Type destType, Type elemType)
	{
		// For arrays
		if (destType.IsArray)
		{
			// Use ToArray - it's already optimized
			MethodInfo toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(elemType);
			return Expression.Call(null, toArrayMethod, source);
		}

		// For List<TObj>
		if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(List<>))
		{
			// Use List<TObj> constructor that takes IEnumerable<TObj> - pre-allocates if Count is known
			ConstructorInfo listCtor = destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elemType)])!;
			return Expression.New(listCtor, source);
		}

		// For HashSet<TObj>
		if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(HashSet<>))
		{
			ConstructorInfo hashSetCtor = destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elemType)])!;
			return Expression.New(hashSetCtor, source);
		}

		// For other collections, use ToList
		MethodInfo toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(elemType);
		return Expression.Call(null, toListMethod, source);
	}

	private static MethodCallExpression CreateMappedCollectionExpression(Expression source, Type destType, Type sourceElemType, Type destElemType)
	{
		// Use LINQ Select + appropriate conversion - more efficient than manual loop
		// because List<TObj> constructor from IEnumerable<TObj> uses optimized copy when source implements ICollection<TObj>

		// Create the element mapping lambda: item => new TDest { ... }
		ParameterExpression itemParam = Expression.Parameter(sourceElemType, "item");
		Expression mappedItem = CreateMappingExpression(itemParam, sourceElemType, destElemType);
		LambdaExpression selectLambda = Expression.Lambda(mappedItem, itemParam);

		// Call Select
		MethodInfo selectMethod = typeof(Enumerable).GetMethods()
			.First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
			.MakeGenericMethod(sourceElemType, destElemType);

		MethodCallExpression selectCall = Expression.Call(null, selectMethod, source, selectLambda);

		// Return appropriate collection type
		if (destType.IsArray)
		{
			MethodInfo toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(destElemType);
			return Expression.Call(null, toArrayMethod, selectCall);
		}

		if (destType.IsGenericType)
		{
			Type genericDef = destType.GetGenericTypeDefinition();

			if (genericDef == typeof(List<>))
			{
				MethodInfo toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(destElemType);
				return Expression.Call(null, toListMethod, selectCall);
			}

			if (genericDef == typeof(HashSet<>))
			{
				MethodInfo toHashSetMethod = typeof(Enumerable).GetMethods()
					.First(m => m.Name == nameof(Enumerable.ToHashSet) && m.GetParameters().Length == 1)
					.MakeGenericMethod(destElemType);
				return Expression.Call(null, toHashSetMethod, selectCall);
			}
		}

		// Default: ToList
		MethodInfo defaultToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(destElemType);
		return Expression.Call(null, defaultToListMethod, selectCall);
	}

	private static Expression CreateDictionaryMappingExpression(Expression source, Type sourceType, Type destType)
	{
		Type[] sourceArgs = sourceType.GetGenericArguments();
		Type[] destArgs = destType.GetGenericArguments();

		Type sourceKeyType = sourceArgs[0];
		Type sourceValueType = sourceArgs[1];
		Type destKeyType = destArgs[0];
		Type destValueType = destArgs[1];

		if (sourceKeyType != destKeyType)
			throw new InvalidOperationException("Dictionary key types must match");

		// Same value types - simple copy
		if (sourceValueType == destValueType)
		{
			ConstructorInfo dictCtor = destType.GetConstructor([typeof(IDictionary<,>).MakeGenericType(sourceKeyType, sourceValueType)])!;
			return Expression.New(dictCtor, source);
		}

		// Need to map values - generate inline loop
		List<ParameterExpression> variables = [];
		List<Expression> bodyExpressions = [];

		// Create destination dictionary
		ParameterExpression destDict = Expression.Variable(destType, "destDict");
		variables.Add(destDict);
		bodyExpressions.Add(Expression.Assign(destDict, Expression.New(destType)));

		// Get enumerator
		Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(sourceKeyType, sourceValueType);
		Type enumerableType = typeof(IEnumerable<>).MakeGenericType(kvpType);
		Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(kvpType);

		MethodInfo getEnumeratorMethod = enumerableType.GetMethod("GetEnumerator")!;
		ParameterExpression enumerator = Expression.Variable(enumeratorType, "enumerator");
		variables.Add(enumerator);
		bodyExpressions.Add(Expression.Assign(enumerator, Expression.Call(source, getEnumeratorMethod)));

		// Loop
		LabelTarget loopBreak = Expression.Label("break");
		MethodInfo moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
		PropertyInfo currentProp = enumeratorType.GetProperty("Current")!;

		ParameterExpression kvpVar = Expression.Variable(kvpType, "kvp");
		variables.Add(kvpVar);

		MemberExpression keyAccess = Expression.Property(kvpVar, "Key");
		MemberExpression valueAccess = Expression.Property(kvpVar, "Value");

		Expression mappedValue = CreateMappingExpression(valueAccess, sourceValueType, destValueType);

		// Add null check for reference type values
		if (!sourceValueType.IsValueType)
		{
			mappedValue = Expression.Condition(
				Expression.Equal(valueAccess, Expression.Constant(null, sourceValueType)),
				Expression.Default(destValueType),
				mappedValue);
		}

		MethodInfo addMethod = destType.GetMethod("Add", [destKeyType, destValueType])!;

		BlockExpression loopBody = Expression.Block(
			Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod)), Expression.Break(loopBreak)),
			Expression.Assign(kvpVar, Expression.Property(enumerator, currentProp)),
			Expression.Call(destDict, addMethod, keyAccess, mappedValue));

		bodyExpressions.Add(Expression.Loop(loopBody, loopBreak));
		bodyExpressions.Add(destDict);

		return Expression.Block(variables, bodyExpressions);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static PropertyInfo[] GetProperties(Type type)
	{
		return GetOrAddPropertiesFromReflectionCache(type);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsCollection(Type type)
	{
		return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !IsDictionary(type);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsDictionary(Type type)
	{
		return typeof(IDictionary).IsAssignableFrom(type);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Type GetElementType(Type collectionType)
	{
		if (collectionType.IsArray)
		{
			return collectionType.GetElementType()!;
		}

		if (collectionType.IsGenericType)
		{
			Type[] genArgs = collectionType.GetGenericArguments();
			if (genArgs.Length > 0)
			{
				return genArgs[0];
			}
		}

		Type? enumInterface = collectionType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		return enumInterface?.GetGenericArguments()[0] ?? typeof(object);
	}
}

// Original Form Below (For Reference)

// using System.Collections;
// using System.Collections.ObjectModel;
// using System.Diagnostics.CodeAnalysis;
// using System.Linq.Expressions;
// using System.Reflection;
// using System.Runtime.CompilerServices;
// using CommonNetFuncs.Core;
// using FastExpressionCompiler;

// using static CommonNetFuncs.Core.ReflectionCaches;

// namespace CommonNetFuncs.FastMap;

// public static class FastMapper
// {
// 	#region Caching

// 	public readonly struct MapperCacheKey(Type sourceType, Type destType) : IEquatable<MapperCacheKey>
// 	{
// 		public readonly Type SourceType = sourceType;
// 		public readonly Type DestType = destType;

// 		public bool Equals(MapperCacheKey other)
// 		{
// 			return other.SourceType == SourceType && other.DestType == DestType;
// 		}

// 		public override bool Equals(object? obj)
// 		{
// 			return obj is MapperCacheKey mapperCacheKey && Equals(mapperCacheKey);
// 		}

// 		public override int GetHashCode()
// 		{
// 			return HashCode.Combine(SourceType, DestType);
// 		}

// 		public static bool operator ==(MapperCacheKey left, MapperCacheKey right)
// 		{
// 			return left.Equals(right);
// 		}

// 		public static bool operator !=(MapperCacheKey left, MapperCacheKey right)
// 		{
// 			return !(left == right);
// 		}
// 	}

// 	private static readonly CacheManager<MapperCacheKey, Delegate> MapperCache = new();

// 	public static ICacheManagerApi<MapperCacheKey, Delegate> CacheManager => MapperCache;

// 	/// <summary>
// 	/// Clears LimitedMapperCache cache and sets the size to the specified value.
// 	/// </summary>
// 	private static Delegate GetOrAddPropertiesFromMapperCache<TObj, TTask>(MapperCacheKey key)
// 	{
// 		// Use GetOrAdd pattern to reduce lock contention
// 		if (CacheManager.IsUsingLimitedCache())
// 		{
// 			return CacheManager.GetOrAddLimitedCache(key, _ => CreateMapper<TObj, TTask>(true));
// 		}
// 		else
// 		{
// 			return CacheManager.GetOrAddCache(key, _ => CreateMapper<TObj, TTask>(true));
// 		}
// 	}

// 	#endregion

// 	/// <summary>
// 	/// Method that maps one object onto another by property name using expression trees
// 	/// </summary>
// 	/// <typeparam name="TObj">Type to map data from</typeparam>
// 	/// <typeparam name="TTask">Type to map data to</typeparam>
// 	/// <param name="source">Object to map data from</param>
// 	/// <returns>New instance of type TTask with values populated from source object</returns>
// 	[return: NotNullIfNotNull(nameof(source))]
// 	public static TTask? FastMap<TObj, TTask>(this TObj source, bool useCache = true) where TObj : class? where TTask : class?
// 	{
// 		if (source == null)
// 		{
// 			return default;
// 		}

// 		Func<TObj, TTask> mapper = useCache ? (Func<TObj, TTask>)GetOrAddPropertiesFromMapperCache<TObj, TTask>(new(typeof(TObj), typeof(TTask))) : CreateMapper<TObj, TTask>(useCache);
// 		return mapper(source)!;
// 	}

// 	private static Func<TObj, TTask> CreateMapper<TObj, TTask>(bool useCache)
// 	{
// 		ParameterExpression sourceParameter = Expression.Parameter(typeof(TObj), "source");
// 		ParameterExpression destinationVariable = Expression.Variable(typeof(TTask), "destination");

// 		Type destType = typeof(TTask);
// 		Type sourceType = typeof(TObj);

// 		// Create variable declaration and initial assignment
// 		List<ParameterExpression> variables = [destinationVariable];
// 		List<Expression> bindings = [];

// 		if (typeof(IDictionary).IsAssignableFrom(sourceType) || typeof(IDictionary).IsAssignableFrom(destType))
// 		{
// 			// Always initialize the destination
// 			bindings.Add(Expression.Assign(destinationVariable, Expression.New(typeof(TTask))));
// 			bindings.Add(CreateCollectionMapping(sourceParameter, destinationVariable, sourceType, destType, useCache));
// 		}
// 		else if (typeof(IEnumerable).IsAssignableFrom(sourceType) && typeof(IEnumerable).IsAssignableFrom(destType) && sourceType != typeof(string) && destType != typeof(string))
// 		{
// 			Type elementType = GetElementType(destType);

// 			if (destType.IsArray)
// 			{
// 				// Create empty array initially
// 				bindings.Add(Expression.Assign(destinationVariable, Expression.NewArrayBounds(destType.GetElementType()!, Expression.Constant(0))));
// 			}
// 			else if (destType.IsReadOnlyCollectionType())
// 			{
// 				// Create temporary list for ReadOnlyCollection
// 				Type listType = typeof(List<>).MakeGenericType(elementType);
// 				ParameterExpression tempList = Expression.Variable(listType, "tempList");
// 				variables.Add(tempList);
// 				bindings.Add(Expression.Assign(tempList, Expression.New(listType)));

// 				if (destType.IsInterface || (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>)))
// 				{
// 					Type concreteType = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
// 					bindings.Add(Expression.Assign(destinationVariable, Expression.New(concreteType.GetConstructor([typeof(IList<>).MakeGenericType(elementType)])!, tempList)));
// 				}
// 				else if (destType.IsGenericType)
// 				{
// 					Type genericTypeDef = destType.GetGenericTypeDefinition();
// 					if (genericTypeDef == typeof(HashSet<>))
// 					{
// 						// For HashSet, convert the temporary list to a HashSet
// 						bindings.Add(Expression.Assign(destinationVariable, Expression.Call(null, MethodInfoCache.ToHashSet.MakeGenericMethod(elementType), tempList)));
// 					}
// 					else if (genericTypeDef == typeof(Stack<>) || genericTypeDef == typeof(Queue<>))
// 					{
// 						bindings.Add(Expression.Assign(destinationVariable, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)])!, tempList)));
// 					}
// 					else
// 					{
// 						bindings.Add(Expression.Assign(destinationVariable, tempList)); // For other collection types, try to create from list
// 					}
// 				}
// 				else
// 				{
// 					bindings.Add(Expression.Assign(destinationVariable, tempList)); // If destination is not ReadOnlyCollection (e.g. List<TObj>), assign the list directly
// 				}
// 			}
// 			else if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(List<>))
// 			{
// 				bindings.Add(Expression.Assign(destinationVariable, Expression.New(destType)));
// 			}
// 			else
// 			{
// 				// For interfaces or other types, try to assign a List<TObj>
// 				Type listType = typeof(List<>).MakeGenericType(elementType);
// 				bindings.Add(Expression.Assign(destinationVariable, Expression.New(listType)));
// 			}

// 			bindings.Add(CreateCollectionMapping(sourceParameter, destinationVariable, sourceType, destType, useCache)); // Add collection mapping
// 		}
// 		else
// 		{
// 			bindings.Add(Expression.Assign(destinationVariable, Expression.New(typeof(TTask)))); // Initialize destination object if not a collection

// 			//PropertyInfo[] sourceProperties = propertyCache.GetOrAdd(typeof(TObj), t => t.GetProperties());
// 			//PropertyInfo[] destinationProperties = propertyCache.GetOrAdd(typeof(TTask), t => t.GetProperties());
// 			PropertyInfo[] sourceProperties = GetOrAddPropertiesFromReflectionCache(typeof(TObj));
// 			PropertyInfo[] destinationProperties = GetOrAddPropertiesFromReflectionCache(typeof(TTask));
// 			HashSet<string> assignedProperties = [];
// 			foreach (PropertyInfo destProp in destinationProperties.Where(x => x.CanWrite))
// 			{
// 				PropertyInfo? sourceProp = sourceProperties.FirstOrDefault(x => x.CanRead && string.Equals(x.Name, destProp.Name));
// 				if (sourceProp != null)
// 				{
// 					Expression sourceAccess = Expression.Property(sourceParameter, sourceProp);
// 					Expression destAccess = Expression.Property(destinationVariable, destProp);

// 					Expression assignExpression;
// 					if (sourceProp.PropertyType == destProp.PropertyType)
// 					{
// 						// Add null check for reference types
// 						if (!sourceProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(sourceProp.PropertyType) != null)
// 						{
// 							assignExpression = Expression.Assign(destAccess,
// 								Expression.Condition(Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
// 								Expression.Default(destProp.PropertyType), sourceAccess));
// 						}
// 						else
// 						{
// 							assignExpression = Expression.Assign(destAccess, sourceAccess);
// 						}
// 					}
// 					else if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType) && typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType))
// 					{
// 						// Add null check for collections
// 						assignExpression = Expression.Assign(destAccess,
// 							Expression.Condition(
// 								Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
// 								Expression.Default(destProp.PropertyType),
// 								CreateCollectionMapping(sourceAccess, destAccess, sourceProp.PropertyType, destProp.PropertyType, useCache).Right));
// 					}
// 					else if (!sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType)
// 					{
// 						MethodInfo nestedMapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceProp.PropertyType, destProp.PropertyType);

// 						assignExpression = Expression.Assign(destAccess,
// 							Expression.Condition(
// 								Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
// 								Expression.Default(destProp.PropertyType),
// 								Expression.Call(null, nestedMapMethod, sourceAccess, Expression.Constant(useCache, typeof(bool)))));
// 					}
// 					else
// 					{
// 						continue;
// 					}

// 					assignedProperties.Add(destProp.Name);
// 					bindings.Add(assignExpression);
// 				}
// 			}

// 			foreach (PropertyInfo destProp in destinationProperties.Where(x => x.CanWrite && !assignedProperties.Contains(x.Name)))
// 			{
// 				Expression destAccess = Expression.Property(destinationVariable, destProp);
// 				if (!destProp.PropertyType.IsValueType && (destProp.PropertyType.IsValueType ? RuntimeHelpers.GetUninitializedObject(destProp.PropertyType) : null) != null)
// 				{
// 					ConstructorInfo? constructorInfo = destProp.PropertyType.GetConstructor(Type.EmptyTypes);
// 					if (constructorInfo != null)
// 					{
// 						ConditionalExpression assignIfNull = Expression.IfThen(
// 							Expression.Equal(destAccess, Expression.Constant(null, destProp.PropertyType)),
// 							Expression.Assign(destAccess, Expression.New(constructorInfo)));
// 						bindings.Add(assignIfNull);
// 					}
// 				}
// 			}
// 		}

// 		bindings.Add(destinationVariable);

// 		BlockExpression body = Expression.Block(variables, bindings);
// 		Expression<Func<TObj, TTask>> lambda = Expression.Lambda<Func<TObj, TTask>>(body, sourceParameter);
// 		return lambda.CompileFast();
// 	}

// 	private static BinaryExpression CreateCollectionMapping(Expression sourceAccess, Expression destAccess, Type sourceType, Type destType, bool useCache)
// 	{
// 		Type sourceElementType = GetElementType(sourceType);
// 		Type destElementType = GetElementType(destType);

// 		if (sourceElementType == destElementType) //Lists are the same type, so directly assign one to the other
// 		{
// 			Expression sourceCollection;
// 			if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>))
// 			{
// 				_ = MethodInfoCache.ToList.MakeGenericMethod(sourceElementType);
// 				sourceCollection = Expression.Call(null, MethodInfoCache.ToList!.MakeGenericMethod(sourceElementType), sourceAccess);
// 			}
// 			else
// 			{
// 				sourceCollection = sourceAccess;
// 			}

// 			if (destType.IsArray)
// 			{
// 				MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(sourceElementType);
// 				return Expression.Assign(destAccess, Expression.Call(null, toArrayMethod, sourceCollection));
// 			}
// 			else if (destType.IsReadOnlyCollectionType())
// 			{
// 				Type elementType = GetElementType(destType);
// 				MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(elementType);
// 				Expression intermediateList = Expression.Call(null, toListMethod, sourceCollection);

// 				if (destType.IsInterface || (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>)))
// 				{
// 					Type concreteType = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
// 					ConstructorInfo ctor = concreteType.GetConstructor([typeof(IList<>).MakeGenericType(elementType)])!;
// 					return Expression.Assign(destAccess, Expression.New(ctor, intermediateList));
// 				}
// 				else if (destType.IsGenericType)
// 				{
// 					Type genericTypeDef = destType.GetGenericTypeDefinition();
// 					if (genericTypeDef == typeof(HashSet<>))
// 					{
// 						MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(elementType);
// 						return Expression.Assign(destAccess, Expression.Call(null, toHashSetMethod, sourceCollection));
// 					}
// 					else if (genericTypeDef == typeof(Stack<>) || genericTypeDef == typeof(Queue<>))
// 					{
// 						return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)])!, intermediateList));
// 					}
// 					else if (genericTypeDef == typeof(List<>))
// 					{
// 						return Expression.Assign(destAccess, intermediateList);
// 					}
// 				}

// 				// For any other type, convert to appropriate collection type
// 				MethodInfo toCollectionMethod = MethodInfoCache.ToList.MakeGenericMethod(elementType);
// 				return Expression.Assign(destAccess, Expression.Call(null, toCollectionMethod, sourceCollection));
// 			}
// 			else if (destType.IsGenericType)
// 			{
// 				Type genericTypeDef = destType.GetGenericTypeDefinition();
// 				if (genericTypeDef == typeof(List<>))
// 				{
// 					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
// 					return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceCollection));
// 				}
// 				else if (genericTypeDef == typeof(HashSet<>))
// 				{
// 					MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(destElementType);
// 					return Expression.Assign(destAccess, Expression.Call(null, toHashSetMethod, sourceCollection));
// 				}
// 				else if (genericTypeDef == typeof(Stack<>))
// 				{
// 					//Must reverse order before inserting values into Stack to preserve original order
// 					MethodInfo reverseMethod = MethodInfoCache.Reverse.MakeGenericMethod(destElementType);
// 					MethodCallExpression reverseCall = Expression.Call(null, reverseMethod, sourceAccess);
// 					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
// 					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, reverseCall);
// 					return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall));
// 				}
// 				else if (genericTypeDef == typeof(Queue<>))
// 				{
// 					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
// 					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, sourceAccess);
// 					return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall));
// 				}
// 				else // List<> or other IEnumerable<> types
// 				{
// 					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
// 					return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceAccess));
// 				}
// 			}
// 			else
// 			{
// 				MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(sourceElementType);
// 				return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceAccess));
// 			}
// 		}
// 		else if (typeof(IDictionary).IsAssignableFrom(sourceType) || typeof(IDictionary).IsAssignableFrom(destType))
// 		{
// 			if (!(typeof(IDictionary).IsAssignableFrom(sourceType) && typeof(IDictionary).IsAssignableFrom(destType)))
// 			{
// 				throw new InvalidOperationException("Both source and destination must be a dictionary in order to be mapped");
// 			}

// 			Type sourceKeyType = sourceType.GetGenericArguments()[0];
// 			Type sourceValueType = sourceType.GetGenericArguments()[1];
// 			Type destKeyType = destType.GetGenericArguments()[0];
// 			Type destValueType = destType.GetGenericArguments()[1];

// 			if (sourceKeyType != destKeyType)
// 			{
// 				throw new InvalidOperationException("Source and destination dictionary key types must match.");
// 			}

// 			Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(sourceKeyType, sourceValueType);
// 			Type destKvpType = typeof(KeyValuePair<,>).MakeGenericType(destKeyType, destValueType);

// 			ParameterExpression kvpParam = Expression.Parameter(kvpType, "kvp");
// 			PropertyInfo keyProp = kvpType.GetProperty("Key")!;
// 			PropertyInfo valueProp = kvpType.GetProperty("Value")!;

// 			Expression keyAccess = Expression.Property(kvpParam, keyProp);
// 			Expression valueAccess = Expression.Property(kvpParam, valueProp);

// 			MethodInfo mapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceValueType, destValueType);
// 			Expression mappedValue = Expression.Call(null, mapMethod, valueAccess, Expression.Constant(useCache, typeof(bool)));

// 			NewExpression newKvp = Expression.New(destKvpType.GetConstructor([destKeyType, destValueType])!, keyAccess, mappedValue);
// 			LambdaExpression selectLambda = Expression.Lambda(newKvp, kvpParam);

// 			MethodInfo selectMethod = MethodInfoCache.Select.MakeGenericMethod(kvpType, destKvpType);

// 			MethodInfo toDictionaryMethod = MethodInfoCache.ToDictionary.MakeGenericMethod(destKvpType, destKeyType, destValueType);

// 			ParameterExpression destKvpParam = Expression.Parameter(destKvpType, "destKvp");
// 			LambdaExpression keySelector = Expression.Lambda(Expression.Property(destKvpParam, "Key"), destKvpParam);
// 			LambdaExpression valueSelector = Expression.Lambda(Expression.Property(destKvpParam, "Value"), destKvpParam);

// 			MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);
// 			MethodCallExpression toDictionaryCall = Expression.Call(null, toDictionaryMethod, selectCall, keySelector, valueSelector);

// 			return Expression.Assign(destAccess, toDictionaryCall);
// 		}
// 		else
// 		{
// 			MethodInfo mapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceElementType, destElementType);
// 			MethodInfo selectMethod = MethodInfoCache.Select.MakeGenericMethod(sourceElementType, destElementType);

// 			ParameterExpression itemParam = Expression.Parameter(sourceElementType, "item");
// 			MethodCallExpression mapCall = Expression.Call(null, mapMethod, itemParam, Expression.Constant(useCache, typeof(bool)));
// 			LambdaExpression selectLambda = Expression.Lambda(mapCall, itemParam);

// 			MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);

// 			Expression finalExpression;
// 			if (destType.IsGenericType)
// 			{
// 				Type genericTypeDef = destType.GetGenericTypeDefinition();
// 				if (genericTypeDef == typeof(HashSet<>))
// 				{
// 					MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(destElementType);
// 					finalExpression = Expression.Call(null, toHashSetMethod, selectCall);
// 				}
// 				else if (genericTypeDef == typeof(Stack<>))
// 				{
// 					//Must reverse order before inserting values into Stack to preserve original order
// 					MethodInfo reverseMethod = MethodInfoCache.Reverse.MakeGenericMethod(destElementType);
// 					MethodCallExpression reverseCall = Expression.Call(null, reverseMethod, selectCall);
// 					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
// 					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, reverseCall);
// 					finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
// 				}
// 				else if (genericTypeDef == typeof(Queue<>))
// 				{
// 					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
// 					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, selectCall);
// 					finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
// 				}
// 				else // Assume List<> or other IEnumerable<> types
// 				{
// 					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
// 					finalExpression = Expression.Call(null, toListMethod, selectCall);
// 				}
// 			}
// 			else if (destType.IsArray)
// 			{
// 				MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
// 				finalExpression = Expression.Call(null, toArrayMethod, selectCall);
// 			}
// 			else
// 			{
// 				throw new InvalidOperationException($"Unsupported collection type: {destType.FullName}");
// 			}

// 			return Expression.Assign(destAccess, finalExpression);
// 		}
// 	}

// 	private static Type GetElementType(Type collectionType)
// 	{
// 		if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
// 		{
// 			return collectionType.GetGenericArguments()[0];
// 		}
// 		else if (collectionType.IsArray)
// 		{
// 			return collectionType.GetElementType()!;
// 		}
// 		else
// 		{
// 			return collectionType.GetInterfaces()
// 				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
// 				.Select(i => i.GetGenericArguments()[0])
// 				.FirstOrDefault() ?? typeof(object);
// 		}
// 	}
// }

// internal static class MethodInfoCache
// {
// 	public static readonly MethodInfo ToList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!;
// 	public static readonly MethodInfo ToArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!;
// 	public static readonly MethodInfo ToHashSet = typeof(Enumerable).GetMethods().First(m => string.Equals(m.Name, nameof(Enumerable.ToHashSet)) && m.GetParameters().Length == 1);
// 	public static readonly MethodInfo FastMap = typeof(FastMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!;
// 	// public static readonly MethodInfo Reverse = typeof(Enumerable).GetMethod(nameof(Enumerable.Reverse))!;
// 	public static readonly MethodInfo Reverse = typeof(Enumerable).GetMethod(nameof(Enumerable.Reverse), BindingFlags.Static | BindingFlags.Public, null, [typeof(IEnumerable<>).MakeGenericType(typeof(object))], null)!;
// 	public static readonly MethodInfo Select = typeof(Enumerable).GetMethods().First(x => string.Equals(x.Name, nameof(Enumerable.Select)) && x.GetParameters().Length == 2)!;
// 	public static readonly MethodInfo ToDictionary = typeof(Enumerable).GetMethods().First(m => string.Equals(m.Name, nameof(Enumerable.ToDictionary)) && m.GetGenericArguments().Length == 3 && m.GetParameters().Length == 3 && m.GetGenericArguments().Select(x => x.Name).Intersect(["TSource", "TKey", "TElement"]).Count() == 3)!;
// }
