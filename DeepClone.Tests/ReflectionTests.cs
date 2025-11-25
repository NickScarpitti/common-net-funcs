using static CommonNetFuncs.DeepClone.Reflection;

namespace DeepClone.Tests;

public sealed class ReflectionTests
{
	[Theory]
	[InlineData(typeof(int))]
	[InlineData(typeof(long))]
	[InlineData(typeof(float))]
	[InlineData(typeof(double))]
	[InlineData(typeof(bool))]
	[InlineData(typeof(char))]
	[InlineData(typeof(string))]
	public void IsPrimitive_ShouldReturnTrue_ForPrimitiveTypes(Type type)
	{
		// Act
		bool result = type.IsPrimitive();

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[InlineData(typeof(DateTime))]
	[InlineData(typeof(object))]
	[InlineData(typeof(List<int>))]
	public void IsPrimitive_ShouldReturnFalse_ForNonPrimitiveTypes(Type type)
	{
		// Act
		bool result = type.IsPrimitive();

		// Assert
		result.ShouldBeFalse();
	}

	//[Fact]
	//public void DeepClone_ShouldReturnNull_WhenInputIsNull()
	//{
	//    // Arrange
	//    TestClass? nullObject = null;

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    TestClass? result = nullObject.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    result.ShouldBeNull();
	//}

	//[Fact]
	//public void DeepClone_ShouldCreateNewInstance_WithSameValues()
	//{
	//    // Arrange
	//    TestClass original = new()
	//    {
	//        Id = 1,
	//        Name = "Test",
	//        Numbers = new List<int> { 1, 2, 3 }
	//    };

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    TestClass? clone = original.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    clone.ShouldNotBeNull();
	//    clone.Id.ShouldBe(original.Id);
	//    clone.Name.ShouldBe(original.Name);
	//    clone.RegularString.ShouldBe(original.RegularString);
	//    clone.Numbers.ShouldBe(original.Numbers);
	//    clone.ShouldNotBeSameAs(original);
	//    clone.Numbers.ShouldNotBeSameAs(original.Numbers);
	//}

	//[Fact]
	//public void DeepClone_ShouldCloneArrays()
	//{
	//    // Arrange
	//    int[] original = new int[] { 1, 2, 3, 4, 5 };

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    int[]? clone = original.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    clone.ShouldNotBeNull();
	//    clone.ShouldBe(original);
	//    clone.ShouldNotBeSameAs(original);
	//}

	//[Fact]
	//public void DeepClone_ShouldCloneComplexArrays()
	//{
	//    // Arrange
	//    TestClass[] original = new TestClass[]
	//    {
	//        new() { Id = 1, Name = "One", RegularString = "Regular String 1" },
	//        new() { Id = 2, Name = "Two", RegularString = "Regular String 2" },
	//        new() { Id = 3, Name = null, RegularString = string.Empty }
	//    };

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    TestClass[]? clone = original.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    clone.ShouldNotBeNull();
	//    clone.Length.ShouldBe(original.Length);
	//    clone.ShouldNotBeSameAs(original);

	//    for (int i = 0; i < original.Length; i++)
	//    {
	//        clone[i].Id.ShouldBe(original[i].Id);
	//        clone[i].Name.ShouldBe(original[i].Name);
	//        clone[i].RegularString.ShouldBe(original[i].RegularString);
	//        clone[i].ShouldNotBeSameAs(original[i]);
	//    }
	//}

	//[Fact]
	//public void DeepClone_ShouldHandleCircularReferences()
	//{
	//    // Arrange
	//    CircularReferenceClass original = new();
	//    original.Reference = original;

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    CircularReferenceClass? clone = original.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    clone.ShouldNotBeNull();
	//    clone.ShouldNotBeSameAs(original);
	//    clone.Reference.ShouldBeSameAs(clone);
	//}

	//[Fact]
	//public void DeepClone_ShouldThrowArgumentException_ForDelegateTypes()
	//{
	//    // Arrange
	//    Action action = () => { };

	//    // Act & Assert
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    Should.Throw<ArgumentException>(() => action.DeepCloneR());
	//    #pragma warning restore CS0618 // Type or member is obsolete
	//}

	//[Fact]
	//public void DeepClone_ShouldHandleInheritance()
	//{
	//    // Arrange
	//    DerivedTestClass original = new()
	//    {
	//        Id = 1,
	//        Name = "Test",
	//        Numbers = new List<int> { 1, 2, 3 },
	//        ExtraProperty = "Extra"
	//    };

	//    // Act
	//    #pragma warning disable CS0618 // Type or member is obsolete
	//    DerivedTestClass? clone = original.DeepCloneR();
	//    #pragma warning restore CS0618 // Type or member is obsolete

	//    // Assert
	//    clone.ShouldNotBeNull();
	//    clone.Id.ShouldBe(original.Id);
	//    clone.Name.ShouldBe(original.Name);
	//    clone.RegularString.ShouldBe(original.RegularString);
	//    clone.Numbers.ShouldBe(original.Numbers);
	//    clone.ExtraProperty.ShouldBe(original.ExtraProperty);
	//    clone.ShouldNotBeSameAs(original);
	//    clone.Numbers.ShouldNotBeSameAs(original.Numbers);
	//}

	// Test helper classes
	//private class TestClass
	//{
	//    public int Id { get; set; }

	//    public string? Name { get; set; }

	//    public string RegularString { get; set; } = "Test String";

	//    public List<int>? Numbers { get; set; }
	//}

	//private sealed class DerivedTestClass : TestClass
	//{
	//    public string? ExtraProperty { get; set; }
	//}

	//private sealed class CircularReferenceClass
	//{
	//    public CircularReferenceClass? Reference { get; set; }
	//}
}
