using System.Data;
using System.Data.Common;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.Sql.Common;

public class UpdateResult
{
    public int RecordsChanged { get; set; }
    public bool Success { get; set; }
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
    public static async Task<DataTable> GetDataTableInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
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
    public static DataTable GetDataTableInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
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
    /// Execute an update query asynchronously
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static Task<UpdateResult> RunUpdateQuery(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return RunUpdateQueryInternal(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Execute an update query asynchronously
    /// </summary>
    /// <param name="conn">Database connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static async Task<UpdateResult> RunUpdateQueryInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
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
    public static UpdateResult RunUpdateQueryInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
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
