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
        private double _fwhm = Double.NaN;
        private DateTime _timeUTC = DateTime.MinValue;
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

                            _timeUTC = Convert.ToDateTime(cursor["time"]);

                            if ((string)cursor["VALUE_"] == "NaN")
                            {
                                _fwhm = Double.NaN;
                            }
                            else
                            {
                                _fwhm = Convert.ToDouble(cursor["VALUE_"]);
                                _weatherLogger.Log(new Dictionary<string, string>()
                                {
                                    ["StarFWHM"] = _fwhm.ToString(),
                                }, _timeUTC);
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
                return DateTime.UtcNow.Subtract(TimeUTC);
            }
        }

        public DateTime TimeUTC
        {
            get
            {
                return _timeUTC;
            }

            set
            {
                _timeUTC = value;
            }
        }

        public double FWHM
        {
            get
            {
                return _fwhm;
            }

            set
            {
                _fwhm = value;
            }
        }
    }
}
