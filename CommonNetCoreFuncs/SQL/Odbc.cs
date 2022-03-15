using System;
using System.Data;
using System.Data.Odbc;
using System.Threading.Tasks;
using System.Data.Common;

namespace CommonNetCoreFuncs.SQL
{
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
        public static async Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30)
        {
            try
            {
                using OdbcConnection conn = new(connStr);
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                using DbDataReader reader = await cmd.ExecuteReaderAsync();
                using DataTable dt = new();
                dt.Load(reader);
                conn.Close();
                return dt;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, (ex.InnerException ?? new()).ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: "+ ex, (ex.InnerException ?? new()).ToString());
            }
            return new DataTable();
        }

        /// <summary>
        /// Returns a DataTable using the SQL and data connection passed to the function
        /// </summary>
        /// <param name="conn">ODBC connection to use</param>
        /// <param name="cmd">Command to use with parameters</param>
        /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
        /// <returns>DataTable containing the results of the SQL query</returns>
        public static async Task<DataTable> GetDataTableWithParms(OdbcConnection conn, OdbcCommand cmd, int commandTimeoutSeconds = 30)
        {
            try
            {
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                using DbDataReader reader = await cmd.ExecuteReaderAsync();
                using DataTable dt = new();
                dt.Load(reader);
                conn.Close();
                return dt;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, (ex.InnerException ?? new()).ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: "+ ex, (ex.InnerException ?? new()).ToString());
            }
            return new DataTable();
        }

        /// <summary>
        /// Returns a DataTable using the SQL and data connection passed to the function
        /// </summary>
        /// <param name="sql">Select query to retrieve populate datatable</param>
        /// <param name="connStr">Connection string to run the query on</param>
        /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
        /// <returns>DataTable containing the results of the SQL query</returns>
        public static DataTable GetDataTableSynchronous(string sql, string connStr, int commandTimeoutSeconds = 30)
        {
            try
            {
                using OdbcConnection conn = new(connStr);
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                using OdbcDataAdapter da = new(cmd);
                using DataTable dt = new();
                da.Fill(dt);
                conn.Close();
                return dt;
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, (ex.InnerException ?? new()).ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Error getting datatable: " + ex, (ex.InnerException ?? new()).ToString());
            }
            return new DataTable();
        }

        /// <summary>
        /// Execute an update query
        /// </summary>
        /// <param name="sql">SQL query to add, delete, or update records</param>
        /// <param name="connStr">Connection string to run the query on</param>
        /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
        /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
        public static async Task<UpdateResult> RunUpdateQuery(string sql, string connStr, int commandTimeoutSeconds = 30)
        {
            UpdateResult updateResult = new();
            try
            {
                using OdbcConnection conn = new(connStr);
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                updateResult.RecordsChanged = await cmd.ExecuteNonQueryAsync();
                updateResult.Success = true; 
                conn.Close();
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, (ex.InnerException ?? new()).ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, (ex.InnerException ?? new()).ToString());
            }
            return updateResult;
        }

        /// <summary>
        /// Execute an update query synchronously
        /// </summary>
        /// <param name="sql">SQL query to add, delete, or update records</param>
        /// <param name="connStr">Connection string to run the query on</param>
        /// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
        /// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
        public static UpdateResult RunUpdateQuerySynchronous(string sql, string connStr, int commandTimeoutSeconds = 30)
        {
            UpdateResult updateResult = new();
            try
            {
                using OdbcConnection conn = new(connStr);
                using OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                updateResult.RecordsChanged = cmd.ExecuteNonQuery();
                updateResult.Success = true;
                conn.Close();
            }
            catch (DbException ex)
            {
                logger.Error("DB Error: " + ex, (ex.InnerException ?? new()).ToString());
            }
            catch (Exception ex)
            {
                logger.Error("Error executing update query: " + ex, (ex.InnerException ?? new()).ToString());
            }
            return updateResult;
        }
    }
}