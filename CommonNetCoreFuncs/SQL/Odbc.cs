using System;
using System.Data;
using System.Data.Odbc;

namespace CommonNetCoreFuncs.SQL
{
    /// <summary>
    /// Interact with ODBC data sources
    /// </summary>
    public static class Odbc
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Returns a datatable using the SQL and data connection passed to the function
        /// </summary>
        /// <param name="sql">Select query to retrieve populate datatable</param>
        /// <param name="connStr">Connection string to run the query on</param>
        /// <returns></returns>
        public static DataTable GetDataTable(string sql, string connStr, bool showConnectionError = false)
        {
            try
            {
                OdbcConnection conn = new(connStr);
                OdbcCommand cmd = new(sql, conn);
                conn.Open();
                OdbcDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                conn.Close();
                da.Dispose();
                return dt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting data table");
            }
            return new DataTable();
        }

        public static void RunUpdateQuery(string sql, string connStr)
        {
            OdbcConnection conn = new(connStr);
            OdbcCommand cmd = new(sql, conn);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}