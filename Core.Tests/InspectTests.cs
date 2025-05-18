using System.ComponentModel;
using CommonNetFuncs.Core;

namespace Core.Tests;

#pragma warning disable CRR1000 // The name does not correspond to naming conventions
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
public class InspectTests
{
    // Helper classes for testing
    public class SimpleClass
    {
        public int IntProp { get; set; }

        public string? StringProp { get; set; }
    }

    public class NestedClass
    {
        public int Id { get; set; }

        public SimpleClass? Child { get; set; }
    }


    public static readonly TheoryData<(Type, object?)> defaultValueTestData = new()
    {
        new(typeof(int), default(int)),
        new(typeof(string), default(string)),
        new(typeof(DateTime), default(DateTime)),
    };

    [Theory]
    [MemberData(nameof(defaultValueTestData), MemberType = typeof(InspectTests))]
    public void GetDefaultValue_ReturnsExpected((Type type, object? expected) values)
    {
        object? result = values.type.GetDefaultValue();
        result.ShouldBe(values.expected);
    }

    [Theory]
    [InlineData(0, null, 2)]
    [InlineData(1, "not default", 0)]
    public void CountDefaultProps_ReturnsCorrectCount(int intProp, string? stringProp, int expected)
    {
        SimpleClass obj = new() { IntProp = intProp, StringProp = stringProp };
        int count = obj.CountDefaultProps();
        count.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(ClassWithDescription), "DescriptionAttribute", true)]
    [InlineData(typeof(SimpleClass), "DescriptionAttribute", false)]
    public void ObjectHasAttribute_Works(Type type, string attrName, bool expected)
    { type.ObjectHasAttribute(attrName).ShouldBe(expected); }

    // Replace the untyped IEnumerable<object[]> with strongly-typed TheoryData<> for better type safety.

    public static TheoryData<SimpleClass?, SimpleClass?, IEnumerable<string>?, bool> IsEqualRTestData => new()
    {
        { new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 5, StringProp = "abc" }, null, true },
        { new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 6, StringProp = "abc" }, null, false },
        { new SimpleClass { IntProp = 5, StringProp = "abc" }, new SimpleClass { IntProp = 6, StringProp = "abc" }, ["IntProp"], true },
        { null, null, null, true },
        { new SimpleClass(), null, null, false },
    };

    [Theory]

    [MemberData(nameof(IsEqualRTestData))]
    public void IsEqualR_Works(object? a, object? b, IEnumerable<string>? exempt, bool expected)
    { a.IsEqualR(b, exempt).ShouldBe(expected); }

    [Theory]
    [MemberData(nameof(IsEqualRTestData))]
    public void IsEqualR_And_IsEqual_Consistency(object? a, object? b, IEnumerable<string>? exempt, bool expected)
    { a.IsEqual(b, exemptProps: exempt).ShouldBe(expected); }

    [Fact]
    public void IsEqual_Recursive_ReturnsTrue_ForIdenticalNestedObjects()
    {
        NestedClass a = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
        NestedClass b = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
        a.IsEqual(b).ShouldBeTrue();
    }

    [Fact]
    public void IsEqual_Recursive_ReturnsFalse_ForDifferentNestedObjects()
    {
        NestedClass a = new() { Id = 1, Child = new SimpleClass { IntProp = 2, StringProp = "x" } };
        NestedClass b = new() { Id = 1, Child = new SimpleClass { IntProp = 3, StringProp = "x" } };
        a.IsEqual(b).ShouldBeFalse();
    }

    [Fact]
    public void IsEqual_Recursive_HandlesCycles()
    {
        CyclicClass a = new();
        CyclicClass b = new();
        a.Self = a;
        b.Self = b;
        a.IsEqual(b).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, "a", 2, "a", new[] { "IntProp" }, true)]
    [InlineData(1, "a", 2, "b", new[] { "IntProp" }, false)]
    public void IsEqual_ExemptProps_Works(int aInt, string aStr, int bInt, string bStr, string[] exempt, bool expected)
    {
        SimpleClass a = new() { IntProp = aInt, StringProp = aStr };
        SimpleClass b = new() { IntProp = bInt, StringProp = bStr };
        a.IsEqual(b, exemptProps: exempt).ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc", "ABC", true, true)]
    [InlineData("abc", "ABC", false, false)]
    [InlineData("abc", "abc", true, true)]
    public void IsEqual_IgnoreStringCase_Works(string aStr, string bStr, bool ignoreCase, bool expected)
    {
        SimpleClass a = new() { IntProp = 1, StringProp = aStr };
        SimpleClass b = new() { IntProp = 1, StringProp = bStr };
        a.IsEqual(b, ignoreStringCase: ignoreCase).ShouldBe(expected);
    }

    public static TheoryData<SimpleClass, SimpleClass, bool> HashTestData => new()
    {
        { new SimpleClass { IntProp = 1, StringProp = "abc" }, new SimpleClass { IntProp = 1, StringProp = "abc" }, true },
        { new SimpleClass { IntProp = 1, StringProp = "abc" }, new SimpleClass { IntProp = 2, StringProp = "abc" }, false },
    };

    [Theory]
    [MemberData(nameof(HashTestData))]
    public void GetHashCode_And_GetHashForObject_Consistency(SimpleClass a, SimpleClass b, bool shouldMatch)
    {
        // a.GetHashCode().ShouldBe(b.GetHashCode(), shouldMatch ? "Hashes should match" : "Hashes should not match");
        string aHash = a.GetHashForObject();
        string bHash = b.GetHashForObject();
        if (shouldMatch)
        {
            aHash.ShouldBe(bHash);
        }
        else
        {
            aHash.ShouldNotBe(bHash);
        }
    }

    [Theory]
    [MemberData(nameof(HashTestData))]
    public async Task GetHashCode_And_GetHashForObjectAsync_Consistency(SimpleClass a, SimpleClass b, bool shouldMatch)
    {
        // a.GetHashCode().ShouldBe(b.GetHashCode(), shouldMatch ? "Hashes should match" : "Hashes should not match");
        string aHash = await a.GetHashForObjectAsync();
        string bHash = await b.GetHashForObjectAsync();
        if (shouldMatch)
        {
            aHash.ShouldBe(bHash);
        }
        else
        {
            aHash.ShouldNotBe(bHash);
        }
    }

    [Fact]
    public void GetHashForObject_ReturnsNullString_ForNull()
    {
        string hash = ((SimpleClass?)null).GetHashForObject();
        hash.ShouldBe("null");
    }

    [Fact]
    public void GetHashForObject_CollectionOrderIndependence()
    {
        ClassWithCollection a = new() { Items = new[] { 1, 2, 3 } };
        ClassWithCollection b = new() { Items = new[] { 3, 2, 1 } };
        a.GetHashForObject().ShouldBe(b.GetHashForObject());
    }

    // Helper types for attribute and cycle tests
    [Description("desc")]
    private class ClassWithDescription
    {
    }

    private class CyclicClass
    {
        public CyclicClass? Self { get; set; }
    }

    private class ClassWithCollection
    {
        public IEnumerable<int>? Items { get; set; }
    }
}

#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
#pragma warning restore IDE0079 // Remove unnecessary suppression
#pragma warning restore CRR1000 // The name does not correspond to naming conventions
