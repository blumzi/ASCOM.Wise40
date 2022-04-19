using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using ASCOM.Wise40.Common;
using Newtonsoft.Json;

namespace ASCOM.Wise40SafeToOperate
{
    public class CloudNetRawData
    {
        public string Name = "CloudNet";
        public string Vendor = "Haifa University";
        public string Model = "CloudNet";
        public DateTime UpdatedAtUT;
        public double AgeInSeconds;
        public Dictionary<string, string> SensorData;
    }

    public class CloudNetResult
    {
        public string timeStamp;
        public bool safeToOperate;
    }

    public class CloudNetSensor : Sensor
    {
        private static PeriodicHttpFetcher periodicHttpFetcher;
        private readonly Dictionary<string, string> sensorData = new Dictionary<string, string>();
        public WeatherLogger _weatherLogger;
        public DateTime _lastFetch = DateTime.MinValue, updatedAtUT, lastUpdatedAtUT = DateTime.MinValue;

        public CloudNetSensor(WiseSafeToOperate instance) :
            base("CloudNet",
                Attribute.Periodic |
                Attribute.ForInfoOnly |
                Attribute.SingleReading |
                Attribute.AlwaysEnabled,
                "", "", "", "",
                instance)
        {
            periodicHttpFetcher = new PeriodicHttpFetcher(
                    "CloudNetSensor",
                    "http://132.66.65.6:9988/Cloudnet",
                    period: TimeSpan.FromMinutes(1),
                    maxAgeMillis: 90*1000);
            _weatherLogger = new WeatherLogger("CloudNet");
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
                string json;
                bool safe;

                try
                {
                    json = periodicHttpFetcher.Result.Replace("'", "");
                    // NOTE: Do not print the value of json, it contains double quotes
                    CloudNetResult result = JsonConvert.DeserializeObject<CloudNetResult>(json);
                    try
                    {
                        updatedAtUT = Convert.ToDateTime(DateTime.ParseExact(result.timeStamp, "dd-MM-yyyy, HH:mm:ss", CultureInfo.InvariantCulture));
                    }
                    catch (FormatException ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $@"CloudNetSensor.GetReading: result.timeStamp: {result.timeStamp}, Caught: DateTime.FormatException at\n{ex.StackTrace}");
                        #endregion
                        return null;
                    }
                    safe = Convert.ToBoolean(result.safeToOperate);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"CloudNetSensor.GetReading: result: timeStamp: {result.timeStamp}, safeToOperate: {result.safeToOperate}");
                    #endregion
                    if (updatedAtUT > lastUpdatedAtUT)
                    {
                        sensorData["CloudCover"] = safe ? "0" : "1";
                        _weatherLogger?.Log(new Dictionary<string, string>()
                        {
                            ["CloudCover"] = (safe ? 20 : 80).ToString(),
                        }, updatedAtUT.ToLocalTime());
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"CloudNetSensor.GetReading: logged data for {updatedAtUT}");
                        #endregion
                        lastUpdatedAtUT = updatedAtUT;
                    }
                    _lastFetch = lastFetch;
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $@"CloudNetSensor.GetReading: Caught: {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    return null;
                }
            }
            return null;
        }

        public override object Digest()
        {
            return new CloudNetRawData()
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
