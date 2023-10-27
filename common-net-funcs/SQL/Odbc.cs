using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using static Common_Net_Funcs.Tools.DebugHelpers;

namespace Common_Net_Funcs.SQL;

public class UpdateResult
{
    public int RecordsChanged { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Interact with ODBC data sources
/// </summary>
public static class Odbc
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="sql">Select query to retrieve populate datatable</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static async Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using OdbcConnection conn = new(connStr);
        using OdbcCommand cmd = new(sql, conn);
        return await GetDataTableInternal(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    /// <summary>
    /// Returns a DataTable using the SQL and data connection passed to the function
    /// </summary>
    /// <param name="conn">ODBC connection to use</param>
    /// <param name="cmd">Command to use with parameters</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static async Task<DataTable> GetDataTable(OdbcConnection conn, OdbcCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        return await GetDataTableInternal(conn, cmd, commandTimeoutSeconds, maxRetry);
    }

    public static async Task<DataTable> GetDataTableInternal(OdbcConnection conn, OdbcCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        using DataTable dt = new();
        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                cmd.CommandTimeout = commandTimeoutSeconds;
                await conn.OpenAsync();
                using DbDataReader reader = await cmd.ExecuteReaderAsync();
                dt.Load(reader);
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: " + ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <returns>DataTable containing the results of the SQL query</returns>
    public static DataTable GetDataTableSynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            using OdbcConnection conn = new(connStr);
            try
            {
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                using OdbcDataAdapter da = new(cmd);
                using DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            finally
            {
                conn.Close();
            }
        }
        return new();
    }

    /// <summary>
    /// Execute an update query
    /// </summary>
    /// <param name="sql">SQL query to add, delete, or update records</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static async Task<UpdateResult> RunUpdateQuery(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            using OdbcConnection conn = new(connStr);
            try
            {
                UpdateResult updateResult = new();
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                await conn.OpenAsync();
                updateResult.RecordsChanged = await cmd.ExecuteNonQueryAsync();
                updateResult.Success = true;
                return updateResult;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <param name="sql">SQL query to add, delete, or update records</param>
    /// <param name="connStr">Connection string to run the query on</param>
    /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
    /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
    public static UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, int commandTimeoutSeconds = 30, int maxRetry = 3)
    {
        for (int i = 0; i < maxRetry; i++)
        {
            using OdbcConnection conn = new(connStr);
            try
            {
                UpdateResult updateResult = new();
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                updateResult.RecordsChanged = cmd.ExecuteNonQuery();
                updateResult.Success = true;
                return updateResult;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, $"{ex.GetLocationOfEexception()} Error");
            }
            finally
            {
                conn.Close();
            }
        }
        return new();
    }
}
