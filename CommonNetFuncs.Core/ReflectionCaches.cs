using System.Reflection;

namespace CommonNetFuncs.Core;

public static class ReflectionCaches
{
    private static readonly CacheManager<Type, PropertyInfo[]> ReflectionCache = new();

    public static ICacheManagerApi<Type, PropertyInfo[]> CacheManager => ReflectionCache;

    /// <summary>
    /// Clears LimitedReflectionCache cache and sets the size to the specified value.
    /// </summary>
    /// <param name="type">Type to get properties for. Will store </param>
    public static PropertyInfo[] GetOrAddPropertiesFromReflectionCache(Type type)
    {
        bool isLimitedCache = CacheManager.IsUsingLimitedCache();
        if (isLimitedCache ? CacheManager.GetLimitedCache().TryGetValue(type, out PropertyInfo[]? properties) :
            CacheManager.GetCache().TryGetValue(type, out properties))
        {
            return properties ?? [];
        }

        properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (isLimitedCache)
        {
            CacheManager.TryAddLimitedCache(type, properties);
        }
        else
        {
            CacheManager.TryAddCache(type, properties);
        }
        return properties;
    }
}
