﻿using System;
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
//using System.Diagnostics;

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

        private static NOVAS31 novas31;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        public static Debugger debugger = Debugger.Instance;

        private bool _connected = false;

        private static readonly ActivityMonitor activityMonitor = ActivityMonitor.Instance;

        private const int waitForOtherAxisMillis = 500;           // half a second between checks setting an axis rate

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
               debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
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
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
                #endregion
            }
        };
        private TrackingRestorer _trackingRestorer;
        #endregion

        public List<WiseVirtualMotor> directionMotors, allMotors;
        public Dictionary<TelescopeAxes, List<WiseVirtualMotor>> axisMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        public WisePin TrackPin;
        private WisePin SlewPin;
        private WisePin NorthGuidePin, SouthGuidePin, EastGuidePin, WestGuidePin;   // Guide motor activation pins
        private WisePin NorthPin, SouthPin, EastPin, WestPin;                       // Set and Slew motors activation pins
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackingMotor;
        private bool _syncingDomePosition = false;

        private static bool _atPark;
        private static bool _movingToSafety = false;

        private Angle _targetRightAscension;
        private Angle _targetDeclination;

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

        public static Dictionary<Const.AxisDirection, string> axisPrimaryNames = new Dictionary<Const.AxisDirection, string>()
            {
                { Const.AxisDirection.Increasing, "East" },
                { Const.AxisDirection.Decreasing, "West" },
            }, axisSecondaryNames = new Dictionary<Const.AxisDirection, string>()
            {
                { Const.AxisDirection.Increasing, "North" },
                { Const.AxisDirection.Decreasing, "South" },
            };

        public static Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>> axisDirectionName = new Dictionary<TelescopeAxes, Dictionary<Const.AxisDirection, string>>()
            {
                { TelescopeAxes.axisPrimary, axisPrimaryNames },
                { TelescopeAxes.axisSecondary, axisSecondaryNames },
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
                            $"Invalid Declination (value: {value}, reason: {reason}), angle: {Angle.FromDegrees(value).ToNiceString()}). Must be between -90 and 90");
                    }
                    break;

                case Angle.AngleType.RA:
                    if (value < 0.0 || value > 24.0)
                    {
                        Exceptor.Throw<InvalidValueException>("CheckCoordinateSanity",
                            $"Invalid RightAscension (value: {value}, reason: {reason}, angle: {Angle.FromHours(value).ToNiceString()}). Must be between 0 to 24");
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
                _targetDeclination = Angle.FromDegrees(value, Angle.AngleType.Dec);
                activityMonitor.StayActive("TargetDeclination was set");
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

                Angle ret = _targetRightAscension;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"TargetRightAscension Get - {ret} ({ret.Hours})");
                #endregion debug
                return _targetRightAscension.Hours;
            }

            set
            {
                CheckCoordinateSanity(Angle.AngleType.RA, value, $"TargetRightAscension Set - {value}");
                _targetRightAscension = Angle.FromHours(value, Angle.AngleType.RA);
                activityMonitor.StayActive("TargetRightAscension was set");
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

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
            _targetRightAscension = null;
            _targetDeclination = null;
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

                activityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.telescope.Line));
                activityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.tracking.Line));
                activityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.parking.Line));
                activityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.shutdown.Line));
                activityMonitor.Event(new Event.DriverConnectEvent(driverID, value, ActivityMonitor.Tracer.idler.Line));
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
            novas31 = new NOVAS31();
            astroutils = new Astrometry.AstroUtils.AstroUtils();

            parkingDeclination = Angle.FromDegrees(66.0, Angle.AngleType.Dec);

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
            }
            catch (WiseException e)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseTele constructor caught: {0}.", e.Message);
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele init() done.");
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
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"AbortSlew: ({reason}) started.");
            #endregion debug

            activityMonitor.StayActive("AbortSlew");
            if (AtPark)
                Exceptor.Throw<InvalidOperationException>("AbortSlew", "Cannot AbortSlew while AtPark");

            Stop($"AbortSlew: ({reason})");

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
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"AbortSlew: ({reason}) done.");
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
                return astroutils.ConditionHA(ret.Hours);
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
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
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
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
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

            if (EnslavesDome && !slewers.Active(Slewers.Type.Dome) && wisesafetooperate.IsSafeWithoutCheckingForShutdown())
            {
                WiseDome._adjustingForTracking = true;
                DomeSlewer(Angle.FromHours(RightAscension), Angle.FromDegrees(Declination), "tracking");
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
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Tracking Set - {value}");
                #endregion

                if (value)
                {
                    if (!wisesafetooperate.IsSafeWithoutCheckingForShutdown() && !ShuttingDown && !BypassCoordinatesSafety)
                            Exceptor.Throw<InvalidOperationException>("Tracking.set", string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

                    if (Simulated)
                        _lastTrackingLST = wisesite.LocalSiderealTime.Hours;

                    if (TrackingMotor.IsOff)
                        TrackingMotor.SetOn(Const.rateTrack);

                    primaryAxisMonitor.ResetRASamples();
                }
                else
                {
                    if (TrackingMotor.IsOn)
                        TrackingMotor.SetOff();
                }
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.Backoff);

                SyncDomePosition = value;
                ActivityMonitor.Instance.Event(new Event.TrackingEvent(value));
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"WiseTele:Stop ({reason}) - started");
            #endregion

            if (Slewing)
            {
                try
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Canceling telescopeCT: #{0}", telescopeCT.GetHashCode());
                    #endregion
                    telescopeCTS.Cancel();
                    telescopeCTS = new CancellationTokenSource();
                }
                catch (AggregateException ax)
                {
                    ax.Handle((Func<Exception, bool>)((ex) =>
                    {
                        #region debug
                        debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                            "Stop: telescope slewing cancellation got {0}", ex.Message);
                        #endregion debug
                        return ex is ObjectDisposedException;
                    }));
                }

                if (EnslavesDome)
                {
                    try
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Calling DomeStopper");
                        #endregion
                        DomeStopper();
                    }
                    catch (AggregateException ax)
                    {
                        ax.Handle((Func<Exception, bool>)((ex) =>
                        {
                            #region debug
                            debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                                "Stop: dome slewing cancellation got {0}", ex.Message);
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:Stop - Stopping {0}", motor.WiseName);
                    #endregion
                    motor.SetOff();
                }

            safetyMonitorTimer.DisableIfNotNeeded();
        }

        public void AbortPulseGuiding()
        {
            pulsing.Abort();
        }

        public void FullStop()
        {
            Stop("Action(\"full-stop\")");
            if (IsPulseGuiding)
                AbortPulseGuiding();
            Tracking = false;

            foreach (WiseVirtualMotor motor in allMotors)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "WiseTele:FullStop - Stopping {0}", motor.WiseName);
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

                if (_movingToSafety)
                    reasons.Add("Moving to safety");

                if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
                    reasons.Add("Shutter is moving");

                if (reasons.Count > 0)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, $"Slewing Get - True ({string.Join("; ", reasons)})");
                    #endregion debug
                    return true;
                }
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "Slewing Get - False");
                #endregion debug
                return false;
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
                Exceptor.Throw<PropertyNotImplementedException>("DeclinationRate", $"value: {value}", true);
            }
        }

        public void HandpadMoveAxis(TelescopeAxes Axis, double Rate)
        {
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"HandpadMoveAxis({Axis}, {Rate})");
            #endregion debug

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None :
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            try
            {
                activityMonitor.NewActivity(new Activity.Handpad(new Activity.Handpad.StartParams() {
                    axis = Axis,
                    rate = Rate,
                    start = (Axis == TelescopeAxes.axisPrimary) ?
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
                    end = (Axis == TelescopeAxes.axisPrimary) ?
                        WiseTele.Instance.RightAscension :
                        WiseTele.Instance.Declination,
                });
                throw;
            }

            if (!BypassCoordinatesSafety)
                safetyMonitorTimer.EnableIfNeeded(SafetyMonitorTimer.ActionWhenNotSafe.StopMotors);
        }

        public void HandpadStop()
        {
            TelescopeAxes axis;

            if (NorthMotor.IsOn || SouthMotor.IsOn)
                axis = TelescopeAxes.axisSecondary;
            else if (WestMotor.IsOn || EastMotor.IsOn)
                axis = TelescopeAxes.axisPrimary;
            else
                return;

            StopAxis(axis);

            activityMonitor.EndActivity(ActivityMonitor.ActivityType.Handpad, new Activity.Handpad.EndParams()
            {
                endState = Activity.State.Succeeded,
                endReason = "HandpadStop()",
                end = (axis == TelescopeAxes.axisPrimary) ?
                        WiseTele.Instance.RightAscension :
                        WiseTele.Instance.Declination,
            });
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Handpad: stopped");
            #endregion
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, $"MoveAxis({Axis}, {Rate})");
            #endregion debug

            if (!wisesafetooperate.IsSafeWithoutCheckingForShutdown() && !ShuttingDown && !BypassCoordinatesSafety)
            {
                Exceptor.Throw<InvalidOperationException>($"MoveAxis({Axis}, {RateName(Rate)})",
                    string.Join(", ", wisesafetooperate.UnsafeReasonsList()));
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
                        "StopAxis({0}):  {1} was on, stopping it.", axis, m.WiseName);
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"InternalMoveAxis({thisAxis}, {RateName(Rate)}): called");
            #endregion debug

            if (thisAxis == TelescopeAxes.axisTertiary)
                Exceptor.Throw<InvalidValueException>($"InternalMoveAxis({thisAxis})", "This telescope cannot move in axisTertiary");

            if (AtPark)
            {
                Exceptor.Throw<InvalidValueException>($"InternalMoveAxis({thisAxis})", "Cannot MoveAxis while AtPark");
            }

            if (Rate != Const.rateStopped && !wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidValueException>($"InternalMoveAxis({thisAxis})", string.Join(", ", wisesafetooperate.UnsafeReasonsList()));

            if (Rate == Const.rateStopped)
            {
                StopAxisAndWaitForHalt(thisAxis);
                safetyMonitorTimer.DisableIfNotNeeded();
                _trackingRestorer.RemoveMover();
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"InternalMoveAxis({thisAxis}, {RateName(Rate)}): done.");
                #endregion
                return true;
            }

            double absRate = Math.Abs(Rate);
            if (!((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                Exceptor.Throw<InvalidValueException>($"InternalMoveAxis({thisAxis}, {absRate})", "Invalid rate.");

            if (!readyToSlewFlags.AxisCanMoveAtRate(thisAxis, absRate))
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"InternalMoveAxis({thisAxis}, {RateName(absRate)}) not BOTH axes are ready to move");
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
                    $"Don't know how to InternalMoveAxis({thisAxis}, {RateName(absRate)}) (no mover) ({axisDirectionName[thisAxis][direction]}) [{e.Message}]");
                #endregion debug
                return false;
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                $"InternalMoveAxis({thisAxis}, {RateName(Rate)}): direction: {axisDirectionName[thisAxis][direction]}, stopTracking: {stopTracking}");
            #endregion debug

            if (stopTracking)
            {
                _trackingRestorer.AddMover();
                Tracking = false;
            }

            #region debug
            Angle currPosition = (thisAxis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(RightAscension, Angle.AngleType.RA) :
                Angle.FromDegrees(Declination, Angle.AngleType.Dec);

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
                $"InternalMoveAxis({thisAxis}, {RateName(absRate)}): currPosition: {currPosition}, started motors: {string.Join(", ", startedMotors)}");
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
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "IsPulseGuiding: {0}", ret);
                #endregion
                return ret;
            }
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToTargetAsync()
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            if (_targetRightAscension == null)
                Exceptor.Throw<ValueNotSetException>("SlewToTargeAsync", "Target RA not set");
            if (_targetDeclination == null)
                Exceptor.Throw<ValueNotSetException>("SlewToTargeAsync", "Target Dec not set");

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.AngleType.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.AngleType.Dec);

            string op = $"SlewToTargetAsync({ra}, {dec})";
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, op);
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
            safer = SaferAtCoordinates(direction, Angle.FromDegrees(ra, Angle.AngleType.RA), Angle.FromDegrees(dec, Angle.AngleType.Dec));
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"SafeToMove({direction}: {safer}");
            #endregion
            return safer;
        }

        public bool SaferAtCoordinates(string dir, Angle ra, Angle dec)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;
            double dist0, dist1;
            bool ret;

            wisesite.PrepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
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
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                $"SaferAtCoordinates({dir}, {ra.ToNiceString()}, {dec.ToNiceString()}): new: {dist0:f12} < curr: {dist1:f12} => {ret}");
            #endregion
            return ret;
        }

        public readonly Angle altLimit = new Angle(16.0, Angle.AngleType.Alt);
        public readonly Angle eastern_haLimit = Angle.FromHours(-7.0, Angle.AngleType.HA);
        public readonly Angle western_haLimit = Angle.FromHours(7.0, Angle.AngleType.HA);
        public readonly Angle lower_decLimit = Angle.FromDegrees(-35.0, Angle.AngleType.Dec);
        public readonly Angle upper_decLimit = Angle.FromDegrees(89.9, Angle.AngleType.Dec);

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
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                WiseSite.astrometricAccuracy,
                0, 0,
                wisesite._onSurface,
                ra.Hours, dec.Degrees,
                WiseSite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            Angle alt = Angle.FromDegrees(90.0 - zd, Angle.AngleType.Alt);
            if (alt < altLimit)
                reasons.Add($"Altitude too low: {alt} < {altLimit}");

            if (dec > upper_decLimit)
                reasons.Add($"Declination too high: {dec} > {upper_decLimit}");
            if (dec < lower_decLimit)
                reasons.Add($"Declination too low: {dec} < {lower_decLimit}");

            double ha = HourAngle;
            if (ha < eastern_haLimit.Hours)
                reasons.Add($"HourAngle too low: {Angle.FromHours(ha, Angle.AngleType.HA)} < {eastern_haLimit}");
            else if (ha > western_haLimit.Hours)
                reasons.Add($"HourAngle too high: {Angle.FromHours(ha, Angle.AngleType.HA)} > {western_haLimit}");

            if (reasons.Count > 0)
            {
                string msg = $"SafeAtCoordinates(ra: {ra}, dec: {dec}) - " + String.Join(", ", reasons.ToArray());
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, msg);
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

            if (_movingToSafety)    // timer callback while being disabled
                return;

            _movingToSafety = true;
            safetyMonitorTimer.Enabled = false;

            if (Tracking)
                Tracking = false;
            Stop(op);

            List<BackoffAction> backoffs = new List<BackoffAction>();
            if (SafeToMove("east"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "East", Rate = -Const.rateSlew });
            else if (SafeToMove("west"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisPrimary, Direction = "West", Rate = Const.rateSlew });

            if (SafeToMove("south"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "South", Rate = Const.rateSlew });
            else if (SafeToMove("north"))
                backoffs.Add(new BackoffAction { Axis = TelescopeAxes.axisSecondary, Direction = "North", Rate = -Const.rateSlew });

            foreach (var b in backoffs)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"{op}: {b.Direction}: calling MoveAxis({b.Axis}, {b.Direction}, {RateName(b.Rate)}) for {backoffMillis} millis ...");
                #endregion
                MoveAxis(b.Axis, b.Rate);
                Thread.Sleep(backoffMillis);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op}: stopping {b.Axis}");
                #endregion
                MoveAxis(b.Axis, Const.rateStopped);
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Backoff: done");
            #endregion

            _movingToSafety = false;
            safetyMonitorTimer.Enabled = true;
        }

        public bool AtPark
        {
            get
            {
                bool ret = _atPark;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, $"AtPark Get - {ret}");
                #endregion debug
                return ret;
            }

            set
            {
                _atPark = value;
                #region debug
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugLogic, $"AtPark Set - {_atPark}");
                #endregion debug
            }
        }

        private void DoShutdown(string reason)
        {
            if (activityMonitor.ShuttingDown)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"doShutdown(reason: {reason}): shutdown already in progress (activityMonitor.ShuttingDown == true) => skipping shutdown");
                #endregion
                return;
            }

            if (AtPark && domeSlaveDriver.AtPark && domeSlaveDriver.ShutterState == ShutterState.shutterClosed)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    $"doShutdown(reason: {reason}): Wise40 is already shut down (AtPark and dome.AtPark and shutterClosed) => skipping shutdown");
                #endregion
                return;
            }

            SafeToOperateDigest safetooperateDigest = JsonConvert.DeserializeObject<SafeToOperateDigest>(wisesafetooperate.Digest);

            bool rememberToCancelSafetyBypass = false;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): starting activity ShuttingDown ...");
            #endregion
            activityMonitor.NewActivity(new Activity.Shutdown(new Activity.Shutdown.StartParams() { reason = reason }));

            if (!safetooperateDigest.Bypassed)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): starting safetooperate bypass ...");
                #endregion
                rememberToCancelSafetyBypass = true;
                wisesafetooperate.Action("start-bypass", "temporary");
            }

            if (AtPark)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: setting AtPark to false ...");
                #endregion
                AtPark = false; // Don't call Unpark(), it throws exception if while ShuttingDown
            }

            if (domeSlaveDriver.AtPark)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling domeSlaveDriver.Unpark() ...");
                #endregion
                DomeSlaveDriver.Unpark();
            }

            if (Slewing)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling AbortSlew() ...");
                #endregion
                AbortSlew("WiseTele:Shutdown():doShutdown()");
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: waiting for !Slewing ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (Slewing);
            }

            if (IsPulseGuiding)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling AbortPulseGuiding() ...");
                #endregion
                AbortPulseGuiding();
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: waiting for !IsPulseGuiding ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (IsPulseGuiding);
            }

            if (domeSlaveDriver.Slewing)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling domeSlaveDriver.AbortSlew() ...");
                #endregion
                DomeSlaveDriver.AbortSlew();
                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: waiting for !domeSlaveDriver.Slewing ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (domeSlaveDriver.Slewing);
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): setting Tracking to false ...");
            #endregion
            Tracking = false;

            if (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
            {
                // Wait for shutter to close before continuing
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling domeSlaveDriver.CloseShutter() ...");
                #endregion
                DomeSlaveDriver.CloseShutter($"reason: DoShutdown(reason: {reason})");
                while (domeSlaveDriver.ShutterState != ShutterState.shutterClosed)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: waiting for domeSlaveDriver.ShutterState == ShutterState.shutterClosed ...");
                    #endregion
                    Thread.Sleep(1000);
                }
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): calling Park() ...");
            #endregion
            try
            {
                Park();
            } catch (Exception ex)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): exception during Park(): {0}", ex.ToString());
                #endregion
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): after Park() ...");
            #endregion

            if (rememberToCancelSafetyBypass)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"doShutdown(reason: {reason}): ending safetooperate bypass ...");
                #endregion
                wisesafetooperate.Action("end-bypass", "temporary");
            }

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "doShutdown: ending activity ShuttingDown ...");
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
            Task.Run(() => DoShutdown(reason));
        }

        //
        // This is the Synchronous version, as mandated by ASCOM
        //
        public void Park()
        {
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park: started");
            #endregion debug
            if (AtPark)
                return;

            Angle targetRa = wisesite.LocalSiderealTime;
            Angle targetDec = parkingDeclination;

            bool wasEnslavingDome = EnslavesDome;
            bool wasTracking = Tracking;
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
                        ra = targetRa.Hours,
                        dec = targetDec.Degrees,
                    },
                    domeStartAz = WiseDome.Instance.Azimuth.Degrees,
                    domeTargetAz = 90.0,
                    shutterPercent = 100,
                }));
                if (wasEnslavingDome)
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: starting DomeParker() ...");
                    #endregion
                    DomeParker();
                }
                TargetRightAscension = targetRa.Hours;
                TargetDeclination = targetDec.Degrees;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: setting Tracking = true ...");
                #endregion
                Tracking = true;

                EnslavesDome = false;
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: starting InternalSlewToCoordinatesSync ...");
                #endregion
                InternalSlewToCoordinatesSync(targetRa, targetDec);
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: after InternalSlewToCoordinatesSync ...");
                #endregion

                do
                {
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: waiting for Telescope to stop Slewing ...");
                    #endregion
                    Thread.Sleep(1000);
                } while (activityMonitor.InProgress(ActivityMonitor.ActivityType.TelescopeSlew));
            }
            catch(Exception ex)
            {
                Parking = false;
                EnslavesDome = wasEnslavingDome;
                Tracking = wasTracking;
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Parking, new Activity.Park.EndParams()
                {
                    endState = Activity.State.Failed,
                    endReason = $"Exception: \"{ex}\".",
                    end = new Activity.TelescopeSlew.Coords
                    {
                        ra = RightAscension,
                        dec = Declination,
                    },
                    domeAz = WiseDome.Instance.Azimuth.Degrees,
                    shutterPercent = WiseDome.Instance.wisedomeshutter.PercentOpen,
                });
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: Aborted due to Exception: {0}", ex.ToString());
                #endregion
                if (ShuttingDown)
                    throw;
                Parking = false;
                Tracking = false;
                activityMonitor.EndActivity(ActivityMonitor.ActivityType.Parking, new Activity.Park.EndParams()
                {
                    endState = Activity.State.Failed,
                    endReason = $"Parking failed due to Exception: {ex} at {ex.StackTrace}",
                    end = new Activity.TelescopeSlew.Coords
                    {
                        ra = RightAscension,
                        dec = Declination,
                    },
                    domeAz = WiseDome.Instance.Azimuth.Degrees,
                    shutterPercent = WiseDome.Instance.wisedomeshutter.PercentOpen,
                });
                EnslavesDome = wasEnslavingDome;
                return;
            }
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Park: all done, setting AtPark == true");
            #endregion
            AtPark = true;
            Parking = false;
            Tracking = false;
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
            EnslavesDome = wasEnslavingDome;
        }

        public void ParkFromGui(bool parkDome)
        {
            #region debug
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park");
            #endregion debug
            if (AtPark)
                return;

            Angle ra = wisesite.LocalSiderealTime;
            Angle dec = parkingDeclination;

            if (parkDome)
                DomeParker();
            SlewToCoordinatesAsync(ra.Hours, dec.Degrees, false);
        }

        private void InternalSlewToCoordinatesSync(Angle RightAscension, Angle Declination)
        {
            string op = $"InternalSlewToCoordinatesSync: ({RightAscension.ToNiceString()}, {Declination.ToNiceString()})";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"{op} called");
            #endregion debug
            try
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"InternalSlewToCoordinatesSync: telescopeCT: #{telescopeCT.GetHashCode()}");
                #endregion

                Task.Run(() => DoSlewToCoordinatesAsync(RightAscension, Declination, op), telescopeCT);
            }
            catch (AggregateException ae)
            {
                ae.Handle((Func<Exception, bool>)((ex) =>
                {
                    #region debug
                    debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                        $"InternalSlewToCoordinatesSync: Caught \"{ex.Message}\" at\n{ex.StackTrace}");
                    #endregion
                    return false;
                }));
            }

            Thread.Sleep(200);
            while (slewers.Count > 0)
                Thread.Sleep(200);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "all slewers done");
            #endregion
        }

        private enum ScopeSlewerStatus { Undefined, CloseEnough, ChangedDirection, Canceled, Failed, Timedout };

        private Angle CurrentPosition(Angle.AngleType angleType)
        {
            switch (angleType)
            {
                case Angle.AngleType.RA:
                    return Angle.FromHours(RightAscension);
                case Angle.AngleType.HA:
                    return Angle.FromHours(HourAngle);
                case Angle.AngleType.Dec:
                    return Angle.FromDegrees(Declination);
                default:
                    Exceptor.Throw<Exception>("CurrentPosition", $"Invalid angle type {angleType}");
                    return Angle.invalid;
            }
        }

        private void ScopeAxisSlewer(TelescopeAxes thisAxis, Angle targetPosition)
        {
            Angle.AngleType coordType = targetPosition.Type;
            Angle currentPosition = CurrentPosition(coordType);

            string slewerName = thisAxis.ToString() + "Slewer";
            DateTime start = DateTime.Now;

            ScopeSlewerStatus status = ScopeSlewerStatus.Undefined;
            ShortestDistanceResult distanceToTarget = currentPosition.ShortestDistance(targetPosition);
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

                        currentPosition = CurrentPosition(coordType);

                        // let the other axis know we're ready to move at this rate
                        readyToSlewFlags.AxisBecomesReadyToMoveAtRate(thisAxis, rate);

                        // check how far we are from target
                        distanceToTarget = currentPosition.ShortestDistance(targetPosition);
                        if (!EnoughDistanceToMove(thisAxis, distanceToTarget.angle, rate))
                        {
                            // there's not enough distance to move at this rate
                            closeEnoughRates++;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{slewerName}: distance {distanceToTarget.angle.ToNiceString()} too short for {RateName(rate)} (closeEnoughRates: {closeEnoughRates})");
                            #endregion
                            continue;
                        }

                        // enough distance to move, let's wait for the other axis
                        while (!readyToSlewFlags.AxisCanMoveAtRate(thisAxis, rate))
                        {
                            currentPosition = CurrentPosition(coordType);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                $"{slewerName} at {RateName(rate)}: current: {currentPosition} waiting for the other axis ...");
                            #endregion
                            telescopeCT.ThrowIfCancellationRequested();
                            Thread.Sleep(50);
                        }

                        currentPosition = CurrentPosition(coordType);
                        distanceToTarget = currentPosition.ShortestDistance(targetPosition);

                        // Wait for InternalMoveAxis to start moving thisAxis
                        while (! InternalMoveAxis(thisAxis, rate, distanceToTarget.direction, false))
                        {
                            const int waitForAxisToStartMovingMillis = 500;

                            currentPosition = CurrentPosition(coordType);
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{slewerName}: at {currentPosition} waiting {waitForAxisToStartMovingMillis} " +
                                $"millis to start InternalMoveAxis({thisAxis}, {RateName(rate)}, {distanceToTarget.direction}) ...");
                            #endregion
                            telescopeCT.ThrowIfCancellationRequested();
                            Thread.Sleep(waitForAxisToStartMovingMillis);
                            telescopeCT.ThrowIfCancellationRequested();
                        }

                        ShortestDistanceResult currentDistance = null;
                        MovementParameters mp = movementParameters[thisAxis][rate];

                        Angle startingPosition = CurrentPosition(coordType);
                        DateTime startingTime = DateTime.Now;
                        ShortestDistanceResult startingDistance = startingPosition.ShortestDistance(targetPosition);
                        const double lowestRad = Double.MaxValue, highestRad = Double.MinValue;
                        //int lowFailures, highFailures, zeroFailures = 0;
                        //int distanceFailures = 0;
                        //const int maxDistanceFailures = 3;
                        //const int maxLowFailures = 3, maxHighFailures = 3, maxZeroFailures = 3;
                        double prevDistance = 0.0;
                        TimeSpan elapsed;

                        //lowFailures = 0;
                        //highFailures = 0;

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

                            currentPosition = CurrentPosition(coordType);

                            currentDistance = currentPosition.ShortestDistance(targetPosition);

                            elapsed = DateTime.Now.Subtract(startingTime);
                            if (elapsed >= mp.maxTime) {
                                #region Timeout
                                status = ScopeSlewerStatus.Timedout;
                                #region debug
                                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                        $"SUSPECT: {slewerName} at {RateName(rate)}: at {currentPosition}, Timedout ==> target: {targetPosition}, elapsed: {elapsed} >= mp.maxTime: {mp.maxTime}");
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
                                        "{0} at {1}: at {2}, ChangedDirection ==> target: {3}, originalDirection: {4} != currentDistance.direction: {5}",
                                        slewerName, RateName(rate), currentPosition, targetPosition,
                                        startingDistance.direction, currentDistance.direction);
                                #endregion
                                break;
                                #endregion
                            }
                            else if (prevDistance != 0.0) {
                                #region Distance to Target is NOT decreasing
                                if (currentDistance.angle.Radians > prevDistance)   // the distance to target is increasing
                                {
                                    #region debug
                                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                        "SUSPECT: distance to target is INCREASING");
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
                                        $"{slewerName}:{GetHashCode()} at {RateName(rate)}: at {currentPosition}, " +
                                        $"CloseEnough ==> target: {targetPosition}, " +
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
                                        $"curr: {currentPosition.Radians:f10}, target: {targetPosition.Radians:f10}, delta: {Math.Abs(targetPosition.Radians - currentPosition.Radians):f10}";
                                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, dbg);
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
                                        $"{slewerName} at {RateName(rate)}: at {currentPosition}, " +
                                        $"moving ==> target: {targetPosition}, " +
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
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                $"mp[{motors}].lowestRad: {lowestRad:f10}, highestRad: {highestRad:f10}, rate: {RateName(rate)}");
                            #endregion
                        }

                        if (status == ScopeSlewerStatus.Timedout)
                            AbortSlew($"target: {targetPosition.ToNiceString()}: Timedout at rate {RateName(rate)} after {elapsed.ToMinimalString()}");
                    }
                }

                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, $"{slewerName} Done at {currentPosition} target: {targetPosition}, " +
                    $"distance-to-target: {distanceToTarget.angle.ToNiceString()}, status: {status}, total-duration: {DateTime.Now.Subtract(start)}");
                #endregion
            }
            catch (OperationCanceledException)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    $"{slewerName} at {RateName(r)}: Slew cancelled at {currentPosition}");
                #endregion debug
                StopAxisAndWaitForHalt(thisAxis, slewerName, r);
                status = ScopeSlewerStatus.Canceled;
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
                Angle.FromHours(RightAscension, Angle.AngleType.RA) :
                Angle.FromDegrees(Declination, Angle.AngleType.Dec);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, msg + $"at {a} waiting for {axis} to stop moving ...");
            #endregion debug
            while (AxisIsMoving(axis))
            {
                Thread.Sleep(waitForOtherAxisMillis);
            }
            AxisIsStoppingDict[axis] = false;
            #region debug
            Angle b = (axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(RightAscension, Angle.AngleType.RA) :
                Angle.FromDegrees(Declination, Angle.AngleType.Dec);
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
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        $"slewer \"{Slewers.Type.Dome}\" completed with status: {domeSlewerTask.Status}");
                    #endregion
                    domeSlewTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    slewers.Delete(Slewers.Type.Dome);
                }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void DomeSlewer(Angle ra, Angle dec, string reason)
        {
            GenericDomeSlewerTask(() => domeSlaveDriver.SlewToAz(ra, dec, reason));
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
        private void DoSlewToCoordinatesAsync(Angle targetRightAscension, Angle targetDeclination, string reason)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            CheckCoordinateSanity(Angle.AngleType.RA, targetRightAscension.Hours, reason);
            CheckCoordinateSanity(Angle.AngleType.Dec, targetDeclination.Degrees, reason);
            // Check coordinates safety ???

            Slewers.Clear();
            readyToSlewFlags.Reset();
            activityMonitor.NewActivity(new Activity.TelescopeSlew(new Activity.TelescopeSlew.StartParams()
            {
                start = new Activity.TelescopeSlew.Coords()
                {
                    ra = RightAscension,
                    dec = Declination
                },
                target = new Activity.TelescopeSlew.Coords()
                {
                    ra = targetRightAscension.Hours,
                    dec = targetDeclination.Degrees
                }
            }));

            ShortestDistanceResult raDistance = targetRightAscension.ShortestDistance(Angle.FromHours(RightAscension));
            ShortestDistanceResult decDistance = targetDeclination.ShortestDistance(Angle.FromDegrees(Declination));

            if (! EnoughDistanceToMove(TelescopeAxes.axisPrimary, raDistance.angle, Const.rateGuide) &&
                ! EnoughDistanceToMove(TelescopeAxes.axisSecondary, decDistance.angle, Const.rateGuide))
            {
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
                return;
            }

            try
            {
                if (EnslavesDome)
                {
                    DomeSlewer(targetRightAscension, targetDeclination, "Follow telescope to new target");
                }

                if (!ShuttingDown)
                    telescopeCT = telescopeCTS.Token;

                foreach (Slewers.Type slewerType in new List<Slewers.Type>() { Slewers.Type.Ra, Slewers.Type.Dec })
                {
                    SlewerTask slewer = new SlewerTask() { type = slewerType, task = null };
                    try
                    {
                        TelescopeAxes axis;
                        Angle angle;
                        if (slewerType == Slewers.Type.Ra)
                        {
                            axis = TelescopeAxes.axisPrimary; angle = targetRightAscension;
                        }
                        else
                        {
                            axis = TelescopeAxes.axisSecondary; angle = targetDeclination;
                        }

                        slewers.Add(slewer);
                        slewer.task = Task.Run(() => ScopeAxisSlewer(axis, angle), telescopeCT).ContinueWith((slewerTask) =>
                        {
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                                $"_doSlewToCoordinatesAsync: Slewer \"{slewer.type}\" completed with status: {slewerTask.Status}");
                            #endregion
                            slewers.Delete(slewerType);

                            if (slewerTask.Status == TaskStatus.Canceled)
                            {
                                Exceptor.Throw<OperationCanceledException>(
                                    $"DoSlewToCoordinatesAsync({targetRightAscension}, {targetDeclination})",
                                    $"Slewer \"{slewer.type}\" Canceled");
                            }
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (OperationCanceledException ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            $"_doSlewToCoordinatesAsync: Slewer \"{slewer.type}\": Caught: {(ex.InnerException ?? ex).Message}" +
                            $"at\n{(ex.InnerException ?? ex).StackTrace}");
                        #endregion
                        if (ShuttingDown)
                            throw;
                    }
                    catch (Exception ex)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic, $"_doSlewToCoordinatesAsync: Failed to run slewer {slewerType}: {ex.Message} at\n{ex.StackTrace}");
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
                    debugger.WriteLine((Debugger.DebugLevel)Debugger.DebugLevel.DebugExceptions,
                        $"_doSlewToCoordinatesAsync: Caught {ex.Message} at\n{ex.StackTrace}");
                    #endregion
                    return false;
                }));
            }
        }

        public void SlewToCoordinates(double RightAscension, double Declination, bool noSafetyCheck = false)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.AngleType.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.AngleType.Dec);

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
                InternalSlewToCoordinatesSync(ra, dec);
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
        public void SlewToCoordinatesAsync(double RightAscension, double Declination, bool doChecks = true)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            CheckCoordinateSanity(Angle.AngleType.RA, RightAscension, "SlewToCoordinatesAsync");
            CheckCoordinateSanity(Angle.AngleType.Dec, Declination, "SlewToCoordinatesAsync");

            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.AngleType.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.AngleType.Dec);

            string op = $"SlewToCoordinatesAsync({ra}, {dec})";

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugLogic, op);
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
                    $"SlewToCoordinatesAsync({RightAscension}, {Declination}) caught exception: {e.Message} at\n{e.StackTrace}");
                #endregion
            }
        }

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
#pragma warning restore IDE1006 // Naming Styles
        {
            string op = $"_slewToCoordinatesAsync({RightAscension.ToNiceString()}, {Declination.ToNiceString()})";

            //if (DecOver90Degrees)
            //{
            //    telescopeCT = telescopeCTS.Token;
            //    Task southScooter = Task.Run(() =>
            //    {
            //        ScootSouth();
            //    }, telescopeCT).ContinueWith((scooter) =>
            //    {
            //        #region debug
            //        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
            //            "southScooter completed with status: {0}", scooter.Status.ToString());
            //        #endregion
            //        _doSlewToCoordinatesAsync(RightAscension, Declination);
            //    }, TaskContinuationOptions.ExecuteSynchronously);
            //}
            //else
                DoSlewToCoordinatesAsync(RightAscension, Declination, op);
        }

        //public void ScootSouth()
        //{
        //    if (!DecOver90Degrees)
        //        return;

        //    #region debug
        //    debugger.WriteLine(Debugger.DebugLevel.DebugLogic, "Scooting South from {0}, {1}",
        //        Angle.FromHours(_instance.RightAscension).ToNiceString(),
        //        Angle.FromDegrees(_instance.Declination).ToNiceString());
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

        public static void SyncToTarget()
        {
            Exceptor.Throw<MethodNotImplementedException>("SyncToTarget", "SyncToTarget not implemented");
        }

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
                return wisesite.Elevation;
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
                return wisesite.Latitude.Degrees;
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
                return wisesite.Longitude.Degrees;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>($"SiteLongitude({value})", "Not implemented", true);
            }
        }

        public void SlewToTarget()
        {
            Angle ra = Angle.FromHours(TargetRightAscension, Angle.AngleType.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.AngleType.Dec);
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
            Exceptor.Throw<MethodNotImplementedException>($"SyncToAltAz({Azimuth}, {Altitude})", "SyncToAltAz not implemented");
        }

        public static void SyncToCoordinates(double RightAscension, double Declination)
        {
            Exceptor.Throw<MethodNotImplementedException>($"SyncToCoordinates({RightAscension}, {Declination})", "SyncToCoordinates not implemented");
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
                return false;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                return false;
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
                return false;
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

        public short SlewSettleTime
        {
            get
            {
                Exceptor.Throw<PropertyNotImplementedException>("SlewSettleTime", "SlewSettleTime");
                return short.MinValue;
            }

            set
            {
                Exceptor.Throw<PropertyNotImplementedException>("SlewSettleTime", "SlewSettleTime", true);
            }
        }

        private void AltAzToHaDec(double alt, double az, ref double ha, ref double dec)
        {
            DateTime ut = DateTime.UtcNow;
            double d1 = 0.0, d2 = 0.0;
            int ret;
            WiseSite site = WiseSite.Instance;
            DriverAccess.ObservingConditions och = WiseSite.och;
            double raRadians = 0.0, decRadians = 0.0;

            ASCOM.Astrometry.SOFA.SOFA sofa = new ASCOM.Astrometry.SOFA.SOFA();
            ret = sofa.Dtf2d("UTC", ut.Year, ut.Month, ut.Day, ut.Hour, ut.Minute, ut.Second, ref d1, ref d2);
            if (ret != 0)
                Exceptor.Throw<Exception>("AltAzToHaDec", $"sofa.Dtf2d returned: {ret}");

            ret = sofa.ObservedToCelestial(type: "A", ob1: az, ob2: 90.0 - alt, utc1: d1, utc2: d2, dut1: 0.0,
                elong: site.Longitude.Radians, phi: site.Latitude.Radians, hm: site.Elevation,
                xp: 0, yp: 0,
                phpa: och.Pressure, tc: och.Temperature, rh: och.Humidity, wl: 0.55,
                rc: ref raRadians, dc: ref decRadians);
            if (ret != 0)
                Exceptor.Throw<Exception>("AltAzToHaDec", $"sofa.ObservedToCelestial returned: {ret}");

            ha = (site.LocalSiderealTime - Angle.FromRadians(raRadians, Angle.AngleType.RA)).Hours;
            if (ha < 0)
                ha += 24;
            if (ha > 24)
                ha -= 24;
            dec = Angle.FromRadians(decRadians, Angle.AngleType.Dec).Degrees;
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            string op = $"SlewToAltAz({Azimuth}, {Altitude})";

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
            {
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");
            }

            double ha = Double.MinValue, dec = Double.MinValue;

            AltAzToHaDec(Altitude, Azimuth, ref ha, ref dec);
            Exceptor.Throw<MethodNotImplementedException>(op, "Not implemented");
        }

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.
        public void SlewToAltAzAsync(double Azimuth, double Altitude)
#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
        {
            string op = $"SlewToAltAzAsync({Azimuth}, {Altitude})";

            if (EnslavesDome && domeSlaveDriver.ShutterIsMoving)
            {
                Exceptor.Throw<InvalidOperationException>(op, "Cannot slew while the shutter is moving");
            }

            double ha = Double.MinValue, dec = Double.MinValue;

            AltAzToHaDec(Altitude, Azimuth, ref ha, ref dec);
            Exceptor.Throw<MethodNotImplementedException>(op, "Not implemented");
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
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, "PulseGuide: Direction={0}, Duration={1}", Direction.ToString(), Duration.ToString());
            #endregion
            if (AtPark)
                Exceptor.Throw<InvalidOperationException>($"PulseGuide({Direction}, {Duration})", "Cannot PulseGuide while AtPark");

            if (Slewing)
                Exceptor.Throw<InvalidOperationException>($"PulseGuide({Direction}, {Duration})", "Cannot PulseGuide while Slewing");

            if (!wisesafetooperate.IsSafe && !ShuttingDown)
                Exceptor.Throw<InvalidOperationException>($"PulseGuide({Direction}, {Duration})", $"Not safe to operate ({wisesafetooperate.UnsafeReasons})");

            (pulsing ?? (pulsing = Pulsing.Instance)).Init();

            TelescopeAxes axis = Pulsing.guideDirection2Axis[Direction];
            if (Pulsing.Active(axis))
                Exceptor.Throw<InvalidOperationException>($"PulseGuide({Direction}, {Duration})", $"Already PulseGuiding on {axis}");

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
                Exceptor.Throw<InvalidOperationException>($"PulseGuide({Direction}, {Duration})", $"Caught {ex.Message} at {ex.StackTrace}");
            }
        }

        private static readonly ArrayList supportedActions = new ArrayList() {
            "active",
            "activities",
            "seconds-till-idle",
            "opmode",
            "status",
            "nearly-parked",
        };

        public ArrayList SupportedActions
        {
            get
            {
                return supportedActions;
            }
        }

        public string Action(string action, string parameter)
        {
            action = action.ToLower();
            parameter = parameter.ToLower();

            switch (action)
            {
                case "active":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        bool x = Convert.ToBoolean(parameter);
                        activityMonitor.StayActive($"action active={x}");
                    }
                    return activityMonitor.ObservatoryIsActive().ToString();

                case "activities":
                    return JsonConvert.SerializeObject(ActivityMonitor.ObservatoryActivities);

                case "shutdown":
                    telescopeCT = telescopeCTS.Token;
                    Task.Run(() => Shutdown(parameter), telescopeCT);
                    return "ok";

                case "abort-shutdown":
                    AbortSlew("Action(\"abort-shutdown\"");
                    activityMonitor.EndActivity(ActivityMonitor.ActivityType.ShuttingDown, new Activity.GenericEndParams
                    {
                        endReason = "Action(\"abort-shutdown\"",
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

                case "safe-to-move":
                    return JsonConvert.SerializeObject(SafeToMove(parameter));

                case "park":
                    Task.Run(() => Park());
                    return "ok";

                case "move-to-preset":
                    switch(parameter)
                    {
                        case "zenith":
                            return MoveToKnownHaDec(new Angle("0h0m0s"), Angle.FromDegrees(wisesite.Latitude.Degrees));

                        case "flat":
                            return MoveToKnownHaDec(new Angle("-1h35m59.0s"), new Angle("41:59:20.0"));

                        case "ha0":
                            return "ok";

                        case "cover":
                            return MoveToKnownHaDec(new Angle("11h55m00.0s"), new Angle("88:00:00.0"));

                        default:
                            return $"move-to-preset: Bad parameter \"{parameter}\"";
                    }

                case "hardware-meta-digest":
                    Hardware.Hardware.Instance.init();
                    WiseTele.Instance.Init();
                    WiseDome.Instance.Init();
                    WiseFocuser.Instance.Init();

                    return JsonConvert.SerializeObject(HardwareMetaDigest.FromHardware());

                case "hardware-digest":
                    return JsonConvert.SerializeObject(HardwareDigest.FromHardware());

                default:
                    Exceptor.Throw<ActionNotImplementedException>($"Action({action})", "Not implemented by this driver");
                    return "false";
            }
        }

        private string MoveToKnownHaDec(Angle ha, Angle dec)
        {
            Angle ra = wisesite.LocalSiderealTime - ha;
            bool savedEnslaveDome = EnslavesDome;

            EnslavesDome = false;
            Tracking = true;
            try
            {
                SlewToCoordinatesAsync(ra.Hours, dec.Degrees, false);
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
                ShortestDistanceResult delta = Angle.FromHours(RightAscension).ShortestDistance(wisesite.LocalSiderealTime);
                if (delta.angle > new Angle("00h10m00s"))
                    return false;

                delta = Angle.FromDegrees(Declination).ShortestDistance(parkingDeclination);
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

                if (slewers.Active(Slewers.Type.Dec) || slewers.Active(Slewers.Type.Ra))
                {
                    string to = null;

                    Angle ra, dec;
                    try
                    {
                        ra = Angle.FromHours(TargetRightAscension, Angle.AngleType.RA);
                        to += " RA " + ra.ToNiceString();
                    }
                    catch { }

                    try
                    {
                        dec = Angle.FromDegrees(TargetDeclination, Angle.AngleType.Dec);
                        to += " DEC " + dec.ToNiceString();
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
                double targetRA = Const.noTarget, targetDec = Const.noTarget;

                if (_targetRightAscension != null)
                    targetRA = _targetRightAscension.Hours;

                if (_targetDeclination != null)
                    targetDec = _targetDeclination.Degrees;

                TelescopeDigest digest = new TelescopeDigest()
                {
                    Current = new TelescopePosition
                    {
                        RightAscension = RightAscension,
                        Declination = Declination,
                    },

                    Target = new TelescopePosition
                    {
                        RightAscension = targetRA,
                        Declination = targetDec,
                    },

                    LocalSiderealTime = wisesite.LocalSiderealTime.Hours,
                    HourAngle = HourAngle,
                    Altitude = Altitude,
                    Azimuth = Azimuth,
                    Slewing = Slewing,
                    Tracking = Tracking,
                    PulseGuiding = IsPulseGuiding,
                    AtPark = AtPark,
                    SecondsTillIdle = secondsTillIdle,
                    EnslavesDome = EnslavesDome,
                    Active = activityMonitor.ObservatoryIsActive(),
                    Activities = ActivityMonitor.ObservatoryActivities,
                    SlewPin = SlewPin.isOn,
                    PrimaryPins = new AxisPins
                    {
                        SetPin = WestPin.isOn || EastPin.isOn,
                        GuidePin = WestGuidePin.isOn || EastGuidePin.isOn,
                    },
                    SecondaryPins = new AxisPins
                    {
                        SetPin = NorthPin.isOn || SouthGuidePin.isOn,
                        GuidePin = NorthGuidePin.isOn || SouthGuidePin.isOn,
                    },
                    SafeAtCurrentCoordinates = SafeAtCoordinates(
                        Angle.FromHours(RightAscension),
                        Angle.FromDegrees(Declination)),
                    BypassCoordinatesSafety = BypassCoordinatesSafety,
                    Status = Status,
                    PrimaryIsMoving = AxisIsMoving(TelescopeAxes.axisPrimary),
                    SecondaryIsMoving = AxisIsMoving(TelescopeAxes.axisSecondary),
                    ShuttingDown = activityMonitor.ShuttingDown,
                };

                return JsonConvert.SerializeObject(digest);
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

    public class TelescopeDigest
    {
        public TelescopePosition Current;
        public TelescopePosition Target;
        public double HourAngle;
        public double Altitude, Azimuth;
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
    }
}
