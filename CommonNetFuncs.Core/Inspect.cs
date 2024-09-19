using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;

namespace CommonNetFuncs.Core;

public static class Inspect
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Gets the default value of the provided type
    /// </summary>
    /// <param name="type">Type to get the default value of</param>
    /// <returns>The default value of the provided type</returns>
    public static object? GetDefaultValue(this Type type)
    {
        return type.IsValueType ? RuntimeHelpers.GetUninitializedObject(type) : null;
    }

    /// <summary>
    /// Get the number of properties in a class that are set to their default value
    /// </summary>
    /// <param name="obj">Object to count default properties in</param>
    /// <returns>Number of properties in a class that are set to their default value</returns>
    public static int CountDefaultProps<T>(this T obj) where T : class
    {
        return typeof(T).GetProperties().Count(x => x.CanWrite && x.GetValue(obj) == x.PropertyType.GetDefaultValue());
    }

    /// <summary>
    /// Returns whether a Type has the specified attribute or not
    /// </summary>
    /// <param name="type">The type to check for the specified attribute</param>
    /// <param name="attributeName">The name of the attribute you are checking the provided type for</param>
    /// <returns>True if the object has the specified attribute</returns>
    public static bool ObjectHasAttribute(this Type type, string attributeName)
    {
        bool hasAttribute = false;
        foreach (object item in type.GetCustomAttributes(true))
        {
            object? typeIdObject = item.GetType().GetProperty("TypeId")?.GetValue(item, null);

            if (typeIdObject != null)
            {
                string? attrName = typeIdObject.GetType().GetProperty("Name")?.GetValue(typeIdObject, null)?.ToString();
                if (attrName?.StrEq(attributeName) == true)
                {
                    hasAttribute = true;
                    break;
                }
            }
        }
        return hasAttribute;
    }

    /// <summary>
    /// Compares two like objects against each other to check to see if they contain the same values
    /// </summary>
    /// <param name="obj1">First object to compare for value equality</param>
    /// <param name="obj2">Second object to compare for value equality</param>
    /// <returns>True if the two objects have the same value for all elements</returns>
    public static bool IsEqualR(this object? obj1, object? obj2)
    {
        return obj1.IsEqualR(obj2, null);
    }

    /// <summary>
    /// Compare two class objects for value equality
    /// </summary>
    /// <param name="obj1">First object to compare for value equality</param>
    /// <param name="obj2">First object to compare for value equality</param>
    /// <param name="exemptProps">Names of properties to not include in the matching check</param>
    /// <returns>True if both objects contain identical values for all properties except for the ones identified by exemptProps</returns>
    public static bool IsEqualR(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null)
    {
        // They're both null.
        if (obj1 == null && obj2 == null)
        {
            return true;
        }

        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null)
        {
            return false;
        }

        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType())
        {
            return false;
        }

        IEnumerable<PropertyInfo> props = obj1.GetType().GetProperties();
        if (exemptProps?.Any() == true)
        {
            props = props.Where(x => exemptProps?.Contains(x.Name) != true);
        }

        foreach (PropertyInfo prop in props)
        {
            object aPropValue = prop.GetValue(obj1) ?? string.Empty;
            object bPropValue = prop.GetValue(obj2) ?? string.Empty;

            bool aIsNumeric = aPropValue.IsNumeric();
            bool bIsNumeric = bPropValue.IsNumeric();

            try
            {
                //This will prevent issues with numbers with varying decimal places from being counted as a difference
                if (aIsNumeric && bIsNumeric && decimal.Parse(aPropValue.ToString()!) != decimal.Parse(bPropValue.ToString()!) ||
                    !(aIsNumeric && bIsNumeric) && aPropValue.ToString() != bPropValue.ToString())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                return false;
            }
        }
        return true;
    }

    private static readonly ConcurrentDictionary<Tuple<Type, bool, bool>, Func<object, object, IEnumerable<string>, bool>> CompareDelegates = [];
    public static bool IsEqual(this object? obj1, object? obj2, IEnumerable<string>? exemptProps = null, bool ignoreStringCase = false, bool recursive = true)
    {
        // They're both null.
        if (obj1 == null && obj2 == null)
        {
            return true;
        }

        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null)
        {
            return false;
        }

        Type type = obj1.GetType();

        // How can they be the same if they're different types?
        if (type != obj2.GetType())
        {
            return false;
        }

        exemptProps ??= [];
        Tuple<Type, bool, bool> key = Tuple.Create(type, ignoreStringCase, recursive);
        if (!CompareDelegates.TryGetValue(key, out Func<object, object, IEnumerable<string>, bool>? compareDelegate))
        {
            compareDelegate = CreateCompareDelegate(type, ignoreStringCase, recursive);
            CompareDelegates[key] = compareDelegate;
        }

        return compareDelegate(obj1, obj2, exemptProps);
    }

    private static Func<object, object, IEnumerable<string>, bool> CreateCompareDelegate(Type type, bool ignoreStringCase, bool recursive)
    {
        ParameterExpression obj1Param = Expression.Parameter(typeof(object), "obj1");
        ParameterExpression obj2Param = Expression.Parameter(typeof(object), "obj2");
        ParameterExpression exemptPropsParam = Expression.Parameter(typeof(IEnumerable<string>), "exemptProps");

        UnaryExpression typedObj1 = Expression.Convert(obj1Param, type);
        UnaryExpression typedObj2 = Expression.Convert(obj2Param, type);

        IEnumerable<PropertyInfo> properties = type.GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        List<Expression> comparisons = [];

        foreach (PropertyInfo prop in properties)
        {
            MethodCallExpression propExemptCheck = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(string)], exemptPropsParam, Expression.Constant(prop.Name));

            MemberExpression value1 = Expression.Property(typedObj1, prop);
            MemberExpression value2 = Expression.Property(typedObj2, prop);

            Expression comparison;

            if (prop.PropertyType == typeof(string))
            {
                if (ignoreStringCase)
                {
                    MethodInfo? equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;
                    comparison = Expression.Call(equalsMethod, value1, value2, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    MethodInfo? equalsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!;
                    comparison = Expression.Call(equalsMethod, value1, value2);
                }
            }
            else if (prop.PropertyType.IsValueType)
            {
                comparison = Expression.Equal(value1, value2);
            }
            else if (typeof(IComparable).IsAssignableFrom(prop.PropertyType))
            {
                comparison = Expression.Equal(Expression.Call(value1, "CompareTo", null, value2), Expression.Constant(0));
            }
            else if (recursive && !prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
            {
                MethodInfo? isEqualMethod = typeof(Inspect).GetMethod(nameof(IsEqual), [typeof(object), typeof(object), typeof(IEnumerable<string>), typeof(bool), typeof(bool)])!;
                comparison = Expression.Call(isEqualMethod, value1, value2, Expression.Constant(null, typeof(IEnumerable<string>)), Expression.Constant(ignoreStringCase), Expression.Constant(recursive));
            }
            else
            {
                //comparison = Expression.Equal(value1, value2);
                comparison = Expression.Constant(true);
            }

            comparisons.Add(Expression.Condition(propExemptCheck, Expression.Constant(true), comparison));
        }

        Expression andAlsoExpression = comparisons.Aggregate(Expression.And);

        Expression<Func<object, object, IEnumerable<string>, bool>> lambda = Expression.Lambda<Func<object, object, IEnumerable<string>, bool>>(
            andAlsoExpression, obj1Param, obj2Param, exemptPropsParam);

        return lambda.Compile();
    }

    /// <summary>
    /// Get hash of an object
    /// </summary>
    /// <param name="obj">Object to get hash code of</param>
    /// <returns>Hash string of object</returns>
    public static int GetHashCode<T>(this T obj)
    {
        PropertyInfo[]? props = obj?.GetType().GetProperties();
        string? allProps = null;
        if (props != null)
        {
            //Order by here makes this consistent
            foreach (PropertyInfo prop in props.OrderBy(x => x.Name))
            {
                object propValue = prop.GetValue(obj) ?? string.Empty;
                allProps += propValue;
            }
        }
        return allProps?.GetHashCode() ?? 0;
    }
}
