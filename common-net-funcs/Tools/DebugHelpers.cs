namespace Common_Net_Funcs.Tools;

public static class DebugHelpers
{
    /// <summary>
    /// Gets the name of the method where the exception occurred
    /// </summary>
    /// <param name="ex">Exception to get the method location of</param>
    /// <returns>Name of the method where the exception occurred</returns>
    public static string GetLocationOfEexception(this Exception ex)
    {
        return $"{ex.TargetSite?.ReflectedType?.ReflectedType?.FullName}.{ex.TargetSite?.ReflectedType?.Name.ExtractBetween("<", ">")}";
    }
}
