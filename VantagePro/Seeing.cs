using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using MySql.Data.MySqlClient;

namespace ASCOM.Wise40.VantagePro
{
    public class Seeing
    {
        private static WeatherLogger _weatherLogger;
        private DateTime _lastQueryTime = DateTime.MinValue;
        private TimeSpan _interval = new TimeSpan(0, 1, 0);

        public Seeing() { }

        public static void init()
        {
            _weatherLogger = new WeatherLogger(stationName: "LCO");
        }

        public void Refresh()
        {
            if (_lastQueryTime != DateTime.MinValue && DateTime.Now.Subtract(_lastQueryTime) < _interval)
                return;
            //
            //  mysql -uhibernate -phibernate -hpubsubdb.tlv.lco.gtn hibernate 
            //    -e 'select from_unixtime(L.TIMESTAMP_/1000), L.VALUE_ from LIVEVALUE as L, PROPERTY as P where L.IDENTIFIER=P.IDENTIFIER and P.ADDRESS_DATUM="Reduction Latest FWHM Median" and P.ADDRESS_DATUMINSTANCE=(select L.VALUE_ from LIVEVALUE as L, PROPERTY as P where P.IDENTIFIER=L.IDENTIFIER and P.ADDRESS_DATUM="Guide Selected Autoguider Name" and P.ADDRESS_OBSERVATORY="doma")'
            //
            string sql = "select from_unixtime(L.TIMESTAMP_/1000) as time, L.VALUE_ from LIVEVALUE as L, PROPERTY as P where L.IDENTIFIER=P.IDENTIFIER and P.ADDRESS_DATUM=\"Reduction Latest FWHM Median\" and P.ADDRESS_DATUMINSTANCE=(select L.VALUE_ from LIVEVALUE as L, PROPERTY as P where P.IDENTIFIER=L.IDENTIFIER and P.ADDRESS_DATUM=\"Guide Selected Autoguider Name\" and P.ADDRESS_OBSERVATORY=\"doma\")";
            try
            {
                using (var sqlConn = new MySqlConnection(Const.MySql.DatabaseConnectionString.LCO_hibernate))
                {
                    sqlConn.Open();
                    using (var sqlCmd = new MySqlCommand(sql, sqlConn))
                    {
                        using (var cursor = sqlCmd.ExecuteReader())
                        {
                            cursor.Read();

                            TimeLocal = Convert.ToDateTime(cursor["time"]);

                            if ((string)cursor["VALUE_"] == "NaN")
                            {
                                FWHM = Double.NaN;
                            }
                            else
                            {
                                FWHM = Convert.ToDouble(cursor["VALUE_"]);
                                _weatherLogger.Log(new Dictionary<string, string>()
                                {
                                    ["StarFWHM"] = FWHM.ToString(),
                                }, TimeLocal);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                #region debug
                WiseVantagePro.debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"RefreshSeeing:\nsql: {sql}\nCaught: {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }

            _lastQueryTime = DateTime.Now;
        }

        public TimeSpan TimeSinceLastUpdate
        {
            get
            {
                return DateTime.Now.Subtract(TimeLocal);
            }
        }

        public DateTime TimeLocal { get; set; } = DateTime.MinValue;

        public double FWHM { get; set; } = Double.MinValue;

        //public static DateTime UnixTimeStampToDateTime(DateTime unixTimeStamp)
        //{
        //    // Unix timestamp is seconds past epoch
        //    DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        //    //return (dtDateTime + (unixTimeStamp)).ToLocalTime();
        //    return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp) + 
        //    //return unixTimeStamp.Add(TimeSpan.FromSeconds(unixEpoch.TotalSeconds));
        //}
    }
}
