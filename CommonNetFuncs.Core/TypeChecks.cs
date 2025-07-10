using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CommonNetFuncs.Core;

public static class TypeChecks
{
    private static readonly ConcurrentDictionary<Type, bool> SimpleTypeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> ReadOnlyCollectionTypeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> NumericTypeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> EnumerableTypeCache = new();

    public static void ClearTypeCheckCaches()
    {
        SimpleTypeCache.Clear();
        ReadOnlyCollectionTypeCache.Clear();
        NumericTypeCache.Clear();
        EnumerableTypeCache.Clear();
    }

    public static void ClearSimpleTypeCache()
    {
        SimpleTypeCache.Clear();
    }

    public static void ClearReadOnlyCollectionTypeCache()
    {
        ReadOnlyCollectionTypeCache.Clear();
    }

    public static void ClearNumericTypeCache()
    {
        NumericTypeCache.Clear();
    }

    public static void ClearEnumerableTypeCache()
    {
        EnumerableTypeCache.Clear();
    }

    /// <summary>
    /// Checks to see if a type is a delegate
    /// </summary>
    /// <param name="type">Type to check to see if it's a delegate</param>
    /// <returns>True if type parameter is a delegate</returns>
    public static bool IsDelegate(this Type type)
    {
        return typeof(Delegate).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks to see if a type is an array
    /// </summary>
    /// <param name="type">Type to check if it's an array</param>
    /// <returns>True if type parameter is an array</returns>
    public static bool IsArray(this Type type)
    {
        return type.IsArray;
    }

    /// <summary>
    /// Checks to see if a type implements IDictionary
    /// </summary>
    /// <param name="type">Type to check if it implements IDictionary</param>
    /// <returns>True if type parameter implements IDictionary</returns>
    public static bool IsDictionary(this Type type)
    {
        return typeof(IDictionary).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks to see if a type implements IEnumerable and is not a string
    /// </summary>
    /// <param name="type">Type to check if it implements IEnumerable</param>
    /// <returns>True if type parameter implements IEnumerable and is not a string</returns>
    public static bool IsEnumerable(this Type type)
    {
        return EnumerableTypeCache.GetOrAdd(type, t => typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string));
    }

    /// <summary>
    /// Checks to see if a type is a class other than a string
    /// </summary>
    /// <param name="type">Type to check to see if it's a class other than a string</param>
    /// <returns>True if type parameter is a class other than a string</returns>
    public static bool IsClassOtherThanString(this Type? type)
    {
        return type?.IsValueType != false || type != typeof(string);
    }

    /// <summary>
    /// Check if an object is a numeric type
    /// </summary>
    /// <param name="value">Object to check to see if it's numeric</param>
    /// <returns>True if value is a numeric type</returns>
    public static bool IsNumeric(this object? value)
    {
        return (value?.GetType()).IsNumericType();
    }

    /// <summary>
    /// Check if a type is a numeric type
    /// </summary>
    /// <param name="type">Type to check to see if it's numeric</param>
    /// <returns>True if type is a numeric type</returns>
    public static bool IsNumericType(this Type? type)
    {
        if (type == null)
        {
            return false;
        }

        return NumericTypeCache.GetOrAdd(type, x =>
        {
            if (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type? underlyingType = Nullable.GetUnderlyingType(x);
                if (underlyingType != null)
                {
                    x = underlyingType;
                }
            }
            return Type.GetTypeCode(x) is TypeCode.Int32 or TypeCode.Int64 or TypeCode.Double or TypeCode.Decimal or TypeCode.Int16 or TypeCode.Byte or TypeCode.SByte or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Single;
        });
    }

    /// <summary>
    /// <para>Returns true if passed in type is a simple type, which includes:</para> <para>Primitives, Enum, String, Decimal, DateTime, DateTimeOffset, TimeSpan, Guid</para>
    /// </summary>
    /// <param name="type">Type to check if it's a simple type</param>
    /// <returns>
    /// <para>True if type is:</para> <para>Primitive, Enum, String, Decimal, DateTime, DateTimeOffset, TimeSpan, Guid</para>
    /// </returns>
    //public static bool IsSimpleType(this Type type)
    //{
    //    return type.IsPrimitive || type.IsEnum
    //        || type == typeof(string)
    //        || type == typeof(decimal)
    //        || type == typeof(DateTime)
    //        || type == typeof(DateTimeOffset)
    //        || type == typeof(TimeSpan)
    //        || type == typeof(Guid);
    //}

    public static bool IsSimpleType(this Type type)
    {
        return SimpleTypeCache.GetOrAdd(type, x => x.IsPrimitive ||
            x.IsEnum ||
            x == typeof(string) ||
            x == typeof(decimal) ||
            x == typeof(DateTime) ||
            x == typeof(DateTimeOffset) ||
            x == typeof(TimeSpan) ||
            x == typeof(Guid) ||
            (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(x.GetGenericArguments()[0])));
    }

    // Duplicated in CommonNetFuncs.FastMap.FastMapper to remove dependency
    /// <summary>
    /// Checks if the specified type is a read-only collection type, such as IReadOnlyCollection<T>, IReadOnlyList<T>, or ReadOnlyCollection<T>.
    /// </summary>
    /// <param name="type">Type to check if it's a read-only collection type</param>
    /// <returns>True if type is a read-only collection type</returns>
    public static bool IsReadOnlyCollectionType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        // Check if the type itself is a generic IReadOnlyCollection<> or IReadOnlyList<> or ReadOnlyCollection<>
        //if (type.IsGenericType)
        //{
        //    Type genericType = type.GetGenericTypeDefinition();
        //    if (genericType == typeof(IReadOnlyCollection<>) ||
        //        genericType == typeof(IReadOnlyList<>) ||
        //        genericType == typeof(ReadOnlyCollection<>))
        //    {
        //        return true;
        //    }
        //}

        //if (type.IsArray)
        //{
        //    return false;
        //}

        //if (type.Name.Equals("List`1"))
        //{
        //    return false;
        //}

        //if (type.Name.Equals("Dictionary`2"))
        //{
        //    return false;
        //}

        //if (type.Name.Equals("ReadOnlyDictionary"))
        //{
        //    return true;
        //}

        //if (type.BaseType?.Name == "Array")
        //{
        //    return false;
        //}

        //// Check all interfaces (including inherited) for IReadOnlyCollection<> or IReadOnlyList<>
        //return type.GetInterfaces().Any(interfaceType =>
        //{
        //    if (!interfaceType.IsGenericType)
        //    {
        //        return false;
        //    }
        //    Type genericInterfaceType = interfaceType.GetGenericTypeDefinition();
        //    return interfaceType.IsGenericType && (genericInterfaceType == typeof(IReadOnlyCollection<>) || genericInterfaceType == typeof(IReadOnlyList<>));
        //});

        // Direct generic type checks
        return ReadOnlyCollectionTypeCache.GetOrAdd(type, x =>
        {
            if (x.IsGenericType)
            {
                Type genericType = x.GetGenericTypeDefinition();
                if (genericType == typeof(IReadOnlyCollection<>) ||
                    genericType == typeof(IReadOnlyList<>) ||
                    genericType == typeof(ReadOnlyCollection<>))
                {
                    return true;
                }
                // Exclude List<> and Dictionary<,>
                if (genericType == typeof(List<>) || genericType == typeof(Dictionary<,>))
                {
                    return false;
                }
            }

            // Exclude arrays
            if (x.IsArray)
            {
                return false;
            }

            // Check for ReadOnlyDictionary<,>
            if (x.IsGenericType && x.GetGenericTypeDefinition().FullName?.StartsWith("System.Collections.ObjectModel.ReadOnlyDictionary`2") == true)
            {
                return true;
            }

            // Check all interfaces for IReadOnlyCollection<> or IReadOnlyList<>
            foreach (Type interfaceType in x.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    Type genericInterfaceType = interfaceType.GetGenericTypeDefinition();
                    if (genericInterfaceType == typeof(IReadOnlyCollection<>) ||
                        genericInterfaceType == typeof(IReadOnlyList<>))
                    {
                        return true;
                    }
                }
            }
            return false;
        });
    }
}
