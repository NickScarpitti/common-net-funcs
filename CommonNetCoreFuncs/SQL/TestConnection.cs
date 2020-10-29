using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace CommonNetCoreFuncs.SQL
{
    public static class TestConnection
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //TODO:: Figure out a way to quickly test if connection is available
        public static bool IsConnectionActive(this string conStr)
        {
            using (SqlConnection con = new SqlConnection(conStr))
            {
                try
                {
                    con.Open();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
