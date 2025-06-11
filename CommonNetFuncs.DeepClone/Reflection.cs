using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommonNetFuncs.DeepClone;

public static class Reflection
{
    private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Deep clone a non-delegate type class (cloned object doesn't retain memory references) using reflection (mid-tier speed)
    /// </summary>
    /// <typeparam name="T">Type of object to clone</typeparam>
    /// <param name="original">Object to clone</param>
    /// <returns>Clone of the original object</returns>
    [return: NotNullIfNotNull(nameof(original))]
    public static T? DeepClone<T>(this T? original)
    {
        return (T?)original.Copy();
    }

    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(string))
        {
            return true;
        }

        return type.IsValueType && type.IsPrimitive;
    }

    private static object? Copy(this object? originalObject)
    {
        return InternalCopy(originalObject, new Dictionary<object, object?>(new ReferenceEqualityComparer()));
    }

    private static object? InternalCopy(object? originalObject, IDictionary<object, object?> visited)
    {
        if (originalObject == null)
        {
            return null;
        }

        Type typeToReflect = originalObject.GetType();
        if (typeToReflect.IsPrimitive())
        {
            return originalObject;
        }

        if (visited.TryGetValue(originalObject, out object? value))
        {
            return value;
        }

        if (typeof(Delegate).IsAssignableFrom(typeToReflect))
        {
            throw new ArgumentException($"Type {typeToReflect.FullName} is a delegate type which is unsupported.", nameof(originalObject));
        }

        object? cloneObject = CloneMethod!.Invoke(originalObject, null);
        if (typeToReflect.IsArray)
        {
            Type? arrayType = typeToReflect.GetElementType();
            if (!arrayType!.IsPrimitive())
            {
                Array clonedArray = (Array)cloneObject!;

                //clonedArray.SetValue((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                for (int i = 0; i < clonedArray.Length; i++)
                {
                    clonedArray.SetValue(InternalCopy(clonedArray.GetValue(i), visited), i);
                }
            }
        }
        visited.Add(originalObject, cloneObject);
        CopyFields(originalObject, visited, cloneObject, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect)
    {
        if (typeToReflect.BaseType != null)
        {
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
            CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
        }
    }

    private static void CopyFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool>? filter = null)
    {
        foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (filter != null && !filter(fieldInfo))
            {
                continue;
            }

            if (fieldInfo.FieldType.IsPrimitive())
            {
                continue;
            }

            object? originalFieldValue = fieldInfo.GetValue(originalObject);
            object? clonedFieldValue = InternalCopy(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
}
