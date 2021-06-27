using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using System.IO;
using System.Threading;
using System.Collections;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using ASCOM.Utilities;

using Newtonsoft.Json;

namespace ASCOM.Wise40.VantagePro
{
    public class WiseVantagePro: WeatherStation
    {
        private static readonly Version version = new Version("0.2");
        public static string driverDescription = $"ASCOM Wise40.VantagePro v{version}";
        private readonly Util util = new Util();

        public enum OpMode { File, Serial };
        public OpMode _opMode = WiseVantagePro.OpMode.File;

        public string _portName = null;
        public int _portSpeed = 19200;
        private System.IO.Ports.SerialPort _port = new System.IO.Ports.SerialPort();

        public static Common.Debugger debugger = Debugger.Instance;
        private bool _connected = false;
        private bool _initialized = false;

        public WiseVantagePro() { }
        static WiseVantagePro() { }

        private Dictionary<string, string> sensorData = null;
        private DateTime _lastDataRead = DateTime.MinValue;
        public DateTime utcTime;

        private static readonly Lazy<WiseVantagePro> lazy = new Lazy<WiseVantagePro>(() => new WiseVantagePro()); // Singleton

        private Seeing _seeing;

        public static WiseVantagePro Instance
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
            if (_opMode == OpMode.File)
                RefreshFromDatafile();
            else
                RefreshFromSerialPort();

            if (_seeing != null)
                _seeing.Refresh();
        }

        public void RefreshFromDatafile()
        {
            if (string.IsNullOrEmpty(DataFile))
            {
                if (_connected)
                    Exceptor.Throw<InvalidValueException>("RefreshFromDatafile", "Null or empty dataFile name");
                else
                    return;
            }

            if (_lastDataRead == DateTime.MinValue || File.GetLastWriteTime(DataFile).CompareTo(_lastDataRead) > 0)
            {
                if (sensorData == null)
                    sensorData = new Dictionary<string, string>();

                for (int tries = 5; tries != 0; tries--)
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(DataFile))
                        {
                            string[] words;
                            string line;

                            if (sr == null)
                                Exceptor.Throw<InvalidValueException>("RefreshFromDatafile", $"Cannot open \"{DataFile}\" for read.");

                            while ((line = sr.ReadLine()) != null)
                            {
                                words = line.Split('=');
                                if (words.Length != 3)
                                    continue;
                                sensorData[words[0]] = words[1];
                            }
                            utcTime = Convert.ToDateTime(sensorData["utcDate"] + " " + sensorData["utcTime"] + "m");

                            _weatherLogger?.Log(new Dictionary<string, string>()
                                {
                                    ["Temperature"] = sensorData["outsideTemp"],
                                    ["Pressure"] = sensorData["barometer"],
                                    ["WindSpeed"] = sensorData["windSpeed"],
                                    ["WindDir"] = sensorData["windDir"],
                                    ["Humidity"] = sensorData["outsideHumidity"],
                                    ["RainRate"] = sensorData["rainRate"],
                                    ["DewPoint"] = util.ConvertUnits(Convert.ToDouble(sensorData["outsideDewPt"]),
                                                        Units.degreesFahrenheit, Units.degreesCelsius).ToString(),
                                }, utcTime.ToLocalTime());

                            _lastDataRead = DateTime.Now;
                        }
                    } catch
                    {
                        Thread.Sleep(500);  // WeatherLink is writing the file
                    }
                }
            }
        }

        private void TryOpenPort()
        {
            if (_port == null)
                _port = new System.IO.Ports.SerialPort();
            else if (_port.IsOpen)
                return;

            _port.PortName = _portName;
            _port.BaudRate = _portSpeed;
            _port.ReadTimeout = 1000;
            _port.ReadBufferSize = 100;
            try
            {
                _port.Open();
            }
            catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "TryOpenPort: Cannot open \"{0}\", ex: {1}",
                    _portName, ex.Message);
                #endregion
                throw;
            }
        }

        private bool TryWakeUpVantagePro()
        {
            TryOpenPort();

            bool awake = false;
            for (var attempts = 3; attempts != 0; attempts--)
            {
                _port.Write("\r");
                if (_port.ReadExisting() == "\n\r")
                {
                    awake = true;
                    break;
                }
            }

            if (!awake)
            {
                #region debug
                #endregion
            }
            return awake;
        }

        /// <summary>
        /// Gets a VantagePro two-bytes entity from the LPS command reply block
        ///  They are transmitted LSB first - (buf[offset] = LSB, buf[offset+1] = MSB)
        /// </summary>
        /// <param name="bytes">The stream of bytes in the reply block</param>
        /// <param name="o">The starting offset</param>
        /// <returns></returns>
        public static ushort GetTwoBytes(byte[] bytes, int o)
        {
            return (ushort) ((bytes[o + 1] << 8) | bytes[o]);
        }

        public void RefreshFromSerialPort()
        {
            if (Simulated)
            {
                sensorData["outsideTemp"] = "300";
                sensorData["windSpeed"] = "400";
                sensorData["windDir"] = "275";
                sensorData["outsideHumidity"] = "85";
                sensorData["barometer"] = "1234";
                sensorData["outsideDewPt"] = "55";
                sensorData["rainRate"] = "11";
                sensorData["ForecastStr"] = "No forecast";
                return;
            }

            if (!TryWakeUpVantagePro())
                return;

            byte[] buf = new byte[99];
            _port.Write("LPS 2 1\n");

            if (_port.ReadByte() != 0x6)
                return;

            if (_port.Read(buf, 0, 99) != 99)
                return;

            // Check the reply is valid - TBD verify the checksum
            if (buf[0] != 'L' || buf[1] != 'O' || buf[2] != 'O' || buf[4] != 1 || buf[95] != '\n' || buf[96] != '\r')
                return;

            ASCOM.Utilities.Util util = new Util();

            double F = GetTwoBytes(buf, 12) / 10.0;
            sensorData["outsideTemp"] = util.ConvertUnits(F, Units.degreesFahrenheit, Units.degreesCelsius).ToString();
            sensorData["windSpeed"] = util.ConvertUnits(buf[14], Units.milesPerHour, Units.metresPerSecond).ToString();
            sensorData["windDir"] = GetTwoBytes(buf, 16).ToString();
            sensorData["outsideHumidity"] = buf[33].ToString();
            sensorData["barometer"] = GetTwoBytes(buf, 7).ToString();
            F = GetTwoBytes(buf, 30);
            sensorData["outsideDewPt"] = util.ConvertUnits(F, Units.degreesFahrenheit, Units.degreesCelsius).ToString();
            sensorData["rainRate"] = GetTwoBytes(buf, 41).ToString();
            sensorData["ForecastStr"] = "No forecast";
        }

        public void Init()
        {
            if (_initialized)
                return;

            WiseName = "VantagePro";
            sensorData = new Dictionary<string, string>();
            ReadProfile();
            _weatherLogger = new WeatherLogger(WiseName);
            if (WiseSite.OperationalMode == WiseSite.OpMode.LCO)
                _seeing = new Seeing();
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

                if (Simulated || _opMode == OpMode.Serial)
                {
                    if (value)
                        TryOpenPort();
                    else
                        _port.Close();
                    _connected = _port.IsOpen;
                }
                else
                {
                    _connected = value;
                }

                ActivityMonitor.Event(new Event.DriverConnectEvent(Const.WiseDriverID.VantagePro, value, line: ActivityMonitor.Tracer.safety.Line));
            }
        }

        public string Description
        {
            get
            {
                return driverDescription;
            }
        }

        private static readonly ArrayList supportedActions = new ArrayList() {
            "raw-data",
            "OCHTag",
            "forecast",
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
            string ret;

            switch (action)
            {
                case "OCHTag":
                    ret = "Wise40.VantagePro2";
                    break;

                case "raw-data":
                    ret = RawData;
                    break;

                case "forecast":
                    return Forecast;

                default:
                    Exceptor.Throw<ActionNotImplementedException>("Action", $"Action(\"{action}\", \"{parameter}\") is not implemented by this driver");
                    return string.Empty;
            }
            return ret;
        }

        public string RawData
        {
            get
            {
                Quality quality = null;

                if (_seeing != null)
                {
                    quality = new Quality
                    {
                        StarFWHM = _seeing.FWHM,
                        UpdatedAtUT = _seeing.TimeLocal,
                        AgeInSeconds = _seeing.TimeSinceLastUpdate.TotalSeconds,
                    };
                }

                Dictionary<string, string> sensorDataCopy = new Dictionary<string, string>(sensorData);

                VantagePro2StationRawData raw = new VantagePro2StationRawData()
                {
                    Name = WiseName,
                    Vendor = Vendor.ToString(),
                    Model = Model.ToString(),
                    UpdateAtUT = utcTime,
                    AgeInSeconds = TimeSinceLastUpdate("Temperature"),
                    SensorData = sensorDataCopy,
                    SkyQuality = quality,
                };

                return JsonConvert.SerializeObject(raw);
            }
        }

        public static string DriverDescription
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
                return "Wrapper for VantagePro Report file. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
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
            string defaultReportFile = Simulated ?
                    "c:/temp/Weather_Wise40_Vantage_Pro.htm" :
                    "c:/Wise40/Weather/Davis VantagePro/Weather_Wise40_Vantage_Pro.htm";

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                Enum.TryParse<OpMode>(driverProfile.GetValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_OpMode, string.Empty, nameof(OpMode.File)), out OpMode mode);
                _opMode = mode;
                DataFile = driverProfile.GetValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_DataFile, string.Empty, defaultReportFile);
                _portName = driverProfile.GetValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_SerialPort, string.Empty, "");
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_OpMode, _opMode.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_DataFile, DataFile);
                driverProfile.WriteValue(Const.WiseDriverID.VantagePro, Const.ProfileName.VantagePro_SerialPort, _portName);
            }
        }

        public string DataFile { get; set; }

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
                Exceptor.Throw<PropertyNotImplementedException>("CloudCover", "Not implemented");
                return Double.NaN;
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
                Refresh();
                var dewPoint = Convert.ToDouble(sensorData["outsideDewPt"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: DewPoint - get => {0}", dewPoint.ToString()));
                #endregion
                return dewPoint;
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
                Refresh();
                var humidity = Convert.ToDouble(sensorData["outsideHumidity"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: Humidity - get => {0}", humidity.ToString()));
                #endregion
                return humidity;
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
                Refresh();
                var pressure = Convert.ToDouble(sensorData["barometer"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: Pressure - get => {0}", pressure.ToString()));
                #endregion
                return pressure;
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
                Refresh();
                var rainRate = Convert.ToDouble(sensorData["rainRate"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"VantagePro: RainRate - get => {rainRate}");
                #endregion
                return rainRate;
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

                case "DewPoint":
                case "Humidity":
                case "Pressure":
                case "Temperature":
                case "WindDirection":
                case "WindSpeed":
                case "RainRate":
                case "StarFWHM":
                    return "SensorDescription - " + PropertyName;

                case "SkyBrightness":
                case "SkyQuality":
                case "SkyTemperature":
                case "WindGust":
                case "CloudCover":
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
                return double.NaN;
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
                return double.NaN;
            }
        }

        /// <summary>
        /// Seeing at the observatory
        /// </summary>
        public double StarFWHM
        {
            get
            {
                return (_seeing == null) ? double.NaN : _seeing.FWHM;
            }
        }

        /// <summary>
        /// Sky temperature at the observatory in deg C
        /// </summary>
        public double SkyTemperature
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("SkyTemperature", "Not implemented");
                return double.NaN;
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
                var temperature = Convert.ToDouble(sensorData["outsideTemp"]);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: Temperature - get => {0}", temperature.ToString()));
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
                case "SkyBrightness":
                case "SkyQuality":
                case "SkyTemperature":
                case "WindGust":
                case "CloudCover":
                    Exceptor.Throw<MethodNotImplementedException>($"TimeSinceLastUpdate({PropertyName})", "Not implemented");
                    return double.NaN;
            }
            Refresh();

            if (PropertyName == "StartFWHM")
            {
                return (_seeing != null) ?
                    _seeing.TimeSinceLastUpdate.TotalSeconds :
                    DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds;
            }

            double seconds = 0.0;
            if (_opMode == OpMode.File)
            {
                seconds = (DateTime.UtcNow - utcTime).TotalSeconds;
            }

            return seconds;
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
                Refresh();
                return Convert.ToDouble(sensorData["windDir"]);
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
                return double.NaN;
            }
        }

        public static double MPS(double kmh)
        {
            return kmh * (1000.0 / 3600.0);
        }

        public static double KMH(double mps)
        {
            return mps * 3.6;
        }

        /// <summary>
        /// Wind speed at the observatory in m/s
        /// </summary>
        public double WindSpeedMps
        {
            get
            {
                Refresh();
                double kmh = Convert.ToSingle(sensorData["windSpeed"]);
                double windSpeed = MPS(kmh);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: WindSpeed - get => {0}", windSpeed.ToString()));
                #endregion
                return windSpeed;
            }
        }

        public string Forecast
        {
            get
            {
                Refresh();
                var forecast = sensorData["ForecastStr"];

                #region debug
                if (WiseSite.CurrentProcessIs(Const.Application.RESTServer))
                    debugger.WriteLine(Debugger.DebugLevel.DebugSafety, $"VantagePro: Forecast - get => {forecast}");
                #endregion
                return forecast;
            }
        }

        #endregion

        public override bool Enabled
        {
            get { return true; }
            set { }
        }

        public override WeatherStationInputMethod InputMethod
        {
            get
            {
                return WeatherStationInputMethod.WeatherLink_HtmlReport;
            }

            set { }
        }

        public override WeatherStationVendor Vendor
        {
            get
            {
                return WeatherStationVendor.DavisInstruments;
            }
        }

        public override WeatherStationModel Model
        {
            get
            {
                return WeatherStationModel.VantagePro2;
            }
        }
    }

    public class Quality
    {
        public DateTime UpdatedAtUT;
        public double AgeInSeconds;
        public double StarFWHM;
    };

    public class VantagePro2StationRawData
    {
        public string Name;
        public string Vendor;
        public string Model;
        public DateTime UpdateAtUT;
        public double AgeInSeconds;
        public Dictionary<string, string> SensorData;
        public Quality SkyQuality;
    }
}
