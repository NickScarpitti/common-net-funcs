using NLog;

namespace CommonNetFuncs.Core;

public static class RunBatches
{
    //public delegate bool BatchedProcess<T>(IReadOnlyList<T> itemsToProcess);
    //public delegate Task<bool> AsyncBatchedProcess<T>(IReadOnlyList<T> itemsToProcess);
    //public delegate Task<bool> AsyncBatchedProcessList<T>(List<T> itemsToProcess);

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    ///// <summary>
    ///// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    ///// </summary>
    ///// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    ///// <param name="processor">Delegate method accepting List<T> and returns a bool indicating success</param>
    ///// <param name="batchSize">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    ///// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    ///// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    ///// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    //public static async Task<bool> RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, AsyncBatchedProcess<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    //{
    //    ArgumentNullException.ThrowIfNull(itemsToProcess);
    //    ArgumentNullException.ThrowIfNull(processor);
    //    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

    //    int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchSize, 1);
    //    bool success = true;

    //    for (int i = 0; i < totalProcesses; i++)
    //    {
    //        IReadOnlyList<T> items = itemsToProcess.Skip(i * batchSize).Take(batchSize).ToList();
    //        success = await processor(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
    //        if (!success && breakOnFail)
    //        {
    //            break;
    //        }
    //        if (logProgress)
    //        {
    //            logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete");
    //        }
    //    }
    //    return success;
    //}

    /////// <summary>
    /////// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /////// </summary>
    /////// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /////// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /////// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /////// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /////// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /////// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    ////public static async Task<bool> RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, AsyncBatchedProcessList<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    ////{
    ////    int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
    ////    bool success = true;

    ////    for (int i = 0; i < totalProcesses; i++)
    ////    {
    ////        List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
    ////        success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
    ////        if (!success && breakOnFail)
    ////        {
    ////            break;
    ////        }
    ////        if (logProgress)
    ////        {
    ////            logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete");
    ////        }
    ////    }
    ////    return success;
    ////}

    /////// <summary>
    /////// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /////// </summary>
    /////// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /////// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /////// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /////// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /////// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /////// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    ////public static async Task<bool> RunBatchedProcess<T>(this List<T> itemsToProcess, AsyncBatchedProcess<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    ////{
    ////    int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
    ////    bool success = true;

    ////    for (int i = 0; i < totalProcesses; i++)
    ////    {
    ////        IReadOnlyList<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
    ////        success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
    ////        if (!success && breakOnFail) { break; }
    ////        if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
    ////    }
    ////    return success;
    ////}

    /////// <summary>
    /////// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /////// </summary>
    /////// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /////// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /////// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /////// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /////// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /////// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    ////public static async Task<bool> RunBatchedProcess<T>(this List<T> itemsToProcess, AsyncBatchedProcessList<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    ////{
    ////    int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
    ////    bool success = true;

    ////    for (int i = 0; i < totalProcesses; i++)
    ////    {
    ////        List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
    ////        success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
    ////        if (!success && breakOnFail) { break; }
    ////        if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
    ////    }
    ////    return success;
    ////}

    ///// <summary>
    ///// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    ///// </summary>
    ///// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    ///// <param name="processor">Delegate method accepting List<T> and returns a bool indicating success</param>
    ///// <param name="batchSize">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    ///// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    ///// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    ///// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    //public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, BatchedProcess<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    //{
    //    ArgumentNullException.ThrowIfNull(itemsToProcess);
    //    ArgumentNullException.ThrowIfNull(processor);
    //    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

    //    int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchSize, 1);
    //    bool success = true;

    //    for (int i = 0; i < totalProcesses; i++)
    //    {
    //        IReadOnlyList<T> items = itemsToProcess.Skip(i * batchSize).Take(batchSize).ToList();
    //        success = processor(items) && success; //Allow for success to fail while still continuing
    //        if (!success && breakOnFail)
    //        {
    //            break;
    //        }
    //        if (logProgress)
    //        {
    //            logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete");
    //        }
    //    }
    //    return success;
    //}
    //
    ///// <summary>
    ///// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    ///// </summary>
    ///// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    ///// <param name="processor">Delegate method accepting List<T> and returns a bool indicating success</param>
    ///// <param name="batchSize">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    ///// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    ///// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    ///// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    //public static Task<bool> RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, AsyncBatchedProcessList<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    //{
    //    // Create an adapter function that converts IReadOnlyList to List
    //    return RunBatchedProcess(itemsToProcess, async (IReadOnlyList<T> items) => await processor(items.ToList()), batchSize, breakOnFail, logProgress);
    //}

    // Single delegate type for async processing that can handle both IReadOnlyList and List
    //public delegate Task<bool> AsyncBatchProcessor<T>(IReadOnlyList<T> batch);
    //public delegate bool SyncBatchProcessor<T>(IReadOnlyList<T> batch);

    /// <summary>
/// Takes a collection of items and processes them in batches using the provided async processor.
/// </summary>
    //public static async Task<bool> RunBatchedProcessAsync<T>(this IEnumerable<T> itemsToProcess, AsyncBatchProcessor<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    public static async Task<bool> RunBatchedProcessAsync<T>(this IEnumerable<T> itemsToProcess, Func<IEnumerable<T>, Task<bool>> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
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
            IReadOnlyList<T> batch = distinctItems.Skip(i * batchSize).Take(batchSize).ToList();

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

    /// <summary>
    /// Takes a collection of items and processes them in batches using the provided sync processor.
    /// </summary>
    //public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, SyncBatchProcessor<T> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, Func<IEnumerable<T>, bool> processor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
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
            IReadOnlyList<T> batch = distinctItems.Skip(i * batchSize).Take(batchSize).ToList();

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
    public static Task<bool> RunBatchedProcessAsync<T>(this IEnumerable<T> itemsToProcess, Func<List<T>, Task<bool>> listProcessor, int batchSize = 10000, bool breakOnFail = true,
        bool logProgress = true)
    {
        // Adapt the List processor to work with IReadOnlyList
        return RunBatchedProcessAsync(itemsToProcess, async batch => await listProcessor(batch.ToList()), batchSize, breakOnFail, logProgress);
    }

    public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, Func<List<T>, bool> listProcessor, int batchSize = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        // Adapt the List processor to work with IReadOnlyList
        return RunBatchedProcess(itemsToProcess, batch => listProcessor(batch.ToList()), batchSize, breakOnFail, logProgress);
    }
}
