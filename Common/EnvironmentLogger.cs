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
            string sql = string.Format("insert into weather_events(Time, Station, {0}) values('{1}', '{2}', {3})",
                string.Join(", ", dict.Keys),
                date.ToString(@"yyyy-MM-dd HH:mm:ss.fff"),
                _stationName,
                string.Join(", ", dict.Values));

            try
            {
                MySqlConnection sqlConn = new MySqlConnection("server=localhost;user=root;database=weather_events;port=3306;password=@!ab4131!@");
                sqlConn.Open();
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConn);
                sqlCmd.ExecuteNonQuery();
                sqlCmd.Dispose();
                sqlConn.Close();
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "EnvironmentLogger.log: cmd.ExecuteNonQuery failed: {0}", ex.StackTrace);
                #endregion
            }
        }
    }
}
