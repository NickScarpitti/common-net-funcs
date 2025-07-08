using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using CommonNetFuncs.Sql.Common;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Sql.Common.DirectQuery;

namespace CommonNetFuncs.Sql.Odbc;

/// <summary>
/// Interact with databases by using direct queries
/// </summary>
public class DirectQuery(Func<string, OdbcConnection>? connectionFactory = null) : IDirectQuery
{
    private readonly Func<string, OdbcConnection> connectionFactory = connectionFactory ?? (connStr => new OdbcConnection(connStr));

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public async Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
    {
        await using OdbcConnection sqlConn = connectionFactory(connStr);
        await using OdbcCommand sqlCmd = new(sql, sqlConn);
        return await GetDataTableInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public DataTable GetDataTableSynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using OdbcConnection sqlConn = connectionFactory(connStr);
        using OdbcCommand sqlCmd = new(sql, sqlConn);
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
    public async Task<UpdateResult> RunUpdateQuery(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
    {
        await using OdbcConnection sqlConn = connectionFactory(connStr);
        await using OdbcCommand sqlCmd = new(sql, sqlConn);
        return await RunUpdateQueryInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="sql">Update query to retrieve run against database</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using OdbcConnection sqlConn = connectionFactory(connStr);
        using OdbcCommand sqlCmd = new(sql, sqlConn);
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
    public async IAsyncEnumerable<T> GetDataStreamAsync<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
    {
        await using OdbcConnection sqlConn = connectionFactory(connStr);
        await using OdbcCommand sqlCmd = new(sql, sqlConn);

        IAsyncEnumerator<T>? enumeratedReader = null;
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                enumeratedReader = Common.DirectQuery.GetDataStreamAsync<T>(sqlConn, sqlCmd, commandTimeoutSeconds, cancellationToken).GetAsyncEnumerator(cancellationToken);
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
    /// Returns a IEnumerable of T resulting from the SQL query
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public IEnumerable<T> GetDataStreamSynchronous<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default) where T : class, new()
    {
        using OdbcConnection sqlConn = connectionFactory(connStr);
        using OdbcCommand sqlCmd = new(sql, sqlConn);

        IEnumerable<T>? results = null;
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                results = Common.DirectQuery.GetDataStreamSynchronous<T>(sqlConn, sqlCmd, commandTimeoutSeconds, cancellationToken);
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
    public async Task<IEnumerable<T>> GetDataDirectAsync<T>(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default) where T : class, new()
    {
        await using OdbcConnection sqlConn = connectionFactory(connStr);
        await using OdbcCommand sqlCmd = new(sql, sqlConn);
        return await Common.DirectQuery.GetDataDirectAsync<T>(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry, cancellationToken).ConfigureAwait(false);
    }
}
