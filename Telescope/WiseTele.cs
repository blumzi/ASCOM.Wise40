using System;
using System.Collections;
using System.Collections.Generic;
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
            public double minRadChangePerPollingInterval;
            public double maxRadChangePerPollingInterval;
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
                case Angle.AngleType.HA:
                    if (value < 0.0 || value > 24.0)
                    {
                        Exceptor.Throw<InvalidValueException>("CheckCoordinateSanity",
                            $"Invalid primary coordinate (value: {value}, reason: {reason}, angle: {Angle.FromHours(value, type).ToNiceString()}). Must be between 0 to 24");
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
                        minRadChangePerPollingInterval = 0.052,
                        maxRadChangePerPollingInterval = 1.5724276374,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:05.0")),
                        stopMovement = new Angle("00h00m02.0s"),
                        minRadChangePerPollingInterval = 0.000146,
                        maxRadChangePerPollingInterval = 0.0436017917,
                        maxTime = TimeSpan.FromMinutes(6),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                        stopMovement = new Angle("00h00m00.1s"),
                        minRadChangePerPollingInterval = 0.0000072,
                        maxRadChangePerPollingInterval = 0.0014668186,
                        maxTime = TimeSpan.FromMinutes(5),
                    }
                },

                [TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>
                {
                    [Const.rateSlew] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:30:00.0"),
                        stopMovement = new Angle("04:30:00.0"),
                        minRadChangePerPollingInterval = 0.0785,
                        maxRadChangePerPollingInterval = 1.6946717173,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateSet] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:10.0"),
                        stopMovement = new Angle("00:00:03.0"),
                        minRadChangePerPollingInterval = 0.000014,
                        maxRadChangePerPollingInterval = 0.0469464707,
                        maxTime = TimeSpan.FromMinutes(5),
                    },

                    [Const.rateGuide] = new MovementParameters()
                    {
                        minimalMovement = new Angle("00:00:01.0"),
                        stopMovement = new Angle("00:00:00.1"),
                        minRadChangePerPollingInterval = 0.00000049,
                        maxRadChangePerPollingInterval = 0.0001234182,
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
                telescopeCTS.Cancel();
                telescopeCTS.Dispose();
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
            safer = SaferAtCoordinates(direction, Angle.RaFromHours(ra), Angle.DecFromDegrees(dec));
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"SafeToMove({direction}: {safer}");
            #endregion
            return safer;
        }

        public bool SaferAtCoordinates(string dir, Angle ra, Angle dec)
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

            dist0 = Math.Abs(Math.Cos(Angle.Deg2Rad(90.0 - zd)));
            dist1 = Math.Abs(Math.Cos(Angle.Deg2Rad(Altitude)));
            ret = dist0 < dist1;
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                $"SaferAtCoordinates({dir}, {ra.ToNiceString()}, {dec.ToNiceString()}): new: {dist0:f12} < curr: {dist1:f12} => {ret}");
            #endregion
            return ret;
        }

        public readonly Angle altLimit = new Angle(16.0, Angle.AngleType.Alt);
        public readonly Angle eastern_haLimit = Angle.HaFromHours(-7.0);
        public readonly Angle western_haLimit = Angle.HaFromHours(7.0);
        public readonly Angle lower_decLimit = Angle.DecFromDegrees(-35.0);
        public readonly Angle upper_decLimit = Angle.DecFromDegrees(89.9);

        /// <summary>
        /// Checks if we're safe at a given position:  Used:
        ///  - before slewing to check if the scope will be safe at the target coordinates
        ///  - by the safety timer to check that the scope is safe at the current coordinates.
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        public string SafeAtCoordinates(Angle ra, Angle dec)
        {
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
                reasons.Add($"Altitude too low: {alt} < {altLimit}");

            if (dec > upper_decLimit)
                reasons.Add($"Declination too high: {dec} > {upper_decLimit}");
            if (dec < lower_decLimit)
                reasons.Add($"Declination too low: {dec} < {lower_decLimit}");

            double ha = HourAngle;
            if (ha < eastern_haLimit.Hours)
                reasons.Add($"HourAngle too low: {Angle.HaFromHours(ha)} < {eastern_haLimit}");
            else if (ha > western_haLimit.Hours)
                reasons.Add($"HourAngle too high: {Angle.HaFromHours(ha)} > {western_haLimit}");

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
        public void Backoff(string reason)
        {
            string op = $"Backoff(reason: {reason})";
            const int backoffMillis = 3000;

            List<BackoffAction> backoffs = new List<BackoffAction>();
            if (SafeToMove("east"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "East", Rate = Const.rateSlew });
            else if (SafeToMove("west"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "West", Rate = -Const.rateSlew });

            if (SafeToMove("south"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "South", Rate = -Const.rateSlew });
            else if (SafeToMove("north"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "North", Rate = Const.rateSlew });

            foreach (var b in backoffs)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"{op}: {b.Direction}: calling MoveAxis({b.Axis}, {b.Direction}, {RateName(b.Rate)}) for {backoffMillis} millis ...");
                #endregion
                MoveAxis(b.Axis, b.Rate);
                Thread.Sleep(backoffMillis);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: stopping {b.Axis}");
                #endregion
                MoveAxis(b.Axis, Const.rateStopped);
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

            Angle parkingRa = wisesite.LocalSiderealTime;
            Angle parkingDec = parkingDeclination;
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
                        ra = parkingRa.Hours,
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
                TargetRightAscension = parkingRa.Hours;
                TargetDeclination = parkingDec.Degrees;

                EnslavesDome = false;
                while (! (primaryAxisMonitor.IsReady && secondaryAxisMonitor.IsReady))
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugTele, "Park: waiting for axis monitors to be ready ...");
                    #endregion
                    Thread.Sleep(500);
                }
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

            Angle ra = wisesite.LocalSiderealTime;
            Angle dec = parkingDeclination;

            if (parkDome)
                DomeParker();
            SlewToCoordinatesAsync(ra.Hours, dec.Degrees, "ParkFromGui", false);
        }

        private void InternalSlewToCoordinatesSync(Angle primaryTargetAngle, Angle secondaryTargetAngle, string whatfor)
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

                Task t = Task.Run(() =>
                {
                    DoSlewToCoordinatesAsync(primaryTargetAngle, secondaryTargetAngle, op);
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

            try
            {
                while (closeEnoughRates != nRates)
                {
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

                        // Wait for InternalMoveAxis to start moving thisAxis
                        while (! InternalMoveAxis(thisAxis, rate, distanceToTarget.direction, false))
                        {
                            const int waitForAxisToStartMovingMillis = 500;

                            currentAngle = CurrentPosition(targetAngle.Type);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{op}: {slewerName}: at {currentAngle} waiting {waitForAxisToStartMovingMillis} " +
                                $"millis to start InternalMoveAxis({thisAxis}, {RateName(rate)}, {distanceToTarget.direction}) ...");
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
                        double prevDistance = 0.0;
                        TimeSpan elapsed;

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

                            if (startingDistance.direction != currentDistance.direction)
                            {
                                #region Direction has changed
                                status = ScopeSlewerStatus.ChangedDirection;
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName} at {RateName(rate)}: " +
                                        $"at {currentAngle}, ChangedDirection ==> target: {targetAngle}, " +
                                        $"originalDirection: {startingDistance.direction} != " +
                                        $"currentDistance.direction: {currentDistance.direction}");
                                #endregion
                                break;
                                #endregion
                            }
                            else if (prevDistance != 0.0) {
                                #region Distance to Target is NOT decreasing
                                if (currentDistance.angle.Radians > prevDistance)   // the distance to target is increasing
                                {
                                    #region debug
                                    debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                                        $"SUSPECT: {op}: distance to target is INCREASING");
                                    #endregion
                                    //status = ScopeSlewerStatus.Failed;
                                    //break;
                                }
                                #endregion
                            }
                            else if (currentDistance.angle <= mp.stopMovement)
                            {
                                #region Reached target
                                status = ScopeSlewerStatus.CloseEnough;
                                double deltaRad = Math.Abs(currentDistance.angle.Radians - mp.stopMovement.Radians);
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName}:{GetHashCode()} at {RateName(rate)}: at {currentAngle}, " +
                                        $"CloseEnough ==> target: {targetAngle}, " +
                                        $"currentDistance.angle.rad: {currentDistance.angle.Radians} <= mp.stopMovement.rad: {mp.stopMovement.Radians}" +
                                        $"delta.rad: {deltaRad}");
                                #endregion
                                break;
                                #endregion
                            }
                            else
                            {
                                double deltaRad = Math.Abs(currentDistance.angle.Radians - mp.stopMovement.Radians);
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
                                byte count = 0;

                                if ((count %= 5) == 0)
                                {
                                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"{op}: {slewerName} at {RateName(rate)}: at {currentAngle}, " +
                                        $"moving ==> target: {targetAngle}, " +
                                        $"remaining (Angle.rad: {currentDistance.angle.Radians:f10}, direction: {currentDistance.direction}) > " +
                                        $"stopMovement.rad: {mp.stopMovement.Radians:f10}, deltaRad: {deltaRad:f10} sleeping {mp.pollingFreqMillis} millis ...");
                                }
                                count++;
                                #endregion debug
                                telescopeCT.ThrowIfCancellationRequested();
                                Thread.Sleep(mp.pollingFreqMillis);
                                telescopeCT.ThrowIfCancellationRequested();
                                // not there yet, continue looping
                            }
                        }
                        prevDistance = currentDistance.angle.Radians;

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

                        if (status == ScopeSlewerStatus.Timedout)
                            AbortSlew($"{op}: Timedout at rate {RateName(rate)} after {elapsed.ToMinimalString()}");
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
            while (AxisIsMoving(axis))
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
        private void DoSlewToCoordinatesAsync(Angle primaryTargetAngle, Angle secondaryTargetAngle, string reason)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            string op = "DoSlewToCoordinatesAsync(" +
                $"{primaryTargetAngle.ToNiceString()}, " +
                $"{secondaryTargetAngle.ToNiceString()}, " +
                $"reason: {reason})";

            Angle.AngleType primaryAngleType = primaryTargetAngle.Type;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: Before CheckCoordinateSanity.");
            #endregion
            CheckCoordinateSanity(primaryAngleType, primaryTargetAngle.Hours, reason);
            CheckCoordinateSanity(secondaryTargetAngle.Type, secondaryTargetAngle.Degrees, reason);
            // Check coordinates safety ???
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

            CheckCoordinateSanity(Angle.AngleType.HA, ha, op);
            CheckCoordinateSanity(Angle.AngleType.Dec, dec, op);

            double alt, az;

            Astrometry.Transform.Transform transform = new Astrometry.Transform.Transform()
            {
                SiteElevation = WiseSite.Elevation,
                SiteLatitude = WiseSite.Latitude,
                SiteLongitude = WiseSite.Longitude,
                SiteTemperature = WiseSite.och.Temperature,
            };

            try
            {
                transform.SetApparent(wisesite.LocalSiderealTime.Hours - ha, dec);
                az = transform.AzimuthTopocentric;
                alt = transform.ElevationTopocentric;

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugTele, $"{op}: calling SlewToAltAz(" +
                        $"az: {Angle.AltFromDegrees(az).ToNiceString()} " +
                        $"alt: {Angle.AzFromDegrees(alt).ToNiceString()})");
                #endregion
                SlewToAltAzAsync(az, alt, op);
            }
            catch (Exception ex)
            {
                Exceptor.Throw<InvalidOperationException>(op, $"Caught: {ex.Message} at {ex.StackTrace}");
            }
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

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");

            if (!ShuttingDown && !wisesafetooperate.IsSafe)
                Exceptor.Throw<InvalidOperationException>(op, string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            try
            {
                DoSlewToCoordinatesAsync(ra, dec, op);
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

            #region debug
            double lst = wisesite.LocalSiderealTime.Hours;
            double ha = RightAscension - lst;
            double dec = Declination;

            debugger.WriteLine(Debugger.DebugLevel.DebugTele,
                $"SyncToCoordinates(ra: {RightAscension}, dec: {Declination}): lst: {lst} " +
                $"Old coord: (ha: {WiseTele.Instance.HourAngle}, dec: {WiseTele.Instance.Declination}), " +
                "New coord: (" + 
                    $"ha: {ha}, renishaw: {renishawHaEncoder.Position}, radians: {Angle.Hours2Rad(ha)}" + ", " +
                    $"dec: {dec}, renishaw: {renishawDecEncoder.Position}, radians: {Angle.Deg2Rad(dec)})");
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
                    "seconds-till-idle",
                    "opmode",
                    "status",
                    "nearly-parked",
                    "slew-to-ha-dec"
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

                case "slew-to-ha-dec":
                    List<string> par = parameter.ToLower().Split(',').ToList();
                    if (par.Count != 2)
                        return "Two parameters needed";

                    double ha = Double.NaN, dec = Double.NaN;
                    foreach(string p in par)
                    {
                        if (p.StartsWith("HourAngle="))
                        {
                            ha = Convert.ToDouble(p.Substring("HourAngle=".Length));
                        }
                        else if (p.StartsWith("Declination"))
                        {
                            dec = Convert.ToDouble(p.Substring("Declination".Length));
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
                BypassCoordinatesSafety = Convert.ToBoolean(driverProfile.GetValue(driverID, Const.ProfileName.Telescope_BypassCoordinatesSafety, string.Empty, false.ToString()));
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
                        Tips = tips,
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

    public class TelescopeTarget
    {
        public double RaDec_RA, RaDec_Dec;
        public double HaDec_HA, HaDec_Dec;
        public double Az, Alt;
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
        public TelescopeTips Tips;
    }
}
