#if !NET6_0_OR_GREATER
namespace CommonNetFuncs.Excel.Npoi.Internal;

/// <summary>
/// Shim for <see cref="System.Linq.Enumerable.MinBy{TSource, TKey}(System.Collections.Generic.IEnumerable{TSource}, System.Func{TSource, TKey})"/> (added in .NET 6).
/// </summary>
internal static class LinqCompat
{
	public static TSource? MinBy<TSource>(this IEnumerable<TSource> source, Func<TSource, double> keySelector)
	{
		bool hasValue = false;
		TSource? minItem = default;
		double minKey = default;
		foreach (TSource item in source)
		{
			double key = keySelector(item);
			if (!hasValue || key < minKey)
			{
				hasValue = true;
				minItem = item;
				minKey = key;
			}
		}
		return minItem;
	}
}
#endif
