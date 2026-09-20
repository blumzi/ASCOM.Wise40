using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;     // parsing the calibration-point action's parameter
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.Wise40SafeToOperate;
using ASCOM.DeviceInterface;

using MccDaq;
using ASCOM.Wise40;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Newtonsoft.Json;

/// <summary>
/// <para>
/// From the Las Campanas web site (http://www.lco.cl/telescopes-information/henrietta-swope) for the Swope telescope,
///   an identical twin to the Wise40 telescope:
/// </para>
/// <para>
/// The Swope telescope was built by the Boller and Chivens Division of the Perkin-Elmer Corp
/// The optical characteristics are discussed in detail by Bowen and Vaughen (1973, Applied Optics, 12, 1430).
/// The optical design is an f/7 Ritchey-Chrétien in which the radii of curvature of the primary and secondary are equal,
/// thereby achieving a zero Petzval sum and a flat field. Astigmatism is eliminated with a Gascoigne corrector lens.
/// This design achieves a well-corrected field about 3 degrees in diameter. However, to do this it was necessary to
///  use a secondary one-half the diameter of the primary, thereby intercepting 25% of the incident light.
/// </para>
/// <para>An f/13.5 secondary used for infrared imaging is also available through a top-end "flip".</para>
/// <para>2. Optical Design</para>
/// <para>The following table gives the optical specifications of the f/7 Cassegrain configuration.</para>
/// <para>
///     Diameter primary:	                            1,016 mm
///     Focal length primary:	                        4,118 mm
///     Focal length Cassegrain:	                    7,112 mm
///     Diameter hole in primary:	                      386 mm
///     Diameter secondary:	                              508 mm
///     Diameter corrector plate:	                      386 mm
///     Distance between mirrors:	                    2,384 mm
///     Focal point distance behind surface of primary:	  610 mm
///     Radius of curvature of focal surface:	              infinity
///     Unvignetted/corrected field of view:	         1.92 degrees
///     Vignetting @ 88 arcmin field angle:	                5 %
///     Scale:	                                       0.0345 mm/arcsec
/// </para>
///
/// </summary>
namespace ASCOM.Wise40
{
    public class WiseTele : WiseObject, IDisposable, IConnectable
    {
        public enum OnIdle {ShutDown, HunkerDown}
        private OnIdle _onIdle = OnIdle.HunkerDown;

        private bool _hunkeringDown = false;
        private bool _hunkeringUp = false;
        private bool _hunkeredDown = false;

        public enum EncodersInUseEnum { Old, New };
        public EncodersInUseEnum encodersInUse;
        private static readonly Version version = new Version(0, 2);
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public static string driverDescription = $"Wise40 Telescope v{version}";

        private readonly SafeAstroutils safeAstroUtils = new SafeAstroutils();

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        public static Debugger debugger = Debugger.Instance;
        public static readonly Exceptor Exceptor = new Exceptor(Debugger.DebugLevel.DebugTele);

        private bool _connected = false;

        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        private const int waitForOtherAxisMillis = 500;           // half a second between checks setting an axis rate

        //
        // How long a reversal of the distance-to-target must PERSIST before the slewer
        //  believes it.  See the ChangedDirection test in ScopeAxisSlewer.
        //
        // It is a duration and not an iteration count on purpose.  CurrentPosition()
        //  returns primaryAxisMonitor.RightAscension, a field the monitor refreshes
        //  about every 59ms, while the slewer loop polls every 10ms - so one bad
        //  reading is handed to roughly six consecutive iterations.  Counting
        //  iterations would confirm nothing; the test has to outlast the cache.
        //
        // 150ms spans at least two independent encoder reads even with the jitter we
        //  see in the sampling cadence (nominal 50ms, measured 59-62ms).
        //
        private const int directionChangeConfirmMillis = 150;

        //
        // How long the distance to target must keep growing before the slew is abandoned.  See
        //  the diverging check in ScopeAxisSlewer.
        //
        // The magnitude threshold there does most of the work, so this only has to outlast a
        //  spurious encoder reading - the same reasoning as directionChangeConfirmMillis above,
        //  and the same 150ms for the same reason: CurrentPosition is a cached field refreshed
        //  about every 59ms, so one bad value reaches several consecutive iterations.
        //
        private const int divergingConfirmMillis = 150;

        public static ManualResetEvent endOfAsyncSlewEvent = null;

        private string _reasonsForSlewing;

        #region TrackingRestoration
        /// <summary>
        /// Remembers the Tracking state when MoveAxis instance(s) are activated.
        /// When no more MoveAxis instance(s) are active, it restores the remembered Tracking stat.e
        /// </summary>
        private class TrackingRestorer
        {
            private bool _wasTracking;
            private bool _savedTrackingState = false;
            private long _axisMovers;

            public TrackingRestorer()
            {
                Interlocked.Exchange(ref _axisMovers, 0);
            }

            public void AddMover()
            {
                long current = Interlocked.Increment(ref _axisMovers);
                #region debug
                string dbg = $"TrackingRestorer:AddMover:  current: {current}";
                #endregion
                if (current == 1)
                {
                    _wasTracking = Instance.Tracking;
                    _savedTrackingState = true;
                    #region debug
                    dbg += $" remembering _wasTracking: {_wasTracking}";
                    #endregion
                }
                #region debug
               debugger.WriteLine(Debugger.DebugLevel.DebugTele, dbg);
                #endregion
            }

            public void RemoveMover()
            {
                long current = Interlocked.Read(ref _axisMovers);
                #region debug
                string dbg = $"TrackingRestorer:RemoveMover:  current: {current}";
                #endregion
                if (current > 0)
                {
                    current = Interlocked.Decrement(ref _axisMovers);
                    if (current == 0 && _savedTrackingState)
                    {
                        Instance.Tracking = _wasTracking;
                        #region debug
                        dbg += $" restored Tracking to {_wasTracking}";
                        #endregion
                    }
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, dbg);
                #endregion
            }
        };
        private TrackingRestorer _trackingRestorer;
        #endregion

        public List<WiseVirtualMotor> directionMotors, allMotors;
        public Dictionary<TelescopeAxes, List<WiseVirtualMotor>> axisMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        //public static readonly RenishawEncoder renishawHaEncoder = new RenishawEncoder(RenishawEncoder.Module.Ha);
        //public static readonly RenishawEncoder renishawDecEncoder = new RenishawEncoder(RenishawEncoder.Module.Dec);
        public static readonly RenishawHAEncoder renishawHaEncoder = new RenishawHAEncoder();
        public static readonly RenishawDecEncoder renishawDecEncoder = new RenishawDecEncoder();

        public WisePin TrackPin;
        private WisePin SlewPin;
        private WisePin NorthGuidePin, SouthGuidePin, EastGuidePin, WestGuidePin;   // Guide motor activation pins
        private WisePin NorthPin, SouthPin, EastPin, WestPin;                       // Set and Slew motors activation pins
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackingMotor;
        private bool _syncingDomePosition = false;

        private static bool _atPark;

        public bool RecoveringSafety { get; set; } = false;

        private Angle _targetRightAscension, _targetHourAngle, _targetDeclination;
        private Angle _targetAltitude, _targetAzimuth;

        // See TargetCoordinateType: recorded where the request arrives, never inferred later.
        private TargetCoordinateType _targetType = TargetCoordinateType.None;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly List<TelescopeAxes> axes = new List<TelescopeAxes> { TelescopeAxes.axisPrimary, TelescopeAxes.axisSecondary };

        public object _primaryEncoderLock = new object(), _secondaryEncoderLock = new object();

        private static readonly WiseSite wisesite = WiseSite.Instance;

        private readonly ReadyToSlewFlags readyToSlewFlags = ReadyToSlewFlags.Instance;

        private System.Threading.Timer trackingTimer;
        private const int trackingDomeAdjustmentInterval = 30 * 1000;   // half a minute

        public Angle parkingDeclination;

        /// <summary>
        /// <para>
        /// Usually two or three tasks are used to perform a slew:
        /// - if the dome is slaved, a dome slewer
        /// - an axisPrimary slewer
        /// - an axisSecondary slewer
        /// </para>
        /// <para>
        /// An asynchronous slew just fires the tasks.
        /// A synchronous slew waits on the whole list to complete.
        /// </para>
        /// </summary>
        public struct SlewerTask
        {
            public Slewers.Type type;
            public Task task;

            public override string ToString()
            {
                return type.ToString();
            }
        }

        private CancellationTokenSource telescopeCTS = new CancellationTokenSource();
        private CancellationToken telescopeCT;

        private CancellationTokenSource domeCTS = new CancellationTokenSource();
        private CancellationToken domeCT;

        public Slewers slewers = Slewers.Instance;
        public Pulsing pulsing = Pulsing.Instance;

        private static PrimaryAxisMonitor primaryAxisMonitor;
        private static SecondaryAxisMonitor secondaryAxisMonitor;

        public double _lastTrackingLST;

        public static readonly Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>> axisDirectionName =
            new Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>>() {
                {
                    TelescopeAxes.axisPrimary, new Dictionary<Const.AxisDirection, string>()
                        {
                            { Const.AxisDirection.Increasing, "East" },
                            { Const.AxisDirection.Decreasing, "West" },
                        }
                },
                {
                    TelescopeAxes.axisSecondary, new Dictionary<Const.AxisDirection, string>()
                        {
                            { Const.AxisDirection.Increasing, "North" },
                            { Const.AxisDirection.Decreasing, "South" },
                        }
                },
            };

        private readonly Hardware.Hardware hardware = Hardware.Hardware.Instance;
        internal static string driverID = Const.WiseDriverID.Telescope;

        private const int defaultPollingFreqMillis = 10;
        public class MovementParameters
        {
            public Angle minimalMovement;
            public Angle maximalMovement;
            public Angle stopMovement;

            //
            // Optional, primary axis only: the coast to allow when the axis is moving
            //  in the Increasing direction (East on axisPrimary).  Null means "this rate
            //  coasts the same both ways", which is how every other entry behaves.
            //
            // The HA axis does NOT coast symmetrically.  Measured 2026-09-18 at slew rate,
            //  both legs entering within 2% of the same speed:
            //
            //      west  HA 2.84 -> 3.03   2.824 deg      west  HA 3.85   2.699 deg
            //      east  HA 3.04 -> 2.90   1.997 deg      east  HA 1.61   2.002 deg
            //
            // The 07:43 east leg was run deliberately in the same hour-angle window as
            //  the 06:44 west leg to separate the two explanations: a 41% difference
            //  survived at identical HA, so this is direction, not a polar-axis imbalance
            //  whose torque would vary as sin(HA).  Eastward coast is also far steadier
            //  than westward - 2.002 and 1.997 deg across 1.4h of hour angle.
            //
            public Angle stopMovementIncreasing;
            //
            // Dead.  Set for every rate here but read nowhere - the only references are
            //  commented out, in ScopeAxisSlewer.  They are the vestige of an earlier attempt
            //  at exactly what stoppedWindowSeconds/stoppedArcsec below now do.  Left in place
            //  rather than removed, because the values are measured data worth keeping, but do
            //  not mistake them for something in use.
            //
            public double minRadChangePerPollingInterval;
            public double maxRadChangePerPollingInterval;

            //
            // When this axis, having last been driven at this rate, counts as STOPPED:
            //  displacement of no more than stoppedArcsec over the last stoppedWindowSeconds.
            //  Zero means "not specified" and AxisMonitor falls back to its finest criterion.
            //
            // Why displacement over a window instead of a per-sample threshold, and why the
            //  value has to differ per rate: at the measured 62ms sampling interval an axis
            //  travels 429 arcsec per sample at slew rate, 3.2 at set and 0.043 at guide -
            //  about 10000:1.  The single primaryEpsilon of 0.400 arcsec/sample sat 1073x
            //  below slew, 8x below set and 9x ABOVE guide, so guide-rate motion was invisible:
            //  a guide leg that ran 20.11s and covered 16.5 arcsec reported not-moving
            //  throughout, and every "stopping distance: 00h00m00.0s" logged at rateGuide is
            //  that artefact rather than a measurement.
            //
            // Lowering the threshold could not have fixed it.  Guide-rate motion is 0.043
            //  arcsec per sample against an encoder quantum of 0.1187 - under half a count -
            //  so the signal is below quantisation, not below the threshold.  The window has
            //  to be long enough for real motion to accumulate past the quantum, which is why
            //  guide gets 2 seconds where slew gets half of one.
            //
            // Plain doubles in ARCSEC, deliberately not Angle: on the primary axis Angle
            //  parses HMS, so "00h00m00.5s" would mean 7.5 arcsec, and that trap has already
            //  cost this file enough.
            //
            public double stoppedWindowSeconds;
            public double stoppedArcsec;
            public int pollingFreqMillis;
            public TimeSpan maxTime;

            public MovementParameters()
            {
                pollingFreqMillis = defaultPollingFreqMillis;
            }
        };

        public class Movement
        {
            public Const.AxisDirection direction;
            public double rate;
            public Angle start;
            public Angle target;            // Where we finally want to get, through all the speed rates.
        };

        public Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>> movementParameters, realMovementParameters, simulatedMovementParameters;
        public Dictionary<TelescopeAxes, Movement> currMovement;         // the current axes movement

        public MovementDictionary movementDict;

        public SafetyMonitorTimer safetyMonitorTimer;

        private DomeSlaveDriver domeSlaveDriver;

        private static readonly WiseSafeToOperate wisesafetooperate = WiseSafeToOperate.Instance;

        public static string RateName(double rate)
        {
            Dictionary<double, string> names = new Dictionary<double, string> {
                { Const.rateStopped,  "rateStopped" },
                { Const.rateSlew,  "rateSlew" },
                { Const.rateSet,  "rateSet" },
                { Const.rateGuide,  "rateGuide" },
                { -Const.rateSlew,  "-rateSlew" },
                { -Const.rateSet,  "-rateSet" },
                { -Const.rateGuide,  "-rateGuide" },
                { Const. rateTrack, "rateTrack" },
            };

            if (names.ContainsKey(rate))
                return names[rate];
            return rate.ToString();
        }

        public static void CheckCoordinateSanity(Angle.AngleType type, double value, string reason)
        {
            switch (type) {
                case Angle.AngleType.Dec:
                    if (value < -90.0 || value > 90.0)
                    {
                        Exceptor.Throw<InvalidValueException>("CheckCoordinateSanity",
                            $"Invalid Declination (value: {value}, reason: {reason}), angle: {Angle.DecFromDegrees(value).ToNiceString()}). Must be between -90 and 90");
                    }
                    break;

                case Angle.AngleType.RA:
                    if (value < 0.0 || value > 24.0)
                    {
                        Exceptor.Throw<InvalidValueException>("CheckCoordinateSanity",
                            $"Invalid Right Ascension (value: {value}, reason: {reason}, angle: {Angle.FromHours(value, type).ToNiceString()}). Must be between 0 to 24");
                    }
                    break;

                //
                // Hour angle is NOT right ascension and does not share its range.  Angle
                //  defines AngleType.HA as -12..+12, non-periodic (Angle.cs), negative east
                //  of the meridian - which is what HourAngle actually returns.
                //
                // These two shared a case, so the 0..24 test rejected every hour angle east
                //  of the meridian.  That made SlewToHaDecAsync throw for half the sky, and
                //  took SlewToAltAzAsync with it, since that builds
                //  Angle.HaFromHours(LocalSiderealTime - ra) and hands it to
                //  DoSlewToCoordinatesAsync, which sanity-checks it here.
                //
                case Angle.AngleType.HA:
                    if (value < -12.0 || value > 12.0)
                    {
                        Exceptor.Throw<InvalidValueException>("CheckCoordinateSanity",
                            $"Invalid Hour Angle (value: {value}, reason: {reason}, angle: {Angle.FromHours(value, type).ToNiceString()}). Must be between -12 and 12");
                    }
                    break;
            }
        }

        public double TargetDeclination
        {
            get
            {
                if (_targetDeclination == null)
                    Exceptor.Throw<ValueNotSetException>("TargetDeclination.get", "TargetDeclination not set");
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    $"TargetDeclination Get - {_targetDeclination} ({_targetDeclination.Degrees})");
                #endregion debug
                return _targetDeclination.Degrees;
            }

            set
            {
                CheckCoordinateSanity(Angle.AngleType.Dec, value, $"TargetDeclination Set - {value}");
                _targetDeclination = Angle.DecFromDegrees(value);

                // This is the ASCOM property, which only an RA/Dec client uses.  The hour-angle
                //  and alt/az paths set the backing field directly and record their own type.
                _targetType = TargetCoordinateType.RaDec;
                ActivityMonitor.StayActive("TargetDeclination was set");
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    $"TargetDeclination Set - {_targetDeclination} ({_targetDeclination.Degrees})");
                #endregion debug
            }
        }

        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == null)
                    Exceptor.Throw<ValueNotSetException>("TargetRightAscension.get", "TargetRightAscension not set");

                double hours = _targetRightAscension.Hours;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"TargetRightAscension Get - {_targetRightAscension} ({hours})");
                #endregion debug
                return hours;
            }

            set
            {
                CheckCoordinateSanity(Angle.AngleType.RA, value, $"TargetRightAscension Set - {value}");
                _targetRightAscension = Angle.RaFromHours(value);
                _targetHourAngle = wisesite.LocalSiderealTime - _targetRightAscension;
                _targetType = TargetCoordinateType.RaDec;
                ActivityMonitor.StayActive("TargetRightAscension was set");
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    $"TargetRightAscension Set - {_targetRightAscension} ({_targetRightAscension.Hours})");
                #endregion debug
            }
        }

        public double ApertureDiameter { get; } = 1.016;    // 40 inches in meters

        public double ApertureArea
        {
            get
            {
                return Math.PI * Math.Pow(ApertureDiameter, 2);
            }
        }

        public bool DoesRefraction
        {
            get
            {
                return false;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>("DoesRefraction.set", "DoesRefraction", true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
                _targetRightAscension = null;
                _targetDeclination = null;
                _targetHourAngle = null;
                _targetAzimuth = null;
                _targetAltitude = null;
                _targetType = TargetCoordinateType.None;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Connect(bool connected)
        {
            foreach (var connectable in connectables)
            {
                connectable.Connect(connected);
            }
            _connected = connected;
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

                if (value && EnslavesDome)
                {
                    if (domeSlaveDriver == null)
                        domeSlaveDriver = DomeSlaveDriver.Instance;

                    if (domeSlaveDriver != null && connectables.Find(x => x.Equals(domeSlaveDriver)) == null)
                        connectables.Add(domeSlaveDriver);
                }

                foreach (var connectable in connectables)
                {
                    connectable.Connect(value);
                }
                _connected = value;

                ActivityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.telescope.Line));
                ActivityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.tracking.Line));
                ActivityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.parking.Line));
                ActivityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.shutdown.Line));
                ActivityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.idler.Line));
            }
        }

        private static bool _initialized = false;

        static WiseTele() { }
        public WiseTele() { }

        private static readonly Lazy<WiseTele> lazy = new Lazy<WiseTele>(() => new WiseTele()); // Singleton

        public static WiseTele Instance
        {
            get
            {
                if (lazy.IsValueCreated)
                    return lazy.Value;

                lazy.Value.Init();
                return lazy.Value;
            }
        }

        public void Init()
        {
            if (_initialized)
                return;

            WiseName = "WiseTele";

            ReadProfile();
            //novas31 = new NOVAS31();
            //astroutils = new Astrometry.AstroUtils.AstroUtils();

            parkingDeclination = Angle.DecFromDegrees(66.0);

            _trackingRestorer = new TrackingRestorer();

            CalculatesRefraction = WiseSite.OperationalProfile.CalculatesRefractionForHorizCoords;
            EnslavesDome = WiseSite.OperationalProfile.EnslavesDome;

            #region MotorDefinitions
            //
            // Define motors-related hardware (pins and encoders)
            //
            try
            {
                connectables = new List<IConnectable>();
                disposables = new List<IDisposable>();

                NorthPin = new WisePin("TeleNorth", hardware.teleboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalOut, controlled: true);
                EastPin = new WisePin("TeleEast", hardware.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut, controlled: true);
                WestPin = new WisePin("TeleWest", hardware.teleboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalOut, controlled: true);
                SouthPin = new WisePin("TeleSouth", hardware.teleboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalOut, controlled: true);

                SlewPin = new WisePin("TeleSlew", hardware.teleboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut, controlled: true);
                TrackPin = new WisePin("TeleTrack", hardware.teleboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut, controlled: true);

                NorthGuidePin = new WisePin("TeleNorthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 0, DigitalPortDirection.DigitalOut, controlled: true);
                EastGuidePin = new WisePin("TeleEastGuide", hardware.teleboard, DigitalPortType.FirstPortB, 1, DigitalPortDirection.DigitalOut, controlled: true);
                WestGuidePin = new WisePin("TeleWestGuide", hardware.teleboard, DigitalPortType.FirstPortB, 2, DigitalPortDirection.DigitalOut, controlled: true);
                SouthGuidePin = new WisePin("TeleSouthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 3, DigitalPortDirection.DigitalOut, controlled: true);

                DecEncoder = new WiseDecEncoder("TeleDecEncoder");
                HAEncoder = new WiseHAEncoder("TeleHAEncoder", DecEncoder);

                //RenishawHaEncoder = new RenishawEncoder(RenishawEncoder.Module.Ha);
                //RenishawDecEncoder = new RenishawEncoder(RenishawEncoder.Module.Dec);
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, $"WiseTele constructor caught: {e.Message} at {e.StackTrace}");
                #endregion debug
            }

            //
            // Define motors-related software interfaces (WiseVirtualMotor)
            //
            NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing, new List<object> { DecEncoder });

            SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing, new List<object> { DecEncoder });

            WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { HAEncoder });

            EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { HAEncoder });

            TrackingMotor = new WiseVirtualMotor("TrackMotor", TrackPin, null, null,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { HAEncoder });
            if (TrackPin.isOn)
                TrackingMotor.SetOn(Const.rateTrack);

            //
            // Define motor groups
            //
            axisMotors = new Dictionary<TelescopeAxes, List<WiseVirtualMotor>>
            {
                [TelescopeAxes.axisPrimary] = new List<WiseVirtualMotor> { EastMotor, WestMotor },
                [TelescopeAxes.axisSecondary] = new List<WiseVirtualMotor> { NorthMotor, SouthMotor }
            };

            directionMotors = new List<WiseVirtualMotor>();
            directionMotors.AddRange(axisMotors[TelescopeAxes.axisPrimary]);
            directionMotors.AddRange(axisMotors[TelescopeAxes.axisSecondary]);

            allMotors = new List<WiseVirtualMotor>();
            allMotors.AddRange(directionMotors);
            allMotors.Add(TrackingMotor);

            List<WiseObject> hardware_elements = new List<WiseObject>();
            hardware_elements.AddRange(allMotors);
            hardware_elements.Add(HAEncoder);
            hardware_elements.Add(DecEncoder);
            #endregion

            safetyMonitorTimer = new SafetyMonitorTimer();
            SyncDomePosition = false;

            #region realMovementParameters
            realMovementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>
            {
                [TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>
                {
                    [Const.rateSlew] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00h02m00.0s"),
                        stopMovement = new Angle("00h12m00.0s"),
                        //
                        // 2.10 deg = the measured 2.00 deg eastward coast plus a small
                        //  margin.  One shared 3.0 deg value cut eastward slews 2.951 deg
                        //  out, left 0.955 deg after the coast, and spent 65.4 s of a
                        //  104.5 s slew crawling it at 52 arcsec/sec.
                        //
                        stopMovementIncreasing = new Angle("00h08m24.0s"),
                        minRadChangePerPollingInterval = 0.052,
                        maxRadChangePerPollingInterval = 1.5724276374,
                        //
                        // 3.2 arcsec over 0.5s = 6.4 arcsec/sec, which is exactly what the old
                        //  0.400 arcsec per 62ms sample came to.  Unchanged on purpose.
                        //
                        stoppedWindowSeconds = 0.5,
                        stoppedArcsec = 3.2,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:05.0")),
                        //
                        // 2.3s of RA = 34.5 arcsec, the mean of three measured coasts
                        //  (34.5, 36.0, 31.5) on 2026-09-18.  At the old 30 arcsec all
                        //  three overshot and the guide leg then reversed to undo it,
                        //  costing 4.9, 11.8 and 20.1 seconds.  The old value looked
                        //  correct only because every earlier measurement was taken with
                        //  raEpsilon = 2e-3 h (108 arcsec/sample), which tripped "stopped"
                        //  while the axis was still coasting and understated this by 38x.
                        //
                        stopMovement = new Angle("00h00m02.3s"),
                        minRadChangePerPollingInterval = 0.000146,
                        maxRadChangePerPollingInterval = 0.0436017917,
                        //
                        // Also unchanged.  Tightening this would add several seconds to the end
                        //  of every slew - the settling tail decays with a time constant of
                        //  about 2.5s - and buy nothing: the set-rate coast was already measured
                        //  reliably at this threshold and cross-checked against positions.
                        //
                        stoppedWindowSeconds = 0.5,
                        stoppedArcsec = 3.2,
                        maxTime = TimeSpan.FromMinutes(6),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                        //
                        // The most this rate will accept.  120 arcsec = 8 SECONDS OF TIME on
                        //  this axis, which parses HMS.  See TooFarToMoveAtRate.
                        //
                        maximalMovement = new Angle("00h00m08.0s"),
                        //
                        // Arrival tolerance, 1.5 -> 3.0 arcsec.  NOTE this axis parses HMS, so
                        //  "00h00m00.2s" is 0.2 SECONDS OF TIME = 3.0 arcsec of angle.  RA
                        //  needs no coast correction: it shows no measurable coast at rateSet
                        //  or rateGuide, and its 30 arcsec set->guide threshold is already
                        //  right - RA guide legs exit CloseEnough without reversing.
                        //
                        stopMovement = new Angle("00h00m00.2s"),
                        minRadChangePerPollingInterval = 0.0000072,
                        maxRadChangePerPollingInterval = 0.0014668186,
                        //
                        // The case that was broken.  0.5 arcsec over 2s = 0.25 arcsec/sec, about
                        //  a third of the measured 0.6-0.8 arcsec/sec guide rate, so a running
                        //  guide leg is now distinguishable from a stopped axis.  0.5 arcsec is
                        //  also some 4 encoder counts, so it sits above quantisation rather than
                        //  inside it.  This is what makes the guide-rate coast measurable at all.
                        //
                        stoppedWindowSeconds = 2.0,
                        stoppedArcsec = 0.5,
                        maxTime = TimeSpan.FromMinutes(5),
                    }
                },

                [TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>
                {
                    [Const.rateSlew] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:30:00.0"),
                        //
                        // MEASURED, and measured as the right thing.  What matters is the
                        //  total travel from the moment this threshold trips to the moment
                        //  the axis is stationary - NOT the "stopping distance" the log
                        //  prints, which understates it because the axis runs on at full
                        //  speed during the detect-and-stop sequence.  A 20 degree slew on
                        //  2026-09-17 logged 2.89 deg of stopping distance while actually
                        //  travelling 3.21 deg from trip to halt.
                        //
                        // Over Dec slew legs from 2026-09-16 plus that one:
                        //      min 2.954   mean 3.153   max 3.386 degrees
                        //
                        // Chosen 3.5, above the maximum, DELIBERATELY conservative: the axis
                        //  should undershoot every time and never sail past its target at
                        //  1.7 deg/sec.  Aiming at the mean would be ~17 seconds faster per
                        //  slew and would overshoot on roughly half of them; that trade was
                        //  considered and declined.
                        //
                        // Was 4.5, which left 1.35 deg to crawl at 49.6 arcsec/sec - 98
                        //  seconds, 73% of a 20 degree slew.  3.5 leaves 0.11 to 0.55 deg,
                        //  so 8 to 40 seconds.
                        //
                        // Side effect worth knowing: the engage threshold is minimalMovement
                        //  + stopMovement, so this also drops from 5.0 to 4.0 degrees and
                        //  more moves now get the fast motor at all.
                        //
                        // RA is deliberately untouched at 3.0: its travel measures 1.85 to
                        //  2.90 deg, so it already undershoots every time with 0.1 deg to
                        //  spare. It was right.
                        //
                        stopMovement = new Angle("03:30:00.0"),
                        minRadChangePerPollingInterval = 0.0785,
                        maxRadChangePerPollingInterval = 1.6946717173,
                        //
                        // 1.2 arcsec over 0.5s = 2.4 arcsec/sec, reproducing this axis's old
                        //  decEpsilon of 0.15 arcsec per 62ms sample.  Dec was always the finer
                        //  of the two; keep it that way.
                        //
                        stoppedWindowSeconds = 0.5,
                        stoppedArcsec = 1.2,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:10.0"),
                        //
                        // stopMovement is the MEASURED coast of this axis after the motor is
                        //  switched off, not a safe margin.  Dec coasts 11.9-38.0 arcsec at
                        //  rateSet, mean 22.7 (52 slews, 2026-09-16 logs; a controlled test on
                        //  2026-09-17 measured 20.0).  It was 3 arcsec, so Dec could not stop
                        //  where it was told: it declared CloseEnough at 3 arcsec, coasted ~23
                        //  arcsec past the target, and every one of 52 guide legs then ran
                        //  BACKWARDS at 0.79 arcsec/sec to undo it - 21 to 48 seconds each.
                        //
                        // Aim at the MEAN coast, not an upper bound.  The guide leg cleans up
                        //  at the same speed in either direction, so undershooting by X costs
                        //  exactly what overshooting by X costs; a conservative value buys
                        //  nothing and guarantees the reversal.
                        //
                        stopMovement = new Angle("00:00:23.0"),
                        minRadChangePerPollingInterval = 0.000014,
                        maxRadChangePerPollingInterval = 0.0469464707,
                        stoppedWindowSeconds = 0.5,
                        stoppedArcsec = 1.2,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:01.0"),
                        //
                        // The most this rate will accept - 120 arcsec.  See TooFarToMoveAtRate.
                        //
                        maximalMovement = new Angle("00:02:00.0"),
                        //
                        // Arrival tolerance.  Was 0.1 arcsec - about 2.3 Renishaw counts, on a
                        //  mount whose own repeatability is ~30 arcsec - so the CloseEnough
                        //  test could never pass and this leg could only ever exit by
                        //  overshooting or by timing out.  3 arcsec is achievable and is far
                        //  below anything the pointing model or a plate solve cares about.
                        //
                        stopMovement = new Angle("00:00:03.0"),
                        minRadChangePerPollingInterval = 0.00000049,
                        maxRadChangePerPollingInterval = 0.0001234182,
                        //
                        // As the primary's guide entry.  0.25 arcsec/sec against a measured Dec
                        //  guide rate of 0.79, and 0.5 arcsec is roughly 11 counts on this
                        //  encoder.  Dec reverses at guide rate on almost every slew, so its
                        //  coast is the one most worth being able to measure.
                        //
                        stoppedWindowSeconds = 2.0,
                        stoppedArcsec = 0.5,
                        maxTime = TimeSpan.FromMinutes(5),
                    }
                }
            };
            #endregion

            #region simulatedMovementParameters
            simulatedMovementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>
            {
                [TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>
                {
                    [Const.rateSlew] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("01:00:00.0")),
                        stopMovement = new Angle("00h01m00.0s"),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                        stopMovement = new Angle("00h00m01.0s"),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                        stopMovement = new Angle("00h00m01.0s"),
                    }
                },

                [TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>
                {
                    [Const.rateSlew] = new MovementParameters()
                    {
                        minimalMovement = new Angle("01:00:00.0"),
                        stopMovement = new Angle("00:01:00.0"),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:01.0"),
                        stopMovement = new Angle("00:00:01.0"),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:01.0"),
                        stopMovement = new Angle("00:00:01.0"),
                    }
                }
            };
            #endregion

            movementParameters = Simulated ?
                simulatedMovementParameters :
                realMovementParameters;

            movementDict = new MovementDictionary
            {
                [new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing)] =
                    new MovementWorker(new WiseVirtualMotor[] { WestMotor }),
                [new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] =
                    new MovementWorker(new WiseVirtualMotor[] { EastMotor }),
                [new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing)] =
                    new MovementWorker(new WiseVirtualMotor[] { NorthMotor }),
                [new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing)] =
                    new MovementWorker(new WiseVirtualMotor[] { SouthMotor })
            };

            primaryAxisMonitor = new PrimaryAxisMonitor();
            secondaryAxisMonitor = new SecondaryAxisMonitor();

            connectables.Add(NorthMotor);
            connectables.Add(EastMotor);
            connectables.Add(WestMotor);
            connectables.Add(SouthMotor);
            connectables.Add(TrackingMotor);
            connectables.Add(HAEncoder);
            connectables.Add(DecEncoder);
            connectables.Add(primaryAxisMonitor);
            connectables.Add(secondaryAxisMonitor);

            disposables.Add(NorthMotor);
            disposables.Add(EastMotor);
            disposables.Add(WestMotor);
            disposables.Add(SouthMotor);
            disposables.Add(TrackingMotor);
            disposables.Add(HAEncoder);
            disposables.Add(DecEncoder);
            //
            // Deliberately NOT the two Renishaw encoders.  They are static readonly,
            //  built once at type initialization and never rebuilt - while this list
            //  is disposed by Driver.Dispose(), i.e. whenever a client releases the
            //  driver object, not only at shutdown.  Disposing them there would
            //  release the BiSS modules for good: Init() would hand out encoders
            //  whose modules nobody ever initializes again.
            //
            try
            {
                SlewPin.SetOff();
                TrackingMotor.SetOff();
                NorthMotor.SetOff();
                EastMotor.SetOff();
                WestMotor.SetOff();
                SouthMotor.SetOff();
            }
            catch (Hardware.Hardware.MaintenanceModeException) {
            }

            _initialized = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, "WiseTele init() done.");
            #endregion debug
        }

        public double FocalLength
        {
            get
            {
                return 7.112;  // from Las Campanas 40" (meters)
            }
        }

        public void AbortSlew(string reason)
        {
            string op = $"AbortSlew(reason: {reason})";

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugTele, $"{op}: started.");
            #endregion debug

            ActivityMonitor.StayActive(op);
            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot AbortSlew while AtPark");

            Stop(op);

            try
            {
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.TelescopeSlew,
                        new Activity.TelescopeSlew.EndParams
                        {
                            endState = Activity.State.Aborted,
                            endReason = reason,
                            end = new Activity.TelescopeSlew.Coords() {
                            ra = RightAscension,
                            dec = Declination
                        },
                    });
            }
            catch { }

            if (!telescopeCT.IsCancellationRequested)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op} - Canceling telescopeCTS: #{telescopeCTS.GetHashCode()}");
                #endregion
                //
                // KILLED THE ASCOM SERVER on 2026-09-20:
                //
                //   System.ObjectDisposedException: The CancellationTokenSource has been disposed.
                //     at WiseTele.AbortSlew(String reason)
                //     at SafetyMonitorTimer.SafetyChecker(Object StateObject)
                //
                // The guard above tests the TOKEN, which says nothing about whether the SOURCE
                //  has been disposed - and a new slew replaces telescopeCTS with a fresh one
                //  while telescopeCT may still refer to the old. Two overlapping aborts, or an
                //  abort racing the start of a slew, and Cancel() lands on a disposed source.
                //
                // The Dispose() that used to follow is GONE.  It was the thing creating disposed
                //  sources for everyone else to trip over, and it was never needed: the two
                //  places that replace telescopeCTS already dispose the old one first, and a
                //  CancellationTokenSource with no timer holds nothing that requires disposal.
                //
                // Caught rather than prevented as well, because this runs on the safety path and
                //  the cost of being wrong is the whole server.
                //
                try
                {
                    telescopeCTS.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"{op} - telescopeCTS was already disposed; the slew it belonged to is over.");
                    #endregion
                }
            }

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugTele, $"{op}: done.");
            #endregion debug
        }

        public double RightAscension
        {
            get
            {
                var ret = primaryAxisMonitor.RightAscension;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"RightAscension Get - {ret} ({ret.Hours})");
                #endregion debug
                return ret.Hours;
            }
        }

        public double HourAngle
        {
            get
            {
                Angle ret = primaryAxisMonitor.HourAngle;
                return safeAstroUtils.ConditionHA(ret.Hours);
            }
        }

        public double Declination
        {
            get
            {
                var ret = secondaryAxisMonitor.Declination;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Declination Get - {ret} ({ret.Degrees})");
                #endregion debug
                return ret.Degrees;
            }
        }

        public double Azimuth
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0;

                wisesite.PrepareRefractionData();
                WiseSite.novas31.Equ2Hor(safeAstroUtils.JulianDateUT1(0), 0,
                    WiseSite.astrometricAccuracy,
                    0, 0,
                    wisesite._onSurface,
                    RightAscension, Declination,
                    WiseSite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                return az;
            }
        }

        public double Altitude
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0;

                wisesite.PrepareRefractionData();
                WiseSite.novas31.Equ2Hor(safeAstroUtils.JulianDateUT1(0), 0,
                    WiseSite.astrometricAccuracy,
                    0, 0,
                    wisesite._onSurface,
                    RightAscension, Declination,
                    WiseSite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                return 90.0 - zd;
            }
        }

        private bool SyncDomePosition
        {
            get
            {
                return _syncingDomePosition;
            }

            set
            {
                if (!EnslavesDome || Parking)
                    return;

                if (trackingTimer == null)
                    trackingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(AdjustDomePositionWhileTracking));

                if (value)
                {
                    trackingTimer.Change(trackingDomeAdjustmentInterval, trackingDomeAdjustmentInterval);
                }
                else
                {
                    trackingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                _syncingDomePosition = value;
            }
        }

        private void AdjustDomePositionWhileTracking(object StateObject)
        {
            if (!Tracking)
            {
                SyncDomePosition = false;
                return;
            }

            if (ShuttingDown)
                return;

            if (EnslavesDome && !Slewers.Active(Slewers.Type.Dome) && wisesafetooperate.IsSafeWithoutCheckingForShutdown())
            {
                WiseDome._adjustingForTracking = true;
                DomeSlewer(Angle.RaFromHours(RightAscension), Angle.DecFromDegrees(Declination), "tracking");
            }
        }

        public bool ShuttingDown
        {
            get
            {
                return activityMonitor.ShuttingDown;
            }
        }

        public bool Tracking
        {
            get
            {
                bool ret = TrackingMotor.IsOn;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Tracking Get - {ret}");
                #endregion
                return ret;
            }

            set
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Tracking Set - {value} (from: {Debugger.CodeLocation})");
                #endregion

                if (value)
                {
                    if (!wisesafetooperate.IsSafeWithoutCheckingForShutdown() && !ShuttingDown && !BypassCoordinatesSafety)
                        Exceptor.Throw<InvalidOperationException>("Tracking.set", string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

                    if (RecoveringSafety)
                        Exceptor.Throw<InvalidOperationException>("Tracking.set", "Safety recovery is active");

                    if (Simulated)
                        _lastTrackingLST = wisesite.LocalSiderealTime.Hours;

                    if (TrackingMotor.IsOff)
                        TrackingMotor.SetOn(Const.rateTrack);

                    PrimaryAxisMonitor.ResetRASamples();
                }
                else
                {
                    if (TrackingMotor.IsOn)
                        TrackingMotor.SetOff();
                }
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Backoff);

                SyncDomePosition = value;
                ActivityMonitor.Event(new Event.TrackingEvent(value));
            }
        }

        public static bool EnslavesDome { get; set; }

        public static bool CalculatesRefraction { get; set; }

        public DriveRates TrackingRate
        {
            get
            {
                return DriveRates.driveSidereal;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>("TrackingRate", $"value: {value}", true);
            }
        }

        /// <summary>
        /// Stop all directional motors that are currently working.
        /// Does not affect tracking.
        /// </summary>
        public void Stop(string reason)
        {
            string op = $"WiseTele:Stop (reason: {reason})";
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: started");
            #endregion

            if (Slewing)
            {
                if (EnslavesDome)
                {
                    try
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Calling DomeStopper");
                        #endregion
                        DomeStopper();
                    }
                    catch (AggregateException ax)
                    {
                        ax.Handle((Func<Exception, bool>)((ex) =>
                        {
                            #region debug
                            debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                                $"{op}: dome slewing cancellation caught \"{ex.Message}\" at\n{ex.StackTrace}");
                            #endregion debug
                            return ex is ObjectDisposedException;
                        }));
                    }
                }
            }

            foreach (WiseVirtualMotor motor in allMotors)
                if (motor.IsOn)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Stopping {motor.WiseName}");
                    #endregion
                    motor.SetOff();
                }

            safetyMonitorTimer.DisableIfNotNeeded();
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: done.");
            #endregion
        }

        public void AbortPulseGuiding(string reason)
        {
            pulsing.Abort($"ASCOM.AbortPulseGuiding: (reason {reason})");
        }

        public void FullStop()
        {
            if (Slewing)
                AbortSlew(reason: "Action(\"full-stop\")");

            if (IsPulseGuiding)
                AbortPulseGuiding("FullStop");
            Tracking = false;

            foreach (WiseVirtualMotor motor in allMotors)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "WiseTele:FullStop - Stopping {0}", motor.WiseName);
                #endregion
                motor.SetOff(); // ForceOff
            }
        }

        public static bool AxisIsMoving(TelescopeAxes axis)
        {
            if (axis == TelescopeAxes.axisPrimary)
                return primaryAxisMonitor.IsMoving;
            if (axis == TelescopeAxes.axisSecondary)
                return secondaryAxisMonitor.IsMoving;
            return false;
        }

        /// <summary>
        /// As above, but judged against the rate the axis was last driven at.  See
        ///  AxisMonitor.IsMovingAtRate: one threshold cannot serve rates spanning 10000:1,
        ///  and the old one could not see guide-rate motion at all.
        /// </summary>
        public static bool AxisIsMoving(TelescopeAxes axis, double rate)
        {
            if (axis == TelescopeAxes.axisPrimary)
                return primaryAxisMonitor.IsMovingAtRate(rate);
            if (axis == TelescopeAxes.axisSecondary)
                return secondaryAxisMonitor.IsMovingAtRate(rate);
            return false;
        }

        public bool DirectionMotorsAreActive
        {
            get
            {
                foreach (WiseVirtualMotor m in directionMotors)
                    if (m.IsOn) return true;
                return false;
            }
        }

        /// <summary>
        /// Implements ITelescopeV3.Slewing Property.
        /// True ONLY during SlewXXX and MoveAxis methods.
        /// </summary>
        public bool Slewing
        {
            get
            {
                List<string> reasons = new List<string>();

                if (slewers?.Count > 0)
                    reasons.Add($"Slewers: {slewers}");

                if (_hunkeringDown)
                    reasons.Add($"Hunkering Down");

                if (_hunkeringUp)
                    reasons.Add($"Hunkering Up");

                if (!IsPulseGuiding && DirectionMotorsAreActive)
                {
                    List<string> motors = new List<string>();
                    foreach (WiseVirtualMotor m in directionMotors)
                    {
                        if (m.IsOn)
                        {
                            motors.Add(m.WiseName);
                        }
                    }

                    reasons.Add($"Motors: {string.Join(", ", motors)}");
                }

                if (RecoveringSafety)
                    reasons.Add("Recovering safety");

                if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                    reasons.Add("Shutter is moving");

                if (reasons.Count > 0)
                {
                    ReasonsForSlewing = string.Join("; ", reasons);
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Slewing Get - True ({_reasonsForSlewing})");
                    #endregion debug
                    return true;
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "Slewing Get - False");
                #endregion debug
                return false;
            }
        }

        public string ReasonsForSlewing
        {
            get
            {
                return Slewing ? _reasonsForSlewing : "No observatory components are moving";
            }

            set
            {
                _reasonsForSlewing = value;
            }
        }

        public double DeclinationRate
        {
            get
            {
                return 0.0;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>("DeclinationRate", $"value: {value}", accessorIsSet: true);
            }
        }

        public void HandpadMoveAxis(TelescopeAxes Axis, double Rate)
        {
            string op = $"HandpadMoveAxis({Axis}, {RateName(Rate)})";

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"{op}: started");
            #endregion debug

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None :
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            try
            {
                activityMonitor.NewActivity(new Activity.Handpad(new Activity.Handpad.StartParams() {
                    axis = Axis,
                    rate = Rate,
                    start_coord = (Axis == TelescopeAxes.axisPrimary) ?
                        WiseTele.Instance.RightAscension :
                        WiseTele.Instance.Declination,
                }));
                InternalMoveAxis(Axis, Rate, direction, false);
            } catch (Exception ex)
            {
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Handpad, new Activity.Handpad.EndParams()
                {
                    endState = Activity.State.Aborted,
                    endReason = $"Exception: {ex.Message}",
                    end_coord = (Axis == TelescopeAxes.axisPrimary) ?
                        WiseTele.Instance.RightAscension :
                        WiseTele.Instance.Declination,
                });
                throw;
            }

            //
            // THIS ASKS FOR Stop AND WILL GET Backoff.  Intentional, but not obvious.
            //
            // InternalMoveAxis above has already asked for Backoff by the time we get here, and
            //  EnableIfNeeded only ever UPGRADES - the enum is ordered None < Stop < Backoff and
            //  a weaker request cannot displace a stronger one.  So a handpad move that wanders
            //  past a limit is backed away from it, not merely stopped, and Stop is never the
            //  effective action anywhere in the driver.
            //
            // The upgrade rule is the deliberate part: before it, whichever caller armed the
            //  timer first decided the action for the whole episode, so a handpad move arriving
            //  first left the monitor unable to back away from a limit at all.  That was the
            //  worse failure, and it is the one that was fixed.
            //
            // The call is kept rather than deleted because it still records what the handpad
            //  WANTS - a human is driving, and being yanked 6 degrees off target is startling -
            //  and because it re-arms the monitor for this move.  If the intent is ever to be
            //  honoured, upgrading is not the thing to change: the handpad would need its own
            //  way of saying "stop only", which is a decision about behaviour, not a tidy-up.
            //
            if (!BypassCoordinatesSafety)
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Stop);
        }

        public void HandpadStop()
        {
            List<TelescopeAxes> axes = new List<TelescopeAxes>();

            if (NorthMotor.IsOn || SouthMotor.IsOn)
                axes.Add(TelescopeAxes.axisSecondary);
            if (WestMotor.IsOn || EastMotor.IsOn)
                axes.Add(TelescopeAxes.axisPrimary);

            if (axes.Count == 0)
                return;

            foreach (TelescopeAxes axis in axes)
            {
                StopAxis(axis);

                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Handpad, new Activity.Handpad.EndParams()
                {
                    endState = Activity.State.Succeeded,
                    endReason = "HandpadStop()",
                    end_coord = (axis == TelescopeAxes.axisPrimary) ?
                            WiseTele.Instance.RightAscension :
                            WiseTele.Instance.Declination,
                });
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Handpad: stopped");
            #endregion
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            string op = $"MoveAxis({Axis}, {RateName(Rate)})";

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"{op}: started");
            #endregion debug

            if (!wisesafetooperate.IsSafeWithoutCheckingForShutdown() && !ShuttingDown && !BypassCoordinatesSafety)
            {
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));
            }

            //
            // Nothing new starts while a recovery is in progress.
            //
            // RecoveringSafety used to guard only Tracking.set, and PulseGuide indirectly through
            //  Slewing.  MoveAxis was not guarded at all, so a move could be accepted in the
            //  middle of a backoff - observed 2026-09-20, a handpad-rate move accepted 1.4s into
            //  a recovery, which then ran for a second until the backoff's own closing stop
            //  killed it.  A recovery that can be interrupted and then silently cancel what
            //  interrupted it is worse than either behaviour on its own.
            //
            // Stop, FullStop and AbortSlew are deliberately NOT guarded: stopping must always
            //  work.  Backoff reaches InternalMoveAxis directly and so is exempt.
            //
            if (RecoveringSafety)
                Exceptor.Throw<InvalidOperationException>(op, "Safety recovery is active");

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None :
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            InternalMoveAxis(Axis, Rate, direction, true);
        }

        public void StopAxis(TelescopeAxes axis)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): called", axis);
            #endregion debug

            // Stop any motors that may be On
            foreach (WiseVirtualMotor m in axisMotors[axis])
                if (m.IsOn)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        $"StopAxis({axis}):  {m.WiseName} was on, stopping it.");
                    #endregion debug
                    m.SetOff();
                }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): done.", axis);
            #endregion debug
        }

        public Dictionary<Const.AxisDirection, Const.AxisDirection> otherDirection = new Dictionary<Const.AxisDirection, Const.AxisDirection>
        {
            [Const.AxisDirection.Increasing] = Const.AxisDirection.Decreasing,
            [Const.AxisDirection.Decreasing] = Const.AxisDirection.Increasing,
            [Const.AxisDirection.None] = Const.AxisDirection.None,
        };

        public Dictionary<TelescopeAxes, TelescopeAxes> otherAxis = new Dictionary<TelescopeAxes, TelescopeAxes>
        {
            [TelescopeAxes.axisPrimary] = TelescopeAxes.axisSecondary,
            [TelescopeAxes.axisSecondary] = TelescopeAxes.axisPrimary,
        };

        /// <summary>
        /// Attempts to move axis "thisAxis" at rate "Rate" in direction "direction"
        /// </summary>
        /// <param name="thisAxis"></param>
        /// <param name="Rate"></param>
        /// <param name="direction"></param>
        /// <param name="stopTracking"></param>
        /// <returns>true if the motion was started, false otherwise</returns>
        private bool InternalMoveAxis(
            TelescopeAxes thisAxis,
            double Rate,
            Const.AxisDirection direction = Const.AxisDirection.None,
            bool stopTracking = false)
        {
            string sign = Rate < 0 ? "-" : "";
            string op = $"InternalMoveAxis({thisAxis}, {sign}{RateName(Math.Abs(Rate))}, {direction})";
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: started");
            #endregion debug

            if (thisAxis == TelescopeAxes.axisTertiary)
                Exceptor.Throw<InvalidValueException>(op, "This telescope cannot move in axisTertiary");

            if (AtPark)
            {
                Exceptor.Throw<InvalidValueException>(op, "Cannot MoveAxis while AtPark");
            }

            if (Rate != Const.rateStopped && !wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidValueException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (Rate == Const.rateStopped)
            {
                StopAxisAndWaitForHalt(thisAxis);
                safetyMonitorTimer.DisableIfNotNeeded();
                _trackingRestorer.RemoveMover();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: done.");
                #endregion
                return true;
            }

            double absRate = Math.Abs(Rate);
            if (!((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                Exceptor.Throw<InvalidValueException>($"InternalMoveAxis({thisAxis}, {absRate})", "Invalid rate.");

            if (!readyToSlewFlags.AxisCanMoveAtRate(thisAxis, absRate))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: not BOTH axes are ready to move");
                #endregion
                return false;
            }

            MovementWorker mover;
            try
            {
                mover = movementDict[new MovementSpecifier(thisAxis, direction)];
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"Don't know how to {op}: (no mover) ({axisDirectionName[thisAxis][direction]}) [{e.Message}]");
                #endregion debug
                return false;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"InternalMoveAxis({thisAxis}, {RateName(Rate)}): direction: {axisDirectionName[thisAxis][direction]}, stopTracking: {stopTracking}");
            #endregion debug

            if (stopTracking)
            {
                if (Tracking)
                {
                    _trackingRestorer.AddMover();
                    Tracking = false;
                }
            }

            #region debug
            Angle currPosition = (thisAxis == TelescopeAxes.axisPrimary) ?
                Angle.RaFromHours(RightAscension) :
                Angle.DecFromDegrees(Declination);

            List<string> startedMotors = new List<string>();
            #endregion
            foreach (WiseVirtualMotor m in mover.motors)
            {
                m.SetOn(absRate);
                #region debug
                startedMotors.Add(m.WiseName);
                #endregion
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"{op}: currPosition: {currPosition}, started motors: {string.Join(", ", startedMotors)}");
            #endregion debug

            if (! BypassCoordinatesSafety)
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Backoff);

            return true;
        }

        public bool IsPulseGuiding
        {
            get
            {
                bool ret = Pulsing.Instance.IsPulseGuiding;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"IsPulseGuiding: {ret}");
                #endregion
                return ret;
            }
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToTargetAsync()
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            if (_targetRightAscension == null)
                Exceptor.Throw<ValueNotSetException>("SlewToTargetAsync", "Target RA not set");
            if (_targetDeclination == null)
                Exceptor.Throw<ValueNotSetException>("SlewToTargetAsync", "Target Dec not set");

            Angle ra = Angle.RaFromHours(TargetRightAscension);
            Angle dec = Angle.DecFromDegrees(TargetDeclination);

            string op = $"SlewToTargetAsync({ra}, {dec})";
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, op);
            #endregion debug

            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while AtPark");

            if (!Tracking)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while NOT Tracking");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");

            string notSafe = SafeAtCoordinates(ra, dec);
            if (!string.IsNullOrEmpty(notSafe))
                Exceptor.Throw<InvalidOperationException>(op, notSafe);

            DoSlewToCoordinatesAsync(_targetRightAscension, _targetDeclination, op);
        }

        /// <summary>
        /// Check whether it's safe at .5 degrees in the specified direction
        /// </summary>
        /// <param name="directions">
        ///   Comma or space delimited list of cardinal direction
        /// </param>
        /// <returns>Safe or not-safe.</returns>
        public bool SafeToMove(string direction)
        {
            double ra = RightAscension;
            double dec = Declination;
            const double delta = 0.5;
            bool safer;

            switch (direction.ToLower())
            {
                case "north":
                    dec += delta;
                    break;
                case "south":
                    dec -= delta;
                    break;
                case "east":
                    ra += Angle.Deg2Hours(delta);
                    break;
                case "west":
                    ra -= Angle.Deg2Hours(delta);
                    break;
            }
            safer = HigherAtCoordinates(direction, Angle.RaFromHours(ra), Angle.DecFromDegrees(dec));
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"SafeToMove({direction}: {safer}");
            #endregion
            return safer;
        }

        /// <summary>
        /// Whether the given coordinates are HIGHER in altitude than where we are now.
        ///
        /// Renamed from SaferAtCoordinates, which overstated it: this consults none of
        ///  altLimit, eastern_haLimit, western_haLimit, lower_decLimit or upper_decLimit.  It
        ///  is an altitude gradient and nothing more.
        ///
        /// Backoff relies on it, and gets away with it because altitude peaks at the meridian
        ///  for a given declination, so "uphill" happens to mean "toward the meridian" for an
        ///  hour-angle violation and "toward the latitude" for either declination limit.  That
        ///  is incidental rather than by construction - worth knowing before the limits or the
        ///  site change.
        /// </summary>
        public bool HigherAtCoordinates(string dir, Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;
            double dist0, dist1;
            bool ret;

            wisesite.PrepareRefractionData();
            WiseSite.novas31.Equ2Hor(safeAstroUtils.JulianDateUT1(0), 0,
                WiseSite.astrometricAccuracy,
                0, 0,
                wisesite._onSurface,
                ra.Hours, dec.Degrees,
                WiseSite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            //
            // Compare the altitudes directly.
            //
            // This was Abs(Cos(alt_target)) < Abs(Cos(alt_now)).  For altitudes between 0 and
            //  90 that is equivalent, but the Abs made a position BELOW the horizon compare
            //  equal to its mirror image above it - Abs(Cos(-20)) == Abs(Cos(+20)) - so a move
            //  from 20 degrees under the horizon to 20 degrees over it read as "not higher"
            //  and was refused.  Exactly the case a recovery exists for.
            //
            dist0 = 90.0 - zd;          // altitude at the candidate coordinates
            dist1 = Altitude;           // altitude now
            ret = dist0 > dist1;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                $"HigherAtCoordinates({dir}, {ra.ToNiceString()}, {dec.ToNiceString()}): new alt: {dist0:f6} > curr alt: {dist1:f6} => {ret}");
            #endregion
            return ret;
        }

        public readonly Angle altLimit = new Angle(16.0, Angle.AngleType.Alt);
        //
        // Tightened from +/-7.0 to +/-6.5 on 2026-09-19, after these limits failed to protect
        //  the mount.
        //
        // A runaway slew reached a PHYSICAL limit switch at HA -6.7255 with Dec 62.915 - inside
        //  the -7.0 soft limit, which therefore never fired.  The HardLimit switch cuts motor
        //  power, so the hardware stopped the axis; nothing in software did.
        //
        // Why a fixed hour angle can be wrong at all: the switches sense absolute orientation,
        //  so their trip locus is a CURVE in (HA, Dec), while these are single numbers that
        //  ignore declination.  There must therefore be declinations where a switch trips before
        //  the soft limit, and Dec 62.9 is one of them.
        //
        // 6.5 buys 0.2255 h - about 3.4 degrees - of margin at the one declination where the
        //  trip point is actually known.  It is NOT provably safe everywhere: the rest of the
        //  (HA, Dec) trip locus has never been mapped, and a full fix would make the limit a
        //  function of declination rather than a constant.  Treat this as a smaller constant,
        //  not as a solved problem.  See [[soft-limits-are-not-conservative]].
        //
        public readonly Angle eastern_haLimit = Angle.HaFromHours(-6.5);
        public readonly Angle western_haLimit = Angle.HaFromHours(6.5);
        public readonly Angle lower_decLimit = Angle.DecFromDegrees(-35.0);
        public readonly Angle upper_decLimit = Angle.DecFromDegrees(89.9);

        //
        // A TEMPORARY extra restriction for on-sky testing, set through the "test-envelope"
        //  Action.  Null means no extra restriction, which is the state after every restart.
        //
        // The point is to keep a test well away from the real danger zones: the limit switch at
        //  the eastern end cut the mount's power on 2026-09-19, and the compiled limits are not
        //  themselves conservative - see [[soft-limits-are-not-conservative]].
        //
        // Three properties make this safe to have in the safety path at all:
        //
        //  1. It can only ADD a rejection reason, never remove one.  SafeAtCoordinates stays at
        //     least as strict as the compiled limits no matter what these hold.
        //  2. The Action REFUSES any value looser than the compiled limit, so it cannot widen
        //     the envelope even by mistake or typo.
        //  3. Deliberately NOT persisted to the profile.  A restart restores the real limits,
        //     which is the honest default - a forgotten tight envelope sitting in the profile
        //     would quietly shrink the working sky for the next ACP night, and an elevated
        //     rebuild wipes the profile subkey anyway.
        //
        // Rejections name the TEST limit explicitly, so a slew refused by the temporary envelope
        //  never looks like a slew refused by a real limit.
        //
        private static double? testAltLimitDeg = null;
        private static double? testHaLimitHours = null;

        /// <summary>
        /// Describes the temporary test envelope, for the Action and for logging.
        /// </summary>
        public static string TestEnvelopeDescription
        {
            get
            {
                if (!testAltLimitDeg.HasValue && !testHaLimitHours.HasValue)
                    return "test-envelope: off (the compiled limits apply)";

                List<string> parts = new List<string>();
                if (testAltLimitDeg.HasValue)
                    parts.Add($"AltLimit={testAltLimitDeg.Value:F1} deg");
                if (testHaLimitHours.HasValue)
                    parts.Add($"HaLimit=+/-{testHaLimitHours.Value:F2} h");
                return "test-envelope: " + String.Join(", ", parts);
            }
        }

        /// <summary>
        /// Checks if we're safe at a given position:  Used:
        ///  - before slewing to check if the scope will be safe at the target coordinates
        ///  - by the safety timer to check that the scope is safe at the current coordinates.
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        public string SafeAtCoordinates(Angle ra, Angle dec)
        {
            return SafeAtCoordinates(ra, dec, out _);
        }

        /// <param name="violations">
        /// Which limits were breached, for callers that have to undo them.  None when safe.
        /// </param>
        public string SafeAtCoordinates(Angle ra, Angle dec, out SafetyViolation violations)
        {
            violations = SafetyViolation.None;

            if (BypassCoordinatesSafety)
                return string.Empty;

            double rar = 0, decr = 0, az = 0, zd = 0;
            List<string> reasons = new List<string>();

            wisesite.PrepareRefractionData();
            WiseSite.novas31.Equ2Hor(safeAstroUtils.JulianDateUT1(0), 0,
                WiseSite.astrometricAccuracy,
                0, 0,
                wisesite._onSurface,
                ra.Hours, dec.Degrees,
                WiseSite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            Angle alt = Angle.AltFromDegrees(90.0 - zd);
            if (alt < altLimit)
            {
                reasons.Add($"Altitude too low: {alt} < {altLimit}");
                violations |= SafetyViolation.AltitudeTooLow;
            }

            //
            // Separate from the check above, never folded into it: the compiled limit must keep
            //  rejecting on its own, whatever the test envelope holds.
            //
            if (testAltLimitDeg.HasValue && alt < Angle.AltFromDegrees(testAltLimitDeg.Value))
            {
                reasons.Add($"Altitude below the TEST limit: {alt} < {Angle.AltFromDegrees(testAltLimitDeg.Value)}");
                violations |= SafetyViolation.AltitudeTooLow;
            }

            if (dec > upper_decLimit)
            {
                reasons.Add($"Declination too high: {dec} > {upper_decLimit}");
                violations |= SafetyViolation.DeclinationTooHigh;
            }
            if (dec < lower_decLimit)
            {
                reasons.Add($"Declination too low: {dec} < {lower_decLimit}");
                violations |= SafetyViolation.DeclinationTooLow;
            }

            //
            // The hour angle OF THE TARGET, derived from the ra argument.
            //
            // This was "double ha = HourAngle", i.e. where the telescope happens to be
            //  pointing right now.  Altitude and declination were tested against the target
            //  while the hour angle was tested against the current position, so a slew whose
            //  target lay beyond the hour-angle limits was accepted as long as the CURRENT
            //  position was inside them.  Altitude does not cover for it: at declination 66 a
            //  target at hour angle 8h - an hour past the western limit - sits at altitude
            //  16.9 degrees, above the 16 degree floor, so nothing rejected it and the mount
            //  was driven at the limit switches.
            //
            // The line was correct for the OTHER caller, the safety timer, which passes the
            //  current right ascension; that is presumably how it survived.  ConditionHA
            //  normalises to -12..+12, matching the HourAngle property and the limits.
            //
            double ha = safeAstroUtils.ConditionHA(wisesite.LocalSiderealTime.Hours - ra.Hours);
            if (ha < eastern_haLimit.Hours)
            {
                reasons.Add($"HourAngle too low: {Angle.HaFromHours(ha)} < {eastern_haLimit}");
                violations |= SafetyViolation.HourAngleTooLow;
            }
            else if (ha > western_haLimit.Hours)
            {
                reasons.Add($"HourAngle too high: {Angle.HaFromHours(ha)} > {western_haLimit}");
                violations |= SafetyViolation.HourAngleTooHigh;
            }

            //
            // A plain if, not another else-if on the chain above: a target beyond BOTH the
            //  compiled limit and the test limit should say so twice rather than hide one.
            //
            // Symmetric, because both ends carry a limit switch.  Splitting it into east and
            //  west is a two-field change here if a test ever needs an asymmetric envelope.
            //
            if (testHaLimitHours.HasValue && Math.Abs(ha) > testHaLimitHours.Value)
            {
                reasons.Add($"HourAngle outside the TEST envelope: {Angle.HaFromHours(ha)} beyond +/-{testHaLimitHours.Value:F2}h");

                // The envelope is symmetric, so the SIGN says which side was left and therefore
                //  which way recovery has to go.
                violations |= (ha > 0) ? SafetyViolation.HourAngleTooHigh : SafetyViolation.HourAngleTooLow;
            }

            if (reasons.Count > 0)
            {
                string msg = $"SafeAtCoordinates(ra: {ra}, dec: {dec}) - " + String.Join(", ", reasons.ToArray());
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, msg);
                #endregion
                return msg;
            }
            else
                return string.Empty;
        }
        private struct BackoffAction
        {
            public TelescopeAxes Axis;
            public string Direction;
            public double Rate;
        };

        /// <summary>
        /// Checks what motors are on and moves the scope away from danger.
        /// </summary>
        /// <summary>
        /// Backs away from whatever limit the CURRENT position breaches.
        /// </summary>
        public void Backoff(string reason)
        {
            SafeAtCoordinates(Angle.RaFromHours(RightAscension), Angle.DecFromDegrees(Declination),
                out SafetyViolation violations);
            Backoff(reason, violations);
        }

        /// <summary>
        /// Backs away from the limits named in <paramref name="violations"/>, and only those.
        /// </summary>
        //
        // It used to move BOTH axes toward higher altitude, whatever had gone wrong: it asked
        //  SafeToMove which way was higher and went that way.  On 2026-09-20 an hour-angle breach
        //  of 14 arcseconds was answered with 3.8 degrees east AND 7.05 degrees south - the
        //  declination axis correcting a declination limit that was never breached, leaving the
        //  mount 7 degrees from where anyone expected it.
        //
        // Now each breach moves its own axis its own way:
        //
        //   hour angle too low  (east of the eastern limit)  -> WEST
        //   hour angle too high (west of the western limit)  -> EAST
        //   declination too high                             -> SOUTH
        //   declination too low                              -> NORTH
        //
        // ALTITUDE is the exception and keeps the old heuristic, because altitude is not the
        //  property of one axis: it is a function of both, and which one to move depends on where
        //  you are. Asking SafeToMove which direction is higher is the right question for that
        //  breach - it just was not the right question for the others.
        //
        // An empty violation set does nothing, which is what a caller that found no breach
        //  should get.
        //
        public void Backoff(string reason, SafetyViolation violations)
        {
            string op = $"Backoff(reason: {reason}, violations: {violations})";
            const int backoffMillis = 3000;

            List<BackoffAction> backoffs = new List<BackoffAction>();

            if (violations.HasFlag(SafetyViolation.HourAngleTooHigh))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "East", Rate = Const.rateSlew });
            else if (violations.HasFlag(SafetyViolation.HourAngleTooLow))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "West", Rate = -Const.rateSlew });

            if (violations.HasFlag(SafetyViolation.DeclinationTooHigh))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "South", Rate = -Const.rateSlew });
            else if (violations.HasFlag(SafetyViolation.DeclinationTooLow))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "North", Rate = Const.rateSlew });

            if (violations.HasFlag(SafetyViolation.AltitudeTooLow))
            {
                //
                // Only whichever axis is not already being moved for a breach of its own, so a
                //  position that is both too low AND past an hour-angle limit does not get two
                //  conflicting commands for the same axis.
                //
                if (!backoffs.Any(b => b.Axis == TelescopeAxes.axisPrimary))
                {
                    if (SafeToMove("east"))
                        backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "East", Rate = Const.rateSlew });
                    else if (SafeToMove("west"))
                        backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "West", Rate = -Const.rateSlew });
                }

                if (!backoffs.Any(b => b.Axis == TelescopeAxes.axisSecondary))
                {
                    if (SafeToMove("south"))
                        backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "South", Rate = -Const.rateSlew });
                    else if (SafeToMove("north"))
                        backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "North", Rate = Const.rateSlew });
                }
            }

            if (backoffs.Count == 0)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: nothing to back away from");
                #endregion
                return;
            }

            //
            // InternalMoveAxis, NOT the public MoveAxis.  Two reasons, both deliberate.
            //
            // The public wrapper now refuses while RecoveringSafety is set, so that an incoming
            //  slew or handpad nudge cannot land on top of a recovery in progress.  The recovery
            //  is the one caller that must be exempt, and going straight to the internal method
            //  is the exemption - no flag to thread through, no way for anything else to claim it.
            //
            // It also drops the wrapper's wisesafetooperate check, which is a fix rather than a
            //  loss: that check made the recovery THROW when the weather was unsafe, which is
            //  exactly when a coordinates violation is most likely and least excusable to ignore.
            //  The try/finally around RecoveringSafety in SafetyChecker exists because of that
            //  throw.  Backing away from a limit is the safe action in any weather.
            //
            // Otherwise identical to what the wrapper did: same direction rule, same
            //  stopTracking: true.
            //
            foreach (var b in backoffs)
            {
                Const.AxisDirection dir = (b.Rate < 0.0) ?
                    Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"{op}: {b.Direction}: calling InternalMoveAxis({b.Axis}, {b.Direction}, {RateName(b.Rate)}) for {backoffMillis} millis ...");
                #endregion
                InternalMoveAxis(b.Axis, b.Rate, dir, true);
                Thread.Sleep(backoffMillis);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: stopping {b.Axis}");
                #endregion
                InternalMoveAxis(b.Axis, Const.rateStopped, Const.AxisDirection.None, true);
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: done");
            #endregion
        }

        public bool AtPark
        {
            get
            {
                bool ret = _atPark;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"AtPark Get - {ret}");
                #endregion debug
                return ret;
            }

            set
            {
                _atPark = value;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"AtPark Set - {_atPark}");
                #endregion debug
            }
        }

        private void DoHunkerDown(string reason)
        {
            string op = $"DoHunkerDown(reason: {reason})";

            if (activityMonitor.HunkeringDown)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: hunkerdown already in progress (activityMonitor.HunkeringDown == true) => skipping hunkerdown");
                #endregion
                return;
            }

            if (domeSlaveDriver.ShutterState == ShutterState.shutterClosing || domeSlaveDriver.ShutterState == ShutterState.shutterClosed)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: Wise40 is already hunkered down (ShutterState = {domeSlaveDriver.ShutterState}) => skipping hunkerdown");
                #endregion
                return;
            }

            _hunkeringDown = true;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: starting activity HunkeringDown ...");
            #endregion
            activityMonitor.NewActivity(new Activity.HunkeringDown(new Activity.HunkeringDown.StartParams() { reason = reason }));

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: setting Tracking to false ...");
            #endregion
            Tracking = false;

            if (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
            {
                // Wait for shutter to close
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling domeSlaveDriver.CloseShutter() ...");
                #endregion
                DomeSlaveDriver.CloseShutter($"{op}");
                while (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for domeSlaveDriver.ShutterState == ShutterState.shutterClosed ...");
                    #endregion
                    Thread.Sleep(1000);
                }
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Shutter is closed.");
            #endregion

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: ending activity HunkeringDown ...");
            #endregion
            activityMonitor.EndActivity(ActivityMonitor.ActivityType.HunkeringDown,
            new Activity.HunkeringDown.GenericEndParams()
            {
                endState = Activity.State.Succeeded,
                endReason = "Hunkerdown done"
            });

            _hunkeringDown = false;
            _hunkeredDown = true;
        }

        private void DoShutdown(string reason)
        {
            string op = $"DoShutdown(reason: {reason})";

            if (activityMonitor.ShuttingDown)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: shutdown already in progress (activityMonitor.ShuttingDown == true) => skipping shutdown");
                #endregion
                return;
            }

            if (AtPark && domeSlaveDriver.AtPark && domeSlaveDriver.ShutterState == ShutterState.shutterClosed)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: Wise40 is already shut down (AtPark and dome.AtPark and shutterClosed) => skipping shutdown");
                #endregion
                return;
            }

            SafeToOperateDigest safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wisesafetooperate.Digest);

            bool rememberToCancelSafetyBypass = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: starting activity ShuttingDown ...");
            #endregion
            activityMonitor.NewActivity(new Activity.Shutdown(new Activity.Shutdown.StartParams() { reason = reason }));
            this._hunkeredDown = false;

            if (!safetooperateDigest.Bypassed)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: starting safetooperate bypass ...");
                #endregion
                rememberToCancelSafetyBypass = true;
                wisesafetooperate.Action("bypass", "start,temporary");
            }

            if (AtPark)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: setting AtPark to false ...");
                #endregion
                AtPark = false; // Don't call Unpark(), it throws exception if while ShuttingDown
            }

            if (domeSlaveDriver.AtPark)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling domeSlaveDriver.Unpark() ...");
                #endregion
                DomeSlaveDriver.Unpark();
            }

            if (Slewing)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling AbortSlew() ...");
                #endregion
                AbortSlew(op);
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for !Slewing ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (Slewing);
            }

            if (IsPulseGuiding)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling AbortPulseGuiding() ...");
                #endregion
                AbortPulseGuiding(op);
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for !IsPulseGuiding ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (IsPulseGuiding);
            }

            if (domeSlaveDriver.Slewing)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling domeSlaveDriver.AbortSlew() ...");
                #endregion
                DomeSlaveDriver.AbortSlew();
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for Slewing to end ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (domeSlaveDriver.Slewing);
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Not Slewing.");
            #endregion

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: setting Tracking to false ...");
            #endregion
            Tracking = false;

            if (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
            {
                // Wait for shutter to close before continuing
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling domeSlaveDriver.CloseShutter() ...");
                #endregion
                DomeSlaveDriver.CloseShutter($"{op}");
                while (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for domeSlaveDriver.ShutterState == ShutterState.shutterClosed ...");
                    #endregion
                    Thread.Sleep(1000);
                }
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Shutter is closed.");
            #endregion

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling Park() ...");
            #endregion
            try
            {
                Park();
            } catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: exception during Park(): {0}", ex.ToString());
                #endregion
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: after Park() ...");
            #endregion

            if (rememberToCancelSafetyBypass)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: ending safetooperate bypass ...");
                #endregion
                wisesafetooperate.Action("bypass", "end,temporary");
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: ending activity ShuttingDown ...");
            #endregion
            activityMonitor.EndActivity(ActivityMonitor.ActivityType.ShuttingDown,
            new Activity.Shutdown.GenericEndParams()
                {
                    endState = Activity.State.Succeeded,
                    endReason = "Shutdown done"
                });
        }

        public void Shutdown(string reason)
        {
            Task.Run(() => DoShutdown(reason), telescopeCT);
        }

        public void Hunkerdown(string reason)
        {
            Task.Run(() => DoHunkerDown(reason), telescopeCT);
        }

        //
        // This is the Synchronous version, as mandated by ASCOM
        //
        public void Park()
        {
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugTele, "Park: started");
            #endregion debug
            if (AtPark)
                return;

            //
            // Park in the MOUNT frame.  This was LocalSiderealTime sampled once, before the
            //  slew, which pinned the target to the meridian as it was at that instant; the
            //  telescope then arrived about 15 arcmin west of it for a 60 second slew.  An
            //  hour angle of zero is the meridian whenever we get there.
            //
            //
            // REVERTED to a right-ascension target on 2026-09-19.  DO NOT make this an hour
            //  angle again until Angle.ShortestDistance is fixed for AngleType.HA and checked
            //  against known values offline.
            //
            // What happened: PR #33 changed this to Angle.HaFromHours(0.0), on the sound
            //  reasoning that an hour angle needs no lead for the slew duration.  But an
            //  HA-typed slew computes its distance wrongly.  Parking from HA -01h21m44.8s to
            //  HA 0 - a true distance of 1.363h, 20.4 degrees, 0.357 rad - the slewer logged
            //
            //      remaining (Angle.rad: 5.3502389458)      i.e. 306 degrees, 20.4 HOURS
            //
            //  and drove EAST, away from the target, the reported distance GROWING every
            //  sample: 5.3502, 5.3504, 5.3518, 5.3554, 5.3613.  ChangedDirection never fired
            //  because the direction was wrong from the first sample rather than changing, so
            //  nothing stopped it.  It ran 77 seconds and covered about 80 degrees of hour
            //  angle before a PHYSICAL limit switch cut motor power and stopped it at HA
            //  -6.7255 - inside the then -7.0 soft limit, which never fired.  The manual
            //  AbortSlew that followed was after the fact; the hardware stopped it, not
            //  software.  Left alone the driver would have kept commanding to mp.maxTime.
            //
            //  The declination axis was fine in the same slew: 0.349 rad for a 20 degree move,
            //  decreasing correctly.  It is specific to the HA angle path.
            //
            //  This matters more than an ordinary bug because Park is what UNATTENDED systems
            //  call: ACP parks at the end of a session and the Dash has a park button.
            //
            // The cost of reverting is the defect PR #33 set out to fix: sampling
            //  LocalSiderealTime once, before the slew, pins the target to the meridian as it
            //  was at that instant, so the mount lands about 15 arcmin west of it for a 60
            //  second slew.  Fifteen arcmin of parking error is a great deal better than an
            //  axis running at a limit.  See [[park-position]].
            //
            Angle parkingRa = wisesite.LocalSiderealTime;
            Angle parkingDec = parkingDeclination;

            //
            // A right ascension rendering of the park target, for the activity record and for
            //  TargetRightAscension, which ASCOM clients read and which has no hour-angle
            //  equivalent.  It is a SNAPSHOT - the meridian as it stands now - and is
            //  deliberately not what the slew aims at.  Same value the old code used as the
            //  target, so nothing a client sees changes.
            //
            Angle parkingRaAtStart = parkingRa;
            bool wasEnslavingDome = EnslavesDome;

            try
            {
                Parking = true;
                activityMonitor.NewActivity(new Activity.Park(new Activity.Park.StartParams() {
                    start = new Activity.TelescopeSlew.Coords
                    {
                        ra = RightAscension,
                        dec = Declination,
                    },
                    target = new Activity.TelescopeSlew.Coords
                    {
                        ra = parkingRaAtStart.Hours,
                        dec = parkingDec.Degrees,
                    },
                    domeStartAz = WiseDome.Instance.Azimuth.Degrees,
                    domeTargetAz = 90.0,
                    shutterPercent = 100,
                }));
                if (wasEnslavingDome)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: starting DomeParker() ...");
                    #endregion
                    DomeParker();
                }
                TargetRightAscension = parkingRaAtStart.Hours;
                TargetDeclination = parkingDec.Degrees;

                EnslavesDome = false;
                while (! (primaryAxisMonitor.IsReady && secondaryAxisMonitor.IsReady))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: waiting for axis monitors to be ready ...");
                    #endregion
                    Thread.Sleep(500);
                }
                //
                // Tracking back on before the slew, as it was before PR #33: the target is a
                //  right ascension again and tracking is what holds one.  Park still ends with
                //  Tracking = false further down, as it always did.
                //
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: setting Tracking = true ...");
                #endregion
                Tracking = true;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: starting InternalSlewToCoordinatesSync ...");
                #endregion
                InternalSlewToCoordinatesSync(parkingRa, parkingDec, "Park");
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: after InternalSlewToCoordinatesSync ...");
                #endregion
            }
            catch(Exception ex)
            {
                Parking = false;
                Tracking = false;
                EnslavesDome = wasEnslavingDome;
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Parking, new Activity.Park.EndParams()
                {
                    endState = Activity.State.Failed,
                    endReason = $"Parking failed due to exception: \"{ex}\".",
                    end = new Activity.TelescopeSlew.Coords
                    {
                        ra = RightAscension,
                        dec = Declination,
                    },
                    domeAz = WiseDome.Instance.Azimuth.Degrees,
                    shutterPercent = WiseDome.Instance.wisedomeshutter.PercentOpen,
                });
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"Park: Aborted due to Exception: {ex.Message}");
                #endregion
                if (ShuttingDown)
                    throw;
                return;
            }

            while (Slewing)
            {
                // The dome (not enslaved at this time) may be still moving
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: Waiting for Slewing to end ...");
                #endregion
                Thread.Sleep(5000);
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: all done.");
            #endregion
            if (WiseSite.OperationalMode != WiseSite.OpMode.WISE)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: setting AtPark == true");
                #endregion
                AtPark = true;
            }
            Parking = false;
            Tracking = false;
            //_targetRightAscension = null;
            //_targetDeclination = null;
            EnslavesDome = wasEnslavingDome;
            activityMonitor.EndActivity(ActivityMonitor.ActivityType.Parking, new Activity.Park.EndParams()
            {
                endState = Activity.State.Succeeded,
                endReason = "Parking done",
                end = new Activity.TelescopeSlew.Coords
                {
                    ra = RightAscension,
                    dec = Declination,
                },
                domeAz = WiseDome.Instance.Azimuth.Degrees,
                shutterPercent = WiseDome.Instance.wisedomeshutter.PercentOpen,
            });
        }

        public void ParkFromGui(bool parkDome)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park");
            #endregion debug
            if (AtPark)
                return;

            //
            // REVERTED with Park() above - see the long comment there.  An HA-typed slew
            //  computes its distance wrongly and drove the primary axis away from the target at
            //  slew rate until it was aborted by hand.
            //
            Angle ra = wisesite.LocalSiderealTime;
            Angle dec = parkingDeclination;

            if (parkDome)
                DomeParker();
            SlewToCoordinatesAsync(ra.Hours, dec.Degrees, "ParkFromGui", false);
        }

        /// <summary>
        /// Rejects a slew whose TARGET lies in the forbidden zone.
        ///
        /// Lives here so that every slew passes through it.  It used to be each caller's
        ///  responsibility, which left holes: DoSlewToCoordinatesAsync carried a
        ///  "// Check coordinates safety ???" comment where the check was not, and an
        ///  hour-angle slew reached safety only indirectly, through the alt/az conversion it
        ///  no longer performs.
        ///
        /// noSafetyCheck is an EXPLICIT opt-out, not an accident of which caller was used.
        ///  move-to-preset "cover" needs it: the mirror cover sits at hour angle 11h55m,
        ///  far outside the +/-7h limits, and must remain reachable.
        /// </summary>
        private void RejectUnsafeTarget(Angle primaryTargetAngle, Angle secondaryTargetAngle, string op)
        {
            Angle ra;

            if (primaryTargetAngle.Type == Angle.AngleType.HA)
            {
                //
                // SafeAtCoordinates works in right ascension, so convert.  It derives the
                //  hour angle back out, which round-trips exactly enough - the two reads of
                //  LocalSiderealTime are microseconds apart.
                //
                double raHours = wisesite.LocalSiderealTime.Hours - primaryTargetAngle.Hours;
                while (raHours < 0.0)
                    raHours += 24.0;
                while (raHours >= 24.0)
                    raHours -= 24.0;
                ra = Angle.RaFromHours(raHours);
            }
            else
                ra = primaryTargetAngle;

            string notSafe = SafeAtCoordinates(ra, secondaryTargetAngle);
            if (!string.IsNullOrEmpty(notSafe))
                Exceptor.Throw<InvalidOperationException>(op, notSafe);
        }

        private void InternalSlewToCoordinatesSync(Angle primaryTargetAngle, Angle secondaryTargetAngle, string whatfor, bool noSafetyCheck = false)
        {
            #region debug
            string op = "InternalSlewToCoordinatesSync(" +
                $"{primaryTargetAngle.ToNiceString()}, " +
                $"{secondaryTargetAngle.ToNiceString()}, " +
                $"for: {whatfor})";

            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op} called");
            #endregion debug
            try
            {
                endOfAsyncSlewEvent = new ManualResetEvent(false);

                if (telescopeCT.IsCancellationRequested)
                {
                    telescopeCTS = new CancellationTokenSource();
                    telescopeCT = telescopeCTS.Token;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"{op}: New telescopeCTS (#{telescopeCTS.GetHashCode()}), telescopeCT: (#{telescopeCT.GetHashCode()})");
                    #endregion
                }

                //
                // Checked HERE, before the task, and then skipped inside it.  An exception
                //  raised inside Task.Run becomes an unobserved task fault - the caller would
                //  see a slew that silently did nothing rather than a rejection.
                //
                if (!noSafetyCheck)
                    RejectUnsafeTarget(primaryTargetAngle, secondaryTargetAngle, op);

                Task t = Task.Run(() =>
                {
                    DoSlewToCoordinatesAsync(primaryTargetAngle, secondaryTargetAngle, op, noSafetyCheck: true);
                    Thread.Sleep(500);
                }, telescopeCT);
                Thread.Sleep(100);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: slewing task status: {t.Status}");
                #endregion
            }
            catch (AggregateException ae)
            {
                ae.Handle((Func<Exception, bool>)((ex) =>
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Caught \"{ex.Message}\" at\n{ex.StackTrace}");
                    #endregion
                    return false;
                }));
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for endOfAsyncSlewEvent");
            #endregion
            endOfAsyncSlewEvent.WaitOne();

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: received endOfAsyncSlewEvent");
            #endregion
            endOfAsyncSlewEvent.Dispose();
            endOfAsyncSlewEvent = null;
        }

        private enum ScopeSlewerStatus { Initial, CloseEnough, ChangedDirection, Canceled, Failed, Timedout };

        private Angle CurrentPosition(Angle.AngleType angleType)
        {
            switch (angleType)
            {
                case Angle.AngleType.RA:
                    return Angle.RaFromHours(RightAscension);
                case Angle.AngleType.HA:
                    return Angle.HaFromHours(HourAngle);
                case Angle.AngleType.Dec:
                    return Angle.DecFromDegrees(Declination);
                default:
                    Exceptor.Throw<Exception>("CurrentPosition", $"Invalid angle type {angleType}");
                    return Angle.Invalid;
            }
        }

        private void ScopeAxisSlewer(Angle targetAngle)
        {
            TelescopeAxes thisAxis = (targetAngle.Type == Angle.AngleType.RA || targetAngle.Type == Angle.AngleType.HA) ?
                TelescopeAxes.axisPrimary :
                TelescopeAxes.axisSecondary;

            Angle currentAngle = CurrentPosition(targetAngle.Type);

            string slewerName = $"{thisAxis}Slewer";
            DateTime start = DateTime.Now;

            string op = $"ScopeAxisSlewer(to: {targetAngle.ToNiceString()}): type: {targetAngle.Type}, from: {currentAngle.ToNiceString()}";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op} ...");
            #endregion

            ScopeSlewerStatus status = ScopeSlewerStatus.Initial;
            ShortestDistanceResult distanceToTarget = currentAngle.ShortestDistance(targetAngle);
            double r = Const.rateStopped;
            int nRates = rates.Count, closeEnoughRates = 0;

            //
            // The outer loop is bounded now.  It never was: AbortSlew on a leg timeout was
            //  the only thing that ended it, and that has been removed so a timeout can be
            //  retried at a faster rate.  Without a cap, an axis that cannot converge - a
            //  stuck encoder, a motor that will not move - would spin here forever.
            //
            // Passes are cheap when things are healthy: a normal slew completes in one.
            //
            const int maxCascadePasses = 5;
            int cascadePasses = 0;

            try
            {
                while (closeEnoughRates != nRates)
                {
                    if (++cascadePasses > maxCascadePasses)
                    {
                        AbortSlew($"{op}: giving up after {maxCascadePasses} passes, still " +
                            $"{distanceToTarget.angle.ToNiceString()} from target");
                        break;
                    }

                    closeEnoughRates = 0;

                    foreach (var rate in rates)
                    {
                        DateTime lastVelocitySampleTime = DateTime.MinValue;
                        TimeSpan velocitySampleInterval = TimeSpan.FromSeconds(1);
                        double lastVelocitySampleRadians = 0;

                        r = rate;
                        telescopeCT.ThrowIfCancellationRequested();

                        currentAngle = CurrentPosition(targetAngle.Type);

                        // let the other axis know we're ready to move at this rate
                        readyToSlewFlags.AxisBecomesReadyToMoveAtRate(thisAxis, rate);

                        // check how far we are from target
                        distanceToTarget = currentAngle.ShortestDistance(targetAngle);
                        if (!EnoughDistanceToMove(thisAxis, distanceToTarget.angle, rate))
                        {
                            // there's not enough distance to move at this rate
                            closeEnoughRates++;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{op}: {slewerName}: distance {distanceToTarget.angle.ToNiceString()} too short for {RateName(rate)} (closeEnoughRates: {closeEnoughRates})");
                            #endregion
                            continue;
                        }

                        if (TooFarToMoveAtRate(thisAxis, distanceToTarget.angle, rate))
                        {
                            //
                            // Too far for this rate - a faster one must take it.  Note this
                            //  does NOT count towards closeEnoughRates: we have not arrived.
                            //
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{op}: {slewerName}: distance {distanceToTarget.angle.ToNiceString()} too FAR for {RateName(rate)}, leaving it to a faster rate");
                            #endregion
                            continue;
                        }

                        // enough distance to move, let's wait for the other axis
                        while (!readyToSlewFlags.AxisCanMoveAtRate(thisAxis, rate))
                        {
                            currentAngle = CurrentPosition(targetAngle.Type);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                 $"{op}: {slewerName} at {RateName(rate)}: current: {currentAngle} waiting for the other axis ...");
                            #endregion
                            telescopeCT.ThrowIfCancellationRequested();
                            Thread.Sleep(50);
                        }

                        currentAngle = CurrentPosition(targetAngle.Type);
                        distanceToTarget = currentAngle.ShortestDistance(targetAngle);

                        //
                        // RE-CHECK, because the rate was chosen BEFORE the rendezvous above
                        //  and that wait is unbounded - it lasts as long as the other axis
                        //  needs, which has been over 90 seconds.  The distance measured then
                        //  can be meaningless by now.
                        //
                        // This is not hypothetical.  On 2026-09-17 the RA axis measured 10.5
                        //  arcsec, correctly skipped slew and set, committed to guide, and
                        //  then waited 64 seconds for Dec.  During that wait the reported
                        //  position jumped 1875 arcsec.  RA drove the whole phantom distance
                        //  at 0.86 arcsec/sec until it hit maxTime, and the slew was aborted.
                        //  Set was never reconsidered, though it was 58x faster and idle.
                        //
                        if (!EnoughDistanceToMove(thisAxis, distanceToTarget.angle, rate))
                        {
                            closeEnoughRates++;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{op}: {slewerName}: after waiting, distance {distanceToTarget.angle.ToNiceString()} is too short for {RateName(rate)} (closeEnoughRates: {closeEnoughRates})");
                            #endregion
                            continue;
                        }

                        if (TooFarToMoveAtRate(thisAxis, distanceToTarget.angle, rate))
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{op}: {slewerName}: after waiting, distance {distanceToTarget.angle.ToNiceString()} is too FAR for {RateName(rate)}, leaving it to a faster rate");
                            #endregion
                            continue;
                        }

                        //
                        // Coordinate sense to MOTOR sense.  movementDict is keyed in right
                        //  ascension sense, so an hour-angle target needs the opposite motor -
                        //  see Angle.MechanicalDirection, which carries the full story.  For RA
                        //  and Dec this is the identity, so nothing else changes.
                        //
                        Const.AxisDirection motorDirection =
                            Angle.MechanicalDirection(distanceToTarget.direction, targetAngle.Type);

                        // Wait for InternalMoveAxis to start moving thisAxis
                        while (! InternalMoveAxis(thisAxis, rate, motorDirection, false))
                        {
                            const int waitForAxisToStartMovingMillis = 500;

                            currentAngle = CurrentPosition(targetAngle.Type);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: {slewerName}: at {currentAngle} waiting {waitForAxisToStartMovingMillis} " +
                                $"millis to start InternalMoveAxis({thisAxis}, {RateName(rate)}, {motorDirection}) ...");
                            #endregion
                            telescopeCT.ThrowIfCancellationRequested();
                            Thread.Sleep(waitForAxisToStartMovingMillis);
                            telescopeCT.ThrowIfCancellationRequested();
                        }

                        ShortestDistanceResult currentDistance = null;
                        MovementParameters mp = movementParameters[thisAxis][rate];

                        Angle startingPosition = CurrentPosition(targetAngle.Type);
                        DateTime startingTime = DateTime.Now;
                        ShortestDistanceResult startingDistance = startingPosition.ShortestDistance(targetAngle);
                        const double lowestRad = Double.MaxValue, highestRad = Double.MinValue;

                        //
                        // Closest the axis has come to the target during this leg, and since when
                        //  it has been further away than that.  DateTime.MinValue means "not
                        //  currently diverging".
                        //
                        double closestRad = Double.MaxValue;
                        DateTime divergingSince = DateTime.MinValue;

                        //
                        // Throttle for the progress line inside the loop below.  Declared HERE
                        //  rather than in the loop body, which is where it was: a fresh
                        //  "byte count = 0" every iteration made "(count %= 5) == 0" always
                        //  true, so the intended one-in-five logged every time - at 10ms, per
                        //  axis.  The count++ at the bottom was dead.
                        //
                        int progressTicks = 0;
                        //
                        // Initialised because the diverging check below can break out of
                        //  the loop before this is assigned.
                        //
                        TimeSpan elapsed = TimeSpan.Zero;

                        //
                        // When the distance-to-target was FIRST seen to have reversed sign.
                        //  DateTime.MinValue means "not currently reversed".
                        //
                        DateTime directionChangedAt = DateTime.MinValue;

                        #region Velocity
                        string motors = "";
                        foreach (WiseVirtualMotor m in axisMotors[thisAxis])
                            if (m.IsOn) motors += m.WiseName + ",";
                        if (thisAxis == TelescopeAxes.axisPrimary && TrackingMotor.IsOn)
                            motors += "Tracking,";
                        motors = motors.TrimEnd(',');
                        #endregion

                        // The axis was set in motion, wait for it to either arrive close enough or overshoot
                        while (true)    // Check if we arrived as far as this rate gets us
                        {
                            telescopeCT.ThrowIfCancellationRequested();

                            currentAngle = CurrentPosition(targetAngle.Type);
                            currentDistance = currentAngle.ShortestDistance(targetAngle);

                            //
                            // IS THE AXIS GETTING CLOSER?
                            //
                            // The one check that does not need to know WHY something is wrong.  On
                            //  2026-09-19 an HA-targeted slew ran 77 seconds and about 80 degrees
                            //  the wrong way, into a limit switch, and nothing in the loop noticed:
                            //  the distance arithmetic was wrong by 15x and the direction-to-motor
                            //  mapping was inverted, so the axis drove away from its target while
                            //  every existing exit condition stayed quiet.
                            //
                            //  ChangedDirection could not catch it either, and correctly so: it
                            //  watches for the direction FLIPPING, and the direction never flipped
                            //  - the target stayed west of the axis the whole time, because the axis
                            //  was running east.  Driving away from a target is invisible to that
                            //  test.
                            //
                            // There WAS a check for this, and it had never run: prevDistance was
                            //  assigned after the while loop closed, so it stayed 0.0 for the
                            //  loop's entire life and its `else if (prevDistance != 0.0)` never
                            //  fired.  Its body only logged, with the status assignment and break
                            //  commented out.  It is gone, replaced by this.
                            //
                            // Deliberately NOT part of the else-if chain below.  The dead branch
                            //  sat above the CloseEnough test, so repairing it in place would have
                            //  disabled arrival detection on every leg - that hazard is why it was
                            //  left alone on 2026-09-17 rather than fixed.
                            //
                            // Two conditions, so ordinary behaviour cannot trip it:
                            //
                            //  . the distance must exceed the CLOSEST approach so far by more than
                            //    mp.stopMovement.  That scales with the rate for free - about 3
                            //    degrees at slew, 34 arcsec at set, 3 arcsec at guide - and an axis
                            //    that is converging never diverges from its own best approach at
                            //    all.  An axis merely sitting still, waiting on the rendezvous or
                            //    held by the other axis' TeleSlew, holds its distance constant and
                            //    is not diverging either.
                            //
                            //  . it must stay that way for divergingConfirmMillis, so a single
                            //    spurious encoder reading cannot abandon a good slew.
                            //
                            // At slew rate this trips after roughly 1.6 seconds and 3 degrees of
                            //  wrong-way travel, against the 77 seconds and 80 degrees it took to
                            //  reach a limit switch unaided.
                            //
                            // AbortSlew rather than status = Failed: Failed only ends this RATE,
                            //  and the cascade would retry up to maxCascadePasses times, giving a
                            //  runaway five more chances.  AbortSlew cancels telescopeCTS, which
                            //  the ThrowIfCancellationRequested above turns into the
                            //  OperationCanceledException this method already handles.
                            //
                            double currentRad = currentDistance.angle.Radians;

                            if (currentRad < closestRad)
                            {
                                closestRad = currentRad;
                                divergingSince = DateTime.MinValue;
                            }
                            else if (currentRad > closestRad + mp.stopMovement.Radians)
                            {
                                if (divergingSince == DateTime.MinValue)
                                {
                                    divergingSince = DateTime.Now;
                                }
                                else if (DateTime.Now.Subtract(divergingSince).TotalMilliseconds >= divergingConfirmMillis)
                                {
                                    #region debug
                                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"SUSPECT: {op}: {slewerName} at {RateName(rate)}: at {currentAngle}, " +
                                        $"DIVERGING ==> target: {targetAngle}: distance " +
                                        $"{currentDistance.angle.ToNiceString()} is more than " +
                                        $"{mp.stopMovement.ToNiceString()} beyond the closest approach of " +
                                        $"{Angle.FromRadians(closestRad, targetAngle.Type).ToNiceString()}, " +
                                        $"for {DateTime.Now.Subtract(divergingSince).TotalMilliseconds:f0}ms - aborting the slew");
                                    #endregion
                                    status = ScopeSlewerStatus.Failed;
                                    AbortSlew($"{op}: {slewerName} at {RateName(rate)}: axis is moving AWAY from the target");
                                    break;
                                }
                            }

                            elapsed = DateTime.Now.Subtract(startingTime);
                            if (elapsed >= mp.maxTime) {
                                #region Timeout
                                status = ScopeSlewerStatus.Timedout;
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"SUSPECT: {op}: {slewerName} at {RateName(rate)}: at {currentAngle}, Timedout ==> target: {targetAngle}, elapsed: {elapsed} >= mp.maxTime: {mp.maxTime}");
                                break;
                                #endregion
                                #endregion
                            }

                            //
                            // A direction change ends the leg, so it must be CONFIRMED rather
                            //  than taken from one reading.
                            //
                            // On 2026-09-18 07:12:59 a single spurious reading of 18h18m27.4s
                            //  arrived while the axis was really at 05h40m55.9s (85ms later).
                            //  The distance to target flipped sign, this test fired on that one
                            //  sample, and the slew was cut 9.57 degrees short - handing 6.875
                            //  degrees to the set leg at 52 arcsec/sec, a four minute stall.
                            //  The 2026-09-16 logs hold 86 ChangedDirection events over 52
                            //  slews, ~1.65 per slew, so this was not a one-off.
                            //
                            // Note the reading never passes through AxisMonitor.Acceptable():
                            //  that filter guards the monitor's own sample queue, not
                            //  CurrentPosition(), which the slewer's arithmetic uses.
                            //
                            // Deliberately NOT part of the else-if chain below: while a
                            //  reversal is unconfirmed the CloseEnough test must keep running,
                            //  so a genuine overshoot still stops the motor immediately and
                            //  only the ChangedDirection exit waits.  At slew rate the wait
                            //  costs 0.28 degrees against a coast of 2.0-2.8 degrees.
                            //
                            //
                            // Which coast applies depends on which way this axis is going.
                            //  Falls back to the single value when a rate has no direction
                            //  specific figure, so every other axis and rate is unchanged.
                            //
                            // MECHANICAL sense here too, not the coordinate one: the two figures
                            //  were measured going east and going west.  On an hour-angle target
                            //  the raw direction would pick the wrong one of the pair - about 0.9
                            //  degrees of coast apart on RA at slew rate, so a landing error
                            //  rather than a hazard, but wrong.
                            Angle stopMovement =
                                (mp.stopMovementIncreasing != null &&
                                 Angle.MechanicalDirection(currentDistance.direction, targetAngle.Type)
                                     == Const.AxisDirection.Increasing)
                                    ? mp.stopMovementIncreasing
                                    : mp.stopMovement;

                            bool directionReversed = startingDistance.direction != currentDistance.direction;

                            if (directionReversed && directionChangedAt == DateTime.MinValue)
                            {
                                directionChangedAt = DateTime.Now;      // first sighting, start the clock
                            }
                            else if (!directionReversed && directionChangedAt != DateTime.MinValue)
                            {
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"SUSPECT: {op}: {slewerName} at {RateName(rate)}: " +
                                        $"at {currentAngle}, a reversal did not hold for " +
                                        $"{directionChangeConfirmMillis}ms - discarding it as a spurious reading");
                                #endregion
                                directionChangedAt = DateTime.MinValue;
                            }

                            if (directionReversed && directionChangedAt != DateTime.MinValue &&
                                DateTime.Now.Subtract(directionChangedAt).TotalMilliseconds >= directionChangeConfirmMillis)
                            {
                                #region Direction has changed
                                status = ScopeSlewerStatus.ChangedDirection;
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName} at {RateName(rate)}: " +
                                        $"at {currentAngle}, ChangedDirection ==> target: {targetAngle}, " +
                                        $"originalDirection: {startingDistance.direction} != " +
                                        $"currentDistance.direction: {currentDistance.direction}, " +
                                        $"confirmed over {DateTime.Now.Subtract(directionChangedAt).TotalMilliseconds:f0}ms");
                                #endregion
                                break;
                                #endregion
                            }
                            else if (currentDistance.angle <= stopMovement)
                            {
                                #region Reached target
                                status = ScopeSlewerStatus.CloseEnough;
                                double deltaRad = Math.Abs(currentDistance.angle.Radians - stopMovement.Radians);
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName}:{GetHashCode()} at {RateName(rate)}: at {currentAngle}, " +
                                        $"CloseEnough ==> target: {targetAngle}, " +
                                        $"currentDistance.angle.rad: {currentDistance.angle.Radians} <= stopMovement.rad: {stopMovement.Radians}" +
                                        $"delta.rad: {deltaRad}");
                                #endregion
                                break;
                                #endregion
                            }
                            else
                            {
                                double deltaRad = Math.Abs(currentDistance.angle.Radians - stopMovement.Radians);
                                #region Try to catch anomalies
                                //if (deltaRad < lowestRad)
                                //    lowestRad = deltaRad;
                                //if (deltaRad > highestRad)
                                //    highestRad = deltaRad;

                                DateTime now = DateTime.Now;
                                DateTime startVelocity = DateTime.MinValue;
                                if (lastVelocitySampleTime == DateTime.MinValue)
                                {
                                    startVelocity = now;
                                    lastVelocitySampleTime = now;
                                    lastVelocitySampleRadians = currentDistance.angle.Radians;
                                }
                                else if ((now - lastVelocitySampleTime) >= velocitySampleInterval)
                                {
                                    double dx = Math.Abs(currentDistance.angle.Radians - lastVelocitySampleRadians);
                                    double dt = (now - lastVelocitySampleTime).TotalMilliseconds;

                                    #region debug
                                    string dbg = $"mp[{thisAxis}, {RateName(rate)}, {motors}].velocity: " +
                                        $"{dx / dt:f10} rad/ms, millis: {(now - startVelocity).TotalMilliseconds}, " +
                                        $"dx: {dx:f10}, dt: {dt:f10}, {DirectionMotorsAreActive}, " +
                                        $"curr: {currentAngle.Radians:f10}, target: {targetAngle.Radians:f10}, delta: {Math.Abs(targetAngle.Radians - currentAngle.Radians):f10}";
                                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, dbg);
                                    #endregion
                                    lastVelocitySampleTime = now;
                                    lastVelocitySampleRadians = currentDistance.angle.Radians;

                                    //    double minRad = mp.minRadChangePerPollingInterval * 0.9;
                                    //    double maxRad = mp.maxRadChangePerPollingInterval * 1.1;

                                    //    if (dx == 0.0 && !AxisIsStopping(thisAxis) && ++zeroFailures > maxZeroFailures)
                                    //    {
                                    //        #region debug
                                    //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    //            $"SUSPECT: NO MOVEMENT: axis: {thisAxis}, {RateName(rate)}, dx: {dx:f10} == 0.0");
                                    //        #endregion
                                    //        zeroFailures = 0;
                                    //        //status = ScopeSlewerStatus.Failed;
                                    //        //break;
                                    //    }

                                    //    if (dx < minRad && lowFailures++ > maxLowFailures)
                                    //    {
                                    //        #region debug
                                    //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    //            $"SUSPECT: TOO LITTLE MOVEMENT: axis: {thisAxis}, {RateName(rate)}, dx: {dx:f10} < expected: {minRad:f10}");
                                    //        #endregion
                                    //        lowFailures = 0;
                                    //        //status = ScopeSlewerStatus.Failed;
                                    //        //break;
                                    //    }

                                    //    if (dx > maxRad && highFailures++ > maxHighFailures)
                                    //    {
                                    //        #region debug
                                    //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                    //            $"SUSPECT: TOO MUCH MOVEMENT: axis: {thisAxis}, {RateName(rate)}, dx: {dx:f10} > expected: {maxRad:f10}");
                                    //        #endregion
                                    //        highFailures = 0;
                                    //        //status = ScopeSlewerStatus.Failed;
                                    //        //break;
                                    //    }
                                }
                                #endregion
                                #region debug
                                //
                                // One line per 5 iterations, and only if DebugAxes is on.
                                //
                                // The loop polls every 10ms but CurrentPosition is refreshed by
                                //  AxisMonitor every 50ms, so four of every five iterations had
                                //  nothing new to report and logged it anyway.  One in five
                                //  matches the rate at which the data actually changes.
                                //
                                // The Debugging() test is not redundant with WriteLine's own:
                                //  C# builds an interpolated string at the CALL SITE, so
                                //  without this the line is fully formatted - four Angle
                                //  ToString calls among them - and then thrown away.
                                //
                                if (++progressTicks % 5 == 0 && Debugger.Debugging(Debugger.DebugLevel.DebugAxes))
                                {
                                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName} at {RateName(rate)}: at {currentAngle}, " +
                                        $"moving ==> target: {targetAngle}, " +
                                        $"remaining (Angle.rad: {currentDistance.angle.Radians:f10}, direction: {currentDistance.direction}) > " +
                                        $"stopMovement.rad: {stopMovement.Radians:f10}, deltaRad: {deltaRad:f10} sleeping {mp.pollingFreqMillis} millis ...");
                                }
                                #endregion debug
                                telescopeCT.ThrowIfCancellationRequested();
                                Thread.Sleep(mp.pollingFreqMillis);
                                telescopeCT.ThrowIfCancellationRequested();
                                // not there yet, continue looping
                            }
                        }
                        if (status == ScopeSlewerStatus.Failed ||
                            status == ScopeSlewerStatus.CloseEnough ||
                            status == ScopeSlewerStatus.Timedout ||
                            status == ScopeSlewerStatus.ChangedDirection)
                        {
                            StopAxisAndWaitForHalt(thisAxis, slewerName, rate);
                            #region Velocity
                            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                                $"mp[{motors}].lowestRad: {lowestRad:f10}, highestRad: {highestRad:f10}, rate: {RateName(rate)}");
                            #endregion
                        }

                        //
                        // A leg timing out used to AbortSlew, killing the whole slew for both
                        //  axes.  It is now recoverable: fall out of this rate and let the
                        //  outer loop re-run the cascade, which re-measures and can pick a
                        //  faster rate.
                        //
                        // On 2026-09-17 the guide leg timed out holding 1875 arcsec it could
                        //  never cover.  Simply re-running the cascade would have handed that
                        //  to set, which clears it in 38 seconds - instead the slew aborted.
                        //  A timeout says "this rate is not working", not "give up".
                        //
                        // The pass cap below is what makes that safe: AbortSlew was the only
                        //  thing bounding the outer loop.
                        //
                        if (status == ScopeSlewerStatus.Timedout)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{op}: {slewerName}: {RateName(rate)} timed out after {elapsed.ToMinimalString()} " +
                                $"with {distanceToTarget.angle.ToNiceString()} to go - retrying the cascade");
                            #endregion
                        }
                    }
                }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: Done at {currentAngle} target: {targetAngle}, " +
                    $"distance-to-target: {distanceToTarget.angle.ToNiceString()}, status: {status}, total-duration: {DateTime.Now.Subtract(start)}");
                #endregion
            }
            catch (OperationCanceledException)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"{op}: at {RateName(r)}: Slew cancelled at {currentAngle}");
                #endregion debug
                StopAxisAndWaitForHalt(thisAxis, slewerName, r);
                //status = ScopeSlewerStatus.Canceled;
                throw;
            }
        }

        //private double  SelectHighestRate(TelescopeAxes axis, Angle distance)
        //{
        //    MovementParameters mp;

        //    foreach (var r in rates)
        //    {
        //        mp = movementParameters[axis][r];
        //        Angle minimalMovementAngle = mp.minimalMovement + mp.stopMovement;

        //        if (distance >= minimalMovementAngle)
        //        {
        //            #region debug
        //            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "SelectHighestRate: {0} selected {1}, distance: {2} >= minimalMovementAngle: {3} (minimal-movement: {4} + stop-movement: {5})",
        //                axis.ToString(), RateName(r), distance, minimalMovementAngle, mp.minimalMovement, mp.stopMovement);
        //            #endregion debug
        //            return r;
        //        }
        //    }

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "SelectHighestRate: {0} selected {1}, distance: {2}",
        //        axis.ToString(), RateName(Const.rateStopped), distance);
        //    #endregion debug
        //    return Const.rateStopped;
        //}

        private bool EnoughDistanceToMove(TelescopeAxes axis, Angle distance, double rate)
        {
            MovementParameters mp = movementParameters[axis][rate];
            Angle minimalMovementAngle = mp.minimalMovement + mp.stopMovement;

            return distance >= minimalMovementAngle;
        }

        /// <summary>
        /// Whether this distance is more than this rate should be asked to cover.
        ///
        /// The counterpart to EnoughDistanceToMove, which only ever tested a MINIMUM.  With
        ///  no upper bound, rateGuide would accept any distance at all - and at 0.86
        ///  arcsec/sec its 5 minute maxTime buys only 258 arcsec, so anything larger is a
        ///  guaranteed timeout rather than a slow arrival.
        ///
        /// Only rateGuide declares a maximalMovement.  That is deliberate: every distance
        ///  must have some rate willing to take it, or ScopeAxisSlewer's outer loop never
        ///  terminates.  A null maximalMovement means "no limit".
        ///
        /// Declining as TOO FAR is not the same as being close enough, and the caller must
        ///  not count it towards closeEnoughRates - the slew has not arrived, it just needs
        ///  a faster rate.
        /// </summary>
        private bool TooFarToMoveAtRate(TelescopeAxes axis, Angle distance, double rate)
        {
            MovementParameters mp = movementParameters[axis][rate];

            return mp.maximalMovement != null && distance > mp.maximalMovement;
        }

        private static readonly Dictionary<TelescopeAxes, bool> AxisIsStoppingDict = new Dictionary<TelescopeAxes, bool>()
        {
            [TelescopeAxes.axisPrimary] = false,
            [TelescopeAxes.axisSecondary] = false,
        };

        //private bool AxisIsStopping(TelescopeAxes axis)
        //{
        //    return AxisIsStoppingDict[axis];
        //}

        public void StopAxisAndWaitForHalt(TelescopeAxes axis, string slewerName = null, double rate = Const.rateStopped)
        {
            string msg = string.Empty;
            if (slewerName != null && rate != Const.rateStopped)
                msg = $"{slewerName} at {RateName(rate)}: ";

            AxisIsStoppingDict[axis] = true;
            StopAxis(axis);

            #region debug
            Angle a = (axis == TelescopeAxes.axisPrimary) ?
                Angle.RaFromHours(RightAscension) :
                Angle.DecFromDegrees(Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, msg + $"at {a} waiting for {axis} to stop moving ...");
            #endregion debug
            //
            // Judged against the rate that was just running.  After a set leg the next rate is
            //  guide at 0.7 arcsec/sec, so "stopped" has to mean a good deal slower than that -
            //  which the old single threshold, equivalent to 6.4 arcsec/sec, did not.
            //
            while (AxisIsMoving(axis, rate))
            {
                Thread.Sleep(waitForOtherAxisMillis);
            }
            AxisIsStoppingDict[axis] = false;
            #region debug
            Angle b = (axis == TelescopeAxes.axisPrimary) ?
                Angle.RaFromHours(RightAscension) :
                Angle.DecFromDegrees(Declination);
            Angle stoppingDistance = b.ShortestDistance(a).angle;
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, msg + $"at {b} {axis} has stopped moving (stopping distance: {stoppingDistance.ToNiceString()})");
            #endregion debug
        }

        private static SlewerTask domeSlewer;

        private static void CheckDomeActionCancelled(object StateObject)
        {
            if (Instance.domeCT.IsCancellationRequested)
            {
                domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Instance.SyncDomePosition = false;
                DomeSlaveDriver.AbortSlew();
            }
        }

        private static System.Threading.Timer domeSlewTimer;

        private void GenericDomeSlewerTask(Action action)
        {
            domeSlewer = new SlewerTask() { type = Slewers.Type.Dome, task = null };
            domeCT = domeCTS.Token;
            domeSlewTimer = new System.Threading.Timer(new TimerCallback(CheckDomeActionCancelled));

            slewers.Add(domeSlewer);
            domeSlewer.task = Task.Run(() =>
                {
                    try
                    {
                        domeSlewTimer.Change(100, 100);
                        action();
                    }
                    catch (OperationCanceledException)
                    {
                        domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        DomeSlaveDriver.AbortSlew();
                        slewers.Delete(Slewers.Type.Dome);
                    }
                }, domeCT).ContinueWith((domeSlewerTask) =>
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"slewer \"{Slewers.Type.Dome}\" completed with status: {domeSlewerTask.Status}");
                    #endregion
                    domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    slewers.Delete(Slewers.Type.Dome);
                }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void DomeSlewer(Angle primaryAngle, Angle dec, string reason)
        {
            GenericDomeSlewerTask(() => domeSlaveDriver.SlewToAz(primaryAngle, dec, reason));
        }

        public void DomeSlewer(double az, string reason)
        {
            GenericDomeSlewerTask(() => domeSlaveDriver.SlewToAz(az, reason));
        }

        public void DomeParker()
        {
            GenericDomeSlewerTask(() => domeSlaveDriver.Park());
        }

        public void DomeCalibrator()
        {
            GenericDomeSlewerTask(() => domeSlaveDriver.FindHome());
        }

        public void DomeStopper()
        {
            domeCTS.Cancel();
            domeCTS = new CancellationTokenSource();
            SyncDomePosition = false;
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        private void DoSlewToCoordinatesAsync(Angle primaryTargetAngle, Angle secondaryTargetAngle, string reason, bool noSafetyCheck = false)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            string op = "DoSlewToCoordinatesAsync(" +
                $"{primaryTargetAngle.ToNiceString()}, " +
                $"{secondaryTargetAngle.ToNiceString()}, " +
                $"reason: {reason})";

            //
            // Nothing new starts while a recovery is in progress.  This is the funnel all three
            //  slew entry points reach - SlewToCoordinatesAsync, SlewToHaDecAsync and
            //  SlewToAltAzAsync - so one guard covers them all.
            //
            // A slew accepted mid-backoff is worse than the handpad case that prompted this: it
            //  builds a new telescopeCTS and new slewer tasks, and then the recovery's trailing
            //  MoveAxis(rateStopped) lands on one of their axes a second or two later, leaving a
            //  slew running on one axis only.
            //
            // Refused rather than queued, matching what Tracking.set already does for the same
            //  condition.  A client that sees the exception can retry; ACP already handles one.
            //
            if (RecoveringSafety)
                Exceptor.Throw<InvalidOperationException>(op, "Safety recovery is active");

            Angle.AngleType primaryAngleType = primaryTargetAngle.Type;

            //
            // An hour-angle target is a MOUNT-frame target: it names where the axis should
            //  point, not a place on the sky.  Tracking has to be off for the axis to hold
            //  it, for two independent reasons:
            //
            //  . The track motor drives the hour angle west at the sidereal rate, 15.04
            //    arcsec/sec, while the slewer's finest correction is rateGuide at about 0.6
            //    arcsec/sec.  The cleanup leg cannot win that race.
            //
            //  . PrimaryAxisMonitor.IsMoving switches on Tracking: with tracking on it judges
            //    motion from _raDeltas, and an axis tracking at sidereal shows a right
            //    ascension delta of zero.  It would read "stopped" while the hour angle walks
            //    away.
            //
            // Right ascension targets are the opposite case and are left alone - there
            //  tracking is what HOLDS the target, and it is invisible to _raDeltas by design.
            //
            // Tracking is deliberately NOT restored afterwards.  Arriving at an hour angle
            //  and then letting the mount drift off it would defeat the point; the caller
            //  re-enables tracking if it wants to follow the sky.
            //
            if (primaryAngleType == Angle.AngleType.HA && Tracking)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: hour-angle target, turning Tracking off so the axis can hold it.");
                #endregion
                Tracking = false;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Before CheckCoordinateSanity.");
            #endregion
            CheckCoordinateSanity(primaryAngleType, primaryTargetAngle.Hours, reason);
            CheckCoordinateSanity(secondaryTargetAngle.Type, secondaryTargetAngle.Degrees, reason);
            if (!noSafetyCheck)
                RejectUnsafeTarget(primaryTargetAngle, secondaryTargetAngle, op);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: After CheckCoordinateSanity.");
            #endregion

            Slewers.Clear();
            readyToSlewFlags.Reset();
            activityMonitor.NewActivity(new Activity.TelescopeSlew(new Activity.TelescopeSlew.StartParams()
            {
                start = new Activity.TelescopeSlew.Coords()
                {
                    ra = CurrentPosition(primaryAngleType).Hours,
                    dec = Declination,
                },
                target = new Activity.TelescopeSlew.Coords()
                {
                    ra = primaryTargetAngle.Hours,
                    dec = secondaryTargetAngle.Degrees
                }
            }));

            ShortestDistanceResult primaryDistance =
                primaryTargetAngle.ShortestDistance(CurrentPosition(primaryAngleType));
            ShortestDistanceResult secondaryDistance = secondaryTargetAngle.ShortestDistance(Angle.DecFromDegrees(Declination));

            if (! EnoughDistanceToMove(TelescopeAxes.axisPrimary, primaryDistance.angle, Const.rateGuide) &&
                ! EnoughDistanceToMove(TelescopeAxes.axisSecondary, secondaryDistance.angle, Const.rateGuide))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Too short.");
                #endregion
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.TelescopeSlew, new Activity.TelescopeSlew.EndParams
                    {
                        endState = Activity.State.Ignored,
                        endReason = "Distance too short",
                        end = new Activity.TelescopeSlew.Coords()
                        {
                            ra = RightAscension,
                            dec = Declination,
                        },
                    });

                if (WiseTele.endOfAsyncSlewEvent != null)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Too short, generating endOfAsyncSlewEvent.");
                    #endregion
                    endOfAsyncSlewEvent.Set();
                }
                return;
            }

            try
            {
                if (EnslavesDome)
                {
                    DomeSlewer(primaryTargetAngle, secondaryTargetAngle, "Follow telescope to new target");
                }

                telescopeCTS = new CancellationTokenSource();
                telescopeCT = telescopeCTS.Token;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                    $"{op}: New telescopeCTS (#{telescopeCTS.GetHashCode()}), telescopeCT: (#{telescopeCT.GetHashCode()})");
                #endregion

                List<Slewers.Type> slewerTypes = primaryAngleType == Angle.AngleType.RA ?
                    new List<Slewers.Type>() { Slewers.Type.Ra, Slewers.Type.Dec } :
                    new List<Slewers.Type>() { Slewers.Type.Ha, Slewers.Type.Dec };

                foreach (Slewers.Type slewerType in slewerTypes)
                {
                    SlewerTask slewer = new SlewerTask() { type = slewerType, task = null };
                    try
                    {
                        Angle angle = (slewerType == Slewers.Type.Ra || slewerType == Slewers.Type.Ha) ?
                            primaryTargetAngle :
                            secondaryTargetAngle;

                        slewers.Add(slewer);
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Running slewer \"{slewer.type}\" ...");
                        #endregion
                        slewer.task = Task.Run(() => ScopeAxisSlewer(angle), telescopeCT).
                            ContinueWith((slewerTask) =>
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                                $"{op}: Slewer \"{slewer.type}\" completed with status: {slewerTask.Status}");
                            #endregion
                            slewers.Delete(slewerType);

                            //
                            // THE TARGET IS OVER once no axis is still going there.
                            //
                            // Nothing used to retire a target: the only code that cleared the
                            //  fields was Dispose, and the two lines that would have done it after
                            //  a park are commented out in Park().  So a target stayed "current"
                            //  until the chain restarted, and the Dash had to infer that it was
                            //  finished by watching Slewing.
                            //
                            // Ra, Dec and Ha only - NOT the dome.  Slewers.Count includes the dome
                            //  slewer, which can still be turning long after the telescope has
                            //  arrived, and it is arrival that ends the target.
                            //
                            // Only the TYPE is cleared.  The right ascension and declination stay
                            //  readable, because TargetRightAscension.get throws ValueNotSetException
                            //  when null and ASCOM says a read returns the last value set - nulling
                            //  them would make every post-slew read throw at whatever client is
                            //  watching, ACP included.  Type is what the digest and the Dash use to
                            //  decide a target exists, so clearing it is enough and costs nothing.
                            //
                            if (!Slewers.Active(Slewers.Type.Ra) &&
                                !Slewers.Active(Slewers.Type.Dec) &&
                                !Slewers.Active(Slewers.Type.Ha))
                            {
                                if (_targetType != TargetCoordinateType.None)
                                {
                                    #region debug
                                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                                        $"{op}: all axis slewers done - retiring the {_targetType} target");
                                    #endregion
                                    _targetType = TargetCoordinateType.None;
                                }
                            }

                            //
                            // A FAULTED slewer used to vanish without a word.
                            //
                            // Only Canceled was handled, and nothing ever read slewerTask.Exception,
                            //  so a slewer that threw logged one line - "completed with status:
                            //  Faulted" - and took its reason with it.  On 2026-09-20 both slewers
                            //  faulted 3 ms after starting and there was NOTHING in the log to say
                            //  why: no exception, no stack, not even which statement.  A telescope
                            //  that silently declines to move is its own kind of hazard, and it
                            //  cost a deploy cycle to get back to this point.
                            //
                            // Logged at DebugExceptions as well as DebugTele: the first is what one
                            //  greps when something went wrong, the second is where the surrounding
                            //  slew narrative lives.
                            //
                            if (slewerTask.Status == TaskStatus.Faulted)
                            {
                                AggregateException ae = slewerTask.Exception;
                                string detail = (ae == null) ?
                                    "(no exception attached)" :
                                    string.Join(" | ", ae.Flatten().InnerExceptions
                                        .Select(e => $"{e.GetType().Name}: {e.Message} at {e.StackTrace}"));
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                                    $"{op}: Slewer \"{slewer.type}\" FAULTED: {detail}");
                                debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                                    $"{op}: Slewer \"{slewer.type}\" FAULTED: {detail}");
                                #endregion
                            }

                            if (slewerTask.Status == TaskStatus.Canceled)
                            {
                                Exceptor.Throw<OperationCanceledException>(
                                    $"{op}",
                                    $"Slewer \"{slewer.type}\" Canceled");
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (OperationCanceledException ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                            $"{op}: Slewer \"{slewer.type}\": Caught: {(ex.InnerException ?? ex).Message}" +
                            $"at\n{(ex.InnerException ?? ex).StackTrace}");
                        #endregion
                        if (ShuttingDown)
                            throw;
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Failed to run slewer {slewerType}: {ex.Message} at\n{ex.StackTrace}");
                        #endregion
                        slewers.Delete(slewerType);
                    }
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((Func<Exception, bool>)((ex) =>
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, $"{op}: Caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    return false;
                }));
            }
        }

        public void SlewToCoordinates(double RightAscension, double Declination, bool noSafetyCheck = false)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.RaFromHours(TargetRightAscension);
            Angle dec = Angle.DecFromDegrees(TargetDeclination);

            string op = $"SlewToCoordinates({ra}, {dec})";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, op);
            #endregion debug

            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while AtPark");

            if (!Tracking)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while NOT Tracking");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");

            if (!noSafetyCheck)
            {
                string notSafe = SafeAtCoordinates(ra, dec);
                if (!string.IsNullOrEmpty(notSafe))
                    Exceptor.Throw<InvalidOperationException>(op, notSafe);
            }

            try
            {
                InternalSlewToCoordinatesSync(ra, dec, op);
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"SlewToCoordinates: InternalSlewToCoordinatesSync({ra}, {dec}) threw exception: {e.Message} at\n{e.StackTrace}");
                #endregion
            }
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToHaDecAsync(double ha, double dec, string whatfor)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            string op = "SlewToHaDecAsync(" +
                    $"ha: {Angle.HaFromHours(ha).ToNiceString()}, " +
                    $"dec: {Angle.DecFromDegrees(dec).ToNiceString()}, " +
                    $"for: {whatfor})";

            //
            // RE-ENABLED 2026-09-20, after both halves of the 2026-09-19 failure were fixed and
            //  checked offline.  It had been disabled here rather than left to drive.
            //
            // What went wrong then, and what fixed it:
            //
            //  1. THE DISTANCE was 15x too large.  Going from HA -01h21m44.8s to HA 0 - a true
            //     0.357 rad - the slewer logged "remaining (Angle.rad: 5.3502389458)", never
            //     approached arrival, and would have run to mp.maxTime.  Cause: FromRadians
            //     treated every type as degrees, and AngleType.HA is the only type that is both
            //     non-periodic and HMS, so it was the only one reaching the unguarded branch.
            //     Fixed in Angle.FromRadians; TestAngleHa covers it.
            //
            //  2. THE DIRECTION reached the wrong motor.  movementDict is keyed in right
            //     ascension sense, and hour angle increases the other way, so the axis ran EAST
            //     when the target lay WEST.  It covered about 80 degrees in 77 seconds before a
            //     physical limit switch cut motor power at HA -6.7255, inside the then -7.0 soft
            //     limit.  Fixed by Angle.MechanicalDirection at the two places the slewer turns
            //     a coordinate direction into hardware.
            //
            // Declination was correct throughout that slew, which is what localised it to the
            //  hour-angle path.
            //
            // Two guards now stand behind this that did not exist then: the diverging check
            //  aborts an axis that moves away from its target for 150 ms, and the eastern and
            //  western soft limits are +/-6.5h rather than +/-7.0.
            //
            // Park and ParkFromGui deliberately still use right-ascension targets.  They are
            //  what UNATTENDED systems call, and parking is accurate to 24 arcmin of pure lead
            //  error as it stands - see the comment in Park() and [[park-position]].  Moving
            //  them onto this path is a separate decision, to be taken once hour-angle slews
            //  have a record on sky.
            //

            CheckCoordinateSanity(Angle.AngleType.HA, ha, op);
            CheckCoordinateSanity(Angle.AngleType.Dec, dec, op);

            //
            // RECORD THE TARGET.  This path set no target field whatever until 2026-09-20, so an
            //  hour-angle slew showed an empty Target group in the Dash and reported no target to
            //  any ASCOM client.  Invisible while the Action was disabled; a real gap once it
            //  worked.
            //
            // The hour angle is the invariant here, so it is stored as given.  The right
            //  ascension is a SNAPSHOT taken now - it is what makes Digest able to derive
            //  altitude and azimuth - and it goes stale as the sky turns, which is correct: for
            //  an hour-angle target it is the right ascension that moves, not the hour angle.
            //
            _targetHourAngle = Angle.HaFromHours(ha);
            _targetDeclination = Angle.DecFromDegrees(dec);

            double raSnapshot = wisesite.LocalSiderealTime.Hours - ha;
            while (raSnapshot < 0.0) raSnapshot += 24.0;
            while (raSnapshot >= 24.0) raSnapshot -= 24.0;
            _targetRightAscension = Angle.RaFromHours(raSnapshot);

            _targetAltitude = null;     // let Digest derive them from the pair above
            _targetAzimuth = null;
            _targetType = TargetCoordinateType.HaDec;

            //
            // Slew to the hour angle DIRECTLY.
            //
            // This used to convert ha -> ra -> alt/az and call SlewToAltAzAsync, which
            //  converted straight back to hour angle.  Three conversions to arrive where we
            //  started, and two real problems with them:
            //
            //  . The ha -> ra step was evaluated ONCE, here, so the target was pinned to the
            //    local sidereal time at the moment of the call.  The sky keeps turning while
            //    the mount slews, so the telescope landed west of the requested hour angle by
            //    however long the slew took - about 15 arcmin for a 60 second slew.
            //
            //  . It went through Transform's TOPOCENTRIC alt/az, i.e. through refraction, and
            //    depended on WiseSite.och.Temperature being available.  An hour angle is a
            //    mechanical statement about where the axis points; refraction has no business
            //    in it.
            //
            // DoSlewToCoordinatesAsync already accepts a typed angle and switches the slewer
            //  on it (Slewers.Type.Ha vs .Ra), and CurrentPosition() serves AngleType.HA from
            //  the encoder, so the loop now compares encoder hour angle against a FIXED
            //  target.  No time dependence, and no lead needed.
            //
            DoSlewToCoordinatesAsync(Angle.HaFromHours(ha), Angle.DecFromDegrees(dec), op);
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToCoordinatesAsync(double RightAscension, double Declination, string whatfor, bool doChecks = true)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            CheckCoordinateSanity(Angle.AngleType.RA, RightAscension, $"SlewToCoordinatesAsync(for: {whatfor})");
            CheckCoordinateSanity(Angle.AngleType.Dec, Declination, $"SlewToCoordinatesAsync(for: {whatfor})");

            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.RaFromHours(TargetRightAscension);
            Angle dec = Angle.DecFromDegrees(TargetDeclination);

            string op = $"SlewToCoordinatesAsync(ra: {ra.ToNiceString()}, dec: {dec.ToNiceString()}, for: {whatfor})";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, op);
            #endregion

            if (doChecks)
            {
                if (AtPark)
                    Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while AtPark");

                if (!Tracking)
                    Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while NOT Tracking");

                string notSafe = SafeAtCoordinates(ra, dec);
                if (!string.IsNullOrEmpty(notSafe))
                    Exceptor.Throw<InvalidOperationException>(op, notSafe);
            }

            if (!ShuttingDown && !wisesafetooperate.IsSafe)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (EnslavesDome)
            {
                if (Instance._onIdle == OnIdle.ShutDown)
                {
                    if (domeSlaveDriver.ShutterIsMoving)
                        Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");
                }
                else if (Instance._onIdle == OnIdle.HunkerDown)
                {
                    _hunkeredDown = false;
                    _hunkeringUp = true;

                    if (domeSlaveDriver.ShutterState == ShutterState.shutterClosing)
                    {
                        domeSlaveDriver.StopShutter($"{op}");
                        while (domeSlaveDriver.ShutterIsMoving)
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for shutter to stop closing");
                            #endregion
                            Thread.Sleep(5000);
                        }
                    }

                    domeSlaveDriver.OpenShutter(true);
                    while (domeSlaveDriver.ShutterIsMoving)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: waiting for shutter to stop opening");
                        #endregion
                        Thread.Sleep(5000);
                    }
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: shutter stopped opening");
                    #endregion
                    _hunkeringUp = false;
                }
            }

            try
            {
                //
                // doChecks == false is the deliberate bypass - MoveToKnownHaDec uses it for
                //  move-to-preset "cover" at hour angle 11h55m.  Thread it through rather
                //  than letting the absence of a caller-side check be the bypass.
                //
                DoSlewToCoordinatesAsync(ra, dec, op, noSafetyCheck: !doChecks);
            }
            catch (Exception e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"{op}: caught exception: {e.Message} at\n{e.StackTrace}");
                #endregion
            }
        }

//#pragma warning disable IDE1006 // Naming Styles
//#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
//        public void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
//#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
//#pragma warning restore IDE1006 // Naming Styles
//        {
//            string op = $"_slewToCoordinatesAsync({RightAscension.ToNiceString()}, {Declination.ToNiceString()})";

//            //if (DecOver90Degrees)
//            //{
//            //    telescopeCT = telescopeCTS.Token;
//            //    Task southScooter = Task.Run(() =>
//            //    {
//            //        ScootSouth();
//            //    }, telescopeCT).ContinueWith((scooter) =>
//            //    {
//            //        #region debug
//            //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
//            //            "southScooter completed with status: {0}", scooter.Status.ToString());
//            //        #endregion
//            //        DoSlewToCoordinatesAsync(RightAscension, Declination);
//            //    }, TaskContinuationOptions.ExecuteSynchronously);
//            //}
//            //else
//            DoSlewToCoordinatesAsync(RightAscension, Declination, op);
//        }

        //public void ScootSouth()
        //{
        //    if (!DecOver90Degrees)
        //        return;

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Scooting South from {0}, {1}",
        //        Angle.RaFromHours(_instance.RightAscension).ToNiceString(),
        //        Angle.DecFromDegrees(_instance.Declination).ToNiceString());
        //    #endregion

        //    double targetRadians = Angle.Deg2Rad(89.5);

        //    _movingToSafety = true;     // make Slewing true

        //    while (true)
        //    {
        //        double remainingRadians = DecEncoder._angle.Radians - targetRadians;
        //        double selectedRate = Const.rateStopped;

        //        // Select the rate at which to move
        //        foreach (var rate in rates)
        //            if (remainingRadians <= _instance.movementParameters[TelescopeAxes.axisSecondary][rate].minimalMovement.Radians) {
        //                selectedRate = rate;
        //                break;
        //            }
        //        if (selectedRate == Const.rateStopped)
        //        {
        //            // Couldn't find a rate at which to move
        //            _movingToSafety = false;
        //            return;
        //        }

        //        // The rate is selected, get moving
        //        InternalMoveAxis(TelescopeAxes.axisSecondary, selectedRate, Const.AxisDirection.Decreasing, false);
        //        MovementParameters mp = _instance.movementParameters[TelescopeAxes.axisSecondary][selectedRate];
        //        while (true)
        //        {
        //            remainingRadians = DecEncoder._angle.Radians - targetRadians;
        //            if (telescopeCT.IsCancellationRequested || (remainingRadians <= 0 || remainingRadians <= mp.stopMovement.Radians))
        //            {
        //                StopAxisAndWaitForHalt(TelescopeAxes.axisSecondary);
        //                _movingToSafety = false;
        //                return;
        //            }
        //            #region debug
        //            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "ScootSouth: at {0}, remainingRadians: {1}, sleeping 10 millis ...",
        //                selectedRate.ToString(), remainingRadians);
        //            #endregion

        //            Thread.Sleep(10);
        //        }
        //    }
        //}

        public void Unpark()
        {
            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>("Unpark", string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Unpark");
            #endregion debug

            if (AtPark)
                AtPark = false;
        }

        public DateTime UTCDate
        {
            get
            {
                return DateTime.UtcNow;
            }

            set
            {
                Exceptor.Throw<ASCOM.PropertyNotImplementedException>($"UTCDate({value})", "Not implemented", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                return new TrackingRates();
            }
        }

        public void SyncToTarget()
        {
            if (!WiseTele.Instance.Tracking)
                Exceptor.Throw<InvalidOperationException>($"SyncToTarget({TargetRightAscension}, {TargetDeclination})", "NOT Tracking");

            #region debug
            double lst = wisesite.LocalSiderealTime.Hours;
            double ra = TargetRightAscension;
            double dec = TargetDeclination;
            double ha = ra - lst;

            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                $"SyncToTarget(ra: {ra}, dec: {dec}): lst: {lst} " +
                $"Old ha: {WiseTele.Instance.HourAngle}, dec: {WiseTele.Instance.Declination}, " +
                "SyncedCoordinates " +
                    $"ha: {ha}, ha.renishaw.position: {renishawHaEncoder.Position}, ha.radians: {Angle.Hours2Rad(ha)}" + ", " +
                    $"dec: {dec}, dec.renishaw.position: {renishawDecEncoder.Position}, dec.radians: {Angle.Deg2Rad(dec)}");
            #endregion
        }

        /* Oct 6, 2021 - We performed a series of ACP FindLostScope.vbs runs which:
         *      - take an image
         *      - plate-solve the image
         *      - if the solving succeeds, calls Telescope.SyncToTarget(ra, dec) with the center-of-image coordinates
         *      
         *  We recorded the following results:
         * 
         * 19:12:17.669 UT 15452,38,-1        DebugTele        SyncToTarget(ra: 22.2302073643848, dec: 69.9793498080761): lst: 22.5665766126597 Old ha: 0.297172339634292, dec: 70.043211700404, SyncedCoordinates ha: -0.336369248274927, ha.renishaw.position: 19351104, ha.radians: -0.088061263272836, dec: 69.9793498080761, dec.renishaw.position: 18816442, dec.radians: 1.22137006255579
         * 19:27:21.012 UT 15452,47,-1        DebugTele        SyncToTarget(ra: 21.9597835410286, dec: -20.0711833033614): lst: 22.8181922191679 Old ha: 0.818266089634292, dec: -20.000001190221, SyncedCoordinates ha: -0.858408678139263, ha.renishaw.position: 19113632, ha.radians: -0.224730866418336, dec: -20.0711833033614, dec.renishaw.position: 11476719, dec.radians: -0.350308233414968
         * 19:34:03.164 UT 15452,34,-1        DebugTele        SyncToTarget(ra: 22.9654613292657, dec: 29.9303564166369): lst: 22.9302066753452 Old ha: -0.0739621004698747, dec: 29.999949981654, SyncedCoordinates ha: 0.035254653920525, ha.renishaw.position: 19520220, ha.radians: 0.00922964681346432, dec: 29.9303564166369, dec.renishaw.position: 15552436, dec.radians: 0.522383265765726
         * 19:39:31.280 UT 15452,8,-1         DebugTele        SyncToTarget(ra: 2.96039255677, dec: 29.9508174662959): lst: 23.021600097902 Old ha: -3.97831594161571, dec: 29.999803497279, SyncedCoordinates ha: -20.061207541132, ha.renishaw.position: 21299784, ha.radians: -5.25201185278004, dec: 29.9508174662959, dec.renishaw.position: 15552267, dec.radians: 0.522740378450689
         * 19:46:01.572 UT 15452,36,-1        DebugTele        SyncToTarget(ra: 18.960845102289, dec: 29.9386467947406): lst: 23.1303113579014 Old ha: 4.13028594640512, dec: 29.999803497279, SyncedCoordinates ha: -4.16946625561244, ha.renishaw.position: 17604386, ha.radians: -1.09156371316855, dec: 29.9386467947406, dec.renishaw.position: 15552372, dec.radians: 0.52252796015987
         * 20:02:40.379 UT 15452,6,-1         DebugTele        SyncToTarget(ra: 20.9612816477871, dec: 49.9335460587729): lst: 23.4085173476953 Old ha: 2.40853301671762, dec: 49.999901153529, SyncedCoordinates ha: -2.44723569990817, ha.renishaw.position: 18388977, ha.radians: -0.640684808036181, dec: 49.9335460587729, dec.renishaw.position: 17182764, dec.radians: 0.871504785921825
         * 21:01:47.659 UT 15452,15,-1        DebugTele        SyncToTarget(ra: 1.80124914400626, dec: 45.8007149196587): lst: 0.396569788730086 Old ha: -1.44340383224071, dec: 45.859813262904, SyncedCoordinates ha: 1.40467935527617, ha.renishaw.position: 20144316, ha.radians: 0.367744195265406, dec: 45.8007149196587, dec.renishaw.position: 16845348, dec.radians: 0.799373275115335
         * 21:09:14.024 UT 15452,14,-1        DebugTele        SyncToTarget(ra: 2.46106168549742, dec: -0.0601692329452953): lst: 0.52090008659588 Old ha: -1.97914601974071, dec: 9.64660290423975E-05, SyncedCoordinates ha: 1.94016159890154, ha.renishaw.position: 20388470, ha.radians: 0.507933118823842, dec: -0.0601692329452953, dec.renishaw.position: 13107123, dec.radians: -0.00105015122329485
         * 21:13:40.805 UT 15452,27,-1        DebugTele        SyncToTarget(ra: 2.48065753093716, dec: -0.0605717901098018): lst: 0.595208800003721 Old ha: -1.92432831140737, dec: 9.64660290423975E-05, SyncedCoordinates ha: 1.88544873093344, ha.renishaw.position: 20363489, ha.radians: 0.493609323485057, dec: -0.0605717901098018, dec.renishaw.position: 13107128, dec.radians: -0.00105717717124298
         * 21:27:56.470 UT 15452,32,-1        DebugTele        SyncToTarget(ra: 22.9979564149898, dec: -0.00278698826495569): lst: 0.833544540292105 Old ha: 1.79626901932179, dec: 0.067918731654043, SyncedCoordinates ha: 22.1644118746977, ha.renishaw.position: 18668013, ha.radians: 5.80262945972405, dec: -0.00278698826495569, dec.renishaw.position: 13112607, dec.radians: -4.86421214379209E-05
         * 21:36:27.315 UT 15452,6,-1         DebugTele        SyncToTarget(ra: 23.9992357357827, dec: -9.99912507505749): lst: 0.975834156346724 Old ha: 0.936869605259292, dec: -9.92890744022095, SyncedCoordinates ha: 23.023401579436, ha.renishaw.position: 19059563, ha.radians: 6.02751243855031, dec: -9.99912507505749, dec.renishaw.position: 12297813, dec.radians: -0.174517654878478
         * 22:08:44.375 UT 15452,25,-1        DebugTele        SyncToTarget(ra: 2.99988336066172, dec: -9.99880359441257): lst: 1.51537951686333 Old ha: -1.52370982182404, dec: -9.93447384647099, SyncedCoordinates ha: 1.48450384379839, ha.renishaw.position: 20180887, ha.radians: 0.388642197491903, dec: -9.99880359441257, dec.renishaw.position: 12297287, dec.radians: -0.174512043982743
         * 22:20:21.672 UT 15452,12,-1        DebugTele        SyncToTarget(ra: 3.99966014055636, dec: 59.9869785909469): lst: 1.70960342637448 Old ha: -2.32974823328238, dec: 60.043602325404, SyncedCoordinates ha: 2.29005671418188, ha.renishaw.position: 20548281, ha.radians: 0.599535445798147, dec: 59.9869785909469, dec.renishaw.position: 18001277, dec.radians: 1.04697028473537
         * 
         * Using ha = ra - lst, we produced the following:
         * 
         *  HourAngle:
         *              renishaw    ha
         *     lowest:  17604386     -4.16946625561244
         *     highest: 21299784    -20.061207541132
         *   
         *  Declination:         
         *              renishaw    dec
         *     lowest:  11476719    -20.0711833033614
         *     highest: 18816442     69.9793498080761
         */

        public string Description
        {
            get
            {
                return driverDescription;
            }
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                return AlignmentModes.algGermanPolar;
            }
        }

        public double SiderealTime
        {
            get
            {
                return wisesite.LocalSiderealTime.Hours;
            }
        }

        public double SiteElevation
        {
            get
            {
                return WiseSite.Elevation;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"SiteElevation({value})", "Not implemented", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                return WiseSite.Latitude;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"SiteLatitude({value})", "Not implemented", true);
            }
        }

        /// <summary>
        /// Site Longitude in degrees as per ASCOM.DriverAccess
        /// </summary>
        public double SiteLongitude
        {
            get
            {
                return WiseSite.Longitude;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"SiteLongitude({value})", "Not implemented", true);
            }
        }

        public void SlewToTarget()
        {
            Angle ra = Angle.RaFromHours(TargetRightAscension);
            Angle dec = Angle.DecFromDegrees(TargetDeclination);
            string op = $"SlewToTarget({ra.Hours}, {dec.Degrees})";

            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"SlewToTarget - {ra}, {dec}");
            #endregion debug

            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while AtPark");

            if (!Tracking)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while NOT Tracking");

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            string notSafe = SafeAtCoordinates(ra, dec);
            if (string.IsNullOrEmpty(notSafe))
                Exceptor.Throw<InvalidOperationException>(op, notSafe);

            SlewToCoordinates(TargetRightAscension, TargetDeclination); // sync
        }

        public static void SyncToAltAz(double Azimuth, double Altitude)
        {
            //#region debug
            //debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
            //    $"SyncToAltAz(az: {Azimuth}, alt: {Altitude}), " +
            //    $"renishaw ha: {Renishaw.Read(Renishaw.EncoderType.HA)}, dec: {Renishaw.Read(Renishaw.EncoderType.Dec)}");
            //#endregion
            Exceptor.Throw<MethodNotImplementedException>($"SyncToAltAz({Azimuth}, {Altitude})", "SyncToAltAz not implemented");
        }

        public static void SyncToCoordinates(double RightAscension, double Declination)
        {
            if (! WiseTele.Instance.Tracking)
                Exceptor.Throw<InvalidOperationException>($"SyncToCoordinates({RightAscension}, {Declination})", "NOT Tracking");

            //
            // Read the sidereal time and both raw Renishaw counts together, in as
            //  few statements as possible.  This simultaneity IS the measurement:
            //  while tracking, the telescope holds its place on sky - so the solved
            //  coordinates stay good for as long as it stays on the field - but the
            //  AXIS turns at 15 arcsec of hour angle per second.  A count is only
            //  meaningful beside the clock reading taken with it.
            //
            double lst = wisesite.LocalSiderealTime.Hours;
            int haCount = renishawHaEncoder.Position;
            int decCount = renishawDecEncoder.Position;

            //
            // Hour angle is LST - RA.  This had it the other way round, and the
            //  values it logged were used to calibrate the Renishaw HA encoder -
            //  see RenishawEncoder.cs - so that encoder ended up reporting the
            //  negative of the hour angle.  Checked against all four of the
            //  2024-09-11 syncs: LST-RA agrees in sign and to ~0.04h with what the
            //  old encoder read at the time, RA-LST is its mirror image.
            //
            double ha = lst - RightAscension;
            double dec = Declination;

            //
            // One calibration point per solved sync, somewhere it can be fitted -
            //  rather than transcribed by hand out of the debug log, which is how
            //  we ended up calibrating a 32 bit encoder from two points.
            //
            RenishawCalibrationLog.Record(
                lstHours: lst,
                solvedRaHours: RightAscension,
                solvedDecDegrees: Declination,
                throughPole: renishawDecEncoder.Over90Deg,
                haCount: haCount,
                decCount: decCount,
                oldHaHours: WiseTele.Instance.HourAngle,
                oldDecDegrees: WiseTele.Instance.Declination,
                renishawHaHours: renishawHaEncoder.HourAngle,
                renishawDecDegrees: renishawDecEncoder.Declination);

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                $"SyncToCoordinates(ra: {RightAscension}, dec: {Declination}): lst: {lst} " +
                $"Old coord: (ha: {WiseTele.Instance.HourAngle}, dec: {WiseTele.Instance.Declination}), " +
                "New coord: (" +
                    $"ha: {ha}, renishaw: {haCount}, radians: {Angle.Hours2Rad(ha)}" + ", " +
                    $"dec: {dec}, renishaw: {decCount}, radians: {Angle.Deg2Rad(dec)})" +
                $", calibration point logged to {RenishawCalibrationLog.Path}");
            #endregion
            //Exceptor.Throw<MethodNotImplementedException>($"SyncToCoordinates({RightAscension}, {Declination})", "SyncToCoordinates not implemented");
        }

        public static bool CanMoveAxis(TelescopeAxes Axis)
        {
            switch (Axis) {
                case TelescopeAxes.axisPrimary: return true;   // Right Ascension
                case TelescopeAxes.axisSecondary: return true; // Declination
                case TelescopeAxes.axisTertiary: return false; // Image Rotator/Derotator
                default:
                    Exceptor.Throw<InvalidValueException>($"CanMoveAxis({Axis})", "Bad axis, should be: 0 to 2");
                    return false;
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                return WiseSite.OperationalProfile.EquatorialSystem;
            }
        }

        public static void FindHome()
        {
            Exceptor.Throw<MethodNotImplementedException>("FindHome", "Not implemented");
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        public static PierSide DestinationSideOfPier(double RightAscension, double Declination)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
        {
            return PierSide.pierEast;
        }

        public bool CanPark
        {
            get
            {
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                return true;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return false;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                return true;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                return true;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                return false;
            }
        }

        public bool CanUnpark
        {
            get
            {
                return true;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                return Const.rateGuide;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"GuideRateDeclination({value})", "Not implemented", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                return Const.rateGuide;
            }
            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"GuideRateRightAscension({value})", "Not implemented", true);
            }
        }

        public bool AtHome
        {
            get
            {
                return false;
            }
        }

        public bool CanFindHome
        {
            get
            {
                return false;
            }
        }

        public static IAxisRates AxisRates(TelescopeAxes Axis)
        {
            return new AxisRates(Axis);
        }

        public short SlewSettleTime { get; set; } = 1;

        public void MakeRaDecFromAltAz(double Azimuth, double Altitude, string whatfor, ref double ra, ref double dec, bool noSafetyCheck = false)
        {
            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(whatfor, "Cannot slew while AtPark");

            if (Tracking)
                Exceptor.Throw<InvalidOperationException>(whatfor, "Cannot slew while Tracking");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>(whatfor, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(whatfor, "Cannot slew while the shutter is moving");

            Astrometry.Transform.Transform transform = new Astrometry.Transform.Transform()
            {
                SiteElevation = SiteElevation,
                SiteLatitude = SiteLatitude,
                SiteLongitude = SiteLongitude,
                SiteTemperature = WiseSite.och.Temperature,
            };

            try
            {
                transform.SetAzimuthElevation(Azimuth, Altitude);
                ra = transform.RAApparent;
                dec = transform.DECApparent;
            }
            catch (Exception ex)
            {
                Exceptor.Throw<InvalidOperationException>(whatfor, $"Cannot transform to apparent coords: {ex.Message}");
            }

            if (!noSafetyCheck)
            {
                string notSafe = SafeAtCoordinates(Angle.RaFromHours(ra), Angle.DecFromDegrees(dec));

                if (!string.IsNullOrEmpty(notSafe))
                    Exceptor.Throw<InvalidOperationException>(whatfor, notSafe);
            }
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToAltAzAsync(double Azimuth, double Altitude, string whatfor, bool noSafetyCheck = false)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            string op = "SlewToAltAzAsync(" +
                $"az: {Angle.AzFromDegrees(Azimuth).ToNiceString()}, " +
                $"alt: {Angle.AltFromDegrees(Altitude).ToNiceString()}, " +
                $"for: {whatfor})";

            double ra = Double.NaN, dec = Double.NaN;

            MakeRaDecFromAltAz(Azimuth, Altitude, op, ref ra, ref dec, noSafetyCheck);

            //
            // RECORD THE TARGET, same gap as SlewToHaDecAsync: this path set nothing either.
            //
            // Altitude and azimuth are the invariants, so they are stored and the equatorial
            //  fields are left null for Digest to re-derive on every tick.  That is what keeps an
            //  alt/az target correct as the sky turns: it is fixed to the dome, and its right
            //  ascension is what moves.
            //
            // This is also why the type cannot be recovered further down the call: the slew is
            //  handed an HOUR ANGLE below, so by the time the slewer sees it this request is
            //  indistinguishable from a genuine HaDec one.
            //
            _targetAltitude = Angle.AltFromDegrees(Altitude);
            _targetAzimuth = Angle.AzFromDegrees(Azimuth);
            _targetRightAscension = null;
            _targetDeclination = null;
            _targetHourAngle = null;
            _targetType = TargetCoordinateType.AltAz;

            DoSlewToCoordinatesAsync(
                Angle.HaFromHours((wisesite.LocalSiderealTime - Angle.RaFromHours(ra)).Hours),
                Angle.DecFromDegrees(dec),
                op);
        }

        public void SlewToAltAz(double Azimuth, double Altitude, bool noSafetyCheck = false)
        {
            string op = $"SlewToAltAz(az: {Angle.FromDegrees(Azimuth).ToNiceString()}, " +
                $"alt: {Angle.AltFromDegrees(Altitude).ToNiceString()})";

            double ra = Double.NaN, dec = Double.NaN;

            MakeRaDecFromAltAz(Azimuth, Altitude, op, ref ra, ref dec, noSafetyCheck);
            InternalSlewToCoordinatesSync(
                Angle.HaFromHours((wisesite.LocalSiderealTime - Angle.RaFromHours(ra)).Hours),
                Angle.DecFromDegrees(dec),
                op);
        }

        public double RightAscensionRate
        {
            get
            {
                return 0.0;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"Set RightAscensionRate({value})", "Not implemented");
            }
        }

        public static void SetPark()
        {
            Exceptor.Throw<MethodNotImplementedException>("SetPark", "SetPark not implemented");
        }

        public PierSide SideOfPier
        {
            get
            {
                return PierSide.pierEast;
            }

            set
            {
                if (value != PierSide.pierEast)
                    Exceptor.Throw<InvalidValueException>($"SideOfPier({value})", "Only pierEast is valid!", true);
            }
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            string op = $"PulseGuide({Direction}, {Duration})";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "PulseGuide: Direction={0}, Duration={1}", Direction.ToString(), Duration.ToString());
            #endregion
            if (AtPark)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot PulseGuide while AtPark");

            if (Slewing)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot PulseGuide while Slewing");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>(op, $"Not safe to operate ({wisesafetooperate.UnsafeReasons})");

            (pulsing ?? (pulsing = Pulsing.Instance)).Init();

            TelescopeAxes axis = Pulsing.guideDirection2Axis[Direction];
            if (Pulsing.Active(axis))
                Exceptor.Throw<InvalidOperationException>(op, $"Already PulseGuiding on {axis}");

            try
            {
                pulsing.Start(Direction, Duration);
                if (axis == TelescopeAxes.axisPrimary)
                {
                    activityMonitor.NewActivity(new Activity.PulsingRa(new Activity.PulsingRa.StartParams()
                    {
                        _start = new Activity.TelescopeSlew.Coords
                        {
                            ra = RightAscension,
                            dec = Declination,
                        },
                        _direction = Direction,
                        _millis = Duration,
                    }));
                }
                else
                {
                    activityMonitor.NewActivity(new Activity.PulsingDec(new Activity.PulsingDec.StartParams()
                    {
                        _start = new Activity.TelescopeSlew.Coords
                        {
                            ra = RightAscension,
                            dec = Declination,
                        },
                        _direction = Direction,
                        _millis = Duration,
                    }));
                }
            }
            catch (Exception ex)
            {
                Exceptor.Throw<InvalidOperationException>(op, $"Caught {ex.Message} at {ex.StackTrace}");
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                return new ArrayList() {
                    "active",
                    "activities",
                    "encoders",
                    "seconds-till-idle",
                    "opmode",
                    "status",
                    "nearly-parked",
                    "slew-to-ha-dec",
                    "calibration-point"
                };
            }
        }

        public string Action(string action, string parameter)
        {
            action = action.ToLower();

            switch (action)
            {
                case "debug":
                    if (!String.IsNullOrEmpty(parameter))
                    {
                        Debugger.DebugLevel newDebugLevel;
                        try
                        {
                            Enum.TryParse<Debugger.DebugLevel>(parameter, out newDebugLevel);
                            Debugger.SetCurrentLevel(newDebugLevel);
                        }
                        catch
                        {
                            return $"Cannot parse DebugLevel \"{parameter}\"";
                        }
                    }
                    return $"{Debugger.Level}";

                case "encoders":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        if (parameter == "old")
                            EncodersInUse = EncodersInUseEnum.Old;
                        else if (parameter == "new")
                            EncodersInUse = EncodersInUseEnum.New;
                        else
                            return $"Bad parameter.  Must be either \"old\" or \"new\".";

                    }
                    return EncodersInUse.ToString().ToLower();

                //
                // Records one Renishaw calibration point from a plate solve.
                //  Parameter: "<solved RA in hours>,<solved Dec in degrees>".
                //
                // Deliberately NOT tied to a sync.  ACP's pointing runs - Test
                //  Pointing.vbs and the model builder - walk an all-sky mesh
                //  solving at every stop and never sync at all, and that mesh is
                //  exactly the spread of angles this calibration needs.  A script
                //  can hand us a point with one line right after its solve:
                //
                //      Telescope.Action "calibration-point", RATrue & "," & DecTrue
                //
                // Nothing here depends on ACP's pointing model, on whether
                //  corrections were applied, or on where it thought it was
                //  pointing.  We record where it REALLY pointed, from the solve,
                //  beside what the encoders read at that instant.  So a model
                //  gathered this way can be thrown away afterwards; only the mesh
                //  mattered.
                //
                case "calibration-point":
                    {
                        if (string.IsNullOrWhiteSpace(parameter))
                            return "error: expected \"<ra hours>,<dec degrees>\"";

                        string[] fields = parameter.Split(',');
                        if (fields.Length != 2 ||
                            !double.TryParse(fields[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double solvedRa) ||
                            !double.TryParse(fields[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double solvedDec))
                            return $"error: cannot parse \"{parameter}\" as \"<ra hours>,<dec degrees>\"";

                        //
                        // Sidereal time and both counts together, before anything
                        //  else - the axis turns 15 arcsec of hour angle a second,
                        //  so a count only means something beside the clock reading
                        //  taken with it.
                        //
                        double lstNow = wisesite.LocalSiderealTime.Hours;
                        int haCountNow = renishawHaEncoder.Position;
                        int decCountNow = renishawDecEncoder.Position;

                        RenishawCalibrationLog.Record(
                            lstHours: lstNow,
                            solvedRaHours: solvedRa,
                            solvedDecDegrees: solvedDec,
                            throughPole: renishawDecEncoder.Over90Deg,
                            haCount: haCountNow,
                            decCount: decCountNow,
                            oldHaHours: Instance.HourAngle,
                            oldDecDegrees: Instance.Declination,
                            renishawHaHours: renishawHaEncoder.HourAngle,
                            renishawDecDegrees: renishawDecEncoder.Declination);

                        return $"ok: {RenishawCalibrationLog.Path}";
                    }

                case "active":
                    if (!string.IsNullOrEmpty(parameter))
                        ActivityMonitor.StayActive($"action active={Convert.ToBoolean(parameter)}");

                    return ActivityMonitor.ObservatoryIsActive().ToString();

                case "activities":
                    return JsonConvert.SerializeObject(ActivityMonitor.ObservatoryActivities);

                case "shutdown":
                    if (parameter == Const.Proto.Request.Wise40IsIdle && ActivityMonitor.ObservatoryIsActive())
                    {
                        return $"{Const.Proto.Reply.Wise40IsActive}{string.Join(", ", ActivityMonitor.ObservatoryActivities)}";
                    }

                    telescopeCTS?.Dispose();
                    telescopeCTS = new CancellationTokenSource();
                    telescopeCT = telescopeCTS.Token;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"Action(\"{action}\"): New telescopeCTS: {telescopeCTS.GetHashCode()}, telescopeCT: {telescopeCT.GetHashCode()}");
                    #endregion
                    try
                    {
                        Task.Run(() => Shutdown(parameter), telescopeCT);
                    }
                    catch (Exception ex)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                            $"Action(\"{action}\"): Caught {ex.Message} at\n{ex.StackTrace}");
                    }
                    return "ok";

                case "hunkerdown":
                    if (parameter == Const.Proto.Request.Wise40IsIdle && ActivityMonitor.ObservatoryIsActive())
                    {
                        return $"{Const.Proto.Reply.Wise40IsActive}{string.Join(", ", ActivityMonitor.ObservatoryActivities)}";
                    }

                    telescopeCTS?.Dispose();
                    telescopeCTS = new CancellationTokenSource();
                    telescopeCT = telescopeCTS.Token;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"Action(\"{action}\"): New telescopeCTS: {telescopeCTS.GetHashCode()}, telescopeCT: {telescopeCT.GetHashCode()}");
                    #endregion

                    try
                    {
                        Task.Run(() => Hunkerdown(parameter), telescopeCT);
                    }
                    catch (Exception ex)
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                            $"Action(\"{action}\"): Caught {ex.Message} at\n{ex.StackTrace}");
                    }
                    return "ok";

                case "abort-shutdown":
                    AbortSlew($"Action(\"{action}\")");
                    activityMonitor.EndActivity(ActivityMonitor.ActivityType.ShuttingDown, new Activity.GenericEndParams
                    {
                        endReason = $"Action(\"{action}\")",
                        endState = Activity.State.Aborted,
                    });
                    return "ok";

                case "opmode":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        Enum.TryParse(parameter.ToUpper(), out WiseSite.OpMode mode);
                        WiseSite.OperationalMode = mode;
                    }
                    return WiseSite.OperationalMode.ToString();

                case "seconds-till-idle":
                    TimeSpan ts = ActivityMonitor.idler.RemainingTime;

                    if (ts != TimeSpan.MaxValue)
                        return (ts.TotalSeconds).ToString();
                    return "-1";

                case "status":
                    return Digest;

                case "nearly-parked":
                    return NearlyParked.ToString();

                case "enslave-dome":
                    if (!string.IsNullOrEmpty(parameter))
                        EnslavesDome = Convert.ToBoolean(parameter);
                    return EnslavesDome.ToString();

                case "full-stop":
                    FullStop();
                    return "ok";

                case "handpad-move-axis":
                    HandpadMoveAxisParameter param = JsonConvert.DeserializeObject<HandpadMoveAxisParameter>(parameter);
                    HandpadMoveAxis(param.axis, param.rate);
                    return "ok";

                case "handpad-stop":
                    HandpadStop();
                    return "ok";

                case "backoff":
                    Backoff($"Action(\"{action}\")");
                    return "ok";

                case "safe-to-move":
                    return JsonConvert.SerializeObject(SafeToMove(parameter.ToLower()));

                case "park":
                    Task.Run(() => Park());
                    return "ok";

                case "move-to-preset":
                    switch(parameter.ToLower())
                    {
                        case "zenith":
                            return MoveToKnownHaDec(new Angle("0h0m0s"), Angle.DecFromDegrees(WiseSite.Latitude));

                        case "ha0":
                            return "ok";

                        case "cover":
                            return MoveToKnownHaDec(new Angle("11h55m00.0s"), new Angle("88:00:00.0"));

                        default:
                            return $"{action}: Bad parameter \"{parameter.ToLower()}\"";
                    }

                case "hardware-meta-digest":
                    Hardware.Hardware.Instance.Init();
                    WiseTele.Instance.Init();
                    WiseDome.Instance.Init();
                    WiseFocuser.Instance.Init();

                    return JsonConvert.SerializeObject(HardwareMetaDigest.FromHardware());

                case "hardware-digest":
                    return JsonConvert.SerializeObject(HardwareDigest.FromHardware());

                case "test-envelope":
                    {
                        //
                        // A temporary, TIGHTER safety envelope for on-sky testing.  See the
                        //  testAltLimitDeg / testHaLimitHours fields above for why this can only
                        //  ever restrict, and why it is deliberately not persisted.
                        //
                        //   test-envelope                          report the current state
                        //   test-envelope   off                    back to the compiled limits
                        //   test-envelope   AltLimit=30,HaLimit=3  restrict (either key optional)
                        //
                        // Braced because C# switch sections share one scope and slew-to-ha-dec
                        //  below already declares par, ha, dec, kv, key and val.
                        //
                        if (string.IsNullOrWhiteSpace(parameter))
                            return TestEnvelopeDescription;

                        if (parameter.Trim().Equals("off", StringComparison.OrdinalIgnoreCase))
                        {
                            testAltLimitDeg = null;
                            testHaLimitHours = null;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugTele, "test-envelope: cleared");
                            #endregion
                            return TestEnvelopeDescription;
                        }

                        double? newAlt = null, newHa = null;

                        foreach (string item in parameter.Split(','))
                        {
                            string[] kvPair = item.Split('=');
                            if (kvPair.Length != 2)
                                return $"Malformed parameter \"{item.Trim()}\", expected Key=Value";

                            string envKey = kvPair[0].Trim();
                            string envVal = kvPair[1].Trim();

                            if (!Double.TryParse(envVal, out double envNum))
                                return $"\"{envVal}\" is not a number";

                            if (envKey.Equals("AltLimit", StringComparison.OrdinalIgnoreCase))
                                newAlt = envNum;
                            else if (envKey.Equals("HaLimit", StringComparison.OrdinalIgnoreCase))
                                newHa = envNum;
                            else
                                return $"Unknown key \"{envKey}\", expected AltLimit or HaLimit";
                        }

                        if (!newAlt.HasValue && !newHa.HasValue)
                            return "Nothing to set: supply AltLimit, HaLimit, or \"off\"";

                        //
                        // REFUSE anything looser than the compiled limit.  This is the guard that
                        //  makes the Action incapable of widening the envelope: a slip of the
                        //  keyboard can only ever make the test area smaller.
                        //
                        if (newAlt.HasValue && newAlt.Value < altLimit.Degrees)
                            return $"Refused: AltLimit={newAlt.Value} is BELOW the real limit of {altLimit.Degrees}; the test envelope may only restrict";

                        if (newHa.HasValue && (newHa.Value <= 0.0 || newHa.Value > western_haLimit.Hours))
                            return $"Refused: HaLimit={newHa.Value} must be above 0 and at most the real limit of {western_haLimit.Hours}; the test envelope may only restrict";

                        //
                        // Refuse to arm an envelope that excludes where the mount is standing.
                        //
                        // The safety monitor is armed when motion STARTS, not continuously, so an
                        //  envelope that already excludes the current position does nothing until
                        //  the next command - and then answers that command with a backoff
                        //  instead of the test that was intended.  Better to refuse now than to
                        //  have the first slew of the night behave inexplicably.
                        //
                        double curAlt = Altitude;
                        double curHa = safeAstroUtils.ConditionHA(HourAngle);

                        if (newAlt.HasValue && curAlt < newAlt.Value)
                            return $"Refused: the mount is at altitude {curAlt:F2}, below the requested AltLimit={newAlt.Value}; move it inside the envelope first";

                        if (newHa.HasValue && Math.Abs(curHa) > newHa.Value)
                            return $"Refused: the mount is at hour angle {curHa:F3}h, outside the requested HaLimit=+/-{newHa.Value}; move it inside the envelope first";

                        if (newAlt.HasValue)
                            testAltLimitDeg = newAlt;
                        if (newHa.HasValue)
                            testHaLimitHours = newHa;

                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, TestEnvelopeDescription);
                        #endregion
                        return TestEnvelopeDescription;
                    }

                case "slew-to-ha-dec":
                    //
                    // Do NOT lowercase here.  This used to be parameter.ToLower(), and the
                    //  two tests below are case-sensitive, so neither could ever match and
                    //  the Action returned "Parameters HourAngle and Declination must be
                    //  supplied" for every well-formed call.  Dash.cs passes
                    //  "HourAngle={ha},Declination={dec}", so the Dash's HA/Dec slew was
                    //  broken by the same bug.
                    //
                    List<string> par = parameter.Split(',').ToList();
                    if (par.Count != 2)
                        return "Two parameters needed";

                    double ha = Double.NaN, dec = Double.NaN;

                    //
                    // Split each item on '=' rather than trusting a hand-counted prefix
                    //  length.  The previous form tested StartsWith("Declination") and then
                    //  cut "Declination".Length characters - eleven, not twelve - so it handed
                    //  "=66" to Convert.ToDouble and threw FormatException.  HourAngle was
                    //  written correctly with its '=' in both places; only Declination was
                    //  wrong, and the case-sensitivity bug fixed earlier had kept the branch
                    //  unreachable, so the asymmetry never showed.
                    //
                    // Also: keys compared case-insensitively, values trimmed, and TryParse
                    //  instead of Convert, so a malformed number produces this Action's own
                    //  error message rather than a FormatException surfacing at the ASCOM
                    //  layer as "Input string was not in a correct format".
                    //
                    foreach (string p in par)
                    {
                        string[] kv = p.Split('=');
                        if (kv.Length != 2)
                            continue;

                        string key = kv[0].Trim();
                        string val = kv[1].Trim();

                        if (key.Equals("HourAngle", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Double.TryParse(val, out ha))
                                ha = Double.NaN;
                        }
                        else if (key.Equals("Declination", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Double.TryParse(val, out dec))
                                dec = Double.NaN;
                        }
                    }

                    if (Double.IsNaN(ha) || Double.IsNaN(dec))
                        return "Parameters HourAngle and Declination must be supplied";

                    SlewToHaDecAsync(ha, dec,
                        $"Action(\"{action}\"): " +
                        $"ha: {Angle.HaFromHours(ha).ToNiceString()}, " +
                        $"dec: {Angle.DecFromDegrees(dec).ToNiceString()}");
                    return "ok";

                default:
                    Exceptor.Throw<ActionNotImplementedException>($"Action({action})", "Not implemented by this driver");
                    return "false";
            }
        }

        private string MoveToKnownHaDec(Angle ha, Angle dec)
        {
            string op = $"MoveToKnownHaDec(ha: {ha.ToNiceString()}, dec: {dec.ToNiceString()})";

            Angle ra = wisesite.LocalSiderealTime - ha;
            bool savedEnslaveDome = EnslavesDome;

            EnslavesDome = false;
            Tracking = true;
            try
            {
                SlewToCoordinatesAsync(ra.Hours, dec.Degrees, op, false);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            EnslavesDome = savedEnslaveDome;
            Tracking = false;

            return "ok";
        }

        private bool NearlyParked
        {
            get
            {
                ShortestDistanceResult delta = Angle.RaFromHours(RightAscension).ShortestDistance(wisesite.LocalSiderealTime);
                if (delta.angle > new Angle("00h10m00s"))
                    return false;

                delta = Angle.DecFromDegrees(Declination).ShortestDistance(parkingDeclination);
                if (delta.angle > new Angle("00d10m00s"))
                    return false;

                if (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
                    return false;

                return true;
            }
        }

        private void CheckConnected(string message)
        {
            if (!_connected)
                Exceptor.Throw<NotConnectedException>("CheckConnected", message);
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            Exceptor.Throw<MethodNotImplementedException>($"CommandBlind({command}, {raw})", "Not implemented");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            if (command == "active")
            {
                return Convert.ToBoolean(Action("active", string.Empty));
            }
            else
            {
                Exceptor.Throw<MethodNotImplementedException>($"CommandBool({command}, {raw})", "Not implemented");
                return false;
            }
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");

            if (command == "opmode")
            {
                return Action("opmode", string.Empty);
            }
            else
            {
                Exceptor.Throw<MethodNotImplementedException>($"CommandString({command}, {raw})", "Not implemented");
                return string.Empty;
            }
        }

        public string DriverInfo
        {
            get
            {
                return $"ASCOM Wise40.Telescope v{version}";
            }
        }

        public string DriverVersion
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return Convert.ToInt16("3");
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal static void ReadProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                if (Enum.TryParse<Accuracy>(driverProfile.GetValue(driverID, Const.ProfileName.Telescope_AstrometricAccuracy, string.Empty, "Full"), out Accuracy acc))
                    WiseSite.astrometricAccuracy = acc;
                else
                    WiseSite.astrometricAccuracy = Accuracy.Full;

                //
                // Default New since 2026-09-17.  The Renishaws were calibrated against
                //  16 plate-solved points on 2026-09-16 and came out better than the old
                //  encoders on both axes: no scale error (HA residual slopes 3.76"/h at
                //  R2 = 0.10), a Dec zero point of +3.06" against the old encoders'
                //  +221.5", and agreement with those independent encoders to 9.5" sd.
                //
                // The default matters more than it looks.  An elevated rebuild
                //  re-registers the driver for COM, which wipes this profile subkey, so
                //  every rebuild silently reverts whatever is persisted here to the
                //  default - which is how a deliberate switch to New was lost once
                //  already.  Keep the default equal to what the telescope should
                //  actually run on.
                //
                Enum.TryParse<EncodersInUseEnum>(driverProfile.GetValue(driverID, Const.ProfileName.Telescope_EncodersInUse, string.Empty, "New"), out EncodersInUseEnum enc);
                Instance.EncodersInUse = enc;
                BypassCoordinatesSafety = Convert.ToBoolean(driverProfile.GetValue(driverID, Const.ProfileName.Telescope_BypassCoordinatesSafety, string.Empty, false.ToString()));
            }

            using (Profile driverProfile = new Profile() { DeviceType = "SafetyMonitor" })
            {
                if (Enum.TryParse(driverProfile.GetValue(Const.WiseDriverID.ObservatoryMonitor,
                    "OnIdle", string.Empty, OnIdle.HunkerDown.ToString()), out OnIdle onIdle))
                    Instance._onIdle = onIdle;
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        public static void WriteProfile()
        {
            using (Profile driverProfile = new Profile() { DeviceType = "Telescope" })
            {
                driverProfile.WriteValue(driverID, Const.ProfileName.Telescope_AstrometricAccuracy, WiseSite.astrometricAccuracy.ToString());
                driverProfile.WriteValue(driverID, Const.ProfileName.Telescope_BypassCoordinatesSafety, BypassCoordinatesSafety.ToString());
                driverProfile.WriteValue(driverID, Const.ProfileName.Telescope_EncodersInUse, Instance.EncodersInUse.ToString());
            }
        }

        public string Status
        {
            get
            {
                string ret = string.Empty;

                (pulsing ?? (pulsing = Pulsing.Instance)).Init();

                if (Slewers.Active(Slewers.Type.Dec) || Slewers.Active(Slewers.Type.Ra))
                {
                    string to = null;

                    Angle ra, dec;
                    try
                    {
                        ra = Angle.RaFromHours(TargetRightAscension);
                        to += " RA " + ra.ToString();
                    }
                    catch { }

                    try
                    {
                        dec = Angle.DecFromDegrees(TargetDeclination);
                        to += " DEC " + dec.ToString();
                    }
                    catch { }

                    if (to != null)
                        to = "to" + to;
                    return Parking ? "Parking " : "Slewing " + to;
                }
                else if (IsPulseGuiding)
                {
                    return "PulseGuiding in " + pulsing.ToString();
                }
                return ret;
            }
        }

        public EncodersInUseEnum EncodersInUse
        {
            get
            {
                return encodersInUse;
            }

            set
            {
                encodersInUse = value;
                WriteProfile();
            }
        }

        public string Digest
        {
            get
            {
                TimeSpan ts = activityMonitor.RemainingTime;
                double secondsTillIdle = (ts == TimeSpan.MaxValue) ? -1 : ts.TotalSeconds;
                double targetRa, targetDec;
                double targetHa, targetAlt, targetAz, temp;

                targetRa = (_targetRightAscension == null) ? Const.noTarget : _targetRightAscension.Hours;
                targetHa = (_targetHourAngle == null) ? Const.noTarget : _targetHourAngle.Hours;
                targetDec = (_targetDeclination == null) ? Const.noTarget : _targetDeclination.Degrees;
                targetAlt = (_targetAltitude == null) ? Const.noTarget : _targetAltitude.Degrees;
                targetAz = (_targetAzimuth == null) ? Const.noTarget : _targetAzimuth.Degrees;

                try
                {
                    temp = WiseSite.och.Temperature;
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"WiseTele.Digest: Cannot get WiseSite.och.temperature: caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    temp = 21.0;
                }

                Astrometry.Transform.Transform t = new Astrometry.Transform.Transform()
                {
                    SiteElevation = WiseSite.Elevation,
                    SiteLatitude = WiseSite.Latitude,
                    SiteLongitude = WiseSite.Longitude,
                    SiteTemperature = temp,
                };

                if (_targetRightAscension != null && _targetDeclination != null &&
                    (_targetAzimuth == null || _targetAltitude == null))
                {
                    try
                    {
                        t.SetApparent(_targetRightAscension.Hours, _targetDeclination.Degrees);
                        targetAlt = t.ElevationTopocentric;
                        targetAz = t.AzimuthTopocentric;
                        _targetAltitude = Angle.AltFromDegrees(targetAlt);
                        _targetAzimuth = Angle.AzFromDegrees(targetAz);
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                            $"WiseTele.Digest: Could not transform apparent to topocentric, caught {ex.Message}");
                        #endregion
                        throw;
                    }
                }

                double lst = Double.NaN;
                try
                {
                    lst = wisesite.LocalSiderealTime.Hours;
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                        $"WiseTele.Digest: Failed to get LST: caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                }

                if (_targetAltitude != null && _targetAzimuth != null &&
                    (_targetRightAscension == null || _targetDeclination == null))
                {
                    try
                    {
                        t.SetAzimuthElevation(_targetAzimuth.Degrees, _targetAltitude.Degrees);
                        targetRa = t.RAApparent;
                        targetDec = t.DECApparent;
                        targetHa = lst - targetRa;
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                            $"WiseTele.Digest: Could not transform topocentric to apparent, caught {ex.Message}");
                        #endregion
                    }
                }
                t.Dispose();

                try
                {
                    TelescopeTips tips = new TelescopeTips()
                    {
                        Tracking = $"Telescope is {(Tracking ? "tracking" : "not tracking")}",
                        Slewing = ReasonsForSlewing,
                        PulseGuiding = Pulsing.Instance.ReasonsForPulseGuiding,
                    };

                    TelescopeDigest digest = new TelescopeDigest()
                    {
                        Current = new TelescopePosition
                        {
                            RightAscension = RightAscension,
                            Declination = Declination,
                            HourAngle = HourAngle,
                            Altitude = Altitude,
                            Azimuth = Azimuth,
                        },

                        Target = new TelescopeTarget
                        {
                            RaDec_RA = targetRa,
                            RaDec_Dec = targetDec,
                            HaDec_HA = targetHa,
                            HaDec_Dec = targetDec,
                            Alt = targetAlt,
                            Az = targetAz,
                            Type = _targetType,
                        },

                        LocalSiderealTime = lst,
                        Slewing = Slewing,
                        Tracking = Tracking,
                        PulseGuiding = IsPulseGuiding,
                        AtPark = AtPark,
                        SecondsTillIdle = secondsTillIdle,
                        EnslavesDome = EnslavesDome,
                        Active = ActivityMonitor.ObservatoryIsActive(),
                        Activities = ActivityMonitor.ObservatoryActivities,
                        SlewPin = SlewPin.isOn,
                        PrimaryPins = new AxisPins
                        {
                            SetPin = WestPin.isOn || EastPin.isOn,
                            GuidePin = WestGuidePin.isOn || EastGuidePin.isOn,
                        },
                        SecondaryPins = new AxisPins
                        {
                            SetPin = NorthPin.isOn || SouthPin.isOn,
                            GuidePin = NorthGuidePin.isOn || SouthGuidePin.isOn,
                        },
                        SafeAtCurrentCoordinates = SafeAtCoordinates(
                            Angle.RaFromHours(RightAscension),
                            Angle.DecFromDegrees(Declination)),
                        BypassCoordinatesSafety = BypassCoordinatesSafety,
                        Status = Status,
                        PrimaryIsMoving = AxisIsMoving(TelescopeAxes.axisPrimary),
                        SecondaryIsMoving = AxisIsMoving(TelescopeAxes.axisSecondary),
                        ShuttingDown = activityMonitor.ShuttingDown,
                        HunkeringDown = activityMonitor.HunkeringDown,
                        HunkeredDown = _hunkeredDown,
                        Tips = tips,
                        EncodersInUse = EncodersInUse,
                        Renishaw = new RenishawDigest
                        {
                            EncHA = renishawHaEncoder.Position,
                            EncDEC = renishawDecEncoder.Position,
                            HA = renishawHaEncoder.HourAngle,
                            Dec = renishawDecEncoder.Declination,
                            oldHA = HAEncoder.OldHourAngle,
                            oldDec = DecEncoder.OldDeclination,
                            deltaHA = Math.Abs(renishawHaEncoder.HourAngle - HAEncoder.OldHourAngle),
                            deltaDec = Math.Abs(renishawDecEncoder.Declination - DecEncoder.OldDeclination),
                            radHA = renishawHaEncoder.Radians,
                            radDec = renishawDecEncoder.Radians,
                        }
                    };

                    string response = JsonConvert.SerializeObject(digest);
                    if (string.IsNullOrEmpty(response))
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugTele, "WiseTelecope:Digest: Empty response");
                        #endregion
                    }
                    return JsonConvert.SerializeObject(digest);
                }
                catch (Exception ex)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"WiseTelecope:Digest: Caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    return JsonConvert.SerializeObject(null);
                }
            }
        }

        public static bool BypassCoordinatesSafety { get; set; } = false;

        public bool Parking { get; set; } = false;

        //public bool DecOver90Degrees
        //{
        //    get
        //    {
        //        return DecEncoder.DecOver90Degrees;
        //    }
        //}
    }

    public class TelescopePosition
    {
        public double RightAscension, Declination;
        public double HourAngle;
        public double Azimuth, Altitude;
    }

    /// <summary>
    /// WHICH COORDINATES THE CLIENT ACTUALLY ASKED FOR.
    /// </summary>
    //
    // Recorded at the entry points, deliberately not inferred from the angle types later on.
    //
    // Two reasons it cannot be inferred.  First, every target ends up with all six values
    //  populated: WiseTele.Digest derives alt/az from ra/dec and ra/dec from alt/az, so by the
    //  time anything reads the digest, nothing distinguishes a requested value from a derived
    //  one.  Second, SlewToAltAzAsync converts to an HOUR ANGLE before calling
    //  DoSlewToCoordinatesAsync, so at the slewer an alt/az request is indistinguishable from an
    //  hour-angle one - inferring from primaryTargetAngle.Type would silently label every alt/az
    //  slew as HaDec.
    //
    public enum TargetCoordinateType
    {
        None,       // nothing requested since startup
        RaDec,
        HaDec,
        AltAz,
    }

    /// <summary>
    /// WHICH limits a position breaches, so that recovery can undo the breach that happened
    /// rather than every breach that might have.
    /// </summary>
    //
    // SafeAtCoordinates returned only prose, so Backoff could not know what to fix.  It guessed
    //  by altitude: it asked SafeToMove which way was HIGHER and moved both axes that way.  On
    //  2026-09-20 an hour-angle breach of 14 arcseconds was answered with 3.8 degrees east AND
    //  7.05 degrees south - the declination move corrected a limit that had not been breached.
    //
    // Flags rather than a single value because a position can breach more than one at once, and
    //  each wants its own axis moved its own way.
    //
    [Flags]
    public enum SafetyViolation
    {
        None = 0,
        AltitudeTooLow = 1 << 0,
        DeclinationTooHigh = 1 << 1,
        DeclinationTooLow = 1 << 2,
        HourAngleTooLow = 1 << 3,    // east of the eastern limit: recover by going WEST
        HourAngleTooHigh = 1 << 4,   // west of the western limit: recover by going EAST
    }

    public class TelescopeTarget
    {
        public double RaDec_RA, RaDec_Dec;
        public double HaDec_HA, HaDec_Dec;
        public double Az, Alt;

        // What the client asked for; the rest of the fields above are derived from it.
        public TargetCoordinateType Type;
    }

    public class AxisPins
    {
        public bool SetPin, GuidePin;
    }

    public class HandpadMoveAxisParameter
    {
        public TelescopeAxes axis;
        public double rate;
    }

    public class TelescopeTips
    {
        public string Tracking;
        public string Slewing;
        public string PulseGuiding;
    }

    public class RenishawDigest
    {
        public int EncHA, EncDEC;
        public double HA, Dec;

        //
        // The OLD encoders' own readings, and the Renishaw-minus-old differences.
        //
        // deltaHA/deltaDec used to be the Renishaw compared against "the hour angle",
        //  which was the old encoder only while the old encoders were in use.  Once
        //  EncodersInUse became New that compared the Renishaw with itself and read
        //  zero by construction.  Both are now explicitly Renishaw minus old, so the
        //  cross-check between two independent sensors holds in either mode.
        //
        // deltaHA is in HOURS, deltaDec in DEGREES.
        //
        public double oldHA, oldDec;
        public double deltaHA, deltaDec;

        public double radHA, radDec;
    }

    public class TelescopeDigest
    {
        public TelescopePosition Current;
        public TelescopeTarget Target;
        public double LocalSiderealTime;
        public bool Slewing;
        public bool Tracking;
        public bool PulseGuiding;
        public bool AtPark;
        public bool Active;
        public bool EnslavesDome;
        public double SecondsTillIdle;
        public List<string> Activities;
        public bool SlewPin;
        public AxisPins PrimaryPins, SecondaryPins;
        public bool PrimaryIsMoving, SecondaryIsMoving;
        public string SafeAtCurrentCoordinates;
        public bool BypassCoordinatesSafety;
        public string Status;
        public bool ShuttingDown;
        public bool HunkeringDown;
        public bool HunkeredDown;
        public TelescopeTips Tips;
        public WiseTele.EncodersInUseEnum EncodersInUse;
        public RenishawDigest Renishaw;
    }
}
