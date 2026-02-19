using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using static CommonNetFuncs.Web.Requests.DistributedCacheExtensions;

namespace Web.Requests.Tests;

public class DistributedCacheExtensionsTests
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = null,
		WriteIndented = true,
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
	};

	[Theory]
	[InlineData("test-key", 123)]
	[InlineData("another-key", 0)]
	[InlineData("string-key", "value")]
	[InlineData("null-key", null)]
	public async Task SetAsync_SetsValueCorrectly<T>(string key, T value)
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		DistributedCacheEntryOptions options = new();
		byte[] expectedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));

		// Act

		await cache.SetAsync(key, value, options);

		// Assert

		A.CallTo(() => cache.SetAsync(key, A<byte[]>.That.Matches(b => b.SequenceEqual(expectedBytes)), options, default)).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task SetAsync_Overload_UsesDefaultOptions()
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		const string key = "default-options";
		const int value = 42;
		byte[] expectedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));

		// Act

		await cache.SetAsync(key, value);

		// Assert

		A.CallTo(() => cache.SetAsync(key, A<byte[]>.That.Matches(b => b.SequenceEqual(expectedBytes)), A<DistributedCacheEntryOptions>.Ignored, default)).MustHaveHappenedOnceExactly();
	}

	[Theory]
	[InlineData("existing-key", 123)]
	[InlineData("string-key", "abc")]
	public void TryGetValue_ReturnsTrueAndDeserializes<T>(string key, T value)
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));
		A.CallTo(() => cache.Get(key)).Returns(bytes);

		// Act

		bool result = cache.TryGetValue(key, out T? actual);

		// Assert

		result.ShouldBeTrue();
		actual.ShouldBeEquivalentTo(value);
	}

	[Fact]
	public void TryGetValue_ReturnsFalse_WhenKeyNotFound()
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		const string key = "missing-key";
		A.CallTo(() => cache.Get(key)).Returns(null);

		// Act

		bool result = cache.TryGetValue(key, out int actual);

		// Assert

		result.ShouldBeFalse();
		actual.ShouldBe(default);
	}

	[Theory]
	[InlineData("async-key", 999)]
	[InlineData("async-string", "async-value")]
	public async Task TryGetValueAsync_ReturnsDeserializedValue<T>(string key, T value)
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));
		A.CallTo(() => cache.GetAsync(key, default)).Returns(bytes);

		// Act

		T? result = await cache.TryGetValueAsync<T>(key);

		// Assert

		result.ShouldBeEquivalentTo(value);
	}

	[Fact]
	public async Task TryGetValueAsync_ReturnsDefault_WhenKeyNotFound()
	{
		// Arrange

		IDistributedCache cache = A.Fake<IDistributedCache>();
		const string key = "not-found";
		A.CallTo(() => cache.GetAsync(key, default)).Returns(Task.FromResult<byte[]?>(null));

		// Act

		int result = await cache.TryGetValueAsync<int>(key);

		// Assert

		result.ShouldBe(default);
	}
}
