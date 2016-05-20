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

using ASCOM.Wise40.Hardware;

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
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "Wise40 Telescope";

        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static bool traceState;

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util util;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtils;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger tl;
        
        public HandpadForm handpad;

        private bool driverInitiatedSlewing = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wise40"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl = new TraceLogger("", "Tele");
            tl.Enabled = true; //  traceState;
            tl.LogMessage("Telescope", "Starting initialisation");

            connectedState = false; // Initialise connected to false
            util = new Util(); //Initialise util object
            astroUtils = new AstroUtils(); // Initialise astro utilities object

            WiseTele.Instance.init(tl);
            //gui = new Thread(new ThreadStart(handpad.Show));

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
            if (IsConnected)
            {
                handpad = new HandpadForm();
                handpad.ShowDialog();
            } else
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
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
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
                tl.LogMessage("Connected Get", IsConnected.ToString());
                return IsConnected;
            }
            set
            {
                tl.LogMessage("Connected Set", value.ToString());
                if (value == IsConnected)
                    return;

                WiseTele.Instance.Connect(value);
                connectedState = value;
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
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
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
                tl.LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                string name = "Wise40 Telescope";
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ITelescope Implementation
        public void AbortSlew()
        {
            if (AtPark)
                throw new InvalidOperationException("Cannot AbortSlew while AtPark");

            if (!driverInitiatedSlewing)
                return;

            tl.LogMessage("AbortSlew", "");
            WiseTele.Instance.abortSlew();
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                AlignmentModes mode = AlignmentModes.algPolar;
                
                tl.LogMessage("AlignmentMode Get", mode.ToString());
                return mode;
            }
        }

        public double Altitude
        {
            get
            {
                double alt = WiseTele.Instance.Altitude;

                tl.LogMessage("Altitude Get", alt.ToString());
                return alt; ;
            }
        }

        public double ApertureArea
        {
            get
            {
                double area = WiseTele.Instance.mirrorArea;

                tl.LogMessage("ApertureArea Get", area.ToString());
                return area;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                double diam = WiseTele.Instance.mirrorDiam;

                tl.LogMessage("ApertureDiameter Get", diam.ToString());
                return diam;
            }
        }

        public bool AtHome
        {
            get
            {
                bool ret = false;       // Homing is not implemented
                tl.LogMessage("AtHome", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool AtPark
        {
            get
            {
                bool ret = WiseTele.Instance.AtPark;

                tl.LogMessage("AtPark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            IAxisRates rates = new AxisRates(Axis);

            tl.LogMessage("AxisRates", "Get - " + rates.ToString());
            return rates;
        }

        public double Azimuth
        {
            get
            {
                double az = WiseTele.Instance.Azimuth;

                tl.LogMessage("Azimuth Get", az.ToString());
                return az;
            }
        }

        public bool CanFindHome
        {
            get
            {
                tl.LogMessage("CanFindHome", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            bool ret;

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary:   ret = true; break ;   // Right Ascension
                case TelescopeAxes.axisSecondary: ret = true; break ;   // Declination
                case TelescopeAxes.axisTertiary:  ret = false; break ;  // Image Rotator/Derotator
                default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
            }
            tl.LogMessage("CanMoveAxis", "Get - " + Axis.ToString() + ": " + ret.ToString());

            return ret;
        }

        public bool CanPark
        {
            get
            {
                tl.LogMessage("CanPark", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                tl.LogMessage("CanPulseGuide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                tl.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                tl.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                bool ret = false;

                tl.LogMessage("CanSetPark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                tl.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                tl.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                tl.LogMessage("CanSetTracking", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                tl.LogMessage("CanSlew", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                tl.LogMessage("CanSlewAltAz", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                tl.LogMessage("CanSlewAltAzAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                tl.LogMessage("CanSlewAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                tl.LogMessage("CanSync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                tl.LogMessage("CanSyncAltAz", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanUnpark
        {
            get
            {
                tl.LogMessage("CanUnpark", "Get - " + true.ToString());
                return true;
            }
        }

        public double Declination
        {
            get
            {
                double declination = WiseTele.Instance.Declination;

                tl.LogMessage("Declination", "Get - " + util.DegreesToDMS(declination, ":", ":"));
                return declination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                double decRate = WiseTele.Instance.DeclinationRate;

                tl.LogMessage("DeclinationRate", "Get - " + decRate.ToString());
                return decRate;
            }

            set
            {
                tl.LogMessage("DeclinationRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            tl.LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
        }

        public bool DoesRefraction
        {
            get
            {
                bool doesRefraction = WiseTele.Instance.doesRefraction;

                tl.LogMessage("DoesRefraction Get", doesRefraction.ToString());
                return doesRefraction;
            }

            set
            {
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction");
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equJ2000;

                tl.LogMessage("EquatorialSystem", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            tl.LogMessage("FindHome", "Not Implemented");
            throw new MethodNotImplementedException("FindHome");
        }

        public double FocalLength
        {
            get
            {
                double fl = WiseTele.Instance.focalLength;

                tl.LogMessage("FocalLength Get", fl.ToString());
                return fl;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                tl.LogMessage("GuideRateDeclination Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
            }
            set
            {
                tl.LogMessage("GuideRateDeclination Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                tl.LogMessage("GuideRateRightAscension Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
            }
            set
            {
                tl.LogMessage("GuideRateRightAscension Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                throw new PropertyNotImplementedException("PulseGuide not supported");
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            WiseTele.Instance.MoveAxis(Axis, Rate, (Rate < 0) ? WiseTele.AxisDirection.Decreasing : WiseTele.AxisDirection.Increasing);
        }

        public void Park()
        {
            tl.LogMessage("Park", "");

            WiseTele.Instance.Park();
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            tl.LogMessage("PulseGuide", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("PulseGuide");
        }

        public double RightAscension
        {
            get
            {
                double rightAscension = WiseTele.Instance.RightAscension;

                tl.LogMessage("RightAscension", "Get - " + util.HoursToHMS(rightAscension));
                return rightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                tl.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                return rightAscensionRate;
            }
            set
            {
                tl.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            tl.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                PierSide side = PierSide.pierEast;  // TBD

                tl.LogMessage("SideOfPier Get", side.ToString());
                return side;
            }
            set
            {
                tl.LogMessage("SideOfPier Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public double SiderealTime
        {
            get
            {
                double st = WiseSite.Instance.LocalSiderealTime;

                tl.LogMessage("SiderealTime", "Get - " + st.ToString());
                return st;
            }
        }

        public double SiteElevation
        {
            get
            {
                double elevation = WiseSite.Instance.Elevation;

                tl.LogMessage("SiteElevation Get", elevation.ToString());
                return elevation;
            }
            set
            {
                tl.LogMessage("SiteElevation Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                double latitude = WiseSite.Instance.Latitude;

                tl.LogMessage("SiteLatitude Get", latitude.ToString());
                return latitude;
            }
            set
            {
                tl.LogMessage("SiteLatitude Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
            }
        }

        public double SiteLongitude
        {
            get
            {
                double longitude = WiseSite.Instance.Longitude;

                tl.LogMessage("SiteLongitude Get", longitude.ToString());
                return longitude;
            }
            set
            {
                tl.LogMessage("SiteLongitude Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
            }
        }

        public short SlewSettleTime
        {
            get
            {
                tl.LogMessage("SlewSettleTime Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }
            set
            {
                tl.LogMessage("SlewSettleTime Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            tl.LogMessage("SlewToAltAz", String.Format("az: {0}, alt: {1}\n", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            tl.LogMessage("SlewToAltAzAsync", String.Format("az: {0}, alt: {1}\n", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!WiseTele.Instance.SafeAtCoordinates(RightAscension, Declination))
                throw new InvalidValueException("Not safe to SlewToCoordinates({0}, {1})", RightAscension.ToString(), Declination.ToString());

            tl.LogMessage("SlewToCoordinates", string.Format("ra: {0}, dec: {0}", RightAscension, Declination));
            WiseTele.Instance.SlewToCoordinatesSync(RightAscension, Declination);
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!WiseTele.Instance.SafeAtCoordinates(RightAscension, Declination))
                throw new InvalidValueException("Not safe to SlewToCoordinatesAsync({0}, {1})", RightAscension.ToString(), Declination.ToString());

            tl.LogMessage("SlewToCoordinatesAsync", string.Format("ra: {0}, dec: {0}", RightAscension, Declination));
            WiseTele.Instance.SlewToCoordinatesAsync(RightAscension, Declination);
        }

        public void SlewToTarget()
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!WiseTele.Instance.SafeAtCoordinates(RightAscension, Declination))
                throw new InvalidValueException("Not safe to SlewToTarget({0}, {1})", TargetRightAscension.ToString(), TargetDeclination.ToString());

            tl.LogMessage("SlewToTarget", string.Format("ra: {0}, dec: {0}", TargetRightAscension, TargetDeclination));
            WiseTele.Instance.SlewToCoordinatesSync(TargetRightAscension, TargetDeclination);
        }

        public void SlewToTargetAsync()
        {
            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while NOT Tracking");

            if (!WiseTele.Instance.SafeAtCoordinates(RightAscension, Declination))
                throw new InvalidValueException("Not safe to SlewToTargetAsync({0}, {1})", TargetRightAscension.ToString(), TargetDeclination.ToString());

            driverInitiatedSlewing = true;
            tl.LogMessage("SlewToTargetAsync", "Started");
            WiseTele.Instance.SlewToTargetAsync();
        }

        public bool Slewing
        {
            get
            {
                bool slewing = WiseTele.Instance.Slewing;

                tl.LogMessage("Slewing Get", slewing.ToString());
                return slewing;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            tl.LogMessage("SyncToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            tl.LogMessage("SyncToCoordinates", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
        }

        public void SyncToTarget()
        {
            tl.LogMessage("SyncToTarget", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToTarget");
        }

        public double TargetDeclination
        {
            get
            {
                return WiseTele.Instance.targetDeclination;
            }
            set
            {
                WiseTele.Instance.targetDeclination = value;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                return WiseTele.Instance.targetRightAscension;
            }
            set
            {
                WiseTele.Instance.targetRightAscension = value;
            }
        }

        public bool Tracking
        {
            get
            {
                bool tracking = WiseTele.Instance.Tracking;

                tl.LogMessage("Tracking", "Get - " + tracking.ToString());
                return tracking;
            }
            set
            {
                WiseTele.Instance.Tracking = value;

                tl.LogMessage("Tracking Set", WiseTele.Instance.Tracking.ToString());
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                DriveRates rates = WiseTele.Instance.TrackingRates;

                tl.LogMessage("TrackingRate Get - ", rates.ToString());
                return rates;
            }

            set
            {
                tl.LogMessage("TrackingRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                tl.LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    tl.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        public DateTime UTCDate
        {
            get
            {
                DateTime utcDate = DateTime.UtcNow;
                tl.LogMessage("UTCDate Get - ", utcDate.ToString());
                return utcDate;
            }
            set
            {
                tl.LogMessage("UTCDate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
            }
        }

        public void Unpark()
        {
            if (WiseTele.Instance.AtPark)
                WiseTele.Instance.AtPark = false;

            tl.LogMessage("Unpark", "Done");
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
                return connectedState;
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
                traceState = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
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
                driverProfile.WriteValue(driverID, traceStateProfileName, traceState.ToString());
                driverProfile.WriteValue(driverID, "Astrometric accuracy", WiseSite.Instance.astrometricAccuracy == Accuracy.Full ? "Full" : "Reduced");
            }
        }

        #endregion

    }
}
