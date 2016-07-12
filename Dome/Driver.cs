//tabs=4
// --------------------------------------------------------------------------------
//
// ASCOM Dome driver for Wise40
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Dome interface version: <To be completed by driver developer>
// Author:		(blumzi) Arie Blumenzweig <blumzi@013.net>
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
#define Dome

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System.Globalization;
using System.Collections;

using ASCOM.Wise40.Hardware;
using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.Dome
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.Dome
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40 from being created and used as the [default] interface
    //

    /// <summary>
    /// ASCOM Dome Driver for Wise40.
    /// </summary>
    [Guid("5cec8f8d-f8be-453d-b80f-9a93a758d08a")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Dome : IDomeV2, IDisposable
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Wise40.Dome";
        /// <summary>
        /// Dome driver for the Wise 40" telescope.
        /// </summary>
        private static string driverDescription = "Wise40 Dome";

        internal static string traceStateProfileName = "Trace Level";
        internal static string debugLevelProfileName = "Debug Level";

        public bool traceState;
        public Common.Debugger debugger;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        public static TraceLogger tl;

        private  WiseDome wisedome = WiseDome.Instance;

        public AutoResetEvent arrived = new AutoResetEvent(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40Hardware"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Dome()
        {
            debugger = new Common.Debugger();
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Dome");
            tl.Enabled = traceState;
            tl.LogMessage("Dome", "Starting initialisation");
            
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object

            wisedome.init(arrived);

            tl.LogMessage("Dome", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE IDomeV2 IMPLEMENTATION
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
            if (wisedome.Connected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(this))
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
                return wisedome.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return wisedome.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            wisedome.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return wisedome.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return wisedome.CommandString(command, raw);
        }

        public void Dispose()
        {
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
            wisedome.Dispose();
            wisedome = null;
        }

        public bool Connected
        {
            get
            {
                return wisedome.Connected;
            }

            set
            {
                wisedome.Connected = value;
            }
        }

        public string Description
        {
            get
            {
                return wisedome.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return wisedome.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return wisedome.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return wisedome.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return wisedome.Name;
            }
        }

        #endregion

        #region IDome Implementation

        public void AbortSlew()
        {
            wisedome.AbortSlew();
        }

        public double Altitude
        {
            get
            {
                return wisedome.Altitude;
            }
        }

        public bool AtHome
        {
            get
            {
                return wisedome.AtHome;
            }
        }

        public bool AtPark
        {
            get
            {
                return wisedome.AtPark;
            }
        }

        public double Azimuth
        {
            get
            {
                double ret = wisedome.Azimuth.Degrees;

                tl.LogMessage("Azimuth Get", ret.ToString());
                return ret;
            }
        }

        public bool CanFindHome
        {
            get
            {
                return wisedome.CanFindHome;
            }
        }

        public bool CanPark
        {
            get
            {
                return wisedome.CanPark;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                return wisedome.CanSetAltitude;
            }
        }

        public bool CanSetAzimuth
        {
            get
            {
                return wisedome.CanSetAzimuth;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return wisedome.CanSetPark;
            }
        }

        public bool CanSetShutter
        {
            get
            {
                return wisedome.CanSetShutter;
            }
        }

        public bool CanSlave
        {
            get
            {
                return wisedome.CanSlave;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                return wisedome.CanSyncAzimuth;
            }
        }

        public void CloseShutter()
        {
            wisedome.CloseShutter();
        }

        public void FindHome()
        {
            wisedome.FindHome();
        }

        public void OpenShutter()
        {
            wisedome.OpenShutter();
        }

        public void Park()
        {
            wisedome.Park();
        }

        public void SetPark()
        {
            wisedome.SetPark();
        }

        public ShutterState ShutterStatus
        {
            get
            {
                return wisedome.ShutterStatus;
            }
        }

        public bool Slaved
        {
            get
            {
                return wisedome.Slaved;
            }

            set
            {
                wisedome.Slaved = value;
            }
        }

        public void SlewToAltitude(double Altitude)
        {
            wisedome.SlewToAltitude(Altitude);
        }

        public void SlewToAzimuth(double Azimuth)
        {
            wisedome.SlewToAzimuth(Azimuth);
        }

        public bool Slewing
        {
            get
            {
                return wisedome.Slewing;
            }
        }

        public void SyncToAzimuth(double degrees)
        {
            wisedome.SyncToAzimuth(degrees);
        }

        public uint debugLevel
        {
            get
            {
                return debugger.Level;
            }

            set
            {
                debugger.Level = value;
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
                P.DeviceType = "Dome";
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
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, "false"));
                debugger.Level = Convert.ToUInt32(driverProfile.GetValue(driverID, debugLevelProfileName, string.Empty, "0"));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Dome";
                driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString());
                driverProfile.WriteValue(driverID, debugLevelProfileName, debugger.Level.ToString());
            }
        }

        #endregion

    }
}
