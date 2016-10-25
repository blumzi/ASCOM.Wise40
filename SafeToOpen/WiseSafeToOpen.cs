using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using ASCOM;
using ASCOM.Utilities;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40.SafeToOpen
{
    public class WiseSafeToOpen
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Wise40.SafeToOpen.SafetyMonitor";
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        internal static string driverDescription = "ASCOM Wise40 SafeToOpen.";

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static string cloudsMaxProfileName = "Clouds Max";
        internal static string windMaxProfileName = "Wind Max";
        internal static string rainMaxProfileName = "Rain Max";
        internal static string lightMaxProfileName = "Light Max";
        internal static string humidityMaxProfileName = "Humidity Max";
        internal static string ageMaxSecondsProfileName = "Age Max";

        internal static CloudSensor.SensorData.CloudCondition cloudsMax;
        internal static double windMax;
        internal static double rainMax;
        internal static CloudSensor.SensorData.DayCondition lightMax;
        internal static double humidityMax;
        internal static int ageMaxSeconds;
        internal static bool traceState;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected = false;

        private Wise40.Common.Debugger debugger = Wise40.Common.Debugger.Instance;
        private TraceLogger tl;

        private static ASCOM.DriverAccess.ObservingConditions boltwood;
        private static ASCOM.DriverAccess.ObservingConditions vantagePro;

        private static WiseSafeToOpen _instance = new WiseSafeToOpen();
        public static WiseSafeToOpen Instance
        {
            get
            {
                return _instance;
            }
        }

        public WiseSafeToOpen()
        {
        }

        static WiseSafeToOpen()
        {
        }

        public void init() {
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Wise40.SafeToOpen");
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false; // Initialise connected to false

            try
            {
                boltwood = new DriverAccess.ObservingConditions("ASCOM.CloudSensor.ObservingConditions");
                vantagePro = new DriverAccess.ObservingConditions("ASCOM.Vantage.ObservingConditions");
            }
            catch
            {
                throw new InvalidOperationException("Could not open weather stations");
            }

            tl.LogMessage("SafetyMonitor", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE ISafetyMonitor IMPLEMENTATION
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
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            if (command == "unsafeReasons")
                return string.Join(Const.crnl, UnsafeReasons);

            return string.Empty;
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
        }

        public bool Connected
        {
            get
            {
                tl.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                if (boltwood != null)
                    boltwood.Connected = value;
                if (vantagePro != null)
                    vantagePro.Connected = value;
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

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "Implements Wise40 weather max. values, wraps Boltwood CloudSensorII and Davis VantagePro. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                tl.LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                string name = "Wise40 SafeToOpen";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion
        
        public List<string> UnsafeReasons
        {
            get
            {
                List<string> reasons = new List<string>();

                if (boltwood == null)
                    reasons.Add("No connection to the Boltwood station");
                else
                {
                    int light = Convert.ToInt32(boltwood.CommandString("daylight", true));
                    if (ageMaxSeconds > 0 && boltwood.TimeSinceLastUpdate("") > ageMaxSeconds)
                        reasons.Add(string.Format("boltwood - data is older than {0} seconds", ageMaxSeconds));
                    if ((int)boltwood.CloudCover > (int)cloudsMax)
                        reasons.Add(string.Format("boltwood - CloudCover {0} is greater than {1}", (int)boltwood.CloudCover > (int)cloudsMax));
                    if (light > (int)lightMax)
                        reasons.Add(string.Format("boltwood - DayLight {0} is greater than {1}", light, lightMax));
                }

                if (vantagePro == null)
                    reasons.Add("No connection to the VantagePro station");
                else
                {
                    if (ageMaxSeconds > 0 && vantagePro.TimeSinceLastUpdate("") > ageMaxSeconds)
                        reasons.Add(string.Format("vantagePro - data is older than {0} seconds", ageMaxSeconds));
                    if (vantagePro.WindSpeed > windMax)
                        reasons.Add(string.Format("vantagePro - WindSpeed {0} is greater than {1}", vantagePro.WindSpeed, windMax));
                    if (vantagePro.Humidity > humidityMax)
                        reasons.Add(string.Format("vantagePro - Humidity {0} is greater than {1}", vantagePro.Humidity, humidityMax));
                    if (vantagePro.RainRate > rainMax)
                        reasons.Add(string.Format("vantagePro - RainRate {0} is greater than {1}", vantagePro.RainRate, rainMax));
                }
                return reasons;
            }
        }

        #region Individual Property Implementations
        #region Boolean Properties (for ASCOM)
        private bool _boltwoodIsValid
        {
            get
            {
                if (boltwood == null)
                    return false;
                if (ageMaxSeconds > 0 && boltwood.TimeSinceLastUpdate("") > ageMaxSeconds)
                    return false;
                return true;
            }
        }

        private bool _vantageProIsValid
        {
            get
            {
                if (vantagePro == null)
                    return false;
                if (ageMaxSeconds > 0 && vantagePro.TimeSinceLastUpdate("") > ageMaxSeconds)
                    return false;
                return true;
            }
        }

        private bool IsSafeLight
        {
            get
            {
                return Convert.ToInt32(boltwood.CommandString("daylight", true)) <= (int)lightMax;
            }
        }

        private bool IsSafeCloudCover
        {
            get
            {
                return (int)boltwood.CloudCover <= (int)cloudsMax;
            }
        }

        private bool IsSafeWindSpeed
        {
            get
            {
                return vantagePro.WindSpeed <= windMax;
            }
        }

        private bool IsSafeHumidity
        {
            get
            {
                return vantagePro.Humidity <= humidityMax;
            }
        }

        private bool IsSafeRain
        {
            get
            {
                return vantagePro.RainRate <= rainMax;
            }
        }
        #endregion

        #region TriState Properties (for object)
        public Const.TriStateStatus isSafeCloudCover
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    IsSafeCloudCover ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeLight
        {
            get
            {
                return !_boltwoodIsValid ? Const.TriStateStatus.Warning :
                    IsSafeLight ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeWindSpeed
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeWindSpeed ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;                
            }
        }

        public Const.TriStateStatus isSafeHumidity
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeHumidity ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }

        public Const.TriStateStatus isSafeRain
        {
            get
            {
                return !_vantageProIsValid ? Const.TriStateStatus.Warning :
                    IsSafeRain ? Const.TriStateStatus.Good : Const.TriStateStatus.Error;
            }
        }
        #endregion
        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                bool ret = 
                    _boltwoodIsValid &&
                    _vantageProIsValid &&
                    IsSafeLight &&
                    IsSafeCloudCover &&
                    IsSafeWindSpeed &&
                    IsSafeHumidity &&
                    IsSafeRain;

                tl.LogMessage("IsSafe Get", ret.ToString());
                return ret;
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

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

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal static void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                switch (driverProfile.GetValue(driverID, cloudsMaxProfileName, string.Empty, 0.ToString()))
                {
                    case "cloudUnknown":
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudUnknown;
                        break;
                    case "cloudClear":
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudClear;
                        break;
                    case "cloudCloudy":
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudCloudy;
                        break;
                    case "cloudVeryCloudy":
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudVeryCloudy;
                        break;
                    case "cloudWet":
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudWet;
                        break;
                    default:
                        cloudsMax = CloudSensor.SensorData.CloudCondition.cloudUnknown;
                        break;
                }
                windMax = Convert.ToDouble(driverProfile.GetValue(driverID, windMaxProfileName, string.Empty, 0.0.ToString()));
                rainMax = Convert.ToDouble(driverProfile.GetValue(driverID, rainMaxProfileName, string.Empty, 0.0.ToString()));
                humidityMax = Convert.ToDouble(driverProfile.GetValue(driverID, humidityMaxProfileName, string.Empty, 0.0.ToString()));
                ageMaxSeconds = Convert.ToInt32(driverProfile.GetValue(driverID, ageMaxSecondsProfileName, string.Empty, 0.0.ToString()));
                switch (driverProfile.GetValue(driverID, lightMaxProfileName, string.Empty, 0.ToString()))
                {
                    case "dayUnknown":
                        lightMax = CloudSensor.SensorData.DayCondition.dayUnknown;
                        break;
                    case "dayDark":
                        lightMax = CloudSensor.SensorData.DayCondition.dayDark;
                        break;
                    case "dayLight":
                        lightMax = CloudSensor.SensorData.DayCondition.dayLight;
                        break;
                    case "dayVeryLight":
                        lightMax = CloudSensor.SensorData.DayCondition.dayVeryLight;
                        break;
                    default:
                        lightMax = CloudSensor.SensorData.DayCondition.dayUnknown;
                        break;
                }
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString());
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, cloudsMax.ToString());
                driverProfile.WriteValue(driverID, windMaxProfileName, windMax.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, rainMax.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, lightMax.ToString());
                driverProfile.WriteValue(driverID, humidityMaxProfileName, humidityMax.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, ageMaxSeconds.ToString());
            }
        }
        #endregion
    }
}
