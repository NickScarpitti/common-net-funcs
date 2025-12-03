using System.Reflection;

namespace CommonNetFuncs.DeepClone;

public static class Reflection
{
	private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

	///// <summary>
	///// Deep clone a non-delegate type class (cloned object doesn't retain memory references) using reflection (mid-tier speed)
	///// </summary>
	///// <typeparam name="T">Type of object to clone.</typeparam>
	///// <param name="original">Object to clone.</param>
	///// <returns>An exact copy of the original object that is distinct from the original object.</returns>
	//[Obsolete("Please use CommonNetFuncs.DeepClone.ExpressionTrees.DeepClone instead")]
	// [return: NotNullIfNotNull(nameof(original))]
	// public static T? DeepCloneR<T>(this T? original)
	// {
	//   return (T?)original.Copy();
	// }

	/// <summary>
	/// Checks if a type is primitive or not.
	/// </summary>
	/// <param name="type">Type to check if it's a primitive or not.</param>
	/// <returns><see langword="true"/> if the type is a primitive for the purpose of deep cloning via reflection.</returns>
	internal static bool IsPrimitive(this Type type)
	{
		if (type == typeof(string))
		{
			return true;
		}

		return type.IsValueType && type.IsPrimitive;
	}

	///// <summary>
	///// Deep clone a non-delegate type class (cloned object doesn't retain memory references) using reflection (mid-tier speed).
	///// </summary>
	///// <param name="originalObject">Object to clone.</param>
	///// <returns>An exact copy of the original object that is distinct from the original object.</returns>
	//private static object? Copy(this object? originalObject)
	//{
	//  return InternalCopy(originalObject, new Dictionary<object, object?>(new ReferenceEqualityComparer()));
	//}

	/// <summary>
	/// Internal method to perform the deep clone operation using reflection.
	/// </summary>
	/// <param name="originalObject">Object to clone.</param>
	/// <param name="visited">A dictionary to keep track of already visited objects to avoid circular references.</param>
	/// <returns>An exact copy of the original object that is distinct from the original object</returns>
	/// <exception cref="ArgumentException">Thrown when the type of <paramref name="originalObject"/> is a delegate type, which is unsupported.</exception>"
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

	/// <summary>
	/// Recursively copies private fields from the base type of the original object to the cloned object.
	/// </summary>
	/// <param name="originalObject">Object to clone.</param>
	/// <param name="visited">Dictionary to keep track of already visited objects to avoid circular references.</param>
	/// <param name="cloneObject">Cloned object.</param>
	/// <param name="typeToReflect">Type of the original object.</param>
	private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect)
	{
		if (typeToReflect.BaseType != null)
		{
			RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
			CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
		}
	}

	/// <summary>
	/// Copy values from fields in the original object into the cloned object.
	/// </summary>
	/// <param name="originalObject">Object to clone.</param>
	/// <param name="visited">Dictionary to keep track of already visited objects to avoid circular references.</param>
	/// <param name="cloneObject">Cloned object.</param>
	/// <param name="typeToReflect">Type of the original object.</param>
	/// <param name="bindingFlags">Optional: Binding flags to use when retrieving fields. Default is BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy</param>
	/// <param name="filter">Optional: filter to apply to fields being copied. Default is <see langword="null"/>.</param>
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
