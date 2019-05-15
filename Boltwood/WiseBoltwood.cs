using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.IO;

using ASCOM.Utilities;
using ASCOM.Wise40;
using ASCOM.Wise40.Common;
using Newtonsoft.Json;

namespace ASCOM.Wise40.Boltwood
{
    public class WiseBoltwood : WiseObject
    {
        private bool _initialized = false;
        private bool _connected = false;
        private static Version version = new Version("0.2");
        public static string driverDescription = string.Format("ASCOM Wise40.Boltwood v{0}", version.ToString());
        private Util utilities = new Util();
        private WiseSite wisesite = WiseSite.Instance;

        public const int nStations = 6;
        public static BoltwoodStation[] stations = new BoltwoodStation[nStations];
        private BoltwoodStation C18Station, C28Station;

        static WiseBoltwood() { }
        public WiseBoltwood() { }

        private static readonly Lazy<WiseBoltwood> lazy = new Lazy<WiseBoltwood>(() => new WiseBoltwood()); // Singleton

        public static WiseBoltwood Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.init();
                return lazy.Value;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            for (int i = 0; i < nStations; i++)
            {
                stations[i] = new BoltwoodStation(i);
                stations[i].ReadProfile();
                if (stations[i].Name == "C18")
                    C18Station = stations[i];
                else if (stations[i].Name == "C28")
                    C28Station = stations[i];

                try
                {
                    stations[i].GetSensorData();
                }
                catch { }
            }

            _initialized = true;
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


        private static ArrayList supportedActions = new ArrayList() {
            "raw-data",
            "OCHTag",
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
            string ret = "";

            switch (action)
            {
                case "OCHTag":
                    ret = "Wise40.Boltwood";
                    break;

                case "raw-data":
                    ret = RawData;
                    break;

                default:
                    throw new ASCOM.ActionNotImplementedException("Action " + action + " is not implemented by this driver");
            }
            return ret;
        }

        public string RawData
        {
            get
            {
                List<BoltwoodStation.BoltwoodStationRawData> list = new List<BoltwoodStation.BoltwoodStationRawData>();

                foreach (var station in WiseBoltwood.stations)
                    list.Add(new BoltwoodStation.BoltwoodStationRawData()
                    {
                        Name = station.Name,
                        Vendor = station.Vendor.ToString(),
                        Model = station.Model.ToString(),
                        SensorData = station.SensorData,
                    });
                return JsonConvert.SerializeObject(list);
            }
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

                ActivityMonitor.Instance.Event(new Event.GlobalEvent(
                    string.Format("{0} {1}", Const.WiseDriverID.Boltwood, value ? "Connected" : "Disconnected")));
            }
        }

        public string Description
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
                return "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public static string driverVersion
        {
            get
            {
                return DriverVersion;
            }
        }

        public static string DriverVersion
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                return "Wise40 Boltwood";
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
                return 0;
            }

            set
            {
                if (value != 0)
                    throw new InvalidValueException("Only 0.0 accepted");
            }
        }

        private double CloudConditionToNumeric(SensorData.CloudCondition condition)
        {
            double ret = 0.0;

            switch (condition)
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
            return ret;
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return ret;
                }

                ret = CloudConditionToNumeric(C18Station.SensorData.cloudCondition);

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugSafety,
                    string.Format("Boltwood: CloudCover_numeric - get => {0} (C18: {1}, C28: {2})",
                    ret.ToString(),
                    CloudConditionToNumeric(C18Station.SensorData.cloudCondition),
                    CloudConditionToNumeric(C28Station.SensorData.cloudCondition)));
                #endregion 
                return ret;
            }
        }

        private void GetAllStationsSensorData()
        {
            foreach (var station in stations)
                if (station.Enabled)
                    station.GetSensorData();
        }

        public SensorData.CloudCondition CloudCover_condition
        {
            get
            {
                try
                {
                    GetAllStationsSensorData();
                }
                catch
                {
                    return SensorData.CloudCondition.cloudUnknown;
                }
                return C18Station.SensorData.cloudCondition;
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return double.NaN;
                }
                return C18Station.SensorData.dewPoint;
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return double.NaN;
                }
                return C18Station.SensorData.humidity;
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
                GetAllStationsSensorData();
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
                    throw new MethodNotImplementedException("SensorDescription(" + PropertyName + ")");
                default:
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return 100; // ???
                }
                var ret = C18Station.SensorData.skyAmbientTemp;

                if (ret == (double)SensorData.SpecialTempValue.specialTempSaturatedHot)
                    ret = 100;
                else if (ret == (double)SensorData.SpecialTempValue.specialTempSaturatedLow)
                    ret = -100.0;
                else if (ret == (double)SensorData.SpecialTempValue.specialTempWet)
                    ret = 100;
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return double.NaN;
                }
                double ret = C18Station.SensorData.ambientTemp;

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
                    throw new MethodNotImplementedException("SensorDescription(" + PropertyName + ")");
            }

            return C18Station.SensorData.age;
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
                    GetAllStationsSensorData();
                }
                catch
                {
                    return double.NaN;
                }
                double ret = C18Station.SensorData.windSpeed;

                switch (C18Station.SensorData.windUnits)
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
            foreach (var station in stations)
                station.ReadProfile();
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            foreach (var station in stations)
                station.WriteProfile();
        }
    }

    public class BoltwoodStation : WeatherStation
    {
        private int _id;
        private bool _enabled;
        private string _name;
        private string _file;
        private WeatherStationVendor _vendor = WeatherStationVendor.Boltwood;
        public WeatherStationModel _model = WeatherStationModel.CloudSensorII;
        private WeatherStationInputMethod _method;
        private DateTime _lastDataRead = DateTime.MinValue;
        private SensorData _sensorData = null;

        public BoltwoodStation(int id)
        {
            Id = id;
            ReadProfile();
        }

        public override WeatherStationVendor Vendor
        {
            get
            {
                return _vendor;
            }
        }

        public override WeatherStationModel Model
        {
            get
            {
                return _model;
            }
        }

        public void ReadProfile()
        {
            string subKey = "Station" + Id.ToString();

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                Name = driverProfile.GetValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_Name, subKey, string.Empty);
                Enabled = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_Enabled, subKey, "false"));
                FilePath = driverProfile.GetValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_DataFile, subKey, string.Empty);

                WeatherStationInputMethod method;

                if (Enum.TryParse<WeatherStationInputMethod>(driverProfile.GetValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_InputMethod, null, "ClarityII"), out method))
                    InputMethod = method;
            }
        }

        public void WriteProfile()
        {
            string subKey = "Station" + Id.ToString();

            using (Profile driverProfile = new Profile() { DeviceType = "ObservingConditions" })
            {
                driverProfile.WriteValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_Name, Name, subKey);
                driverProfile.WriteValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_Enabled, Enabled.ToString(), subKey);
                driverProfile.WriteValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_DataFile, FilePath, subKey);
                driverProfile.WriteValue(Const.WiseDriverID.Boltwood, Const.ProfileName.Boltwood_InputMethod, InputMethod.ToString(), subKey);
            }
        }

        public void GetSensorData()
        {
            switch (InputMethod)
            {
                case WeatherStationInputMethod.ClarityII:
                    GetClarityIISensorData();
                    break;

                case WeatherStationInputMethod.Weizmann_TBD:
                case WeatherStationInputMethod.Korean_TBD:
                    break;
            }
        }
        private void GetWeizmannSensorData() { }
        private void GetKoreanSensorData() { }
        private void GetClarityIISensorData()
        {
            string str;

            if (FilePath == null || FilePath == string.Empty)
                throw new InvalidOperationException("GetSensorData: _dataFile name is either null or empty!");

            if (!File.Exists(FilePath))
                throw new InvalidOperationException(string.Format("GetSensorData: _dataFile \"{0}\" DOES NOT exist!", FilePath));

            if (_lastDataRead == DateTime.MinValue || File.GetLastWriteTime(FilePath).CompareTo(_lastDataRead) > 0)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(FilePath))
                    {
                        str = sr.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("GetSensorData: Cannot read \"{0}\", caught {1}", FilePath, e.Message));
                }

                _sensorData = new SensorData(str);
                _lastDataRead = DateTime.Now;
            }
        }

        public SensorData SensorData
        {
            get
            {
                return _sensorData;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public int Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }

        public override bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
            }
        }

        public override WeatherStationInputMethod InputMethod
        {
            get
            {
                return _method;
            }

            set
            {
                _method = value;
            }
        }

        public string FilePath
        {
            get
            {
                return _file;
            }

            set
            {
                _file = value;
            }
        }

        public class BoltwoodStationRawData
        {
            public string Name;
            public string Vendor;
            public string Model;
            public SensorData SensorData;
        }
    }
}
