using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using static CommonNetFuncs.Core.TypeChecks;

namespace CommonNetFuncs.FastMap;

public static class ExpressionTreeMapper
{
    private static readonly ConcurrentDictionary<(Type, Type), Delegate> mapperCache = [];

    /// <summary>
    /// Method that maps one object onto another by property name using expression trees
    /// </summary>
    /// <typeparam name="T">Type to map data from</typeparam>
    /// <typeparam name="UT">Type to map data to</typeparam>
    /// <param name="source">Object to map data from</param>
    /// <returns>New instance of type UT with values populated from source object</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static UT? FastMap<T, UT>(this T source)
    {
        if (source == null) { return default; }

        Func<T, UT> mapper = (Func<T, UT>)mapperCache.GetOrAdd((typeof(T), typeof(UT)), _ => CreateMapper<T, UT>());
        return mapper(source)!;
    }

    private static Func<T, UT> CreateMapper<T, UT>()
    {
        ParameterExpression sourceParameter = Expression.Parameter(typeof(T), "source");
        ParameterExpression destinationVariable = Expression.Variable(typeof(UT), "destination");
        BinaryExpression destinationAssignment = Expression.Assign(destinationVariable, Expression.New(typeof(UT)));

        List<Expression> bindings = [];
        bindings.Add(destinationAssignment);

        PropertyInfo[] sourceProperties = typeof(T).GetProperties(); //.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        PropertyInfo[] destinationProperties = typeof(UT).GetProperties(); //.GetProperties(BindingFlags.Public | BindingFlags.Instance)

        // Check if source and destination are IEnumerable and not strings (since strings are char[])
        if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(string) && typeof(IEnumerable).IsAssignableFrom(typeof(UT)) && typeof(T) != typeof(string))
        {
            // If both source and destination are IEnumerable, map the entire collection
            bindings.Add(CreateCollectionMapping(sourceParameter, destinationVariable, typeof(T), typeof(UT)));
        }
        else
        {
            foreach (PropertyInfo destProp in destinationProperties.Where(x => x.CanWrite))
            {
                PropertyInfo? sourceProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name && p.CanRead);
                if (sourceProp != null)
                {
                    Expression sourceAccess = Expression.Property(sourceParameter, sourceProp);
                    Expression destAccess = Expression.Property(destinationVariable, destProp);

                    Expression assignExpression;

                    if (sourceProp.PropertyType == destProp.PropertyType) //Direct assignment
                    {
                        assignExpression = Expression.Assign(destAccess, sourceAccess);
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType) && typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType)) //Is collection, so need to use collection specific logic
                    {
                        assignExpression = CreateCollectionMapping(sourceAccess, destAccess, sourceProp.PropertyType, destProp.PropertyType);
                    }
                    else if (!sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType) //Is a class, so recursively call FastMap
                    {
                        MethodInfo nestedMapMethod = typeof(ExpressionTreeMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(sourceProp.PropertyType, destProp.PropertyType);
                        assignExpression = Expression.Assign(destAccess, Expression.Call(null, nestedMapMethod, sourceAccess));
                    }
                    else
                    {
                        continue; // Skip incompatible types
                    }

                    bindings.Add(assignExpression);
                }
            }
        }

        bindings.Add(destinationVariable);

        BlockExpression body = Expression.Block([destinationVariable], bindings);
        Expression<Func<T, UT>> lambda = Expression.Lambda<Func<T, UT>>(body, sourceParameter);
        return lambda.Compile();
    }

    private static BinaryExpression CreateCollectionMapping(Expression sourceAccess, Expression destAccess, Type sourceType, Type destType)
    {
        Type sourceElementType = GetElementType(sourceType);
        Type destElementType = GetElementType(destType);

        if (sourceElementType == destElementType) //Lists are the same type, so directly assign one to the other
        {
            return Expression.Assign(destAccess, Expression.Call(typeof(Enumerable), "ToList", [sourceElementType], sourceAccess));
        }
        else if (sourceType.IsDictionary() || destType.IsDictionary())
        {
            if (!(sourceType.IsDictionary() && destType.IsDictionary()))
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

            MethodInfo mapMethod = typeof(ExpressionTreeMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(sourceValueType, destValueType);
            Expression mappedValue = Expression.Call(null, mapMethod, valueAccess);

            NewExpression newKvp = Expression.New(destKvpType.GetConstructor([destKeyType, destValueType])!, keyAccess, mappedValue);
            LambdaExpression selectLambda = Expression.Lambda(newKvp, kvpParam);

            MethodInfo selectMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Select" && m.GetParameters().Length == 2).MakeGenericMethod(kvpType, destKvpType);

            MethodInfo toDictionaryMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "ToDictionary" && m.GetGenericArguments().Length == 3 && m.GetParameters().Length == 3 && m.GetGenericArguments().Select(x => x.Name).Intersect(["TSource", "TKey", "TElement"]).Count() == 3)
                .MakeGenericMethod(destKvpType, destKeyType, destValueType);

            ParameterExpression destKvpParam = Expression.Parameter(destKvpType, "destKvp");
            LambdaExpression keySelector = Expression.Lambda(Expression.Property(destKvpParam, "Key"), destKvpParam);
            LambdaExpression valueSelector = Expression.Lambda(Expression.Property(destKvpParam, "Value"), destKvpParam);

            MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);
            MethodCallExpression toDictionaryCall = Expression.Call(null, toDictionaryMethod, selectCall, keySelector, valueSelector);

            return Expression.Assign(destAccess, toDictionaryCall);
        }
        else
        {
            MethodInfo mapMethod = typeof(ExpressionTreeMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(sourceElementType, destElementType);
            MethodInfo selectMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Select" && m.GetParameters().Length == 2).MakeGenericMethod(sourceElementType, destElementType);

            ParameterExpression itemParam = Expression.Parameter(sourceElementType, "item");
            MethodCallExpression mapCall = Expression.Call(null, mapMethod, itemParam);
            LambdaExpression selectLambda = Expression.Lambda(mapCall, itemParam);

            MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);

            Expression finalExpression;
            if (destType.IsGenericType)
            {
                Type genericTypeDef = destType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(HashSet<>))
                {
                    MethodInfo toHashSetMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "ToHashSet" && m.GetParameters().Length == 1).MakeGenericMethod(destElementType);
                    finalExpression = Expression.Call(null, toHashSetMethod, selectCall);
                }
                else if (genericTypeDef == typeof(Stack<>))
                {
                    //Must reverse order before inserting values into Stack to preserve original order
                    MethodInfo reverseMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Reverse))!.MakeGenericMethod(destElementType);
                    MethodCallExpression reverseCall = Expression.Call(null, reverseMethod, selectCall);
                    MethodInfo toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(destElementType);
                    MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, reverseCall);
                    finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
                }
                else if (genericTypeDef == typeof(Queue<>))
                {
                    MethodInfo toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(destElementType);
                    MethodCallExpression toArrayCall = Expression.Call(null, toArrayMethod, selectCall);
                    finalExpression = Expression.New(destType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElementType)])!, toArrayCall);
                }
                else // Assume List<> or other IEnumerable<> types
                {
                    MethodInfo toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(destElementType);
                    finalExpression = Expression.Call(null, toListMethod, selectCall);
                }
            }
            else if (destType.IsArray)
            {
                MethodInfo toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!.MakeGenericMethod(destElementType);
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
