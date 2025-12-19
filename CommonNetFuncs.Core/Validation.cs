using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static class Validation
{
	[return: NotNullIfNotNull(nameof(obj))]
	public static T? SetInvalidPropertiesToDefault<T>(this T? obj, bool validateAll = true) where T : class
	{
		if (obj == null)
		{
			return obj;
		}
		ValidationContext context = new(obj);
		List<ValidationResult> validationResults = [];
		if (!Validator.TryValidateObject(obj, context, validationResults, validateAll))
		{
			HashSet<string> propertiesToSetToDefault = new(validationResults.SelectMany(x => x.MemberNames));
			foreach (PropertyInfo prop in GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => propertiesToSetToDefault.Contains(x.Name)))
			{
				prop.SetValue(obj, default);
			}
		}
		return obj;
	}
}
