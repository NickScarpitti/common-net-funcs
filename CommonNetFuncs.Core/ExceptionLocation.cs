using System.Reflection;

namespace CommonNetFuncs.Core;

public static class ExceptionLocation
{
    /// <summary>
    /// Gets the name of the method where the exception occurred
    /// </summary>
    /// <param name="ex">Exception to get the method location of</param>
    /// <returns>Name of the method where the exception occurred</returns>
    public static string GetLocationOfException(this Exception ex)
    {
        MethodBase? method = ex.TargetSite;
        Type? type = method?.ReflectedType;
        if (method == null)
        {
            return "null.";
        }

        if (type == null)
        {
            return $"null.{method.Name}";
        }

        return $"{type.FullName}.{method.Name}";
    }
}
