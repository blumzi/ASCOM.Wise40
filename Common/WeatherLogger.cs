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
        private readonly string _stationName;
        private static readonly Debugger debugger = Debugger.Instance;
        private DateTime prevLocalLoggedTime = DateTime.MinValue;           // local time
        private readonly object _lock = new object();

        public WeatherLogger(string stationName)
        {
            _stationName = stationName;

            string sql = $"SELECT time FROM weather WHERE station = '{stationName}' ORDER BY time DESC LIMIT 0 , 1; ";

            try
            {
                using (var _sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather))
                {
                    _sqlConn.Open();
                    #pragma warning disable CA2100
                    using (var sqlCmd = new MySqlCommand(sql, _sqlConn))
                    #pragma warning restore CA2100
                    {
                        using (var cursor = sqlCmd.ExecuteReader())
                        {
                            cursor.Read();

                            prevLocalLoggedTime = Convert.ToDateTime(cursor["time"]).ToLocalTime();
                        }
                    }
                }
            }
            catch
            {
                prevLocalLoggedTime = DateTime.MinValue;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"WeatherLogger({stationName}): .const:prevLoggedLocalTime: {prevLocalLoggedTime:yyyy-MM-dd HH:mm:ss.fff}");
            #endregion
        }

        static WeatherLogger() { }

        public void Log(Dictionary<string, string> dict, DateTime currentLocalLoggedTime)
        {
            if (!(WiseSite.CurrentProcessIs(Const.Application.RESTServer) ||
                    WiseSite.CurrentProcessIs(Const.Application.OCH)))
            {
                return;
            }

            if (currentLocalLoggedTime.CompareTo(prevLocalLoggedTime) <= 0)
                return;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{_stationName}: curr: {currentLocalLoggedTime} > prev: {prevLocalLoggedTime}");
            #endregion

            lock (_lock)
            {
                string sql = $"insert into weather (time, Station, {string.Join(", ", dict.Keys)})" +
                    $" values(TIMESTAMP('{currentLocalLoggedTime.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fff}'), '{_stationName}', {string.Join(", ", dict.Values)})";

                try
                {
                    using (var _sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.Wise_weather))
                    {
                        _sqlConn.Open();
#pragma warning disable CA2100
                        using (var sqlCmd = new MySqlCommand(sql, _sqlConn))
#pragma warning restore CA2100
                        {
                            sqlCmd.ExecuteNonQuery();
                        }
                        prevLocalLoggedTime = currentLocalLoggedTime;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            $"WeatherLogger.Log({_stationName}): prevLoggedTime: {prevLocalLoggedTime:yyyy-MM-dd HH:mm:ss.fff}");
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
