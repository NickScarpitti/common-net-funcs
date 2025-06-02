using System.Collections;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class TypeChecksTests
{
    private readonly Fixture _fixture;

    public TypeChecksTests()
    {
        _fixture = new Fixture();
    }

    [Theory]
    [InlineData(typeof(Action), true)]
    [InlineData(typeof(Func<int>), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    public void IsDelegate_ShouldIdentifyDelegateTypes(Type type, bool expected)
    {
        // Act
        bool result = type.IsDelegate();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(int[]), true)]
    [InlineData(typeof(string[]), true)]
    [InlineData(typeof(List<int>), false)]
    [InlineData(typeof(int), false)]
    public void IsArray_ShouldIdentifyArrayTypes(Type type, bool expected)
    {
        // Act
        bool result = type.IsArray();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(Dictionary<int, string>), true)]
    [InlineData(typeof(Hashtable), true)]
    [InlineData(typeof(List<int>), false)]
    [InlineData(typeof(int), false)]
    public void IsDictionary_ShouldIdentifyDictionaryTypes(Type type, bool expected)
    {
        // Act
        bool result = type.IsDictionary();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(List<int>), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int[]), true)]
    [InlineData(typeof(int), false)]
    public void IsEnumerable_ShouldIdentifyEnumerableTypes(Type type, bool expected)
    {
        // Act
        bool result = type.IsEnumerable();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(List<int>), true)]
    [InlineData(null, true)]
    public void IsClassOtherThanString_ShouldIdentifyClassTypesOtherThanString(Type? type, bool expected)
    {
        // Act
        bool result = type.IsClassOtherThanString();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(123, true)]
    [InlineData(123.45, true)]
    [InlineData("string", false)]
    [InlineData(null, false)]
    public void IsNumeric_ShouldIdentifyNumericObjects(object? value, bool expected)
    {
        // Act
        bool result = value.IsNumeric();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int?), true)]
    [InlineData(null, false)]
    public void IsNumericType_ShouldIdentifyNumericTypes(Type? type, bool expected)
    {
        // Act
        bool result = type.IsNumericType();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(List<int>), false)]
    public void IsSimpleType_ShouldIdentifySimpleTypes(Type type, bool expected)
    {
        // Act
        bool result = type.IsSimpleType();

        // Assert
        result.ShouldBe(expected);
    }
}
