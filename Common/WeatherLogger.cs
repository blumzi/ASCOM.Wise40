using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using MySql.Data.MySqlClient;
using MySql.Data;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Common
{
    public class WeatherLogger
    {
        private string _stationName;
        private static Debugger debugger = Debugger.Instance;
        private DateTime _lastLoggedTime = DateTime.MinValue;
        private object _lock = new object();

        public WeatherLogger(string stationName)
        {
            _stationName = stationName;

            string sql = $"SELECT time FROM weather WHERE station = '{stationName}' ORDER BY time DESC LIMIT 0 , 1; ";

            try
            {
                using (var sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather))
                {
                    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        using (var cursor = sqlCmd.ExecuteReader())
                        {
                            cursor.Read();

                            _lastLoggedTime = Convert.ToDateTime(cursor["time"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lastLoggedTime = DateTime.MinValue;
            }
        }

        static WeatherLogger() { }

        public void Log(Dictionary<string, string> dict, DateTime date)
        {
            if (!WiseSite.CurrentProcessIsASCOMServer)
                return;

            lock (_lock)
            {
                if (date.CompareTo(_lastLoggedTime) <= 0)
                    return;

                string sql = $"insert into weather.weather (Time, Station, {string.Join(", ", dict.Keys)})" +
                    $" values('{date.ToMySqlDateTime()}', '{_stationName}', {string.Join(", ", dict.Values)})";

                try
                {
                    using (var sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather))
                    {
                        sqlConn.Open();
                        using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                        {
                            sqlCmd.ExecuteNonQuery();
                            _lastLoggedTime = date;
                        }
                    }
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        $"EnvironmentLogger.log: \nsql: {sql}\n Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                }
            }
        }
    }
}
