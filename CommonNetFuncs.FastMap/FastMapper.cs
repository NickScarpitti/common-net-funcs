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
		{

			return default;
		}


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
					valueExpr = Expression.Condition(Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
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
				continue;   // Incompatible types - skip
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
		{

			throw new InvalidOperationException("Dictionary key types must match");
		}

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
