//tabs=4
// --------------------------------------------------------------------------------
//
// ASCOM Telescope driver for Wise40
//
// Description:	A Boller and Chivens telescope (wide field Ritchey-Chretien Reflector)
//   mounted on a rigid, off-axis equatorial mount, with:
//   - 40 inch diameter clear aperture f/4 primary mirror
//   - 20.1 inch diameter f/7 Cassegrain secondary mirror
//   - corrector quartz lens located 4 inches above the surface of the primary mirror.
//
// Implements:	ASCOM Telescope interface version: V3
// Author:		(blumzi) Arie Blumenzweig <blumzi@013.net>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 22-Jan-2016	AB	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Telescope

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
using System.Globalization;
using System.Collections;

using ASCOM.Wise40.Common;

namespace ASCOM.Wise40
{
    //
    // Your driver's DeviceID is ASCOM.Wise40.Telescope
    //
    // The Guid attribute sets the CLSID for ASCOM.Wise40.Telescope
    // The ClassInterface/None addribute prevents an empty interface called
    // _Wise40 from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Telescope Driver for Wise40.
    /// </summary>
    [Guid("320779e0-0cf2-47ca-8486-4472c9a0fe5e")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Telescope : ITelescopeV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.Wise40.Telescope";

        public static bool _trace = true;   // TODO: fix profile value

        internal static string debugLevelProfileName = "Debug Level";
        internal static string astrometricAccuracyProfileName = "Astrometric accuracy";
        internal static string traceStateProfileName = "Trace";
        internal static string enslaveDomeProfileName = "Enslave Dome";
        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connected;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util util;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtils;

        private Astrometry.NOVAS.NOVAS31 novas31 = new Astrometry.NOVAS.NOVAS31();

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        public TraceLogger tl;
        
        public HandpadForm handpad;

        private Common.Debugger debugger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            ReadProfile(); // Read device configuration from the ASCOM Profile store
            debugger = new Common.Debugger(WiseTele.Instance.debugger.Level);

            tl = new TraceLogger("", "Tele");
            tl.Enabled = _trace;
            tl.LogMessage("Telescope", "Starting initialisation");

            _connected = false; // Initialise connected to false
            util = new Util(); //Initialise util object
            astroUtils = new AstroUtils(); // Initialise astro utilities object

            WiseTele.Instance.init(this);

            tl.LogMessage("Telescope", "Completed initialisation");
        }

        //
        // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
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
            if (IsConnected && WiseTele.Instance.debugger.Debugging(Common.Debugger.DebugLevel.DebugDevice))
            {
                handpad = new HandpadForm();
                handpad.ShowDialog();
            } else
                using (TelescopeSetupDialogForm F = new TelescopeSetupDialogForm(_trace,
                    WiseTele.Instance.debugger.Level,
                    WiseSite.Instance.astrometricAccuracy,
                    WiseTele.Instance._enslaveDome))
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
                return WiseTele.Instance.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return WiseTele.Instance.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            WiseTele.Instance.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return WiseTele.Instance.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return WiseTele.Instance.CommandString(command, raw);
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;

            util.Dispose();
            util = null;

            astroUtils.Dispose();
            astroUtils = null;

            WiseTele.Instance.Dispose();
        }

        public bool Connected
        {
            get
            {
                _connected = WiseTele.Instance.Connected;
                return _connected;
            }

            set
            {
                WiseTele.Instance.Connected = value;
                _connected = WiseTele.Instance.Connected;
            }
        }

        public string Description
        {
            get
            {
                return WiseTele.Instance.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return WiseTele.Instance.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return WiseTele.Instance.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return WiseTele.Instance.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return WiseTele.Instance.Name;
            }
        }

        #endregion

        #region ITelescope Implementation
        public void AbortSlew()
        {
            WiseTele.Instance.AbortSlew();
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                return WiseTele.Instance.AlignmentMode;
            }
        }

        public double Altitude
        {
            get
            {
                return WiseTele.Instance.Altitude;
            }
        }

        public double ApertureArea
        {
            get
            {
                return WiseTele.Instance.ApertureArea;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                return WiseTele.Instance.ApertureDiameter;
            }
        }

        public bool AtHome
        {
            get
            {
                return WiseTele.Instance.AtHome;
            }
        }

        public bool AtPark
        {
            get
            {
                return WiseTele.Instance.AtPark;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            return WiseTele.Instance.AxisRates(Axis);
        }

        public double Azimuth
        {
            get
            {
                return WiseTele.Instance.Azimuth;
            }
        }

        public bool CanFindHome
        {
            get
            {
                return WiseTele.Instance.CanFindHome;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            return WiseTele.Instance.CanMoveAxis(Axis);
        }

        public bool CanPark
        {
            get
            {
                return WiseTele.Instance.CanPark;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                return WiseTele.Instance.CanPulseGuide;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                return WiseTele.Instance.CanSetDeclinationRate;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                return WiseTele.Instance.CanSetGuideRates;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return WiseTele.Instance.CanSetPark;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                return WiseTele.Instance.CanSetPierSide;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                return WiseTele.Instance.CanSetRightAscensionRate;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                return WiseTele.Instance.CanSetTracking;
            }
        }

        public bool CanSlew
        {
            get
            {
                return WiseTele.Instance.CanSlew;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                return WiseTele.Instance.CanSlewAltAz;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                return WiseTele.Instance.CanSlewAltAzAsync;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                return WiseTele.Instance.CanSlewAsync;
            }
        }

        public bool CanSync
        {
            get
            {
                return WiseTele.Instance.CanSync;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                return WiseTele.Instance.CanSyncAltAz;
            }
        }

        public bool CanUnpark
        {
            get
            {
                return WiseTele.Instance.CanUnpark;
            }
        }

        public double Declination
        {
            get
            {
                return WiseTele.Instance.Declination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                return WiseTele.Instance.DeclinationRate;
            }

            set
            {
                WiseTele.Instance.DeclinationRate = value;
            }
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            return WiseTele.Instance.DestinationSideOfPier(RightAscension, Declination);
        }

        public bool DoesRefraction
        {
            get
            {
                return WiseTele.Instance.doesRefraction;
            }

            set
            {
                WiseTele.Instance.doesRefraction = value;
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                return WiseTele.Instance.EquatorialSystem;
            }
        }

        public void FindHome()
        {
            WiseTele.Instance.FindHome();
        }

        public double FocalLength
        {
            get
            {
                return WiseTele.Instance.FocalLength;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                return WiseTele.Instance.GuideRateDeclination;
            }

            set
            {
                WiseTele.Instance.GuideRateDeclination = value;
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                return WiseTele.Instance.GuideRateRightAscension;
            }

            set
            {
                WiseTele.Instance.GuideRateRightAscension = value;
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                return WiseTele.Instance.IsPulseGuiding;
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            WiseTele.Instance.MoveAxis(Axis, Rate);
        }

        public void Park()
        {
            WiseTele.Instance.Park();
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            WiseTele.Instance.PulseGuide(Direction, Duration);
        }

        public double RightAscension
        {
            get
            {
                return WiseTele.Instance.RightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                return WiseTele.Instance.RightAscensionRate;
            }
            set
            {
                WiseTele.Instance.RightAscensionRate = value;
            }
        }

        public void SetPark()
        {
            WiseTele.Instance.SetPark();
        }

        public PierSide SideOfPier
        {
            get
            {
                return WiseTele.Instance.SideOfPier;
            }
            set
            {
                WiseTele.Instance.SideOfPier = value;
            }
        }

        public double SiderealTime
        {
            get
            {
                return WiseTele.Instance.SiderealTime;
            }
        }

        public double SiteElevation
        {
            get
            {
                return WiseTele.Instance.SiteElevation;
            }

            set
            {
                WiseTele.Instance.SiteElevation = value;
            }
        }

        public double SiteLatitude
        {
            get
            {
                return WiseTele.Instance.SiteLatitude;
            }
            set
            {
                WiseTele.Instance.SiteLatitude = value;
            }
        }

        public double SiteLongitude
        {
            get
            {
                return WiseTele.Instance.SiteLongitude;
            }
            set
            {
                WiseTele.Instance.SiteLongitude = value;
            }
        }

        public short SlewSettleTime
        {
            get
            {
                return WiseTele.Instance.SlewSettleTime;
            }

            set
            {
                WiseTele.Instance.SlewSettleTime = value;
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            WiseTele.Instance.SlewToAltAz(Azimuth, Altitude);
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            WiseTele.Instance.SlewToAltAzAsync(Azimuth, Altitude);
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {

            WiseTele.Instance.SlewToCoordinates(RightAscension, Declination);
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            WiseTele.Instance.SlewToCoordinatesAsync(RightAscension, Declination);
        }

        public void SlewToTarget()
        {
            WiseTele.Instance.SlewToTarget();
        }

        public void SlewToTargetAsync()
        {
            WiseTele.Instance.SlewToTargetAsync();
        }

        public bool Slewing
        {
            get
            {
                return WiseTele.Instance.Slewing;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            WiseTele.Instance.SyncToAltAz(Azimuth, Altitude);
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            WiseTele.Instance.SyncToCoordinates(RightAscension, Declination);
        }

        public void SyncToTarget()
        {
            WiseTele.Instance.SyncToTarget();
        }

        public double TargetDeclination
        {
            get
            {
                return WiseTele.Instance.TargetDeclination;
            }

            set
            {
                WiseTele.Instance.TargetDeclination = value;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                return WiseTele.Instance.TargetRightAscension;
            }

            set
            {
                WiseTele.Instance.TargetRightAscension = value;
            }
        }

        public bool Tracking
        {
            get
            {
                return WiseTele.Instance.Tracking;
            }

            set
            {
                WiseTele.Instance.Tracking = value;
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                return WiseTele.Instance.TrackingRate;
            }

            set
            {
                WiseTele.Instance.TrackingRate = value;
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                return WiseTele.Instance.TrackingRates;
            }
        }

        public DateTime UTCDate
        {
            get
            {
                return WiseTele.Instance.UTCDate;
            }

            set
            {
                WiseTele.Instance.UTCDate = value;
            }
        }

        public void Unpark()
        {
            WiseTele.Instance.Unpark();
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
                P.DeviceType = "Telescope";
                if (bRegister)
                {
                    P.Register(driverID, WiseTele.driverDescription);
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
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                _trace = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, "false"));
                //WiseTele.Instance.debugger.Level = Convert.ToUInt32(driverProfile.GetValue(driverID, debugLevelProfileName, string.Empty, "0"));
                WiseTele.Instance.debugger.Level = (uint)Common.Debugger.DebugLevel.DebugAll;
                WiseSite.Instance.astrometricAccuracy = driverProfile.GetValue(driverID, astrometricAccuracyProfileName, string.Empty, "Full") == "Full" ?
                    Accuracy.Full : Accuracy.Reduced;
                WiseTele.Instance._enslaveDome = Convert.ToBoolean(driverProfile.GetValue(driverID, enslaveDomeProfileName, string.Empty, "false"));
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                driverProfile.WriteValue(driverID, traceStateProfileName, _trace.ToString());
                driverProfile.WriteValue(driverID, astrometricAccuracyProfileName, WiseSite.Instance.astrometricAccuracy == Accuracy.Full ? "Full" : "Reduced");
                driverProfile.WriteValue(driverID, debugLevelProfileName, WiseTele.Instance.debugger.Level.ToString());
                driverProfile.WriteValue(driverID, enslaveDomeProfileName, WiseTele.Instance._enslaveDome.ToString());
            }
        }

        #endregion

    }
}
