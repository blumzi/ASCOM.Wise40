using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using MySql.Data;
using MySql.Data.MySqlClient;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{
    public class EnvironmentLogger
    {
        private string _stationName;
        private static Debugger debugger = Debugger.Instance;

        public EnvironmentLogger(string stationName)
        {
            _stationName = stationName;
        }

        static EnvironmentLogger() { }

        public void Log(Dictionary<string, string> dict, DateTime date)
        {
            string sql = string.Format("insert into weather(Time, Station, {0}) values('{1}', '{2}', {3})",
                string.Join(", ", dict.Keys),
                date.ToString(@"yyyy-MM-dd HH:mm:ss.fff"),
                _stationName,
                string.Join(", ", dict.Values));

            try
            {
                using (var sqlConn = new MySqlConnection(ActivityMonitor.Tracer.MySqlActivitiesConnectionString))
                {
                    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        sqlCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "EnvironmentLogger.log: \nsql: {0}\n failed: {1}", sql, ex.StackTrace);
                #endregion
            }
        }
    }
}
