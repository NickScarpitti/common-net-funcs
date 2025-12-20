using System.Reflection;
using System.Runtime.CompilerServices;

namespace CommonNetFuncs.Core;

public static class ReflectionCaches
{
	private static readonly CacheManager<Type, PropertyInfo[]> ReflectionCache = new();

	public static ICacheManagerApi<Type, PropertyInfo[]> CacheManager => ReflectionCache;

	/// <summary>
	/// Gets the properties of <paramref name="type"/> from the reflection cache, or adds them if not already cached.
	/// Uses GetOrAdd pattern for better performance under contention.
	/// </summary>
	/// <param name="type">Type to get properties for. Will store found properties in cache if <paramref name="type"/> is not already cached</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PropertyInfo[] GetOrAddPropertiesFromReflectionCache(Type type)
	{
		// Use GetOrAdd pattern - more efficient than TryGetValue + TryAdd
		return CacheManager.IsUsingLimitedCache()
			? CacheManager.GetOrAddLimitedCache(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			: CacheManager.GetOrAddCache(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
	}
}
