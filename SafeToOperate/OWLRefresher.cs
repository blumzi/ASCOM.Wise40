using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Text.RegularExpressions;
using ASCOM.Utilities;

namespace ASCOM.Wise40SafeToOperate
{
    public class OWLRefresher : Sensor
    {
        private const string OWLDir = Const.topWise40Directory + "/Weather/OWL/";
        private const string AWSFile = OWLDir + "AWS.txt";
        private static DateTime lastAWSReadTime = DateTime.MinValue;

        public class Station
        {
            public WeatherLogger _weatherLogger;
            public Dictionary<string, double> _sensorData;
            public DateTime _dateUtc;
        }

        //
        // Only the AWS remains.  The STWM stations - WDS_1..3, THS_4..8, CLS_10..12 -
        //  were dropped on 2026-08-15.  Their readings had become nonsense (THS_5
        //  reporting 700C and 110% humidity, all three CLS pinned at exactly -59.9)
        //  and each one of them cost some 28 seconds of every startup, looking up
        //  its last logged time in a weather table nothing had written for it since
        //  November 2023.  Eleven stations, five and a quarter minutes.
        //
        // STWM.txt is still being written by whatever writes it.  We simply no
        //  longer read it.
        //
        public static Dictionary<string, Station> stations = new Dictionary<string, Station>()
        {
            { "AWS_22", new Station() },
        };

        public OWLRefresher(WiseSafeToOperate instance) :
            base("OWLRefresher",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading,
                "", "", "", "",
                instance)
        {
            ReadSensorProfile();

            foreach (var s in stations.Keys)
            {
                stations[s]._weatherLogger = new WeatherLogger(s);
                stations[s]._sensorData = new Dictionary<string, double>();
                stations[s]._dateUtc = DateTime.MinValue;
            }
        }

        public override string UnsafeReason()
        {
            return string.Empty;
        }

        private void ParseAWS()
        {
            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(AWSFile);

            if (lastAWSReadTime == DateTime.MinValue || lastWriteTime.CompareTo(lastAWSReadTime) > 0)
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(AWSFile))
                {
                    try
                    {
                        // 2019-11-04 06:29:51 | AWS  | AWS_22 - Temp.: 19, Hum.: 48, W/S(min): 1.7, W/S(avg): 2.1, W/S(max): 2.9, W/D(min): 64, W/D(avg): 84, W/D(max): 95

                        string content = sr.ReadToEnd().Replace("\0", string.Empty);
                        Regex r = new Regex(@"(?<dateUtc>[\d\-]+\s[\d:]+).*" +
                                        @"(?<station>AWS_[\d]+) - " +
                                        @"Temp\.:\s+(?<temperature>[\d.]+),\s+" +
                                        @"Hum\.:\s+(?<humidity>[\d.]+),.*" +
                                        @"W/S\(avg\):\s+(?<windSpeed>[\d.]+),.*" +
                                        @"W/D\(avg\):\s+(?<windDir>[\d.]+),");
                        Match m = r.Match(content);
                        if (m.Success)
                        {
                            Station station = stations[m.Result("${station}")];

                            station._dateUtc = Convert.ToDateTime(m.Result("${dateUtc}" + " Z"));
                            station._sensorData["temperature"] = Convert.ToDouble(m.Result("${temperature}"));
                            station._sensorData["humidity"] = Convert.ToDouble(m.Result("${humidity}"));
                            station._sensorData["windSpeed"] = Convert.ToDouble(m.Result("${windSpeed}"));
                            station._sensorData["windDir"] = Convert.ToDouble(m.Result("${windDir}"));

                            station._weatherLogger?.Log(new Dictionary<string, string>()
                                {
                                    ["Temperature"] = station._sensorData["temperature"].ToString(),
                                    ["Humidity"] = station._sensorData["humidity"].ToString(),
                                    ["WindSpeed"] = station._sensorData["windSpeed"].ToString(),
                                    ["WindDir"] = station._sensorData["windDir"].ToString(),
                                }, station._dateUtc.ToLocalTime());

                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                                $"AWS: content: [{content}]");
                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                            $"OWLRefresher:getReading: Could not read {AWSFile}:\nCaught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        return;
                    }
                }
                lastAWSReadTime = lastWriteTime;
            }
        }

        public override Reading GetReading()
        {
            if (Enabled) {
                ParseAWS();
            }
            return null;
        }

        public override object Digest()
        {
            if (Enabled)
                return new OWLDigest();
            return null;
        }

        public override string MaxAsString
        {
            get { return ""; }
            set { }
        }

        public override void WriteSensorProfile() {
            // Same argument-order slip as ARDO had - see the note there.  The value is written
            //  through Enabled.ToString() like every other sensor rather than a hand-rolled
            //  "true"/"false", since the store now holds a real JSON boolean either way.
            wisesafetooperate._profile.WriteValue(Const.WiseDriverID.SafeToOperate, "Enabled", Enabled.ToString(), "OWLRefresher");
        }
        public override void ReadSensorProfile() {
            Enabled = Convert.ToBoolean(wisesafetooperate._profile.GetValue(Const.WiseDriverID.SafeToOperate, "Enabled", "OWLRefresher", true.ToString()));
        }

        public override string Status
        {
            get
            {
                return "";
            }
        }

        public class AWSDigest
        {
            public string Name;
            public double Temperature;
            public double Humidity;
            public double WindSpeed;
            public double WindDir;
        }

        public class OWLDigest {
            public string Vendor;
            public string Model;
            public DateTime UpdatedAtUT;
            public double AgeInSeconds;
            public AWSDigest AWS;

            public OWLDigest()
            {
                string name;
                Station station;

                Vendor = "Korea Astronomy & Space Science Institute";
                Model = "Optical Wide-field patroL (OWL)";
                UpdatedAtUT = stations["AWS_22"]._dateUtc;
                AgeInSeconds = (DateTime.UtcNow - UpdatedAtUT).TotalSeconds;

                name = "AWS_22";
                station = stations[name];
                if (station._sensorData.ContainsKey("temperature") && station._sensorData.ContainsKey("humidity") &&
                    station._sensorData.ContainsKey("windSpeed") && station._sensorData.ContainsKey("windDir"))
                {
                    AWS = new AWSDigest
                    {
                        Name = name,
                        Temperature = station._sensorData["temperature"],
                        Humidity = station._sensorData["humidity"],
                        WindSpeed = station._sensorData["windSpeed"],
                        WindDir = station._sensorData["windDir"],
                    };
                }
            }
        };
    }
}
