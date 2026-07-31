using System.Globalization;
using CommonNetFuncs.Core.Internal;

namespace Core.Tests.Internal;

public sealed class ThrowHelperTests
{
	[Fact]
	public void ThrowIfNull_WithNonNullArgument_DoesNotThrow()
	{
		Should.NotThrow(() => ThrowHelper.ThrowIfNull("value", "paramName"));
	}

	[Fact]
	public void ThrowIfNull_WithNullArgument_ThrowsArgumentNullException()
	{
		object? argument = null;
		ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => ThrowHelper.ThrowIfNull(argument, "paramName"));
		exception.ParamName.ShouldBe("paramName");
	}

	[Theory]
	[InlineData(1)]
	[InlineData(100)]
	[InlineData(int.MaxValue)]
	public void ThrowIfNegativeOrZero_WithPositiveValue_DoesNotThrow(int value)
	{
		Should.NotThrow(() => ThrowHelper.ThrowIfNegativeOrZero(value, "paramName"));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MinValue)]
	public void ThrowIfNegativeOrZero_WithNonPositiveValue_ThrowsArgumentOutOfRangeException(int value)
	{
		ArgumentOutOfRangeException exception = Should.Throw<ArgumentOutOfRangeException>(() => ThrowHelper.ThrowIfNegativeOrZero(value, "paramName"));
		exception.ParamName.ShouldBe("paramName");
	}

	[Fact]
	public void ThrowIfDisposed_WhenNotDisposed_DoesNotThrow()
	{
		Should.NotThrow(() => ThrowHelper.ThrowIfDisposed(false, new object()));
	}

	[Fact]
	public void ThrowIfDisposed_WhenDisposed_ThrowsObjectDisposedException()
	{
		Should.Throw<ObjectDisposedException>(() => ThrowHelper.ThrowIfDisposed(true, new object()));
	}
}

public sealed class RandomCompatTests
{
	[Fact]
	public void Shuffle_PreservesAllElements()
	{
		int[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
		int[] original = [.. values];

		RandomCompat.Shuffle(values.AsSpan());

		values.ShouldBe(original, ignoreOrder: true);
	}

	[Fact]
	public void Shuffle_EmptyArray_DoesNotThrow()
	{
		int[] values = [];
		Should.NotThrow(() => RandomCompat.Shuffle(values.AsSpan()));
	}

	[Fact]
	public void Shuffle_SingleElement_DoesNotChangeValue()
	{
		int[] values = [42];
		RandomCompat.Shuffle(values.AsSpan());
		values.ShouldBe([42]);
	}

	[Fact]
	public void GetItems_ReturnsRequestedCount_WithValuesFromSource()
	{
		int[] source = [1, 2, 3];
		int[] result = RandomCompat.GetItems(source, 10);

		result.Length.ShouldBe(10);
		result.ShouldAllBe(x => source.Contains(x));
	}

	[Fact]
	public void GetItems_WithEmptySource_ThrowsArgumentException()
	{
		int[] source = [];
		Should.Throw<ArgumentException>(() => RandomCompat.GetItems(source, 5));
	}

	[Fact]
	public void GetItems_WithZeroCount_ReturnsEmptyArray()
	{
		int[] source = [1, 2, 3];
		int[] result = RandomCompat.GetItems(source, 0);
		result.ShouldBeEmpty();
	}
}

public sealed class DateTimeCompatTests
{
	[Fact]
	public void TryParse_ValidDate_ReturnsTrueAndParsesValue()
	{
		bool result = DateTimeCompat.TryParse("2024-01-15", CultureInfo.InvariantCulture, out DateTime parsed);

		result.ShouldBeTrue();
		parsed.ShouldBe(new DateTime(2024, 1, 15));
	}

	[Fact]
	public void TryParse_InvalidDate_ReturnsFalse()
	{
		bool result = DateTimeCompat.TryParse("not-a-date", CultureInfo.InvariantCulture, out DateTime parsed);

		result.ShouldBeFalse();
		parsed.ShouldBe(default);
	}

	[Fact]
	public void TryParse_NullValue_ReturnsFalse()
	{
		bool result = DateTimeCompat.TryParse(null, CultureInfo.InvariantCulture, out DateTime parsed);
		result.ShouldBeFalse();
	}

	[Fact]
	public void TryParse_NullProvider_UsesCurrentCulture()
	{
		bool result = DateTimeCompat.TryParse("2024-01-15", null, out DateTime parsed);
		result.ShouldBeTrue();
		parsed.Year.ShouldBe(2024);
	}
}

public sealed class DateOnlyCompatTests
{
	[Fact]
	public void TryParse_String_ValidDate_ReturnsTrueAndParsesValue()
	{
		bool result = DateOnlyCompat.TryParse("2024-01-15", CultureInfo.InvariantCulture, out DateOnly parsed);

		result.ShouldBeTrue();
		parsed.ShouldBe(new DateOnly(2024, 1, 15));
	}

	[Fact]
	public void TryParse_String_InvalidDate_ReturnsFalse()
	{
		bool result = DateOnlyCompat.TryParse("not-a-date", CultureInfo.InvariantCulture, out DateOnly parsed);
		result.ShouldBeFalse();
		parsed.ShouldBe(default);
	}

	[Fact]
	public void TryParse_Span_ValidDate_ReturnsTrueAndParsesValue()
	{
		bool result = DateOnlyCompat.TryParse("2024-01-15".AsSpan(), CultureInfo.InvariantCulture, out DateOnly parsed);

		result.ShouldBeTrue();
		parsed.ShouldBe(new DateOnly(2024, 1, 15));
	}

	[Fact]
	public void TryParse_Span_InvalidDate_ReturnsFalse()
	{
		bool result = DateOnlyCompat.TryParse("not-a-date".AsSpan(), CultureInfo.InvariantCulture, out DateOnly parsed);
		result.ShouldBeFalse();
	}
}

public sealed class MathCompatTests
{
	[Theory]
	[InlineData(1.005, 2, 1.0)]     // truncates toward zero, does not round the midpoint up
	[InlineData(-1.005, 2, -1.0)]   // truncates toward zero for negative values too
	[InlineData(1.239, 2, 1.23)]
	[InlineData(1.0, 0, 1.0)]
	public void Round_Double_TruncatesTowardZero(double value, int digits, double expected)
	{
		MathCompat.Round(value, digits).ShouldBe(expected, 0.0001);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(16)]
	public void Round_Double_InvalidDigits_ThrowsArgumentOutOfRangeException(int digits)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => MathCompat.Round(1.23, digits));
	}

	[Theory]
	[InlineData(1.239, 2, 1.23)]
	[InlineData(1.0, 0, 1.0)]
	public void Round_Decimal_TruncatesTowardZero(double value, int digits, double expected)
	{
		MathCompat.Round((decimal)value, digits).ShouldBe((decimal)expected);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(29)]
	public void Round_Decimal_InvalidDigits_ThrowsArgumentOutOfRangeException(int digits)
	{
		Should.Throw<ArgumentOutOfRangeException>(() => MathCompat.Round(1.23m, digits));
	}

	[Theory]
	[InlineData(12, 8, 4)]
	[InlineData(25, 15, 5)]
	[InlineData(7, 13, 1)]
	[InlineData(-12, 8, 4)]      // absolute value used
	[InlineData(0, 5, 5)]
	public void Gcd_ReturnsGreatestCommonDenominator(long a, long b, long expected)
	{
		MathCompat.Gcd(a, b).ShouldBe(expected);
	}
}

public sealed class HashCompatTests
{
	[Fact]
	public void ToHexStringLower_ReturnsLowercaseHex()
	{
		byte[] bytes = [0xAB, 0xCD, 0xEF, 0x01];
		string result = HashCompat.ToHexStringLower(bytes);
		result.ShouldBe("abcdef01");
	}

	[Fact]
	public void ToHexStringLower_EmptyArray_ReturnsEmptyString()
	{
		HashCompat.ToHexStringLower([]).ShouldBe(string.Empty);
	}

	[Fact]
	public void Md5HashData_ReturnsExpectedHash()
	{
		byte[] source = "hello world"u8.ToArray();
		byte[] hash = HashCompat.Md5HashData(source);
		HashCompat.ToHexStringLower(hash).ShouldBe("5eb63bbbe01eeed093cb22bb8f5acdc3");
	}

	[Fact]
	public void Sha1HashData_ReturnsExpectedHash()
	{
		byte[] source = "hello world"u8.ToArray();
		byte[] hash = HashCompat.Sha1HashData(source);
		HashCompat.ToHexStringLower(hash).ShouldBe("2aae6c35c94fcfb415dbe95f408b9ce91ee846ed");
	}

	[Fact]
	public void Sha256HashData_ReturnsExpectedHash()
	{
		byte[] source = "hello world"u8.ToArray();
		byte[] hash = HashCompat.Sha256HashData(source);
		HashCompat.ToHexStringLower(hash).ShouldBe("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9");
	}

	[Fact]
	public void Sha384HashData_ReturnsHashOfExpectedLength()
	{
		byte[] source = "hello world"u8.ToArray();
		byte[] hash = HashCompat.Sha384HashData(source);
		hash.Length.ShouldBe(48);
	}

	[Fact]
	public void Sha512HashData_ReturnsHashOfExpectedLength()
	{
		byte[] source = "hello world"u8.ToArray();
		byte[] hash = HashCompat.Sha512HashData(source);
		hash.Length.ShouldBe(64);
	}

	[Fact]
	public async Task Md5HashDataAsync_ReturnsExpectedHash()
	{
		using MemoryStream stream = new("hello world"u8.ToArray());
		byte[] hash = await HashCompat.Md5HashDataAsync(stream, TestContext.Current.CancellationToken);
		HashCompat.ToHexStringLower(hash).ShouldBe("5eb63bbbe01eeed093cb22bb8f5acdc3");
	}

	[Fact]
	public async Task ComputeHashAsync_WithSha256_ReturnsExpectedHash()
	{
		using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
		using MemoryStream stream = new("hello world"u8.ToArray());
		byte[] hash = await HashCompat.ComputeHashAsync(sha256, stream, TestContext.Current.CancellationToken);
		HashCompat.ToHexStringLower(hash).ShouldBe("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9");
	}
}

public sealed class AsyncCompatTests
{
	[Fact]
	public async Task ForEachAsync_ProcessesAllItems()
	{
		int[] source = [1, 2, 3, 4, 5];
		System.Collections.Concurrent.ConcurrentBag<int> processed = [];

		await AsyncCompat.ForEachAsync(source, async (item, ct) =>
		{
			await Task.Yield();
			processed.Add(item);
		});

		processed.OrderBy(x => x).ShouldBe(source);
	}

	[Fact]
	public async Task ForEachAsync_WithCancellationToken_ProcessesAllItemsWhenNotCancelled()
	{
		int[] source = [1, 2, 3];
		System.Collections.Concurrent.ConcurrentBag<int> processed = [];

		await AsyncCompat.ForEachAsync(source, CancellationToken.None, async (item, ct) =>
		{
			await Task.Yield();
			processed.Add(item);
		});

		processed.Count.ShouldBe(3);
	}

	[Fact]
	public async Task ForEachAsync_WithCancelledToken_ThrowsOperationCanceledException()
	{
		int[] source = [1, 2, 3];
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();

		await Should.ThrowAsync<OperationCanceledException>(async () =>
			await AsyncCompat.ForEachAsync(source, cts.Token, async (item, ct) =>
			{
				await Task.Yield();
			}));
	}

	[Fact]
	public async Task ForAsync_ProcessesAllIndexesInRange()
	{
		System.Collections.Concurrent.ConcurrentBag<int> processed = [];

		await AsyncCompat.ForAsync(0, 5, CancellationToken.None, async (i, ct) =>
		{
			await Task.Yield();
			processed.Add(i);
		});

		processed.OrderBy(x => x).ShouldBe([0, 1, 2, 3, 4]);
	}

	[Fact]
	public async Task ForAsync_EmptyRange_ProcessesNothing()
	{
		int count = 0;
		await AsyncCompat.ForAsync(5, 5, CancellationToken.None, (i, ct) =>
		{
			count++;
			return ValueTask.CompletedTask;
		});

		count.ShouldBe(0);
	}
}

public sealed class CancellationTokenSourceExtensionsTests
{
	[Fact]
	public async Task CancelAsync_CancelsTheToken()
	{
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();
		cts.IsCancellationRequested.ShouldBeTrue();
	}

	// On both test TFMs the real BCL instance method takes precedence over the extension, so the shim body is never actually
	// reached via normal member resolution. Call it explicitly via its static form to exercise the shim itself (netstandard2.1 build only).
#if !CORE_NATIVE_BUILD
	[Fact]
	public async Task CancelAsync_Shim_CancelsTheToken()
	{
		using CancellationTokenSource cts = new();
		await CancellationTokenSourceExtensions.CancelAsync(cts);
		cts.IsCancellationRequested.ShouldBeTrue();
	}
#endif
}

public sealed class HashAlgorithmExtensionsTests
{
	[Fact]
	public async Task ComputeHashAsync_Extension_ReturnsExpectedHash()
	{
		using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
		using MemoryStream stream = new("hello world"u8.ToArray());
		byte[] hash = await md5.ComputeHashAsync(stream, TestContext.Current.CancellationToken);
		HashCompat.ToHexStringLower(hash).ShouldBe("5eb63bbbe01eeed093cb22bb8f5acdc3");
	}

	// On both test TFMs the real BCL instance method takes precedence over the extension, so the shim body is never actually
	// reached via normal member resolution. Call it explicitly via its static form to exercise the shim itself (netstandard2.1 build only).
#if !CORE_NATIVE_BUILD
	[Fact]
	public async Task ComputeHashAsync_Shim_ReturnsExpectedHash()
	{
		using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
		using MemoryStream stream = new("hello world"u8.ToArray());
		byte[] hash = await HashAlgorithmExtensions.ComputeHashAsync(md5, stream, TestContext.Current.CancellationToken);
		HashCompat.ToHexStringLower(hash).ShouldBe("5eb63bbbe01eeed093cb22bb8f5acdc3");
	}
#endif
}

// DictionaryCompatExtensions.AsReadOnly only exists in the netstandard2.1 build of Core (guarded by #if !NET9_0_OR_GREATER in production); on net10.0 the type isn't compiled at all.
#if !CORE_NATIVE_BUILD
public sealed class DictionaryCompatExtensionsTests
{
	[Fact]
	public void AsReadOnly_ReturnsReadOnlyViewOfDictionary()
	{
		Dictionary<string, int> dictionary = new() { ["a"] = 1, ["b"] = 2 };
		// Called via explicit static syntax to unambiguously target the shim rather than the real BCL extension when both are in scope.
		IReadOnlyDictionary<string, int> readOnly = DictionaryCompatExtensions.AsReadOnly(dictionary);

		readOnly.Count.ShouldBe(2);
		readOnly["a"].ShouldBe(1);
	}

	[Fact]
	public void AsReadOnly_ReflectsChangesToUnderlyingDictionary()
	{
		Dictionary<string, int> dictionary = new() { ["a"] = 1 };
		IReadOnlyDictionary<string, int> readOnly = DictionaryCompatExtensions.AsReadOnly(dictionary);

		dictionary["b"] = 2;

		readOnly.Count.ShouldBe(2);
		readOnly["b"].ShouldBe(2);
	}
}
#endif
