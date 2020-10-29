using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Text;

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
                OdbcConnection conn = new OdbcConnection(connStr);
                OdbcCommand cmd = new OdbcCommand(sql, conn);
                conn.Open();
                OdbcDataAdapter da = new OdbcDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                conn.Close();
                da.Dispose();
                return dt;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting data table");
                return null;
            }

        }
        public static void RunUpdateQuery(string sql, string connStr)
        {
            OdbcConnection conn = new OdbcConnection(connStr);
            OdbcCommand cmd = new OdbcCommand(sql, conn);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}
