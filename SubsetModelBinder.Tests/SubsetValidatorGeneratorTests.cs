<<<<<<< HEAD
﻿using CommonNetFuncs.SubsetModelBinder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SubsetModelBinder.Tests;

public class SubsetValidatorGeneratorTests
{
    const string AttributeSource = @"
        namespace CommonNetFuncs.SubsetModelBinder
        {
            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class SubsetOfAttribute : System.Attribute
            {
                public SubsetOfAttribute(System.Type sourceType, bool isMvcApp = false, bool allowInheritedProperties = true, bool ignoreType = false) { }
            }
        }";

    [Fact]
    public static void Generator_Creates_MetadataAttribute()
    {
        // Arrange
        const string source = @"
            using CommonNetFuncs.SubsetModelBinder;

            namespace TestNamespace
            {
                public class OriginalClass
                {
                    public string Name { get; set; }
                }

                [SubsetOf(typeof(OriginalClass))]
                public partial class SubsetClass
                {
                    public string Name { get; set; }
                }
            }";

        // Create compilation
        CSharpCompilation compilation = CreateCompilation(source);

        // Create an instance of our generator
        SubsetValidatorGenerator generator = new();

        // Create the driver that will inject our generator into the compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        driver = driver.RunGenerators(compilation);

        // Use driver to find the generated files
        GeneratorDriverRunResult result = driver.GetRunResult();

        // Assert
        SyntaxTree generatedFile = result.GeneratedTrees[0];
        Assert.Contains("[MetadataType(typeof(OriginalClass))]", generatedFile.ToString());
        Assert.Contains("public partial class SubsetClass", generatedFile.ToString());
    }

    [Fact]
    public void Generator_Reports_TypeMismatch_Diagnostic()
    {
        // Arrange
        const string source = @"
            using CommonNetFuncs.SubsetModelBinder;

            namespace TestNamespace
            {
                public class OriginalClass
                {
                    public string Name { get; set; }
                }

                [SubsetOf(typeof(OriginalClass))]
                public partial class SubsetClass
                {
                    public int Name { get; set; }  // Type mismatch here
                }
            }";

        // Create compilation
        CSharpCompilation compilation = CreateCompilation(source);

        SubsetValidatorGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        driver = driver.RunGenerators(compilation);

        // Get diagnostics
        GeneratorDriverRunResult result = driver.GetRunResult();

        // Assert
        Diagnostic diagnostic = result.Diagnostics[0];
        Assert.Equal("SG0001", diagnostic.Id);
        Assert.Contains("has a different type than in the original class", diagnostic.GetMessage());
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[] {
                CSharpSyntaxTree.ParseText(source),
                CSharpSyntaxTree.ParseText(AttributeSource)
            },
            new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
=======
﻿using CommonNetFuncs.SubsetModelBinder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SubsetModelBinder.Tests;

public class SubsetValidatorGeneratorTests
{
    const string AttributeSource = @"
        namespace CommonNetFuncs.SubsetModelBinder
        {
            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class SubsetOfAttribute : System.Attribute
            {
                public SubsetOfAttribute(System.Type sourceType, bool isMvcApp = false, bool allowInheritedProperties = true, bool ignoreType = false) { }
            }
        }";

    [Fact]
    public static void Generator_Creates_MetadataAttribute()
    {
        // Arrange
        const string source = @"
            using CommonNetFuncs.SubsetModelBinder;

            namespace TestNamespace
            {
                public class OriginalClass
                {
                    public string Name { get; set; }
                }

                [SubsetOf(typeof(OriginalClass))]
                public partial class SubsetClass
                {
                    public string Name { get; set; }
                }
            }";

        // Create compilation
        CSharpCompilation compilation = CreateCompilation(source);

        // Create an instance of our generator
        SubsetValidatorGenerator generator = new();

        // Create the driver that will inject our generator into the compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        driver = driver.RunGenerators(compilation);

        // Use driver to find the generated files
        GeneratorDriverRunResult result = driver.GetRunResult();

        // Assert
        SyntaxTree generatedFile = result.GeneratedTrees[0];
        generatedFile.ToString().ShouldContain("[MetadataType(typeof(OriginalClass))]");
        generatedFile.ToString().ShouldContain("public partial class SubsetClass");
    }

    [Fact]
    public void Generator_Reports_TypeMismatch_Diagnostic()
    {
        // Arrange
        const string source = @"
            using CommonNetFuncs.SubsetModelBinder;

            namespace TestNamespace
            {
                public class OriginalClass
                {
                    public string Name { get; set; }
                }

                [SubsetOf(typeof(OriginalClass))]
                public partial class SubsetClass
                {
                    public int Name { get; set; }  // Type mismatch here
                }
            }";

        // Create compilation
        CSharpCompilation compilation = CreateCompilation(source);

        SubsetValidatorGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        driver = driver.RunGenerators(compilation);

        // Get diagnostics
        GeneratorDriverRunResult result = driver.GetRunResult();

        // Assert
        Diagnostic diagnostic = result.Diagnostics[0];
        diagnostic.Id.ShouldBe("SG0001");
        diagnostic.GetMessage().ShouldContain("has a different type than in the original class");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[] {
                CSharpSyntaxTree.ParseText(source),
                CSharpSyntaxTree.ParseText(AttributeSource)
            },
            new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
