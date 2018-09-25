using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.IO;

using ASCOM.Utilities;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.Boltwood
{
    public class WiseBoltwood: WiseObject
    {
        private bool _initialized = false;
        private bool _connected = false;
        private static Version version = new Version("0.2");
        public static string driverDescription = string.Format("ASCOM Wise40.Boltwood v{0}", version.ToString());
        private string _dataFile;
        private Util utilities = new Util();
        private DateTime _lastDataRead = DateTime.MinValue;
        private WiseSite wisesite = WiseSite.Instance;

        private TraceLogger tl;
        private static volatile WiseBoltwood _instance; // Singleton
        private static object syncObject = new object();
        static WiseBoltwood() {}
        public WiseBoltwood() {}

        private SensorData _sensorData = null;

        public static WiseBoltwood Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncObject)
                    {
                        if (_instance == null)
                            _instance = new WiseBoltwood();
                    }
                }
                _instance.init();
                return _instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            ReadProfile();
            try
            {
                GetSensorData();
            }
            catch { }

            debugger.init();
            tl = new TraceLogger("", "Wise40.Boltwood");
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("ObservingConditions", "initialized");

            _initialized = true;
        }


        public void GetSensorData()
        {
            string str;

            if (_dataFile == null || _dataFile == string.Empty)
                throw new InvalidOperationException("GetSensorData: _dataFile name is either null or empty!");

            if (!File.Exists(_dataFile))
                throw new InvalidOperationException(string.Format("GetSensorData: _dataFile \"{0}\" DOES NOT exist!", _dataFile));

            if (_lastDataRead == DateTime.MinValue || File.GetLastWriteTime(_dataFile).CompareTo(_lastDataRead) > 0)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(_dataFile))
                    {
                        str = sr.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("GetSensorData: Cannot read \"{0}\", caught {1}", _dataFile, e.Message));
                }
                                    
                _sensorData = new SensorData(str);
                _lastDataRead = DateTime.Now;
            }
        }

        private Common.Debugger debugger = Debugger.Instance;

        //
        // PUBLIC COM INTERFACE IObservingConditions IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (_connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                //tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            throw new ASCOM.MethodNotImplementedException("CommandBool");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            //Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
        }

        public bool Connected
        {
            get
            {
                //tl.LogMessage("Connected Get", IsConnected.ToString());
                //return IsConnected;
                return _connected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                if (value)
                {
                    _connected = true;
                    // TODO connect to the device
                }
                else
                {
                    _connected = false;
                    // TODO disconnect from the device
                }
            }
        }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
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

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                tl.LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public new string Name
        {
            get
            {
                string name = "Wise40 Boltwood";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

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
        public double CloudCover_numeric
        {
            get
            {
                double ret = 0.0;

                try
                {
                    GetSensorData();
                } catch
                {
                    return ret;
                }

                switch (_sensorData.cloudCondition)
                {
                    case SensorData.CloudCondition.cloudClear:
                    case SensorData.CloudCondition.cloudUnknown:
                        ret = 0.0;
                        break;
                    case SensorData.CloudCondition.cloudCloudy:
                        ret = 50.0;
                        break;
                    case SensorData.CloudCondition.cloudVeryCloudy:
                        ret = 90.0;
                        break;
                }

                tl.LogMessage("CloudCover", string.Format("get - {0}", ret));
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("Boltwood: CloudCover_numeric - get => {0}", ret.ToString()));
                #endregion 
                return ret;
            }
        }

        public SensorData.CloudCondition CloudCover_condition
        {
            get
            {
                try
                {
                    GetSensorData();
                } catch
                {
                    return SensorData.CloudCondition.cloudUnknown;
                }
                return _sensorData.cloudCondition;
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
                try
                {
                    GetSensorData();
                } catch
                {
                    return double.NaN;
                }
                double ret = _sensorData.dewPoint;
                tl.LogMessage("DewPoint", string.Format("get - {0}", ret));
                return ret;
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
                try
                {
                    GetSensorData();
                } catch
                {
                    return double.NaN;
                }
                double ret = _sensorData.humidity;
                tl.LogMessage("Humidity", string.Format("get - {0}", ret));
                return ret;
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
                tl.LogMessage("Pressure", "get - not implemented");
                throw new PropertyNotImplementedException("Pressure", false);
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
                tl.LogMessage("RainRate", "get - not implemented");
                throw new PropertyNotImplementedException("RainRate", false);
            }
        }

        /// <summary>
        /// Forces the driver to immediatley query its attached hardware to refresh sensor
        /// values
        /// </summary>
        public void Refresh()
        {
            try
            {
                GetSensorData();
            }
            catch { }
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
                case "SkyTemperature":
                case "Temperature":
                case "WindSpeed":
                case "CloudCover":
                    return PropertyName + " Description";
                case "Pressure":
                case "RainRate":
                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                case "WindDirection":
                case "WindGust":
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
                try
                {
                    GetSensorData();
                } catch
                {
                    return 100; // ???
                }
                var ret = _sensorData.skyAmbientTemp;

                if (ret == (double)SensorData.SpecialTempValue.specialTempSaturatedHot)
                    ret = 100;
                else if (ret == (double)SensorData.SpecialTempValue.specialTempSaturatedLow)
                    ret = -100.0;
                else if (ret == (double)SensorData.SpecialTempValue.specialTempWet)
                    ret = 100;
                tl.LogMessage("SkyTemperature", string.Format("get - {0}", ret));
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("Boltwood: SkyTemperature - get => {0}", ret.ToString()));
                #endregion 
                return ret;
            }
        }

        /// <summary>
        /// Temperature at the observatory in deg C
        /// </summary>
        public double Temperature
        {
            get
            {
                try
                {
                    GetSensorData();
                } catch
                {
                    return double.NaN;
                }
                double ret = _sensorData.ambientTemp;

                tl.LogMessage("Temperature", string.Format("get - {0}", ret));
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("Boltwood: Temperature - get => {0}", ret.ToString()));
                #endregion 
                return ret;
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
                case "WindGust":
                case "Pressure":
                case "RainRate":
                case "WindDirection":
                    tl.LogMessage("TimeSinceLastUpdate", PropertyName + " - not implemented");
                    throw new MethodNotImplementedException("SensorDescription(" + PropertyName + ")");
            }

            var ret = _sensorData.age;

            tl.LogMessage("TimeSinceLastUpdate", string.Format("{0} {1}", PropertyName, ret.ToString()));
            return ret;
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
                tl.LogMessage("WindDirection", "get - not implemented");
                throw new PropertyNotImplementedException("WindDirection", false);
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

        /// <summary>
        /// Wind speed at the observatory in m/s
        /// </summary>
        public double WindSpeed
        {
            get
            {
                try
                {
                    GetSensorData();
                } catch
                {
                    return double.NaN;
                }
                double ret = _sensorData.windSpeed;

                switch (_sensorData.windUnits)
                {
                    case SensorData.WindUnits.windKmPerHour:
                        ret = ret * 1000 / 3600;
                        break;
                    case SensorData.WindUnits.windMilesPerHour:
                        ret = utilities.ConvertUnits(ret, Units.milesPerHour, Units.metresPerSecond);
                        break;
                    case SensorData.WindUnits.windMeterPerSecond:
                        break;
                }

                tl.LogMessage("WindSpeed", string.Format("get - {0}", ret));
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety, string.Format("Boltwood: WindSpeed - get => {0}", ret.ToString()));
                #endregion 
                return ret;
            }
        }

        #endregion

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            string defaultDataFile = Simulated ? "c:/temp/ClarityII-data.txt" : "//WO-NEO/Temp/clarityII-data.txt";

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
                _dataFile = driverProfile.GetValue(Const.wiseBoltwoodDriverID, Const.ProfileName.Boltwood_DataFile, string.Empty, defaultDataFile);
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
                driverProfile.WriteValue(Const.wiseBoltwoodDriverID, Const.ProfileName.Boltwood_DataFile, _dataFile);
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
    }
}
