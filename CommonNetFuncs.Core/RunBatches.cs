using NLog;

namespace CommonNetFuncs.Core;

public static class RunBatches
{
    public delegate bool BatchedProcess<T>(List<T> itemsToProcess);
    public delegate Task<bool> AsyncBatchedProcess<T>(IEnumerable<T> itemsToProcess);
    public delegate Task<bool> AsyncBatchedProcessList<T>(List<T> itemsToProcess);

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /// </summary>
    /// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    public static async Task<bool> RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, AsyncBatchedProcess<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
        bool success = true;

        for (int i = 0; i < totalProcesses; i++)
        {
            List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
            success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
            if (!success && breakOnFail) { break; }
            if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
        }
        return success;
    }

    /// <summary>
    /// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /// </summary>
    /// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    public static async Task<bool> RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, AsyncBatchedProcessList<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
        bool success = true;

        for (int i = 0; i < totalProcesses; i++)
        {
            List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
            success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
            if (!success && breakOnFail) { break; }
            if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
        }
        return success;
    }

    /// <summary>
    /// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /// </summary>
    /// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    public static async Task<bool> RunBatchedProcess<T>(this List<T> itemsToProcess, AsyncBatchedProcess<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
        bool success = true;

        for (int i = 0; i < totalProcesses; i++)
        {
            List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
            success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
            if (!success && breakOnFail) { break; }
            if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
        }
        return success;
    }

    /// <summary>
    /// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /// </summary>
    /// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    public static async Task<bool> RunBatchedProcess<T>(this List<T> itemsToProcess, AsyncBatchedProcessList<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
        bool success = true;

        for (int i = 0; i < totalProcesses; i++)
        {
            List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
            success = await batchedProcess(items).ConfigureAwait(false) && success; //Allow for success to fail while still continuing
            if (!success && breakOnFail) { break; }
            if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
        }
        return success;
    }

    /// <summary>
    /// Takes a collection of an object with type T, and delegate method that accepts a list of the same object type T, and chunks the collection to the designated size and sequentially runs each chunk through the method.
    /// </summary>
    /// <param name="itemsToProcess">Items of type T to be passed into the delegate method</param>
    /// <param name="batchedProcess">Delegate method accepting List<T> and returns a bool indicating success</param>
    /// <param name="batchInterval">Number of items from itemsToProcess to pass to batchedProcess at a time</param>
    /// <param name="breakOnFail">If batchedProcess fails, stop running subsequent batches through batchedProcess</param>
    /// <param name="logProgress">If true, show log output indicating how many batches have been run out of the total number of batches to run</param>
    /// <returns>True if batchedProcess returned true for items in itemsToProcess</returns>
    public static bool RunBatchedProcess<T>(this IEnumerable<T> itemsToProcess, BatchedProcess<T> batchedProcess, int batchInterval = 10000, bool breakOnFail = true, bool logProgress = true)
    {
        int totalProcesses = (int)MathHelpers.Ceiling((decimal)itemsToProcess.Distinct().Count() / batchInterval, 1);
        bool success = true;

        for (int i = 0; i < totalProcesses; i++)
        {
            List<T> items = itemsToProcess.Skip(i * batchInterval).Take(batchInterval).ToList();
            success = batchedProcess(items) && success; //Allow for success to fail while still continuing
            if (!success && breakOnFail) { break; }
            if (logProgress) { logger.Info("{msg}", $"Process {i + 1}/{totalProcesses} complete"); }
        }
        return success;
    }
}
