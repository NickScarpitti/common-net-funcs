using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class CopyTests
{
    private readonly Fixture _fixture;

    public CopyTests()
    {
        _fixture = new Fixture();
    }

    #region CopyPropertiesTo Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesTo_ShouldCopyMatchingProperties(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Description = "Test Description"
        };

        DestinationClass destination = new();

        // Act
        source.CopyPropertiesTo(destination, useCache);

        // Assert
        destination.Id.ShouldBe(source.Id);
        destination.Name.ShouldBe(source.Name);

        // Description is not in destination, so it shouldn't be copied
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesTo_WithNullSource_ShouldNotThrowException(bool useCache)
    {
        // Arrange
        SourceClass? source = null;
        DestinationClass destination = new();

        // Act & Assert
        Should.NotThrow(() => source.CopyPropertiesTo(destination, useCache));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesTo_WithNullDestination_ShouldNotThrowException(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test"
        };
        DestinationClass? destination = null;

        // Act & Assert
        Should.NotThrow(() => source.CopyPropertiesTo(destination, useCache));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesTo_WithDifferentPropertyTypes_ShouldNotCopyIncompatibleProperties(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            CreatedDate = DateTime.Now
        };

        DestinationWithDifferentTypes destination = new()
        {
            Id = 0, // Will be copied
            Name = null, // Will be copied
            CreatedDate = 0 // Should not be copied as types don't match
        };

        // Act
        source.CopyPropertiesTo(destination, useCache);

        // Assert
        destination.Id.ShouldBe(source.Id);
        destination.Name.ShouldBe(source.Name);
        destination.CreatedDate.ShouldBe(0); // Should remain unchanged
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesTo_WithReadOnlyDestinationProperties_ShouldNotCopyToReadOnlyProperties(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test"
        };

        ClassWithReadOnlyProperty destination = new();

        // Act
        source.CopyPropertiesTo(destination, useCache);

        // Assert
        destination.Id.ShouldBe(0); // Should remain unchanged as it's read-only
        destination.Name.ShouldBe(source.Name);
    }

    #endregion

    #region CopyPropertiesToNew<T> Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNew_ShouldCreateNewInstanceWithCopiedProperties(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Description = "Test Description"
        };

        // Act
        SourceClass result = source.CopyPropertiesToNew(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(source);
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Description.ShouldBe(source.Description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNew_WithNullSource_ShouldReturnNull(bool useCache)
    {
        // Arrange
        SourceClass? source = null;

        // Act
        SourceClass? result = source.CopyPropertiesToNew(useCache: useCache);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CopyPropertiesToNew<T, UT> Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNew_Generic_ShouldCreateNewInstanceOfDifferentTypeWithCopiedProperties(bool useCache)
    {
        // Arrange
        SourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Description = "Test Description"
        };

        // Act
        DestinationClass result = source.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<DestinationClass>();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);

        // Description is not in destination, so it shouldn't be copied
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNew_Generic_WithNullSource_ShouldReturnNull(bool useCache)
    {
        // Arrange
        SourceClass? source = null;

        // Act
        DestinationClass? result = source?.CopyPropertiesToNew<SourceClass, DestinationClass>(useCache: useCache);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CopyPropertiesRecursive Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNewRecursive_ShouldCopySimpleProperties(bool useCache)
    {
        // Arrange
        ComplexSourceClass source = new()
        {
            Id = 1,
            Name = "Test"
        };

        // Act
        ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNewRecursive_ShouldCopyNestedObjects(bool useCache)
    {
        // Arrange
        ComplexSourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Child = new SourceClass
            {
                Id = 2,
                Name = "Child"
            }
        };

        // Act
        ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Child.ShouldNotBeNull();
        result.Child.Id.ShouldBe(source.Child.Id);
        result.Child.Name.ShouldBe(source.Child.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNewRecursive_ShouldCopyCollections(bool useCache)
    {
        // Arrange
        ComplexSourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Items = new List<SourceClass>
                {
                    new() { Id = 2, Name = "Item 1" },
                    new() { Id = 3, Name = "Item 2" }
                }
        };

        // Act
        ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Items.ShouldNotBeNull();
        result.Items.Count.ShouldBe(source.Items.Count);
        result.Items[0].Id.ShouldBe(source.Items[0].Id);
        result.Items[0].Name.ShouldBe(source.Items[0].Name);
        result.Items[1].Id.ShouldBe(source.Items[1].Id);
        result.Items[1].Name.ShouldBe(source.Items[1].Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesRecursive_ShouldCopyDictionaries(bool useCache)
    {
        // Arrange
        ComplexSourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Dictionary = new Dictionary<string, SourceClass>
            {
                ["key1"] = new SourceClass { Id = 2, Name = "Value 1" },
                ["key2"] = new SourceClass { Id = 3, Name = "Value 2" }
            }
        };

        // Act
        ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Dictionary.ShouldNotBeNull();
        result.Dictionary.Count.ShouldBe(source.Dictionary.Count);
        result.Dictionary["key1"].Id.ShouldBe(source.Dictionary["key1"].Id);
        result.Dictionary["key1"].Name.ShouldBe(source.Dictionary["key1"].Name);
        result.Dictionary["key2"].Id.ShouldBe(source.Dictionary["key2"].Id);
        result.Dictionary["key2"].Name.ShouldBe(source.Dictionary["key2"].Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNewRecursive_WithMaxDepth_ShouldLimitRecursionDepth(bool useCache)
    {
        // Arrange
        ComplexSourceClass source = new()
        {
            Id = 1,
            Name = "Test",
            Child = new SourceClass
            {
                Id = 2,
                Name = "Child"
            },
            NestedChild = new ComplexSourceClass
            {
                Id = 3,
                Name = "Nested",
                Child = new SourceClass
                {
                    Id = 4,
                    Name = "Deeply Nested"
                }
            }
        };

        // Act - Set max depth to 1
        ComplexDestinationClass result = source.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(1, useCache);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(source.Id);
        result.Name.ShouldBe(source.Name);
        result.Child.ShouldNotBeNull();
        result.Child.Id.ShouldBe(source.Child.Id);
        result.Child.Name.ShouldBe(source.Child.Name);

        result.NestedChild.ShouldNotBeNull();
        result.NestedChild.Id.ShouldBe(source.NestedChild.Id);
        result.NestedChild.Name.ShouldBe(source.NestedChild.Name);

        // This should be null because we limited recursion depth to 1
        result.NestedChild.Child.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyPropertiesToNewRecursive_WithNullSource_ShouldReturnNull(bool useCache)
    {
        // Arrange
        ComplexSourceClass? source = null;

        // Act
        ComplexDestinationClass? result = source?.CopyPropertiesToNewRecursive<ComplexSourceClass, ComplexDestinationClass>(useCache: useCache);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region MergeInstances Tests

    [Fact]
    public void MergeInstances_WithMultipleObjects_ShouldMergeNonDefaultValues()
    {
        // Arrange
        SourceClass target = new()
        {
            Id = 0, // Default value
            Name = null // Default value
        };

        SourceClass source1 = new()
        {
            Id = 1, // Non-default value
            Name = null // Default value
        };

        SourceClass source2 = new()
        {
            Id = 0, // Default value
            Name = "Test" // Non-default value
        };

        // Act
        SourceClass result = target.MergeInstances(new[] { source1, source2 });

        // Assert
        result.ShouldBeSameAs(target); // Should return the same instance
        result.Id.ShouldBe(1); // Should take value from source1
        result.Name.ShouldBe("Test"); // Should take value from source2
    }

    [Fact]
    public void MergeInstances_WithSingleObject_ShouldMergeNonDefaultValues()
    {
        // Arrange
        SourceClass target = new()
        {
            Id = 0, // Default value
            Name = null // Default value
        };

        SourceClass source = new()
        {
            Id = 1, // Non-default value
            Name = "Test" // Non-default value
        };

        // Act
        SourceClass result = target.MergeInstances(source);

        // Assert
        result.ShouldBeSameAs(target); // Should return the same instance
        result.Id.ShouldBe(1); // Should take value from source
        result.Name.ShouldBe("Test"); // Should take value from source
    }

    [Fact]
    public void MergeInstances_WithDefaultValues_ShouldNotOverrideNonDefaultValues()
    {
        // Arrange
        SourceClass target = new()
        {
            Id = 1, // Non-default value
            Name = "Original" // Non-default value
        };

        SourceClass source = new()
        {
            Id = 0, // Default value
            Name = "Test" // Non-default value
        };

        // Act
        SourceClass result = target.MergeInstances(source);

        // Assert
        result.ShouldBeSameAs(target); // Should return the same instance
        result.Id.ShouldBe(1); // Should keep original value
        result.Name.ShouldBe("Original"); // Should keep original value since it's non-default
    }

    #endregion

    #region Test Classes

    public sealed class SourceClass
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public sealed class DestinationClass
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        // No Description property
    }

    public sealed class DestinationWithDifferentTypes
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int CreatedDate { get; set; } // Different type from SourceClass.CreatedDate
    }

    public sealed class ClassWithReadOnlyProperty
    {
        public int Id { get; } // Read-only property

        public string? Name { get; set; }
    }

    public sealed class ComplexSourceClass
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public SourceClass? Child { get; set; }

        public ComplexSourceClass? NestedChild { get; set; }

        public List<SourceClass>? Items { get; set; }

        public Dictionary<string, SourceClass>? Dictionary { get; set; }
    }

    public sealed class ComplexDestinationClass
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public DestinationClass? Child { get; set; }

        public ComplexDestinationClass? NestedChild { get; set; }

        public List<DestinationClass>? Items { get; set; }

        public Dictionary<string, DestinationClass>? Dictionary { get; set; }
    }

    #endregion
}
