namespace Common_Net_Funcs.Tools;
public static class DebugHelpers
{
    public static string GetLocationOfEexception(this Exception ex)
    {
        return $"{ex.TargetSite?.ReflectedType?.ReflectedType?.FullName}.{ex.TargetSite?.ReflectedType?.Name.ExtractBetween("<", ">")}";
    }
}
