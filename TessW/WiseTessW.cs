using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ASCOM.Wise40.Common;
using ASCOM.Utilities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ASCOM.Wise40.TessW
{
    public class WiseTessW : WeatherStation
    {
        private static readonly Version version = new Version("0.2");
        public static string driverDescription = string.Format($"ASCOM Wise40.TessW v{version}");

        private readonly Debugger debugger = Debugger.Instance;
        private bool _connected = false;
        private bool _enabled = false;
        private bool _initialized = false;
        public DateTime updatedAtUT;

        public WiseTessW() { }
        static WiseTessW() { }

        private DateTime _lastDataFetch = DateTime.MinValue;
        private readonly Dictionary<string, string> sensorData = new Dictionary<string, string>();

        private PeriodicHttpFetcher periodicHttpFetcher;

        private static readonly Lazy<WiseTessW> lazy = new Lazy<WiseTessW>(() => new WiseTessW()); // Singleton
        private const string defaultIPAddress = "192.168.1.100";

        public static WiseTessW Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        /// <summary>
        /// Forces the driver to immediatley query its attached hardware to refresh sensor
        /// values
        /// </summary>
        public void Refresh()
        {
            if (!Connected || !Enabled)
                return;

            DateTime lastFetch = periodicHttpFetcher.LastSuccess;
            if (lastFetch == DateTime.MinValue || lastFetch <= _lastDataFetch)
                return;

            try
            {
                string content = periodicHttpFetcher.Result;

                /// <!DOCTYPE html><html><head><meta name="viewport" content="width=device-width,user-scalable=0">
                /// <title>STA mode</title></head>
                /// <body><META HTTP-EQUIV="Refresh" Content= "4" > <h2>STARS4ALL<br>TESS-W Data</h2>
                /// <h3> Mag.V :  0.00 mv/as2<br> Frec. : 50000.00 Hz<br> T. IR :   -1.91 &ordm;C<br> T. Sens:   38.91 &ordm;C<br><br> Wifi :   -95 dBm<br>mqtt sec.: 17246</h3>
                /// <p><a href="/config">Show Settings</a></p></body></html>
                Regex r = new Regex(@"Mag.V :\s+(?<mag>[\d.]+).*" +
                                    @"Frec.[\s:]+(?<frec>[\d.-]+).*" +
                                    @"T. IR[\s:]+(?<tempSky>[\d.-]+).*" +
                                    @"T. Sens[\s:]+(?<tempAmb>[\d.-]+).*" /* +
                                    @"Wifi[\s:]+(?<wifi>[\d.-]+).*" +
                                    @"mqtt sec.[\s:]+(?<mqtt>\d+).*" */);
                Match m = r.Match(content);
                if (m.Success)
                {
                    Instance.sensorData["mag"] = m.Result("${mag}");
                    Instance.sensorData["frec"] = m.Result("${frec}");
                    Instance.sensorData["tempSky"] = m.Result("${tempSky}");
                    Instance.sensorData["tempAmb"] = m.Result("${tempAmb}");
                    //Instance.sensorData["wifi"] = m.Result("${wifi}");
                    //Instance.sensorData["mqtt"] = m.Result("${mqtt}");

                    double tAmb = Convert.ToDouble(Instance.sensorData["tempAmb"]);
                    double tSky = Convert.ToDouble(Instance.sensorData["tempSky"]);

                    double percent = 100 - (3 * (tAmb - tSky));
                    Instance.sensorData["cloudCover"] = (Math.Max(percent, 0.0)).ToString();

                    Instance.updatedAtUT = DateTime.UtcNow;

                    Instance._weatherLogger?.Log(new Dictionary<string, string>()
                    {
                        ["Temperature"] = Instance.sensorData["tempAmb"],
                        ["SkyAmbientTemp"] = Instance.sensorData["tempSky"],
                        ["CloudCover"] = Instance.sensorData["cloudCover"],
                    }, Instance.updatedAtUT);

                    #region debug
                    Instance.debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                        $"TessW.Refresh: Success, content: [{content}], duration: {periodicHttpFetcher.Duration}");
                    #endregion

                    _lastDataFetch = lastFetch;
                }
            }
            catch { }
        }

        public void Init()
        {
            if (_initialized)
                return;

            WiseName = "TessW";
            ReadProfile();
            periodicHttpFetcher = new PeriodicHttpFetcher(
                WiseName,
                $"http://{IpAddress}",
                TimeSpan.FromMinutes(1)
            );
            _weatherLogger = new WeatherLogger("TESS-w");
            Refresh();

            _initialized = true;
        }

        public bool Connected
        {
            get
            {
                return _connected;
            }

            set
            {
                if (value == _connected)
                    return;

                _connected = value;

                ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.TessW, value, line: ActivityMonitor.Tracer.safety.Line));

                if (_connected && Enabled)
                    periodicHttpFetcher.Enabled = true;
                else if (!_connected || !Enabled)
                    periodicHttpFetcher.Enabled = false;
            }
        }

        private readonly static ArrayList supportedActions = new ArrayList() {
            "OCHTag",
            "raw-data",
        };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        public string Action(string action, string parameter)
        {
            switch (action)
            {
                case "OCHTag":
                    return "Wise40.TessW";

                case "raw-data":
                    return RawData;

                default:
                    Exceptor.Throw<ActionNotImplementedException>($"Action({action}, {parameter})", "Not implemented by this driver");
                    return string.Empty;
            }
        }

        public string RawData
        {
            get
            {
                TessWStationRawData raw = new TessWStationRawData()
                {
                    Name = WiseName,
                    Vendor = Vendor.ToString(),
                    Model = Model.ToString(),
                    UpdatedAtUT = updatedAtUT,
                    AgeInSeconds = (DateTime.UtcNow - updatedAtUT).TotalSeconds,
                    SensorData = sensorData,
                };

                return JsonConvert.SerializeObject(raw);
            }
        }

        public string DriverDescription
        {
            get
            {
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                return "TessW Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public static string DriverVersion
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                IpAddress = driverProfile.GetValue(Const.WiseDriverID.TessW, Const.ProfileName.TessW_IpAddress, string.Empty, defaultIPAddress);
                Enabled = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.TessW, Const.ProfileName.TessW_Enabled, string.Empty, "true"));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.TessW, Const.ProfileName.TessW_IpAddress, IpAddress);
                driverProfile.WriteValue(Const.WiseDriverID.TessW, Const.ProfileName.TessW_Enabled, Enabled.ToString());
            }
        }

        public string IpAddress { get; set; }

        #region IObservingConditions Implementation

        /// <summary>
        /// Gets and sets the time period over which observations wil be averaged
        /// </summary>
        /// <remarks>
        /// Get must be implemented, if it can't be changed it must return 0
        /// Time period (hours) over which the property values will be averaged 0.0 =
        /// current value, 0.5= average for the last 30 minutes, 1.0 = average for the
        /// last hour
        /// </remarks>
        public double AveragePeriod
        {
            get
            {
                return 0;
            }

            set
            {
                if (value != 0)
                    Exceptor.Throw<InvalidValueException>("AveragePeriod", "Only 0.0 accepted");
            }
        }

        /// <summary>
        /// Amount of sky obscured by cloud
        /// </summary>
        /// <remarks>0%= clear sky, 100% = 100% cloud coverage</remarks>
        public double CloudCover
        {
            get
            {
                return Convert.ToDouble(sensorData["cloudCover"]);
            }
        }

        /// <summary>
        /// Atmospheric dew point at the observatory in deg C
        /// </summary>
        /// <remarks>
        /// Normally optional but mandatory if <see cref=" ASCOM.DeviceInterface.IObservingConditions.Humidity"/>
        /// Is provided
        /// </remarks>
        public double DewPoint
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("DewPoint", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Atmospheric relative humidity at the observatory in percent
        /// </summary>
        /// <remarks>
        /// Normally optional but mandatory if <see cref="ASCOM.DeviceInterface.IObservingConditions.DewPoint"/>
        /// Is provided
        /// </remarks>
        public double Humidity
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("Humidity", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Atmospheric pressure at the observatory in hectoPascals (mB)
        /// </summary>
        /// <remarks>
        /// This must be the pressure at the observatory and not the "reduced" pressure
        /// at sea level. Please check whether your pressure sensor delivers local pressure
        /// or sea level pressure and adjust if required to observatory pressure.
        /// </remarks>
        public double Pressure
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("Pressure", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Rain rate at the observatory
        /// </summary>
        /// <remarks>
        /// This property can be interpreted as 0.0 = Dry any positive nonzero value
        /// = wet.
        /// </remarks>
        public double RainRate
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("RainRate", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Provides a description of the sensor providing the requested property
        /// </summary>
        /// <param name="PropertyName">Name of the property whose sensor description is required</param>
        /// <returns>The sensor description string</returns>
        /// <remarks>
        /// PropertyName must be one of the sensor properties,
        /// properties that are not implemented must throw the MethodNotImplementedException
        /// </remarks>
        public static string SensorDescription(string PropertyName)
        {
            switch (PropertyName)
            {
                case "AveragePeriod":
                    return "Average period in hours, immediate values are only available";

                case "Temperature":
                case "SkyTemperature":
                case "CloudCover":
                    return "SensorDescription - " + PropertyName;

                case "DewPoint":
                case "Humidity":
                case "Pressure":
                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                case "WindGust":
                case "WindDirection":
                case "WindSpeed":
                case "RainRate":
                    Exceptor.Throw<MethodNotImplementedException>($"SensorDescription({PropertyName})", "Not implemented");
                    return string.Empty;
                default:
                    Exceptor.Throw<InvalidValueException>($"SensorDescription({PropertyName})", "Not implemented");
                    return string.Empty;
            }
        }

        /// <summary>
        /// Sky brightness at the observatory
        /// </summary>
        public double SkyBrightness
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("SkyBrightness", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Sky quality at the observatory
        /// </summary>
        public double SkyQuality
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("SkyQuality", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Seeing at the observatory
        /// </summary>
        public double StarFWHM
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("StarFWHM", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Sky temperature at the observatory in deg C
        /// </summary>
        public double SkyTemperature
        {
            get
            {
                Refresh();
                var temperature = Convert.ToDouble(sensorData["tempSky"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format($"TessW: SkyTemperature - get => {temperature}"));
                #endregion
                return temperature;
            }
        }

        /// <summary>
        /// Temperature at the observatory in deg C
        /// </summary>
        public double Temperature
        {
            get
            {
                Refresh();
                var temperature = Convert.ToDouble(sensorData["tempAmb"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format($"TessW: Temperature - get => {temperature}"));
                #endregion
                return temperature;
            }
        }

        /// <summary>
        /// Provides the time since the sensor value was last updated
        /// </summary>
        /// <param name="PropertyName">Name of the property whose time since last update Is required</param>
        /// <returns>Time in seconds since the last sensor update for this property</returns>
        /// <remarks>
        /// PropertyName should be one of the sensor properties Or empty string to get
        /// the last update of any parameter. A negative value indicates no valid value
        /// ever received.
        /// </remarks>
        public double TimeSinceLastUpdate(string PropertyName)
        {
            switch (PropertyName)
            {
                case "DewPoint":
                case "Humidity":
                case "Pressure":
                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                case "WindGust":
                case "WindDirection":
                case "WindSpeed":
                case "RainRate":
                    Exceptor.Throw<MethodNotImplementedException>($"TimeSinceLastUpdate({PropertyName})", "Not implemented");
                    return Double.NaN;
            }

            Refresh();

            return DateTime.Now.Subtract(_lastDataFetch).TotalSeconds;
        }

        /// <summary>
        /// Wind direction at the observatory in degrees
        /// </summary>
        /// <remarks>
        /// 0..360.0, 360=N, 180=S, 90=E, 270=W. When there Is no wind the driver will
        /// return a value of 0 for wind direction
        /// </remarks>
        public double WindDirection
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("WindDirection", "Not implemented");
                return Double.NaN;
            }
        }

        /// <summary>
        /// Peak 3 second wind gust at the observatory over the last 2 minutes in m/s
        /// </summary>
        public double WindGust
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("WindGust", "Not implemented");
                return Double.NaN;
            }
        }
        public double WindSpeed
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("WindSpeed", "Not implemented");
                return Double.NaN;
            }
        }

        #endregion

        public override bool Enabled
        {
            get {
                return _enabled;
            }

            set
            {
                if (periodicHttpFetcher != null)
                {
                    if (value && Connected)
                        periodicHttpFetcher.Enabled = true;
                    else if (!value || !Connected)
                        periodicHttpFetcher.Enabled = false;
                }

                _enabled = value;
            }
        }

        public override WeatherStationInputMethod InputMethod
        {
            get
            {
                return WeatherStationInputMethod.TessW;
            }

            set { }
        }

        public override WeatherStationVendor Vendor
        {
            get
            {
                return WeatherStationVendor.Stars4All;
            }
        }

        public override WeatherStationModel Model
        {
            get
            {
                return WeatherStationModel.TessW;
            }
        }
    }

    public class TessWStationRawData
    {
        public string Name;
        public string Vendor;
        public string Model;
        public DateTime UpdatedAtUT;
        public double AgeInSeconds;
        public Dictionary<string, string> SensorData;
    }
}
