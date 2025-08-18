using System.Reflection;
using CommonNetFuncs.Core;

namespace Core.Tests;

public class ReflectionCachesTests : IDisposable
{
    private readonly Type _testType = typeof(TestClass);

    public ReflectionCachesTests()
    {
        // Ensure a clean state before each test
        ReflectionCaches.CacheManager.SetUseLimitedCache(true);
        ReflectionCaches.CacheManager.SetLimitedCacheSize(100);
        ReflectionCaches.CacheManager.ClearAllCaches();
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
                ReflectionCaches.CacheManager.SetUseLimitedCache(true);
                ReflectionCaches.CacheManager.SetLimitedCacheSize(100);
                ReflectionCaches.CacheManager.ClearAllCaches();
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
        ReflectionCaches.CacheManager.ClearAllCaches();

        // Assert
        // Should be empty, so a new call should trigger reflection again (no exception)
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(x => x.Name == nameof(TestClass.Prop1));
        props.ShouldContain(x => x.Name == nameof(TestClass.Prop2));
    }

    [Fact]
    public void ClearReflectionCache_ClearsUnboundedCache()
    {
        // Arrange
        ReflectionCaches.CacheManager.SetUseLimitedCache(false);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.CacheManager.ClearCache();

        // Assert
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(x => x.Name == nameof(TestClass.Prop1));
    }

    [Fact]
    public void ClearLimitedReflectionCache_ClearsLimitedCache()
    {
        // Arrange
        ReflectionCaches.CacheManager.SetUseLimitedCache(true);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.CacheManager.ClearLimitedCache();

        // Assert
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.ShouldContain(x => x.Name == nameof(TestClass.Prop2));
    }

    [Fact]
    public void SetLimitedReflectionCacheSize_ChangesCacheSizeAndClearsCache()
    {
        // Arrange
        ReflectionCaches.CacheManager.SetUseLimitedCache(true);
        ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        // Act
        ReflectionCaches.CacheManager.SetLimitedCacheSize(1);

        // Assert
        ReflectionCaches.CacheManager.GetLimitedCacheSize().ShouldBe(1);
        // Should still work after resize
        PropertyInfo[] props = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        props.Length.ShouldBe(2);
    }

    [Fact]
    public void SetLimitedReflectionCacheSize_DoesNotClearIfNotUsingLimitedCache()
    {
        // Arrange
        ReflectionCaches.CacheManager.SetUseLimitedCache(false);
        ReflectionCaches.CacheManager.SetLimitedCacheSize(5);

        // Assert
        ReflectionCaches.CacheManager.GetLimitedCacheSize().ShouldBe(5);
        ReflectionCaches.CacheManager.IsUsingLimitedCache().ShouldBeFalse();
    }

    [Fact]
    public void SetUseLimitedReflectionCache_SwitchesModesAndClearsCaches()
    {
        // Arrange
        ReflectionCaches.CacheManager.SetUseLimitedCache(false);
        ReflectionCaches.CacheManager.IsUsingLimitedCache().ShouldBeFalse();

        // Act
        ReflectionCaches.CacheManager.SetUseLimitedCache(true);

        // Assert
        ReflectionCaches.CacheManager.IsUsingLimitedCache().ShouldBeTrue();
    }

    [Fact]
    public void GetLimitedReflectionCacheSize_ReturnsCurrentSize()
    {
        ReflectionCaches.CacheManager.SetLimitedCacheSize(42);
        ReflectionCaches.CacheManager.GetLimitedCacheSize().ShouldBe(42);
    }

    [Fact]
    public void IsUsingLimitedReflectionCache_ReturnsCurrentMode()
    {
        ReflectionCaches.CacheManager.SetUseLimitedCache(false);
        ReflectionCaches.CacheManager.IsUsingLimitedCache().ShouldBeFalse();

        ReflectionCaches.CacheManager.SetUseLimitedCache(true);
        ReflectionCaches.CacheManager.IsUsingLimitedCache().ShouldBeTrue();
    }

    [Fact]
    public void GetOrAddPropertiesFromCache_UsesLimitedCache_WhenEnabled()
    {
        ReflectionCaches.CacheManager.SetUseLimitedCache(true);

        PropertyInfo[] props1 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);
        PropertyInfo[] props2 = ReflectionCaches.GetOrAddPropertiesFromReflectionCache(_testType);

        props1.ShouldBe(props2); // Should be cached
        props1.Length.ShouldBe(2);
    }

    [Fact]
    public void GetOrAddPropertiesFromCache_UsesUnboundedCache_WhenDisabled()
    {
        ReflectionCaches.CacheManager.SetUseLimitedCache(false);

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
