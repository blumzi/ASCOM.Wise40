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

namespace ASCOM.Wise40 //.Telescope
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
    [Guid("05CBFDAA-DDA0-4248-9240-288A797CD7B3")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class Telescope : ITelescopeV3, IDisposable
    {
        /// <summary>
        /// Remembers if the ASCOM driver has initiated a SlewXXX or MoveAxis
        /// </summary>
        private bool _driverInitiatedSlew = false;

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

        private Common.Debugger debugger = Common.Debugger.Instance;

        private static WiseTele wisetele = WiseTele.Instance;
        private static WiseSite wisesite = WiseSite.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            _connected = false; // Initialise connected to false
            util = new Util(); //Initialise util object
            astroUtils = new AstroUtils(); // Initialise astro utilities object

            wisetele.init();
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
            using (TelescopeSetupDialogForm F = new TelescopeSetupDialogForm())
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
                return wisetele.SupportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            return wisetele.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw)
        {
            wisetele.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return wisetele.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw)
        {
            return wisetele.CommandString(command, raw);
        }

        public void Dispose()
        {
            util.Dispose();
            util = null;

            astroUtils.Dispose();
            astroUtils = null;

            wisetele.Dispose();
        }

        public bool Connected
        {
            get
            {
                _connected = wisetele.Connected;
                return _connected;
            }

            set
            {
                wisetele.Connected = value;
                _connected = wisetele.Connected;
            }
        }

        public string Description
        {
            get
            {
                return wisetele.Description;
            }
        }

        public string DriverInfo
        {
            get
            {
                return wisetele.DriverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                return wisetele.DriverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                return wisetele.InterfaceVersion;
            }
        }

        public string Name
        {
            get
            {
                return wisetele.WiseName;
            }
        }

        #endregion

        #region ITelescope Implementation
        public void AbortSlew()
        {
            wisetele.AbortSlew("ASCOM AbortSlew");
            _driverInitiatedSlew = false;
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                return wisetele.AlignmentMode;
            }
        }

        public double Altitude
        {
            get
            {
                return wisetele.Altitude;
            }
        }

        public double ApertureArea
        {
            get
            {
                return wisetele.ApertureArea;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                return wisetele.ApertureDiameter;
            }
        }

        public bool AtHome
        {
            get
            {
                return wisetele.AtHome;
            }
        }

        public bool AtPark
        {
            get
            {
                return wisetele.AtPark;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            return wisetele.AxisRates(Axis);
        }

        public double Azimuth
        {
            get
            {
                return wisetele.Azimuth;
            }
        }

        public bool CanFindHome
        {
            get
            {
                return wisetele.CanFindHome;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            return wisetele.CanMoveAxis(Axis);
        }

        public bool CanPark
        {
            get
            {
                return wisetele.CanPark;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                return wisetele.CanPulseGuide;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                return wisetele.CanSetDeclinationRate;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                return wisetele.CanSetGuideRates;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return wisetele.CanSetPark;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                return wisetele.CanSetPierSide;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                return wisetele.CanSetRightAscensionRate;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                return wisetele.CanSetTracking;
            }
        }

        public bool CanSlew
        {
            get
            {
                return wisetele.CanSlew;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                return wisetele.CanSlewAltAz;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                return wisetele.CanSlewAltAzAsync;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                return wisetele.CanSlewAsync;
            }
        }

        public bool CanSync
        {
            get
            {
                return wisetele.CanSync;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                return wisetele.CanSyncAltAz;
            }
        }

        public bool CanUnpark
        {
            get
            {
                return wisetele.CanUnpark;
            }
        }

        public double Declination
        {
            get
            {
                return wisetele.Declination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                return wisetele.DeclinationRate;
            }

            set
            {
                wisetele.DeclinationRate = value;
            }
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            return wisetele.DestinationSideOfPier(RightAscension, Declination);
        }

        public bool DoesRefraction
        {
            get
            {
                return wisetele.doesRefraction;
            }

            set
            {
                wisetele.doesRefraction = value;
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                return wisetele.EquatorialSystem;
            }
        }

        public void FindHome()
        {
            wisetele.FindHome();
        }

        public double FocalLength
        {
            get
            {
                return wisetele.FocalLength;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                return wisetele.GuideRateDeclination;
            }

            set
            {
                wisetele.GuideRateDeclination = value;
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                return wisetele.GuideRateRightAscension;
            }

            set
            {
                wisetele.GuideRateRightAscension = value;
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                return wisetele.IsPulseGuiding;
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            if (Rate == Const.rateStopped)
            {
                _driverInitiatedSlew = false;
                wisetele.MoveAxis(Axis, Rate);
                return;
            }

            try
            {
                _driverInitiatedSlew = true;
                wisetele.MoveAxis(Axis, Rate);
            } catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void Park()
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            wisetele.Park();
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            wisetele.PulseGuide(Direction, Duration);
        }

        public double RightAscension
        {
            get
            {
                return wisetele.RightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                return wisetele.RightAscensionRate;
            }

            set
            {
                wisetele.RightAscensionRate = value;
            }
        }

        public void SetPark()
        {
            wisetele.SetPark();
        }

        public PierSide SideOfPier
        {
            get
            {
                return wisetele.SideOfPier;
            }

            set
            {
                wisetele.SideOfPier = value;
            }
        }

        public double SiderealTime
        {
            get
            {
                return wisetele.SiderealTime;
            }
        }

        public double SiteElevation
        {
            get
            {
                return wisetele.SiteElevation;
            }

            set
            {
                wisetele.SiteElevation = value;
            }
        }

        public double SiteLatitude
        {
            get
            {
                return wisetele.SiteLatitude;
            }
            set
            {
                wisetele.SiteLatitude = value;
            }
        }

        public double SiteLongitude
        {
            get
            {
                return wisetele.SiteLongitude;
            }
            set
            {
                wisetele.SiteLongitude = value;
            }
        }

        public short SlewSettleTime
        {
            get
            {
                return wisetele.SlewSettleTime;
            }

            set
            {
                wisetele.SlewSettleTime = value;
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToAltAz(Azimuth, Altitude);
            } catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToAltAzAsync(Azimuth, Altitude);
            }
            catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToCoordinates(RightAscension, Declination);
            }
            catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToCoordinatesAsync(RightAscension, Declination);
            }
            catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void SlewToTarget()
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToTarget();
            }
            catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public void SlewToTargetAsync()
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            try
            {
                _driverInitiatedSlew = true;
                wisetele.SlewToTargetAsync();
            }
            catch
            {
                _driverInitiatedSlew = false;
                throw;
            }
            _driverInitiatedSlew = false;
        }

        public bool Slewing
        {
            get
            {
                return _driverInitiatedSlew || wisetele.Slewing;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            wisetele.SyncToAltAz(Azimuth, Altitude);
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            wisetele.SyncToCoordinates(RightAscension, Declination);
        }

        public void SyncToTarget()
        {
            wisetele.SyncToTarget();
        }

        public double TargetDeclination
        {
            get
            {
                return wisetele.TargetDeclination;
            }

            set
            {
                if (wisetele.ShuttingDown)
                    throw new InvalidOperationException("Wise40 is shutting down");

                wisetele.TargetDeclination = value;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                return wisetele.TargetRightAscension;
            }

            set
            {
                if (wisetele.ShuttingDown)
                    throw new InvalidOperationException("Wise40 is shutting down");

                wisetele.TargetRightAscension = value;
            }
        }

        public bool Tracking
        {
            get
            {
                return wisetele.Tracking;
            }

            set
            {
                if (wisetele.ShuttingDown)
                    throw new InvalidOperationException("Wise40 is shutting down");

                wisetele.Tracking = value;
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                return wisetele.TrackingRate;
            }

            set
            {
                wisetele.TrackingRate = value;
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                return wisetele.TrackingRates;
            }
        }

        public DateTime UTCDate
        {
            get
            {
                return wisetele.UTCDate;
            }

            set
            {
                wisetele.UTCDate = value;
            }
        }

        public void Unpark()
        {
            if (wisetele.ShuttingDown)
                throw new InvalidOperationException("Wise40 is shutting down");

            wisetele.Unpark();
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
                    P.Register(Const.WiseDriverID.Telescope, WiseTele.driverDescription);
                }
                else
                {
                    P.Unregister(Const.WiseDriverID.Telescope);
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
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            WiseSite.OpMode opMode = WiseSite.OperationalMode;

            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Telescope";
                switch (opMode)
                {
                    case WiseSite.OpMode.LCO:
                        WiseTele._enslaveDome = true;
                        break;
                    case WiseSite.OpMode.WISE:
                        WiseTele._enslaveDome = Convert.ToBoolean(driverProfile.GetValue(Const.WiseDriverID.Telescope, Const.ProfileName.Telescope_EnslaveDome, string.Empty, "false"));
                        break;
                    case WiseSite.OpMode.ACP:
                        WiseTele._enslaveDome = false;
                        break;
                }
                WiseSite.astrometricAccuracy = 
                    driverProfile.GetValue(Const.WiseDriverID.Telescope, Const.ProfileName.Telescope_AstrometricAccuracy, string.Empty, "Full") == "Full" ?
                        Accuracy.Full :
                        Accuracy.Reduced;
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
                driverProfile.WriteValue(Const.WiseDriverID.Telescope, Const.ProfileName.Telescope_AstrometricAccuracy, WiseSite.astrometricAccuracy == Accuracy.Full ? "Full" : "Reduced");
                driverProfile.WriteValue(Const.WiseDriverID.Telescope, Const.ProfileName.Telescope_EnslaveDome, WiseTele._enslaveDome.ToString());
                driverProfile.WriteValue(Const.WiseDriverID.Telescope, Const.ProfileName.Telescope_CalculateRefraction, WiseTele._calculateRefraction.ToString());
            }
        }

        #endregion

    }
}
