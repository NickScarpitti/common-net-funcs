using System;
using System.Data;
using System.Data.Odbc;
using System.Threading.Tasks;
using System.Data.Common;
namespace CommonNetCoreFuncs.SQL
{
    /// <summary>
    /// Interact with ODBC data sources
    /// </summary>
    public static class Odbc
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Returns a DataTable using the SQL and data connection passed to the function
        /// </summary>
        /// <param name="sql">Select query to retrieve populate datatable</param>
        /// <param name="connStr">Connection string to run the query on</param>
        /// <returns>DataTable containing the results of the SQL query</returns>
        public static async Task<DataTable> GetDataTable(string sql, string connStr, int commandTimeoutSeconds = 30, bool showConnectionError = false)
        {
            try
            {
                OdbcConnection conn = new(connStr);
                OdbcCommand cmd = new(sql, conn);
                cmd.CommandTimeout = commandTimeoutSeconds;
                conn.Open();
                DbDataReader reader = await cmd.ExecuteReaderAsync();
                DataTable dt = new DataTable();
                dt.Load(reader);

                //Synchronous method for reference
                //OdbcDataAdapter da = new(cmd);
                //DataTable dt = new();
                //da.Fill(dt);
                //conn.Close();
                //da.Dispose();
                return dt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting data table");
            }
            return new DataTable();
        }

        /// <summary>
        /// Execute an update query
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="connStr"></param>
        public static bool RunUpdateQuery(string sql, string connStr)
        {
            bool success = false;
            try
            {
                OdbcConnection conn = new(connStr);
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing update query");
                success = false;
            }
            return success;
        }
    }
}