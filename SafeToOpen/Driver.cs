//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM SafetyMonitor driver for Wise40.SafeToOpen
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM SafetyMonitor interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define SafetyMonitor

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;

using ASCOM.DriverAccess;
using ASCOM.CloudSensor;

namespace ASCOM.Wise40.SafeToOpen
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.SafeToOpen.SafetyMonitor
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.SafeToOpen.SafetyMonitor
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40.SafeToOpen from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM SafetyMonitor Driver for Wise40.SafeToOpen.
    /// </summary>
    [Guid("6b5388b9-d420-4596-9311-f5f9c3dd090f")]
    [ClassInterface(ClassInterfaceType.None)]
    public class SafetyMonitor : ISafetyMonitor
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
        private static string driverDescription = "ASCOM Wise40 SafeToOpen.";

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
        private bool _connected;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger tl;

        private static ASCOM.DriverAccess.ObservingConditions boltwood;
        private static ASCOM.DriverAccess.ObservingConditions vantagePro;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40.SafeToOpen"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public SafetyMonitor()
        {
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Wise40.SafeToOpen");
            tl.Enabled = traceState;
            tl.LogMessage("SafetyMonitor", "Starting initialisation");

            _connected = false; // Initialise connected to false

            boltwood = new DriverAccess.ObservingConditions("ASCOM.CloudSensor.ObservingConditions");
            vantagePro = new DriverAccess.ObservingConditions("ASCOM.Vantage.ObservingConditions");

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
            if (IsConnected)
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

            throw new ASCOM.MethodNotImplementedException("CommandString");
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
                tl.LogMessage("Connected Get", IsConnected.ToString());
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == IsConnected)
                    return;

                boltwood.Connected = value;
                vantagePro.Connected = value;
                _connected = value;
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
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
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
            // set by the driver wizard
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
                string name = "Short driver name - please customise";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe
        {
            get
            {
                bool ret = true;

                int light = Convert.ToInt32(boltwood.CommandString("daylight", true));
                if (ageMaxSeconds > 0 && boltwood.TimeSinceLastUpdate("") > ageMaxSeconds)
                    ret = false;
                else if ((int)boltwood.CloudCover > (int)cloudsMax)
                    ret = false;
                else if (light > (int)lightMax)
                    ret = false;

                if (ageMaxSeconds > 0 && vantagePro.TimeSinceLastUpdate("") > ageMaxSeconds)
                    ret = false;
                else if (vantagePro.WindSpeed > windMax)
                    ret = false;
                else if (vantagePro.Humidity > humidityMax)
                    ret = false;
                else if (vantagePro.RainRate > rainMax)
                    ret = false;

                tl.LogMessage("IsSafe Get", ret.ToString());
                return ret;
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "SafetyMonitor";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return _connected;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
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
                int i;

                driverProfile.DeviceType = "SafetyMonitor";
                traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                switch(driverProfile.GetValue(driverID, cloudsMaxProfileName, string.Empty, 0.ToString())) {
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
                switch(driverProfile.GetValue(driverID, lightMaxProfileName, string.Empty, 0.ToString()))
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
