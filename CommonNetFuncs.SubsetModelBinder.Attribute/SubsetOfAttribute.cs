﻿namespace CommonNetFuncs.SubsetModelBinder;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SubsetOfAttribute(Type sourceType, bool isMvcApp = false, bool allowInheritedProperties = true, bool ignoreType = false) : Attribute
{
  public Type SourceType { get; } = sourceType;

  public bool IsMvcApp { get; } = isMvcApp;

  public bool AllowInheritedProperties { get; } = allowInheritedProperties;

  public bool IgnoreType { get; } = ignoreType;
}
