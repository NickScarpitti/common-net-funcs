using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CommonNetFuncs.Core;

public static class Validation
{
    [return: NotNullIfNotNull(nameof(obj))]
    public static T? SetInvalidPropertiesToDefault<T>(this T? obj, bool validateAll = true) where T : class
    {
        if (obj == null) { return obj; }
        ValidationContext context = new(obj);
        List<ValidationResult> validationResults = [];
        if(!Validator.TryValidateObject(obj, context, validationResults, validateAll))
        {
            IEnumerable<string> propertiesToSetToDefault = validationResults.SelectMany(x => x.MemberNames).Distinct();
            foreach (PropertyInfo prop in typeof(T).GetProperties().Where(x => propertiesToSetToDefault.Contains(x.Name)))
            {
                prop.SetValue(obj, default);
            }
        }
        return obj;
    }
}
