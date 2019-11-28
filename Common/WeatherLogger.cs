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

            string sql = $"SELECT time FROM weather WHERE station = '{stationName}' ORDER BY time DESC LIMIT 0 , 1; ";

            try
            {
                //return;

                //using (var sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather))
                //{
                //    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, _sqlConn))
                    {
                        using (var cursor = sqlCmd.ExecuteReader())
                        {
                            cursor.Read();

                            _lastLoggedTime = Convert.ToDateTime(cursor["time"]);
                        }
                    //}
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
            try
            {
                _sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather);
                _sqlConn.Open();
            } catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"static WeatherLogger: Caught {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
        }

        public void Dispose() {
            _sqlConn.Close();
            _sqlConn.Dispose();
        }

        public void Log(Dictionary<string, string> dict, DateTime time)
        {
            //return;

            if (!(WiseSite.CurrentProcessIs(Const.Application.RESTServer) ||
                    WiseSite.CurrentProcessIs(Const.Application.OCH)))
                return;

            if (time.CompareTo(_lastLoggedTime) <= 0)
                return;

            lock (_lock)
            {
                string sql = $"insert into weather.weather (time, Station, {string.Join(", ", dict.Keys)})" +
                    $" values(TIMESTAMP('{time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}'), '{_stationName}', {string.Join(", ", dict.Values)})";
                //$" values(TIMESTAMP(CONVERT_TZ('{time}', '+00:00', @@global.time_zone)), '{_stationName}', {string.Join(", ", dict.Values)})";

                try
                {
                    using (var sqlCmd = new MySqlCommand(sql, _sqlConn))
                    {
                        sqlCmd.ExecuteNonQuery();
                        _lastLoggedTime = time;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"WeatherLogger.Log({_stationName}): _lastLoggedTime: {_lastLoggedTime}");
                        #endregion
                    }
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
    }
}
