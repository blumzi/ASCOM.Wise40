using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using MySql.Data.MySqlClient;
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

            string sql = $"SELECT time FROM weather WHERE station = '{_stationName}' ORDER BY time DESC LIMIT 0 , 1; ";

            //
            // What this reports is worth being careful about.  It used to swallow
            //  every exception into DateTime.MinValue and log only the date, so a
            //  station with no rows, a database that was down, and a query that
            //  timed out all looked identical - and one of them was really
            //  happening.  Until 2026-08-15 the weather table had no index on
            //  Station, so this query was a backward scan of 36 million rows and
            //  took some 28 seconds per station, sometimes tipping over its own
            //  timeout.  Nobody could tell, because the failure looked exactly like
            //  an idle sensor.
            //
            // So: distinguish "no rows" from "it went wrong", say which, and say
            //  how long it took.
            //
            string outcome;
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

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
                            if (cursor.Read())
                            {
                                prevLocalLoggedTime = Convert.ToDateTime(cursor["time"]).ToLocalTime();
                                outcome = $"prevLoggedLocalTime: {prevLocalLoggedTime:yyyy-MM-dd HH:mm:ss.fff}";
                            }
                            else
                            {
                                //  No rows for this station.  Not an error - it has
                                //   simply never been logged.
                                prevLocalLoggedTime = DateTime.MinValue;
                                outcome = "no rows for this station yet";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                prevLocalLoggedTime = DateTime.MinValue;
                outcome = $"FAILED: {ex.Message}";
            }
            stopwatch.Stop();

#region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"WeatherLogger({_stationName}): .const: {outcome} (took {stopwatch.ElapsedMilliseconds}ms)");
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
