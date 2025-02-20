using System.Data;
using System.Data.Common;
using CommonNetFuncs.Sql.Common;
using Microsoft.Data.SqlClient;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Sql.Common.DirectQuery;

namespace CommonNetFuncs.Sql.SqlServer;

/// <summary>
/// Interact with databases by using direct queries
/// </summary>
public static class DirectQuery
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static async Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        await using SqlConnection sqlConn = new(connStr);
        await using SqlCommand sqlCmd = new(sql, sqlConn);
        return await GetDataTableInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static DataTable GetDataTableSynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using SqlConnection sqlConn = new(connStr);
        using SqlCommand sqlCmd = new(sql, sqlConn);
        return GetDataTableInternalSynchronous(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Execute an update query asynchronously
    /// </summary>
    /// <param name="sql">Update query to retrieve run against database</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static async Task<UpdateResult> RunUpdateQuery(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        await using SqlConnection sqlConn = new(connStr);
        await using SqlCommand sqlCmd = new(sql, sqlConn);
        return await RunUpdateQueryInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="sql">Update query to retrieve run against database</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using SqlConnection sqlConn = new(connStr);
        using SqlCommand sqlCmd = new(sql, sqlConn);
        return RunUpdateQueryInternalSynchronous(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Returns a IAsyncEnumerable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static async IAsyncEnumerable<T> GetDataStreaming<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3) where T : class, new()
    {
        await using SqlConnection sqlConn = new(connStr);
        await using SqlCommand sqlCmd = new(sql, sqlConn);

        IAsyncEnumerator<T>? enumeratedReader = null;
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                enumeratedReader = GetDataStreamAsync<T>(sqlConn, sqlCmd, commandTimeoutSeconds).GetAsyncEnumerator();
                break;
            }
            catch (DbException ex)
            {
                logger.Error($"DB Error: {ex}", "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting datatable: {ex}", "{msg}", $"{ex.GetLocationOfException()} Error");
            }
        }

        if (enumeratedReader != null)
        {
            while (await enumeratedReader.MoveNextAsync().ConfigureAwait(false))
            {
                yield return enumeratedReader!.Current;
            }
        }
        else
        {
            yield break;
        }
    }

    /// <summary>
    /// Returns a IAsyncEnumerable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static IEnumerable<T> GetDataStreamingSynchronous<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3) where T : class, new()
    {
        using SqlConnection sqlConn = new(connStr);
        using SqlCommand sqlCmd = new(sql, sqlConn);

        IEnumerable<T>? results = null;
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                results = GetDataStreamSynchronous<T>(sqlConn, sqlCmd, commandTimeoutSeconds);
                break;
            }
            catch (DbException ex)
            {
                logger.Error($"DB Error: {ex}", "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting datatable: {ex}", "{msg}", $"{ex.GetLocationOfException()} Error");
            }
        }

        return results ?? [];
    }

    /// <summary>
    /// Returns an IEnumerable of T resulting from the SQL query
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static async Task<IEnumerable<T>> GetDataDirect<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3) where T : class, new()
    {
        await using SqlConnection sqlConn = new(connStr);
        await using SqlCommand sqlCmd = new(sql, sqlConn);
        return await GetDataDirectAsync<T>(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry).ConfigureAwait(false);
    }
}
