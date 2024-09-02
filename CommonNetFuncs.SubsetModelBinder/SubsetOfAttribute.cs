namespace CommonNetFuncs.SubsetModelBinder;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SubsetOfAttribute(Type sourceType, bool isMvcApp = false, bool allowInheritedProperties = true) : Attribute
{
    public Type SourceType { get; } = sourceType;
    public bool IsMvcApp { get; } = isMvcApp;
    public bool AllowInheritedProperties { get; } = allowInheritedProperties;
}
