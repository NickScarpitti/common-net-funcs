using System.Data;
using Npgsql;
using CommonNetFuncs.Sql.Common;
using static CommonNetFuncs.Sql.Common.DirectQuery;

namespace CommonNetFuncs.Sql.PostgreSQL;

/// <summary>
/// Interact with databases by using direct queries
/// </summary>
public static class DirectQuery
{
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
        await using NpgsqlConnection sqlConn = new(connStr);
        await using NpgsqlCommand sqlCmd = new(sql, sqlConn);
        return await GetDataTableInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
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
        using NpgsqlConnection sqlConn = new(connStr);
        using NpgsqlCommand sqlCmd = new(sql, sqlConn);
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
        await using NpgsqlConnection sqlConn = new(connStr);
        await using NpgsqlCommand sqlCmd = new(sql, sqlConn);
        return await RunUpdateQueryInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
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
        using NpgsqlConnection sqlConn = new(connStr);
        using NpgsqlCommand sqlCmd = new(sql, sqlConn);
        return RunUpdateQueryInternalSynchronous(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
    }
}
