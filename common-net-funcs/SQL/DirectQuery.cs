using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using Npgsql;
using static Common_Net_Funcs.Tools.DebugHelpers;

namespace Common_Net_Funcs.SQL;

public class UpdateResult
{
    public int RecordsChanged { get; set; }
    public bool Success { get; set; }
}

public enum EDbType
{
    SQLSERVER = 0,
    PGSQL = 1,
    ODBC = 2,
}

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
    public static async Task<DataTable> GetDataTable(string sql, string connStr, EDbType dbType, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        switch (dbType)
        {
            case EDbType.SQLSERVER:
                await using (SqlConnection sqlConn = new(connStr))
                {
                    await using SqlCommand sqlCmd = new(sql, sqlConn);
                    return await GetDataTableInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.PGSQL:
                await using (NpgsqlConnection pgConn = new(connStr))
                {
                    await using NpgsqlCommand pgCmd = new(sql, pgConn);
                    return await GetDataTableInternal(pgConn, pgCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.ODBC:
                await using (OdbcConnection odbcConn = new(connStr))
                {
                    await using OdbcCommand odbcCmd = new(sql, odbcConn);
                    return await GetDataTableInternal(odbcConn, odbcCmd, commandTimeoutSeconds, maxRetry);
                }
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the database command</returns>
    public static Task<DataTable> GetDataTable(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return GetDataTableInternal(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Reads data using into a DataTable object using the provided database connection and command
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the database command</returns>
    private static async Task<DataTable> GetDataTableInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using DataTable dt = new();
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                cmd.CommandTimeout = commandTimeoutSeconds;
                await conn.OpenAsync();
                await using DbDataReader reader = await cmd.ExecuteReaderAsync();
                dt.Load(reader);
                break;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                dt.Clear();
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                dt.Clear();
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        return dt;
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static DataTable GetDataTableSynchronous(string sql, string connStr, EDbType dbType, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        switch (dbType)
        {
            case EDbType.SQLSERVER:
                using (SqlConnection sqlConn = new(connStr))
                {
                    using SqlCommand sqlCmd = new(sql, sqlConn);
                    return GetDataTableInternalSynchronous(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.PGSQL:
                using (NpgsqlConnection pgConn = new(connStr))
                {
                    using NpgsqlCommand pgCmd = new(sql, pgConn);
                    return GetDataTableInternalSynchronous(pgConn, pgCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.ODBC:
                using (OdbcConnection odbcConn = new(connStr))
                {
                    using OdbcCommand odbcCmd = new(sql, odbcConn);
                    return GetDataTableInternalSynchronous(odbcConn, odbcCmd, commandTimeoutSeconds, maxRetry);
                }
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the database command</returns>
    public static DataTable GetDataTableSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return GetDataTableInternalSynchronous(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Reads data using into a DataTable object using the provided database connection and command
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>DataTable containing the results of the database command</returns>
    private static DataTable GetDataTableInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using DataTable dt = new();
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                using DbDataReader reader = cmd.ExecuteReader();
                dt.Load(reader);
                break;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                dt.Clear();
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                dt.Clear();
            }
            finally
            {
                conn.Close();
            }
        }
        return dt;
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="sql">Update query to retrieve run against database</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static async Task<UpdateResult> RunUpdateQuery(string sql, string connStr, EDbType dbType, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        switch (dbType)
        {
            case EDbType.SQLSERVER:
                await using (SqlConnection sqlConn = new(connStr))
                {
                    await using SqlCommand sqlCmd = new(sql, sqlConn);
                    return await RunUpdateQueryInternal(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.PGSQL:
                await using (NpgsqlConnection pgConn = new(connStr))
                {
                    await using NpgsqlCommand pgCmd = new(sql, pgConn);
                    return await RunUpdateQueryInternal(pgConn, pgCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.ODBC:
                await using (OdbcConnection odbcConn = new(connStr))
                {
                    await using OdbcCommand odbcCmd = new(sql, odbcConn);
                    return await RunUpdateQueryInternal(odbcConn, odbcCmd, commandTimeoutSeconds, maxRetry);
                }
            default:
                throw new NotImplementedException();
        }
    }

    public static Task<UpdateResult> RunUpdateQuery(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return RunUpdateQueryInternal(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    private static async Task<UpdateResult> RunUpdateQueryInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                UpdateResult updateResult = new();
                cmd.CommandTimeout = commandTimeoutSeconds;
                await conn.OpenAsync();
                updateResult.RecordsChanged = await cmd.ExecuteNonQueryAsync();
                updateResult.Success = true;
                return updateResult;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
        return new();
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="sql">Update query to retrieve run against database</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, EDbType dbType, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        switch (dbType)
        {
            case EDbType.SQLSERVER:
                using (SqlConnection sqlConn = new(connStr))
                {
                    using SqlCommand sqlCmd = new(sql, sqlConn);
                    return RunUpdateQueryInternalSynchronous(sqlConn, sqlCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.PGSQL:
                using (NpgsqlConnection pgConn = new(connStr))
                {
                    using NpgsqlCommand pgCmd = new(sql, pgConn);
                    return RunUpdateQueryInternalSynchronous(pgConn, pgCmd, commandTimeoutSeconds, maxRetry);
                }
            case EDbType.ODBC:
                using (OdbcConnection odbcConn = new(connStr))
                {
                    using OdbcCommand odbcCmd = new(sql, odbcConn);
                    return RunUpdateQueryInternalSynchronous(odbcConn, odbcCmd, commandTimeoutSeconds, maxRetry);
                }
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static UpdateResult RunUpdateQuerySynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return RunUpdateQueryInternalSynchronous(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Execute an update query synchronously
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    private static UpdateResult RunUpdateQueryInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                UpdateResult updateResult = new();
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                updateResult.RecordsChanged = cmd.ExecuteNonQuery();
                updateResult.Success = true;
                return updateResult;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            finally
            {
                conn.Close();
            }
        }
        return new();
    }
}
