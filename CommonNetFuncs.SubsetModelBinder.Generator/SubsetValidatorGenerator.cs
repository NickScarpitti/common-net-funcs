using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommonNetFuncs.SubsetModelBinder;

[Generator(LanguageNames.CSharp)]
public sealed class SubsetValidatorGenerator : IIncrementalGenerator
{
    private const string FullyQualifiedAttributeName = "CommonNetFuncs.SubsetModelBinder.SubsetOfAttribute"; //typeof(SubsetOfAttribute).Namespace + "." + nameof(SubsetOfAttribute);//"CommonNetFuncs.SubsetModelBinder.SubsetOfAttribute"; //typeof(SubsetOfAttribute).Namespace + nameof(SubsetOfAttribute);
    private const string AttributeName = "SubsetOfAttribute"; //typeof(SubsetOfAttribute).Namespace + "." + nameof(SubsetOfAttribute);//"CommonNetFuncs.SubsetModelBinder.SubsetOfAttribute"; //typeof(SubsetOfAttribute).Namespace + nameof(SubsetOfAttribute);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
        .ForAttributeWithMetadataName( //Used to be CreateSyntaxProvider
                FullyQualifiedAttributeName,
                predicate: (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static(ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, static(spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context) //Used to be GeneratorSyntaxContext
    {
        ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)context.TargetNode;//.Node;
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol)
                {
                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (string.Equals(fullName, FullyQualifiedAttributeName))
                    {
                        return classDeclarationSyntax;
                    }
                }
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        // Use a ConcurrentDictionary to track reported diagnostics
        ConcurrentDictionary<string, bool> reportedDiagnostics = new();

        foreach (ClassDeclarationSyntax subsetClass in classes)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(subsetClass.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(subsetClass) is not INamedTypeSymbol subsetClassSymbol)
            {
                continue;
            }

            // Check if the class is marked as partial
            if (!subsetClass.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                ReportDiagnosticOnce(context, "SG0003", "Class must be partial", $"The class '{subsetClassSymbol.Name}' decorated with SubsetOf attribute must be marked as partial", subsetClass.Identifier.GetLocation(), reportedDiagnostics);
            }

            AttributeData? subsetOfAttribute = subsetClassSymbol.GetAttributes().FirstOrDefault(a => string.Equals(a.AttributeClass?.Name, AttributeName));

            if (subsetOfAttribute == null)
            {
                continue;
            }

            if (subsetOfAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol originalTypeSymbol)
            {
                continue;
            }

            bool isMvcApp = subsetOfAttribute.ConstructorArguments.Length > 1 && (bool)subsetOfAttribute.ConstructorArguments[1].Value!;
            bool allowInheritedProperties = subsetOfAttribute.ConstructorArguments.Length > 2 && (bool)subsetOfAttribute.ConstructorArguments[2].Value!;
            bool ignoreType = subsetOfAttribute.ConstructorArguments.Length > 3 && (bool)subsetOfAttribute.ConstructorArguments[3].Value!;

            // Get all properties of the original type, including inherited ones
            Dictionary<string, IPropertySymbol> originalProperties = GetAllProperties(originalTypeSymbol, allowInheritedProperties);

            foreach (IPropertySymbol subsetProperty in subsetClassSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (!originalProperties.TryGetValue(subsetProperty.Name, out IPropertySymbol? originalProperty))
                {
                    ReportDiagnosticOnce(context, "SG0002", "Property not found", $"Property '{subsetProperty.Name}' is not present in the parent class '{originalTypeSymbol.Name}'{(allowInheritedProperties ? " or its base classes" : string.Empty)}",
                        subsetProperty.Locations.FirstOrDefault(), reportedDiagnostics);
                }
                else if (!ignoreType && !SymbolEqualityComparer.Default.Equals(subsetProperty.Type, originalProperty.Type))
                {
                    ReportDiagnosticOnce(context, "SG0001", "Property type mismatch", $"Property '{subsetProperty.Name}' has a different type than in the original class '{originalTypeSymbol.Name}'{(allowInheritedProperties ? " or its base classes" : string.Empty)}. Expected: {originalProperty.Type.Name}, Found: {subsetProperty.Type.Name}",
                        subsetProperty.Locations.FirstOrDefault(), reportedDiagnostics);
                }
            }

            string attributeCode = GenerateAttributeCode(subsetClassSymbol, originalTypeSymbol, isMvcApp);
            context.AddSource($"{subsetClassSymbol.Name}_Attributes.g.cs", SourceText.From(attributeCode, Encoding.UTF8));
        }
    }

    private static string GenerateAttributeCode(INamedTypeSymbol subsetClassSymbol, INamedTypeSymbol originalTypeSymbol, bool isMvcApp)
    {
        StringBuilder sb = new();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine(isMvcApp ? "using Microsoft.AspNetCore.Mvc;" : "using System.ComponentModel.DataAnnotations;");
        sb.AppendLine($"namespace {subsetClassSymbol.ContainingNamespace.ToDisplayString()}");
        sb.AppendLine("{");
        sb.AppendLine($"    [{(isMvcApp ? "ModelMetadataType" : "MetadataType")}(typeof({originalTypeSymbol.Name}))]");
        sb.AppendLine($"    public partial class {subsetClassSymbol.Name}");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void ReportDiagnosticOnce(SourceProductionContext context, string id, string title, string message, Location? location, ConcurrentDictionary<string, bool> reportedDiagnostics)
    {
        string key = $"{id}:{location}:{message}";
        if (reportedDiagnostics.TryAdd(key, true))
        {
            DiagnosticDescriptor descriptor = new(id, title, message, nameof(SubsetValidatorGenerator), DiagnosticSeverity.Error, isEnabledByDefault: true);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location ?? Location.None));
        }
    }

    private static Dictionary<string, IPropertySymbol> GetAllProperties(INamedTypeSymbol type, bool includeInheritedProperties)
    {
        Dictionary<string, IPropertySymbol> properties = [];
        INamedTypeSymbol? currentType = type;

        while (currentType != null)
        {
            foreach (IPropertySymbol member in currentType.GetMembers().OfType<IPropertySymbol>())
            {
                if (!properties.ContainsKey(member.Name))
                {
                    properties[member.Name] = member;
                }
            }

            if (!includeInheritedProperties)
            {
                break; // Stop after processing the current type if inherited properties are not allowed
            }

            currentType = currentType.BaseType;
        }

        return properties;
    }
}

//Less efficient ISourceGenerator
//[Generator]
//public sealed class SubsetValidatorGenerator : ISourceGenerator
//{
//    public void Initialize(GeneratorInitializationContext context)
//    {
//        // No initialization required for this generator
//        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
//    }

//    public void Execute(GeneratorExecutionContext context)
//    {
//        IEnumerable<ClassDeclarationSyntax> subsetClasses = context.Compilation.SyntaxTrees
//            .SelectMany(x => x.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
//            .Where(x => x.AttributeLists.SelectMany(y => y.Attributes).Any(y => y.Name.ToString() == "SubsetOf" || y.Name.ToString() == nameof(SubsetOfAttribute)));

//        foreach (ClassDeclarationSyntax subsetClass in subsetClasses)
//        {
//            SemanticModel semanticModel = context.Compilation.GetSemanticModel(subsetClass.SyntaxTree);

//            if (semanticModel.GetDeclaredSymbol(subsetClass) is not INamedTypeSymbol subsetClassSymbol) continue;

//            // Check if the class is marked as partial
//            if (!subsetClass.Modifiers.Any(SyntaxKind.PartialKeyword))
//            {
//                ReportDiagnostic(context, "SG0003", "Class must be partial", $"The class '{subsetClassSymbol.Name}' decorated with SubsetOf attribute must be marked as partial", subsetClass.Identifier.GetLocation());
//            }

//            AttributeData? subsetOfAttribute = subsetClassSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == nameof(SubsetOfAttribute));

//            if (subsetOfAttribute == null) continue;

//            if (subsetOfAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol originalTypeSymbol) continue;

//            List<Diagnostic> diagnostics = [];

//            foreach (IPropertySymbol subsetProperty in subsetClassSymbol.GetMembers().OfType<IPropertySymbol>())
//            {
//                IPropertySymbol originalProperty = originalTypeSymbol.GetMembers(subsetProperty.Name).OfType<IPropertySymbol>().FirstOrDefault();

//                if (originalProperty == null)
//                {
//                    ReportDiagnostic(context, "SG0002", "Property not found", $"Property '{subsetProperty.Name}' is not present in the parent class '{originalTypeSymbol.Name}'", subsetProperty.Locations.FirstOrDefault());
//                }
//                else if (!SymbolEqualityComparer.Default.Equals(subsetProperty.Type, originalProperty.Type))
//                {
//                    ReportDiagnostic(context, "SG0001", "Property type mismatch",
//                        $"Property '{subsetProperty.Name}' has a different type than in the original class '{originalTypeSymbol.Name}'. Expected: {originalProperty.Type.Name}, Found: {subsetProperty.Type.Name}",
//                        subsetProperty.Locations.FirstOrDefault());
//                }
//            }

//            foreach (Diagnostic diagnostic in diagnostics)
//            {
//                context.ReportDiagnostic(diagnostic);
//            }

//            bool isMvcApp = subsetOfAttribute.ConstructorArguments.Length > 1 && (bool)subsetOfAttribute.ConstructorArguments[1].Value!;
//            string attributeCode = GenerateAttributeCode(subsetClassSymbol, originalTypeSymbol, isMvcApp);
//            context.AddSource($"{subsetClassSymbol.Name}_Attributes.g.cs", SourceText.From(attributeCode, Encoding.UTF8));
//        }
//    }

//    private static string GenerateAttributeCode(INamedTypeSymbol subsetClassSymbol, INamedTypeSymbol originalTypeSymbol, bool isMvcApp)
//    {
//        StringBuilder sb = new();
//        sb.AppendLine("#nullable enable");
//        sb.AppendLine("using System;");
//        sb.AppendLine(isMvcApp ? "using Microsoft.AspNetCore.Mvc;" : "using System.ComponentModel.DataAnnotations;");
//        sb.AppendLine($"namespace {subsetClassSymbol.ContainingNamespace.ToDisplayString()}");
//        sb.AppendLine("{");
//        sb.AppendLine($"    [{(isMvcApp ? "ModelMetadataType" : "MetadataType")}(typeof({originalTypeSymbol.Name}))]");
//        sb.AppendLine($"    public partial class {subsetClassSymbol.Name}");
//        sb.AppendLine("    {");

//        //This appends properties that have attributes
//        //foreach (IPropertySymbol subsetProperty in subsetClassSymbol.GetMembers().OfType<IPropertySymbol>())
//        //{
//        //    IPropertySymbol originalProperty = originalTypeSymbol.GetMembers(subsetProperty.Name).OfType<IPropertySymbol>().FirstOrDefault();
//        //    if (originalProperty != null)
//        //    {
//        //        System.Collections.Immutable.ImmutableArray<AttributeData> attributes = originalProperty.GetAttributes();
//        //        if (attributes.Any())
//        //        {
//        //            sb.AppendLine($"        // Attributes for {subsetProperty.Name}");
//        //            foreach (AttributeData attribute in attributes)
//        //            {
//        //                sb.AppendLine($"        {attribute.AttributeClass?.ToDisplayString()}({string.Join(", ", attribute.ConstructorArguments.Select(ca => ca.ToCSharpString()))})");
//        //            }
//        //            sb.AppendLine($"        public {subsetProperty.Type.ToDisplayString()} {subsetProperty.Name} {{ get; set; }}");
//        //            sb.AppendLine();
//        //        }
//        //    }
//        //}

//        sb.AppendLine("    }");
//        sb.AppendLine("}");

//        return sb.ToString();
//    }

//    private static void ReportDiagnostic(GeneratorExecutionContext context, string id, string title, string message, Location? location)
//    {
//        DiagnosticDescriptor descriptor = new(id, title, message, nameof(SubsetValidatorGenerator), DiagnosticSeverity.Error, isEnabledByDefault: true);
//        context.ReportDiagnostic(Diagnostic.Create(descriptor, location ?? Location.None));
//    }
//}

//public sealed class SyntaxReceiver : ISyntaxReceiver
//{
//    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

//    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
//    {
//        // Any class with at least one attribute is a candidate for source generation
//        if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0)
//        {
//            CandidateClasses.Add(classDeclarationSyntax);
//        }
//    }
//}
