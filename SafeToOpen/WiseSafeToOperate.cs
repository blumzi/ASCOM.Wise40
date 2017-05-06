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
using ASCOM.Wise40.Boltwood;
using ASCOM.Wise40.VantagePro;

namespace ASCOM.Wise40.SafeToOperate
{
    public class WiseSafeToOperate
    {
        private static Version version = new Version(0, 2);
        public enum Type { Open, Image };
        private Type _type;

        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        public string driverID;
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public string driverDescription;
        private string name;

        internal static string cloudsMaxProfileName = "Clouds Max";
        internal static string windMaxProfileName = "Wind Max";
        internal static string rainMaxProfileName = "Rain Max";
        internal static string lightMaxProfileName = "Light Max";
        internal static string humidityMaxProfileName = "Humidity Max";
        internal static string ageMaxSecondsProfileName = "Age Max";

        public Boltwood.SensorData.CloudCondition cloudsMaxEnum;
        public Boltwood.SensorData.DayCondition lightMaxEnum;

        public double cloudsMaxValue;
        public double windMax;
        public double rainMax;
        public int lightMaxValue;
        public double humidityMax;
        public int ageMaxSeconds;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected = false;

        private Wise40.Common.Debugger debugger = Wise40.Common.Debugger.Instance;
        private static TraceLogger tl;
        
        WiseBoltwood boltwood = WiseBoltwood.Instance;
        WiseVantagePro vantagePro = WiseVantagePro.Instance;

        private static WiseSafeToOperate _instanceOpen = new WiseSafeToOperate(Type.Open);
        private static WiseSafeToOperate _instanceImage = new WiseSafeToOperate(Type.Image);

        public static WiseSafeToOperate InstanceOpen
        {
            get
            {
                _instanceOpen.init();
                return _instanceOpen;
            }
        }

        public static WiseSafeToOperate InstanceImage
        {
            get
            {
                _instanceImage.init();
                return _instanceImage;
            }
        }

        public WiseSafeToOperate(Type type)
        {
            _type = type;
        }
        
        public void init()
        {
            string type = _type == Type.Open ? "Open" : "Image";

            name = "Wise40 SafeTo" + type;
            driverID = "ASCOM.Wise40.SafeTo" + type + ".SafetyMonitor";
            driverDescription = string.Format("ASCOM Wise40.SafeTo{0} v{1}", type, version.ToString());
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Wise40.SafeTo" + type);
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false; // Initialise connected to false

            try
            {
                boltwood.init();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Could not init boltwood: {0}", ex.Message));
            }

            try
            {
                //vantagePro = new DriverAccess.ObservingConditions("ASCOM.Vantage.ObservingConditions");
                vantagePro.init();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Could not init vantagePro: {0}", ex.Message));
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
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new ASCOM.MethodNotImplementedException("CommandBool");
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

        public string DriverId
        {
            get
            {
                tl.LogMessage("DriverId Get", driverID);
                return driverID;
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
                string driverInfo = "Implements Wise40 weather max. values, wraps Boltwood CloudSensorII and Davis VantagePro. Version: " + DriverVersion;
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public static string DriverVersion
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

                if (!_boltwoodIsValid)
                    reasons.Add("The Boltwood CloudSensor either not responding or data is too old.");
                else
                {
                    if (!IsSafeCloudCover)
                        reasons.Add("Too cloudy.");
                    if (!IsSafeLight)
                        reasons.Add("Too much light.");
                }

                if (!_vantageProIsValid)
                    reasons.Add("The VantagePro either not responding or data is too old.");
                else
                {
                    if (!IsSafeWindSpeed)
                        reasons.Add("Too windy.");
                    if (!IsSafeHumidity)
                        reasons.Add("Too humid.");
                    if (!IsSafeRain)
                        reasons.Add("Too wet.");
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
                Dictionary<string, int> dayConditions = new Dictionary<string, int>
                {
                    {"dayUnknown", 0 },
                    {"dayDark", 1 },
                    {"dayLight", 2 },
                    {"dayVeryLight", 3 },
                };
                int light = dayConditions[boltwood.CommandString("daylight", true)];
                return (light != 0) && (light <= lightMaxValue);
            }
        }

        private bool IsSafeCloudCover
        {
            get
            {
                return boltwood.CloudCover <= cloudsMaxValue;
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
                bool ret;

                if (!_connected)
                    ret = false;
                else
                    ret = 
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
        public void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";                

                cloudsMaxEnum = (Boltwood.SensorData.CloudCondition)
                    Enum.Parse(typeof(Boltwood.SensorData.CloudCondition),
                        driverProfile.GetValue(driverID, cloudsMaxProfileName, string.Empty, Boltwood.SensorData.CloudCondition.cloudClear.ToString()));
                cloudsMaxValue = Boltwood.SensorData.doubleCloudCondition[cloudsMaxEnum];     
                           
                windMax = Convert.ToDouble(driverProfile.GetValue(driverID, windMaxProfileName, string.Empty, 0.0.ToString()));
                rainMax = Convert.ToDouble(driverProfile.GetValue(driverID, rainMaxProfileName, string.Empty, 0.0.ToString()));
                humidityMax = Convert.ToDouble(driverProfile.GetValue(driverID, humidityMaxProfileName, string.Empty, 0.0.ToString()));
                ageMaxSeconds = Convert.ToInt32(driverProfile.GetValue(driverID, ageMaxSecondsProfileName, string.Empty, 0.ToString()));

                lightMaxEnum = (Boltwood.SensorData.DayCondition)
                    Enum.Parse(typeof(Boltwood.SensorData.DayCondition),
                        driverProfile.GetValue(driverID, lightMaxProfileName, string.Empty, "dayUnknown"));
                lightMaxValue = (int)lightMaxEnum;
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(driverID, cloudsMaxProfileName, cloudsMaxEnum.ToString());
                driverProfile.WriteValue(driverID, windMaxProfileName, windMax.ToString());
                driverProfile.WriteValue(driverID, rainMaxProfileName, rainMax.ToString());
                driverProfile.WriteValue(driverID, lightMaxProfileName, lightMaxEnum.ToString());
                driverProfile.WriteValue(driverID, humidityMaxProfileName, humidityMax.ToString());
                driverProfile.WriteValue(driverID, ageMaxSecondsProfileName, ageMaxSeconds.ToString());
            }
        }
        #endregion
    }
}
