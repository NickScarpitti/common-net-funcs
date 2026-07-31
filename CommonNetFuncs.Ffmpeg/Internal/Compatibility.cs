using System.Diagnostics;

namespace CommonNetFuncs.Ffmpeg.Internal;

/// <summary>
/// Shims for BCL/Core members that only exist on newer TFMs, needed so this project can multi-target netstandard2.1.
/// Not compiled for TFMs where the real member already exists.
/// </summary>
#if !NET5_0_OR_GREATER
internal static class ProcessCompatExtensions
{
	public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
	{
		return Task.Run(() => process.WaitForExit(), cancellationToken);
	}
}
#endif

#if !NET7_0_OR_GREATER
internal static class EnumerableCompatExtensions
{
	public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(static x => x);
	}
}

/// <summary>
/// Non-generic replacement for <c>CommonNetFuncs.Core.UnitConversion.GetFileSizeFromBytesWithUnits</c>, which relies on
/// generic math (IBinaryInteger&lt;T&gt;) that isn't available on netstandard2.1.
/// </summary>
internal static class FileSizeCompatExtensions
{
	public static string GetFileSizeFromBytesWithUnits(this long inputBytes, int decimalPlaces = 1)
	{
		long bytes = Math.Abs(inputBytes);
		long multiplier = bytes > inputBytes ? -1L : 1L;
		decimal kb = Math.Round(bytes / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
		decimal mb = Math.Round(kb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
		decimal gb = Math.Round(mb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
		decimal tb = Math.Round(gb / 1024m, decimalPlaces, MidpointRounding.AwayFromZero);
		return bytes >= 1024
			? kb >= 1024
				? mb >= 1024
					? gb >= 1024
						? $"{tb * multiplier} TB"
						: $"{gb * multiplier} GB"
					: $"{mb * multiplier} MB"
				: $"{kb * multiplier} KB"
			: $"{bytes * multiplier} B";
	}

	public static string GetFileSizeFromBytesWithUnits(this long? nullBytes, int decimalPlaces = 1)
	{
		return nullBytes == null ? "-0" : nullBytes.Value.GetFileSizeFromBytesWithUnits(decimalPlaces);
	}
}
#endif
