using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Common_Net_Funcs.Tools;

public static class ExpressionTreeMapper
{
    private static readonly ConcurrentDictionary<(Type, Type), Delegate> _cachedMappers = [];

    [return: NotNullIfNotNull(nameof(source))]
    public static UT? FastMap<T, UT>(this T source)
    {
        if (source == null) return default;

        Func<T, UT> mapper = (Func<T, UT>)_cachedMappers.GetOrAdd((typeof(T), typeof(UT)), _ => CreateMapper<T, UT>());
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
        else
        {
            MethodInfo mapMethod = typeof(ExpressionTreeMapper).GetMethod(nameof(FastMap), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(sourceElementType, destElementType);
            MethodInfo selectMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Select" && m.GetParameters().Length == 2).MakeGenericMethod(sourceElementType, destElementType);
            MethodInfo toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(destElementType);

            ParameterExpression itemParam = Expression.Parameter(sourceElementType, "item");
            MethodCallExpression mapCall = Expression.Call(null, mapMethod, itemParam);
            LambdaExpression selectLambda = Expression.Lambda(mapCall, itemParam);

            MethodCallExpression selectCall = Expression.Call(null, selectMethod, sourceAccess, selectLambda);
            MethodCallExpression toListCall = Expression.Call(null, toListMethod, selectCall);

            return Expression.Assign(destAccess, toListCall);
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
