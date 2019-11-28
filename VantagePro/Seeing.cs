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
        private static WeatherLogger _logger;
        private double _fwhm;
        private DateTime _timeUTC = DateTime.MinValue;

        public Seeing() { }

        public static void init()
        {
            _logger = new WeatherLogger(stationName: "LCO");
        }

        public void Refresh()
        {
            //
            //  mysql -uhibernate -phibernate -hpubsubdb.tlv.lco.gtn hibernate 
            //      -e "select from_unixtime(TIMESTAMP_/1000), VALUE_ from LIVEVALUE  where  IDENTIFIER=5743146590416427613"
            //
            string sql = "select from_unixtime(TIMESTAMP_/1000) as time, VALUE_ from LIVEVALUE  where  IDENTIFIER=5743146590416427613";
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
                            _fwhm = Convert.ToDouble(cursor["VALUE_"]);

                            if ((string)cursor["VALUE_"] == "NaN")
                            {
                                _fwhm = Double.NaN;
                            }
                            else
                            {
                                _fwhm = Convert.ToDouble(cursor["VALUE_"]);
                                _logger.Log(new Dictionary<string, string>()
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
                WiseVantagePro.debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"RefreshSeeing:\nsql: {sql}\nCaught: {ex.Message} at\n{ex.StackTrace}");
                #endregion
            }
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
