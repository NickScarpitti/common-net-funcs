using System.Diagnostics.CodeAnalysis;

namespace CommonNetFuncs.Core.Internal;

/// <summary>
/// Shims for BCL members that only exist on newer TFMs so this project can multi-target netstandard2.1.
/// On net6.0+ these simply forward to the real BCL implementation.
/// </summary>
internal static class ThrowHelper
{
	public static void ThrowIfNull([NotNull] object? argument, string? paramName)
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(argument, paramName);
#else
		if (argument is null)
		{
			throw new ArgumentNullException(paramName);
		}
#endif
	}

	public static void ThrowIfNegativeOrZero(int value, string? paramName)
	{
#if NET8_0_OR_GREATER
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
#else
		if (value <= 0)
		{
			throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative and non-zero value.");
		}
#endif
	}

	public static void ThrowIfDisposed(bool disposed, object instance)
	{
#if NET7_0_OR_GREATER
		ObjectDisposedException.ThrowIf(disposed, instance);
#else
		if (disposed)
		{
			throw new ObjectDisposedException(instance.GetType().FullName);
		}
#endif
	}
}

/// <summary>
/// Shim for <see cref="System.Random.Shared"/>.Shuffle(...) (added in .NET 8) so netstandard2.1 has an equivalent.
/// </summary>
internal static class RandomCompat
{
#if !NET8_0_OR_GREATER
	private static readonly System.Random SharedFallback = new();
#endif

	public static void Shuffle<T>(Span<T> values)
	{
#if NET8_0_OR_GREATER
		System.Random.Shared.Shuffle(values);
#else
		int n = values.Length;
		while (n > 1)
		{
			n--;
			int k = SharedFallback.Next(n + 1);
			(values[k], values[n]) = (values[n], values[k]);
		}
#endif
	}

	public static T[] GetItems<T>(T[] source, int count)
	{
#if NET8_0_OR_GREATER
		return System.Random.Shared.GetItems(source, count);
#else
		if (source.Length == 0)
		{
			throw new ArgumentException("Source collection cannot be empty.", nameof(source));
		}

		T[] result = new T[count];
		for (int i = 0; i < count; i++)
		{
			result[i] = source[SharedFallback.Next(source.Length)];
		}
		return result;
#endif
	}
}

/// <summary>
/// Shim for the <c>DateTime.TryParse(string?, IFormatProvider?, out DateTime)</c> 3-argument overload (added in .NET 7 as part of the IParsable retrofit),
/// which is not available on netstandard2.1.
/// </summary>
internal static class DateTimeCompat
{
	public static bool TryParse(string? value, IFormatProvider? provider, out DateTime result)
	{
#if NET7_0_OR_GREATER
		return DateTime.TryParse(value, provider, out result);
#else
		return DateTime.TryParse(value, provider, System.Globalization.DateTimeStyles.None, out result);
#endif
	}
}

#if !NET9_0_OR_GREATER
/// <summary>
/// Shim for <see cref="System.Collections.Generic.CollectionExtensions.AsReadOnly{TKey, TValue}(IDictionary{TKey, TValue})"/> (added in .NET 9).
/// Not needed on net9.0+, where the real extension method takes precedence.
/// </summary>
internal static class DictionaryCompatExtensions
{
	public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
	{
		return new System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>(dictionary);
	}
}
#endif

/// <summary>
/// Shim for <see cref="Math.Round(decimal, int, MidpointRounding)"/>/<see cref="Math.Round(double, int, MidpointRounding)"/> with <see cref="MidpointRounding.ToZero"/> (added in .NET 8).
/// </summary>
internal static class MathCompat
{
	public static double Round(double value, int digits)
	{
		if (digits is < 0 or > 15)
		{
			throw new ArgumentOutOfRangeException(nameof(digits), "Rounding digits must be between 0 and 15, inclusive.");
		}
#if NET8_0_OR_GREATER
		return Math.Round(value, digits, MidpointRounding.ToZero);
#else
		// MidpointRounding.ToZero truncates toward zero for every value, not only exact midpoints
		double factor = Math.Pow(10, digits);
		return Math.Truncate(value * factor) / factor;
#endif
	}

	public static decimal Round(decimal value, int digits)
	{
		if (digits is < 0 or > 28)
		{
			throw new ArgumentOutOfRangeException(nameof(digits), "Rounding digits must be between 0 and 28, inclusive.");
		}
#if NET8_0_OR_GREATER
		return Math.Round(value, digits, MidpointRounding.ToZero);
#else
		// MidpointRounding.ToZero truncates toward zero for every value, not only exact midpoints
		decimal factor = (decimal)Math.Pow(10, digits);
		return Math.Truncate(value * factor) / factor;
#endif
	}

	/// <summary>
	/// Non-generic greatest common denominator, used instead of the generic-math based <c>MathHelpers.GreatestCommonDenominator&lt;T&gt;</c> (net7.0+ only) for netstandard2.1 compatibility.
	/// </summary>
	public static long Gcd(long a, long b)
	{
		a = Math.Abs(a);
		b = Math.Abs(b);
		while (b != 0)
		{
			long temp = b;
			b = a % b;
			a = temp;
		}
		return a;
	}
}

/// <summary>
/// Shims for <see cref="Cryptography.Convert.ToHexStringLower(byte[])"/> (added in .NET 9) and the static
/// HashData/HashDataAsync methods added to the hash algorithm classes in .NET 5-7.
/// </summary>
internal static class HashCompat
{
	public static string ToHexStringLower(byte[] bytes)
	{
#if NET9_0_OR_GREATER
		return Convert.ToHexStringLower(bytes);
#elif NET5_0_OR_GREATER
		return Convert.ToHexString(bytes).ToLowerInvariant();
#else
		char[] chars = new char[bytes.Length * 2];
		for (int i = 0; i < bytes.Length; i++)
		{
			int b = bytes[i];
			chars[i * 2] = GetHexChar(b >> 4);
			chars[(i * 2) + 1] = GetHexChar(b & 0xF);
		}
		return new string(chars);

		static char GetHexChar(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
#endif
	}

	public static byte[] Md5HashData(byte[] source)
	{
#if NET5_0_OR_GREATER
		return System.Security.Cryptography.MD5.HashData(source);
#else
		using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
		return md5.ComputeHash(source);
#endif
	}

	public static byte[] Sha1HashData(byte[] source)
	{
#if NET5_0_OR_GREATER
		return System.Security.Cryptography.SHA1.HashData(source);
#else
		using System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
		return sha1.ComputeHash(source);
#endif
	}

	public static byte[] Sha256HashData(byte[] source)
	{
#if NET5_0_OR_GREATER
		return System.Security.Cryptography.SHA256.HashData(source);
#else
		using System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
		return sha256.ComputeHash(source);
#endif
	}

	public static byte[] Sha384HashData(byte[] source)
	{
#if NET5_0_OR_GREATER
		return System.Security.Cryptography.SHA384.HashData(source);
#else
		using System.Security.Cryptography.SHA384 sha384 = System.Security.Cryptography.SHA384.Create();
		return sha384.ComputeHash(source);
#endif
	}

	public static byte[] Sha512HashData(byte[] source)
	{
#if NET5_0_OR_GREATER
		return System.Security.Cryptography.SHA512.HashData(source);
#else
		using System.Security.Cryptography.SHA512 sha512 = System.Security.Cryptography.SHA512.Create();
		return sha512.ComputeHash(source);
#endif
	}

	public static async Task<byte[]> Md5HashDataAsync(Stream source, CancellationToken cancellationToken = default)
	{
#if NET6_0_OR_GREATER
		return await System.Security.Cryptography.MD5.HashDataAsync(source, cancellationToken).ConfigureAwait(false);
#else
		using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
		return await ComputeHashAsync(md5, source, cancellationToken).ConfigureAwait(false);
#endif
	}

	public static async Task<byte[]> ComputeHashAsync(System.Security.Cryptography.HashAlgorithm algorithm, Stream stream, CancellationToken cancellationToken = default)
	{
#if NET7_0_OR_GREATER
		return await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
#else
		return await Task.Run(() => algorithm.ComputeHash(stream), cancellationToken).ConfigureAwait(false);
#endif
	}
}

/// <summary>
/// Shims for <see cref="Parallel.ForEachAsync"/> / <see cref="Parallel.ForAsync"/> (added in .NET 6), so netstandard2.1 has an equivalent.
/// The netstandard2.1 fallback runs all iterations concurrently rather than with a bounded degree of parallelism.
/// </summary>
internal static class AsyncCompat
{
	public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
	{
		return ForEachAsync(source, CancellationToken.None, body);
	}

	public static async Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
	{
#if NET6_0_OR_GREATER
		await Parallel.ForEachAsync(source, cancellationToken, body).ConfigureAwait(false);
#else
		List<Task> tasks = [];
		foreach (TSource item in source)
		{
			cancellationToken.ThrowIfCancellationRequested();
			tasks.Add(body(item, cancellationToken).AsTask());
		}
		await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
	}

	public static async Task ForAsync(int fromInclusive, int toExclusive, CancellationToken cancellationToken, Func<int, CancellationToken, ValueTask> body)
	{
#if NET6_0_OR_GREATER
		await Parallel.ForAsync(fromInclusive, toExclusive, cancellationToken, body).ConfigureAwait(false);
#else
		List<Task> tasks = [];
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			tasks.Add(body(i, cancellationToken).AsTask());
		}
		await Task.WhenAll(tasks).ConfigureAwait(false);
#endif
	}
}

#if !NET8_0_OR_GREATER
/// <summary>
/// Shim for <see cref="CancellationTokenSource.CancelAsync"/> (added in .NET 8). Not needed on net8.0+, where the real instance method takes precedence.
/// </summary>
internal static class CancellationTokenSourceExtensions
{
	public static Task CancelAsync(this CancellationTokenSource cancellationTokenSource)
	{
		cancellationTokenSource.Cancel();
		return Task.CompletedTask;
	}
}
#endif

#if !NET7_0_OR_GREATER
/// <summary>
/// Shim for <see cref="System.Security.Cryptography.HashAlgorithm.ComputeHashAsync"/> (added in .NET 7). Not needed on net7.0+, where the real instance method takes precedence.
/// </summary>
internal static class HashAlgorithmExtensions
{
	public static Task<byte[]> ComputeHashAsync(this System.Security.Cryptography.HashAlgorithm algorithm, Stream stream, CancellationToken cancellationToken = default)
	{
		return Task.Run(() => algorithm.ComputeHash(stream), cancellationToken);
	}
}
#endif

/// <summary>
/// Shim for the <c>DateOnly.TryParse(string?, IFormatProvider?, out DateOnly)</c> 3-argument overload, which the netstandard2.1
/// DateOnly polyfill (Portable.System.DateTimeOnly) does not expose.
/// </summary>
internal static class DateOnlyCompat
{
	public static bool TryParse(string? value, IFormatProvider? provider, out DateOnly result)
	{
#if NET6_0_OR_GREATER
		return DateOnly.TryParse(value, provider, out result);
#else
		return DateOnly.TryParse(value, provider, System.Globalization.DateTimeStyles.None, out result);
#endif
	}

	public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, out DateOnly result)
	{
#if NET6_0_OR_GREATER
		return DateOnly.TryParse(value, provider, out result);
#else
		return TryParse(value.ToString(), provider, out result);
#endif
	}
}
