using System.Collections;
using System.Globalization;

namespace CommonNetFuncs.Core;

public static class TypeChecks
{
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
        return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    /// <summary>
    /// Checks to see if a type is a class other than a string
    /// </summary>
    /// <param name="type">Type to check to see if it's a class other than a string</param>
    /// <returns>True if type parameter is a class other than a string</returns>
    public static bool IsClassOtherThanString(this Type? type)
    {
        return type == null || (!type.IsValueType && type != typeof(string)); //Added type == null || - Nick
    }

    /// <summary>
    /// Check if an object is a numeric type
    /// </summary>
    /// <param name="testObject">Object to check to see if it's numeric</param>
    /// <returns>True if testObject is a numeric type</returns>
    public static bool IsNumeric(this object? testObject)
    {
        bool isNumeric = false;
        if (!string.IsNullOrWhiteSpace(testObject?.ToString()))
        {
            isNumeric = decimal.TryParse(testObject.ToString(), NumberStyles.Number, NumberFormatInfo.InvariantInfo, out _);
        }
        return isNumeric;
    }

    /// <summary>
    /// <para>Returns true if passed in type is a simple type, which includes:</para>
    /// <para>Primitives, Enum, String, Decimal, DateTime, DateTimeOffset, TimeSpan, Guid</para>
    /// </summary>
    /// <param name="type">Type to check if it's a simple type</param>
    /// <returns>
    /// <para>True if type is:</para>
    /// <para>Primitive, Enum, String, Decimal, DateTime, DateTimeOffset, TimeSpan, Guid</para>
    /// </returns>
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }
}
