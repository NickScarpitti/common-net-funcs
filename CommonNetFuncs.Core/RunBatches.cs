namespace CommonNetFuncs.Core;

/// <summary>
/// Run batches of operations on a collection of items.
/// </summary>
public static class RunBatches
{
	//public delegate bool BatchedProcess<T>(IReadOnlyList<T> itemsToProcess);
	//public delegate Task<bool> AsyncBatchedProcess<T>(IReadOnlyList<T> itemsToProcess);
	//public delegate Task<bool> AsyncBatchedProcessList<T>(List<T> itemsToProcess);

	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Takes a collection of items and processes them in batches using the provided async processor.
	/// </summary>
	public static async Task<bool> RunBatchedProcessAsync<T>(this IEnumerable<T> itemsToProcess, Func<IEnumerable<T>, Task<bool>> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(itemsToProcess);
		ArgumentNullException.ThrowIfNull(processor);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

		// Materialize distinct items once
		List<T> distinctItems = itemsToProcess.Distinct().ToList();
		int totalBatches = (int)MathHelpers.Ceiling((decimal)distinctItems.Count / batchSize, 1);
		bool success = true;

		for (int i = 0; i < totalBatches; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			int start = i * batchSize;
			int count = Math.Min(batchSize, distinctItems.Count - start);
			IReadOnlyList<T> batch = distinctItems.GetRange(start, count);

			success &= await processor(batch).ConfigureAwait(false);

			if (logProgress)
			{
				logger.Info("{msg}", $"Process {i + 1}/{totalBatches} complete");
			}

			if (!success && breakOnFail)
			{
				break;
			}
		}

		return success;
	}

	public static Task<bool> RunBatchedProcessAsync<T>(this IEnumerable<T> itemsToProcess, Func<List<T>, Task<bool>> listProcessor, int batchSize = 10000, bool breakOnFail = true,
			bool logProgress = true, CancellationToken cancellationToken = default)
	{
		// Adapt the List processor to work with IReadOnlyList
		return RunBatchedProcessAsync(itemsToProcess, async batch => await listProcessor(batch is List<T> l ? l : batch.ToList()).ConfigureAwait(false), batchSize, breakOnFail, logProgress, cancellationToken);
	}

	/// <summary>
	/// Takes a collection of items and processes them in batches using the provided sync processor.
	/// </summary>
	//public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, SyncBatchProcessor<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
	public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, Func<IEnumerable<T>, bool> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(itemsToProcess);
		ArgumentNullException.ThrowIfNull(processor);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

		// Materialize distinct items once
		List<T> distinctItems = itemsToProcess.Distinct().ToList();
		int totalBatches = (int)MathHelpers.Ceiling((decimal)distinctItems.Count / batchSize, 1);
		bool success = true;

		for (int i = 0; i < totalBatches; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			int start = i * batchSize;
			int count = Math.Min(batchSize, distinctItems.Count - start);
			IReadOnlyList<T> batch = distinctItems.GetRange(start, count);

			success &= processor(batch);

			if (logProgress)
			{
				logger.Info("{msg}", $"Process {i + 1}/{totalBatches} complete");
			}

			if (!success && breakOnFail)
			{
				break;
			}
		}

		return success;
	}

	// Extension methods for List-specific processors
	public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, Func<List<T>, bool> listProcessor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true,
			CancellationToken cancellationToken = default)
	{
		// Adapt the List processor to work with IReadOnlyList
		return RunBatchedProcess(itemsToProcess, batch => listProcessor(batch is List<T> l ? l : batch.ToList()), batchSize, breakOnFail, logProgress, cancellationToken);
	}
}
