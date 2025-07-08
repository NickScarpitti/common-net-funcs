using System.Reflection;

namespace CommonNetFuncs.Core;

public static class ReflectionCaches
{
    private static int LimitedReflectionCacheSize = 100;
    private static readonly Dictionary<Type, PropertyInfo[]> ReflectionCache = new();
    private static FixedFIFODictionary<Type, PropertyInfo[]> LimitedReflectionCache = new(LimitedReflectionCacheSize); // Default to 100 entries
    private static bool UseLimitedReflectionCache = true;

    public static void ClearReflectionCaches()
    {
        ClearReflectionCache();
        ClearLimitedReflectionCache();
    }

    public static void ClearReflectionCache()
    {
        ReflectionCache.Clear();
    }

    public static void ClearLimitedReflectionCache()
    {
        LimitedReflectionCache.Clear();
    }

    /// <summary>
    /// Clears LimitedReflectionCache cache and sets the size to the specified value.
    /// </summary>
    /// <param name="size">Maximum number of entries to allow in LimitedReflectionCache before removing oldest entry according to FIFO rules</param>
    public static void SetLimitedReflectionCacheSize(int size)
    {
        LimitedReflectionCacheSize = size;
        if (UseLimitedReflectionCache)
        {
            ClearLimitedReflectionCache();
            LimitedReflectionCache = new(LimitedReflectionCacheSize);
        }
    }

    /// <summary>
    /// Clears caches and initializes LimitedReflectionCache to use the size specified by LimitedReflectionCacheSize or 0 if UseLimitedReflectionCache is false.
    /// </summary>
    /// <param name="useLimitedReflectionCache">When true, uses cache with limited number of total records</param>
    public static void SetUseLimitedReflectionCache(bool useLimitedReflectionCache)
    {
        ClearReflectionCaches();
        SetLimitedReflectionCacheSize(useLimitedReflectionCache ? LimitedReflectionCacheSize : 1);
        UseLimitedReflectionCache = useLimitedReflectionCache;
    }

    public static int GetLimitedReflectionCacheSize()
    {
        return LimitedReflectionCacheSize;
    }

    /// <summary>
    /// Returns whether the LimitedReflectionCache is being used.
    /// </summary>
    public static bool IsUsingLimitedReflectionCache()
    {
        return UseLimitedReflectionCache;
    }

    /// <summary>
    /// Clears LimitedReflectionCache cache and sets the size to the specified value.
    /// </summary>
    /// <param name="type">Type to get properties for. Will store </param>
    public static PropertyInfo[] GetOrAddPropertiesFromCache(Type type)
    {
        if (UseLimitedReflectionCache)
        {
            if (LimitedReflectionCache.TryGetValue(type, out PropertyInfo[]? properties))
            {
                return properties ?? [];
            }
            properties = type.GetProperties();
            LimitedReflectionCache.Add(type, properties);
            return properties;
        }
        else
        {
            if (ReflectionCache.TryGetValue(type, out PropertyInfo[]? properties))
            {
                return properties ?? [];
            }
            properties = type.GetProperties();
            ReflectionCache.Add(type, properties);
            return properties;
        }
    }
}
