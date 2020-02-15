using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM.Wise40.Common;
using System.Text.RegularExpressions;

namespace ASCOM.Wise40SafeToOperate
{
    public class OWLRefresher : Sensor
    {
        private static string OWLDir = Const.topWise40Directory + "/Weather/OWL/";
        private static string AWSFile = OWLDir + "AWS.txt";
        private static string STWMFile = OWLDir + "STWM.txt";
        private static DateTime lastAWSReadTime = DateTime.MinValue;
        private static DateTime lastSTWMReadTime = DateTime.MinValue;
        private static TimeSpan _interval = new TimeSpan(0, 1, 0);

        public class Station
        {
            public WeatherLogger _weatherLogger;
            public Dictionary<string, double> _sensorData;
            public DateTime _dateUtc;
        }

        public static Dictionary<string, Station> stations = new Dictionary<string, Station>()
        {
            { "AWS_22", new Station() },
            { "WDS_1", new Station() }, { "WDS_2", new Station() }, { "WDS_3", new Station() },
            { "CLS_10", new Station() }, { "CLS_11", new Station() }, { "CLS_12", new Station() },
            { "THS_4", new Station() }, { "THS_5", new Station() }, { "THS_6", new Station() }, { "THS_7", new Station() }, { "THS_8", new Station() },
        };        

        public OWLRefresher(WiseSafeToOperate instance) :
            base("OWLRefresher",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "", "", "", "",
                instance)
        {
            foreach (var s in stations.Keys)
            {
                stations[s]._weatherLogger = new WeatherLogger(s);
                stations[s]._sensorData = new Dictionary<string, double>();
                stations[s]._dateUtc = DateTime.MinValue;
            }
        }

        public override string reason()
        {
            return string.Empty;
        }

        private void parseAWS()
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

                            if (station._weatherLogger != null)
                            {
                                station._weatherLogger.Log(new Dictionary<string, string>()
                                {
                                    ["Temperature"] = station._sensorData["temperature"].ToString(),
                                    ["Humidity"] = station._sensorData["humidity"].ToString(),
                                    ["WindSpeed"] = station._sensorData["windSpeed"].ToString(),
                                    ["WindDir"] = station._sensorData["windDir"].ToString(),

                                }, station._dateUtc);
                            }

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

        private void parseSTWM()
        {
            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(STWMFile);

            if (lastSTWMReadTime == DateTime.MinValue || lastWriteTime.CompareTo(lastSTWMReadTime) > 0)
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(STWMFile))
                {
                    try
                    {
                        // 2019-11-04 05:46:37
                        // STWM
                        // WDS_1 - W/S(min): 0.8, W/S(avg): 1.6, W/S(max): 2.7, W/D(min): 11, W/D(avg): 36, W/D(max): 60
                        // WDS_2 - W/S(min): 0.8, W/S(avg): 1.4, W/S(max): 2.6, W/D(min): 13, W/D(avg): 38, W/D(max): 69
                        // WDS_3 - W/S(min): 0.9, W/S(avg): 2.2, W/S(max): 3.1, W/D(min): 13, W/D(avg): 32, W/D(max): 74
                        // THS_4 - Temp.: 18.2, Hum.: 55.5
                        // THS_5 - Temp.: 18.1, Hum.: 56.3
                        // THS_6 - Temp.: 17.8, Hum.: 55.5
                        // CLS_10 - Cloud: -39.1
                        // CLS_11 - Cloud: -40.29
                        // CLS_12 - Cloud: -38.29
                        // THS_7 - Temp.: 17.8, Hum.: 62.7
                        // THS_8 - Temp.: 18.2, Hum.: 61.7


                        string content = sr.ReadToEnd().Replace("\0", string.Empty);
                        Regex r = new Regex(@"(?<dateUtc>[\d\-]+\s[\d:]+).*" +
                                        @"WDS_1.*W/S\(avg\):\s+(?<wds1_windSpeed>[\d.]+),.*W/D\(avg\):\s+(?<wds1_windDir>[\d.]+).*" +
                                        @"WDS_2.*W/S\(avg\):\s+(?<wds2_windSpeed>[\d.]+),.*W/D\(avg\):\s+(?<wds2_windDir>[\d.]+).*" +
                                        @"WDS_3.*W/S\(avg\):\s+(?<wds3_windSpeed>[\d.]+),.*W/D\(avg\):\s+(?<wds3_windDir>[\d.]+).*" +
                                        @"THS_4\s+-\s+Temp\.:\s+(?<ths4_temperature>[\d.-]+),\s+Hum\.:\s+(?<ths4_humidity>[\d.]+).*" +
                                        @"THS_5\s+-\s+Temp\.:\s+(?<ths5_temperature>[\d.-]+),\s+Hum\.:\s+(?<ths5_humidity>[\d.]+).*" +
                                        @"THS_6\s+-\s+Temp\.:\s+(?<ths6_temperature>[\d.-]+),\s+Hum\.:\s+(?<ths6_humidity>[\d.]+).*" +
                                        @"CLS_10\s+-\s+Cloud:\s+(?<cls10_skyAmbientTemp>[\d.-]+).*" +
                                        @"CLS_11\s+-\s+Cloud:\s+(?<cls11_skyAmbientTemp>[\d.-]+).*" +
                                        @"CLS_12\s+-\s+Cloud:\s+(?<cls12_skyAmbientTemp>[\d.-]+).*" +
                                        @"THS_7\s+-\s+Temp\.:\s+(?<ths7_temperature>[\d.-]+),\s+Hum\.:\s+(?<ths7_humidity>[\d.]+).*" +
                                        @"THS_8\s+-\s+Temp\.:\s+(?<ths8_temperature>[\d.-]+),\s+Hum\.:\s+(?<ths8_humidity>[\d.]+).*");

                        Match m = r.Match(content);
                        if (m.Success)
                        {
                            DateTime time = Convert.ToDateTime(m.Result("${dateUtc}") + " Z");
                            Station station;

                            foreach (string i in new List<string> { "1", "2", "3" }) {
                                station = stations["WDS_" + i];

                                station._sensorData["windSpeed"] = Convert.ToDouble(m.Result("${wds" + i + "_windSpeed}"));
                                station._sensorData["windDir"] = Convert.ToDouble(m.Result("${wds" + i + "_windDir}"));
                                station._weatherLogger.Log(new Dictionary<string, string>
                                {
                                    ["WindSpeed"] = station._sensorData["windSpeed"].ToString(),
                                    ["WindDir"] = station._sensorData["windDir"].ToString(),
                                }, time);
                            }

                            foreach (string i in new List<string> { "4", "5", "6", "7", "8" })
                            {
                                station = stations["THS_" + i];

                                station._sensorData["temperature"] = Convert.ToDouble(m.Result("${ths" + i + "_temperature}"));
                                station._sensorData["humidity"] = Convert.ToDouble(m.Result("${ths" + i + "_humidity}"));
                                station._weatherLogger.Log(new Dictionary<string, string>
                                {
                                    ["Temperature"] = station._sensorData["temperature"].ToString(),
                                    ["Humidity"] = station._sensorData["humidity"].ToString(),
                                }, time);
                            }

                            foreach (string i in new List<string> { "10", "11", "12" })
                            {
                                station = stations["CLS_" + i];

                                station._sensorData["skyAmbientTemp"] = Convert.ToDouble(m.Result("${cls" + i + "_skyAmbientTemp}"));
                                station._weatherLogger.Log(new Dictionary<string, string>
                                {
                                    ["SkyAmbientTemp"] = station._sensorData["skyAmbientTemp"].ToString(),
                                }, time);
                            }

                            #region debug
                            //debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                            //    $"STWM: content: [{content}]");
                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                            $"OWLRefresher:getReading: Could not read {STWMFile}:\nCaught {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        return;
                    }
                }
                lastSTWMReadTime = lastWriteTime;
            }
        }
        public override Reading getReading()
        {
            parseAWS();
            parseSTWM();
            return null;
        }

        public override object Digest()
        {
            return new OWLDigest();
        }

        public override string MaxAsString
        {
            get { return ""; }
            set { }
        }

        public override void writeSensorProfile() { }
        public override void readSensorProfile() { }

        public override string Status
        {
            get
            {
                return "";
            }
        }

        public class WDSDigest
        {
            public string Name;
            public double WindSpeed;
            public double WindDir;

        }

        public class THSDigest
        {
            public string Name;
            public double Temperature;
            public double Humidity;
        }

        public class CLSDigest
        {
            public string Name;
            public double SkyAmbientTemp;
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
            public List<WDSDigest> WDS;
            public List<THSDigest> THS;
            public List<CLSDigest> CLS;

            public OWLDigest()
            {
                string name;
                Station station;

                Vendor = "Korea Astronomy & Space Science Institute";
                Model = "Optical Wide-field patroL (OWL)";
                UpdatedAtUT = stations["AWS_22"]._dateUtc;
                AgeInSeconds = (DateTime.UtcNow - UpdatedAtUT).TotalSeconds;
                WDS = new List<WDSDigest>();
                foreach (var s in new List<string> { "1", "2", "3" }) {
                    name = "WDS_" + s;
                    station = stations[name];
                    if (station._sensorData.ContainsKey("windSpeed") && station._sensorData.ContainsKey("windDir"))
                        WDS.Add(new WDSDigest
                        {
                            Name = name,
                            WindSpeed = station._sensorData["windSpeed"],
                            WindDir = station._sensorData["windDir"],
                        });
                }

                THS = new List<THSDigest>();
                foreach (var s in new List<string> { "4", "5", "6", "7", "8" })
                {
                    name = "THS_" + s;
                    station = stations[name];
                    if (station._sensorData.ContainsKey("temperature") && station._sensorData.ContainsKey("humidity"))
                        THS.Add(new THSDigest
                        {
                            Name = name,
                            Temperature = station._sensorData["temperature"],
                            Humidity = station._sensorData["humidity"],
                        });
                }

                CLS = new List<CLSDigest>();
                foreach (var s in new List<string> { "10", "11", "12" })
                {
                    name = "CLS_" + s;
                    station = stations[name];
                    if (station._sensorData.ContainsKey("skyAmbientTemp"))
                        CLS.Add(new CLSDigest
                        {
                            Name = name,
                            SkyAmbientTemp = station._sensorData["skyAmbientTemp"],
                        });
                }

                name = "AWS_22";
                station = stations[name];
                if (station._sensorData.ContainsKey("temperature") && station._sensorData.ContainsKey("humidity") &&
                    station._sensorData.ContainsKey("windSpeed") && station._sensorData.ContainsKey("windDir"))
                        AWS = new AWSDigest {
                            Name = name,
                            Temperature = station._sensorData["temperature"],
                            Humidity = station._sensorData["humidity"],
                            WindSpeed = station._sensorData["windSpeed"],
                            WindDir = station._sensorData["windDir"],
                        };
            }
        };
    }
}
