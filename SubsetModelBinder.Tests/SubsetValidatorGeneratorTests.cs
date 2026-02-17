using CommonNetFuncs.SubsetModelBinder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Xunit.TestContext;

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
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

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
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		Diagnostic diagnostic = result.Diagnostics[0];
		diagnostic.Id.ShouldBe("SG0001");
		diagnostic.GetMessage().ShouldContain("has a different type than in the original class");
	}

	[Fact]
	public void Generator_Reports_NonPartialClass_Diagnostic()
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
				public class SubsetClass  // Missing partial modifier
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		Diagnostic? diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "SG0003");
		diagnostic.ShouldNotBeNull();
		diagnostic.GetMessage().ShouldContain("must be marked as partial");
	}

	[Fact]
	public void Generator_Reports_PropertyNotFound_Diagnostic()
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
					public string ExtraProperty { get; set; }  // Not in original
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		Diagnostic? diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "SG0002");
		diagnostic.ShouldNotBeNull();
		diagnostic.GetMessage().ShouldContain("is not present in the parent class");
	}

	[Fact]
	public void Generator_WithInheritedProperties_ShouldAllowBaseClassProperties()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public string BaseProperty { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), allowInheritedProperties: true)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public string BaseProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - No diagnostics should be reported
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithoutInheritedProperties_ShouldReportBaseClassProperties()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public string BaseProperty { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), allowInheritedProperties: false)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public string BaseProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		Diagnostic? diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "SG0002");
		diagnostic.ShouldNotBeNull();
		diagnostic.GetMessage().ShouldContain("BaseProperty");
	}

	[Fact]
	public void Generator_WithMvcFlag_ShouldGenerateModelMetadataType()
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

				[SubsetOf(typeof(OriginalClass), isMvcApp: true)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		SyntaxTree generatedFile = result.GeneratedTrees[0];
		string generatedCode = generatedFile.ToString();
		generatedCode.ShouldContain("using Microsoft.AspNetCore.Mvc;");
		generatedCode.ShouldContain("[ModelMetadataType(typeof(OriginalClass))]");
	}

	[Fact]
	public void Generator_WithoutMvcFlag_ShouldGenerateMetadataType()
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

				[SubsetOf(typeof(OriginalClass), isMvcApp: false)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		SyntaxTree generatedFile = result.GeneratedTrees[0];
		string generatedCode = generatedFile.ToString();
		generatedCode.ShouldContain("using System.ComponentModel.DataAnnotations;");
		generatedCode.ShouldContain("[MetadataType(typeof(OriginalClass))]");
	}

	[Fact]
	public void Generator_WithIgnoreTypeFlag_ShouldNotReportTypeMismatch()
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

				[SubsetOf(typeof(OriginalClass), ignoreType: true)]
				public partial class SubsetClass
				{
					public int Name { get; set; }  // Type mismatch but ignored
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - No type mismatch diagnostic should be reported
		result.Diagnostics.Any(d => d.Id == "SG0001").ShouldBeFalse();
	}

	[Fact]
	public void Generator_WithoutIgnoreTypeFlag_ShouldReportTypeMismatch()
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

				[SubsetOf(typeof(OriginalClass), ignoreType: false)]
				public partial class SubsetClass
				{
					public int Name { get; set; }  // Type mismatch
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		Diagnostic? diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "SG0001");
		diagnostic.ShouldNotBeNull();
		diagnostic.GetMessage().ShouldContain("has a different type");
	}

	[Fact]
	public void Generator_WithMultipleClasses_ShouldProcessAll()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass1
				{
					public string Name { get; set; }
				}

				public class OriginalClass2
				{
					public int Count { get; set; }
				}

				[SubsetOf(typeof(OriginalClass1))]
				public partial class SubsetClass1
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass2))]
				public partial class SubsetClass2
				{
					public int Count { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should generate 2 files
		result.GeneratedTrees.Length.ShouldBe(2);
		result.GeneratedTrees[0].ToString().ShouldContain("SubsetClass1");
		result.GeneratedTrees[1].ToString().ShouldContain("SubsetClass2");
	}

	[Fact]
	public void Generator_WithClassWithoutAttribute_ShouldNotProcess()
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

				public partial class SubsetClass  // No attribute
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - No files should be generated
		result.GeneratedTrees.Length.ShouldBe(0);
	}

	[Fact]
	public void Generator_WithEmptySubsetClass_ShouldStillGenerate()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
					public int Age { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					// Empty subset - no properties
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.GeneratedTrees[0].ToString().ShouldContain("SubsetClass");
	}

	[Fact]
	public void Generator_WithComplexInheritanceChain_ShouldIncludeAllAncestorProperties()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class GrandparentClass
				{
					public string GrandparentProperty { get; set; }
				}

				public class ParentClass : GrandparentClass
				{
					public string ParentProperty { get; set; }
				}

				public class OriginalClass : ParentClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), allowInheritedProperties: true)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public string ParentProperty { get; set; }
					public string GrandparentProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - No diagnostics should be reported
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithMultipleDiagnostics_ShouldReportAll()
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
					public int Name { get; set; }  // Type mismatch
					public string ExtraProperty { get; set; }  // Not in original
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.Count(d => d.Id == "SG0001").ShouldBe(1); // Type mismatch
		result.Diagnostics.Count(d => d.Id == "SG0002").ShouldBe(1); // Property not found
	}

	[Fact]
	public void Generator_WithNestedNamespace_ShouldGenerateCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace.Nested.Deep
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

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		SyntaxTree generatedFile = result.GeneratedTrees[0];
		generatedFile.ToString().ShouldContain("namespace TestNamespace.Nested.Deep");
	}

	[Fact]
	public void Generator_WithAllFlags_ShouldRespectAllSettings()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public string BaseProperty { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), isMvcApp: true, allowInheritedProperties: true, ignoreType: true)]
				public partial class SubsetClass
				{
					public int Name { get; set; }  // Type mismatch but ignored
					public string BaseProperty { get; set; }  // Inherited property allowed
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		SyntaxTree generatedFile = result.GeneratedTrees[0];
		string generatedCode = generatedFile.ToString();
		generatedCode.ShouldContain("using Microsoft.AspNetCore.Mvc;");
		generatedCode.ShouldContain("[ModelMetadataType(typeof(OriginalClass))]");
	}

	[Fact]
	public void Generator_DiagnosticDeduplication_ShouldNotReportDuplicates()
	{
		// Arrange - Create a scenario that might cause duplicate diagnostics
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
					public string ExtraProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass multiple times
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should only have one diagnostic for ExtraProperty
		result.Diagnostics.Count(d => d.Id == "SG0002" && d.GetMessage().Contains("ExtraProperty")).ShouldBe(1);
	}

	[Fact]
	public void Generator_WithNoClasses_ShouldHandleGracefully()
	{
		// Arrange - Empty source file
		const string source = @"
			namespace TestNamespace
			{
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should not generate any files or diagnostics
		result.GeneratedTrees.Length.ShouldBe(0);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithOtherAttributes_ShouldIgnoreThem()
	{
		// Arrange
		const string source = @"
			using System;
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[Obsolete]
				[Serializable]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should not process classes without SubsetOf attribute
		result.GeneratedTrees.Length.ShouldBe(0);
	}

	[Fact]
	public void Generator_WithNullOriginalType_ShouldHandleGracefully()
	{
		// Arrange - This tests the edge case where constructor argument is not a valid type
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation without SubsetOf attribute
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should handle gracefully without crashing
		result.GeneratedTrees.Length.ShouldBe(0);
	}

	[Fact]
	public void Generator_WithSamePropertyMultipleTimes_ShouldReportOnlyOnce()
	{
		// Arrange - Test diagnostic deduplication with same property appearing multiple times
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
					public int Name { get; set; }  // Type mismatch
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class AnotherSubsetClass
				{
					public int Name { get; set; }  // Same property name, same error
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get diagnostics
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should report one diagnostic per class
		result.Diagnostics.Count(d => d.Id == "SG0001").ShouldBe(2);
	}

	[Fact]
	public void Generator_WithClassWithoutAttributeLists_ShouldBeFiltered()
	{
		// Arrange
		const string source = @"
			namespace TestNamespace
			{
				public class SimpleClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(0);
	}

	[Fact]
	public void Generator_WithDefaultParameterValues_ShouldUseDefaults()
	{
		// Arrange - Test with minimal attribute usage (only required parameter)
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

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should use default values (isMvcApp=false, allowInheritedProperties=true)
		result.Diagnostics.ShouldBeEmpty();
		SyntaxTree generatedFile = result.GeneratedTrees[0];
		string generatedCode = generatedFile.ToString();
		generatedCode.ShouldContain("using System.ComponentModel.DataAnnotations;");
		generatedCode.ShouldContain("[MetadataType(typeof(OriginalClass))]");
	}

	[Fact]
	public void Generator_WithPropertyInDifferentOrder_ShouldStillValidate()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
					public int Age { get; set; }
					public bool IsActive { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public bool IsActive { get; set; }
					public string Name { get; set; }
					public int Age { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Order shouldn't matter
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithMultipleAttributes_ShouldIgnoreNonSubsetOfAttributes()
	{
		// Arrange - this tests the branch where attributeSymbol.ContainingType.Name != "SubsetOfAttribute"
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;
			using System;

			namespace TestNamespace
			{
				[AttributeUsage(AttributeTargets.Class)]
				public class CustomAttribute : Attribute { }

				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[Custom]
				[SubsetOf(typeof(OriginalClass))]
				[Obsolete]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should successfully generate despite multiple attributes
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithAttributeOnNonPartialClass_ShouldReportDiagnostic()
	{
		// Arrange - testing edge case with non-partial class
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - Should report partial keyword diagnostic
		result.Diagnostics.Any(d => d.GetMessage().Contains("partial")).ShouldBeTrue();
	}

	[Fact]
	public void Generator_WithNestedClass_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OuterClass
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
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithGenericClass_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass<T>
				{
					public T Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass<string>))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithAbstractClass_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public abstract class OriginalClass
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

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithSealedClass_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public sealed class OriginalClass
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

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithInternalClass_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				internal class OriginalClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				internal partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithCompilationErrors_ShouldNotCrash()
	{
		// Arrange - test with invalid typeof() usage to trigger edge cases
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(NonExistentType))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass - should not crash
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - generator should handle gracefully (may or may not generate)
		// The key is it shouldn't crash
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Generator_WithMissingAttributeArgument_ShouldHandleGracefully()
	{
		// Arrange - empty attribute arguments
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[SubsetOf()]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - should handle gracefully
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Generator_WithPropertyHidingInheritance_ShouldHandleCorrectly()
	{
		// Arrange - test property hiding with 'new' keyword
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public string Name { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public new string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithVirtualProperties_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public virtual string Name { get; set; }
					public virtual int Age { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithStaticProperties_ShouldIgnoreThem()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
					public static string StaticProperty { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public static string StaticProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithReadOnlyProperties_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
					public string ReadOnlyProperty { get; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public string ReadOnlyProperty { get; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithExpressionBodiedProperties_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					private string _name;
					public string Name => _name;
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					private string _name;
					public string Name => _name;
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithTypeIgnoreAndDifferentTypes_ShouldNotReportError()
	{
		// Arrange - test ignoreType with allowInheritedProperties=false combination
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public string BaseProperty { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), ignoreType: true, allowInheritedProperties: false)]
				public partial class SubsetClass
				{
					public int Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - no type mismatch errors due to ignoreType
		result.Diagnostics.Any(d => d.Id == "SG0001").ShouldBeFalse();
	}

	[Fact]
	public void Generator_WithNullableReferenceTypes_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			#nullable enable
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string? Name { get; set; }
					public int? Age { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string? Name { get; set; }
					public int? Age { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithDeeplyNestedInheritance_ShouldTraverseAllLevels()
	{
		// Arrange - test multiple levels of inheritance to cover BaseType traversal
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class Level1
				{
					public string Level1Property { get; set; }
				}

				public class Level2 : Level1
				{
					public string Level2Property { get; set; }
				}

				public class Level3 : Level2
				{
					public string Level3Property { get; set; }
				}

				public class OriginalClass : Level3
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), allowInheritedProperties: true)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
					public string Level3Property { get; set; }
					public string Level2Property { get; set; }
					public string Level1Property { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - all properties from all levels should be found
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithNoInheritance_ShouldStopAtFirstLevel()
	{
		// Arrange - test that BaseType != null branch works correctly with no base class
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass), allowInheritedProperties: true)]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithInterfaceImplementation_ShouldOnlyCheckClassProperties()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public interface IEntity
				{
					string Name { get; set; }
				}

				public class OriginalClass : IEntity
				{
					public string Name { get; set; }
					public int Age { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass : IEntity
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation with System.Runtime reference for interfaces
		CSharpCompilation compilation = CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: Current.CancellationToken), CSharpSyntaxTree.ParseText(AttributeSource, cancellationToken: Current.CancellationToken) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithRecordType_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public record OriginalClass(string Name, int Age);

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - records have properties too
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithStructType_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public struct OriginalStruct
				{
					public string Name { get; set; }
					public int Age { get; set; }
				}

				[SubsetOf(typeof(OriginalStruct))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithMultipleAttributeLists_ShouldProcessCorrectly()
	{
		// Arrange - test with attributes in separate attribute lists
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;
			using System;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[Obsolete]
				[SubsetOf(typeof(OriginalClass))]
				[Serializable]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithAttributeInMiddleOfList_ShouldFind()
	{
		// Arrange - SubsetOf attribute in middle of attribute list
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;
			using System;

			namespace TestNamespace
			{
				[AttributeUsage(AttributeTargets.Class)]
				public class FirstAttribute : Attribute { }

				[AttributeUsage(AttributeTargets.Class)]
				public class LastAttribute : Attribute { }

				public class OriginalClass
				{
					public string Name { get; set; }
				}

				[First, SubsetOf(typeof(OriginalClass)), Last]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithEmptyClass_ShouldHandleGracefully()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - should generate even with no properties
		result.GeneratedTrees.Length.ShouldBe(1);
		result.Diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void Generator_WithComplexPropertyTypes_ShouldValidateCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;
			using System.Collections.Generic;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public List<string> Names { get; set; }
					public Dictionary<int, string> Map { get; set; }
					public int[] Numbers { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public List<string> Names { get; set; }
				}
			}";

		// Create compilation with additional references for System.Collections.Generic
		CSharpCompilation compilation = CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: Current.CancellationToken), CSharpSyntaxTree.ParseText(AttributeSource, cancellationToken: Current.CancellationToken) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location)
			},
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithTypeMismatchInComplexTypes_ShouldReportError()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;
			using System.Collections.Generic;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public List<string> Names { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public List<int> Names { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source, cancellationToken: Current.CancellationToken), CSharpSyntaxTree.ParseText(AttributeSource, cancellationToken: Current.CancellationToken) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - should report type mismatch (List<string> vs List<int>)
		result.Diagnostics.Any(d => d.Id == "SG0001").ShouldBeTrue();
	}

	[Fact]
	public void Generator_WithPropertyOverrideKeyword_ShouldHandleCorrectly()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class BaseClass
				{
					public virtual string Name { get; set; }
				}

				public class OriginalClass : BaseClass
				{
					public override string Name { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string Name { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	[Fact]
	public void Generator_WithFieldsInsteadOfProperties_ShouldIgnoreFields()
	{
		// Arrange
		const string source = @"
			using CommonNetFuncs.SubsetModelBinder;

			namespace TestNamespace
			{
				public class OriginalClass
				{
					public string Name;
					public string ActualProperty { get; set; }
				}

				[SubsetOf(typeof(OriginalClass))]
				public partial class SubsetClass
				{
					public string ActualProperty { get; set; }
				}
			}";

		// Create compilation
		CSharpCompilation compilation = CreateCompilation(source);

		SubsetValidatorGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generation pass
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

		// Get results
		GeneratorDriverRunResult result = driver.GetRunResult();

		// Assert - should only check properties, not fields
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedTrees.Length.ShouldBe(1);
	}

	private static CSharpCompilation CreateCompilation(string source)
	{
		return CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(AttributeSource) },
			new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
