using System;
using System.Collections.Generic;
using System.Linq;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40SafeToOperate
{
    public class ARDORawData
    {
        public string Name = "ARDO";
        public string Vendor = "Lunatico Astronomia";
        public string Model = "AAG Cloudwatcher";
        public DateTime UpdatedAtUT;
        public double AgeInSeconds;
        public Dictionary<string, string> SensorData;
    }

    public class ARDOSensor : Sensor
    {
        private static PeriodicHttpFetcher periodicHttpFetcher;
        private readonly Dictionary<string, string> sensorData = new Dictionary<string, string>();
        public WeatherLogger _weatherLogger;
        public DateTime _lastFetch = DateTime.MinValue, updatedAtUT, lastUpdatedAtUT = DateTime.MinValue;

        public ARDOSensor(WiseSafeToOperate instance) :
            base("ARDO",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "", "", "", "",
                instance)
        {
            periodicHttpFetcher = new PeriodicHttpFetcher(
                    "ARDOSensor",
                    "http://2.55.90.188:10500/cgi-bin/cgiLastData",
                    period: TimeSpan.FromMinutes(1),
                    maxAgeMillis: 90*1000);
            _weatherLogger = new WeatherLogger("ARDO");
        }

        public override string UnsafeReason()
        {
            return string.Empty;
        }

        public override Reading GetReading()
        {
            DateTime lastFetch = periodicHttpFetcher.LastSuccess;

            if (lastFetch > _lastFetch)
            {
                try
                {
                    string result = periodicHttpFetcher.Result;

                    foreach (string line in result.Split('\n').ToList())
                    {
                        List<string> words = line.Split('=').ToList();
                        if (words.Count == 2)
                            sensorData[words[0]] = words[1];
                    }

                    DateTime.TryParse(sensorData["dataGMTTime"] + "Z", out updatedAtUT);
                    if (updatedAtUT > lastUpdatedAtUT)
                    {
                        _weatherLogger?.Log(new Dictionary<string, string>()
                        {
                            ["Temperature"] = sensorData["temp"],
                            ["SkyAmbientTemp"] = sensorData["clouds"],
                            ["Humidity"] = sensorData["hum"],
                            ["DewPoint"] = sensorData["dewp"],
                            ["WindSpeed"] = sensorData["wind"],
                        }, updatedAtUT.ToLocalTime());
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"ARDOSensor.GetReading: logged data for {updatedAtUT}");
                        #endregion
                        lastUpdatedAtUT = updatedAtUT;
                    }
                    _lastFetch = lastFetch;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public override object Digest()
        {
            return new ARDORawData()
            {
                UpdatedAtUT = updatedAtUT,
                AgeInSeconds = DateTime.Now.Subtract(updatedAtUT).TotalSeconds,
                SensorData = sensorData,
            };
        }

        public override string MaxAsString
        {
            get { return ""; }
            set { }
        }

        public override void WriteSensorProfile() { }
        public override void ReadSensorProfile() { }

        public override string Status
        {
            get
            {
                return "";
            }
        }
    }
}
