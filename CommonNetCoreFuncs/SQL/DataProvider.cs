using System.Data;

//using IBM.Data.Db2;

namespace CommonNetCoreFuncs.SQL
{
    public class DataProvider
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static DataTable GetDataTable(string sql, string connStr, bool showConnectionError = false)
        {
            //try
            //{
            //    DB2Connection conn = new(connStr);
            //    DB2Command cmd = new DB2Command(sql, conn);
            //    DataTable dt = new();
            //    conn.Open();
            //    if (conn.IsOpen)
            //    {
            //        using (DB2DataAdapter da = new(cmd))
            //        {
            //            da.Fill(dt);
            //        }
            //        conn.Close();
            //    }
            //    return dt;
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error(ex, "Error getting data table");
            //}
            return new DataTable();
        }
    }
}