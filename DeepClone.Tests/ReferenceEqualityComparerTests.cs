﻿namespace DeepClone.Tests;

public sealed class ReferenceEqualityComparerTests
{
    private readonly Fixture _fixture;
    private readonly CommonNetFuncs.DeepClone.ReferenceEqualityComparer _comparer;

    public ReferenceEqualityComparerTests()
    {
        _fixture = new Fixture();
        _comparer = new CommonNetFuncs.DeepClone.ReferenceEqualityComparer();
    }

    [Theory]
    [InlineData(null, null, true)]
    public void Equals_WhenBothParametersAreNull_ShouldReturnTrue(object? x, object? y, bool expected)
    {
        // Act
        bool result = _comparer.Equals(x, y);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Equals_WhenSameReference_ShouldReturnTrue()
    {
        // Arrange
        object obj = new();

        // Act
        bool result = _comparer.Equals(obj, obj);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_WhenDifferentReferences_ShouldReturnFalse()
    {
        // Arrange
        string str1 = _fixture.Create<string>();
        string str2 = new(str1.ToCharArray());

        // Act
        bool result = _comparer.Equals(str1, str2);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, 0)]
    public void GetHashCode_WhenObjectIsNull_ShouldReturnZero(object? obj, int expected)
    {
        // Act
        int result = _comparer.GetHashCode(obj!);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GetHashCode_WhenObjectNotNull_ShouldReturnObjectHashCode()
    {
        // Arrange
        object obj = new();
        int expected = obj.GetHashCode();

        // Act
        int result = _comparer.GetHashCode(obj);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GetHashCode_WhenCalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        object obj = new();

        // Act
        int firstCall = _comparer.GetHashCode(obj);
        int secondCall = _comparer.GetHashCode(obj);

        // Assert
        firstCall.ShouldBe(secondCall);
    }
}
