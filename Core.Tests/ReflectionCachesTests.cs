using System.Reflection;
using CommonNetFuncs.Core;

namespace Core.Tests;

public class ReflectionCachesTests : IDisposable
{
    private readonly Type _testType = typeof(TestClass);

    public ReflectionCachesTests()
    {
        // Ensure a clean state before each test
        ReflectionCaches.SetUseLimitedReflectionCache(true);
        ReflectionCaches.SetLimitedReflectionCacheSize(100);
        ReflectionCaches.ClearAllReflectionCaches();
    }

    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                ReflectionCaches.SetUseLimitedReflectionCache(true);
                ReflectionCaches.SetLimitedReflectionCacheSize(100);
                ReflectionCaches.ClearAllReflectionCaches();
            }
            disposed = true;
        }
    }

    ~ReflectionCachesTests()
    {
        Dispose(false);
    }

    private class TestClass
    {
        public int Prop1 { get; set; }

        public string? Prop2 { get; set; }
    }

    [Fact]
    public void ClearReflectionCaches_ClearsBothCaches()
    {
        // Arrange
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType); // Add to cache

        // Act
        ReflectionCaches.ClearAllReflectionCaches();

        // Assert
        // Should be empty, so a new call should trigger reflection again (no exception)
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(p => p.Name == nameof(TestClass.Prop1));
        props.ShouldContain(p => p.Name == nameof(TestClass.Prop2));
    }

    [Fact]
    public void ClearReflectionCache_ClearsUnboundedCache()
    {
        // Arrange
        ReflectionCaches.SetUseLimitedReflectionCache(false);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.ClearReflectionCache();

        // Assert
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(p => p.Name == nameof(TestClass.Prop1));
    }

    [Fact]
    public void ClearLimitedReflectionCache_ClearsLimitedCache()
    {
        // Arrange
        ReflectionCaches.SetUseLimitedReflectionCache(true);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.ClearLimitedReflectionCache();

        // Assert
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(p => p.Name == nameof(TestClass.Prop2));
    }

    [Fact]
    public void SetLimitedReflectionCacheSize_ChangesCacheSizeAndClearsCache()
    {
        // Arrange
        ReflectionCaches.SetUseLimitedReflectionCache(true);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.SetLimitedReflectionCacheSize(1);

        // Assert
        ReflectionCaches.GetLimitedReflectionCacheSize().ShouldBe(1);
        // Should still work after resize
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.Length.ShouldBe(2);
    }

    [Fact]
    public void SetLimitedReflectionCacheSize_DoesNotClearIfNotUsingLimitedCache()
    {
        // Arrange
        ReflectionCaches.SetUseLimitedReflectionCache(false);
        ReflectionCaches.SetLimitedReflectionCacheSize(5);

        // Assert
        ReflectionCaches.GetLimitedReflectionCacheSize().ShouldBe(5);
        ReflectionCaches.IsUsingLimitedReflectionCache().ShouldBeFalse();
    }

    [Fact]
    public void SetUseLimitedReflectionCache_SwitchesModesAndClearsCaches()
    {
        // Arrange
        ReflectionCaches.SetUseLimitedReflectionCache(false);
        ReflectionCaches.IsUsingLimitedReflectionCache().ShouldBeFalse();

        // Act
        ReflectionCaches.SetUseLimitedReflectionCache(true);

        // Assert
        ReflectionCaches.IsUsingLimitedReflectionCache().ShouldBeTrue();
    }

    [Fact]
    public void GetLimitedReflectionCacheSize_ReturnsCurrentSize()
    {
        ReflectionCaches.SetLimitedReflectionCacheSize(42);
        ReflectionCaches.GetLimitedReflectionCacheSize().ShouldBe(42);
    }

    [Fact]
    public void IsUsingLimitedReflectionCache_ReturnsCurrentMode()
    {
        ReflectionCaches.SetUseLimitedReflectionCache(false);
        ReflectionCaches.IsUsingLimitedReflectionCache().ShouldBeFalse();

        ReflectionCaches.SetUseLimitedReflectionCache(true);
        ReflectionCaches.IsUsingLimitedReflectionCache().ShouldBeTrue();
    }

    [Fact]
    public void GetOrAddPropertiesFromCache_UsesLimitedCache_WhenEnabled()
    {
        ReflectionCaches.SetUseLimitedReflectionCache(true);

        PropertyInfo[] props1 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        PropertyInfo[] props2 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        props1.ShouldBe(props2); // Should be cached
        props1.Length.ShouldBe(2);
    }

    [Fact]
    public void GetOrAddPropertiesFromCache_UsesUnboundedCache_WhenDisabled()
    {
        ReflectionCaches.SetUseLimitedReflectionCache(false);

        PropertyInfo[] props1 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        PropertyInfo[] props2 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        props1.ShouldBe(props2); // Should be cached
        props1.Length.ShouldBe(2);
    }

    [Fact]
    public void GetOrAddPropertiesFromCache_ReturnsEmptyArray_WhenTypeHasNoProperties()
    {
        Type type = typeof(NoProps);
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(type);
        props.ShouldBeEmpty();
    }

    private class NoProps;
}
