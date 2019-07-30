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
            //return;

            string sql = string.Format("insert into weather(Time, Station, {0}) values('{1}', '{2}', {3})",
                string.Join(", ", dict.Keys),
                date.ToString(@"yyyy-MM-dd HH:mm:ss.fff"),
                _stationName,
                string.Join(", ", dict.Values));

            try
            {
                MySqlConnection sqlConn = new MySqlConnection("server=localhost;user=root;database=weather;port=3306;password=@!ab4131!@");
                sqlConn.Open();
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConn);
                sqlCmd.ExecuteNonQuery();
                sqlCmd.Dispose();
                sqlConn.Close();
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
