using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommonNetFuncs.Core;
using FastExpressionCompiler;

using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.FastMap;

public static class FastMapper
{
	#region Caching

	public readonly struct MapperCacheKey(Type sourceType, Type destType) : IEquatable<MapperCacheKey>
	{
		public readonly Type SourceType = sourceType;
		public readonly Type DestType = destType;

		public bool Equals(MapperCacheKey other)
		{
			return other.SourceType == SourceType && other.DestType == DestType;
		}

		public override bool Equals(object? obj)
		{
			return obj is MapperCacheKey mapperCacheKey && Equals(mapperCacheKey);
		}

		public override int GetHashCode()
		{
			return SourceType.GetHashCode() + DestType.GetHashCode();
		}

		public static bool operator ==(MapperCacheKey left, MapperCacheKey right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MapperCacheKey left, MapperCacheKey right)
		{
			return !(left == right);
		}
	}

	private static readonly CacheManager<MapperCacheKey, Delegate> MapperCache = new();

	public static ICacheManagerApi<MapperCacheKey, Delegate> CacheManager => MapperCache;

	/// <summary>
	/// Clears LimitedMapperCache cache and sets the size to the specified value.
	/// </summary>
	private static Delegate GetOrAddPropertiesFromMapperCache<T, UT>(MapperCacheKey key)
	{
		bool isLimitedCache = CacheManager.IsUsingLimitedCache();
		if (isLimitedCache ? CacheManager.GetLimitedCache().TryGetValue(key, out Delegate? function) :
						CacheManager.GetCache().TryGetValue(key, out function))
		{
			return function!;
		}

		function = CreateMapper<T, UT>(true);
		if (isLimitedCache)
		{
			CacheManager.TryAddLimitedCache(key, function);
		}
		else
		{
			CacheManager.TryAddCache(key, function!);
		}
		return function;
	}

	#endregion

	/// <summary>
	/// Method that maps one object onto another by property name using expression trees
	/// </summary>
	/// <typeparam name="T">Type to map data from</typeparam>
	/// <typeparam name="UT">Type to map data to</typeparam>
	/// <param name="source">Object to map data from</param>
	/// <returns>New instance of type UT with values populated from source object</returns>
	[return: NotNullIfNotNull(nameof(source))]
	public static UT? FastMap<T, UT>(this T source, bool useCache = true) where T : class? where UT : class?
	{
		if (source == null)
		{
			return default;
		}

		Func<T, UT> mapper = useCache ? (Func<T, UT>)GetOrAddPropertiesFromMapperCache<T, UT>(new(typeof(T), typeof(UT))) : CreateMapper<T, UT>(useCache);
		return mapper(source)!;
	}

	private static Func<T, UT> CreateMapper<T, UT>(bool useCache)
	{
		ParameterExpression sourceParameter = Expression.Parameter(typeof(T), "source");
		ParameterExpression destinationVariable = Expression.Variable(typeof(UT), "destination");

		Type destType = typeof(UT);
		Type sourceType = typeof(T);

		// Create variable declaration and initial assignment
		List<ParameterExpression> variables = [destinationVariable];
		List<Expression> bindings = [];

		if (typeof(IDictionary).IsAssignableFrom(sourceType) || typeof(IDictionary).IsAssignableFrom(destType))
		{
			// Always initialize the destination
			bindings.Add(Expression.Assign(destinationVariable, Expression.New(typeof(UT))));
			bindings.Add(CreateCollectionMapping(sourceParameter, destinationVariable, sourceType, destType, useCache));
		}
		else if (typeof(IEnumerable).IsAssignableFrom(sourceType) && typeof(IEnumerable).IsAssignableFrom(destType) && sourceType != typeof(string) && destType != typeof(string))
		{
			Type elementType = GetElementType(destType);

			if (destType.IsArray)
			{
				// Create empty array initially
				bindings.Add(Expression.Assign(destinationVariable, Expression.NewArrayBounds(destType.GetElementType()!, Expression.Constant(0))));
			}
			else if (destType.IsReadOnlyCollectionType())
			{
				// Create temporary list for ReadOnlyCollection
				Type listType = typeof(List<>).MakeGenericType(elementType);
				ParameterExpression tempList = Expression.Variable(listType, "tempList");
				variables.Add(tempList);
				bindings.Add(Expression.Assign(tempList, Expression.New(listType)));

				if (destType.IsInterface || (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>)))
				{
					Type concreteType = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
					bindings.Add(Expression.Assign(destinationVariable, Expression.New(concreteType.GetConstructor([typeof(IList<>).MakeGenericType(elementType)])!, tempList)));
				}
				else if (destType.IsGenericType)
				{
					Type genericTypeDef = destType.GetGenericTypeDefinition();
					if (genericTypeDef == typeof(HashSet<>))
					{
						// For HashSet, convert the temporary list to a HashSet
						bindings.Add(Expression.Assign(destinationVariable, Expression.Call(null, MethodInfoCache.ToHashSet.MakeGenericMethod(elementType), tempList)));
					}
					else if (genericTypeDef == typeof(Stack<>) || genericTypeDef == typeof(Queue<>))
					{
						bindings.Add(Expression.Assign(destinationVariable,
														Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)])!, tempList)));
					}
					else
					{
						bindings.Add(Expression.Assign(destinationVariable, tempList)); // For other collection types, try to create from list
					}
				}
				else
				{
					bindings.Add(Expression.Assign(destinationVariable, tempList)); // If destination is not ReadOnlyCollection (e.g. List<T>), assign the list directly
				}
			}
			else if (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(List<>))
			{
				bindings.Add(Expression.Assign(destinationVariable, Expression.New(destType)));
			}
			else
			{
				// For interfaces or other types, try to assign a List<T>
				Type listType = typeof(List<>).MakeGenericType(elementType);
				bindings.Add(Expression.Assign(destinationVariable, Expression.New(listType)));
			}

			bindings.Add(CreateCollectionMapping(sourceParameter, destinationVariable, sourceType, destType, useCache)); // Add collection mapping
		}
		else
		{
			bindings.Add(Expression.Assign(destinationVariable, Expression.New(typeof(UT)))); // Initialize destination object if not a collection
																																												//PropertyInfo[] sourceProperties = propertyCache.GetOrAdd(typeof(T), t => t.GetProperties());
																																												//PropertyInfo[] destinationProperties = propertyCache.GetOrAdd(typeof(UT), t => t.GetProperties());
			PropertyInfo[] sourceProperties = GetOrAddPropertiesFromReflectionCache(typeof(T));
			PropertyInfo[] destinationProperties = GetOrAddPropertiesFromReflectionCache(typeof(UT));
			HashSet<string> assignedProperties = [];
			foreach (PropertyInfo destProp in destinationProperties.Where(x => x.CanWrite))
			{
				PropertyInfo? sourceProp = sourceProperties.FirstOrDefault(x => x.CanRead && string.Equals(x.Name, destProp.Name));
				if (sourceProp != null)
				{
					Expression sourceAccess = Expression.Property(sourceParameter, sourceProp);
					Expression destAccess = Expression.Property(destinationVariable, destProp);

					Expression assignExpression;
					if (sourceProp.PropertyType == destProp.PropertyType)
					{
						// Add null check for reference types
						if (!sourceProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(sourceProp.PropertyType) != null)
						{
							assignExpression = Expression.Assign(destAccess,
																Expression.Condition(Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
																Expression.Default(destProp.PropertyType), sourceAccess));
						}
						else
						{
							assignExpression = Expression.Assign(destAccess, sourceAccess);
						}
					}
					else if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType) && typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType))
					{
						// Add null check for collections
						assignExpression = Expression.Assign(destAccess,
														Expression.Condition(
																Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
																Expression.Default(destProp.PropertyType),
																CreateCollectionMapping(sourceAccess, destAccess, sourceProp.PropertyType, destProp.PropertyType, useCache).Right));
					}
					else if (!sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType)
					{
						MethodInfo nestedMapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceProp.PropertyType, destProp.PropertyType);

						assignExpression = Expression.Assign(destAccess,
														Expression.Condition(
																Expression.Equal(sourceAccess, Expression.Constant(null, sourceProp.PropertyType)),
																Expression.Default(destProp.PropertyType),
																Expression.Call(null, nestedMapMethod, sourceAccess, Expression.Constant(useCache, typeof(bool)))));
					}
					else
					{
						continue;
					}

					assignedProperties.Add(destProp.Name);
					bindings.Add(assignExpression);
				}
			}

			foreach (PropertyInfo destProp in destinationProperties.Where(x => x.CanWrite && !assignedProperties.Contains(x.Name)))
			{
				Expression destAccess = Expression.Property(destinationVariable, destProp);
				if (!destProp.PropertyType.IsValueType && (destProp.PropertyType.IsValueType ? RuntimeHelpers.GetUninitializedObject(destProp.PropertyType) : null) != null)
				{
					ConstructorInfo? constructorInfo = destProp.PropertyType.GetConstructor(Type.EmptyTypes);
					if (constructorInfo != null)
					{
						ConditionalExpression assignIfNull = Expression.IfThen(
														Expression.Equal(destAccess, Expression.Constant(null, destProp.PropertyType)),
														Expression.Assign(destAccess, Expression.New(constructorInfo)));
						bindings.Add(assignIfNull);
					}
				}
			}
		}

		bindings.Add(destinationVariable);

		BlockExpression body = Expression.Block(variables, bindings);
		Expression<Func<T, UT>> lambda = Expression.Lambda<Func<T, UT>>(body, sourceParameter);
		return lambda.CompileFast();
	}

	private static BinaryExpression CreateCollectionMapping(Expression sourceAccess, Expression destAccess, Type sourceType, Type destType, bool useCache)
	{
		Type sourceElementType = GetElementType(sourceType);
		Type destElementType = GetElementType(destType);

		if (sourceElementType == destElementType) //Lists are the same type, so directly assign one to the other
		{
			Expression sourceCollection;
			if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>))
			{
				_ = MethodInfoCache.ToList.MakeGenericMethod(sourceElementType);
				sourceCollection = Expression.Call(null, MethodInfoCache.ToList!.MakeGenericMethod(sourceElementType), sourceAccess);
			}
			else
			{
				sourceCollection = sourceAccess;
			}

			if (destType.IsArray)
			{
				MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(sourceElementType);
				return Expression.Assign(destAccess, Expression.Call(null, toArrayMethod, sourceCollection));
			}
			else if (destType.IsReadOnlyCollectionType())
			{
				Type elementType = GetElementType(destType);
				MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(elementType);
				Expression intermediateList = Expression.Call(null, toListMethod, sourceCollection);

				if (destType.IsInterface || (destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>)))
				{
					Type concreteType = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
					ConstructorInfo ctor = concreteType.GetConstructor([typeof(IList<>).MakeGenericType(elementType)])!;
					return Expression.Assign(destAccess, Expression.New(ctor, intermediateList));
				}
				else if (destType.IsGenericType)
				{
					Type genericTypeDef = destType.GetGenericTypeDefinition();
					if (genericTypeDef == typeof(HashSet<>))
					{
						MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(elementType);
						return Expression.Assign(destAccess, Expression.Call(null, toHashSetMethod, sourceCollection));
					}
					else if (genericTypeDef == typeof(Stack<>) || genericTypeDef == typeof(Queue<>))
					{
						return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)])!, intermediateList));
					}
					else if (genericTypeDef == typeof(List<>))
					{
						return Expression.Assign(destAccess, intermediateList);
					}
				}

				// For any other type, convert to appropriate collection type
				MethodInfo toCollectionMethod = MethodInfoCache.ToList.MakeGenericMethod(elementType);
				return Expression.Assign(destAccess, Expression.Call(null, toCollectionMethod, sourceCollection));
			}
			else if (destType.IsGenericType)
			{
				Type genericTypeDef = destType.GetGenericTypeDefinition();
				if (genericTypeDef == typeof(List<>))
				{
					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
					return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceCollection));
				}
				else if (genericTypeDef == typeof(HashSet<>))
				{
					MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(destElementType);
					return Expression.Assign(destAccess, Expression.Call(null, toHashSetMethod, sourceCollection));
				}
				else if (genericTypeDef == typeof(Stack<>))
				{
					//Must reverse order before inserting values into Stack to preserve original order
					MethodInfo reverseMethod = MethodInfoCache.Reverse.MakeGenericMethod(destElementType);
					MethodCallExpression reverseCall = Expression.Call(null, reverseMethod, sourceAccess);
					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, reverseCall);
					return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall));
				}
				else if (genericTypeDef == typeof(Queue<>))
				{
					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, sourceAccess);
					return Expression.Assign(destAccess, Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall));
				}
				else // List<> or other IEnumerable<> types
				{
					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
					return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceAccess));
				}
			}
			else
			{
				MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(sourceElementType);
				return Expression.Assign(destAccess, Expression.Call(null, toListMethod, sourceAccess));
			}
		}
		else if (typeof(IDictionary).IsAssignableFrom(sourceType) || typeof(IDictionary).IsAssignableFrom(destType))
		{
			if (!(typeof(IDictionary).IsAssignableFrom(sourceType) && typeof(IDictionary).IsAssignableFrom(destType)))
			{
				throw new InvalidOperationException("Both source and destination must be a dictionary in order to be mapped");
			}

			Type sourceKeyType = sourceType.GetGenericArguments()[0];
			Type sourceValueType = sourceType.GetGenericArguments()[1];
			Type destKeyType = destType.GetGenericArguments()[0];
			Type destValueType = destType.GetGenericArguments()[1];

			if (sourceKeyType != destKeyType)
			{
				throw new InvalidOperationException("Source and destination dictionary key types must match.");
			}

			Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(sourceKeyType, sourceValueType);
			Type destKvpType = typeof(KeyValuePair<,>).MakeGenericType(destKeyType, destValueType);

			ParameterExpression kvpParam = Expression.Parameter(kvpType, "kvp");
			PropertyInfo keyProp = kvpType.GetProperty("Key")!;
			PropertyInfo valueProp = kvpType.GetProperty("Value")!;

			Expression keyAccess = Expression.Property(kvpParam, keyProp);
			Expression valueAccess = Expression.Property(kvpParam, valueProp);

			MethodInfo mapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceValueType, destValueType);
			Expression mappedValue = Expression.Call(null, mapMethod, valueAccess, Expression.Constant(useCache, typeof(bool)));

			NewExpression newKvp = Expression.New(destKvpType.GetConstructor([destKeyType, destValueType])!, keyAccess, mappedValue);
			LambdaExpression selectLambda = Expression.Lambda(newKvp, kvpParam);

			MethodInfo selectMethod = MethodInfoCache.Select.MakeGenericMethod(kvpType, destKvpType);

			MethodInfo toDictionaryMethod = MethodInfoCache.ToDictionary.MakeGenericMethod(destKvpType, destKeyType, destValueType);

			ParameterExpression destKvpParam = Expression.Parameter(destKvpType, "destKvp");
			LambdaExpression keySelector = Expression.Lambda(Expression.Property(destKvpParam, "Key"), destKvpParam);
			LambdaExpression valueSelector = Expression.Lambda(Expression.Property(destKvpParam, "Value"), destKvpParam);

			MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);
			MethodCallExpression toDictionaryCall = Expression.Call(null, toDictionaryMethod, selectCall, keySelector, valueSelector);

			return Expression.Assign(destAccess, toDictionaryCall);
		}
		else
		{
			MethodInfo mapMethod = MethodInfoCache.FastMap.MakeGenericMethod(sourceElementType, destElementType);
			MethodInfo selectMethod = MethodInfoCache.Select.MakeGenericMethod(sourceElementType, destElementType);

			ParameterExpression itemParam = Expression.Parameter(sourceElementType, "item");
			MethodCallExpression mapCall = Expression.Call(null, mapMethod, itemParam, Expression.Constant(useCache, typeof(bool)));
			LambdaExpression selectLambda = Expression.Lambda(mapCall, itemParam);

			MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);

			Expression finalExpression;
			if (destType.IsGenericType)
			{
				Type genericTypeDef = destType.GetGenericTypeDefinition();
				if (genericTypeDef == typeof(HashSet<>))
				{
					MethodInfo toHashSetMethod = MethodInfoCache.ToHashSet.MakeGenericMethod(destElementType);
					finalExpression = Expression.Call(null, toHashSetMethod, selectCall);
				}
				else if (genericTypeDef == typeof(Stack<>))
				{
					//Must reverse order before inserting values into Stack to preserve original order
					MethodInfo reverseMethod = MethodInfoCache.Reverse.MakeGenericMethod(destElementType);
					MethodCallExpression reverseCall = Expression.Call(null, reverseMethod, selectCall);
					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, reverseCall);
					finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
				}
				else if (genericTypeDef == typeof(Queue<>))
				{
					MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
					MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, selectCall);
					finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
				}
				else // Assume List<> or other IEnumerable<> types
				{
					MethodInfo toListMethod = MethodInfoCache.ToList.MakeGenericMethod(destElementType);
					finalExpression = Expression.Call(null, toListMethod, selectCall);
				}
			}
			else if (destType.IsArray)
			{
				MethodInfo toArrayMethod = MethodInfoCache.ToArray.MakeGenericMethod(destElementType);
				finalExpression = Expression.Call(null, toArrayMethod, selectCall);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported collection type: {destType.FullName}");
			}

			return Expression.Assign(destAccess, finalExpression);
		}
	}

	private static Type GetElementType(Type collectionType)
	{
		if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
		{
			return collectionType.GetGenericArguments()[0];
		}
		else if (collectionType.IsArray)
		{
			return collectionType.GetElementType()!;
		}
		else
		{
			return collectionType.GetInterfaces()
								.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
								.Select(i => i.GetGenericArguments()[0])
								.FirstOrDefault() ?? typeof(object);
		}
	}
}

internal static class MethodInfoCache
{
	public static readonly MethodInfo ToList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!;
	public static readonly MethodInfo ToArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!;
	public static readonly MethodInfo ToHashSet = typeof(Enumerable).GetMethods().First(m => string.Equals(m.Name, nameof(Enumerable.ToHashSet)) && m.GetParameters().Length == 1);
	public static readonly MethodInfo FastMap = typeof(FastMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!;
	// public static readonly MethodInfo Reverse = typeof(Enumerable).GetMethod(nameof(Enumerable.Reverse))!;
	public static readonly MethodInfo Reverse = typeof(Enumerable).GetMethod(nameof(Enumerable.Reverse), BindingFlags.Static | BindingFlags.Public, null, [typeof(IEnumerable<>).MakeGenericType(typeof(object))], null)!;
	public static readonly MethodInfo Select = typeof(Enumerable).GetMethods().First(x => string.Equals(x.Name, nameof(Enumerable.Select)) && x.GetParameters().Length == 2)!;
	public static readonly MethodInfo ToDictionary = typeof(Enumerable).GetMethods().First(m => string.Equals(m.Name, nameof(Enumerable.ToDictionary)) && m.GetGenericArguments().Length == 3 && m.GetParameters().Length == 3 && m.GetGenericArguments().Select(x => x.Name).Intersect(["TSource", "TKey", "TElement"]).Count() == 3)!;
}
