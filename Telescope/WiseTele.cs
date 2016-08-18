using System;
using System.Collections;
using System.Collections.Generic;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM.Astrometry.NOVAS;
using ASCOM.Wise40.Common;
using ASCOM.Wise40.Hardware;
using ASCOM.DeviceInterface;

using MccDaq;
using ASCOM.Wise40;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// From the Las Campanas web site (http://www.lco.cl/telescopes-information/henrietta-swope) for the Swope telescope,
///   an identical twin to the Wise40 telescope:
/// 
/// The Swope telescope was built by the Boller and Chivens Division of the Perkin-Elmer Corp
/// The optical characteristics are discussed in detail by Bowen and Vaughen (1973, Applied Optics, 12, 1430).
/// The optical design is an f/7 Ritchey-Chrétien in which the radii of curvature of the primary and secondary are equal,
/// thereby achieving a zero Petzval sum and a flat field. Astigmatism is eliminated with a Gascoigne corrector lens.
/// This design achieves a well-corrected field about 3 degrees in diameter. However, to do this it was necessary to 
///  use a secondary one-half the diameter of the primary, thereby intercepting 25% of the incident light.
/// 
/// An f/13.5 secondary used for infrared imaging is also available through a top-end "flip".
///  
/// 
/// 2. Optical Design
///  
/// The following table gives the optical specifications of the f/7 Cassegrain configuration.
/// 
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
/// 
/// </summary>
namespace ASCOM.Wise40
{
    public class WiseTele : IDisposable, IConnectable, ISimulated
    {
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        public static string driverDescription = "Wise40 Telescope";

        private static NOVAS31 novas31;
        private static Util ascomutils;
        private static Astrometry.AstroUtils.AstroUtils astroutils;

        private List<IConnectable> connectables;
        private List<IDisposable> disposables;

        public TraceLogger traceLogger = new TraceLogger();
        public Debugger debugger = Debugger.Instance;

        private bool _connected = false;
        private bool _simulated = false;

        private List<WiseVirtualMotor> directionMotors, allMotors;
        public Dictionary<TelescopeAxes, List<WiseVirtualMotor>> axisMotors;

        public WiseHAEncoder HAEncoder;
        public WiseDecEncoder DecEncoder;

        public WisePin TrackPin;
        public WiseVirtualMotor NorthMotor, SouthMotor, EastMotor, WestMotor, TrackingMotor;

        private bool _slewing = false;
        private bool _atPark;

        private double mainMirrorDiam = 1.016;    // 40inch (meters)

        private Angle _targetRightAscension;
        private Angle _targetDeclination;

        public static readonly List<double> rates = new List<double> { Const.rateSlew, Const.rateSet, Const.rateGuide };
        public static readonly List<TelescopeAxes> axes = new List<TelescopeAxes> { TelescopeAxes.axisPrimary, TelescopeAxes.axisSecondary };

        //private long _primaryIsMoving = 0, _secondaryIsMoving = 0;
        //private const int _nAxisValues = 5;
        //private List<double> _primaryAxisValues = new List<double>(_nAxisValues);
        //private List<uint> _secondaryAxisValues = new List<uint>(_nAxisValues);

        private object _primaryValuesLock = new Object(), _secondaryValuesLock = new Object();

        public object _primaryEncoderLock = new object(), _secondaryEncoderLock = new object();

        private static WiseSite wisesite = WiseSite.Instance;

        private static Angle primarySafetyBackoff = new Angle("00h05m00.0s");
        private static Angle secondarySafetyBackoff = new Angle("00:05:00.0");

        /// <summary>
        /// Usually two or three tasks are used to perform a slew:
        /// - if the dome is slaved, a dome slewer
        /// - an axisPrimary slewer
        /// - an axisSecondary slewer
        /// 
        /// An asynchronous slew just fires the tasks.
        /// A synchronous slew waits on the whole list to complete.
        /// </summary>
        private List<Task> slewers = new List<Task>();
        private static CancellationTokenSource slewingCancellationTokenSource;
        private static CancellationToken slewingCancellationToken;

        ///// <summary>
        ///// Another background Task checks whether the telescope is acually moving
        /////  by monitoring the RightAscension (should not change if Tracking) and
        /////  Declination (should not change).
        ///// </summary>
        //private Task movementCheckerTask;
        //private static CancellationTokenSource movementCheckerCancellationTokenSource;
        //private static CancellationToken movementCheckerCancellationToken;

        private AxisMonitor primaryStatusMonitor, secondaryStatusMonitor;
        Dictionary<TelescopeAxes, AxisMonitor> axisStatusMonitors;

        public double _lastTrackingLST;

        /// <summary>
        /// These are per-rate semphores:
        ///  - Zeroed before slews
        ///  - Increased by each axis slewer when it is ready to slew at the specific rate
        ///  - Both slewers wait for the semaphore to read 2 (both axes are ready) before actuating the motors
        /// </summary>
        //ReadyToSlewFlags readyToSlew = new ReadyToSlewFlags();

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

        private Hardware.Hardware hardware = Hardware.Hardware.Instance;
        internal static string driverID = "ASCOM.Wise40.Telescope";
        internal static string debugLevelProfileName = "Debug Level";
        internal static string astrometricAccuracyProfileName = "Astrometric accuracy";
        internal static string traceStateProfileName = "Trace";
        internal static string enslaveDomeProfileName = "Enslave Dome";

        public class MovementParameters
        {
            //public Angle changeDirection;
            public Angle minimalMovement;
            public Angle stopMovement;
            public double millisecondsPerDegree;
        };

        public class Movement
        {
            public Const.AxisDirection direction;
            public double rate;
            public Angle start;
            public Angle target;            // Where we finally want to get, through all the speed rates.
            //public Angle intermediateTarget;     // An intermediate step towards the finalAngle, at one specific speed rate.
            public Angle distanceToTarget;
            public string threadName;
            public TelescopeAxes axis;
        };

        public Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>> movementParameters;
        public Dictionary<TelescopeAxes, Movement> prevMovement;         // remembers data about the previous axes movement, specifically the direction
        public Dictionary<TelescopeAxes, Movement> currMovement;         // the current axes movement        

        private static Angle altLimit = new Angle(14.0, Angle.Type.Alt); // telescope must not go below this Altitude (14 min)

        public MovementDictionary movementDict;
        private bool wasTracking;

        private SafetyMonitorTimer safetyMonitorTimer;

        public bool _enslaveDome = false;
        private DomeSlaveDriver domeSlaveDriver = DomeSlaveDriver.Instance;

        private bool _driverInitiatedSlewing = false;
        private bool _wasSlewing = false;

        /// <summary>
        /// From real-life measurements on axisPrimary, four samples, spaced at 5deg gave the following HA encoder values:
        ///     1.  5deg: 1422425
        ///     2. 10deg: 1412187
        ///     3. 15deg: 1401946
        ///     4. 20deg: 1391713
        ///     
        /// The respective deltas are:
        ///     1. d1: 10238
        ///     2. d2: 10241
        ///     3. d3: 10233
        ///     
        /// This gives approx 2047.46 ticks per 1deg, or 3600/2047.46 arcseconds per encoder tick.
        /// 
        /// </summary>

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

        public double TargetDeclination {
            get
            {
                if (_targetDeclination == null)
                    throw new ValueNotSetException("Target not set");

                traceLogger.LogMessage("TargetDeclination Get", string.Format("{0}", _targetDeclination));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Get - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
                return _targetDeclination.Degrees;
            }

            set
            {
                if (value < -90.0 || value > 90.0)
                    throw new InvalidValueException(string.Format("Invalid Declination {0}. Must be between -90 and 90", value));

                _targetDeclination = Angle.FromDegrees(value, Angle.Type.Dec);
                traceLogger.LogMessage("TargetDeclination Set", string.Format("{0}", _targetDeclination));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetDeclination Set - {0} ({1})", _targetDeclination, _targetDeclination.Degrees));
            }
        }

        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == null)
                    throw new ValueNotSetException("Target RA not set");

                Angle ret = _targetRightAscension;

                traceLogger.LogMessage("TargetRightAscension Get", string.Format("{0}", _targetRightAscension));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Get - {0} ({1})", ret, ret.Hours));

                return _targetRightAscension.Hours;
            }

            set
            {
                if (value < 0.0 || value > 24.0)
                    throw new ASCOM.InvalidValueException(string.Format("Invalid RightAscension {0}. Must be between 0 to 24", value));

                _targetRightAscension = Angle.FromHours(value, Angle.Type.RA);
                traceLogger.LogMessage("TargetRightAscension Set", string.Format("{0}", _targetRightAscension));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM,
                    string.Format("TargetRightAscension Set - {0} ({1})",
                    _targetRightAscension,
                    _targetRightAscension.Hours));
            }
        }

        public double ApertureDiameter
        {
            get
            {
                return mainMirrorDiam;
            }
        }

        public double ApertureArea
        {
            get
            {
                return Math.PI * Math.Pow(ApertureDiameter, 2);
            }
        }

        public bool doesRefraction {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("DoesRefraction Get", ret.ToString());
                return ret;
            }

            set
            {
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction");
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
                traceLogger.LogMessage("Connected Get", _connected.ToString());
                return _connected;
            }

            set
            {
                traceLogger.LogMessage("Connected Set", value.ToString());
                if (value == _connected)
                    return;

                if (_enslaveDome && connectables.Find(x => x.Equals(domeSlaveDriver)) == null)
                    connectables.Add(domeSlaveDriver);

                foreach (var connectable in connectables)
                {
                    connectable.Connect(value);
                }
                _connected = value;
                //if (_connected)
                //{
                //    movementCheckerCancellationTokenSource = new CancellationTokenSource();
                //    movementCheckerCancellationToken = movementCheckerCancellationTokenSource.Token;

                //    try
                //    {
                //        movementCheckerTask = Task.Run(() =>
                //                                {
                //                                    Thread.CurrentThread.Name = "AxisMovementChecker";
                //                                    AxisMovementChecker();
                //                                }, movementCheckerCancellationToken);
                //    }
                //    catch (OperationCanceledException) { }
                //}
                //else
                //{
                //    if (movementCheckerTask != null)
                //    {
                //        movementCheckerCancellationTokenSource.Cancel();
                //    }
                //}
            }
        }

        //private void CheckAxisMovement(object StateObject)
        //{
        //    foreach (TelescopeAxes axis in axes)
        //    {
        //        if (axis == TelescopeAxes.axisPrimary)
        //        {
        //            var epsilon = new Angle("00:00:00.5").Degrees;
        //            lock (_primaryValuesLock)
        //            {
        //                _primaryAxisValues.Enqueue(RightAscension);

        //                if (_primaryAxisValues.Count == _nAxisValues)
        //                {
        //                    var deltas = new List<double>();
        //                    for (var i = 1; i < _primaryAxisValues.Count; i++)
        //                        deltas.Add(Math.Abs(_primaryAxisValues[i]  - _primaryAxisValues[i - 1]));

        //                    double max = deltas.Max();
        //                    Interlocked.Exchange(ref _primaryIsMoving, (max <= epsilon) ? 0 : 1);
        //                    //debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
        //                    //    "CheckAxisMovement: primaryAxis: max: {0}, _primaryIsMoving: {1}, epsilon: {2}",
        //                    //    max, Interlocked.Read(ref _primaryIsMoving), epsilon);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            uint epsilon = 1;
        //            lock (_secondaryValuesLock)
        //            {
        //                if (_secondaryAxisValues.Count == _nAxisValues)
        //                    _secondaryAxisValues.Remove(_secondaryAxisValues.ElementAt(0));
        //                _secondaryAxisValues.Add(DecEncoder.Value);

        //                if (_secondaryAxisValues.Count == _nAxisValues)
        //                {
        //                    var deltas = new List<uint>();
        //                    for (var i = 1; i < _secondaryAxisValues.Count; i++)
        //                        deltas.Add((uint)Math.Abs(_secondaryAxisValues[i] - _secondaryAxisValues[i - 1]));

        //                    Interlocked.Exchange(ref _secondaryIsMoving, (deltas.Max() <= epsilon) ? 0 : 1);
        //                }
        //            }
        //        }
        //    }
        //}

        //public void AxisMovementChecker()
        //{
        //    TimerCallback axisMovementTimerCallback = new TimerCallback(CheckAxisMovement);
        //    System.Threading.Timer movementCheckerTimer = new System.Threading.Timer(axisMovementTimerCallback);
        //    movementCheckerTimer.Change(100, 100);
        //}

        private static readonly WiseTele instance = new WiseTele(); // Singleton
        private static bool _initialized = false;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WiseTele()
        {
        }

        public WiseTele()
        {
        }

        public static WiseTele Instance
        {
            get
            {
                return instance;
            }
        }

        public void init()
        {
            if (_initialized)
                return;

            WisePin SlewPin = null;
            WisePin NorthGuidePin = null, SouthGuidePin = null, EastGuidePin = null, WestGuidePin = null;   // Guide motor activation pins
            WisePin NorthPin = null, SouthPin = null, EastPin = null, WestPin = null;                       // Set and Slew motors activation pinsisInitialized = true;

            ReadProfile();
            debugger.init();
            traceLogger = new TraceLogger("", "Tele");
            novas31 = new NOVAS31();
            ascomutils = new Util();
            astroutils = new Astrometry.AstroUtils.AstroUtils();
            hardware.init();
            wisesite.init();

            #region MotorDefinitions
            //
            // Define motors-related hardware (pins and encoders)
            //
            try
            {
                instance.connectables = new List<IConnectable>();
                instance.disposables = new List<IDisposable>();

                NorthPin = new WisePin("TeleNorth", hardware.teleboard, DigitalPortType.FirstPortCL, 0, DigitalPortDirection.DigitalOut);
                EastPin = new WisePin("TeleEast", hardware.teleboard, DigitalPortType.FirstPortCL, 1, DigitalPortDirection.DigitalOut);
                WestPin = new WisePin("TeleWest", hardware.teleboard, DigitalPortType.FirstPortCL, 2, DigitalPortDirection.DigitalOut);
                SouthPin = new WisePin("TeleSouth", hardware.teleboard, DigitalPortType.FirstPortCL, 3, DigitalPortDirection.DigitalOut);

                SlewPin = new WisePin("TeleSlew", hardware.teleboard, DigitalPortType.FirstPortCH, 0, DigitalPortDirection.DigitalOut);
                TrackPin = new WisePin("TeleTrack", hardware.teleboard, DigitalPortType.FirstPortCH, 2, DigitalPortDirection.DigitalOut);

                NorthGuidePin = new WisePin("TeleNorthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 0, DigitalPortDirection.DigitalOut);
                EastGuidePin = new WisePin("TeleEastGuide", hardware.teleboard, DigitalPortType.FirstPortB, 1, DigitalPortDirection.DigitalOut);
                WestGuidePin = new WisePin("TeleWestGuide", hardware.teleboard, DigitalPortType.FirstPortB, 2, DigitalPortDirection.DigitalOut);
                SouthGuidePin = new WisePin("TeleSouthGuide", hardware.teleboard, DigitalPortType.FirstPortB, 3, DigitalPortDirection.DigitalOut);

                instance.HAEncoder = new WiseHAEncoder("TeleHAEncoder");
                instance.DecEncoder = new WiseDecEncoder("TeleDecEncoder");
            }
            catch (WiseException e)
            {
               debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "WiseTele constructor caught: {0}.", e.Message);
            }

            //
            // Define motors-related software interfaces (WiseVirtualMotor)
            //
            instance.NorthMotor = new WiseVirtualMotor("NorthMotor", NorthPin, NorthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing, new List<object> { instance.DecEncoder });

            instance.SouthMotor = new WiseVirtualMotor("SouthMotor", SouthPin, SouthGuidePin, SlewPin,
                TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing, new List<object> { instance.DecEncoder });

            instance.WestMotor = new WiseVirtualMotor("WestMotor", WestPin, WestGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });

            instance.EastMotor = new WiseVirtualMotor("EastMotor", EastPin, EastGuidePin, SlewPin,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing, new List<object> { instance.HAEncoder });

            instance.TrackingMotor = new WiseVirtualMotor("TrackMotor", TrackPin, null, null,
                TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing, new List<object> { instance.HAEncoder });

            //
            // Define motor groups
            //
            instance.axisMotors = new Dictionary<TelescopeAxes, List<WiseVirtualMotor>>();
            instance.axisMotors[TelescopeAxes.axisPrimary] = new List<WiseVirtualMotor> { instance.EastMotor, instance.WestMotor };
            instance.axisMotors[TelescopeAxes.axisSecondary] = new List<WiseVirtualMotor> { instance.NorthMotor, instance.SouthMotor };

            instance.directionMotors = new List<WiseVirtualMotor>();
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisPrimary]);
            instance.directionMotors.AddRange(instance.axisMotors[TelescopeAxes.axisSecondary]);

            instance.allMotors = new List<WiseVirtualMotor>();
            instance.allMotors.AddRange(instance.directionMotors);
            instance.allMotors.Add(TrackingMotor);

            List<ISimulated> hardware_elements = new List<ISimulated>();
            hardware_elements.AddRange(instance.allMotors);
            hardware_elements.Add(instance.HAEncoder);
            hardware_elements.Add(instance.DecEncoder);
            foreach (ISimulated s in hardware_elements)
            {
                if (s.Simulated)
                {
                    Simulated = true;
                    break;
                }
            }
            #endregion

            TimerCallback safetyMonitorTimerCallback = new TimerCallback(DoCheckSafety);
            safetyMonitorTimer = new SafetyMonitorTimer(safetyMonitorTimerCallback, 100, 100);

            #region MovementParameters
            //
            // Initialize movement parameters.
            // These should be real-world, measured values.
            //
            instance.movementParameters = new Dictionary<TelescopeAxes, Dictionary<double, MovementParameters>>();

            instance.movementParameters[TelescopeAxes.axisPrimary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSlew] = new MovementParameters()
            {
                //changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:30.0")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("01:00:00.0")),
                stopMovement = new Angle("00h19m00.0s"),
                millisecondsPerDegree = 500.0,      // 2deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateSet] = new MovementParameters()
            {            
                //changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:20.0")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:05.0")),
                stopMovement = new Angle("00h00m01.0s"),
                millisecondsPerDegree = 60000.0,    // 1min/sec
            };

            instance.movementParameters[TelescopeAxes.axisPrimary][Const.rateGuide] = new MovementParameters()
            {
                //changeDirection = Angle.FromHours(Angle.Deg2Hours("00:00:00.5")),
                minimalMovement = Angle.FromHours(Angle.Deg2Hours("00:00:01.0")),
                stopMovement = new Angle("00h00m00.1s"),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary] = new Dictionary<double, MovementParameters>();
            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSlew] = new MovementParameters()
            {
                //changeDirection = new Angle("00:00:30.0"),
                minimalMovement = new Angle("01:00:00.0"),
                stopMovement = new Angle("00:08:00.0"),
                millisecondsPerDegree = 500.0,      // 2 deg/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateSet] = new MovementParameters()
            {
                //changeDirection = new Angle("00:00:30.0"),
                minimalMovement = new Angle("00:00:05.0"),
                stopMovement = new Angle("00:00:01.0"),
                millisecondsPerDegree = 60000.0,    // 1 min/sec
            };

            instance.movementParameters[TelescopeAxes.axisSecondary][Const.rateGuide] = new MovementParameters()
            {
                //changeDirection = new Angle("00:00:00.5"),
                minimalMovement = new Angle("00:00:01.0"),
                stopMovement = Angle.FromDegrees(Angle.Hours2Deg("00h00m00.5s")),
                millisecondsPerDegree = 3600000.0,  // 1 sec/sec
            };
            #endregion

            // prevMovement remembers the previous movement, so we can detect change-of-direction
            instance.prevMovement = new Dictionary<TelescopeAxes, Movement>();
            instance.prevMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            instance.prevMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };

            // currMovement contains the current telescope-movement parameters
            instance.currMovement = new Dictionary<TelescopeAxes, Movement>();
            instance.currMovement[TelescopeAxes.axisPrimary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };
            instance.currMovement[TelescopeAxes.axisSecondary] = new Movement() { direction = Const.AxisDirection.None, rate = Const.rateStopped };


            instance.movementDict = new MovementDictionary();
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { WestMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisPrimary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { EastMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Increasing)] =
                new MovementWorker(new WiseVirtualMotor[] { NorthMotor });
            instance.movementDict[new MovementSpecifier(TelescopeAxes.axisSecondary, Const.AxisDirection.Decreasing)] =
                new MovementWorker(new WiseVirtualMotor[] { SouthMotor });

            primaryStatusMonitor = new AxisMonitor(TelescopeAxes.axisPrimary);
            secondaryStatusMonitor = new AxisMonitor(TelescopeAxes.axisSecondary);
            axisStatusMonitors = new Dictionary<TelescopeAxes, AxisMonitor>()
            {
                { TelescopeAxes.axisPrimary, primaryStatusMonitor },
                { TelescopeAxes.axisSecondary, secondaryStatusMonitor },
            };

            instance.connectables.Add(instance.NorthMotor);
            instance.connectables.Add(instance.EastMotor);
            instance.connectables.Add(instance.WestMotor);
            instance.connectables.Add(instance.SouthMotor);
            instance.connectables.Add(instance.TrackingMotor);
            instance.connectables.Add(instance.HAEncoder);
            instance.connectables.Add(instance.DecEncoder);
            instance.connectables.Add(instance.primaryStatusMonitor);
            instance.connectables.Add(instance.secondaryStatusMonitor);

            instance.disposables.Add(instance.NorthMotor);
            instance.disposables.Add(instance.EastMotor);
            instance.disposables.Add(instance.WestMotor);
            instance.disposables.Add(instance.SouthMotor);
            instance.disposables.Add(instance.TrackingMotor);
            instance.disposables.Add(instance.HAEncoder);
            instance.disposables.Add(instance.DecEncoder);

            SlewPin.SetOff();
            instance.TrackingMotor.SetOff();
            instance.NorthMotor.SetOff();
            instance.EastMotor.SetOff();
            instance.WestMotor.SetOff();
            instance.SouthMotor.SetOff();

            instance.domeSlaveDriver.init();
            instance.connectables.Add(instance.domeSlaveDriver);

            _initialized = true;
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "WiseTele init() done.");
        }

        public double FocalLength
        {
            get
            {
                double ret = 7.112;  // from Las Campanas 40" (meters)

                traceLogger.LogMessage("FocalLength Get", ret.ToString());
                return ret;   
            }
        }

        public void AbortSlew()
        {
            if (AtPark)
                throw new InvalidOperationException("Cannot AbortSlew while AtPark");

            if (!_driverInitiatedSlewing)
                return;

            slewingCancellationTokenSource.Cancel();
            traceLogger.LogMessage("AbortSlew", "");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "AbortSlew");
        }

        public double RightAscension
        {
            get
            {
                var ret = HAEncoder.RightAscension;

                traceLogger.LogMessage("RightAscension", string.Format("Get - {0} ({1})", ret, ret.Hours));
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("RightAscension Get - {0} ({1})", ret, ret.Hours));

                return ret.Hours;
            }
        }

        public double HourAngle
        {
            get
            {
                return astroutils.ConditionHA(HAEncoder.Angle.Hours);
            }
        }

        public double Declination
        {
            get
            {
                var ret = Angle.FromDegrees(DecEncoder.Declination);

                traceLogger.LogMessage("Declination", string.Format("Get - {0} ({1})", ret, ret.Degrees));
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Declination Get - {0} ({1})", ret, ret.Degrees));
                return ret.Degrees;
            }
        }

        public double Azimuth
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd= 0;

                wisesite.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                traceLogger.LogMessage("Azimuth Get", az.ToString());
                return az;
            }
        }

        public double Altitude
        {
            get
            {
                double rar = 0, decr = 0, az = 0, zd = 0, alt;

                wisesite.prepareRefractionData();
                novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                    wisesite.astrometricAccuracy,
                    0, 0,
                    wisesite.onSurface,
                    RightAscension, Declination,
                    wisesite.refractionOption,
                    ref zd, ref az, ref rar, ref decr);

                alt = 90.0 - zd;
                traceLogger.LogMessage("Altitude Get", alt.ToString());
                return alt;
            }
        }

        public bool Tracking
        {
            get
            {
                bool ret = TrackingMotor.isOn;

                traceLogger.LogMessage("Tracking", "Get - " + ret.ToString());
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Get - {0}", ret));
                return ret;
            }

            set
            {
                traceLogger.LogMessage("Tracking Set", value.ToString());
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Tracking Set - {0}", value));

                if (value)
                {
                    _lastTrackingLST = wisesite.LocalSiderealTime.Hours;

                    if (TrackingMotor.isOff)
                        TrackingMotor.SetOn(Const.rateTrack);
                    if (!safetyMonitorTimer.isOn)
                        safetyMonitorTimer.SetOn();
                }
                else
                {
                    if (TrackingMotor.isOn)
                        TrackingMotor.SetOff();

                    bool active = false;
                    foreach (WiseVirtualMotor m in directionMotors)
                    {
                        if (m.isOn)
                        {
                            active = true;
                            break;
                        }
                        if (active && !safetyMonitorTimer.isOn)
                            safetyMonitorTimer.SetOn();
                    }
                }
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                var rates = DriveRates.driveSidereal;

                traceLogger.LogMessage("TrackingRate Get - ", rates.ToString());
                return rates;
            }

            set
            {
                traceLogger.LogMessage("TrackingRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
            }
        }

        public void Stop()
        {
            if (slewingCancellationTokenSource != null)
            {
                try
                {
                    slewingCancellationTokenSource.Cancel();
                } catch (AggregateException ax)
                {
                    ax.Handle((ex) =>
                    {
                        debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                            "Stop: got {0}", ex.Message);
                        if (ex is ObjectDisposedException)
                            return true;
                        return false;
                    });                        
                }
            }                

            foreach (WiseVirtualMotor motor in directionMotors)
                if (motor.isOn)
                    motor.SetOff();

            safetyMonitorTimer.SetOff();
            
            Slewing = false;
        }

        public bool AxisIsMoving(TelescopeAxes axis)
        {
            bool ret = false;

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    ret = primaryStatusMonitor.IsMoving;
                    break;
                case TelescopeAxes.axisSecondary:
                    ret = secondaryStatusMonitor.IsMoving;
                    break;
            }

            return ret;
        }

        public bool Moving
        {
            get
            {
                foreach (WiseVirtualMotor m in instance.directionMotors)
                    if (m.isOn)
                        return true;
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
                bool ret = false;

                if (_slewing)
                    ret = true;
                else
                {
                    foreach (var slewer in slewers)
                        if (slewer.Status == TaskStatus.Created || 
                            slewer.Status == TaskStatus.WaitingToRun || 
                            slewer.Status == TaskStatus.Running)
                        {
                            ret = true;
                            break;
                        }
                }

                traceLogger.LogMessage("Slewing Get", ret.ToString());
                debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("Slewing Get - {0}", ret));

                if (_wasSlewing == true && ret == false)
                {
                    slewingCancellationTokenSource = null;
                    _driverInitiatedSlewing = false;
                }

                _wasSlewing = ret;

                return ret;
            }

            set
            {
                _slewing = value;
            }

        }

        public double DeclinationRate
        {
            get
            {
                double decRate = 0.0;

                traceLogger.LogMessage("DeclinationRate", "Get - " + decRate.ToString());
                return decRate;
            }

            set
            {
                traceLogger.LogMessage("DeclinationRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            traceLogger.LogMessage("MoveAxis", string.Format("MoveAxis({0}, {1})", Axis, Rate));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("MoveAxis({0}, {1})", Axis, Rate));

            Const.AxisDirection direction = (Rate == Const.rateStopped) ? Const.AxisDirection.None : 
                (Rate < 0.0) ? Const.AxisDirection.Decreasing : Const.AxisDirection.Increasing;

            _driverInitiatedSlewing = true;

            try
            {
                _moveAxis(Axis, Rate, direction, true);
            }
            catch (Exception e)
            {
                _driverInitiatedSlewing = false;
                throw e;
            }
        }

        private void StopAxis(TelescopeAxes axis)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): called", axis);

            // Stop any motors that may be On
            foreach (WiseVirtualMotor m in instance.axisMotors[axis])
                if (m.isOn)
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "StopAxis({0}):  {1} was on, stopping it.", axis, m.name);
                    m.SetOff();
                }

            // Restore tracking
            if (wasTracking)
                Tracking = true;
            
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "StopAxis({0}): done.", axis);

            _slewing = false;
        }

        private void _moveAxis(
            TelescopeAxes Axis,
            double Rate,
            Const.AxisDirection direction = Const.AxisDirection.None,
            bool stopTracking = false)
        {
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): called", Axis, RateName(Rate));

            MovementWorker mover = null;
            TelescopeAxes thisAxis = Axis;
            TelescopeAxes otherAxis = (thisAxis == TelescopeAxes.axisPrimary) ? TelescopeAxes.axisSecondary : TelescopeAxes.axisPrimary;

            if (thisAxis == TelescopeAxes.axisTertiary)
                throw new InvalidValueException("Cannot move in axisTertiary");

            if (AtPark)
            {
                Instance.currMovement[Axis].rate = Const.rateStopped;
                throw new InvalidValueException("Cannot MoveAxis while AtPark");
            }

            double absRate = Math.Abs(Rate);
            if (! ((absRate == Const.rateSlew) || (absRate == Const.rateSet) || (absRate == Const.rateGuide)))
                throw new InvalidValueException(string.Format("_moveAxis({0}, {1}): Invalid rate.", Axis, Rate));

            if (! axisStatusMonitors[Axis].CanMoveAtRate(absRate))
            {
                string msg = string.Format("Cannot _moveAxis({0}, {1}) ({2}) while {3} is moving at {4}",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], otherAxis, RateName(currMovement[otherAxis].rate));

                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, msg);
                throw new InvalidValueException(msg);
            }

            try
                {
                mover = movementDict[new MovementSpecifier(Axis, direction)];
            } catch(Exception e) {
                throw new InvalidValueException(string.Format("Don't know how to _moveAxis({0}, {1}) (no mover) ({2}) [{3}]",
                    Axis, RateName(Rate), axisDirectionName[Axis][direction], e.Message));
            }

            debugger.WriteLine(Debugger.DebugLevel.DebugAxes, "_moveAxis({0}, {1}): direction: {2}, stopTracking: {3}",
                Axis, RateName(Rate), axisDirectionName[Axis][direction], stopTracking);
            wasTracking = Tracking;
            if (stopTracking)
                Tracking = false;

            _slewing = true;

            foreach (WiseVirtualMotor m in mover.motors) {
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes, 
                    "_moveAxis({0}, {1}): starting {2}", Axis, RateName(Rate), m.name);
                m.SetOn(Rate);
            }

            if (Moving && !safetyMonitorTimer.isOn)
                safetyMonitorTimer.SetOn();
        }

        public bool IsPulseGuiding
        {
            get
            {
                throw new PropertyNotImplementedException("IsPulseGuiding");
            }
        }

        public void SlewToTargetAsync()
        {
            if (_targetRightAscension == null)
                throw new ValueNotSetException("Target RA not set");
            if (_targetDeclination == null)
                throw new ValueNotSetException("Target Dec not set");

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToTargetAsync", string.Format("Started: ra: {0}, dec: {1}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTargetAsync({0}, {1})", ra, dec));


            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToTargetAsync while NOT Tracking");


            if (! SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToTargetAsync({0}, {1})", ra, dec));

            _driverInitiatedSlewing = true;

            _slewToCoordinatesAsync(_targetRightAscension, _targetDeclination);
        }

        /// <summary>
        /// Checks if we're safe at a given position
        ///  
        /// If (takeAction == true) then take the apropriate recovery action
        /// </summary>
        /// <param name="ra">RightAscension of the checked position</param>
        /// <param name="dec">Declination of the checked position</param>
        /// <param name="takeAction">Take recovery actions or not</param>
        public bool SafeAtCoordinates(Angle ra, Angle dec, bool takeAction = false)
        {
            double rar = 0, decr = 0, az = 0, zd = 0;
            Angle alt;

            wisesite.prepareRefractionData();
            novas31.Equ2Hor(astroutils.JulianDateUT1(0), 0,
                wisesite.astrometricAccuracy,
                0, 0,
                wisesite.onSurface,
                ra.Hours, dec.Degrees,
                wisesite.refractionOption,
                ref zd, ref az, ref rar, ref decr);

            alt = Angle.FromDegrees(90.0 - zd);
            if (alt < altLimit)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                    string.Format("Not safe at ra: {0}, dec: {1} (alt: {2} < altLimit: {3})",
                    ra,
                    dec,
                    alt,
                    altLimit));

                if (takeAction)
                {
                    Angle safeRa = ra, safeDec = dec;
                    bool wasNotTracking = !Tracking;

                    if (WestMotor.isOn)
                        safeRa += primarySafetyBackoff;
                    else if (EastMotor.isOn)
                        safeRa -= primarySafetyBackoff;

                    if (SouthMotor.isOn)
                        safeDec += secondarySafetyBackoff;
                    else if (NorthMotor.isOn)
                        safeDec -= secondarySafetyBackoff;

                    Stop();

                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        "SafeAtCoordinates: slewing to ({0}, {1})", safeRa, safeDec);
                    #endregion

                    if (wasNotTracking)
                        Tracking = true;
                    SlewToCoordinates(safeRa.Hours, safeDec.Degrees);
                    if (wasNotTracking)
                        Tracking = false;
                }
                return false;
            }
            
            return true;
        }

        public bool Simulated
        {
            get
            {
                return _simulated;
            }
            set
            {
                _simulated = value;
            }
        }

        public bool AtPark
        {
            get
            {
                bool ret = _atPark;

                traceLogger.LogMessage("AtPark", "Get - " + ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtPark Get - {0}", ret));

                return ret;
            }

            set
            {
                _atPark = value;
            }
        }

        public void Park()
        {
            traceLogger.LogMessage("Park", "");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Park");

            if (AtPark)
                return;

            _slewToCoordinatesSync(wisesite.LocalSiderealTime, wisesite.Latitude);
            AtPark = true;
        }

        private void _slewToCoordinatesSync(Angle RightAscension, Angle Declination)
        {
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), called.", RightAscension, Declination);
            #endregion debug
            _slewToCoordinatesAsync(RightAscension, Declination);
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), waiting ...", RightAscension, Declination);
            #endregion debug
            Task.WaitAll(slewers.ToArray());
            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "_slewToCoordinatesSync: ({0}, {1}), done.", RightAscension, Declination);
            #endregion debug
        }

        private enum SlewerStatus { Undefined, CloseEnough, ChangedDirection, Canceled };

        private void Slewer(TelescopeAxes axis, Angle targetAngle)
        {
            SlewerStatus status;
            instance.currMovement[axis] = new Movement() { rate = Const.rateStopped };
            Movement cm = instance.currMovement[axis];

            cm.threadName = Thread.CurrentThread.Name;
            cm.target = targetAngle;
            do
            {
                switch (status = SlewCloser(axis))
                {
                    case SlewerStatus.CloseEnough:
                    case SlewerStatus.Canceled:
                        return;
                    case SlewerStatus.ChangedDirection:
                        break;
                }
            }
            while (true);
        }

        private void StopAxisAndWaitForHalt(TelescopeAxes axis)
        {
            Movement cm = instance.currMovement[axis];
            StopAxis(axis);
            cm.rate = Const.rateStopped;

            #region debug
            Angle a = (cm.axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(instance.RightAscension) :
                Angle.FromDegrees(instance.Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0} at {1}: {2} waiting for {3} to stop moving ...",
                cm.threadName, RateName(cm.rate), a, axis);
            #endregion debug

            while (AxisIsMoving(axis))
            {
                Thread.Sleep(500);
            }
            #region debug
            a = (axis == TelescopeAxes.axisPrimary) ?
                Angle.FromHours(instance.RightAscension) :
                Angle.FromDegrees(instance.Declination);
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0} at {1}: {2} {3} has stopped moving.",
                cm.threadName, RateName(cm.rate), a, axis);
            #endregion debug
        }

        private SlewerStatus SlewCloser(TelescopeAxes axis)
        {
            Movement cm = Instance.currMovement[axis];
            Angle currPosition = new Angle(0.0);
            MovementParameters mp;
            AxisMonitor axisStatus = (axis == TelescopeAxes.axisPrimary) ?
                primaryStatusMonitor : secondaryStatusMonitor;

            #region debug
            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                "{0}: cm.finalTarget: {1}", cm.threadName, cm.target);
            #endregion debug

            try
            {
                foreach (var rate in rates)
                {
                    mp = movementParameters[axis][rate];

                    slewingCancellationToken.ThrowIfCancellationRequested();

                    //
                    // Both axes can now move at the current rate
                    //
                    cm.start = (axis == TelescopeAxes.axisPrimary) ?
                        Angle.FromHours(RightAscension, Angle.Type.RA) :
                        Angle.FromDegrees(Declination, Angle.Type.Dec);

                    var shortest = cm.start.ShortestDistance(cm.target);
                    cm.distanceToTarget = shortest.angle;
                    cm.direction = shortest.direction;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: cm.start: {2}, cm.distanceToTarget: {3}, cm.direction: {4}",
                        cm.threadName, RateName(rate), cm.start, cm.distanceToTarget, cm.direction);
                    #endregion debug

                    Angle minimalMovementAngle = mp.minimalMovement + mp.stopMovement;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: minimalMovementAngle: {2}",
                        cm.threadName, RateName(rate), minimalMovementAngle);
                    #endregion debug

                    if (cm.distanceToTarget < minimalMovementAngle)
                    {
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                            "{0} at {1}: Not moving, too short ({2} < {3}, {4} < {5})",
                            cm.threadName, RateName(rate),
                            cm.distanceToTarget, minimalMovementAngle,
                            cm.distanceToTarget.Degrees, minimalMovementAngle.Degrees);
                        #endregion debug
                        continue;   // this rate is no good, try thr next one
                    }

                    //
                    // Wait till this axis is allowed to move at the current rate.
                    //
                    while (!axisStatus.CanMoveAtRate(rate))
                    {
                        const int syncMillis = 500;
                        #region debug
                        debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                           "{0} at {1}: waiting {2} millis for the other axis ...",
                           cm.threadName, RateName(rate), syncMillis);
                        #endregion debug

                        slewingCancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(syncMillis);
                    }

                    //
                    // Set the axis in motion at the current rate ...
                    //
                    cm.rate = rate;
                    #region debug
                    debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                        "{0} at {1}: cm.start: {2}, cm.distanceToFinalTarget: {3}, cm.direction: {4}",
                        cm.threadName, RateName(cm.rate), cm.start, cm.distanceToTarget, cm.direction);
                    #endregion debug

                    slewingCancellationToken.ThrowIfCancellationRequested();
                    _moveAxis(axis, cm.rate, cm.direction, false);

                    //
                    // ... and wait for it to arrive at target
                    //
                    ShortestDistanceResult remainingDistance = null;
                    ShortestDistanceResult prevRemainingDistance = null;
                    bool arrived = false;

                    while (!arrived)
                    {
                        const int waitMillis = 10;    // TODO: make it configurable or constant

                        slewingCancellationToken.ThrowIfCancellationRequested();

                        currPosition = (axis == TelescopeAxes.axisPrimary) ?
                            Angle.FromHours(instance.RightAscension, Angle.Type.RA) :
                            Angle.FromDegrees(instance.Declination, Angle.Type.Dec);
                        remainingDistance = currPosition.ShortestDistance(cm.target);

                        if (remainingDistance.angle <= mp.stopMovement)
                        {
                            arrived = true;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                "{0} at {1}: stopping at {2}, ({3} from cm.target: {4}): CloseEnough: remainingDistance.angle {5} <= mp.stopMovement {6}",
                                cm.threadName, RateName(cm.rate), currPosition,
                                remainingDistance.angle, cm.target,
                                remainingDistance.angle, mp.stopMovement);
                            #endregion
                            StopAxisAndWaitForHalt(axis);
                            break;
                        }
                        else if (cm.direction != remainingDistance.direction)
                        {
                            arrived = true;
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                "{0} at {1}: at {2}, changing direction, {3} => {4}",
                                cm.threadName, RateName(cm.rate), currPosition,
                                cm.direction.ToString(), remainingDistance.direction.ToString()
                                );
                            #endregion
                            StopAxisAndWaitForHalt(axis);
                            return SlewerStatus.ChangedDirection;
                        }
                        else
                        {
                            // Not there yet, continue waiting
                            #region debug
                            debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                                    "{0} at {1}: still moving, currPosition: {2} ==> finalTarget: {3}, remaining (Angle: {4}, degrees: {5}, direction: {6}), stopMovement: ({7}, {8}), sleeping {9} millis ...",
                                    cm.threadName, RateName(cm.rate), currPosition,
                                    cm.target,
                                    remainingDistance.angle, remainingDistance.angle.Degrees, remainingDistance.direction,
                                    mp.stopMovement, mp.stopMovement.Degrees,
                                    waitMillis);
                            #endregion debug
                            prevRemainingDistance = remainingDistance;
                            slewingCancellationToken.ThrowIfCancellationRequested();
                            Thread.Sleep(waitMillis);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                #region debug
                debugger.WriteLine(Debugger.DebugLevel.DebugAxes,
                    "{0} at {1}: Slew cancelled at {2}", cm.threadName, RateName(cm.rate), currPosition);
                #endregion debug
                StopAxisAndWaitForHalt(axis);
                return SlewerStatus.Canceled;
            }
            return SlewerStatus.CloseEnough;
        }

        private void _slewToCoordinatesAsync(Angle RightAscension, Angle Declination)
        {
            slewers.Clear();
            slewingCancellationTokenSource = new CancellationTokenSource();
            slewingCancellationToken = slewingCancellationTokenSource.Token;
            Slewing = true;

            try
            {
                if (instance._enslaveDome)
                    slewers.Add(Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = "domeSlewer";
                        try
                        {
                            domeSlaveDriver.SlewStartAsync(RightAscension, Declination);
                        }
                        catch (OperationCanceledException)
                        {
                            domeSlaveDriver.AbortSlew();
                        }
                    }, slewingCancellationToken));

                slewers.Add(Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "primarySlewer";
                    Slewer(TelescopeAxes.axisPrimary, RightAscension);
                }, slewingCancellationToken));

                slewers.Add(Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "secondarySlewer";
                    Slewer(TelescopeAxes.axisSecondary, Declination);
                }, slewingCancellationToken));

            } catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    debugger.WriteLine(Debugger.DebugLevel.DebugLogic,
                        "_slewToCoordinatesAsync: Caught {0}", ex.Message);
                    return false;
                });
            }
        }

        public void SlewToCoordinates(double RightAscension, double Declination)
        {
            Angle ra = Angle.FromHours(RightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(Declination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToCoordinates", string.Format("ra: {0}, dec: {0}", ra, dec));
            debugger.WriteLine(Debugger.DebugLevel.DebugASCOM, string.Format("SlewToCoordinates - {0}, {1}", ra, dec));

            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");


            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToCoordinates({0}, {1})", ra, dec));

            try
            {
                _slewToCoordinatesSync(ra, dec);
            }
            catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions,
                    "SlewToCoordinates: _slewToCoordinatesSync({0}, {1}) threw exception: {2}",
                    ra, dec, e.Message);
                _slewing = false;
            }
        }

        public void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            TargetRightAscension = RightAscension;
            TargetDeclination = Declination;

            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            debugger.WriteLine(Debugger.DebugLevel.DebugDevice, "SlewToCoordinatesAsync({0}, {1})", ra, dec);

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToCoordinatesAsync({0}, {1})", ra, dec));

            try
            {
                _slewToCoordinatesAsync(ra, dec);
            } catch (Exception e)
            {
                debugger.WriteLine(Debugger.DebugLevel.DebugExceptions, "SlewToCoordinatesAsync({0}, {1}) caught exception: {2}",
                    RightAscension, Declination, e.Message);
                _slewing = false;
            }
        }

        private void DoCheckSafety(object StateObject)
        {
            SafeAtCoordinates(Angle.FromHours(RightAscension, Angle.Type.RA), Angle.FromDegrees(Declination, Angle.Type.Dec), true);
        }

        public void Unpark()
        {
            if (AtPark)
                AtPark = false;

            traceLogger.LogMessage("Unpark", "Done");
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, "Unpark");
        }

        public DateTime UTCDate
        {
            get
            {
                DateTime utcDate = DateTime.UtcNow;
                traceLogger.LogMessage("UTCDate Get - ", utcDate.ToString());
                return utcDate;
            }

            set
            {
                traceLogger.LogMessage("UTCDate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                traceLogger.LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    traceLogger.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        public void SyncToTarget()
        {
            traceLogger.LogMessage("SyncToTarget", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToTarget");
        }

        public string Description
        {
            get
            {
                var ret = "Wise40 Telescope";
                traceLogger.LogMessage("Description Get", ret);
                return ret;
            }
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                AlignmentModes mode = AlignmentModes.algPolar;

                traceLogger.LogMessage("AlignmentMode Get", mode.ToString());
                return mode;
            }
        }

        public double SiderealTime
        {
            get
            {
                double ret = wisesite.LocalSiderealTime.Hours;

                traceLogger.LogMessage("SiderealTime", "Get - " + ret.ToString());
                return ret;
            }
        }

        public double SiteElevation
        {
            get
            {
                double elevation = wisesite.Elevation;

                traceLogger.LogMessage("SiteElevation Get", elevation.ToString());
                return elevation;
            }

            set
            {
                traceLogger.LogMessage("SiteElevation Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                double latitude = wisesite.Latitude.Degrees;

                traceLogger.LogMessage("SiteLatitude Get", latitude.ToString());
                return latitude;
            }
            set
            {
                traceLogger.LogMessage("SiteLatitude Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteLatitude", true);
            }
        }

        /// <summary>
        /// Site Longitude in degrees as per ASCOM.DriverAccess
        /// </summary>
        public double SiteLongitude
        {
            get
            {
                double longitude = wisesite.Longitude.Degrees;

                traceLogger.LogMessage("SiteLongitude Get", longitude.ToString());
                return longitude;
            }
            set
            {
                traceLogger.LogMessage("SiteLongitude Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteLongitude", true);
            }
        }

        public void SlewToTarget()
        {
            Angle ra = Angle.FromHours(TargetRightAscension, Angle.Type.RA);
            Angle dec = Angle.FromDegrees(TargetDeclination, Angle.Type.Dec);

            traceLogger.LogMessage("SlewToTarget", string.Format("ra: {0}, dec: {0}", ra, dec));
            debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("SlewToTarget - {0}, {1}", ra, dec));

            if (AtPark)
                throw new InvalidOperationException("Cannot SlewToCoordinates while AtPark");

            if (!Tracking)
                throw new InvalidOperationException("Cannot SlewToCoordinates while NOT Tracking");

            if (!SafeAtCoordinates(ra, dec))
                throw new InvalidOperationException(string.Format("Not safe to SlewToTarget({0}, {1})", ra, dec));

            _driverInitiatedSlewing = true;

            try
            {
                SlewToCoordinates(TargetRightAscension, TargetDeclination); // sync
            }
            catch (Exception e)
            {
                _driverInitiatedSlewing = false;
                throw e;
            }
        }

        public void SyncToAltAz(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SyncToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double RightAscension, double Declination)
        {
            traceLogger.LogMessage("SyncToCoordinates", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            bool ret;

            switch (Axis)
            {
                case TelescopeAxes.axisPrimary: ret = true; break;   // Right Ascension
                case TelescopeAxes.axisSecondary: ret = true; break;   // Declination
                case TelescopeAxes.axisTertiary: ret = false; break;  // Image Rotator/Derotator
                default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
            }
            traceLogger.LogMessage("CanMoveAxis", "Get - " + Axis.ToString() + ": " + ret.ToString());

            return ret;
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equJ2000;

                traceLogger.LogMessage("EquatorialSystem", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            traceLogger.LogMessage("FindHome", "Not Implemented");
            throw new MethodNotImplementedException("FindHome");
        }

        public PierSide DestinationSideOfPier(double RightAscension, double Declination)
        {
            traceLogger.LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
        }

        public bool CanPark
        {
            get
            {
                traceLogger.LogMessage("CanPark", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                traceLogger.LogMessage("CanPulseGuide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                traceLogger.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                traceLogger.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSetPark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                traceLogger.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                traceLogger.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                traceLogger.LogMessage("CanSetTracking", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanSlew", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSlewAltAz", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSlewAltAzAsync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanSlewAsync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSync
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSync", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanSyncAltAz", "Get - " + ret.ToString());
                return ret;
            }
        }

        public bool CanUnpark
        {
            get
            {
                bool ret = true;

                traceLogger.LogMessage("CanUnpark", "Get - " + ret.ToString());
                return ret;
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                traceLogger.LogMessage("GuideRateDeclination Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
            }
            set
            {
                traceLogger.LogMessage("GuideRateDeclination Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                traceLogger.LogMessage("GuideRateRightAscension Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
            }
            set
            {
                traceLogger.LogMessage("GuideRateRightAscension Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
            }
        }

        public bool AtHome
        {
            get
            {
                bool ret = false;       // Homing is not implemented

                traceLogger.LogMessage("AtHome", "Get - " + ret.ToString());
                debugger.WriteLine(Common.Debugger.DebugLevel.DebugASCOM, string.Format("AtHome Get - {0}", ret));
                return ret;
            }
        }

        public bool CanFindHome
        {
            get
            {
                bool ret = false;

                traceLogger.LogMessage("CanFindHome", "Get - " + ret.ToString());
                return ret;
            }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            IAxisRates rates = new AxisRates(Axis);

            traceLogger.LogMessage("AxisRates", "Get - " + rates.ToString());
            return rates;
        }

        public short SlewSettleTime
        {
            get
            {
                traceLogger.LogMessage("SlewSettleTime Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }

            set
            {
                traceLogger.LogMessage("SlewSettleTime Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SlewToAltAz", String.Format("SlewToAltAz({0}, {1})", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
        }

        public void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            traceLogger.LogMessage("SlewToAltAzAsync", String.Format("SlewToAltAzAsync({0}, {1})", Azimuth, Altitude));
            throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                traceLogger.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                return rightAscensionRate;
            }

            set
            {
                traceLogger.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            traceLogger.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                PierSide side = PierSide.pierEast;

                traceLogger.LogMessage("SideOfPier Get", side.ToString());
                return side;
            }

            set
            {
                traceLogger.LogMessage("SideOfPier Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            traceLogger.LogMessage("PulseGuide", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("PulseGuide");
        }

        public ArrayList SupportedActions
        {
            get
            {
                traceLogger.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        private void CheckConnected(string message)
        {
            if (!_connected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            // or
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            //string ret = CommandString(command, raw);
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

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "First ASCOM driver for the Wise40 telescope. Version: " + 
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                traceLogger.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            get
            {
                traceLogger.LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                string name = "Wise40 Telescope";
                traceLogger.LogMessage("Name Get", name);
                return name;
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
                traceLogger.Enabled = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, "false"));
                debugger.Level = Convert.ToUInt32(driverProfile.GetValue(driverID, debugLevelProfileName, string.Empty, "0"));
                _enslaveDome = Convert.ToBoolean(driverProfile.GetValue(driverID, enslaveDomeProfileName, string.Empty, "false"));
                wisesite.astrometricAccuracy =
                    driverProfile.GetValue(driverID, astrometricAccuracyProfileName, string.Empty, "Full") == "Full" ?
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
                driverProfile.WriteValue(driverID, traceStateProfileName, traceLogger.Enabled.ToString());
                driverProfile.WriteValue(driverID, astrometricAccuracyProfileName, wisesite.astrometricAccuracy == Accuracy.Full ? "Full" : "Reduced");
                driverProfile.WriteValue(driverID, debugLevelProfileName, debugger.Level.ToString());
                driverProfile.WriteValue(driverID, enslaveDomeProfileName, _enslaveDome.ToString());
            }
        }
    }
}
