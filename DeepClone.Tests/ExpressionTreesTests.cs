using System.Reflection;
using static CommonNetFuncs.DeepClone.ExpressionTrees;

namespace DeepClone.Tests;

public class ExpressionTreesTests
{
    public class TestClass
    {
        public int Number { get; set; }

        public string? Text { get; set; }

        public List<int>? Numbers { get; set; }

        public int[]? NumberArray { get; set; }

        public TestClass? Child { get; set; }

        public readonly string ReadOnlyField = "test";

        #pragma warning disable IDE0051 // Remove unused private members
        #pragma warning disable RCS1213 // Remove unused member declaration
        #pragma warning disable CS0414 // The field is assigned but its value is never used
        private readonly int _privateReadOnlyField = 42;
        #pragma warning disable IDE0051 // Remove unused private members
        #pragma warning restore RCS1213 // Remove unused member declaration
        #pragma warning restore CS0414 //The field is assigned but its value is never used
    }

    public struct TestStruct
    {
        public int Number { get; set; }

        public string? Text { get; set; }

        public TestClass? RefType { get; set; }
    }

    public delegate void TestDelegate();

    [Fact]
    public void DeepClone_WhenInputIsNull_ShouldReturnNull()
    {
        // Arrange
        TestClass? source = null;

        // Act
        TestClass? result = source.DeepClone();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void DeepClone_WhenInputIsPrimitiveType_ShouldReturnSameValue()
    {
        // Arrange
        const int source = 42;

        // Act
        int result = source.DeepClone();

        // Assert
        result.ShouldBe(source);
    }

    [Fact]
    public void DeepClone_WhenInputIsString_ShouldReturnSameInstance()
    {
        // Arrange
        const string source = "test";

        // Act
        string result = source.DeepClone();

        // Assert
        result.ShouldBe(source);
        ReferenceEquals(result, source).ShouldBeTrue();
    }

    [Fact]
    public void DeepClone_WhenInputIsDelegate_ShouldThrowArgumentException()
    {
        // Arrange
        TestDelegate source = () => { };

        // Act & Assert
        Should.Throw<ArgumentException>(() => source.DeepClone());
    }

    [Fact]
    public void DeepClone_WhenInputIsSimpleClass_ShouldCreateDeepCopy()
    {
        // Arrange
        TestClass source = new()
        {
            Number = 42,
            Text = "test"
        };

        // Act
        TestClass result = source.DeepClone();

        // Assert
        result.ShouldNotBeSameAs(source);
        result.Number.ShouldBe(source.Number);
        result.Text.ShouldBe(source.Text);
    }

    [Fact]
    public void DeepClone_WhenInputHasCircularReference_ShouldHandleCorrectly()
    {
        // Arrange
        TestClass source = new()
        {
            Number = 42,
            Text = "parent"
        };

        source.Child = new()
        {
            Number = 24,
            Text = "child",
            Child = source
        };

        // Act
        TestClass result = source.DeepClone();

        // Assert
        result.ShouldNotBeSameAs(source);
        result.Child.ShouldNotBeSameAs(source.Child);
        result.Child?.Child.ShouldBeSameAs(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DeepClone_WhenInputIsArray_ShouldCreateDeepCopy(int dimensions)
    {
        // Arrange
        Array source = dimensions switch
        {
            1 => new int[] { 1, 2, 3 },
            2 => new int[,] { { 1, 2 }, { 3, 4 } },
            3 => new int[,,] { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } },
            _ => throw new ArgumentException("Unsupported dimension")
        };

        // Act
        Array result = source.DeepClone();

        // Assert
        result.ShouldNotBeSameAs(source);
        result.Length.ShouldBe(source.Length);
        result.Rank.ShouldBe(dimensions);
    }

    [Fact]
    public void DeepClone_WhenInputIsStruct_WithClassMembers_ShouldCreateDeepCopy()
    {
        // Arrange
        TestStruct source = new()
        {
            Number = 42,
            Text = "test",
            RefType = new TestClass { Number = 24, Text = "inner" }
        };

        // Act
        TestStruct result = source.DeepClone();

        // Assert
        result.Number.ShouldBe(source.Number);
        result.Text.ShouldBe(source.Text);
        result.RefType.ShouldNotBeSameAs(source.RefType);
        result.RefType!.Number.ShouldBe(source.RefType.Number);
        result.RefType.Text.ShouldBe(source.RefType.Text);
    }

    [Fact]
    public void DeepClone_WhenInputHasReadOnlyFields_ShouldCreateDeepCopy()
    {
        // Arrange
        TestClass source = new();

        // Act
        TestClass result = source.DeepClone();

        // Assert
        result.ShouldNotBeSameAs(source);
        result.ReadOnlyField.ShouldBe(source.ReadOnlyField);

        // Use reflection to check private readonly field
        FieldInfo? fieldInfo = typeof(TestClass).GetField("_privateReadOnlyField", BindingFlags.NonPublic | BindingFlags.Instance);
        fieldInfo.ShouldNotBeNull();
        fieldInfo.GetValue(result).ShouldBe(fieldInfo.GetValue(source));
    }

    [Fact]
    public void DeepClone_WhenInputHasCollection_ShouldCreateDeepCopy()
    {
        // Arrange
        TestClass source = new()
        {
            Numbers = new List<int> { 1, 2, 3 }
        };

        // Act
        TestClass result = source.DeepClone();

        // Assert
        result.Numbers.ShouldNotBeSameAs(source.Numbers);
        result.Numbers!.Count.ShouldBe(source.Numbers.Count);
        result.Numbers.SequenceEqual(source.Numbers!).ShouldBeTrue();
    }

    [Fact]
    public void DeepClone_WhenInputHasArray_ShouldCreateDeepCopy()
    {
        // Arrange
        TestClass[] source =
        [
            new TestClass
            {
                Numbers = [0, 1, 2],
                NumberArray = [1, 2, 3]
            },
            new TestClass
            {
                Numbers = [3, 4, 5],
                NumberArray = [4, 5, 6]
            },
            new TestClass
            {
                Numbers = [6, 7, 8],
                NumberArray = [7, 8, 9]
            },
        ];

        // Act
        TestClass[] result = source.DeepClone();

        // Assert
        result.Length.ShouldBe(source.Length);

        for (int i = 0; i < result.Length; i++)
        {
            result[i].NumberArray.ShouldNotBeSameAs(source[i].NumberArray);
            result[i].Numbers.ShouldNotBeSameAs(source[i].Numbers);
            result[i].NumberArray!.Length.ShouldBe(source[i].NumberArray!.Length);
            result[i].Numbers!.Count.ShouldBe(source[i].Numbers!.Count);
            result[i].NumberArray!.SequenceEqual(source[i].NumberArray!).ShouldBeTrue();
            result[i].Numbers!.SequenceEqual(source[i].Numbers!).ShouldBeTrue();
        }
    }

    [Fact]
    public void DeepClone_WithCustomDictionary_ShouldUseProvidedDictionary()
    {
        // Arrange
        TestClass source = new() { Number = 42 };
        Dictionary<object, object> customDict = new(ReferenceEqualityComparer.Instance);

        // Act
        TestClass result = source.DeepClone(customDict);

        // Assert
        result.ShouldNotBeSameAs(source);
        customDict.Count.ShouldBe(1);
        customDict.ContainsKey(source).ShouldBeTrue();
        customDict[source].ShouldBeSameAs(result);
    }
}
