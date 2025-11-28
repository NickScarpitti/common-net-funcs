using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace CommonNetFuncs.Web.Requests;

public static class DistributedCacheExtensions
{
	private static readonly JsonSerializerOptions serializerOptions = new()
	{
		PropertyNamingPolicy = null,
		WriteIndented = true,
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static Task SetAsync<T>(this IDistributedCache cache, string key, T value)
	{
		return SetAsync(cache, key, value, new DistributedCacheEntryOptions());
	}

	public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, serializerOptions));
		return cache.SetAsync(key, bytes, options);
	}

	public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T? value)
	{
		byte[]? val = cache.Get(key);
		value = default;
		if (val == null)
		{
			return false;
		}

		value = JsonSerializer.Deserialize<T>(val, serializerOptions);
		return true;
	}

	public static async Task<T?> TryGetValueAsync<T>(this IDistributedCache cache, string key)
	{
		byte[]? val = await cache.GetAsync(key);
		if (val == null)
		{
			return default;
		}

		await using MemoryStream stream = new(val);
		return await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions).ConfigureAwait(false);
	}
}
