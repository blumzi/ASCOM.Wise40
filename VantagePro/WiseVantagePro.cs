using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using System.IO;
using System.Threading;
using ASCOM.Wise40.Common;
using ASCOM.Utilities;


namespace ASCOM.Wise40.VantagePro
{
    public class WiseVantagePro: WiseObject
    {
        private string _dataFile;
        private static WiseVantagePro _instance = new WiseVantagePro();
        private static Version version = new Version("0.2");
        public static string driverDescription = string.Format("ASCOM Wise40.VantagePro v{0}", version.ToString());
        private Util util = new Util();
        private TraceLogger tl;

        public enum OpMode { File, Serial };
        public OpMode _opMode = WiseVantagePro.OpMode.File;

        public string _portName = null;
        public int _portSpeed = 19200;
        System.IO.Ports.SerialPort _port = new System.IO.Ports.SerialPort();

        Common.Debugger debugger = Debugger.Instance;
        private bool _connected = false;
        private bool _initialized = false;

        public WiseVantagePro() { }
        static WiseVantagePro() { }

        private Dictionary<string, string> sensorData = null;
        private DateTime _lastDataRead = DateTime.MinValue;
        private static object syncObject = new object();

        public static WiseVantagePro Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseVantagePro();
                    }
                }
                _instance.init();
                return _instance;
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
        }

        public void RefreshFromDatafile()
        {
            tl.LogMessage("Refresh", "dataFile: " + _dataFile);

            if (_dataFile == null || _dataFile == string.Empty)
            {
                if (_connected)
                    throw new InvalidValueException("Null or empty dataFile name");
                else
                    return;
            }

            if (_lastDataRead == DateTime.MinValue || File.GetLastWriteTime(_dataFile).CompareTo(_lastDataRead) > 0)
            {
                if (sensorData == null)
                    sensorData = new Dictionary<string, string>();

                for (int tries = 5; tries != 0; tries--)
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(_dataFile))
                        {
                            string[] words;
                            string line;

                            if (sr == null)
                                throw new InvalidValueException(string.Format("Refresh: cannot open \"{0}\" for read.", _dataFile));

                            while ((line = sr.ReadLine()) != null)
                            {
                                words = line.Split('=');
                                if (words.Length != 3)
                                    continue;
                                sensorData[words[0]] = words[1];
                            }
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
        public ushort getTwoBytes(byte[] bytes, int o)
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

            double F = getTwoBytes(buf, 12) / 10.0;
            sensorData["outsideTemp"] = util.ConvertUnits(F, Units.degreesFarenheit, Units.degreesCelsius).ToString();
            sensorData["windSpeed"] = util.ConvertUnits(buf[14], Units.milesPerHour, Units.metresPerSecond).ToString();
            sensorData["windDir"] = getTwoBytes(buf, 16).ToString();
            sensorData["outsideHumidity"] = buf[33].ToString();
            sensorData["barometer"] = getTwoBytes(buf, 7).ToString();
            F = getTwoBytes(buf, 30);
            sensorData["outsideDewPt"] = util.ConvertUnits(F, Units.degreesFarenheit, Units.degreesCelsius).ToString();
            sensorData["rainRate"] = getTwoBytes(buf, 41).ToString();
            sensorData["ForecastStr"] = "No forecast";
        }

        public void init()
        {
            if (_initialized)
                return;

            Name = "Wise40.VantagePro";
            tl = new TraceLogger("", "Wise40.VantagePro");
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("ObservingConditions", "initialized");

            sensorData = new Dictionary<string, string>();

            ReadProfile();
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
                tl.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                if (Simulated || _opMode == OpMode.Serial)
                {
                    if (value == true)
                        TryOpenPort();
                    else
                        _port.Close();
                    _connected = _port.IsOpen;
                } else
                    _connected = value;
            }
        }

        public string Description
        {
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
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
                string driverInfo = "Wrapper for VantagePro Report file. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public static string driverVersion
        {
            get
            {
                return _instance.DriverVersion;
            }
        }

        public string DriverVersion
        {
            get
            {
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
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
                ;
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                OpMode mode;

                Enum.TryParse<OpMode>(driverProfile.GetValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_OpMode, string.Empty, OpMode.File.ToString()), out mode);
                _opMode = mode;
                _dataFile = driverProfile.GetValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_DataFile, string.Empty, defaultReportFile);
                _portName = driverProfile.GetValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_SerialPort, string.Empty, "");
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                driverProfile.WriteValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_OpMode, _opMode.ToString());
                driverProfile.WriteValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_DataFile, _dataFile);
                driverProfile.WriteValue(Const.wiseVantageProDriverID, Const.ProfileName.VantagePro_SerialPort, _portName);
            }
        }

        public string DataFile
        {
            get
            {
                return _dataFile;
            }

            set
            {
                _dataFile = value;
            }
        }

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
                tl.LogMessage("AveragePeriod", "get - 0");
                return 0;
            }
            set
            {
                tl.LogMessage("AveragePeriod", string.Format("set - {0}", value));
                if (value != 0)
                    throw new InvalidValueException("Only 0.0 accepted");
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
                tl.LogMessage("CloudCover", "get - not implemented");
                throw new PropertyNotImplementedException("CloudCover", false);
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

                tl.LogMessage("DewPoint", "get - " + dewPoint.ToString());
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

                tl.LogMessage("Humidity", "get - " + humidity.ToString());
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

                tl.LogMessage("Pressure", "get - " + pressure.ToString());
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

                tl.LogMessage("RainRate", "get - " + rainRate.ToString());
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("VantagePro: RainRate - get => {0}", rainRate.ToString()));
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
        public string SensorDescription(string PropertyName)
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
                    return "SensorDescription - " + PropertyName;

                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                case "SkyTemperature":
                case "WindGust":
                case "CloudCover":
                    tl.LogMessage("SensorDescription", PropertyName + " - not implemented");
                    throw new MethodNotImplementedException("SensorDescription(" + PropertyName + ")");
                default:
                    tl.LogMessage("SensorDescription", PropertyName + " - unrecognised");
                    throw new ASCOM.InvalidValueException("SensorDescription(" + PropertyName + ")");
            }
        }

        /// <summary>
        /// Sky brightness at the observatory
        /// </summary>
        public double SkyBrightness
        {
            get
            {
                tl.LogMessage("SkyBrightness", "get - not implemented");
                throw new PropertyNotImplementedException("SkyBrightness", false);
            }
        }

        /// <summary>
        /// Sky quality at the observatory
        /// </summary>
        public double SkyQuality
        {
            get
            {
                tl.LogMessage("SkyQuality", "get - not implemented");
                throw new PropertyNotImplementedException("SkyQuality", false);
            }
        }

        /// <summary>
        /// Seeing at the observatory
        /// </summary>
        public double StarFWHM
        {
            get
            {
                tl.LogMessage("StarFWHM", "get - not implemented");
                throw new PropertyNotImplementedException("StarFWHM", false);
            }
        }

        /// <summary>
        /// Sky temperature at the observatory in deg C
        /// </summary>
        public double SkyTemperature
        {
            get
            {
                tl.LogMessage("SkyTemperature", "get - not implemented");
                throw new PropertyNotImplementedException("SkyTemperature", false);
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

                tl.LogMessage("Temperature", "get - " + temperature.ToString());
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
                case "StarFWHM":
                case "SkyTemperature":
                case "WindGust":
                case "CloudCover":
                    tl.LogMessage("TimeSinceLastUpdate", PropertyName + " - not implemented");
                    throw new MethodNotImplementedException("SensorDescription(" + PropertyName + ")");
            }
            Refresh();

            double seconds = 0.0;
            if (_opMode == OpMode.File)
            {
                string dateTime = sensorData["utcDate"] + " " + sensorData["utcTime"] + "m";
                DateTime lastUpdate = Convert.ToDateTime(dateTime);
                seconds = (DateTime.UtcNow - lastUpdate).TotalSeconds;
            }

            tl.LogMessage("TimeSinceLastUpdate", PropertyName + seconds.ToString());
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
                var windDir = Convert.ToDouble(sensorData["windDir"]);

                tl.LogMessage("WindDirection", "get - " + windDir.ToString());
                return windDir;
            }
        }

        /// <summary>
        /// Peak 3 second wind gust at the observatory over the last 2 minutes in m/s
        /// </summary>
        public double WindGust
        {
            get
            {
                tl.LogMessage("WindGust", "get - not implemented");
                throw new PropertyNotImplementedException("WindGust", false);
            }
        }

        public double MPS(double kmh)
        {
            return kmh * (1000.0 / 3600.0);
        }

        public double KMH(double mps)
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

                tl.LogMessage("WindSpeed", "get - " + windSpeed.ToString());
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
                return sensorData["ForecastStr"];
            }
        }

        #endregion
    }
}
