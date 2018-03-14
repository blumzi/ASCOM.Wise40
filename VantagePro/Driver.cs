//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM ObservingConditions driver for Wise40.VantagePro
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM ObservingConditions interface version: <To be completed by driver developer>
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
#define ObservingConditions

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Collections;


namespace ASCOM.Wise40.VantagePro
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.VantagePro.ObservingConditions
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.VantagePro.ObservingConditions
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40.VantagePro from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM ObservingConditions Driver for Wise40.VantagePro.
    /// </summary>
    [Guid("1a13f123-87d0-4b46-a378-c7da7f98ce12")]
    [ClassInterface(ClassInterfaceType.None)]
    public class ObservingConditions : IObservingConditions
    {
        private Common.Debugger debugger = Common.Debugger.Instance;
        private WiseVantagePro vantagePro = WiseVantagePro.Instance;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger tl;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40.VantagePro"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public ObservingConditions()
        {
            vantagePro.init();
            debugger.init();

            tl = new TraceLogger("", "Wise40.VantagePro");
            tl.Enabled = debugger.Tracing;
            tl.LogMessage("ObservingConditions", "Starting initialisation");

            tl.LogMessage("ObservingConditions", "Completed initialisation");
        }


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
            if (vantagePro.Connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    vantagePro.WriteProfile();
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
            throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
        }

        public bool Connected
        {
            get
            {
                var ret = vantagePro.Connected;
                tl.LogMessage("Connected Get", ret.ToString());
                return ret;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == vantagePro.Connected)
                    return;

                vantagePro.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                var ret = vantagePro.Description;

                tl.LogMessage("Description Get", ret);
                return ret;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = vantagePro.DriverInfo;

                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                string driverVersion = vantagePro.DriverVersion;
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
                var name = vantagePro.Name;
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
                var ret = vantagePro.AveragePeriod;
                tl.LogMessage("AveragePeriod", string.Format("get - {0}", ret));
                return ret;
            }
            set
            {
                vantagePro.AveragePeriod = value;
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
                return vantagePro.CloudCover;
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
                return vantagePro.DewPoint;
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
                return vantagePro.Humidity;
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
                return vantagePro.Pressure;
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
                return vantagePro.RainRate;
            }
        }

        /// <summary>
        /// Forces the driver to immediatley query its attached hardware to refresh sensor
        /// values
        /// </summary>
        public void Refresh()
        {
            vantagePro.Refresh();
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
            return vantagePro.SensorDescription(PropertyName);
        }

        /// <summary>
        /// Sky brightness at the observatory
        /// </summary>
        public double SkyBrightness
        {
            get
            {
                return vantagePro.SkyBrightness;
            }
        }

        /// <summary>
        /// Sky quality at the observatory
        /// </summary>
        public double SkyQuality
        {
            get
            {
                return vantagePro.SkyQuality;
            }
        }

        /// <summary>
        /// Seeing at the observatory
        /// </summary>
        public double StarFWHM
        {
            get
            {
                return vantagePro.StarFWHM;
            }
        }

        /// <summary>
        /// Sky temperature at the observatory in deg C
        /// </summary>
        public double SkyTemperature
        {
            get
            {
                return vantagePro.SkyTemperature;
            }
        }

        /// <summary>
        /// Temperature at the observatory in deg C
        /// </summary>
        public double Temperature
        {
            get
            {
                return vantagePro.Temperature;
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
            return vantagePro.TimeSinceLastUpdate(PropertyName);
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
                return vantagePro.WindDirection;
            }
        }

        /// <summary>
        /// Peak 3 second wind gust at the observatory over the last 2 minutes in m/s
        /// </summary>
        public double WindGust
        {
            get
            {
                return vantagePro.WindGust;
            }
        }

        /// <summary>
        /// Wind speed at the observatory in m/s
        /// </summary>
        public double WindSpeed
        {
            get
            {
                return vantagePro.WindSpeedMps;
            }
        }

        #endregion

        #region private methods

        #region calculate the gust strength as the largest wind recorded over the last two minutes

        // save the time and wind speed values
        private Dictionary<DateTime, double> winds = new Dictionary<DateTime, double>();

        private double gustStrength;

        private void UpdateGusts(double speed)
        {
            Dictionary<DateTime, double> newWinds = new Dictionary<DateTime, double>();
            var last = DateTime.Now - TimeSpan.FromMinutes(2);
            winds.Add(DateTime.Now, speed);
            var gust = 0.0;
            foreach (var item in winds)
            {
                if (item.Key > last)
                {
                    newWinds.Add(item.Key, item.Value);
                    if (item.Value > gust)
                        gust = item.Value;
                }
            }
            gustStrength = gust;
            winds = newWinds;
        }

        #endregion

        private void LogMessage(string identifier, string message, params object[] args)
        {
            tl.LogMessage(identifier, string.Format(message, args));
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development


        public static string driverID = "ASCOM.Wise40.VantagePro.ObservingConditions";
        private static Version version = new Version("0.2");
        //public static string driverDescription = string.Format("ASCOM Wise40.VantagePro v{0}", version.ToString());
        public static string driverDescription = string.Format("ASCOM Wise40.VantagePro v0.2");
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
                P.DeviceType = "ObservingConditions";
                if (bRegister)
                {
                    //P.Register(WiseVantagePro.driverID, WiseVantagePro.DriverDescription);
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    //P.Unregister(WiseVantagePro.driverID);
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
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!vantagePro.Connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        #endregion

    }
}
