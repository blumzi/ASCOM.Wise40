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
    public class WeatherLogger: IDisposable
    {
        private string _stationName;
        private static Debugger debugger = Debugger.Instance;
        private DateTime _lastLoggedTime = DateTime.MinValue;
        private object _lock = new object();
        private static MySqlConnection _sqlConn;

        public WeatherLogger(string stationName)
        {
            _stationName = stationName;

            if (_sqlConn == null)
            {
                OpenSqlConnection();
            }

            string sql = $"SELECT time FROM weather WHERE station = '{stationName}' ORDER BY time DESC LIMIT 0 , 1; ";

            try
            {
                CheckSqlConnection();
                using (var sqlCmd = new MySqlCommand(sql, _sqlConn))
                {
                    using (var cursor = sqlCmd.ExecuteReader())
                    {
                        cursor.Read();

                        _lastLoggedTime = Convert.ToDateTime(cursor["time"]);
                    }
                }
            }
            catch
            {
                _lastLoggedTime = DateTime.MinValue;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"WeatherLogger({stationName}): _lastLoggedTime: {_lastLoggedTime}");
            #endregion
        }

        static WeatherLogger() {
            OpenSqlConnection();
        }

        public void Dispose() {
            _sqlConn.Close();
            _sqlConn.Dispose();
        }

        public void Log(Dictionary<string, string> dict, DateTime time)
        {
            if (!(WiseSite.CurrentProcessIs(Const.Application.RESTServer) ||
                    WiseSite.CurrentProcessIs(Const.Application.OCH)))
                return;

            if (time.CompareTo(_lastLoggedTime) <= 0)
                return;

            lock (_lock)
            {
                string sql = $"insert into weather (time, Station, {string.Join(", ", dict.Keys)})" +
                    $" values(TIMESTAMP('{time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")}'), '{_stationName}', {string.Join(", ", dict.Values)})";

                try
                {
                    CheckSqlConnection();

                    var sqlCmd = new MySqlCommand(sql, _sqlConn);
                    sqlCmd.ExecuteNonQuery();
                    _lastLoggedTime = time;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        $"WeatherLogger.Log({_stationName}): _lastLoggedTime: {_lastLoggedTime}");
                    #endregion
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        $"WeatherLogger.log: \nsql: {sql}\n Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                }
            }
        }

        static void OpenSqlConnection()
        {
            try
            {
                _sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather);
                _sqlConn.Open();
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"static OpenSqlConnection: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
        }

        /// <summary>
        /// Check thats the current connection works, otherwise it gets closed and re-opened.
        /// </summary>
        static void CheckSqlConnection()
        {
            if (! _sqlConn.Ping())
            {
                _sqlConn.Close();
                OpenSqlConnection();
            }
        }
    }
}
