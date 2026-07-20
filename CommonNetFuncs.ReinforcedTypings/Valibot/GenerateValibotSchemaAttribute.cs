namespace CommonNetFuncs.ReinforcedTypings.Valibot;

/// <summary>
/// Opt-in marker that triggers Valibot validation schema generation for this class.
/// Apply alongside <c>[TsInterface]</c> on classes that represent form-submission models
/// (i.e. classes that have validation attributes and are used as inputs on the frontend).
/// Read-only response/query models that don't need client-side validation should omit this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class GenerateValibotSchemaAttribute : Attribute { }
